using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One type and how the generators should treat it. Null means "not decided yet".</summary>
    public class TypeEntry
    {
        public string name;

        // Required. null here means "not decided yet" and the object is left untouched.
        public string collider;   // none | mesh | box | sphere
        public string layer;      // interactable | obstacle | noObstacle | nav | keep
        public string occluder;   // auto | yes | no

        // Optional overrides. Omitted from the file entirely when unset, so that a null in this
        // file always means "undecided" and never "derive it" — the two were indistinguishable
        // when every entry carried "static": null.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string @static;    // yes | no; otherwise derived from layer
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string tag;        // e.g. "Door"; otherwise derived from layer

        // Interactable behaviour to attach, e.g. "Door". Deliberately separate from the layer:
        // door_frame is interactable too but must not become an openable door, so the layer
        // cannot decide this.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string script;

        /// <summary>
        /// False while any required field is still null. An undecided entry is reported and its
        /// objects are left untouched — the whole point is that "present but blank" must not
        /// quietly mean "no collider, default layer".
        /// </summary>
        [JsonIgnore]
        public bool IsDecided => collider != null && layer != null && occluder != null;
    }

    public class TypeRegistry
    {
        public List<TypeEntry> types = new List<TypeEntry>();

        [JsonIgnore]
        readonly Dictionary<string, TypeEntry> index = new Dictionary<string, TypeEntry>();

        public static TypeRegistry FromJson(string json)
        {
            var registry = JsonConvert.DeserializeObject<TypeRegistry>(json) ?? new TypeRegistry();
            if (registry.types == null) registry.types = new List<TypeEntry>();
            registry.Reindex();
            return registry;
        }

        public static TypeRegistry Load(string path)
            => File.Exists(path) ? FromJson(File.ReadAllText(path)) : new TypeRegistry();

        public void Save(string path)
        {
            // Undecided entries float to the top, so a freshly seeded type is the first thing in
            // the file rather than something to scroll for. Once its fields are filled in it
            // settles into the alphabetical body on the next save, which keeps diffs readable —
            // plain insertion order would leave the file permanently unsorted.
            types.Sort((a, b) =>
            {
                bool aDecided = a.IsDecided, bDecided = b.IsDecided;
                if (aDecided != bDecided) return aDecided ? 1 : -1;
                return string.CompareOrdinal(a.name, b.name);
            });
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        void Reindex()
        {
            index.Clear();
            foreach (var t in types)
                if (t != null && !string.IsNullOrEmpty(t.name))
                    index[t.name] = t;
        }

        /// <summary>Placeholder standing for one all-digit token inside a pattern name.</summary>
        public const string IntPlaceholder = "<int>";

        /// <summary>True for entries written as a pattern, e.g. "window_&lt;int&gt;_glass".</summary>
        public static bool IsPattern(string name)
            => !string.IsNullOrEmpty(name) && name.Contains(IntPlaceholder);

        public TypeEntry Find(string typeKey) => Find(typeKey, out _);

        /// <summary>
        /// Exact name first, then patterns. Never a substring match — that is the bug this
        /// replaces. An exact entry always beats a pattern, so a single instance can still be
        /// given its own behaviour later without touching the pattern that covers the rest.
        /// </summary>
        public TypeEntry Find(string typeKey, out bool ambiguous)
        {
            ambiguous = false;
            if (string.IsNullOrEmpty(typeKey)) return null;
            if (index.TryGetValue(typeKey, out var exact)) return exact;

            TypeEntry best = null;
            int bestLiterals = -1;
            foreach (var t in types)
            {
                if (t == null || !IsPattern(t.name)) continue;
                if (!PatternMatches(t.name, typeKey)) continue;

                int literals = CountLiteralTokens(t.name);
                if (literals > bestLiterals) { best = t; bestLiterals = literals; ambiguous = false; }
                else if (literals == bestLiterals) ambiguous = true;   // two equally specific patterns
            }
            return best;
        }

        /// <summary>Token-by-token comparison; the placeholder accepts one all-digit token.</summary>
        static bool PatternMatches(string pattern, string typeKey)
        {
            var p = pattern.Split('_');
            var k = typeKey.Split('_');
            if (p.Length != k.Length) return false;

            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] == IntPlaceholder)
                {
                    if (k[i].Length == 0) return false;
                    foreach (char c in k[i]) if (!char.IsDigit(c)) return false;
                }
                else if (!string.Equals(p[i], k[i], System.StringComparison.Ordinal)) return false;
            }
            return true;
        }

        static int CountLiteralTokens(string pattern)
        {
            int n = 0;
            foreach (var token in pattern.Split('_')) if (token != IntPlaceholder) n++;
            return n;
        }

        /// <summary>
        /// Adds an undecided entry. Does nothing when the key is already covered — including by
        /// a pattern, so seeding never re-creates the concrete names a pattern was written to
        /// replace.
        /// </summary>
        public void Seed(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey) || Find(typeKey) != null) return;
            var entry = new TypeEntry { name = typeKey };
            types.Add(entry);
            index[typeKey] = entry;
        }

        class PrefixFile { public List<string> prefixes = new List<string>(); }

        public static List<string> PrefixesFromJson(string json)
        {
            var file = JsonConvert.DeserializeObject<PrefixFile>(json) ?? new PrefixFile();
            return file.prefixes ?? new List<string>();
        }

        public static List<string> LoadPrefixes(string path)
            => File.Exists(path) ? PrefixesFromJson(File.ReadAllText(path)) : new List<string>();

        public static void SavePrefixes(string path, List<string> prefixes)
        {
            prefixes.Sort(string.CompareOrdinal);
            File.WriteAllText(path,
                JsonConvert.SerializeObject(new PrefixFile { prefixes = prefixes }, Formatting.Indented));
        }
    }
}
