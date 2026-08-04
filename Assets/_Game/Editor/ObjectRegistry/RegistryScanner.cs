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
        /// <summary>Container names that could serve as prefixes. Proposals only — never applied.</summary>
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
        /// a multi-word type ("wall_edge"), which no automatic guard can catch.
        /// </summary>
        public static ScanResult Scan(GameObject root, IReadOnlyList<string> approvedPrefixes)
        {
            var result = new ScanResult();
            if (root == null) return result;

            var containers = new HashSet<string>();
            var keys = new HashSet<string>();

            void Walk(Transform t)
            {
                if (t.childCount > 0) containers.Add(StripTrailingInt(t.name));

                if (t.GetComponent<MeshRenderer>() != null)
                {
                    string key = ObjectTypeKey.Derive(t.name, approvedPrefixes);
                    keys.Add(key);
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
                result.proposedPrefixes.Add(c);
                if (keys.Contains(c)) result.riskyPrefixes.Add(c);
            }
            result.proposedPrefixes.Sort(string.CompareOrdinal);
            result.riskyPrefixes.Sort(string.CompareOrdinal);
            return result;
        }

        public static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
            return string.Join("/", parts.ToArray());
        }

        static string StripTrailingInt(string s)
        {
            int u = s.LastIndexOf('_');
            if (u <= 0) return s;
            string tail = s.Substring(u + 1);
            if (tail.Length == 0) return s;
            foreach (char c in tail) if (!char.IsDigit(c)) return s;
            return s.Substring(0, u);
        }
    }
}
