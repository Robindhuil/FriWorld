using System.Collections.Generic;
using System.Text;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// Formats what the scan found. Deliberately short: a key with its count and at most a few
    /// example paths, not every hit. The whole system only works while this stays readable — a
    /// thousand-line dump gets skipped, and then nothing is being caught at all.
    /// </summary>
    public static class RegistryReport
    {
        public const int MaxExamplePaths = 3;

        public static string Build(ScanResult scan, TypeRegistry registry)
        {
            var unknown = new Dictionary<string, List<string>>();
            var undecided = new Dictionary<string, List<string>>();
            var used = new HashSet<string>();

            var ambiguousKeys = new Dictionary<string, List<string>>();

            foreach (var o in scan.objects)
            {
                var entry = registry.Find(o.typeKey, out bool ambiguous);
                if (ambiguous) Add(ambiguousKeys, o.typeKey, o.path);

                if (entry == null) Add(unknown, o.typeKey, o.path);
                else
                {
                    // Record the entry that answered, so a pattern counts as used by every key
                    // it covers rather than looking dead.
                    used.Add(entry.name);
                    if (!entry.IsDecided) Add(undecided, o.typeKey, o.path);
                }
            }

            // A key that still carries a room code means no prefix covered this object, so the
            // key is the whole name and will never match anything. That is a missing prefix,
            // not a missing type — a different fix, so it gets its own section.
            var unstripped = new Dictionary<string, List<string>>();
            foreach (var o in scan.objects)
                if (LooksUnstripped(o.typeKey)) Add(unstripped, o.typeKey, o.path);

            var dead = new List<string>();
            foreach (var t in registry.types)
                if (t != null && !string.IsNullOrEmpty(t.name) && !used.Contains(t.name))
                    dead.Add(t.name);
            dead.Sort(string.CompareOrdinal);

            var sb = new StringBuilder();
            sb.AppendLine("[ObjectRegistry] scanned " + scan.objects.Count + " mesh objects");
            Section(sb, "UNKNOWN types (not in the registry — likely a naming mistake)", unknown);
            Section(sb, "UNDECIDED types (in the registry, fields still null)", undecided);
            Section(sb, "UNSTRIPPED keys (a prefix is missing from ObjectPrefixes.json)", unstripped);
            Section(sb, "AMBIGUOUS (two equally specific patterns match — make one of them exact)", ambiguousKeys);

            if (scan.riskyPrefixes.Count > 0)
            {
                sb.AppendLine("  RISKY proposed prefixes (also a type key — do NOT approve these):");
                foreach (var p in scan.riskyPrefixes) sb.AppendLine("    " + p);
            }

            if (dead.Count > 0)
            {
                sb.AppendLine("  DEAD registry entries (" + dead.Count + ", nothing uses them):");
                foreach (var d in dead) sb.AppendLine("    " + d);
            }

            if (unknown.Count == 0 && undecided.Count == 0 && unstripped.Count == 0
                && ambiguousKeys.Count == 0)
                sb.AppendLine("  every scanned object resolved to a decided type");

            return sb.ToString();
        }

        /// <summary>True when the key still starts with a room code such as ra100 or rb308.</summary>
        static bool LooksUnstripped(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            int underscore = key.IndexOf('_');
            string head = underscore > 0 ? key.Substring(0, underscore) : key;
            if (head.Length < 3) return false;
            if (char.ToLowerInvariant(head[0]) != 'r' || !char.IsLetter(head[1])) return false;
            for (int i = 2; i < head.Length; i++) if (!char.IsDigit(head[i])) return false;
            return true;
        }

        static void Add(Dictionary<string, List<string>> map, string key, string path)
        {
            if (!map.TryGetValue(key, out var list)) { list = new List<string>(); map[key] = list; }
            list.Add(path);
        }

        static void Section(StringBuilder sb, string title, Dictionary<string, List<string>> map)
        {
            if (map.Count == 0) return;
            sb.AppendLine("  " + title + ": " + map.Count);
            foreach (var kv in map)
            {
                sb.AppendLine("    " + kv.Key + "  x" + kv.Value.Count);
                for (int i = 0; i < kv.Value.Count && i < MaxExamplePaths; i++)
                    sb.AppendLine("        " + kv.Value[i]);
                if (kv.Value.Count > MaxExamplePaths)
                    sb.AppendLine("        … and " + (kv.Value.Count - MaxExamplePaths) + " more");
            }
        }
    }
}
