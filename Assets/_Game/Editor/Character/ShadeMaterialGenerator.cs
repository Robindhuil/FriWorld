using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 2 — Generate Shades.
    ///
    /// Turns every colorway into real .mat assets, one per slot key. Doing it here rather than at
    /// runtime is what keeps the swap free: applying a look is then a reference assignment, the
    /// materials stay shared across every NPC wearing that colorway, and the SRP Batcher keeps
    /// batching them.
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

                    Directory.CreateDirectory(Path.Combine(OutputRoot, way.colorClass));

                    for (int baseKey = 1; baseKey <= def.mainColors; baseKey++)
                    {
                        if (way.colors == null || way.colors.Count < baseKey)
                        {
                            problems.Add($"colorway '{way.colorClass}/{way.id}' has no colour {baseKey}");
                            continue;
                        }

                        if (!ColorUtility.TryParseHtmlString(way.colors[baseKey - 1], out var color))
                        {
                            problems.Add($"colorway '{way.colorClass}/{way.id}' colour {baseKey} "
                                         + $"'{way.colors[baseKey - 1]}' is unreadable");
                            continue;
                        }

                        if (Write(way, def, baseKey, 0, color, problems)) written++;

                        if (def.shadeValue.HasValue && def.shadeSaturation.HasValue)
                        {
                            var shade = ShadeColor.Derive(color, def.shadeValue.Value,
                                                          def.shadeSaturation.Value);
                            if (Write(way, def, baseKey, 1, shade, problems)) written++;
                        }
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
        static bool Write(ColorwayDef way, ColorClassDef def, int baseKey, int shadeLevel,
                          Color color, List<string> problems)
        {
            string key = shadeLevel == 0 ? baseKey.ToString() : $"{baseKey}{shadeLevel}";

            // Prefer a template authored for the shade slot itself; fall back to the base slot,
            // which is the common case — the shade usually only differs in colour.
            var template = LoadTemplate($"char_{def.name}_{key}")
                           ?? LoadTemplate($"char_{def.name}_{baseKey}");

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
                created.SetColor(BaseColor, color);
                AssetDatabase.CreateAsset(created, path);
            }
            else
            {
                // Keep the asset — its GUID is already in the baked catalog and in anything else
                // that happens to reference it. Only the colour is re-derived.
                existing.shader = template.shader;
                existing.CopyPropertiesFromMaterial(template);
                existing.SetColor(BaseColor, color);
                EditorUtility.SetDirty(existing);
            }

            return true;
        }

        static Material LoadTemplate(string materialName) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{SourceDir}/{materialName}.mat");
    }
}
