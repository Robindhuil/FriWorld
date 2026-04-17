using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class GenerateColliders
{
    // Edit these keyword sets to match your naming rules.
    private static readonly string[] MeshColliderKeywords =
    {
        //interior
        "door_frame",
        "wall",
        "outer_wall",
        "big_window",
        "ceiling",
        "fence",
        "roof",
        "sofa",
        "table",
        //exterior
        "curb",
        "pot_tree",
        "rock",
        "tree_bot",
        "sun_block",
        "nav",
    };

    private static readonly string[] BoxColliderKeywords =
    {
        //interior
        "window",
        "glass",
        "door",
        "thick_door",
        "foor",
        "door_slide",
        "parapet",
        "radiator",
        "e_box",
        "pillar",
        "vent",
        "box",
        "couch",
        "counter",
        "counter_bar",
        "chair",
        "desk",
        "board",
        "calcetto",
        "trash_bin",
        "handicap_machine",
        //exterior
        "bycicle_shelter_rack",
        "plant_pot",
        "ventilator_frame",
        "ventilator_floor",
        "ventilator_ladder",
        "billboard",
        "drainage",
        "preform",
        "ramp",
        "trash_container",
        "gazebo_bench",
    };

    private static readonly string[] SphereColliderKeywords = { };

    private static readonly string[] IgnoreKeywords =
    {
        //interior
        "lamp",
        "doorstep",
        "room_sign",
        "thick_door_headlight",
        "sign",
        "poster",
        "e_plug",
        "e_plug_cover",
        "hydrant",
        "construction",
        "bell_thingy",
        "radiator_pipe",
        "drain",
        "support",
        //exterior
        "bush",
        "shrub",
        "tree_top",
    };

    private enum ColliderRule
    {
        None,
        Ignore,
        Mesh,
        Box,
        Sphere,
    }

    private readonly struct RuleMatch
    {
        public readonly ColliderRule Rule;
        public readonly string Keyword;
        public readonly int TokenLength;
        public readonly int CharLength;

        public RuleMatch(ColliderRule rule, string keyword, int tokenLength, int charLength)
        {
            Rule = rule;
            Keyword = keyword;
            TokenLength = tokenLength;
            CharLength = charLength;
        }
    }

    private readonly struct KeywordEntry
    {
        public readonly ColliderRule Rule;
        public readonly string Keyword;
        public readonly List<string> Tokens;

        public KeywordEntry(ColliderRule rule, string keyword, List<string> tokens)
        {
            Rule = rule;
            Keyword = keyword;
            Tokens = tokens;
        }
    }

    [MenuItem("Tools/Colliders/Generate From Name Keywords")]
    private static void GenerateFromSelectedHierarchy()
    {
        GameObject[] selectedRoots = Selection.gameObjects;
        if (selectedRoots == null || selectedRoots.Length == 0)
        {
            Debug.LogWarning("[GenerateColliders] No objects selected in Hierarchy.");
            return;
        }

        int meshAdded = 0;
        int boxAdded = 0;
        int sphereAdded = 0;
        int outdatedRemoved = 0;
        int ignored = 0;
        int alreadyHad = 0;

        List<string> unmatchedNames = new List<string>();
        List<string> ambiguousNames = new List<string>();
        HashSet<Transform> visited = new HashSet<Transform>();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Generate Colliders From Keywords");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject root in selectedRoots)
        {
            if (root == null)
            {
                continue;
            }

            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t == null || visited.Contains(t))
                {
                    continue;
                }

                visited.Add(t);
                GameObject go = t.gameObject;

                RuleMatch resolved = ResolveRuleForName(
                    go.name,
                    out List<RuleMatch> competingMatches
                );
                if (competingMatches.Count > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(GetHierarchyPath(go.transform));
                    sb.Append(" => resolved '");
                    sb.Append(resolved.Rule);
                    sb.Append("' by keyword '");
                    sb.Append(resolved.Keyword);
                    sb.Append("'. Matches: ");

                    for (int i = 0; i < competingMatches.Count; i++)
                    {
                        RuleMatch m = competingMatches[i];
                        sb.Append("[");
                        sb.Append(m.Rule);
                        sb.Append(": ");
                        sb.Append(m.Keyword);
                        sb.Append("]");
                        if (i < competingMatches.Count - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    ambiguousNames.Add(sb.ToString());
                }

                switch (resolved.Rule)
                {
                    case ColliderRule.Ignore:
                        outdatedRemoved += RemoveManagedColliders(go, true, true, true);
                        ignored++;
                        break;

                    case ColliderRule.Mesh:
                        outdatedRemoved += RemoveManagedColliders(go, false, true, true);
                        if (go.GetComponent<MeshCollider>() == null)
                        {
                            Undo.AddComponent<MeshCollider>(go);
                            meshAdded++;
                        }
                        else
                        {
                            alreadyHad++;
                        }
                        break;

                    case ColliderRule.Box:
                        outdatedRemoved += RemoveManagedColliders(go, true, false, true);
                        BoxCollider box = go.GetComponent<BoxCollider>();
                        if (box == null)
                        {
                            box = Undo.AddComponent<BoxCollider>(go);
                            boxAdded++;
                        }
                        else
                        {
                            alreadyHad++;
                        }

                        if (TryGetLocalBounds(go, out Bounds boxBounds))
                        {
                            box.center = boxBounds.center;
                            box.size = boxBounds.size;
                        }
                        break;

                    case ColliderRule.Sphere:
                        outdatedRemoved += RemoveManagedColliders(go, true, true, false);
                        SphereCollider sphere = go.GetComponent<SphereCollider>();
                        if (sphere == null)
                        {
                            sphere = Undo.AddComponent<SphereCollider>(go);
                            sphereAdded++;
                        }
                        else
                        {
                            alreadyHad++;
                        }

                        if (TryGetLocalBounds(go, out Bounds sphereBounds))
                        {
                            sphere.center = sphereBounds.center;
                            sphere.radius = sphereBounds.extents.magnitude;
                        }
                        break;

                    case ColliderRule.None:
                    default:
                        if (HasMeshSource(go))
                        {
                            unmatchedNames.Add(GetHierarchyPath(go.transform));
                        }
                        break;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"[GenerateColliders] Completed. Processed: {visited.Count}, "
                + $"Added Mesh: {meshAdded}, Added Box: {boxAdded}, Added Sphere: {sphereAdded}, "
                + $"Removed Outdated: {outdatedRemoved}, Ignored: {ignored}, "
                + $"Already Had Target Collider: {alreadyHad}, Unmatched: {unmatchedNames.Count}, Ambiguous: {ambiguousNames.Count}"
        );

        if (unmatchedNames.Count > 0)
        {
            Debug.LogWarning(
                "[GenerateColliders] Objects with no matching keyword:\n"
                    + string.Join("\n", unmatchedNames)
            );
        }

        if (ambiguousNames.Count > 0)
        {
            Debug.LogWarning(
                "[GenerateColliders] Objects that matched multiple keyword rules (resolved by specificity and rule priority):\n"
                    + string.Join("\n", ambiguousNames)
            );
        }
    }

    [MenuItem("Tools/Colliders/Generate From Name Keywords", true)]
    private static bool ValidateGenerateFromSelectedHierarchy()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    [MenuItem("Tools/Colliders/Validate Keyword Integrity")]
    private static void ValidateKeywordIntegrity()
    {
        List<KeywordEntry> entries = BuildKeywordEntries();
        List<string> issues = new List<string>();
        List<string> warnings = new List<string>();

        if (entries.Count == 0)
        {
            Debug.LogWarning(
                "[GenerateColliders] Keyword integrity check: no valid keywords configured."
            );
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            KeywordEntry a = entries[i];
            for (int j = i + 1; j < entries.Count; j++)
            {
                KeywordEntry b = entries[j];

                if (AreTokensEqual(a.Tokens, b.Tokens))
                {
                    if (a.Rule == b.Rule)
                    {
                        issues.Add(
                            $"Duplicate keyword in {a.Rule}: '{a.Keyword}' and '{b.Keyword}'"
                        );
                    }
                    else
                    {
                        ColliderRule winner = IsBetterMatch(
                            new RuleMatch(a.Rule, a.Keyword, a.Tokens.Count, a.Keyword.Length),
                            new RuleMatch(b.Rule, b.Keyword, b.Tokens.Count, b.Keyword.Length)
                        )
                            ? a.Rule
                            : b.Rule;

                        issues.Add(
                            $"Conflicting exact keywords: '{a.Keyword}' ({a.Rule}) vs '{b.Keyword}' ({b.Rule}). "
                                + $"Objects matching this token sequence always resolve to {winner}."
                        );
                    }

                    continue;
                }

                if (
                    ContainsTokenSequence(a.Tokens, b.Tokens)
                    || ContainsTokenSequence(b.Tokens, a.Tokens)
                )
                {
                    warnings.Add(
                        $"Potential overlap: '{a.Keyword}' ({a.Rule}) and '{b.Keyword}' ({b.Rule}). "
                            + "Ignore always wins when present; otherwise longer/more specific keyword wins."
                    );
                }
            }
        }

        Debug.Log(
            $"[GenerateColliders] Keyword integrity check done. "
                + $"Keywords: {entries.Count}, Issues: {issues.Count}, Overlap warnings: {warnings.Count}."
        );

        if (issues.Count > 0)
        {
            Debug.LogError("[GenerateColliders] Integrity issues:\n" + string.Join("\n", issues));
        }

        if (warnings.Count > 0)
        {
            Debug.LogWarning(
                "[GenerateColliders] Integrity overlap warnings:\n" + string.Join("\n", warnings)
            );
        }

        if (issues.Count == 0 && warnings.Count == 0)
        {
            Debug.Log("[GenerateColliders] Integrity check passed: no conflicts found.");
        }
    }

    private static RuleMatch ResolveRuleForName(string objectName, out List<RuleMatch> allMatches)
    {
        allMatches = CollectMatches(objectName);
        if (allMatches.Count == 0)
        {
            return new RuleMatch(ColliderRule.None, string.Empty, 0, 0);
        }

        // Hard safety rule: any ignore match overrides all collider assignment matches.
        bool hasIgnoreMatch = false;
        RuleMatch bestIgnore = new RuleMatch(ColliderRule.None, string.Empty, 0, 0);
        for (int i = 0; i < allMatches.Count; i++)
        {
            RuleMatch m = allMatches[i];
            if (m.Rule != ColliderRule.Ignore)
            {
                continue;
            }

            if (!hasIgnoreMatch || IsBetterMatch(m, bestIgnore))
            {
                bestIgnore = m;
                hasIgnoreMatch = true;
            }
        }

        if (hasIgnoreMatch)
        {
            return bestIgnore;
        }

        RuleMatch best = allMatches[0];
        for (int i = 1; i < allMatches.Count; i++)
        {
            RuleMatch candidate = allMatches[i];
            if (IsBetterMatch(candidate, best))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static List<RuleMatch> CollectMatches(string objectName)
    {
        List<RuleMatch> matches = new List<RuleMatch>();
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return matches;
        }

        List<string> objectTokens = Tokenize(objectName);
        TryAddMatches(matches, objectTokens, IgnoreKeywords, ColliderRule.Ignore);
        TryAddMatches(matches, objectTokens, MeshColliderKeywords, ColliderRule.Mesh);
        TryAddMatches(matches, objectTokens, BoxColliderKeywords, ColliderRule.Box);
        TryAddMatches(matches, objectTokens, SphereColliderKeywords, ColliderRule.Sphere);

        return matches;
    }

    private static void TryAddMatches(
        List<RuleMatch> output,
        List<string> objectTokens,
        string[] keywords,
        ColliderRule rule
    )
    {
        if (keywords == null || keywords.Length == 0)
        {
            return;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            List<string> keywordTokens = Tokenize(keyword);
            if (keywordTokens.Count == 0)
            {
                continue;
            }

            if (ContainsTokenSequence(objectTokens, keywordTokens))
            {
                output.Add(new RuleMatch(rule, keyword, keywordTokens.Count, keyword.Length));
            }
        }
    }

    private static bool IsBetterMatch(RuleMatch candidate, RuleMatch current)
    {
        if (candidate.TokenLength != current.TokenLength)
        {
            return candidate.TokenLength > current.TokenLength;
        }

        if (candidate.CharLength != current.CharLength)
        {
            return candidate.CharLength > current.CharLength;
        }

        int candidatePriority = GetRulePriority(candidate.Rule);
        int currentPriority = GetRulePriority(current.Rule);
        if (candidatePriority != currentPriority)
        {
            return candidatePriority < currentPriority;
        }

        return false;
    }

    private static int GetRulePriority(ColliderRule rule)
    {
        switch (rule)
        {
            case ColliderRule.Ignore:
                return 0;
            case ColliderRule.Mesh:
                return 1;
            case ColliderRule.Box:
                return 2;
            case ColliderRule.Sphere:
                return 3;
            default:
                return 99;
        }
    }

    private static bool ContainsTokenSequence(List<string> haystack, List<string> needle)
    {
        if (needle.Count > haystack.Count)
        {
            return false;
        }

        for (int i = 0; i <= haystack.Count - needle.Count; i++)
        {
            bool allEqual = true;
            for (int j = 0; j < needle.Count; j++)
            {
                if (
                    !string.Equals(
                        haystack[i + j],
                        needle[j],
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    allEqual = false;
                    break;
                }
            }

            if (allEqual)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreTokensEqual(List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static List<KeywordEntry> BuildKeywordEntries()
    {
        List<KeywordEntry> entries = new List<KeywordEntry>();
        AddKeywordEntries(entries, IgnoreKeywords, ColliderRule.Ignore);
        AddKeywordEntries(entries, MeshColliderKeywords, ColliderRule.Mesh);
        AddKeywordEntries(entries, BoxColliderKeywords, ColliderRule.Box);
        AddKeywordEntries(entries, SphereColliderKeywords, ColliderRule.Sphere);
        return entries;
    }

    private static void AddKeywordEntries(
        List<KeywordEntry> entries,
        string[] keywords,
        ColliderRule rule
    )
    {
        if (keywords == null)
        {
            return;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            List<string> tokens = Tokenize(keyword);
            if (tokens.Count == 0)
            {
                continue;
            }

            entries.Add(new KeywordEntry(rule, keyword, tokens));
        }
    }

    private static List<string> Tokenize(string input)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return tokens;
        }

        StringBuilder tokenBuilder = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsLetterOrDigit(c))
            {
                tokenBuilder.Append(char.ToLowerInvariant(c));
            }
            else if (tokenBuilder.Length > 0)
            {
                tokens.Add(tokenBuilder.ToString());
                tokenBuilder.Clear();
            }
        }

        if (tokenBuilder.Length > 0)
        {
            tokens.Add(tokenBuilder.ToString());
        }

        return tokens;
    }

    private static int RemoveManagedColliders(
        GameObject go,
        bool removeMesh,
        bool removeBox,
        bool removeSphere
    )
    {
        int removedCount = 0;

        if (removeMesh)
        {
            MeshCollider mesh = go.GetComponent<MeshCollider>();
            if (mesh != null)
            {
                Undo.DestroyObjectImmediate(mesh);
                removedCount++;
            }
        }

        if (removeBox)
        {
            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                Undo.DestroyObjectImmediate(box);
                removedCount++;
            }
        }

        if (removeSphere)
        {
            SphereCollider sphere = go.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Undo.DestroyObjectImmediate(sphere);
                removedCount++;
            }
        }

        return removedCount;
    }

    private static bool HasMeshSource(GameObject go)
    {
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return true;
        }

        SkinnedMeshRenderer skinned = go.GetComponent<SkinnedMeshRenderer>();
        if (skinned != null && skinned.sharedMesh != null)
        {
            return true;
        }

        return false;
    }

    private static bool TryGetLocalBounds(GameObject go, out Bounds localBounds)
    {
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            localBounds = meshFilter.sharedMesh.bounds;
            return true;
        }

        SkinnedMeshRenderer skinned = go.GetComponent<SkinnedMeshRenderer>();
        if (skinned != null)
        {
            localBounds = skinned.localBounds;
            return true;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            localBounds = WorldBoundsToLocalBounds(go.transform, renderer.bounds);
            return true;
        }

        localBounds = default;
        return false;
    }

    private static Bounds WorldBoundsToLocalBounds(Transform target, Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 ext = worldBounds.extents;

        Vector3[] worldCorners =
        {
            center + new Vector3(ext.x, ext.y, ext.z),
            center + new Vector3(ext.x, ext.y, -ext.z),
            center + new Vector3(ext.x, -ext.y, ext.z),
            center + new Vector3(ext.x, -ext.y, -ext.z),
            center + new Vector3(-ext.x, ext.y, ext.z),
            center + new Vector3(-ext.x, ext.y, -ext.z),
            center + new Vector3(-ext.x, -ext.y, ext.z),
            center + new Vector3(-ext.x, -ext.y, -ext.z),
        };

        Vector3 localMin = new Vector3(
            float.PositiveInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity
        );
        Vector3 localMax = new Vector3(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.NegativeInfinity
        );

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 local = target.InverseTransformPoint(worldCorners[i]);
            localMin = Vector3.Min(localMin, local);
            localMax = Vector3.Max(localMax, local);
        }

        Bounds result = new Bounds();
        result.SetMinMax(localMin, localMax);
        return result;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null)
        {
            return "<null>";
        }

        string path = t.name;
        Transform current = t.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
