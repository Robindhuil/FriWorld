
public abstract class BaseState
{
    public Npc npc;
    public StateMachine stateMachine;
    public abstract void Enter();
    public abstract void Perform();
    public abstract void Exit();
}
