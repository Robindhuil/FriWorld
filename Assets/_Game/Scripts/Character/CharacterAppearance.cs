using System;

namespace FriWorld.Character
{
    /// <summary>
    /// One character's whole look, as indices. Small enough to be a save file field and a spawn
    /// argument at once: this is what gets stored for the player and what a seed produces for an
    /// NPC.
    /// </summary>
    [Serializable]
    public struct CharacterAppearance
    {
        /// <summary>No legal preset for that slot class. Apply strips the class entirely.</summary>
        public const byte None = byte.MaxValue;

        public Gender gender;

        /// <summary>Index within the slot class, parallel to CharacterCatalog.slotClasses.</summary>
        public byte[] preset;

        /// <summary>Index within the colour slot's own palette, one entry per colour slot. A slot
        /// is a class and a key — torso 1, torso 2 — and each rolls independently.</summary>
        public byte[] colorway;

        /// <summary>Stature across the body's [min, max] band. Decode with BodySize.Metres.</summary>
        public byte height;
    }
}
