using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One area container found in the prefab, paired with the decision that applies.</summary>
    public struct AreaMatch
    {
        public Transform transform;

        /// <summary>Full container name, trimmed. The key into RoomPlatforms.json.</summary>
        public string area;

        /// <summary>null when the area is missing from the file or still undecided.</summary>
        public string platform;
    }

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
        public const string ObjectsBranch = "Objects";
        public const string BuildingBranch = "fri_building";

        /// <summary>Opens the prefab in an isolated preview scene. Always pair with Close.</summary>
        public static GameObject Open() => PrefabUtility.LoadPrefabContents(PrefabPath);

        public static void Close(GameObject contents) => PrefabUtility.UnloadPrefabContents(contents);

        public static void SaveAndClose(GameObject contents)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        public static Transform Branch(GameObject prefabRoot, string branchName)
            => prefabRoot == null ? null : prefabRoot.transform.Find(branchName);

        /// <summary>
        /// The area containers in a subtree, each with its decision.
        ///
        /// An area is a container whose name IS an approved prefix — exact match, no stripping.
        /// The scanner proposes container names whole, so ra100_corridor_1 and ra100_corridor_2
        /// are two prefixes and two areas that decide separately. The approved list is also what
        /// keeps furniture out: ra102_lamp is a container, but nobody approved it as a prefix.
        /// </summary>
        public static List<AreaMatch> Match(Transform branchRoot, IReadOnlyList<string> approvedPrefixes,
                                            RoomPlatforms platforms)
        {
            var matches = new List<AreaMatch>();
            if (branchRoot == null || approvedPrefixes == null) return matches;

            var approved = new HashSet<string>(approvedPrefixes, StringComparer.OrdinalIgnoreCase);
            Walk(branchRoot, approved, platforms, matches);
            return matches;
        }

        /// <summary>Distinct area names in the whole prefab. This is what Reconcile consumes.</summary>
        public static List<string> AreaNames(GameObject prefabRoot, IReadOnlyList<string> approvedPrefixes)
        {
            var names = new List<string>();
            if (prefabRoot == null) return names;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var match in Match(prefabRoot.transform, approvedPrefixes, null))
                if (seen.Add(match.area)) names.Add(match.area);

            names.Sort(string.CompareOrdinal);
            return names;
        }

        /// <summary>
        /// True when another area sits inside this one. Gating such a container would strip its
        /// inner areas too and void their own decisions without saying so, which is why the
        /// appliers refuse. Today this catches rb, terrace, rb_basement and rc000_cafeteria —
        /// every one of them a container of other areas.
        /// </summary>
        public static bool ContainsAnotherArea(AreaMatch outer, List<AreaMatch> all)
        {
            foreach (var other in all)
            {
                if (other.transform == null || other.transform == outer.transform) continue;
                if (other.transform.IsChildOf(outer.transform)) return true;
            }
            return false;
        }

        static void Walk(Transform t, HashSet<string> approved, RoomPlatforms platforms,
                         List<AreaMatch> acc)
        {
            foreach (Transform child in t)
            {
                string name = child.name.Trim();
                if (child.childCount > 0 && approved.Contains(name))
                {
                    acc.Add(new AreaMatch
                    {
                        transform = child,
                        area = name,
                        platform = platforms == null ? null : platforms.PlatformOf(name),
                    });
                }

                // Keep descending. Areas sit at different depths: Objects/rc holds rooms
                // directly, Objects/ra puts a floor level in between. An area inside an area is
                // legitimate too — ContainsAnotherArea is what stops it being gated.
                Walk(child, approved, platforms, acc);
            }
        }
    }
}
