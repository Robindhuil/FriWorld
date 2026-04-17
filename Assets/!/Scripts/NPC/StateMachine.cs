using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private BaseState activeState;
    private WonderState wonderState;

    public void Initialise()
    {
        wonderState = new WonderState();
        ChangeState(wonderState);
    }

    void Start()
    {

    }

    void Update()
    {
        if (activeState != null)
        {
            activeState.Perform();
        }
    }
    public void ChangeState(BaseState newState)
    {
        if (activeState != null)
        {
            activeState.Exit();
        }
        activeState = newState;

        if (activeState != null)
        {
            activeState.StateMachine = this;
            activeState.Npc = GetComponent<Npc>();
            activeState.Enter();
        }
    }

    public void PerformState()
    {
        if (activeState != null)
        {
            activeState.Perform();
        }
    }
}
