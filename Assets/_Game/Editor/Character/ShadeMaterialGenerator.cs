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
                foreach (var def in classes.colorClasses)
                {
                    // A follower has no colorways of its own; it writes one material per colorway
                    // of the class it follows, under that colorway's id, so the two line up index
                    // for index when the catalog is baked and the roll is copied across.
                    var source = def;

                    if (!string.IsNullOrEmpty(def.follows))
                    {
                        if (!classByName.TryGetValue(def.follows, out source))
                        {
                            problems.Add($"colour class '{def.name}' follows '{def.follows}', "
                                         + "which is not a colour class");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(source.follows))
                        {
                            problems.Add($"colour class '{def.name}' follows '{source.name}', "
                                         + $"which follows '{source.follows}' — one level only");
                            continue;
                        }
                    }

                    foreach (var way in colorways.colorways)
                    {
                        if (way.colorClass != source.name) continue;

                        if (way.slot < 1 || way.slot > def.mainColors)
                        {
                            if (source == def)
                                problems.Add($"colorway '{way.colorClass} {way.slot}/{way.id}' is for "
                                             + $"slot {way.slot}, the class declares {def.mainColors}");
                            continue;
                        }

                        if (!ColorUtility.TryParseHtmlString(way.color, out var color))
                        {
                            problems.Add($"colorway '{way.colorClass} {way.slot}/{way.id}' has an "
                                         + $"unreadable colour '{way.color}'");
                            continue;
                        }

                        Directory.CreateDirectory(Path.Combine(OutputRoot, def.name));

                        if (source != def)
                            color = ShadeColor.Derive(color, def.followValue, def.followSaturation);

                        if (Write(def, way, 0, color, problems)) written++;

                        if (def.shadeValue.HasValue && def.shadeSaturation.HasValue)
                        {
                            var shade = ShadeColor.Derive(color, def.shadeValue.Value,
                                                          def.shadeSaturation.Value);
                            if (Write(def, way, 1, shade, problems)) written++;
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
        static bool Write(ColorClassDef def, ColorwayDef way, int shadeLevel, Color color,
                          List<string> problems)
        {
            string key = shadeLevel == 0 ? way.slot.ToString() : $"{way.slot}{shadeLevel}";

            string path = $"{OutputRoot}/{def.name}/mt_char_{def.name}_{way.id}_{key}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            // A shade is the same material a stop darker, so when the base colour of this very
            // colorway is already an asset, that asset is the better source than the template:
            // whatever was tuned on it by hand carries into the shade instead of being reverted.
            Material template = null;
            if (shadeLevel > 0)
                template = AssetDatabase.LoadAssetAtPath<Material>(
                    $"{OutputRoot}/{def.name}/mt_char_{def.name}_{way.id}_{way.slot}.mat");

            // Otherwise a template authored for the shade slot itself, then the base slot, which
            // is the common case — the shade usually only differs in colour.
            template = template
                       ?? LoadTemplate($"char_{def.name}_{key}")
                       ?? LoadTemplate($"char_{def.name}_{way.slot}");

            if (template == null)
            {
                problems.Add($"no source template for char_{def.name}_{key} in {SourceDir}");
                return false;
            }

            if (existing == null)
            {
                var created = new Material(template) { name = Path.GetFileNameWithoutExtension(path) };
                Tint(created, color);
                AssetDatabase.CreateAsset(created, path);
            }
            else
            {
                // Keep the asset — its GUID is already in the baked catalog and in anything else
                // that happens to reference it. Only the colour is re-derived: copying the whole
                // template over it would undo anything an artist changed on the material itself,
                // which is exactly what this tool promises not to do.
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
