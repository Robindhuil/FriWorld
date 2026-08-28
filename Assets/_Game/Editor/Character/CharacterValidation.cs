using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character.Editor
{
    public enum Severity { Error, Note }

    public struct Issue
    {
        public Severity severity;
        public string text;

        public override string ToString() =>
            (severity == Severity.Error ? "ERROR " : "note  ") + text;
    }

    /// <summary>One renderer found while scanning a base prefab.</summary>
    public sealed class ScannedObject
    {
        public string name;
        public string[] materialNames = Array.Empty<string>();
    }

    public sealed class ScannedBody
    {
        public string prefabPath;
        public Gender gender;
        public List<ScannedObject> objects = new List<ScannedObject>();
    }

    /// <summary>
    /// Everything Report checks, with no Unity scene access of its own.
    ///
    /// The split matters: scanning a prefab needs the editor and a real asset, deciding whether
    /// what was scanned is coherent does not. Keeping the decision pure is what makes the rules
    /// testable one by one instead of by opening a prefab.
    ///
    /// Errors mean the bake would produce something wrong. Notes are things worth knowing — above
    /// all a material whose keyword is not a colour class, which is a legitimate way to say
    /// "leave this slot alone" and must not read as a failure.
    /// </summary>
    public static class CharacterValidation
    {
        public static List<Issue> Check(
            ClassRegistry classes,
            ColorwayRegistry colorways,
            PresetRegistry presets,
            IReadOnlyList<ScannedBody> bodies)
        {
            var issues = new List<Issue>();

            void Error(string text) => issues.Add(new Issue { severity = Severity.Error, text = text });
            void Note(string text) => issues.Add(new Issue { severity = Severity.Note, text = text });

            // ---- classes -------------------------------------------------------------
            var colorClassByName = new Dictionary<string, ColorClassDef>(StringComparer.Ordinal);
            foreach (var def in classes.colorClasses)
            {
                if (colorClassByName.ContainsKey(def.name))
                    Error($"DUPLICATE colour class '{def.name}' appears twice in CharacterClasses.json");
                else
                    colorClassByName[def.name] = def;

                if (def.mainColors < 1 || def.mainColors > 9)
                    Error($"RANGE colour class '{def.name}' declares mainColors {def.mainColors}, must be 1..9");

                if (def.shadeValue.HasValue != def.shadeSaturation.HasValue)
                    Error($"SHADE colour class '{def.name}' sets only one of shadeValue / shadeSaturation");
            }

            var slotClasses = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in classes.slotClasses)
                if (!slotClasses.Add(name))
                    Error($"DUPLICATE slot class '{name}' appears twice in CharacterClasses.json");

            // ---- colorways -----------------------------------------------------------
            var colorwayCount = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var way in colorways.colorways)
            {
                if (!colorClassByName.TryGetValue(way.colorClass, out var def))
                {
                    Error($"UNKNOWN colorway '{way.id}' names colour class '{way.colorClass}', "
                          + "which CharacterClasses.json does not declare");
                    continue;
                }

                int given = way.colors == null ? 0 : way.colors.Count;
                if (given != def.mainColors)
                    Error($"COUNT colorway '{way.colorClass}/{way.id}' lists {given} colours, "
                          + $"the class declares mainColors {def.mainColors}");

                if (way.colors != null)
                    foreach (string hex in way.colors)
                        if (!ColorUtility.TryParseHtmlString(hex, out _))
                            Error($"COLOUR colorway '{way.colorClass}/{way.id}' has an unreadable colour '{hex}'");

                colorwayCount.TryGetValue(way.colorClass, out int seen);
                colorwayCount[way.colorClass] = seen + 1;
            }

            foreach (var pair in colorClassByName)
            {
                colorwayCount.TryGetValue(pair.Key, out int count);
                if (count == 0)
                    Error($"EMPTY colour class '{pair.Key}' has no colorway");
                else if (count > 254)
                    Error($"OVERFLOW colour class '{pair.Key}' has {count} colorways, the index holds 254");
            }

            // ---- tags ----------------------------------------------------------------
            var providedTags = new HashSet<string>(StringComparer.Ordinal);
            foreach (var preset in presets.presets)
                if (preset.tags != null)
                    foreach (string tag in preset.tags)
                        providedTags.Add(tag);

            if (providedTags.Count > 32)
                Error($"OVERFLOW {providedTags.Count} distinct tags, the bitmask holds 32");

            // ---- presets -------------------------------------------------------------
            foreach (var preset in presets.presets)
            {
                string who = preset.objectName ?? "(no object)";

                if (string.IsNullOrEmpty(preset.objectName))
                    Error($"MISSING a preset in slot class '{preset.slotClass}' has no object name");

                if (!slotClasses.Contains(preset.slotClass))
                    Error($"UNKNOWN preset '{who}' names slot class '{preset.slotClass}', "
                          + "which CharacterClasses.json does not declare");

                if (ParseGender(preset.gender) == null)
                    Error($"GENDER preset '{who}' has gender '{preset.gender}', expected any / male / female");

                if (preset.weight < 1)
                    Error($"WEIGHT preset '{who}' has weight {preset.weight}, must be at least 1");

                if (preset.hides != null)
                    foreach (string section in preset.hides)
                        if (!BodySectionNames.TryParseKey(section, out _))
                            Error($"SECTION preset '{who}' hides '{section}', which is not a body section");

                if (preset.conflicts != null)
                    foreach (string tag in preset.conflicts)
                        if (!providedTags.Contains(tag))
                            Error($"DEAD preset '{who}' conflicts with tag '{tag}', which no preset provides");
            }

            // ---- body sizes ----------------------------------------------------------
            var bodyByGender = new Dictionary<string, BodyDef>(StringComparer.Ordinal);
            foreach (var def in classes.bodies)
            {
                if (def.gender != "male" && def.gender != "female")
                {
                    Error($"GENDER body entry has gender '{def.gender}', expected male / female");
                    continue;
                }

                if (bodyByGender.ContainsKey(def.gender))
                    Error($"DUPLICATE body entry for '{def.gender}' appears twice");
                else
                    bodyByGender[def.gender] = def;

                if (def.modelHeight <= 0f)
                    Error($"HEIGHT body '{def.gender}' has modelHeight {def.modelHeight}, must be above zero");

                if (def.heightMin >= def.heightMax)
                    Error($"HEIGHT body '{def.gender}' has heightMin {def.heightMin} "
                          + $"not below heightMax {def.heightMax}");

                if (def.heightMean < def.heightMin || def.heightMean > def.heightMax)
                    Error($"HEIGHT body '{def.gender}' has heightMean {def.heightMean} "
                          + $"outside [{def.heightMin}, {def.heightMax}]");

                if (def.heightDeviation < 0f)
                    Error($"HEIGHT body '{def.gender}' has a negative heightDeviation");

                // The thing that actually goes wrong with uniform scale: heads. A head is close to
                // a fixed 23 cm whatever the stature, so the further the scale strays from 1 the
                // more wrong it reads — small enough and the character starts looking like a child.
                if (def.modelHeight > 0f && def.heightMin < def.heightMax)
                {
                    float low = def.heightMin / def.modelHeight;
                    float high = def.heightMax / def.modelHeight;
                    float worst = Mathf.Max(Mathf.Abs(1f - low), Mathf.Abs(1f - high));

                    if (worst > 0.10f)
                        Note($"SCALE body '{def.gender}' scales between {low:0.000} and {high:0.000}, "
                             + $"up to {worst * 100f:0.0}% off the model. Past about 10% the head "
                             + "reads wrong — consider moving modelHeight nearer heightMean.");
                }
            }

            // ---- per body ------------------------------------------------------------
            foreach (var body in bodies)
            {
                string genderKey = body.gender == Gender.Male ? "male" : "female";
                if (!bodyByGender.ContainsKey(genderKey))
                    Error($"MISSING {body.gender}: no entry in \"bodies\" of CharacterClasses.json, "
                          + "so this body has no height to scale to");

                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var scanned in body.objects)
                    if (!names.Add(scanned.name))
                        Error($"DUPLICATE {body.gender}: two objects named '{scanned.name}' in "
                              + $"{body.prefabPath}; the slot map is keyed on the name");

                // Which sections the body actually carries. Resolved through the object name so
                // male_body_chest and female_body_chest both land on Chest.
                int present = 0;
                foreach (var scanned in body.objects)
                    if (BodySectionNames.TryParseObject(scanned.name, out var found))
                        present |= (int)found;

                foreach (var entry in BodySectionNames.All)
                    if ((present & (int)entry.section) == 0)
                        Error($"MISSING {body.gender}: body section '{entry.key}' is not in {body.prefabPath}");

                foreach (var preset in presets.presets)
                {
                    var gate = ParseGender(preset.gender);
                    if (gate == null || !PresetRules.GenderAllows(gate.Value, body.gender)) continue;

                    if (!names.Contains(preset.objectName))
                        Error($"MISSING {body.gender}: preset object '{preset.objectName}' is not in {body.prefabPath}");
                }

                foreach (string slotClass in classes.slotClasses)
                {
                    int usable = 0;
                    foreach (var preset in presets.presets)
                    {
                        if (preset.slotClass != slotClass) continue;
                        var gate = ParseGender(preset.gender);
                        if (gate != null && PresetRules.GenderAllows(gate.Value, body.gender)) usable++;
                    }

                    if (usable == 0)
                        Error($"EMPTY {body.gender}: slot class '{slotClass}' has no preset — "
                              + "the NPC would be missing that part");
                    else if (usable > 254)
                        Error($"OVERFLOW {body.gender}: slot class '{slotClass}' has {usable} presets, "
                              + "the index holds 254");
                }

                foreach (var scanned in body.objects)
                {
                    foreach (string materialName in scanned.materialNames)
                    {
                        if (!MaterialSlotKey.TryParse(materialName, out var slot))
                        {
                            Error($"UNPARSED {body.gender}: '{scanned.name}' carries '{materialName}', "
                                  + "which is not char_<class>_<key>");
                            continue;
                        }

                        if (!colorClassByName.TryGetValue(slot.ColorClass, out var def))
                        {
                            Note($"IGNORED {body.gender}: '{scanned.name}' slot '{materialName}' — "
                                 + $"'{slot.ColorClass}' is not a colour class, the slot stays as authored");
                            continue;
                        }

                        if (slot.BaseKey > def.mainColors)
                            Error($"RANGE {body.gender}: '{materialName}' asks for colour {slot.BaseKey}, "
                                  + $"class '{slot.ColorClass}' declares {def.mainColors}");

                        if (slot.ShadeLevel > 1)
                            Error($"SHADE {body.gender}: '{materialName}' asks for shade level "
                                  + $"{slot.ShadeLevel}; only level 1 is supported");

                        if (slot.ShadeLevel == 1 && !def.shadeValue.HasValue)
                            Error($"SHADE {body.gender}: '{materialName}' asks for a shade, "
                                  + $"class '{slot.ColorClass}' declares no shadeValue");
                    }
                }
            }

            return issues;
        }

        public static GenderGate? ParseGender(string gender)
        {
            switch (gender)
            {
                case "any":    return GenderGate.Any;
                case "male":   return GenderGate.Male;
                case "female": return GenderGate.Female;
                default:       return null;
            }
        }
    }
}
