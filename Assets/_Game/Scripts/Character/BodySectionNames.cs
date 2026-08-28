using System;
using System.Collections.Generic;

namespace FriWorld.Character
{
    /// <summary>
    /// Maps between a body section and the GameObject that carries it in the base prefab.
    ///
    /// The object is called &lt;gender&gt;_body_&lt;key&gt; — male_body_upperarm_L — and the key
    /// is whatever follows the final "_body_". The prefix exists so both bodies can live in one
    /// blend file without a name clash; the key must not carry it, so a hides mask written once
    /// in CharacterPresets.json applies to either body. Same split as ObjectTypeKey: the name is
    /// what the author sees, the key is what the register looks up.
    ///
    /// Matching is ordinal and case-sensitive on purpose. Blender writes the side as _L and _R;
    /// accepting _l as well would let two objects claim the same section and the loser would
    /// disappear without a word.
    /// </summary>
    public static class BodySectionNames
    {
        const string Marker = "_body_";

        static readonly (string key, BodySection section)[] Table =
        {
            ("neck",       BodySection.Neck),
            ("chest",      BodySection.Chest),
            ("abdomen",    BodySection.Abdomen),
            ("hips",       BodySection.Hips),
            ("upperarm_L", BodySection.UpperArmL),
            ("upperarm_R", BodySection.UpperArmR),
            ("forearm_L",  BodySection.ForearmL),
            ("forearm_R",  BodySection.ForearmR),
            ("hand_L",     BodySection.HandL),
            ("hand_R",     BodySection.HandR),
            ("thigh_L",    BodySection.ThighL),
            ("thigh_R",    BodySection.ThighR),
            ("calf_L",     BodySection.CalfL),
            ("calf_R",     BodySection.CalfR),
            ("foot_L",     BodySection.FootL),
            ("foot_R",     BodySection.FootR),
        };

        public static IReadOnlyList<(string key, BodySection section)> All => Table;

        /// <summary>Exact key lookup — "upperarm_L". This is what CharacterPresets.json writes
        /// in its hides array.</summary>
        public static bool TryParseKey(string key, out BodySection section)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var entry in Table)
                {
                    if (string.Equals(entry.key, key, StringComparison.Ordinal))
                    {
                        section = entry.section;
                        return true;
                    }
                }
            }

            section = BodySection.None;
            return false;
        }

        /// <summary>Key lookup from a GameObject name — "male_body_upperarm_L". A name with no
        /// "_body_" in it is taken as a bare key, so a body that drops the prefix later still
        /// works.</summary>
        public static bool TryParseObject(string objectName, out BodySection section)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                section = BodySection.None;
                return false;
            }

            // Last occurrence, not first: a container could itself be called "body".
            int marker = objectName.LastIndexOf(Marker, StringComparison.Ordinal);
            string key = marker < 0
                ? objectName
                : objectName.Substring(marker + Marker.Length);

            return TryParseKey(key, out section);
        }

        /// <summary>The key, without any body prefix. For reports.</summary>
        public static string KeyOf(BodySection section)
        {
            foreach (var entry in Table)
                if (entry.section == section)
                    return entry.key;
            return null;
        }
    }
}
