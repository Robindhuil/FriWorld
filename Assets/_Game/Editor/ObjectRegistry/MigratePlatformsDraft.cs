using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// One-off: fills RoomPlatforms.json from the hand-written prefix-level draft. Every area
    /// takes the value of its name with the instance number removed, so one draft row can decide
    /// several areas — which is the point, since the draft could not express them separately.
    ///
    /// Delete this file and the draft once it has run. It is committed only so the conversion is
    /// reproducible and reviewable in the diff.
    /// </summary>
    public static class MigratePlatformsDraft
    {
        const string DraftPath = "Assets/_Game/Editor/Platforms.json";

        [MenuItem("Tools/Object Registry/Migrate Platforms Draft")]
        static void Migrate()
        {
            if (!File.Exists(DraftPath))
            {
                Debug.LogError("[MigrateDraft] " + DraftPath + " not found — already migrated?");
                return;
            }

            // The draft is not valid JSON: it was typed by hand to capture intent, in the shape
            // "name", "platform": "value", and at least one row is missing its comma. One regex
            // over a known one-time input beats a tolerant parser nothing else will ever use.
            var draft = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(File.ReadAllText(DraftPath),
                         "\"([^\"]+)\"\\s*,?\\s*\"platform\"\\s*:\\s*\"([^\"]+)\""))
                draft[m.Groups[1].Value] = m.Groups[2].Value;

            var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
            var platforms = RoomPlatforms.Load(ObjectRegistryMenu.RoomPlatformsPath);

            List<string> areas;
            var contents = RoomGateScope.Open();
            try
            {
                areas = RoomGateScope.AreaNames(contents, prefixes);
            }
            finally
            {
                RoomGateScope.Close(contents);
            }

            platforms.Reconcile(areas);

            int filled = 0;
            var unmatched = new List<string>();
            foreach (var area in areas)
            {
                var entry = platforms.Find(area);
                if (entry == null || entry.IsDecided) continue;

                string key = StripInstanceNumber(area);
                if (draft.TryGetValue(key, out var value) && RoomPlatforms.IsValidPlatform(value))
                {
                    entry.platform = value;
                    filled++;
                }
                else unmatched.Add(area);
            }

            platforms.Save(ObjectRegistryMenu.RoomPlatformsPath);
            AssetDatabase.Refresh();

            var sb = new StringBuilder();
            sb.AppendLine("[MigrateDraft] draft rows " + draft.Count + " → decided " + filled
                        + " of " + platforms.rooms.Count + " areas");
            if (unmatched.Count > 0)
            {
                sb.AppendLine("  no draft value, left undecided (" + unmatched.Count + "):");
                foreach (var a in unmatched) sb.AppendLine("    " + a);
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Removes a trailing "_&lt;digits&gt;". Local to the migration on purpose: the draft is
        /// the only thing left in the project keyed without the instance number, and this helper
        /// dies with it.
        /// </summary>
        static string StripInstanceNumber(string s)
        {
            int underscore = s.LastIndexOf('_');
            if (underscore <= 0) return s;

            string tail = s.Substring(underscore + 1);
            if (tail.Length == 0) return s;
            foreach (char c in tail) if (!char.IsDigit(c)) return s;
            return s.Substring(0, underscore);
        }
    }
}
