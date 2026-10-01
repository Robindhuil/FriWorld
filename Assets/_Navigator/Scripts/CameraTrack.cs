using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace FriWorld.Navigator
{
    /// <summary>
    /// The camera flight as a pure function of time: <see cref="Evaluate"/> gives the pose for
    /// any t, so pausing, seeking and changing speed are only a different t — nothing
    /// accumulates frame to frame.
    ///
    /// Built once from a navmesh path: resampled evenly, set on the navmesh floor, kept off the
    /// walls where the corridor allows it, smoothed, and timed with a trapezoid speed profile
    /// (ease in, cruise, ease out).
    /// </summary>
    public sealed class CameraTrack
    {
        [Serializable]
        public struct Settings
        {
            [Tooltip("Camera height above the navmesh (m).")]
            public float eyeHeight;

            [Tooltip("Speed along the path once it has eased in (m/s).")]
            public float cruiseSpeed;

            [Tooltip("Time to reach cruise speed, and to come to a stop at the end (s).")]
            public float easeTime;

            [Tooltip("How far ahead along the path the camera looks (m).")]
            public float lookAhead;

            [Tooltip("Keep the path this far from navmesh edges where the corridor is wide enough (m).")]
            public float wallClearance;

            [Tooltip("Over the first metres the camera leaves its placed pose for the path; over the last ones it turns to the door (m).")]
            [FormerlySerializedAs("endTurnDistance")]
            public float turnDistance;

            [Tooltip("Steepest the camera looks up or down, e.g. on stairs (degrees).")]
            public float maxPitch;

            public static Settings Default => new Settings
            {
                eyeHeight = 1.6f,
                cruiseSpeed = 3f,
                easeTime = 1.5f,
                lookAhead = 4f,
                wallClearance = 0.6f,
                turnDistance = 3f,
                maxPitch = 20f,
            };
        }

        private const float Spacing = 0.25f;   // distance between resampled path points (m)
        private const int SmoothRadius = 4;    // moving-average half-window, in points
        private const int Passes = 3;
        private const int LookSmoothRadius = 8;  // the view follows a much calmer copy of the path

        private readonly Vector3[] points;     // on the navmesh, evenly spaced, last one at Length
        private readonly Vector3[] lookPoints; // same spacing, smoothed hard; only for the view direction
        private readonly float step;
        private readonly float cruise;
        private readonly Vector3 endFacing;
        private readonly Vector3 startOffset;      // placed camera minus the path's first eye point
        private readonly Quaternion startRotation;
        private readonly Settings settings;

        public float Length { get; }
        public float Duration { get; }

        /// <param name="corners">Navmesh path from below the placed camera to the door anchor.</param>
        /// <param name="startPosition">Where the camera was placed; the flight begins exactly there.</param>
        /// <param name="startRotation">How the camera was placed; the flight begins looking that way.</param>
        public static CameraTrack Build(Vector3[] corners, Vector3 startPosition, Quaternion startRotation,
                                        Vector3 endFacing, Settings settings, NavMeshQueryFilter filter)
        {
            // The corners only mark where the path turns on the floor plan, not where it starts to
            // climb: a corridor that runs straight onto a flight of stairs has no corner at its foot,
            // and the line between two corners cuts through the air or the floor. Heights are
            // therefore read off the navmesh, here on the raw path — once the passes below cut
            // across a stairwell, the flight beside or above is just as near. Read raw, they carry
            // the navmesh's centimetre noise; the smoothing after each pass takes it out.
            var pts = Resample(corners, Spacing, out _, out _);
            FollowFloor(pts, filter);
            for (int pass = 0; pass < Passes; pass++)
            {
                KeepOffWalls(pts, settings.wallClearance, filter);
                // Always smooth last: each push depends on which edge happens to be nearest,
                // so an unsmoothed push leaves a sideways zigzag every Spacing metres.
                pts = Smooth(pts, SmoothRadius);
            }

            pts = Resample(pts, Spacing, out float length, out float step);
            return new CameraTrack(pts, step, length, startPosition, startRotation, endFacing, settings);
        }

        private CameraTrack(Vector3[] points, float step, float length, Vector3 startPosition, Quaternion startRotation,
                            Vector3 endFacing, Settings settings)
        {
            this.points = points;
            // Centimetre ripples left in the path are invisible as position but, seen from a
            // few metres ahead, swing the view by degrees several times a second. Direction is
            // therefore taken from a copy smoothed over about ±2 m, which also makes the camera
            // start turning a little before a corner, as a person would.
            lookPoints = points;
            for (int pass = 0; pass < Passes; pass++)
                lookPoints = Smooth(lookPoints, LookSmoothRadius);
            this.step = step;
            this.settings = settings;
            this.startRotation = startRotation;
            startOffset = startPosition - (points[0] + Vector3.up * settings.eyeHeight);
            Length = length;

            endFacing.y = 0f;
            this.endFacing = endFacing.sqrMagnitude > 1e-6f ? endFacing.normalized : Vector3.forward;

            float ease = Mathf.Max(0.01f, settings.easeTime);
            cruise = Mathf.Max(0.01f, settings.cruiseSpeed);
            if (length < cruise * ease)
                cruise = Mathf.Max(0.01f, length / ease);   // too short to reach cruise speed
            Duration = length < 1e-3f ? 0f : length / cruise + ease;
        }

        /// <summary>Camera pose at time <paramref name="t"/> seconds into the flight.</summary>
        public void Evaluate(float t, out Vector3 position, out Quaternion rotation)
        {
            float d = DistanceAt(t);
            Vector3 floor = PointAt(points, d);

            // 1 at the start, 0 once the camera is turnDistance along: the placed pose fades out.
            float placed = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, settings.turnDistance, d));
            position = floor + Vector3.up * settings.eyeHeight + startOffset * placed;

            // Both ends of the direction come from the calm copy, so ripples in the real path
            // cannot reach the view. Clamped to the end rather than dropped, so the direction
            // stays continuous as the path runs out.
            Vector3 dir = PointAt(lookPoints, Mathf.Min(d + settings.lookAhead, Length)) - PointAt(lookPoints, d);
            if (dir.sqrMagnitude < 0.01f)
                dir = endFacing;
            dir = ClampPitch(dir.normalized, settings.maxPitch);

            float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Length - settings.turnDistance, Length, d));
            dir = Vector3.Slerp(dir, endFacing, turn);
            rotation = Quaternion.Slerp(Quaternion.LookRotation(dir, Vector3.up), startRotation, placed);
        }

        /// <summary>Distance along the path at time t: ease in, cruise, ease out.</summary>
        private float DistanceAt(float t)
        {
            if (t <= 0f || Duration <= 0f)
                return 0f;
            if (t >= Duration)
                return Length;

            float ease = Duration - Length / cruise;
            float accel = cruise / ease;
            if (t < ease)
                return 0.5f * accel * t * t;
            if (t > Duration - ease)
            {
                float left = Duration - t;
                return Length - 0.5f * accel * left * left;
            }

            return 0.5f * cruise * ease + cruise * (t - ease);
        }

        /// <summary>
        /// Catmull-Rom through the evenly spaced points. A straight lerp between them has a kink at
        /// every point, which at 60 fps reads as a small shake even on a straight corridor.
        /// </summary>
        private Vector3 PointAt(Vector3[] pts, float d)
        {
            int last = pts.Length - 1;
            float f = Mathf.Clamp(d / step, 0f, last);
            int i = Mathf.Min((int)f, last - 1);
            float u = f - i;

            Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(i + 2, last)];
            return 0.5f * (2f * p1
                           + (p2 - p0) * u
                           + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (u * u)
                           + (3f * p1 - p0 - 3f * p2 + p3) * (u * u * u));
        }

        private static Vector3 ClampPitch(Vector3 dir, float maxPitch)
        {
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 1e-6f)
                return dir;
            float pitch = Mathf.Atan2(dir.y, flat.magnitude) * Mathf.Rad2Deg;
            float clamped = Mathf.Clamp(pitch, -maxPitch, maxPitch) * Mathf.Deg2Rad;
            return flat.normalized * Mathf.Cos(clamped) + Vector3.up * Mathf.Sin(clamped);
        }

        /// <summary>
        /// Sets each point's height to the navmesh under it, walking from the start: every lookup
        /// begins at the height of the point before, so on a staircase it lands on the flight the
        /// path is on, never the one above, below or beside it.
        /// </summary>
        private static void FollowFloor(Vector3[] pts, NavMeshQueryFilter filter)
        {
            // Endpoints stay: the start is under the placed camera, the end is in front of the door.
            for (int i = 1; i < pts.Length - 1; i++)
            {
                // On a slope the nearest navmesh point lies off to the side, a little above the
                // floor straight below; a few lookups converge on that. Off the navmesh, the
                // height of the point before carries on.
                float y = pts[i - 1].y;
                for (int k = 0; k < 3; k++)
                {
                    if (!NavMesh.SamplePosition(new Vector3(pts[i].x, y, pts[i].z), out NavMeshHit hit, 0.5f, filter))
                        break;
                    y = hit.position.y;
                }

                pts[i].y = y;
            }
        }

        /// <summary>
        /// Pushes points sideways away from the navmesh edges. Only x and z move: the navmesh is
        /// queried for where the edge is, but its surface height is never copied into the path.
        /// </summary>
        private static void KeepOffWalls(Vector3[] pts, float clearance, NavMeshQueryFilter filter)
        {
            // Endpoints stay: the start is under the placed camera, the end is in front of the door.
            for (int i = 1; i < pts.Length - 1; i++)
            {
                Vector3 p = pts[i];
                if (!NavMesh.SamplePosition(p, out NavMeshHit onMesh, 0.6f, filter))
                    continue;
                if (!NavMesh.FindClosestEdge(onMesh.position, out NavMeshHit edge, filter) || edge.distance >= clearance)
                    continue;

                Vector3 away = onMesh.position - edge.position;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-6f)
                    away = new Vector3(edge.normal.x, 0f, edge.normal.z);
                if (away.sqrMagnitude < 1e-6f)
                    continue;

                Vector3 pushed = onMesh.position + away.normalized * (clearance - edge.distance);
                if (NavMesh.SamplePosition(pushed, out NavMeshHit back, 0.3f, filter))
                    pts[i] = new Vector3(back.position.x, p.y, back.position.z);
            }
        }

        private static Vector3[] Smooth(Vector3[] pts, int radius)
        {
            var result = new Vector3[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                // The window shrinks towards the ends, so the endpoints never move.
                int r = Mathf.Min(radius, Mathf.Min(i, pts.Length - 1 - i));
                Vector3 sum = Vector3.zero;
                for (int k = -r; k <= r; k++)
                    sum += pts[i + k];
                result[i] = sum / (2 * r + 1);
            }

            return result;
        }

        /// <summary>Evenly spaced points along a polyline; the last one lands exactly on its end.</summary>
        private static Vector3[] Resample(IList<Vector3> poly, float spacing, out float length, out float step)
        {
            length = 0f;
            var cumulative = new float[poly.Count];
            for (int i = 1; i < poly.Count; i++)
            {
                length += Vector3.Distance(poly[i - 1], poly[i]);
                cumulative[i] = length;
            }

            if (poly.Count < 2 || length < 1e-3f)
            {
                Vector3 p = poly.Count > 0 ? poly[0] : Vector3.zero;
                step = 1f;
                length = 0f;
                return new[] { p, p };
            }

            int n = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
            step = length / n;
            var result = new Vector3[n + 1];
            int seg = 0;
            for (int k = 0; k <= n; k++)
            {
                float d = k * step;
                while (seg < poly.Count - 2 && cumulative[seg + 1] < d)
                    seg++;
                float segLength = cumulative[seg + 1] - cumulative[seg];
                float f = segLength > 1e-6f ? (d - cumulative[seg]) / segLength : 0f;
                result[k] = Vector3.Lerp(poly[seg], poly[seg + 1], Mathf.Clamp01(f));
            }

            return result;
        }
    }
}
