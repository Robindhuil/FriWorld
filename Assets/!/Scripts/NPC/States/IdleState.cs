using UnityEngine;

public class DialogueState : BaseState
{
    public DialogueState() { }

    public override void Enter()
    {
        npc.Animator.SetBool("IsDialogue", true);
    }

    public override void Perform()
    {
    }

    public override void Exit()
    {
        npc.Animator.SetBool("IsDialogue", false);
    }
}
