using System;
using System.Collections.Generic;
using System.Text;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// The keyword arrays as they exist in GenerateColliders and GenerateLayersAndStatic today,
    /// kept only to seed the registry and to diff old behaviour against new. Deleted once the
    /// migration is verified — see Task 11 of the implementation plan.
    /// </summary>
    public static class LegacyKeywordRules
    {
        public static readonly string[] ColliderMesh = {
            "door_frame","wall","outer_wall","big_window","ceiling","fence","roof","sofa","table",
            "parapet","pillar","curb","tree_pot","rock","tree_bot","sun_block","nav","barrier" };

        public static readonly string[] ColliderBox = {
            "window","glass","door","thick_door","foor","door_slide","vent","box","couch","counter",
            "counter_bar","chair","desk","board","calcetto","trash_bin","handicap_machine",
            "bycicle_shelter_rack","plant_tree_pot","ventilator","billboard","drainage","preform",
            "ramp","trash_container","gazebo_bench" };

        public static readonly string[] ColliderIgnore = {
            "lamp","doorstep","room_sign","thick_door_headlight","sign","poster","e_plug",
            "e_plug_cover","hydrant","construction","bell_thingy","radiator_pipe","drain","support",
            "radiator","e_box","bush","shrub","plant_pot","tree_top" };

        public static readonly string[] LayerInteractable = { "door", "door_slide" };

        public static readonly string[] LayerObstacle = {
            "door_frame","thick_door","foor","wall","outer_wall","big_window","fence","sofa","window",
            "pillar","couch","counter","desk","trash_bin","barrier","pot_tree","rock","tree_bot",
            "plant_tree_pot","ventilator","billboard","preform","ramp","trash_container","sun_block" };

        public static readonly string[] LayerNoObstacle = {
            "lamp","doorstep","thick_door_headlight","room_sign","sign","poster","e_plug",
            "e_plug_cover","hydrant","construction","bell_thingy","radiator_pipe","drain","support",
            "ceiling","roof","table","parapet","radiator","e_box","vent","box","counter_bar","chair",
            "board","bycicle_shelter_rack","gazebo_bench","glass","handicap_machine","calcetto",
            "plant_pot","bush","shrub","tree_top" };

        public static readonly string[] LayerNav = { "nav", "drainage", "curb" };

        public static readonly string[] OccluderKeywords = {
            "wall","outer_wall","ceiling","roof","nav","door_frame","foor","pillar" };

        /// <summary>Longest token-sequence match, mirroring the old resolver.</summary>
        public static string BestMatch(string objectName, string[] keywords)
        {
            var nameTokens = Tokenize(objectName);
            string best = null;
            int bestLen = 0;
            foreach (var kw in keywords)
            {
                var kwTokens = Tokenize(kw);
                if (kwTokens.Count == 0 || kwTokens.Count <= bestLen) continue;
                if (ContainsSequence(nameTokens, kwTokens)) { best = kw; bestLen = kwTokens.Count; }
            }
            return best;
        }

        public static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return tokens;
            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        public static bool ContainsSequence(List<string> haystack, List<string> needle)
        {
            if (needle.Count > haystack.Count) return false;
            for (int i = 0; i <= haystack.Count - needle.Count; i++)
            {
                bool all = true;
                for (int j = 0; j < needle.Count; j++)
                    if (!string.Equals(haystack[i + j], needle[j], StringComparison.OrdinalIgnoreCase))
                    { all = false; break; }
                if (all) return true;
            }
            return false;
        }
    }
}
