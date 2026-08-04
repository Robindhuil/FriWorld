using System;
using System.Collections.Generic;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// Reduces a scene object's name to the type key used to look up its behaviour.
    ///
    /// Four steps: strip the longest matching prefix, strip the room's own leading instance
    /// number, strip the trailing instance number, and whatever remains is the key. Pure C#
    /// with no Unity dependencies so it can be unit-tested directly.
    /// </summary>
    public static class ObjectTypeKey
    {
        /// <summary>
        /// Derives the type key. <paramref name="prefixes"/> must be the approved list from
        /// ObjectPrefixes.json — never a set harvested on the fly, or a prefix that happens to
        /// equal a type word ("wall") would eat part of a multi-word type ("wall_edge").
        /// </summary>
        public static string Derive(string objectName, IReadOnlyList<string> prefixes)
        {
            if (string.IsNullOrEmpty(objectName))
                return string.Empty;

            // Longest first, so "ra100_corridor" wins over "ra100".
            var ordered = new List<string>(prefixes ?? new List<string>());
            ordered.Sort((a, b) => (b ?? "").Length.CompareTo((a ?? "").Length));

            foreach (var prefix in ordered)
            {
                if (string.IsNullOrEmpty(prefix)) continue;
                if (objectName.Length <= prefix.Length + 1) continue;
                if (!objectName.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase)) continue;

                string candidate = objectName.Substring(prefix.Length + 1);
                candidate = StripLeadingInt(candidate);
                candidate = StripOverrideTokens(candidate);
                candidate = StripTrailingInt(candidate);

                // The strip has to leave a word behind. Otherwise "lamp_2" against a prefix
                // "lamp" collapses to "2", and that one key swallows every lamp in the project.
                if (!HasLetter(candidate)) continue;

                return candidate;
            }

            // No prefix applied: only the trailing instance number comes off. A leading number
            // is not stripped here because there is no prefix it could have belonged to.
            return StripTrailingInt(StripOverrideTokens(objectName));
        }

        /// <summary>
        /// Removes the UNO / UYO markers. They are per-object exceptions handled separately by
        /// GenerateLayersAndStatic, and they say nothing about what the object *is* — leaving
        /// them in would split one type into "wall", "wall_3_UNO" and "outer_wall_13_UNO".
        /// Case-sensitive, matching how the override itself is detected.
        /// </summary>
        static string StripOverrideTokens(string s)
        {
            if (s.IndexOf("UNO", StringComparison.Ordinal) < 0 &&
                s.IndexOf("UYO", StringComparison.Ordinal) < 0)
                return s;

            var parts = s.Split('_');
            var kept = new List<string>(parts.Length);
            foreach (var p in parts)
                if (p != "UNO" && p != "UYO")
                    kept.Add(p);

            return kept.Count == 0 ? s : string.Join("_", kept.ToArray());
        }

        static string StripLeadingInt(string s)
        {
            int underscore = s.IndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(0, underscore)) ? s.Substring(underscore + 1) : s;
        }

        static string StripTrailingInt(string s)
        {
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;
            return AllDigits(s.Substring(underscore + 1)) ? s.Substring(0, underscore) : s;
        }

        static bool AllDigits(string s)
        {
            if (s.Length == 0) return false;
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        static bool HasLetter(string s)
        {
            foreach (char c in s) if (char.IsLetter(c)) return true;
            return false;
        }
    }
}
