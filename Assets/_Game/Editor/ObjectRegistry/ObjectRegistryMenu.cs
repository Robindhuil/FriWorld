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

        [MenuItem("Tools/Object Registry/Seed From Legacy Keyword Rules")]
        static void SeedFromLegacy()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int seeded = 0, prefilled = 0;
            var seen = new HashSet<string>();
            foreach (var o in scan.objects)
            {
                if (!seen.Add(o.typeKey)) continue;
                if (registry.Find(o.typeKey) != null) continue;

                registry.Seed(o.typeKey);
                seeded++;

                // Apply the old rules to the type key. Where the old rules produce nothing the
                // entry stays null, which is exactly the "you never decided this" state.
                var entry = registry.Find(o.typeKey);
                string collider = LegacyCollider(o.typeKey);
                string layer = LegacyLayer(o.typeKey);
                if (collider != null && layer != null)
                {
                    entry.collider = collider;
                    entry.layer = layer;
                    entry.occluder = "auto";
                    prefilled++;
                }
            }

            registry.Save(TypesPath);
            AssetDatabase.Refresh();
            Debug.Log("[ObjectRegistry] seeded " + seeded + " types, " + prefilled
                    + " pre-filled from the old keyword rules, " + (seeded - prefilled)
                    + " left null and needing a decision.");
        }

        static string LegacyCollider(string typeKey)
        {
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderIgnore) != null) return "none";
            var mesh = LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderMesh);
            var box  = LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.ColliderBox);
            if (mesh == null && box == null) return null;
            if (mesh == null) return "box";
            if (box == null) return "mesh";
            return mesh.Length >= box.Length ? "mesh" : "box";
        }

        static string LegacyLayer(string typeKey)
        {
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerInteractable) != null) return "interactable";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerNoObstacle) != null) return "noObstacle";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerObstacle) != null) return "obstacle";
            if (LegacyKeywordRules.BestMatch(typeKey, LegacyKeywordRules.LayerNav) != null) return "nav";
            return null;
        }

        [MenuItem("Tools/Object Registry/Dry Run Diff (old rules vs registry)")]
        static void DryRunDiff()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            int same = 0, changed = 0, unhandled = 0;
            // Group by type key so the output stays readable: one line per key, not per object.
            var byKey = new Dictionary<string, string>();
            var counts = new Dictionary<string, int>();

            foreach (var o in scan.objects)
            {
                // The old rules matched the RAW object name, not a derived key. That difference
                // is the whole point, so the comparison has to use the raw name on the old side.
                string oldCollider = LegacyCollider(o.gameObject.name) ?? "none";
                string oldLayer    = LegacyLayer(o.gameObject.name) ?? "keep";

                var entry = registry.Find(o.typeKey);
                if (entry == null || !entry.IsDecided) { unhandled++; continue; }

                if (entry.collider == oldCollider && entry.layer == oldLayer) { same++; continue; }

                changed++;
                string line = o.typeKey + "   old: " + oldCollider + "/" + oldLayer
                            + "   new: " + entry.collider + "/" + entry.layer;
                byKey[line] = o.path;
                counts.TryGetValue(line, out int c);
                counts[line] = c + 1;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[ObjectRegistry] dry run over " + scan.objects.Count + " objects");
            sb.AppendLine("  identical: " + same);
            sb.AppendLine("  CHANGED:   " + changed + "  (each is either a fix or a regression — classify them)");
            sb.AppendLine("  unhandled: " + unhandled + "  (unknown or undecided; left untouched)");
            if (byKey.Count > 0)
            {
                sb.AppendLine("  changes by type (" + byKey.Count + " distinct):");
                foreach (var kv in byKey)
                    sb.AppendLine("    x" + counts[kv.Key] + "  " + kv.Key + "\n        e.g. " + kv.Value);
            }
            Debug.Log(sb.ToString());
        }

        [MenuItem("Tools/Object Registry/Report On Selection", true)]
        [MenuItem("Tools/Object Registry/Seed Missing Types From Selection", true)]
        [MenuItem("Tools/Object Registry/Add Prefixes From Selection", true)]
        [MenuItem("Tools/Object Registry/Seed From Legacy Keyword Rules", true)]
        [MenuItem("Tools/Object Registry/Dry Run Diff (old rules vs registry)", true)]
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
