using System;
using System.Collections.Generic;
using System.Text;
using FriWorld.ObjectRegistry;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Assigns layers, static flags, shadow casting and the Door tag from the object type registry.
///
/// The layer comes from <c>ObjectTypes.json</c>, looked up by the type key derived from the
/// object's name. Static flags and the Door tag are derived from that layer unless the entry
/// overrides them. An object whose type is unknown or undecided is left exactly as it is and
/// reported — it never inherits a similarly named type's behaviour.
///
/// Shadow casting is not in the registry, for the same reason Occluder Static is not: a name
/// cannot tell you whether a surface is see-through, so both are decided from the materials.
/// A type key cannot express it either — <c>window_&lt;int&gt;_glass</c> covers both the glazing
/// and the solid parapet under it, which are separate renderers of the same type.
/// It belongs in this pass rather than a tool of its own because this pass writes into
/// <c>FriBuilding.prefab</c>; anything that writes shadow casting onto the scene instance is a
/// prefab override and the next run of this pipeline throws it away.
///
/// UNO / UYO in an object's name still win over the registry: they are per-instance exceptions
/// that a type-level registry cannot express.
/// </summary>
public static class GenerateLayersAndStatic
{
    private static readonly StaticEditorFlags AllStaticFlags =
        StaticEditorFlags.ContributeGI
        | StaticEditorFlags.OccluderStatic
        | StaticEditorFlags.OccludeeStatic
        | StaticEditorFlags.BatchingStatic
        | StaticEditorFlags.NavigationStatic
        | StaticEditorFlags.OffMeshLinkGeneration
        | StaticEditorFlags.ReflectionProbeStatic;

    private static readonly StaticEditorFlags OccludeeOnlyFlags =
        AllStaticFlags & ~StaticEditorFlags.OccluderStatic;

    // Materials at or past this queue are alpha-tested / transparent. Anything you can see
    // through must never be an occluder: Umbra would then cull the geometry behind it, which
    // the player can plainly see. This matters here because a single "window" renderer carries
    // both an opaque frame material and a transparent glass one.
    private const int TransparentRenderQueue = 2450;

    // Summed face area of the world bounds. Below this an object cannot hide anything behind
    // it, so making it an occluder only costs bake time and memory.
    private const float MinOccluderArea = 2f;

    private const string InteractableLayerName = "Interactable";
    private const string ObstacleLayerName = "Obstacle";
    private const string NoObstacleLayerName = "NoObstacle";
    private const string NavLayerName = "Nav";
    private const string DoorTagName = "Door";

    // The registry's script field, not the layer, says whether something is a door. door_frame,
    // door_frame_<int>_glass and thick_door_hinge sit on the Interactable layer too — they are
    // trim around a door, not a door — so a tag derived from the layer put "Door" on 304 objects
    // that carry no Door behaviour at all. SetupInteractables and DoorComponentGates already key
    // off script for exactly this reason.
    private const string DoorScriptName = "Door";
    private const string UntaggedTagName = "Untagged";
    private const string UnoOverrideKeyword = "UNO";
    private const string UyoOverrideKeyword = "UYO";

    [MenuItem("FriWorld/Generate/Layers And Static From Registry")]
    private static void AssignOnPrefab()
        => PrefabTarget.Run("GenerateLayersAndStatic", root => Assign(new[] { root }));

    /// <summary>
    /// Walks the subtrees and applies layer, static flags, tag and NavMesh modifier. Public so
    /// the same pass can be pointed at a prefab root or, in a test, at a hand-built hierarchy.
    /// </summary>
    public static void Assign(GameObject[] roots)
    {
        if (!TryGetRequiredLayers(out Dictionary<string, int> layerMap))
        {
            return;
        }

        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("[GenerateLayersAndStatic] Nothing to walk.");
            return;
        }

        var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
        var registry = TypeRegistry.Load(ObjectRegistryMenu.TypesPath);
        if (registry.types.Count == 0)
        {
            Debug.LogError("[GenerateLayersAndStatic] " + ObjectRegistryMenu.TypesPath
                + " is empty. Run FriWorld > Registry > Seed Missing Types From Selection "
                + "first, otherwise every object would be skipped.");
            return;
        }

        int layerChanged = 0, staticChanged = 0, tagChanged = 0, shadowCastingChanged = 0;
        int navMeshModifierAdded = 0, navMeshModifierConfigured = 0;
        int overridden = 0, unchanged = 0;

        List<string> unresolved = new List<string>();
        List<string> badValues = new List<string>();
        HashSet<Transform> visited = new HashSet<Transform>();

        bool canAssignDoorTag = IsTagDefined(DoorTagName);
        if (!canAssignDoorTag)
        {
            Debug.LogWarning(
                "[GenerateLayersAndStatic] Tag 'Door' is not defined in Project Settings > Tags "
                + "and Layers. Door tag assignment/removal will be skipped."
            );
        }

