using UnityEngine;

namespace FriWorld.Crowd
{
    /// <summary>
    /// Walks an NpcActor between the waypoints of a PathWay.
    ///
    /// The only thing driving a body today. When the agent simulation arrives it takes this role
    /// over — reading a timetable instead of a list of points — and NpcActor does not change,
    /// because it never knew who was calling it.
    /// </summary>
    [RequireComponent(typeof(NpcActor))]
    public sealed class WaypointDirector : MonoBehaviour
    {
        [SerializeField] PathWay path;
        [SerializeField] bool randomOrder = true;

        [Tooltip("Seconds to linger on arrival before heading somewhere else.")]
        [SerializeField] float waitOnArrival = 3f;

        NpcActor actor;
        WaypointCursor cursor;
        float waitTimer;
        bool waiting;

        /// <summary>Called by the spawner before Start, so a run stays reproducible.</summary>
        public void Configure(PathWay wanderPath, int seed)
        {
            path = wanderPath;
            cursor = new WaypointCursor(Count, randomOrder, new System.Random(seed));
        }

        int Count => path != null && path.Waypoints != null ? path.Waypoints.Count : 0;

        void Awake()
        {
            actor = GetComponent<NpcActor>();
            if (cursor == null)
                cursor = new WaypointCursor(Count, randomOrder, new System.Random(GetInstanceID()));
        }

        void Update()
        {
            if (Count == 0 || !actor.IsReady) return;

            if (!actor.HasArrived)
            {
                waiting = false;
                return;
            }

            if (!waiting)
            {
                waiting = true;
                waitTimer = 0f;
            }

            waitTimer += Time.deltaTime;
            if (waitTimer < waitOnArrival) return;

            int index = cursor.Next();
            if (index < 0) return;

            var waypoint = path.Waypoints[index];
            if (waypoint != null) actor.GoTo(waypoint.position);

            waiting = false;
        }
    }
}
