using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Character > 3 — Bake Catalog.
    ///
    /// Compiles the three registers into CharacterCatalog.asset: names become indices, colorway
    /// ids become Material references, and every renderer's material names become a slot map.
    /// After this the game reads one asset and parses nothing.
    ///
    /// It refuses to write while Report still finds an error. A catalog baked from a broken
    /// register is worse than no catalog, because it looks like it worked.
    /// </summary>
    public static class CharacterCatalogBaker
    {
        public const string CatalogPath = "Assets/Resources/CharacterCatalog.asset";
        const string MaterialRoot = ShadeMaterialGenerator.OutputRoot;

        public static void Run()
        {
            var classes = CharacterRegistries.LoadClasses();
            var colorwayRegistry = CharacterRegistries.LoadColorways();
            var presetRegistry = CharacterRegistries.LoadPresets();

            var missing = new List<string>();
            var bodies = CharacterScan.ReadBoth(missing);

            var issues = CharacterValidation.Check(classes, colorwayRegistry, presetRegistry, bodies);

            int errors = missing.Count;
            foreach (var issue in issues)
                if (issue.severity == Severity.Error) errors++;

            if (errors > 0)
            {
                Debug.LogError($"Bake Catalog refused: Report finds {errors} errors. "
                               + "Run Character > 1 — Report and fix them first.");
                return;
            }

            var catalog = LoadOrCreate();

            catalog.slotClasses = classes.slotClasses.ToArray();

            var colorClassNames = new List<string>();
            foreach (var def in classes.colorClasses) colorClassNames.Add(def.name);
            catalog.colorClasses = colorClassNames.ToArray();

            catalog.tags = CollectTags(presetRegistry);

            BakeColorways(catalog, classes, colorwayRegistry);

            foreach (var body in bodies)
            {
                var bundle = body.gender == Gender.Male ? catalog.male : catalog.female;
                bundle.basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(body.prefabPath);
                BakePresets(catalog, bundle, presetRegistry, body.gender);
                BakeSlotMaps(catalog, bundle, body);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"Bake Catalog: {catalog.slotClasses.Length} slot classes, "
                      + $"{catalog.colorClasses.Length} colour classes, "
                      + $"{catalog.colorways.Length} colorways, "
                      + $"{catalog.tags.Length} tags, "
                      + $"male {catalog.male.presets.Length} presets / {catalog.male.slotMaps.Length} slot maps, "
                      + $"female {catalog.female.presets.Length} presets / {catalog.female.slotMaps.Length} slot maps.");
        }

        static CharacterCatalog LoadOrCreate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        static string[] CollectTags(PresetRegistry presets)
        {
            var tags = new List<string>();
            foreach (var preset in presets.presets)
            {
                if (preset.tags == null) continue;
                foreach (string tag in preset.tags)
                    if (!tags.Contains(tag)) tags.Add(tag);
            }

            // Stable order, so a rebake produces the same masks.
            tags.Sort(StringComparer.Ordinal);
            return tags.ToArray();
        }

        static int MaskOf(IEnumerable<string> tags, string[] table)
        {
            int mask = 0;
            if (tags == null) return mask;

            foreach (string tag in tags)
            {
                int index = Array.IndexOf(table, tag);
                if (index >= 0) mask |= 1 << index;
            }

            return mask;
        }

        static void BakeColorways(CharacterCatalog catalog, ClassRegistry classes,
                                  ColorwayRegistry registry)
        {
            var entries = new List<ColorwayEntry>();
            var start = new int[catalog.colorClasses.Length + 1];

            for (int c = 0; c < catalog.colorClasses.Length; c++)
            {
                start[c] = entries.Count;
                string className = catalog.colorClasses[c];
                var def = classes.colorClasses.Find(d => d.name == className);

                foreach (var way in registry.colorways)
                {
                    if (way.colorClass != className) continue;

                    var materials = new Material[def.mainColors * 2];
                    for (int baseKey = 1; baseKey <= def.mainColors; baseKey++)
                    {
                        materials[(baseKey - 1) * 2] = LoadMaterial(className, way.id, baseKey.ToString());
                        if (def.shadeValue.HasValue)
                            materials[(baseKey - 1) * 2 + 1] =
                                LoadMaterial(className, way.id, $"{baseKey}1");
                    }

                    entries.Add(new ColorwayEntry
                    {
                        colorClass = c,
                        id = way.id,
                        displayName = way.displayName,
                        materials = materials,
                    });
                }
            }

            start[catalog.colorClasses.Length] = entries.Count;
            catalog.colorways = entries.ToArray();
            catalog.colorwayStart = start;
        }

        static Material LoadMaterial(string colorClass, string colorwayId, string key) =>
            AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialRoot}/{colorClass}/mt_char_{colorClass}_{colorwayId}_{key}.mat");

        static void BakePresets(CharacterCatalog catalog, GenderBundle bundle,
                                PresetRegistry registry, Gender gender)
        {
            var entries = new List<PresetEntry>();
            var start = new int[catalog.slotClasses.Length + 1];

            for (int s = 0; s < catalog.slotClasses.Length; s++)
            {
                start[s] = entries.Count;
                string slotClass = catalog.slotClasses[s];

                foreach (var preset in registry.presets)
                {
                    if (preset.slotClass != slotClass) continue;

                    var gate = CharacterValidation.ParseGender(preset.gender);
                    if (gate == null || !PresetRules.GenderAllows(gate.Value, gender)) continue;

                    int hides = 0;
                    if (preset.hides != null)
                        foreach (string section in preset.hides)
                            if (BodySectionNames.TryParseKey(section, out var parsed))
                                hides |= (int)parsed;

                    entries.Add(new PresetEntry
                    {
                        slotClass = s,
                        objectName = preset.objectName,
                        displayName = preset.displayName,
                        gender = gate.Value,
                        hides = hides,
                        tagMask = MaskOf(preset.tags, catalog.tags),
                        conflictMask = MaskOf(preset.conflicts, catalog.tags),
                        weight = preset.weight,
                    });
                }
            }

            start[catalog.slotClasses.Length] = entries.Count;
            bundle.presets = entries.ToArray();
            bundle.presetStart = start;
        }

        static void BakeSlotMaps(CharacterCatalog catalog, GenderBundle bundle, ScannedBody body)
        {
            var maps = new List<RendererSlotMap>();

            foreach (var scanned in body.objects)
            {
                int count = scanned.materialNames.Length;
                var colorClass = new int[count];
                var materialIndex = new int[count];
                bool anythingToDo = false;

                for (int i = 0; i < count; i++)
                {
                    colorClass[i] = -1;
                    materialIndex[i] = -1;

                    if (!MaterialSlotKey.TryParse(scanned.materialNames[i], out var slot)) continue;

                    int index = Array.IndexOf(catalog.colorClasses, slot.ColorClass);
                    if (index < 0) continue;   // char_leather_1 and friends: left as authored

                    colorClass[i] = index;
                    materialIndex[i] = (slot.BaseKey - 1) * 2 + slot.ShadeLevel;
                    anythingToDo = true;
                }

                // A renderer with nothing to recolour needs no entry; SlotMap returning null is
                // already the "leave it alone" path.
                if (!anythingToDo) continue;

                maps.Add(new RendererSlotMap
                {
                    objectName = scanned.name,
                    colorClass = colorClass,
                    materialIndex = materialIndex,
                });
            }

            bundle.slotMaps = maps.ToArray();
        }
    }
}
