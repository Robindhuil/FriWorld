using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

public static class GenerateLayersAndStatic
{
    // Edit these keyword sets to match your naming rules.
    // Priority: Interactable > NoObstacle > Obstacle > Nav > default no static change to layers.
    private static readonly string[] InteractableKeywords = { "door", "door_slide" };

    private static readonly string[] ObstacleKeywords =
    {
        //interior
        "door_frame",
        "thick_door",
        "foor",
        "wall",
        "outer_wall",
        "big_window",
        "fence",
        "sofa",
        "window",
        "pillar",
        "couch",
        "counter",
        "desk",
        "trash_bin",
        //exterior
        "pot_tree",
        "rock",
        "tree_bot",
        "plant_pot",
        "ventilator_frame",
        "ventilator_floor",
        "ventilator_ladder",
        "billboard",
        "preform",
        "ramp",
        "trash_container",
        "sun_block",
    };

    private static readonly string[] NoObstacleKeywords =
    {
        //interior
        "lamp",
        "doorstep",
        "thick_door_headlight",
        "room_sign",
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
        "ceiling",
        "roof",
        "table",
        "parapet",
        "radiator",
        "e_box",
        "vent",
        "box",
        "counter_bar",
        "chair",
        "board",
        "bycicle_shelter_rack",
        "gazebo_bench",
        "glass",
        "handicap_machine",
        "calcetto",
        //exterior
        "bush",
        "shrub",
        "tree_top",
    };

    private static readonly string[] NavKeywords = { "nav", "drainage", "curb" };

    private const string InteractableLayerName = "Interactable";
    private const string ObstacleLayerName = "Obstacle";
    private const string NoObstacleLayerName = "NoObstacle";
    private const string NavLayerName = "Nav";
    private const string DoorTagName = "Door";
    private const string UntaggedTagName = "Untagged";
    private const string UnoOverrideKeyword = "UNO";
    private const string UyoOverrideKeyword = "UYO";

    private enum LayerRule
    {
        None,
        Interactable,
        Obstacle,
        NoObstacle,
        Nav,
    }

    private readonly struct RuleMatch
    {
        public readonly LayerRule Rule;
        public readonly string Keyword;
        public readonly int TokenLength;
        public readonly int CharLength;

        public RuleMatch(LayerRule rule, string keyword, int tokenLength, int charLength)
        {
            Rule = rule;
            Keyword = keyword;
            TokenLength = tokenLength;
            CharLength = charLength;
        }
    }

