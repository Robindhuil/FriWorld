using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.Character.Editor
{
    public sealed class ColorClassDef
    {
        public string name;
        public int mainColors = 1;

        /// <summary>Null means the class has no darker shade. Zero would mean black.</summary>
        public float? shadeValue;
        public float? shadeSaturation;
    }

    /// <summary>
    /// How tall one body stands and how much that varies.
    ///
    /// modelHeight is what the mesh actually measures, so the scale a character ends up with is
    /// height / modelHeight. Keeping the two apart is the point: when the model is resized in
    /// Blender only modelHeight changes, and the population stays where it was.
    /// </summary>
    public sealed class BodyDef
    {
        public string gender;
        public float modelHeight;
        public float heightMean;
        public float heightDeviation;
        public float heightMin;
        public float heightMax;
    }

    public sealed class ClassRegistry
    {
        public List<ColorClassDef> colorClasses = new List<ColorClassDef>();
        public List<string> slotClasses = new List<string>();
        public List<BodyDef> bodies = new List<BodyDef>();
    }

    /// <summary>
    /// One colour available to one colour slot.
    ///
    /// The slot is the class plus the key from the material name: torso 1 is a garment's main
    /// colour, torso 2 its secondary. They have separate palettes and roll separately, so the
    /// secondary is free of the main whether it is a stripe, a print or a tie.
    /// </summary>
    public sealed class ColorwayDef
    {
        public string colorClass;

        /// <summary>Which key of the class this colour is for. Defaults to the main colour.</summary>
        public int slot = 1;

        public string id;
        public string displayName;
        public string color;
    }

    public sealed class ColorwayRegistry
    {
        public List<ColorwayDef> colorways = new List<ColorwayDef>();
    }

    public sealed class PresetDef
    {
        public string slotClass;

        /// <summary>The GameObject name in the base prefab. "object" is a C# keyword.</summary>
        [JsonProperty("object")] public string objectName;

        public string displayName;
        public string gender = "any";
        public List<string> hides = new List<string>();
        public List<string> tags = new List<string>();
        public List<string> conflicts = new List<string>();
        public int weight = 1;
    }

    public sealed class PresetRegistry
    {
        public List<PresetDef> presets = new List<PresetDef>();
    }

    /// <summary>
    /// The three hand-edited registers, next to ObjectTypes.json and RoomPlatforms.json.
    ///
    /// They are the source of truth and nothing but the editor reads them: turning "navy" into
    /// an actual Material is what Bake Catalog is for.
    /// </summary>
    public static class CharacterRegistries
    {
        public const string ClassesPath   = "Assets/_Game/Editor/CharacterClasses.json";
        public const string ColorwaysPath = "Assets/_Game/Editor/CharacterColorways.json";
        public const string PresetsPath   = "Assets/_Game/Editor/CharacterPresets.json";

        /// <summary>A missing file reads as an empty register, so a fresh clone can still run
        /// Report and be told what to fill in.</summary>
        public static T LoadFrom<T>(string path) where T : new()
        {
            if (!File.Exists(path)) return new T();
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) ?? new T();
        }

        public static ClassRegistry LoadClasses() => LoadFrom<ClassRegistry>(ClassesPath);
        public static ColorwayRegistry LoadColorways() => LoadFrom<ColorwayRegistry>(ColorwaysPath);
        public static PresetRegistry LoadPresets() => LoadFrom<PresetRegistry>(PresetsPath);
    }
}
