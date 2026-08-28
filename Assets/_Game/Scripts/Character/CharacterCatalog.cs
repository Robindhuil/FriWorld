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

    /// <summary>
    /// One colour a colour slot can take.
    ///
    /// A colorway is a single colour, not a package of them. Each colour slot — torso 1, torso 2,
    /// legs 1 — has its own palette and draws from it independently, so the secondary colour of a
    /// garment is free of its main colour whether that secondary is a stripe, a print or a tie.
    /// </summary>
    [Serializable]
    public sealed class ColorwayEntry
    {
        public int colorSlot;
        public string id;
        public string displayName;

        public Material material;

        /// <summary>The derived darker material, null when the class declares no shade.</summary>
        public Material shade;

        public Material For(int shadeLevel) => shadeLevel <= 0 ? material : shade;
    }

    /// <summary>What to do with each material slot of one renderer, baked from its names.</summary>
    [Serializable]
    public sealed class RendererSlotMap
    {
        public string objectName;

        /// <summary>Per material slot: index into the catalog's colour slots, or -1 to leave the
        /// slot as authored. That -1 is how char_leather_1 survives untouched.</summary>
        public int[] colorSlot = Array.Empty<int>();

        /// <summary>Per material slot: 0 for the base colour, 1 for the derived shade.</summary>
        public int[] shadeLevel = Array.Empty<int>();
    }

    /// <summary>
    /// How tall this body stands and how much that varies across the population.
    ///
    /// Height is carried as a byte across [min, max] rather than as a float, so the whole look
    /// stays a row of indices: 20 cm over 255 steps is under a millimetre, and a creator slider
    /// maps onto it directly.
    ///
    /// It becomes a uniform scale on the character root, never a scale on one axis. A bone has
    /// its own rotation, and a non-uniform scale composed with a rotation is a shear, not a
    /// stretch — a Y-only scale leaves the legs longer but the head egg-shaped and a T-posed arm
    /// vertically fatter. Uniform scale costs a slightly-off head size instead, and that only
    /// shows once the scale strays far from 1, which is why modelHeight should sit near the mean.
    /// </summary>
    [Serializable]
    public sealed class BodySize
    {
        /// <summary>What the mesh actually measures, floor to crown.</summary>
        public float modelHeight = 1f;

        public float mean = 1f;
        public float deviation;
        public float min = 1f;
        public float max = 1f;

        public float Metres(byte height) =>
            max <= min ? mean : Mathf.Lerp(min, max, height / 255f);

        public float ScaleFor(byte height) =>
            modelHeight <= 0f ? 1f : Metres(height) / modelHeight;

        public byte Quantise(float metres) => max <= min
            ? (byte)0
            : (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(min, max, metres) * 255f), 0, 255);

        /// <summary>
        /// Box-Muller, because adult stature is very close to normally distributed.
        ///
        /// A draw outside the band is redrawn rather than clamped. Clamping looks harmless and is
        /// not: with a band of about one and a half deviations, roughly 15% of a crowd lands on
        /// exactly the shortest and exactly the tallest value, which is a pile nobody has in real
        /// life. Redrawing gives the truncated bell the band actually describes.
        /// </summary>
        public byte Roll(System.Random rng)
        {
            if (deviation <= 0f) return Quantise(mean);

            // Bounded so a nonsensical band — a mean far outside [min, max] — cannot spin here.
            for (int attempt = 0; attempt < 16; attempt++)
            {
                double u1 = 1.0 - rng.NextDouble();   // (0, 1], so Log never sees zero
                double u2 = rng.NextDouble();
                double normal = System.Math.Sqrt(-2.0 * System.Math.Log(u1))
                                * System.Math.Sin(2.0 * System.Math.PI * u2);

                float metres = (float)(mean + deviation * normal);
                if (metres >= min && metres <= max) return Quantise(metres);
            }

            return Quantise(Mathf.Clamp(mean, min, max));
        }
    }

    [Serializable]
    public sealed class GenderBundle
    {
        public GameObject basePrefab;

        public BodySize size = new BodySize();

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
    ///
    /// Colour is organised by **slot**, not by class. A colour class says how many colours a
    /// garment has; each of those slots then has its own palette and rolls independently. That is
    /// what keeps a shirt's secondary colour free of its main one without the code having to know
    /// whether that secondary is a stripe, a print or a tie.
    /// </summary>
    [CreateAssetMenu(menuName = "FriWorld/Character Catalog", fileName = "CharacterCatalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        public string[] slotClasses = Array.Empty<string>();
        public string[] colorClasses = Array.Empty<string>();
        public string[] tags = Array.Empty<string>();

        /// <summary>Colour slots, flattened. Parallel arrays: which class, and which key in it.</summary>
        public int[] colorSlotClass = Array.Empty<int>();
        public int[] colorSlotKey = Array.Empty<int>();

        /// <summary>Sorted by colorSlot so colorwayStart can index into it.</summary>
        public ColorwayEntry[] colorways = Array.Empty<ColorwayEntry>();

        /// <summary>CSR offsets, length colorSlotClass.Length + 1.</summary>
        public int[] colorwayStart = Array.Empty<int>();

        public GenderBundle male = new GenderBundle();
        public GenderBundle female = new GenderBundle();

        Dictionary<string, RendererSlotMap> maleMaps;
        Dictionary<string, RendererSlotMap> femaleMaps;

        public int ColorSlotCount => colorSlotClass.Length;

        public GenderBundle Bundle(Gender gender) => gender == Gender.Male ? male : female;

        public BodySize Size(Gender gender)
        {
            var bundle = Bundle(gender);
            return bundle != null ? bundle.size : null;
        }

        /// <summary>Flat index of one colour slot, or -1 when the catalog does not declare it.</summary>
        public int ColorSlotIndex(int colorClass, int baseKey)
        {
            for (int i = 0; i < colorSlotClass.Length; i++)
                if (colorSlotClass[i] == colorClass && colorSlotKey[i] == baseKey)
                    return i;
            return -1;
        }

        /// <summary>"torso 2" — for reports.</summary>
        public string ColorSlotName(int colorSlot) =>
            colorSlot >= 0 && colorSlot < colorSlotClass.Length
                ? colorClasses[colorSlotClass[colorSlot]] + " " + colorSlotKey[colorSlot]
                : "?";

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

        public int ColorwayCount(int colorSlot)
        {
            if (colorwayStart == null) return 0;
            if (colorSlot < 0 || colorSlot + 1 >= colorwayStart.Length) return 0;
            return colorwayStart[colorSlot + 1] - colorwayStart[colorSlot];
        }

        public ColorwayEntry Colorway(int colorSlot, int index) =>
            colorways[colorwayStart[colorSlot] + index];

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
