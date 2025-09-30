using UnityEngine;

public class WonderState : BaseState
{
    public int waypointIndex;
    public float waitTimer;
    private System.Random random;
    private bool useRandomMovement;

    public WonderState()
    {
        int seed = System.Environment.TickCount + Random.Range(0, 10000);
        random = new System.Random(seed);
    }

    public override void Enter()
    {
        useRandomMovement = npc.randomMovement;

        if (npc.canMove)
        {
            npc.Agent.SetDestination(npc.path.waypoints[waypointIndex].position);
            npc.Animator.SetBool("IsWalking", true);
        }
    }

    public override void Perform()
    {
        if (!npc.isInDialogue && npc.canMove)
        {
            WonderCycle();

            float velocityMagnitude = npc.Agent.velocity.magnitude;

            npc.Animator.SetBool("IsWalking", velocityMagnitude > 0.1f);
        }
    }

    public override void Exit()
    {
        npc.nextWaypointIndex = waypointIndex;
        npc.Animator.SetBool("IsWalking", false);
    }

    public void WonderCycle()
    {
        if (npc.Agent.remainingDistance < npc.Agent.stoppingDistance + 5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer > 3)
            {
                if (useRandomMovement)
                {
                    waypointIndex = random.Next(0, npc.path.waypoints.Count);
                }
                else
                {
                    if (waypointIndex < npc.path.waypoints.Count - 1)
                        waypointIndex++;
                    else
                        waypointIndex = 0;
                }

                npc.Agent.SetDestination(npc.path.waypoints[waypointIndex].position);
                waitTimer = 0f;
            }
        }
    }
}
