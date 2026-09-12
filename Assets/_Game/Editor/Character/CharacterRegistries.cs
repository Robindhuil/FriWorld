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

        /// <summary>
        /// The class this one takes its colour from, or null when it rolls on its own.
        ///
        /// Eyebrows are the case. They need their own material, because the texture is the brow
        /// atlas and not the hair one, but a blond head with black brows reads as a bug rather
        /// than as variety. A follower has no palette of its own: it inherits the source's
        /// colorways one for one, under the same ids, and rolls to whatever the source rolled.
        /// </summary>
        public string follows;

        /// <summary>How the inherited colour is bent on the way in. 1 and 1 take it unchanged;
        /// brows sit a little darker than the hair they follow.</summary>
        public float followValue = 1f;
        public float followSaturation = 1f;

        /// <summary>
        /// How far the follower may stray from what the source rolled, in palette steps.
        ///
        /// Zero means it never does: brows are the hair colour, full stop. A beard is the case
        /// for one — beards genuinely run a shade off the hair, and forbidding that makes a crowd
        /// look printed. Since the step is an index, the palette has to be ordered from dark to
        /// light for it to mean anything, and Report checks that it is.
        /// </summary>
        public int followDrift;

        /// <summary>How often the drift happens at all. 0.25 is a quarter of the crowd.</summary>
        public float followDriftChance;
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
    /// One continuous face feature — a nose that widens, a jaw that broadens.
    ///
    /// Unlike a preset, which picks one of a list, an axis is a slider: the values compose, so
    /// six axes are a surface of faces rather than six choices.
    ///
    /// "shape" is the blend shape authored on the face mesh. The same name has to exist on
    /// everything that sits on the face — brows, beard, eyes, scalp — or those stay behind while
    /// the face moves under them. tools/blender/propagate_shape_keys.py is what writes them.
    ///
    /// One key covers both directions. Blend shape weights are not clamped in this project
    /// (legacyClampBlendShapeWeights is 0), so a narrow nose is simply the negative weight of the
    /// key that widens it, and nothing has to be sculpted twice.
    /// </summary>
    public sealed class ShapeAxisDef
    {
        public string name;
        public string shape;

        /// <summary>
        /// Weight at each end of the byte range, and they are not required to match.
        ///
        /// A feature rarely varies as far in both directions: a nose goes further toward broad
        /// than toward pinched, so rangeMax 100 with rangeMin -50 is the honest description of
        /// it. Both are trims on the sculpted key, so narrowing an end never means resculpting.
        /// </summary>
        public float rangeMin = -100f;
        public float rangeMax = 100f;

        /// <summary>Where the population sits. 128 is the sculpted neutral.</summary>
        public int mean = 128;
        public float deviation = 45f;
    }

    public sealed class ShapeRegistry
    {
        public List<ShapeAxisDef> axes = new List<ShapeAxisDef>();
    }

    /// <summary>
    /// The four hand-edited registers, next to ObjectTypes.json and RoomPlatforms.json.
    ///
    /// They are the source of truth and nothing but the editor reads them: turning "navy" into
    /// an actual Material is what Bake Catalog is for.
    /// </summary>
    public static class CharacterRegistries
    {
        public const string ClassesPath   = "Assets/_Game/Editor/CharacterClasses.json";
        public const string ColorwaysPath = "Assets/_Game/Editor/CharacterColorways.json";
        public const string PresetsPath   = "Assets/_Game/Editor/CharacterPresets.json";
        public const string ShapesPath    = "Assets/_Game/Editor/CharacterShapes.json";

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
        public static ShapeRegistry LoadShapes() => LoadFrom<ShapeRegistry>(ShapesPath);
    }
}
