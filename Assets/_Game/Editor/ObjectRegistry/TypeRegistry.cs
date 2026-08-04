using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One type and how the generators should treat it. Null means "not decided yet".</summary>
    public class TypeEntry
    {
        public string name;
        public string collider;   // none | mesh | box | sphere
        public string layer;      // interactable | obstacle | noObstacle | nav | keep
        public string occluder;   // auto | yes | no
        public string @static;    // optional override: yes | no
        public string tag;        // optional override, e.g. "Door"

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
            types.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        void Reindex()
        {
            index.Clear();
            foreach (var t in types)
                if (t != null && !string.IsNullOrEmpty(t.name))
                    index[t.name] = t;
        }

        /// <summary>Exact lookup. Never a substring match — that is the bug this replaces.</summary>
        public TypeEntry Find(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey)) return null;
            return index.TryGetValue(typeKey, out var entry) ? entry : null;
        }

        /// <summary>Adds an undecided entry. Existing entries are left alone.</summary>
        public void Seed(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey) || index.ContainsKey(typeKey)) return;
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
