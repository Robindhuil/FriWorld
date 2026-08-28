using System.Collections.Generic;

namespace FriWorld.Character
{
    /// <summary>
    /// Rolls a legal look from a seed.
    ///
    /// Deterministic on purpose: an NPC whose seed comes from its identity looks the same after a
    /// respawn without anything being stored. System.Random rather than UnityEngine.Random so a
    /// roll cannot be disturbed by, or disturb, whatever else is drawing random numbers.
    /// </summary>
    public static class CharacterRandomizer
    {
        public static CharacterAppearance Roll(int seed, CharacterCatalog catalog, Gender gender)
        {
            var rng = new System.Random(seed);

            var look = new CharacterAppearance
            {
                gender = gender,
                preset = new byte[catalog.slotClasses.Length],
                colorway = new byte[catalog.colorClasses.Length],
            };

            int takenTags = 0;
            int forbiddenTags = 0;

            var candidates = new List<int>();
            var weights = new List<int>();

            for (int slot = 0; slot < catalog.slotClasses.Length; slot++)
            {
                candidates.Clear();
                weights.Clear();

                int count = catalog.PresetCount(gender, slot);
                for (int i = 0; i < count; i++)
                {
                    var candidate = catalog.Preset(gender, slot, i);
                    if (!PresetRules.IsAllowed(candidate, gender, takenTags, forbiddenTags)) continue;

                    candidates.Add(i);
                    weights.Add(candidate.weight);
                }

                if (candidates.Count == 0)
                {
                    // Falling back to index 0 would quietly break whichever rule excluded it.
                    // Leaving the class empty is visible, and Report already warns about a class
                    // that can never be filled.
                    look.preset[slot] = CharacterAppearance.None;
                    continue;
                }

                int picked = candidates[PresetRules.PickWeighted(weights, rng.NextDouble())];
                look.preset[slot] = (byte)picked;

                var chosen = catalog.Preset(gender, slot, picked);
                takenTags |= chosen.tagMask;
                forbiddenTags |= chosen.conflictMask;
            }

            for (int colorClass = 0; colorClass < catalog.colorClasses.Length; colorClass++)
            {
                int count = catalog.ColorwayCount(colorClass);
                look.colorway[colorClass] = count == 0
                    ? CharacterAppearance.None
                    : (byte)rng.Next(count);
            }

            // Drawn last so that adding stature to the system did not shift every existing seed's
            // clothing. A body with no declared size rolls to the middle of nothing and scales 1.
            var size = catalog.Size(gender);
            look.height = size != null ? size.Roll(rng) : (byte)0;

            return look;
        }
    }
}
