using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Navigator
{
    /// <summary>
    /// When and which way a door opens for the flight. Everything here is a function of the
    /// distance along the path, so seeking backwards closes the doors again by itself.
    /// </summary>
    public static class DoorPassage
    {
        /// <summary>DoorRotation of a fully open door, the value FriWorld's Door script uses.</summary>
        public const float OpenAngle = 90.9f;

        // A passed door starts opening this far ahead of the camera and is fully open here…
        private const float OpenFrom = 3f;
        private const float OpenBy = 1f;
        // …and starts closing this far behind it, shut here.
        private const float CloseFrom = 1.5f;
        private const float CloseBy = 3.5f;

        // Past the edge of a leaf still counts as through it: the path through a double door
        // crosses where the two leaves meet.
        private const float WidthMargin = 0.3f;
        private const float FloorTolerance = 0.5f;

        /// <summary>
        /// Whether the path goes through the doorway — crosses the plane of the closed leaf within
        /// its width, on its floor — and if so, how far along and in which direction.
        /// </summary>
        public static bool TryFindPass(IReadOnlyList<Vector3> path, float step, DoorLeaf door,
                                       out float pass, out Vector3 direction)
        {
            for (int i = 0; i + 1 < path.Count; i++)
            {
                float a = Vector3.Dot(path[i] - door.center, door.normal);
                float b = Vector3.Dot(path[i + 1] - door.center, door.normal);
                if ((a < 0f) == (b < 0f))
                    continue;

                float f = a / (a - b);
                Vector3 crossing = Vector3.Lerp(path[i], path[i + 1], f);
                if (Mathf.Abs(Vector3.Dot(crossing - door.center, door.along)) > door.halfWidth + WidthMargin)
                    continue;
                if (Mathf.Abs(crossing.y - door.bottom) > FloorTolerance)
                    continue;

                pass = (i + f) * step;
                direction = path[i + 1] - path[i];
                direction.y = 0f;
                direction.Normalize();
                return true;
            }

            pass = 0f;
            direction = Vector3.zero;
            return false;
        }

        /// <summary>0 closed … 1 open, for a door the camera passes at <paramref name="pass"/>.</summary>
        public static float RouteOpening(float distance, float pass)
        {
            float opening = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pass - OpenFrom, pass - OpenBy, distance));
            float closing = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(pass + CloseFrom, pass + CloseBy, distance));
            return Mathf.Min(opening, closing);
        }

        /// <summary>
        /// DoorRotation that swings the leaf away from a camera coming in <paramref name="approach"/>.
        /// The same rule as FriWorld's Door script: when a positive yaw about the hinge moves the
        /// leaf away from whoever comes, it uses -90.9.
        /// </summary>
        public static float OpenRotation(Vector3 hinge, Vector3 center, Vector3 approach)
        {
            Vector3 arm = center - hinge;
            arm.y = 0f;
            Vector3 swing = Vector3.Cross(Vector3.up, arm);
            return Vector3.Dot(swing, approach) >= 0f ? -OpenAngle : OpenAngle;
        }
    }
}
