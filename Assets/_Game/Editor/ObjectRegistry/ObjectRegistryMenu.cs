using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    public static class ObjectRegistryMenu
    {
        public const string PrefixesPath      = "Assets/_Game/Editor/ObjectPrefixes.json";
        public const string TypesPath         = "Assets/_Game/Editor/ObjectTypes.json";
        public const string RoomPlatformsPath = "Assets/_Game/Editor/RoomPlatforms.json";

        [MenuItem("FriWorld/Registry/Report On Selection")]
        static void Report()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;
            Debug.Log(RegistryReport.Build(scan, registry));
        }

        [MenuItem("FriWorld/Registry/Seed Missing Types From Selection")]
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

        [MenuItem("FriWorld/Registry/Add Prefixes From Selection")]
        static void AddPrefixes()
        {
            if (!TryScanSelection(out var scan, out var registry)) return;

            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            // Case-insensitive, because matching is: keeping both "Outside" and "outside"
            // would be two rows doing one job.
            var known = new HashSet<string>(prefixes, System.StringComparer.OrdinalIgnoreCase);
            var added = new List<string>();
            var eatsAType = new List<string>();

            foreach (var p in scan.proposedPrefixes)
            {
                // A prefix that is also a type key would eat part of a multi-word type
                // ("wall" swallowing "wall_edge").
                if (scan.riskyPrefixes.Contains(p)) continue;

                if (EatsARegisteredType(p, registry)) { eatsAType.Add(p); continue; }

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
            if (eatsAType.Count > 0)
            {
                sb.AppendLine("  WITHHELD — each would swallow the head of a registered type:");
                foreach (var p in eatsAType)
                    sb.AppendLine("    ! " + p + "  →  " + FirstTypeEaten(p, registry));
            }
            Debug.Log(sb.ToString());

            // New containers mean new areas, and an area with no row in RoomPlatforms.json is
            // invisible to the gate tooling. One scan, two files kept in step.
            Debug.Log(SyncRoomPlatformsFile());
        }

        /// <summary>
        /// True when approving this prefix would swallow the head of a registered type — e.g.
        /// "cubboard_1" turns cubboard_1_part_1 into "part", and the cubboard_1_part entry goes
        /// dead. The scanner's own risky list only catches a prefix that IS a type key; this
        /// catches the ones that merely start one, which is the same failure with a longer name.
        /// </summary>
        static bool EatsARegisteredType(string prefix, TypeRegistry registry)
            => FirstTypeEaten(prefix, registry) != null;

        static string FirstTypeEaten(string prefix, TypeRegistry registry)
        {
            if (string.IsNullOrEmpty(prefix) || registry == null) return null;

            string head = prefix + "_";
            foreach (var t in registry.types)
                if (t != null && !string.IsNullOrEmpty(t.name)
                    && t.name.StartsWith(head, System.StringComparison.OrdinalIgnoreCase))
                    return t.name;
            return null;
        }

        [MenuItem("FriWorld/Registry/Sync Room Platforms")]
        static void SyncRoomPlatforms() => Debug.Log(SyncRoomPlatformsFile());

        /// <summary>
        /// Brings RoomPlatforms.json in line with the areas that exist. An area is a container
        /// whose name is an approved prefix, so the two files share one key space and stay
        /// readable side by side.
        ///
        /// Scans the whole PREFAB, not the current selection: a partial selection would leave the
        /// file describing half a building, and the appliers need the whole picture to be able to
        /// tell "this gate should go" from "I have not looked there".
        /// </summary>
        internal static string SyncRoomPlatformsFile()
        {
            var prefixes = TypeRegistry.LoadPrefixes(PrefixesPath);
            var platforms = RoomPlatforms.Load(RoomPlatformsPath);

            ReconcileResult result;
            var contents = RoomGateScope.Open();
            try
            {
                result = platforms.Reconcile(RoomGateScope.AreaNames(contents, prefixes));
            }
            finally
            {
                RoomGateScope.Close(contents);
            }

            platforms.Save(RoomPlatformsPath);
            AssetDatabase.Refresh();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[RoomPlatforms] " + platforms.rooms.Count + " areas in " + RoomPlatformsPath);

            if (result.added.Count > 0)
            {
                sb.AppendLine("  NEW, undecided — they are at the TOP of the file ("
                            + result.added.Count + "):");
                for (int i = 0; i < result.added.Count && i < 30; i++)
                    sb.AppendLine("    + " + result.added[i]);
                if (result.added.Count > 30)
                    sb.AppendLine("    … and " + (result.added.Count - 30) + " more");
            }
            if (result.orphans.Count > 0)
            {
                sb.AppendLine("  ORPHANS — no such container any more, the decision was KEPT ("
                            + result.orphans.Count + "):");
                for (int i = 0; i < result.orphans.Count && i < 30; i++)
                    sb.AppendLine("    ? " + result.orphans[i]);
                if (result.orphans.Count > 30)
                    sb.AppendLine("    … and " + (result.orphans.Count - 30) + " more");
            }
            if (result.added.Count == 0 && result.orphans.Count == 0)
                sb.AppendLine("  already in sync");

            return sb.ToString();
        }

        [MenuItem("FriWorld/Registry/Report On Selection", true)]
        [MenuItem("FriWorld/Registry/Seed Missing Types From Selection", true)]
        [MenuItem("FriWorld/Registry/Add Prefixes From Selection", true)]
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
