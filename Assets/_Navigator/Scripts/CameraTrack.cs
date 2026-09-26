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
    /// Built once from a navmesh path: resampled evenly, kept off the walls where the corridor
    /// allows it, smoothed, and timed with a trapezoid speed profile (ease in, cruise, ease out).
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
                cruiseSpeed = 4f,
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

        private readonly Vector3[] points;     // on the navmesh, evenly spaced, last one at Length
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
            var pts = Resample(corners, Spacing, out _, out _);
            for (int pass = 0; pass < Passes; pass++)
            {
                KeepOffWalls(pts, settings.wallClearance, filter);
                if (pass < Passes - 1)
                    pts = Smooth(pts, SmoothRadius);
            }

            pts = Resample(pts, Spacing, out float length, out float step);
            return new CameraTrack(pts, step, length, startPosition, startRotation, endFacing, settings);
        }

        private CameraTrack(Vector3[] points, float step, float length, Vector3 startPosition, Quaternion startRotation,
                            Vector3 endFacing, Settings settings)
        {
            this.points = points;
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
            Vector3 floor = PointAt(d);

            // 1 at the start, 0 once the camera is turnDistance along: the placed pose fades out.
            float placed = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, settings.turnDistance, d));
            position = floor + Vector3.up * settings.eyeHeight + startOffset * placed;

            // Average a few points ahead so the view does not twitch at every corner. Clamped to
            // the end rather than dropped, so the direction stays continuous as the path runs out.
            Vector3 ahead = Vector3.zero;
            for (int k = 1; k <= 4; k++)
                ahead += PointAt(Mathf.Min(d + settings.lookAhead * k / 4f, Length));
            Vector3 dir = ahead / 4f - floor;
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

        private Vector3 PointAt(float d)
        {
            float f = Mathf.Clamp(d / step, 0f, points.Length - 1);
            int i = Mathf.Min((int)f, points.Length - 2);
            return Vector3.Lerp(points[i], points[i + 1], f - i);
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

        /// <summary>Snaps points onto the navmesh and pushes them away from its edges.</summary>
        private static void KeepOffWalls(Vector3[] pts, float clearance, NavMeshQueryFilter filter)
        {
            // Endpoints stay: the start is the reception, the end is the spot in front of the door.
            for (int i = 1; i < pts.Length - 1; i++)
            {
                Vector3 p = pts[i];
                if (NavMesh.SamplePosition(p, out NavMeshHit onMesh, 0.6f, filter))
                    p = onMesh.position;

                if (NavMesh.FindClosestEdge(p, out NavMeshHit edge, filter) && edge.distance < clearance)
                {
                    Vector3 away = p - edge.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 1e-6f)
                        away = edge.normal;
                    Vector3 pushed = p + away.normalized * (clearance - edge.distance);
                    if (NavMesh.SamplePosition(pushed, out NavMeshHit back, 0.3f, filter))
                        p = back.position;
                }

                pts[i] = p;
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
