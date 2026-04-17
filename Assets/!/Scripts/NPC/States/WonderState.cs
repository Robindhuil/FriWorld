using UnityEngine;
using UnityEngine.AI;

public class WonderState : BaseState
{
    public int WaypointIndex { get; set; }

    [SerializeField]
    private float waitTimer;

    private System.Random random;
    private bool useRandomMovement;

    private const float EdgeBiasStartDistance = 0.8f;
    private const float EdgeBiasProbeOffset = 0.35f;
    private const float EdgeBiasMaxLateralSpeed = 0.65f;
    private const float EdgeBiasSmoothing = 6f;
    private const float EdgeBiasDeadZone = 0.04f;
    private const float EdgeBiasMinVelocity = 0.05f;

    private float smoothedLateralBias;
    private Vector3 lastMoveForward = Vector3.forward;

    public WonderState()
    {
        int seed = System.Environment.TickCount + Random.Range(0, 10000);
        random = new System.Random(seed);
    }

    public override void Enter()
    {
        useRandomMovement = Npc.RandomMovement;

        // Better local steering quality helps reduce wall-hugging.
        Npc.Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        if (Npc.CanMove)
        {
            Npc.Agent.SetDestination(Npc.Path.Waypoints[WaypointIndex].position);
            Npc.Animator.SetBool("IsWalking", true);
        }
    }

    public override void Perform()
    {
        if (!Npc.IsInDialogue && Npc.CanMove)
        {
            WonderCycle();
            ApplyEdgeCenteringBias();

            float velocityMagnitude = Npc.Agent.velocity.magnitude;

            Npc.Animator.SetBool("IsWalking", velocityMagnitude > 0.1f);
        }
    }

    public override void Exit()
    {
        Npc.nextWaypointIndex = WaypointIndex;
        Npc.Animator.SetBool("IsWalking", false);
    }

    public void WonderCycle()
    {
        if (Npc.Agent.remainingDistance < Npc.Agent.stoppingDistance + 5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer > 3)
            {
                if (useRandomMovement)
                {
                    WaypointIndex = random.Next(0, Npc.Path.Waypoints.Count);
                }
                else
                {
                    if (WaypointIndex < Npc.Path.Waypoints.Count - 1)
                        WaypointIndex++;
                    else
                        WaypointIndex = 0;
                }

                Npc.Agent.SetDestination(Npc.Path.Waypoints[WaypointIndex].position);
                waitTimer = 0f;
            }
        }
    }

    private void ApplyEdgeCenteringBias()
    {
        if (Npc.Agent == null || !Npc.Agent.hasPath || Npc.Agent.pathPending)
        {
            return;
        }

        if (Npc.Agent.velocity.sqrMagnitude < EdgeBiasMinVelocity * EdgeBiasMinVelocity)
        {
            return;
        }

        Vector3 position = Npc.transform.position;

        Vector3 forward = Npc.Agent.desiredVelocity;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
        {
            forward.Normalize();
            lastMoveForward = forward;
        }
        else
        {
            forward = lastMoveForward;
        }

        if (
            !NavMeshCenteringUtility.TryCalculateNormalizedEdgeBias(
                position,
                forward,
                EdgeBiasProbeOffset,
                EdgeBiasStartDistance,
                EdgeBiasDeadZone,
                out float targetBias,
                out Vector3 right
            )
        )
        {
            return;
        }

        float blendT = 1f - Mathf.Exp(-EdgeBiasSmoothing * Time.deltaTime);
        smoothedLateralBias = Mathf.Lerp(smoothedLateralBias, targetBias, blendT);

        Vector3 lateralMove =
            right * (smoothedLateralBias * EdgeBiasMaxLateralSpeed * Time.deltaTime);
        Npc.Agent.Move(lateralMove);
    }
}
