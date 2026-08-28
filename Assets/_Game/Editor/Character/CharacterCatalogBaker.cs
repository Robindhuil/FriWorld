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

            int errors = 0;
            foreach (var issue in issues)
                if (issue.severity == Severity.Error) errors++;

            if (errors > 0)
            {
                Debug.LogError($"Bake Catalog refused: Report finds {errors} errors. "
                               + "Run Character > 1 — Report and fix them first.");
                return;
            }

            if (bodies.Count == 0)
            {
                Debug.LogError("Bake Catalog refused: no base prefab exists to bake.");
                return;
            }

            // A body that does not exist yet is a different thing from a register that is wrong.
            // The bodies that do exist bake into a valid catalog; the missing one keeps showing
            // up as an error in Report until it is modelled.
            if (missing.Count > 0)
                Debug.LogWarning($"Bake Catalog: baking {bodies.Count} of 2 bodies. Still missing: "
                                 + string.Join(", ", missing));

            var catalog = LoadOrCreate();

            // Clear whichever bundle has no body, so a stale bake cannot linger in the asset.
            foreach (Gender gender in new[] { Gender.Male, Gender.Female })
            {
                bool present = false;
                foreach (var body in bodies) if (body.gender == gender) present = true;
                if (present) continue;

                var stale = gender == Gender.Male ? catalog.male : catalog.female;
                stale.basePrefab = null;
                stale.presets = new PresetEntry[0];
                stale.presetStart = new int[classes.slotClasses.Count + 1];
                stale.slotMaps = new RendererSlotMap[0];
            }

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
                bundle.size = BakeSize(classes, body.gender);
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

        /// <summary>A body with no entry in "bodies" scales 1, which is the model's own height.</summary>
        static BodySize BakeSize(ClassRegistry classes, Gender gender)
        {
            string wanted = gender == Gender.Male ? "male" : "female";

            foreach (var def in classes.bodies)
            {
                if (def.gender != wanted) continue;

                return new BodySize
                {
                    modelHeight = def.modelHeight,
                    mean = def.heightMean,
                    deviation = def.heightDeviation,
                    min = def.heightMin,
                    max = def.heightMax,
                };
            }

            return new BodySize
            {
                modelHeight = 1f, mean = 1f, deviation = 0f, min = 1f, max = 1f,
            };
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

        /// <summary>
        /// Colour slots first — one per (class, key) the classes declare — then the colorways
        /// sorted into them. A colorway belongs to a slot, not to a class, which is what lets a
        /// garment's secondary colour have its own palette.
        /// </summary>
        static void BakeColorways(CharacterCatalog catalog, ClassRegistry classes,
                                  ColorwayRegistry registry)
        {
            var slotClass = new List<int>();
            var slotKey = new List<int>();

            for (int c = 0; c < catalog.colorClasses.Length; c++)
            {
                var def = classes.colorClasses.Find(d => d.name == catalog.colorClasses[c]);
                for (int key = 1; key <= def.mainColors; key++)
                {
                    slotClass.Add(c);
                    slotKey.Add(key);
                }
            }

            catalog.colorSlotClass = slotClass.ToArray();
            catalog.colorSlotKey = slotKey.ToArray();

            var entries = new List<ColorwayEntry>();
            var start = new int[slotClass.Count + 1];

            for (int slot = 0; slot < slotClass.Count; slot++)
            {
                start[slot] = entries.Count;

                string className = catalog.colorClasses[slotClass[slot]];
                int key = slotKey[slot];
                var def = classes.colorClasses.Find(d => d.name == className);

                foreach (var way in registry.colorways)
                {
                    if (way.colorClass != className || way.slot != key) continue;

                    entries.Add(new ColorwayEntry
                    {
                        colorSlot = slot,
                        id = way.id,
                        displayName = way.displayName,
                        material = LoadMaterial(className, way.id, key.ToString()),
                        shade = def.shadeValue.HasValue
                            ? LoadMaterial(className, way.id, key + "1")
                            : null,
                    });
                }
            }

            start[slotClass.Count] = entries.Count;
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
                var colorSlot = new int[count];
                var shadeLevel = new int[count];
                bool anythingToDo = false;

                for (int i = 0; i < count; i++)
                {
                    colorSlot[i] = -1;
                    shadeLevel[i] = 0;

                    if (!MaterialSlotKey.TryParse(scanned.materialNames[i], out var parsed)) continue;

                    int colorClass = Array.IndexOf(catalog.colorClasses, parsed.ColorClass);
                    if (colorClass < 0) continue;   // char_leather_1 and friends: left as authored

                    int slot = catalog.ColorSlotIndex(colorClass, parsed.BaseKey);
                    if (slot < 0) continue;

                    colorSlot[i] = slot;
                    shadeLevel[i] = parsed.ShadeLevel;
                    anythingToDo = true;
                }

                // A renderer with nothing to recolour needs no entry; SlotMap returning null is
                // already the "leave it alone" path.
                if (!anythingToDo) continue;

                maps.Add(new RendererSlotMap
                {
                    objectName = scanned.name,
                    colorSlot = colorSlot,
                    shadeLevel = shadeLevel,
                });
            }

            bundle.slotMaps = maps.ToArray();
        }
    }
}
