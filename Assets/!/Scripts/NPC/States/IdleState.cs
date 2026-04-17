using UnityEngine;

public class DialogueState : BaseState
{
    public DialogueState() { }

    public override void Enter()
    {
        Npc.Animator.SetBool("IsDialogue", true);
    }

    public override void Perform()
    {
    }

    public override void Exit()
    {
        Npc.Animator.SetBool("IsDialogue", false);
    }
}
