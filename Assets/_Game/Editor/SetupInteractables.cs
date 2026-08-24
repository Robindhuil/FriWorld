using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Attaches interactable behaviour from the object type registry.
///
/// Which objects get which script is decided by the <c>script</c> field in
/// <c>ObjectTypes.json</c>. That has to be explicit rather than derived from the layer:
/// <c>door_frame</c> is on the Interactable layer too, but must not become an openable door.
///
/// Tag and layer are not touched here — GenerateLayersAndStatic owns those.
/// </summary>
public static class SetupInteractables
{
    private const string DoorScript = "Door";
    private const string DoorControllerPath = "Assets/_Game/Animations/Door_Interaction.controller";

    // 281 of the 282 doors that existed before carried 90; the field's own default is 90.9,
    // which a single door had kept. Setting it explicitly avoids reintroducing that stray value.
    private const float DoorOpenRotation = 90f;

    [MenuItem("FriWorld/Generate/Interactables From Registry")]
    private static void SetupOnPrefab()
        => PrefabTarget.Run("SetupInteractables", root => Run(new[] { root }, apply: true));

    [MenuItem("FriWorld/Generate/Interactables - Report Only (dry run)")]
    private static void ReportOnly()
    {
        // Opened and closed WITHOUT saving. A dry run that writes the prefab is not a dry run.
        GameObject contents = RoomGateScope.Open();
        try { Run(new[] { contents }, apply: false); }
        finally { RoomGateScope.Close(contents); }
    }

    /// <summary>
    /// Walks the subtrees and attaches the behaviour each type's script field names. Public so
    /// the same pass can be pointed at a prefab root or, in a test, at a hand-built hierarchy.
    /// </summary>
    public static void Run(GameObject[] roots, bool apply)
    {
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("[SetupInteractables] Nothing to walk.");
            return;
        }

        var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
        var registry = TypeRegistry.Load(ObjectRegistryMenu.TypesPath);
        if (registry.types.Count == 0)
        {
            Debug.LogError("[SetupInteractables] " + ObjectRegistryMenu.TypesPath + " is empty.");
            return;
        }

        AnimatorController doorController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(DoorControllerPath);
        if (doorController == null)
        {
            Debug.LogError("[SetupInteractables] Animator controller not found at "
                + DoorControllerPath + ". Doors would end up unable to animate.");
            return;
        }

        int scriptAdded = 0, scriptAlready = 0;
        int animatorAdded = 0, controllerAssigned = 0;
        int audioRouted = 0, audioAlreadyRouted = 0;
        var unknownScripts = new Dictionary<string, int>();
        var byType = new Dictionary<string, int>();
        var visited = new HashSet<Transform>();

        // No Undo group: this edits a prefab asset in a preview scene, where Undo does not
        // apply. Git is the undo.
        foreach (GameObject root in roots)
        {
            if (root == null) continue;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !visited.Add(t)) continue;

                GameObject go = t.gameObject;
                if (go.GetComponent<Renderer>() == null) continue;

                var entry = registry.Find(ObjectTypeKey.Derive(go.name, prefixes));
                if (entry == null || string.IsNullOrEmpty(entry.script)) continue;

                if (entry.script != DoorScript)
                {
                    unknownScripts.TryGetValue(entry.script, out int u);
                    unknownScripts[entry.script] = u + 1;
                    continue;
                }

                byType.TryGetValue(entry.name, out int c);
                byType[entry.name] = c + 1;

                bool hasDoor = go.GetComponent<Door>() != null;
                if (hasDoor) scriptAlready++;
                else scriptAdded++;

                Animator animator = go.GetComponent<Animator>();
                if (animator == null) animatorAdded++;
                else if (animator.runtimeAnimatorController != doorController) controllerAssigned++;

                if (!apply) continue;

                if (!hasDoor)
                {
                    // AddComponent runs Reset(), which sets the prompt, sound and range, and
                    // OnValidate then adds and configures the AudioSource by itself.
                    Door door = go.AddComponent<Door>();
                    var so = new SerializedObject(door);
                    var rotation = so.FindProperty("openRotationAmount");
                    if (rotation != null)
                    {
                        rotation.floatValue = DoorOpenRotation;
                        so.ApplyModifiedProperties();
                    }
                }

                if (animator == null) animator = go.AddComponent<Animator>();
                if (animator.runtimeAnimatorController != doorController)
                {
                    animator.runtimeAnimatorController = doorController;
                }

                // Without an output group the sound bypasses the mixer, so no volume slider in
                // the settings menu can touch it. Adding Door above ran OnValidate, which has
                // already created the AudioSource by now.
                var source = go.GetComponent<AudioSource>();
                if (source != null)
                {
                    if (SfxMixerGroup.Route(source)) audioRouted++;
                    else audioAlreadyRouted++;
                }

                EditorUtility.SetDirty(go);
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[SetupInteractables] " + (apply ? "applied" : "DRY RUN — nothing changed")
                    + " over " + visited.Count + " objects");
        sb.AppendLine("  Door script:  " + scriptAdded + " to add, " + scriptAlready + " already present");
        sb.AppendLine("  Animator:     " + animatorAdded + " to add, " + controllerAssigned
                    + " needing the controller assigned");
        if (apply)
            sb.AppendLine("  Sfx routing:  " + audioRouted + " sent to the mixer, "
                        + audioAlreadyRouted + " already had a group");
        sb.AppendLine("  by type:");
        foreach (var kv in byType) sb.AppendLine("      " + kv.Key + "   x" + kv.Value);

        if (unknownScripts.Count > 0)
        {
            sb.AppendLine("  UNKNOWN script values in the registry (nothing was attached):");
            foreach (var kv in unknownScripts) sb.AppendLine("      \"" + kv.Key + "\"  x" + kv.Value);
        }

        Debug.Log(sb.ToString());
    }

}