        // No Undo group: this edits a prefab asset in a preview scene, where Undo does not
        // apply. Git is the undo.
        foreach (GameObject root in roots)
        {
            if (root == null) continue;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !visited.Add(t)) continue;

                GameObject go = t.gameObject;

                // The registry is keyed on the names of objects that carry geometry. Grouping
                // transforms are left on whatever layer they already have.
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer == null) continue;

                int targetLayer;
                bool targetStatic;
                TypeEntry entry = null;

                if (TryGetOverrideMatch(go.name, out bool forceObstacle))
                {
                    // UNO / UYO in the name are per-instance exceptions and win over the type.
                    targetLayer = layerMap[forceObstacle ? ObstacleLayerName : NoObstacleLayerName];
                    targetStatic = true;
                    overridden++;
                }
                else
                {
                    string typeKey = ObjectTypeKey.Derive(go.name, prefixes);
                    entry = registry.Find(typeKey);

                    if (entry == null || !entry.IsDecided)
                    {
                        unresolved.Add(typeKey + "   " + GetHierarchyPath(t));
                        continue;
                    }

                    switch (entry.layer)
                    {
                        case "interactable": targetLayer = layerMap[InteractableLayerName]; targetStatic = false; break;
                        case "obstacle":     targetLayer = layerMap[ObstacleLayerName];     targetStatic = true;  break;
                        case "noObstacle":   targetLayer = layerMap[NoObstacleLayerName];   targetStatic = true;  break;
                        case "nav":          targetLayer = layerMap[NavLayerName];          targetStatic = true;  break;
                        case "keep":         targetLayer = go.layer;                        targetStatic = false; break;
                        default:
                            badValues.Add(typeKey + "   layer=\"" + entry.layer + "\"");
                            continue;
                    }

                    if (entry.@static == "yes") targetStatic = true;
                    else if (entry.@static == "no") targetStatic = false;
                }

                bool modifierAdded = false, modifierConfigured = false;
                if (targetLayer == layerMap[ObstacleLayerName])
                {
                    EnsureObstacleNavMeshModifier(go, out modifierAdded, out modifierConfigured);
                    if (modifierAdded) navMeshModifierAdded++;
                    if (modifierConfigured) navMeshModifierConfigured++;
                }

                // Door tag follows the script the type asks for, unless the entry names a tag
                // itself. Deriving it from the Interactable layer tagged every door_frame too.
                bool isDoor = entry != null && entry.script == DoorScriptName;
                string desiredTag = entry != null && entry.tag != null
                    ? entry.tag
                    : (isDoor ? DoorTagName : UntaggedTagName);

                bool requiresTagUpdate = false;
                string nextTag = go.tag;
                if (canAssignDoorTag && !string.Equals(go.tag, desiredTag, StringComparison.Ordinal))
                {
                    requiresTagUpdate = true;
                    nextTag = desiredTag;
                }

                StaticEditorFlags desiredFlags = (StaticEditorFlags)0;
                if (targetStatic)
                {
                    bool occluder;
                    if (entry != null && entry.occluder == "yes") occluder = true;
                    else if (entry != null && entry.occluder == "no") occluder = false;
                    else occluder = ShouldBeOccluder(go);   // "auto", and the path UNO/UYO takes

                    desiredFlags = occluder ? AllStaticFlags : OccludeeOnlyFlags;
                }

                StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(go);

                // Same see-through test as the occluder decision, for the same reason: what the
                // player looks through, the lightmapper must be able to shoot through too. It has
                // to stay a material test — a "window" object is an assembly, and the glazing and
                // the solid parapet under it are separate renderers sharing one type key.
                var desiredShadowCasting = IsSeeThrough(renderer)
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : UnityEngine.Rendering.ShadowCastingMode.On;

                bool hasChange =
                    go.layer != targetLayer
                    || currentFlags != desiredFlags
                    || renderer.shadowCastingMode != desiredShadowCasting
                    || modifierAdded
                    || modifierConfigured
                    || requiresTagUpdate;

                if (!hasChange)
                {
                    unchanged++;
                    continue;
                }


                if (go.layer != targetLayer)
                {
                    go.layer = targetLayer;
                    layerChanged++;
                }

                if (currentFlags != desiredFlags)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, desiredFlags);
                    staticChanged++;
                }

                if (requiresTagUpdate)
                {
                    go.tag = nextTag;
                    tagChanged++;
                }

                if (renderer.shadowCastingMode != desiredShadowCasting)
                {
                    renderer.shadowCastingMode = desiredShadowCasting;
                    EditorUtility.SetDirty(renderer);
                    shadowCastingChanged++;
                }

                EditorUtility.SetDirty(go);
            }
        }

        Debug.Log(
            $"[GenerateLayersAndStatic] Completed. Processed: {visited.Count}, "
                + $"LayerChanged: {layerChanged}, StaticChanged: {staticChanged}, TagChanged: {tagChanged}, "
                + $"ShadowCastingChanged: {shadowCastingChanged}, "
                + $"NameOverrides (UNO/UYO): {overridden}, "
                + $"NavMeshModifierAdded: {navMeshModifierAdded}, "
                + $"NavMeshModifierConfigured: {navMeshModifierConfigured}, "
                + $"Unchanged: {unchanged}, LeftUntouched: {unresolved.Count}"
        );

        if (unresolved.Count > 0)
        {
            Debug.LogWarning("[GenerateLayersAndStatic] " + unresolved.Count
                + " objects were left untouched because their type is unknown or undecided.\n"
                + "Run FriWorld > Registry > Report On Selection to see them grouped, or fix "
                + "them in " + ObjectRegistryMenu.TypesPath + ":\n"
                + string.Join("\n", unresolved.ToArray()));
        }

        if (badValues.Count > 0)
        {
            Debug.LogError("[GenerateLayersAndStatic] Unrecognised layer values (expected "
                + "interactable, obstacle, noObstacle, nav or keep):\n"
                + string.Join("\n", badValues.ToArray()));
        }
    }

    /// <summary>
    /// Final say on Occluder Static. A name cannot tell you whether a surface is see-through,
    /// and in this project it frequently isn't what it sounds like — a "window" renderer carries
    /// an opaque frame material *and* transparent glass, and a fair number of "door_frame"
    /// objects are glazed. Baking those as occluders makes Umbra cull whatever is behind the
    /// glass, which the player can see straight through.
    /// </summary>
    public static bool ShouldBeOccluder(GameObject go)
    {
        if (go == null) return false;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return false;

        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].renderQueue >= TransparentRenderQueue)
            {
                return false;
            }
        }

        Vector3 size = renderer.bounds.size;
        float area = size.x * size.y + size.y * size.z + size.x * size.z;
        return area >= MinOccluderArea;
    }

    /// <summary>
    /// True when every material on the renderer is see-through, i.e. nothing on this mesh is
    /// meant to stop light. A window pane whose frame shares the same renderer does not qualify
    /// — the frame is opaque and has to keep casting its shadow.
    ///
    /// The Progressive lightmapper treats every shadow caster as fully opaque whatever the
    /// material says, so this is what decides where daylight can enter the building. It has to
    /// stay per-renderer: <c>ra006_window_1</c> is three renderers — an opaque frame, the glazing
    /// on the see-through <c>glass</c> material, and a <c>mt_glass_2</c> slab that is the solid
    /// parapet under the sill plus the head above it. Only the middle one may let light through.
    /// </summary>
    public static bool IsSeeThrough(Renderer renderer)
    {
        if (renderer == null) return false;

        Material[] materials = renderer.sharedMaterials;
        if (materials.Length == 0) return false;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null || materials[i].renderQueue < TransparentRenderQueue)
            {
                return false;
            }
        }

        return true;
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
            int id = LayerMask.NameToLayer(required[i]);
            if (id < 0) { missing.Add(required[i]); continue; }
            layerMap[required[i]] = id;
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

    /// <summary>
    /// UNO / UYO markers in an object's name. Case-sensitive on purpose, so an ordinary word
    /// containing "uno" is not mistaken for the override. UYO wins if both appear.
    /// </summary>
    private static bool TryGetOverrideMatch(string objectName, out bool forceObstacle)
    {
        forceObstacle = false;

        bool hasUno = false, hasUyo = false;
        foreach (string token in TokenizeCaseSensitive(objectName))
        {
            if (token == UnoOverrideKeyword) hasUno = true;
            else if (token == UyoOverrideKeyword) hasUyo = true;
        }

        if (hasUyo) { forceObstacle = true; return true; }
        if (hasUno) { forceObstacle = false; return true; }
        return false;
    }

    private static List<string> TokenizeCaseSensitive(string input)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return tokens;

        StringBuilder tokenBuilder = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsLetterOrDigit(c)) tokenBuilder.Append(c);
            else if (tokenBuilder.Length > 0) { tokens.Add(tokenBuilder.ToString()); tokenBuilder.Clear(); }
        }
        if (tokenBuilder.Length > 0) tokens.Add(tokenBuilder.ToString());

        return tokens;
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
            modifier = go.AddComponent<NavMeshModifier>();
            modifierAdded = true;
        }

        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
        if (notWalkableArea < 0) notWalkableArea = 1;

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

        if (!requiresPropertyUpdate && !requiresAffectedAgentsUpdate) return;


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

    private static bool IsTagDefined(string tag)
    {
        string[] tags = InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";

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
