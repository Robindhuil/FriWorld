using System;
using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Character
{
    public enum Gender { Male = 0, Female = 1 }

    /// <summary>Which bodies a preset may appear on.</summary>
    public enum GenderGate { Any = 0, Male = 1, Female = 2 }

    [Serializable]
    public sealed class PresetEntry
    {
        public int slotClass;
        public string objectName;
        public string displayName;
        public GenderGate gender;

        /// <summary>BodySection bitmask of the skin this preset covers completely.</summary>
        public int hides;

        /// <summary>Bit per tag in CharacterCatalog.tags.</summary>
        public int tagMask;
        public int conflictMask;

        public int weight = 1;
    }

    [Serializable]
    public sealed class ColorwayEntry
    {
        public int colorClass;
        public string id;
        public string displayName;

        /// <summary>Dense, index = (baseKey - 1) * 2 + shadeLevel. Entries the class does not
        /// declare are null and are never asked for.</summary>
        public Material[] materials = Array.Empty<Material>();

        /// <summary>Not called "Material" — a method whose name equals its return type makes
        /// every later use of that type inside this class ambiguous.</summary>
        public Material MaterialFor(int baseKey, int shadeLevel)
        {
            int index = (baseKey - 1) * 2 + shadeLevel;
            return index >= 0 && index < materials.Length ? materials[index] : null;
        }
    }

    /// <summary>What to do with each material slot of one renderer, baked from its names.</summary>
    [Serializable]
    public sealed class RendererSlotMap
    {
        public string objectName;

        /// <summary>Per material slot: index into colorClasses, or -1 to leave it as authored.
        /// That -1 is how char_leather_1 survives untouched.</summary>
        public int[] colorClass = Array.Empty<int>();

        /// <summary>Per material slot: (baseKey - 1) * 2 + shadeLevel.</summary>
        public int[] materialIndex = Array.Empty<int>();
    }

    [Serializable]
    public sealed class GenderBundle
    {
        public GameObject basePrefab;

        /// <summary>Sorted by slotClass so presetStart can index into it.</summary>
        public PresetEntry[] presets = Array.Empty<PresetEntry>();

        /// <summary>CSR offsets, length slotClasses.Length + 1.</summary>
        public int[] presetStart = Array.Empty<int>();

        public RendererSlotMap[] slotMaps = Array.Empty<RendererSlotMap>();
    }

    /// <summary>
    /// The baked output of the three JSON registers: the only thing the game reads.
    ///
    /// Everything is an index, not a string, because Apply runs once per NPC spawn and string
    /// work there is pure waste. The names are kept only so a report can be read by a human.
    /// </summary>
    [CreateAssetMenu(menuName = "FriWorld/Character Catalog", fileName = "CharacterCatalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        public string[] slotClasses = Array.Empty<string>();
        public string[] colorClasses = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();

        /// <summary>Sorted by colorClass so colorwayStart can index into it.</summary>
        public ColorwayEntry[] colorways = Array.Empty<ColorwayEntry>();

        /// <summary>CSR offsets, length colorClasses.Length + 1.</summary>
        public int[] colorwayStart = Array.Empty<int>();

        public GenderBundle male = new GenderBundle();
        public GenderBundle female = new GenderBundle();

        Dictionary<string, RendererSlotMap> maleMaps;
        Dictionary<string, RendererSlotMap> femaleMaps;

        public GenderBundle Bundle(Gender gender) => gender == Gender.Male ? male : female;

        public int PresetCount(Gender gender, int slotClass)
        {
            var bundle = Bundle(gender);
            if (bundle == null || bundle.presetStart == null) return 0;
            if (slotClass < 0 || slotClass + 1 >= bundle.presetStart.Length) return 0;
            return bundle.presetStart[slotClass + 1] - bundle.presetStart[slotClass];
        }

        public PresetEntry Preset(Gender gender, int slotClass, int index)
        {
            var bundle = Bundle(gender);
            return bundle.presets[bundle.presetStart[slotClass] + index];
        }

        public int ColorwayCount(int colorClass)
        {
            if (colorwayStart == null) return 0;
            if (colorClass < 0 || colorClass + 1 >= colorwayStart.Length) return 0;
            return colorwayStart[colorClass + 1] - colorwayStart[colorClass];
        }

        public ColorwayEntry Colorway(int colorClass, int index) =>
            colorways[colorwayStart[colorClass] + index];

        public RendererSlotMap SlotMap(Gender gender, string objectName)
        {
            var cache = gender == Gender.Male
                ? maleMaps ?? (maleMaps = Index(male))
                : femaleMaps ?? (femaleMaps = Index(female));

            return objectName != null && cache.TryGetValue(objectName, out var map) ? map : null;
        }

        void OnDisable()
        {
            // Domain reload or an edit to the asset invalidates the caches.
            maleMaps = null;
            femaleMaps = null;
        }

        static Dictionary<string, RendererSlotMap> Index(GenderBundle bundle)
        {
            var map = new Dictionary<string, RendererSlotMap>(StringComparer.Ordinal);
            if (bundle == null || bundle.slotMaps == null) return map;

            foreach (var entry in bundle.slotMaps)
                if (entry != null && !string.IsNullOrEmpty(entry.objectName))
                    map[entry.objectName] = entry;

            return map;
        }
    }
}
