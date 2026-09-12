using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > Sync Shades From Materials.
    ///
    /// Reads the colour back off the generated base materials, writes it into
    /// CharacterColorways.json, and re-derives the shades from it.
    ///
    /// The register says which colours exist; the material is where a colour actually gets
    /// picked, because that is where the colour picker is. Nudging a skin tone in the inspector
    /// therefore leaves the two disagreeing, and the shade — the darker material the ear samples
    /// — still belongs to the colour that was there before. This closes that gap in one step
    /// instead of copying hex codes by hand.
    /// </summary>
    public static class ColorwaySync
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static void Run()
        {
            var classes = CharacterRegistries.LoadClasses();
            var registry = CharacterRegistries.LoadColorways();

            var byName = new Dictionary<string, ColorClassDef>();
            foreach (var def in classes.colorClasses) byName[def.name] = def;

            // The register is hand-written, one colorway per line, so the hex is patched in place.
            // Re-serialising it would reformat the whole file for the sake of six characters.
            string text = File.ReadAllText(CharacterRegistries.ColorwaysPath);
            var changed = new List<string>();

            foreach (var way in registry.colorways)
            {
                if (!byName.TryGetValue(way.colorClass, out var def)) continue;

                // A follower carries no materials under its own colorway ids — its palette is the
                // source's, and the source is what gets read here.
                if (!string.IsNullOrEmpty(def.follows)) continue;

                string path = $"{ShadeMaterialGenerator.OutputRoot}/{way.colorClass}/"
                              + $"mt_char_{way.colorClass}_{way.id}_{way.slot}.mat";

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.HasProperty(BaseColor)) continue;

                string onMaterial = "#" + ColorUtility.ToHtmlStringRGB(material.GetColor(BaseColor));
                if (string.Equals(onMaterial, way.color, StringComparison.OrdinalIgnoreCase)) continue;

                string pattern = "(\"colorClass\"[^}]*?\"" + Regex.Escape(way.colorClass)
                                 + "\"[^}]*?\"id\"[^}]*?\"" + Regex.Escape(way.id)
                                 + "\"[^}]*?\"color\"[^}]*?\")#[0-9A-Fa-f]{6}(\")";

                string patched = Regex.Replace(text, pattern, "$1" + onMaterial + "$2");
                if (patched == text)
                {
                    Debug.LogWarning($"Sync Shades: '{way.colorClass}/{way.id}' not found in "
                                     + $"{CharacterRegistries.ColorwaysPath}; its colour stays {way.color}");
                    continue;
                }

                changed.Add($"{way.colorClass} {way.slot}/{way.id}: {way.color} -> {onMaterial}");
                text = patched;
            }

            if (changed.Count > 0)
            {
                File.WriteAllText(CharacterRegistries.ColorwaysPath, text);
                AssetDatabase.Refresh();
            }

            // Run either way: a shade can be stale even when the register already agreed, which is
            // what happens when the hex was edited by hand rather than on the material.
            ShadeMaterialGenerator.Run();

            var report = new StringBuilder();
            report.Append("Sync Shades: ").Append(changed.Count).Append(" colorways taken from their material");
            foreach (string line in changed) report.Append("\n  ").Append(line);
            Debug.Log(report.ToString());
        }
    }
}
