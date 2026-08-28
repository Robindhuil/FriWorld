using System;

namespace FriWorld.Character
{
    /// <summary>
    /// The pieces of bare skin a clothing preset can hide. One bit each, so a preset's whole
    /// coverage is a single int and several presets combine with OR.
    ///
    /// The head is deliberately missing: head shape is a slot class, so the head arrives from a
    /// preset and is never hidden. male_body_head exists in the prefab today only as a stand-in
    /// until head presets are modelled.
    /// </summary>
    [Flags]
    public enum BodySection
    {
        None      = 0,
        Neck      = 1 << 0,
        Chest     = 1 << 1,
        Abdomen   = 1 << 2,
        Hips      = 1 << 3,
        UpperArmL = 1 << 4,
        UpperArmR = 1 << 5,
        ForearmL  = 1 << 6,
        ForearmR  = 1 << 7,
        HandL     = 1 << 8,
        HandR     = 1 << 9,
        ThighL    = 1 << 10,
        ThighR    = 1 << 11,
        CalfL     = 1 << 12,
        CalfR     = 1 << 13,
        FootL     = 1 << 14,
        FootR     = 1 << 15,
    }
}
