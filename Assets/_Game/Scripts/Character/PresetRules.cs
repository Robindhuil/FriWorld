using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    /// <summary>
    /// Whether a preset may be worn given what has already been chosen.
    ///
    /// Conflicts are symmetric and both directions are checked on every candidate, so the order
    /// the slot classes are visited in cannot change which combinations are legal. That is what
    /// lets the randomizer be a single pass with no backtracking.
    /// </summary>
    public static class PresetRules
    {
        public static bool GenderAllows(GenderGate gate, Gender gender)
        {
            switch (gate)
            {
                case GenderGate.Any:    return true;
                case GenderGate.Male:   return gender == Gender.Male;
                case GenderGate.Female: return gender == Gender.Female;
                default:                return false;
            }
        }

        /// <param name="takenTags">OR of the tags provided by everything chosen so far.</param>
        /// <param name="forbiddenTags">OR of the tags forbidden by everything chosen so far.</param>
        public static bool IsAllowed(PresetEntry preset, Gender gender, int takenTags, int forbiddenTags)
        {
            if (preset == null) return false;
            if (!GenderAllows(preset.gender, gender)) return false;
            if ((preset.tagMask & forbiddenTags) != 0) return false;
            if ((preset.conflictMask & takenTags) != 0) return false;
            return true;
        }

        /// <param name="roll">Uniform in [0, 1).</param>
        /// <returns>Index into <paramref name="weights"/>, or -1 when there is nothing to pick.</returns>
        public static int PickWeighted(IReadOnlyList<int> weights, double roll)
        {
            if (weights == null || weights.Count == 0) return -1;

            long total = 0;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0, weights[i]);

            // Every candidate weighted zero still has to produce something rather than nothing;
            // Report already flags a weight below 1.
            if (total <= 0) return 0;

            double target = roll * total;
            long running = 0;

            for (int i = 0; i < weights.Count; i++)
            {
                running += Mathf.Max(0, weights[i]);
                if (target < running) return i;
            }

            return weights.Count - 1;
        }
    }
}
