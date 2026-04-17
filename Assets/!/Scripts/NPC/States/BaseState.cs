
public abstract class BaseState
{
    public Npc Npc { get; set; }
    public StateMachine StateMachine { get; set; }
    public abstract void Enter();
    public abstract void Perform();
    public abstract void Exit();
}