    [MenuItem("Tools/Layers/Assign Layers And Static From Keywords")]
    private static void AssignLayersAndStaticFromSelectedHierarchy()
    {
        if (!TryGetRequiredLayers(out Dictionary<string, int> layerMap))
        {
            return;
        }

        GameObject[] selectedRoots = Selection.gameObjects;
        if (selectedRoots == null || selectedRoots.Length == 0)
        {
            Debug.LogWarning("[GenerateLayersAndStatic] No objects selected in Hierarchy.");
            return;
        }

        int interactableAssigned = 0;
        int obstacleAssigned = 0;
        int noObstacleAssigned = 0;
        int navAssigned = 0;
        int staticAssigned = 0;

        int layerChanged = 0;
        int staticChanged = 0;
        int tagChanged = 0;
        int navMeshModifierAdded = 0;
        int navMeshModifierConfigured = 0;
        int unchanged = 0;

        bool canAssignDoorTag = IsTagDefined(DoorTagName);
        if (!canAssignDoorTag)
        {
            Debug.LogWarning(
                "[GenerateLayersAndStatic] Tag 'Door' is not defined in Project Settings > Tags and Layers. Door tag assignment/removal will be skipped."
            );
        }

        List<string> ambiguousNames = new List<string>();
        HashSet<Transform> visited = new HashSet<Transform>();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Assign Layers And Static From Keywords");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject root in selectedRoots)
        {
            if (root == null)
            {
                continue;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null || visited.Contains(t))
                {
                    continue;
                }

                visited.Add(t);
                GameObject go = t.gameObject;

                RuleMatch resolved = ResolveRuleForName(go.name, out List<RuleMatch> allMatches);
                if (allMatches.Count > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(GetHierarchyPath(go.transform));
                    sb.Append(" => resolved '");
                    sb.Append(resolved.Rule);
                    sb.Append("' by keyword '");
                    sb.Append(resolved.Keyword);
                    sb.Append("'. Matches: ");

                    for (int m = 0; m < allMatches.Count; m++)
                    {
                        RuleMatch match = allMatches[m];
                        sb.Append("[");
                        sb.Append(match.Rule);
                        sb.Append(": ");
                        sb.Append(match.Keyword);
                        sb.Append("]");
                        if (m < allMatches.Count - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    ambiguousNames.Add(sb.ToString());
                }

                int targetLayer;
                bool targetStatic;
                switch (resolved.Rule)
                {
                    case LayerRule.Interactable:
                        targetLayer = layerMap[InteractableLayerName];
                        targetStatic = false;
                        interactableAssigned++;
                        break;

                    case LayerRule.Obstacle:
                        targetLayer = layerMap[ObstacleLayerName];
                        targetStatic = true;
                        obstacleAssigned++;
                        break;

                    case LayerRule.NoObstacle:
                        targetLayer = layerMap[NoObstacleLayerName];
                        targetStatic = true;
                        noObstacleAssigned++;
                        break;

                    case LayerRule.Nav:
                        targetLayer = layerMap[NavLayerName];
                        targetStatic = true;
                        navAssigned++;
                        break;

                    case LayerRule.None:
                    default:
                        targetLayer = go.layer;
                        targetStatic = false;
                        staticAssigned++;
                        break;
                }

                bool modifierAdded = false;
                bool modifierConfigured = false;
                if (resolved.Rule == LayerRule.Obstacle)
                {
                    EnsureObstacleNavMeshModifier(go, out modifierAdded, out modifierConfigured);
                    if (modifierAdded)
                    {
                        navMeshModifierAdded++;
                    }

                    if (modifierConfigured)
                    {
                        navMeshModifierConfigured++;
                    }
                }

                bool shouldHaveDoorTag = ShouldHaveDoorTag(resolved);
                bool requiresTagUpdate = false;
                string nextTag = go.tag;
                if (canAssignDoorTag)
                {
                    bool hasDoorTagNow = string.Equals(
                        go.tag,
                        DoorTagName,
                        System.StringComparison.Ordinal
                    );

                    if (shouldHaveDoorTag)
                    {
                        if (!hasDoorTagNow)
                        {
                            requiresTagUpdate = true;
                            nextTag = DoorTagName;
                        }
                    }
                    else if (hasDoorTagNow)
                    {
                        requiresTagUpdate = true;
                        nextTag = UntaggedTagName;
                    }
                }

                bool hasChange =
                    go.layer != targetLayer
                    || go.isStatic != targetStatic
                    || modifierAdded
                    || modifierConfigured
                    || requiresTagUpdate;
                if (!hasChange)
                {
                    unchanged++;
                    continue;
                }

                Undo.RecordObject(go, "Assign Layer/Static");

                if (go.layer != targetLayer)
                {
                    go.layer = targetLayer;
                    layerChanged++;
                }

                if (go.isStatic != targetStatic)
                {
                    go.isStatic = targetStatic;
                    staticChanged++;
                }

                if (requiresTagUpdate)
                {
                    go.tag = nextTag;
                    tagChanged++;
                }

                EditorUtility.SetDirty(go);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"[GenerateLayersAndStatic] Completed. Processed: {visited.Count}, "
                + $"Interactable: {interactableAssigned}, Obstacle: {obstacleAssigned}, "
                + $"NoObstacle: {noObstacleAssigned}, Nav: {navAssigned}, DefaultStatic: {staticAssigned}, "
                + $"ObstacleNavMeshModifierAdded: {navMeshModifierAdded}, "
                + $"ObstacleNavMeshModifierConfigured: {navMeshModifierConfigured}, "
                + $"LayerChanged: {layerChanged}, StaticChanged: {staticChanged}, TagChanged: {tagChanged}, Unchanged: {unchanged}, "
                + $"Ambiguous: {ambiguousNames.Count}"
        );

        if (ambiguousNames.Count > 0)
        {
            Debug.LogWarning(
                "[GenerateLayersAndStatic] Objects matched multiple keyword sets (resolved by specificity + rule priority):\n"
                    + string.Join("\n", ambiguousNames)
            );
        }
    }

    [MenuItem("Tools/Layers/Assign Layers And Static From Keywords", true)]
    private static bool ValidateAssignLayersAndStaticFromSelectedHierarchy()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    [MenuItem("Tools/Layers/Validate Layer Keyword Integrity")]
    private static void ValidateKeywordIntegrity()
    {
        List<RuleMatch> entries = BuildKeywordEntries();
        List<string> issues = new List<string>();
        List<string> warnings = new List<string>();

        if (entries.Count == 0)
        {
            Debug.LogWarning(
                "[GenerateLayersAndStatic] Integrity check: no valid keywords configured."
            );
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RuleMatch a = entries[i];
            for (int j = i + 1; j < entries.Count; j++)
            {
                RuleMatch b = entries[j];

                List<string> aTokens = Tokenize(a.Keyword);
                List<string> bTokens = Tokenize(b.Keyword);

                if (AreTokensEqual(aTokens, bTokens))
                {
                    if (a.Rule == b.Rule)
                    {
                        issues.Add(
                            $"Duplicate keyword in {a.Rule}: '{a.Keyword}' and '{b.Keyword}'"
                        );
                    }
                    else
                    {
                        LayerRule winner = IsBetterMatch(a, b) ? a.Rule : b.Rule;
                        issues.Add(
                            $"Conflicting exact keywords: '{a.Keyword}' ({a.Rule}) vs '{b.Keyword}' ({b.Rule}). "
                                + $"Objects matching this token sequence always resolve to {winner}."
                        );
                    }

                    continue;
                }

                if (
                    ContainsTokenSequence(aTokens, bTokens)
                    || ContainsTokenSequence(bTokens, aTokens)
                )
                {
                    warnings.Add(
                        $"Potential overlap: '{a.Keyword}' ({a.Rule}) and '{b.Keyword}' ({b.Rule}). "
                            + "More specific keyword wins; if equal specificity, priority is Interactable > NoObstacle > Obstacle > Nav."
                    );
                }
            }
        }

        Debug.Log(
            $"[GenerateLayersAndStatic] Integrity check done. "
                + $"Keywords: {entries.Count}, Issues: {issues.Count}, Overlap warnings: {warnings.Count}."
        );

        if (issues.Count > 0)
        {
            Debug.LogError(
                "[GenerateLayersAndStatic] Integrity issues:\n" + string.Join("\n", issues)
            );
        }

        if (warnings.Count > 0)
        {
            Debug.LogWarning(
                "[GenerateLayersAndStatic] Integrity overlap warnings:\n"
                    + string.Join("\n", warnings)
            );
        }

        if (issues.Count == 0 && warnings.Count == 0)
        {
            Debug.Log("[GenerateLayersAndStatic] Integrity check passed: no conflicts found.");
        }
    }

    private static bool TryGetRequiredLayers(out Dictionary<string, int> layerMap)
    {
        layerMap = new Dictionary<string, int>();
        string[] required =
        {
            InteractableLayerName,
            ObstacleLayerName,
            NoObstacleLayerName,
            NavLayerName,
        };

        List<string> missing = new List<string>();
        for (int i = 0; i < required.Length; i++)
        {
            string layer = required[i];
            int id = LayerMask.NameToLayer(layer);
            if (id < 0)
            {
                missing.Add(layer);
                continue;
            }

            layerMap[layer] = id;
        }

        if (missing.Count > 0)
        {
            Debug.LogError(
                "[GenerateLayersAndStatic] Missing layers in Project Settings > Tags and Layers:\n"
                    + string.Join("\n", missing)
            );
            return false;
        }

        return true;
    }

    private static RuleMatch ResolveRuleForName(string objectName, out List<RuleMatch> allMatches)
    {
        List<string> objectTokens = Tokenize(objectName);
        if (TryGetOverrideMatch(objectName, out RuleMatch overrideMatch))
        {
            allMatches = new List<RuleMatch> { overrideMatch };
            return overrideMatch;
        }

        allMatches = CollectMatches(objectTokens);
        if (allMatches.Count == 0)
        {
            return new RuleMatch(LayerRule.None, string.Empty, 0, 0);
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

    private static bool TryGetOverrideMatch(string objectName, out RuleMatch overrideMatch)
    {
        overrideMatch = new RuleMatch(LayerRule.None, string.Empty, 0, 0);
        List<string> caseSensitiveTokens = TokenizeCaseSensitive(objectName);
        if (caseSensitiveTokens.Count == 0)
        {
            return false;
        }

        bool hasUno = false;
        bool hasUyo = false;

        for (int i = 0; i < caseSensitiveTokens.Count; i++)
        {
            string token = caseSensitiveTokens[i];
            if (token == UnoOverrideKeyword)
            {
                hasUno = true;
            }
            else if (token == UyoOverrideKeyword)
            {
                hasUyo = true;
            }
        }

        if (hasUno && hasUyo)
        {
            // If both override tokens are present, UYO wins as an explicit obstacle force.
            overrideMatch = new RuleMatch(
                LayerRule.Obstacle,
                UyoOverrideKeyword,
                1,
                UyoOverrideKeyword.Length
            );
            return true;
        }

        if (hasUyo)
        {
            overrideMatch = new RuleMatch(
                LayerRule.Obstacle,
                UyoOverrideKeyword,
                1,
                UyoOverrideKeyword.Length
            );
            return true;
        }

        if (hasUno)
        {
            overrideMatch = new RuleMatch(
                LayerRule.NoObstacle,
                UnoOverrideKeyword,
                1,
                UnoOverrideKeyword.Length
            );
            return true;
        }

        return false;
    }

    private static List<string> TokenizeCaseSensitive(string objectNameForOverrideScan)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(objectNameForOverrideScan))
        {
            return tokens;
        }

        StringBuilder tokenBuilder = new StringBuilder();
        for (int i = 0; i < objectNameForOverrideScan.Length; i++)
        {
            char c = objectNameForOverrideScan[i];
            if (char.IsLetterOrDigit(c))
            {
                tokenBuilder.Append(c);
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

    private static List<RuleMatch> BuildKeywordEntries()
    {
        List<RuleMatch> entries = new List<RuleMatch>();
        AddKeywordEntries(entries, InteractableKeywords, LayerRule.Interactable);
        AddKeywordEntries(entries, ObstacleKeywords, LayerRule.Obstacle);
        AddKeywordEntries(entries, NoObstacleKeywords, LayerRule.NoObstacle);
        AddKeywordEntries(entries, NavKeywords, LayerRule.Nav);
        return entries;
    }

    private static void AddKeywordEntries(List<RuleMatch> output, string[] keywords, LayerRule rule)
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

            List<string> keywordTokens = Tokenize(keyword);
            if (keywordTokens.Count == 0)
            {
                continue;
            }

            output.Add(new RuleMatch(rule, keyword, keywordTokens.Count, keyword.Length));
        }
    }

    private static List<RuleMatch> CollectMatches(List<string> objectTokens)
    {
        List<RuleMatch> matches = new List<RuleMatch>();
        if (objectTokens == null || objectTokens.Count == 0)
        {
            return matches;
        }

        TryAddMatches(matches, objectTokens, InteractableKeywords, LayerRule.Interactable);
        TryAddMatches(matches, objectTokens, ObstacleKeywords, LayerRule.Obstacle);
        TryAddMatches(matches, objectTokens, NoObstacleKeywords, LayerRule.NoObstacle);
        TryAddMatches(matches, objectTokens, NavKeywords, LayerRule.Nav);

        return matches;
    }

    private static void TryAddMatches(
        List<RuleMatch> output,
        List<string> objectTokens,
        string[] keywords,
        LayerRule rule
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

    private static int GetRulePriority(LayerRule rule)
    {
        switch (rule)
        {
            case LayerRule.Interactable:
                return 0;
            case LayerRule.NoObstacle:
                return 1;
            case LayerRule.Obstacle:
                return 2;
            case LayerRule.Nav:
                return 3;
            default:
                return 99;
        }
    }

    private static void EnsureObstacleNavMeshModifier(
        GameObject go,
        out bool modifierAdded,
        out bool modifierConfigured
    )
    {
        modifierAdded = false;
        modifierConfigured = false;

        NavMeshModifier modifier = go.GetComponent<NavMeshModifier>();
        if (modifier == null)
        {
            modifier = Undo.AddComponent<NavMeshModifier>(go);
            modifierAdded = true;
        }

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
        if (notWalkableArea < 0)
        {
            notWalkableArea = 1;
        }

        bool requiresPropertyUpdate = !modifier.overrideArea || modifier.area != notWalkableArea;

        SerializedObject serializedModifier = new SerializedObject(modifier);
        SerializedProperty affectedAgents = serializedModifier.FindProperty("m_AffectedAgents");

        bool requiresAffectedAgentsUpdate = true;
        if (affectedAgents != null && affectedAgents.isArray)
        {
            bool hasSingleAllAgent =
                affectedAgents.arraySize == 1
                && affectedAgents.GetArrayElementAtIndex(0).intValue == -1;
            requiresAffectedAgentsUpdate = !hasSingleAllAgent;
        }

        if (!requiresPropertyUpdate && !requiresAffectedAgentsUpdate)
        {
            return;
        }

        Undo.RecordObject(modifier, "Configure Obstacle NavMesh Modifier");

        if (requiresPropertyUpdate)
        {
            modifier.overrideArea = true;
            modifier.area = notWalkableArea;
        }

        if (requiresAffectedAgentsUpdate && affectedAgents != null && affectedAgents.isArray)
        {
            affectedAgents.arraySize = 1;
            affectedAgents.GetArrayElementAtIndex(0).intValue = -1;
            serializedModifier.ApplyModifiedProperties();
        }

        modifierConfigured = true;
        EditorUtility.SetDirty(modifier);
    }

    private static bool ShouldHaveDoorTag(RuleMatch resolved)
    {
        if (resolved.Rule != LayerRule.Interactable)
        {
            return false;
        }

        return string.Equals(resolved.Keyword, "door", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                resolved.Keyword,
                "door_slide",
                System.StringComparison.OrdinalIgnoreCase
            );
    }

    private static bool IsTagDefined(string tag)
    {
        string[] tags = InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (string.Equals(tags[i], tag, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
