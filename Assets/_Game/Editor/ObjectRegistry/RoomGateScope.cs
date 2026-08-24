using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>
    /// Reads the FriBuilding prefab asset.
    ///
    /// Everything the gate tooling writes goes into the PREFAB ASSET, never onto a scene
    /// instance. Components added to the instance are prefab overrides, and one revert or one
    /// .blend reimport wipes them — that is how the door gates were lost before this existed,
    /// while the PlatformGates that happened to sit in the asset survived.
    ///
    /// Lives in the registry assembly rather than beside the appliers because the sync menu
    /// needs it too, and Assembly-CSharp-Editor can see this assembly but not the reverse. It
    /// therefore knows nothing about PlatformGate, ComponentGate or Door.
    /// </summary>
    public static class RoomGateScope
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/FriBuilding/FriBuilding.prefab";

        /// <summary>Opens the prefab in an isolated preview scene. Always pair with Close.</summary>
        public static GameObject Open() => PrefabUtility.LoadPrefabContents(PrefabPath);

        public static void Close(GameObject contents) => PrefabUtility.UnloadPrefabContents(contents);

        public static void SaveAndClose(GameObject contents)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        /// <summary>
        /// The area containers: every container whose name is an approved prefix.
        ///
        /// Exact membership, no stripping. The scanner proposes container names whole, so
        /// ra100_corridor_1 and ra100_corridor_2 are two prefixes and two areas that decide
        /// separately. The approved list is also what keeps furniture out — ra102_lamp is a
        /// container, but nobody approved it as a prefix, so it is not an area.
        /// </summary>
        public static List<string> AreaNames(GameObject prefabRoot, IReadOnlyList<string> approvedPrefixes)
        {
            var names = new List<string>();
            if (prefabRoot == null || approvedPrefixes == null) return names;

            var approved = new HashSet<string>(approvedPrefixes, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Walk(prefabRoot.transform, approved, seen, names);

            names.Sort(string.CompareOrdinal);
            return names;
        }

        static void Walk(Transform t, HashSet<string> approved, HashSet<string> seen, List<string> acc)
        {
            foreach (Transform child in t)
            {
                string name = child.name.Trim();
                if (child.childCount > 0 && approved.Contains(name) && seen.Add(name))
                    acc.Add(name);

                // Keep descending. Areas sit at different depths: Objects/rc holds rooms
                // directly, Objects/ra puts a floor level in between.
                Walk(child, approved, seen, acc);
            }
        }
    }
}
