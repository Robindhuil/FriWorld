namespace FriWorld.Crowd
{
    /// <summary>
    /// Which waypoint comes next.
    ///
    /// Pure C# on purpose: everything else in this layer hangs off a NavMeshAgent and cannot be
    /// tested without a baked navmesh, so the one decision worth testing is kept out of Unity.
    /// </summary>
    public sealed class WaypointCursor
    {
        readonly int count;
        readonly bool randomOrder;
        readonly System.Random random;

        int current = -1;

        public WaypointCursor(int count, bool randomOrder, System.Random random)
        {
            this.count = count;
            this.randomOrder = randomOrder;
            this.random = random;
        }

        /// <summary>Start from a known point without counting it as a visit.</summary>
        public void Reset(int index) => current = index;

        /// <returns>The next waypoint index, or -1 when there are none.</returns>
        public int Next()
        {
            if (count <= 0) return -1;
            if (count == 1) return current = 0;

            if (!randomOrder) return current = (current + 1) % count;
            if (current < 0) return current = random.Next(0, count);

            // Step to one of the others rather than drawing and rejecting: an NPC sent to the
            // point it is already standing on reads as frozen, not as pausing.
            int step = random.Next(1, count);
            return current = (current + step) % count;
        }
    }
}
