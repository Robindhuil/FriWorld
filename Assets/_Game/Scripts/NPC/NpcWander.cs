using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Walks an NPC between the waypoints of a PathWay and nothing else.
///
/// Deliberately not the old Npc + StateMachine pair: this carries no dialogue, no quests and
/// no Animator, so a character built by CharacterBuilder can walk the faculty before any of
/// that is rewritten. See docs/2026-08-28-npc-skripty-na-prerobenie.md.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NpcWander : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Waypoints to wander between. Without one the NPC just stands there.")]
    [SerializeField] private PathWay path;

    [Tooltip("Pick the next waypoint at random rather than walking the path in order.")]
    [SerializeField] private bool randomOrder = true;

    [Header("Timing")]
    [Tooltip("How close counts as arrived, on top of the agent's stopping distance.")]
    [SerializeField] private float arriveDistance = 5f;

    [Tooltip("Seconds to linger once arrived, before heading somewhere else.")]
    [SerializeField] private float waitOnArrival = 3f;

    // Keeping an NPC off the wall: the same lateral nudge WonderState used, kept because it is
    // the difference between people walking down a corridor and people grinding along its side.
    const float EdgeBiasStartDistance = 0.8f;
    const float EdgeBiasProbeOffset = 0.35f;
    const float EdgeBiasMaxLateralSpeed = 0.65f;
    const float EdgeBiasSmoothing = 6f;
    const float EdgeBiasDeadZone = 0.04f;
    const float EdgeBiasMinVelocity = 0.05f;

    NavMeshAgent agent;
    System.Random random;
    int waypointIndex;
    float waitTimer;
    float smoothedLateralBias;
    Vector3 lastMoveForward = Vector3.forward;

    /// <summary>Called by the spawner before Start so a run stays reproducible.</summary>
    public void Configure(PathWay wanderPath, int seed)
    {
        path = wanderPath;
        random = new System.Random(seed);
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (random == null) random = new System.Random(GetInstanceID());
    }

    void Start()
    {
        if (!HasPath()) return;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        waypointIndex = random.Next(0, path.Waypoints.Count);
        Head();
    }

    void Update()
    {
        if (!HasPath()) return;
        if (!agent.isOnNavMesh) return;

        Cycle();
        ApplyEdgeCenteringBias();
    }

    bool HasPath() => path != null && path.Waypoints != null && path.Waypoints.Count > 0;

    void Head()
    {
        var waypoint = path.Waypoints[waypointIndex];
        if (waypoint != null) agent.SetDestination(waypoint.position);
    }

    void Cycle()
    {
        if (agent.pathPending) return;
        if (agent.remainingDistance >= agent.stoppingDistance + arriveDistance) return;

        waitTimer += Time.deltaTime;
        if (waitTimer <= waitOnArrival) return;

        if (randomOrder) waypointIndex = random.Next(0, path.Waypoints.Count);
        else waypointIndex = (waypointIndex + 1) % path.Waypoints.Count;

        Head();
        waitTimer = 0f;
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
