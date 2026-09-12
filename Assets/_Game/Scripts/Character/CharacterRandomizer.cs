using System.Collections.Generic;
using UnityEngine;

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
                colorway = new byte[catalog.ColorSlotCount],
                shape = new byte[catalog.ShapeAxisCount],
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

            // Per colour slot, not per colour class: the secondary colour of a garment draws from
            // its own palette and is free of the main one.
            for (int colorSlot = 0; colorSlot < catalog.ColorSlotCount; colorSlot++)
            {
                if (catalog.FollowedSlot(colorSlot) >= 0) continue;   // taken below, not drawn

                int count = catalog.ColorwayCount(colorSlot);
                look.colorway[colorSlot] = count == 0
                    ? CharacterAppearance.None
                    : (byte)rng.Next(count);
            }

            // A follower takes the roll of the slot it follows rather than drawing its own, which
            // is what keeps brows on the same head as the hair. Its palette is the source's, index
            // for index, so the same number means the same colour family. Drawing nothing here
            // also means adding a follower does not shift any existing seed.
            //
            // A drift of one step is what keeps beards from looking printed: a beard genuinely
            // runs a shade off the hair, and because the palette is ordered from dark to light,
            // one index either way is exactly that — never three colours on one head.
            for (int colorSlot = 0; colorSlot < catalog.ColorSlotCount; colorSlot++)
            {
                int source = catalog.FollowedSlot(colorSlot);
                if (source < 0) continue;

                byte picked = look.colorway[source];
                int count = catalog.ColorwayCount(colorSlot);
                if (count == 0 || picked == CharacterAppearance.None)
                {
                    look.colorway[colorSlot] = CharacterAppearance.None;
                    continue;
                }

                int index = Mathf.Min(picked, count - 1);
                int drift = catalog.SlotDrift(colorSlot);
                float chance = catalog.SlotDriftChance(colorSlot);

                // The draws happen only for a slot that declares a drift, so turning one on does
                // not reshuffle the colours of every seed that had none.
                if (drift > 0 && chance > 0f && rng.NextDouble() < chance)
                {
                    int step = 1 + rng.Next(drift);
                    if (rng.NextDouble() < 0.5) step = -step;
                    index = Mathf.Clamp(index + step, 0, count - 1);
                }

                look.colorway[colorSlot] = (byte)index;
            }

            // Drawn last so that adding stature to the system did not shift every existing seed's
            // clothing. A body with no declared size rolls to the middle of nothing and scales 1.
            var size = catalog.Size(gender);
            look.height = size != null ? size.Roll(rng) : (byte)0;

            // Appended after height for the same reason height came after clothing: a seed that
            // already had a face keeps the face it had when the next axis is added.
            for (int axis = 0; axis < catalog.ShapeAxisCount; axis++)
                look.shape[axis] = catalog.shapeAxes[axis].Roll(rng);

            return look;
        }
    }
}
