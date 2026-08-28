namespace FriWorld.Crowd
{
    /// <summary>
    /// What an NPC is doing, published by the body and read by nobody yet.
    ///
    /// This is the seam between behaviour and animation. Behaviour must never call
    /// Animator.SetBool — the old WonderState does, which is exactly why it throws on a
    /// generated character that has no animator. The body says what it is doing; a view
    /// component will later turn that into animator parameters, and a character with no
    /// animator keeps working.
    ///
    /// Two values today. The list grows with the animations, not with the behaviour.
    /// </summary>
    public enum NpcActivity
    {
        Idle,
        Walking,
    }
}
