using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 2 — Generate Shades.
    ///
    /// Turns every colorway into real .mat assets: one for the colour, one for its derived shade
    /// where the class declares one. Doing it here rather than at runtime is what keeps the swap
    /// free: applying a look is then a reference assignment, the materials stay shared across
    /// every NPC wearing that colour, and the SRP Batcher keeps batching them.
    ///
    /// The look of a material — shader, normal map, smoothness — comes from the source template
    /// extracted from the model. Only _BaseColor is overwritten, so re-running this never undoes
    /// art work.
    /// </summary>
    public static class ShadeMaterialGenerator
    {
        public const string SourceDir = "Assets/_Game/Art/Materials/Character/_source";
        public const string OutputRoot = "Assets/_Game/Art/Materials/Character";

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // URP Lit renders from _BaseColor and keeps _Color only as the legacy alias. Leaving the
        // alias on the template's colour makes the inspector disagree with what is on screen,
        // which is a confusing half hour for whoever opens the material next.
        static readonly int LegacyColor = Shader.PropertyToID("_Color");

        public static void Run()
        {
            var classes = CharacterRegistries.LoadClasses();
            var colorways = CharacterRegistries.LoadColorways();

            var classByName = new Dictionary<string, ColorClassDef>();
            foreach (var def in classes.colorClasses) classByName[def.name] = def;

            int written = 0;
            var problems = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var way in colorways.colorways)
                {
                    if (!classByName.TryGetValue(way.colorClass, out var def))
                    {
                        problems.Add($"colorway '{way.id}' names unknown colour class '{way.colorClass}'");
                        continue;
                    }

                    if (way.slot < 1 || way.slot > def.mainColors)
                    {
                        problems.Add($"colorway '{way.colorClass} {way.slot}/{way.id}' is for slot "
                                     + $"{way.slot}, the class declares {def.mainColors}");
                        continue;
                    }

                    if (!ColorUtility.TryParseHtmlString(way.color, out var color))
                    {
                        problems.Add($"colorway '{way.colorClass} {way.slot}/{way.id}' has an "
                                     + $"unreadable colour '{way.color}'");
                        continue;
                    }

                    Directory.CreateDirectory(Path.Combine(OutputRoot, way.colorClass));

                    if (Write(way, def, 0, color, problems)) written++;

                    if (def.shadeValue.HasValue && def.shadeSaturation.HasValue)
                    {
                        var shade = ShadeColor.Derive(color, def.shadeValue.Value,
                                                      def.shadeSaturation.Value);
                        if (Write(way, def, 1, shade, problems)) written++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string summary = $"Generate Shades: {written} materials written, {problems.Count} problems.";
            if (problems.Count > 0) Debug.LogError(summary + "\n" + string.Join("\n", problems));
            else Debug.Log(summary);
        }

        /// <summary>Creates or updates one material. Returns false when the template is missing.</summary>
        static bool Write(ColorwayDef way, ColorClassDef def, int shadeLevel, Color color,
                          List<string> problems)
        {
            string key = shadeLevel == 0 ? way.slot.ToString() : $"{way.slot}{shadeLevel}";

            // Prefer a template authored for the shade slot itself; fall back to the base slot,
            // which is the common case — the shade usually only differs in colour.
            var template = LoadTemplate($"char_{def.name}_{key}")
                           ?? LoadTemplate($"char_{def.name}_{way.slot}");

            if (template == null)
            {
                problems.Add($"no source template for char_{def.name}_{key} in {SourceDir}");
                return false;
            }

            string path = $"{OutputRoot}/{def.name}/mt_char_{def.name}_{way.id}_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                var created = new Material(template) { name = Path.GetFileNameWithoutExtension(path) };
                Tint(created, color);
                AssetDatabase.CreateAsset(created, path);
            }
            else
            {
                // Keep the asset — its GUID is already in the baked catalog and in anything else
                // that happens to reference it. Only the colour is re-derived.
                existing.shader = template.shader;
                existing.CopyPropertiesFromMaterial(template);
                Tint(existing, color);
                EditorUtility.SetDirty(existing);
            }

            return true;
        }

        static void Tint(Material material, Color color)
        {
            material.SetColor(BaseColor, color);
            if (material.HasProperty(LegacyColor)) material.SetColor(LegacyColor, color);
        }

        static Material LoadTemplate(string materialName) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{SourceDir}/{materialName}.mat");
    }
}
