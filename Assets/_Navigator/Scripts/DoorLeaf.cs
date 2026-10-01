using UnityEngine;

namespace FriWorld.Navigator
{
    /// <summary>
    /// A closed door leaf as the flight sees it: where its doorway is, which way it faces, how wide
    /// it is and which floor it stands on. Measured once, while every door is still closed.
    /// </summary>
    public struct DoorLeaf
    {
        public Vector3 center;
        public Vector3 normal;     // horizontal, the way the leaf faces; a path through the doorway crosses it
        public Vector3 along;      // horizontal, across the doorway
        public float halfWidth;
        public float bottom;       // height of the floor the leaf stands on

        /// <summary>
        /// Measures the biggest mesh under <paramref name="door"/>: the axis closest to vertical is
        /// its height, the thinner of the other two is the one it faces, the wider one its width.
        /// </summary>
        public static bool TryMeasure(Transform door, out DoorLeaf leaf)
        {
            leaf = default;
            MeshFilter main = null;
            float biggest = -1f;
            foreach (var filter in door.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                float size = Vector3.Scale(filter.sharedMesh.bounds.size, filter.transform.lossyScale).sqrMagnitude;
                if (size > biggest)
                {
                    biggest = size;
                    main = filter;
                }
            }

            if (main == null)
                return false;

            Transform t = main.transform;
            Bounds bounds = main.sharedMesh.bounds;
            Vector3 scale = t.lossyScale;
            Vector3[] axes = { t.right, t.up, t.forward };
            float[] half =
            {
                Mathf.Abs(bounds.extents.x * scale.x),
                Mathf.Abs(bounds.extents.y * scale.y),
                Mathf.Abs(bounds.extents.z * scale.z),
            };

            int up = 0;
            for (int i = 1; i < 3; i++)
            {
                if (Mathf.Abs(axes[i].y) > Mathf.Abs(axes[up].y))
                    up = i;
            }

            int a = (up + 1) % 3;
            int b = (up + 2) % 3;
            int thin = half[a] <= half[b] ? a : b;
            int wide = thin == a ? b : a;

            Vector3 normal = Flat(axes[thin]);
            Vector3 along = Flat(axes[wide]);
            if (normal.sqrMagnitude < 1e-6f || along.sqrMagnitude < 1e-6f)
                return false;

            Vector3 center = t.TransformPoint(bounds.center);
            leaf = new DoorLeaf
            {
                center = center,
                normal = normal.normalized,
                along = along.normalized,
                halfWidth = half[wide],
                bottom = center.y - half[up],
            };
            return true;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
