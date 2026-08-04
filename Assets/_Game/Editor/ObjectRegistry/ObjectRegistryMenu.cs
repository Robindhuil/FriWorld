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

        [MenuItem("Tools/Object Registry/Propose Prefixes From Selection")]
        static void ProposePrefixes()
        {
            if (!TryScanSelection(out var scan, out _)) return;

            var existing = TypeRegistry.LoadPrefixes(PrefixesPath);
            var known = new HashSet<string>(existing);
            var fresh = new List<string>();
            foreach (var p in scan.proposedPrefixes)
                if (!known.Contains(p) && !scan.riskyPrefixes.Contains(p)) fresh.Add(p);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[ObjectRegistry] " + fresh.Count
                        + " prefix proposals (NOT written — review, then add by hand):");
            foreach (var p in fresh) sb.AppendLine("    " + p);
            if (scan.riskyPrefixes.Count > 0)
            {
                sb.AppendLine("  withheld as risky (each is also a type key):");
                foreach (var p in scan.riskyPrefixes) sb.AppendLine("    " + p);
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Object Registry/Report On Selection", true)]
        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection", true)]
        [MenuItem("Tools/Object Registry/Propose Prefixes From Selection", true)]
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
