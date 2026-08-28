using UnityEngine;
using UnityEngine.AI;

namespace FriWorld.Crowd
{
    /// <summary>
    /// An NPC body. It walks where it is told and decides nothing.
    ///
    /// That passivity is the whole point of this class. Today a WaypointDirector calls GoTo;
    /// later the agent simulation will, from a timetable, for whichever NPCs are near enough to
    /// be worth drawing. Because the body has no opinion about where it goes, that is a change
    /// of caller rather than a rewrite — which is exactly what the old Npc and the short-lived
    /// NpcWander both got wrong by ticking themselves.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcActor : MonoBehaviour
    {
        [Tooltip("How close counts as arrived, on top of the agent's own stopping distance.")]
        [SerializeField] float arriveDistance = 1.5f;

        // Keeping an NPC off the wall: the lateral nudge from the old WonderState, the one part
        // of that class worth carrying over. Without it they grind along corridor walls.
        const float EdgeBiasStartDistance = 0.8f;
        const float EdgeBiasProbeOffset = 0.35f;
        const float EdgeBiasMaxLateralSpeed = 0.65f;
        const float EdgeBiasSmoothing = 6f;
        const float EdgeBiasDeadZone = 0.04f;
        const float EdgeBiasMinVelocity = 0.05f;

        NavMeshAgent agent;
        float smoothedLateralBias;
        Vector3 lastMoveForward = Vector3.forward;

        public NpcActivity Activity { get; private set; } = NpcActivity.Idle;

        /// <summary>False until the agent is actually on the navmesh — a body spawned off it
        /// cannot be told to go anywhere yet.</summary>
        public bool IsReady => agent != null && agent.isOnNavMesh;

        /// <summary>
        /// Nothing left to walk to.
        ///
        /// A path that is not complete counts as arrived: the destination is unreachable, and an
        /// incomplete path leaves remainingDistance at Infinity, so waiting for the distance to
        /// come down is how an NPC freezes on the spot for the rest of the session.
        /// </summary>
        public bool HasArrived
        {
            get
            {
                if (!IsReady || agent.pathPending) return false;
                if (!agent.hasPath) return true;
                if (agent.pathStatus != NavMeshPathStatus.PathComplete) return true;
                return agent.remainingDistance <= agent.stoppingDistance + arriveDistance;
            }
        }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public void GoTo(Vector3 destination)
        {
            if (!IsReady) return;
            agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!IsReady) return;
            agent.ResetPath();
        }

        void Update()
        {
            if (!IsReady) return;

            Activity = agent.velocity.sqrMagnitude > EdgeBiasMinVelocity * EdgeBiasMinVelocity
                ? NpcActivity.Walking
                : NpcActivity.Idle;

            ApplyEdgeCenteringBias();
        }

        void ApplyEdgeCenteringBias()
        {
            if (!agent.hasPath || agent.pathPending) return;
            if (agent.velocity.sqrMagnitude < EdgeBiasMinVelocity * EdgeBiasMinVelocity) return;

            Vector3 forward = agent.desiredVelocity;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                lastMoveForward = forward;
            }
            else forward = lastMoveForward;

            if (!NavMeshCenteringUtility.TryCalculateNormalizedEdgeBias(
                    transform.position, forward,
                    EdgeBiasProbeOffset, EdgeBiasStartDistance, EdgeBiasDeadZone,
                    out float targetBias, out Vector3 right))
                return;

            float blend = 1f - Mathf.Exp(-EdgeBiasSmoothing * Time.deltaTime);
            smoothedLateralBias = Mathf.Lerp(smoothedLateralBias, targetBias, blend);

            agent.Move(right * (smoothedLateralBias * EdgeBiasMaxLateralSpeed * Time.deltaTime));
        }
    }
}
