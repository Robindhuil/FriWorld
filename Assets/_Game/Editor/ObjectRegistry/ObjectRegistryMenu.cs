using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    public static class ObjectRegistryMenu
    {
        public const string PrefixesPath = "Assets/_Game/Editor/ObjectPrefixes.json";
        public const string TypesPath    = "Assets/_Game/Editor/ObjectTypes.json";

        [MenuItem("Tools/Object Registry/Report On Selection")]
        static void Report()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;
            Debug.Log(RegistryReport.Build(scan, registry));
        }

        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection")]
        static void Seed()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int added = 0;
            foreach (var o in scan.objects)
                if (registry.Find(o.typeKey) == null) { registry.Seed(o.typeKey); added++; }

            registry.Save(TypesPath);
            AssetDatabase.Refresh();
            Debug.Log("[ObjectRegistry] seeded " + added + " undecided types into " + TypesPath
                    + "\nFill them in — until then their objects are left untouched.");
        }

        [MenuItem("Tools/Object Registry/Add Prefixes From Selection")]
        static void AddPrefixes()
        {
            if (!TryScanSelection(out var scan, out _)) return;

            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            // Case-insensitive, because matching is: keeping both "Outside" and "outside"
            // would be two rows doing one job.
            var known = new HashSet<string>(prefixes, System.StringComparer.OrdinalIgnoreCase);
            var added = new List<string>();

            foreach (var p in scan.proposedPrefixes)
            {
                // A prefix that is also a type key would eat part of a multi-word type
                // ("wall" swallowing "wall_edge"), and no automatic guard catches that.
                if (scan.riskyPrefixes.Contains(p)) continue;
                if (!known.Add(p)) continue;
                prefixes.Add(p);
                added.Add(p);
            }

            TypeRegistry.SavePrefixes(PrefixesPath, prefixes);
            AssetDatabase.Refresh();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[ObjectRegistry] added " + added.Count + " prefixes to " + PrefixesPath
                        + " (" + prefixes.Count + " total). Review them with git diff — nothing is"
                        + " applied to the scene until the generators run.");
            for (int i = 0; i < added.Count && i < 30; i++) sb.AppendLine("    + " + added[i]);
            if (added.Count > 30) sb.AppendLine("    … and " + (added.Count - 30) + " more");

            if (scan.riskyPrefixes.Count > 0)
            {
                sb.AppendLine("  WITHHELD as risky — each is also a type key, add by hand only if you are sure:");
                foreach (var p in scan.riskyPrefixes) sb.AppendLine("    ! " + p);
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Object Registry/Report On Selection", true)]
        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection", true)]
        [MenuItem("Tools/Object Registry/Add Prefixes From Selection", true)]
        static bool ValidateSelection() => Selection.gameObjects != null && Selection.gameObjects.Length > 0;

        internal static bool TryScanSelection(out ScanResult scan, out TypeRegistry registry)
        {
            scan = null;
            registry = TypeRegistry.Load(TypesPath);

            var roots = Selection.gameObjects;
            if (roots == null || roots.Length == 0)
            {
                Debug.LogWarning("[ObjectRegistry] Nothing selected in the Hierarchy.");
                return false;
            }

            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            scan = new ScanResult();
            foreach (var root in roots)
            {
                if (root == null) continue;
                var partial = RegistryScanner.Scan(root, prefixes);
                scan.objects.AddRange(partial.objects);
                foreach (var p in partial.proposedPrefixes)
                    if (!scan.proposedPrefixes.Contains(p)) scan.proposedPrefixes.Add(p);
                foreach (var p in partial.riskyPrefixes)
                    if (!scan.riskyPrefixes.Contains(p)) scan.riskyPrefixes.Add(p);
            }
            return true;
        }
    }
}
