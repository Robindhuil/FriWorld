using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.ObjectRegistry
{
    /// <summary>One mesh object found by the scan, with the key it derived to.</summary>
    public struct ScannedObject
    {
        public GameObject gameObject;
        public string typeKey;
        public string path;
    }

    public class ScanResult
    {
        public readonly List<ScannedObject> objects = new List<ScannedObject>();
        /// <summary>
        /// Container names that would actually shorten at least one descendant's name. Container
        /// names that prefix nothing are left out — they would only pad the file.
        ///
        /// Names are proposed WHOLE, instance number included: ra100_corridor_1 and
        /// ra100_corridor_2 are separate rows. They are separate places, and collapsing them into
        /// one ra100_corridor row would force one decision on both. The type key is unaffected
        /// either way, because ObjectTypeKey removes whichever instance number is left over.
        /// </summary>
        public readonly List<string> proposedPrefixes = new List<string>();
        /// <summary>Proposed prefixes that are also a derived type key. Applying one would eat a type.</summary>
        public readonly List<string> riskyPrefixes = new List<string>();
    }

    public static class RegistryScanner
    {
        /// <summary>
        /// Walks the subtree and derives a type key for every mesh object, using the approved
        /// prefix list only. Container names are collected separately as proposals for the
        /// human to review — a prefix equal to a type word ("wall") would silently eat part of
        /// a multi-word type ("wall_edge").
        ///
        /// Two guards catch that. This one flags a proposal that IS a derived key; the second,
        /// in ObjectRegistryMenu, flags one that merely STARTS a registered type name. Neither
        /// removes anything — both withhold and report, because the call is the human's.
        /// </summary>
        public static ScanResult Scan(GameObject root, IReadOnlyList<string> approvedPrefixes)
        {
            var result = new ScanResult();
            if (root == null) return result;

            var containers = new HashSet<string>();
            var leafNames = new List<string>();
            var keys = new HashSet<string>();

            void Walk(Transform t)
            {
                if (t.childCount > 0) containers.Add(t.name.Trim());

                if (t.GetComponent<MeshRenderer>() != null)
                {
                    string key = ObjectTypeKey.Derive(t.name, approvedPrefixes);
                    keys.Add(key);
                    leafNames.Add(t.name);
                    result.objects.Add(new ScannedObject
                    {
                        gameObject = t.gameObject,
                        typeKey = key,
                        path = PathOf(t),
                    });
                }

                foreach (Transform child in t) Walk(child);
            }

            Walk(root.transform);

            foreach (var c in containers)
            {
                if (string.IsNullOrEmpty(c)) continue;
                if (!PrefixesAnyLeaf(c, leafNames)) continue;   // would change nothing

                result.proposedPrefixes.Add(c);
                if (keys.Contains(c)) result.riskyPrefixes.Add(c);
            }
            result.proposedPrefixes.Sort(string.CompareOrdinal);
            result.riskyPrefixes.Sort(string.CompareOrdinal);
            return result;
        }

        /// <summary>
        /// True when this container name would actually shorten some leaf's name. Without this
        /// check the proposal list carries every container in the subtree, most of which prefix
        /// nothing — on FriBuilding/Objects that is 741 names instead of the ones that matter.
        /// </summary>
        static bool PrefixesAnyLeaf(string container, List<string> leafNames)
        {
            string withSeparator = container + "_";
            for (int i = 0; i < leafNames.Count; i++)
                if (leafNames[i].Length > withSeparator.Length
                    && leafNames[i].StartsWith(withSeparator, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts.ToArray());
        }
    }
}
