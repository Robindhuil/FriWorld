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
    private static void SetupFromSelection() => Run(apply: true);

    [MenuItem("FriWorld/Generate/Interactables - Report Only (dry run)")]
    private static void ReportOnly() => Run(apply: false);

    private static void Run(bool apply)
    {
        GameObject[] roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("[SetupInteractables] No objects selected in Hierarchy.");
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
        var unknownScripts = new Dictionary<string, int>();
        var byType = new Dictionary<string, int>();
        var visited = new HashSet<Transform>();

        if (apply)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Interactables From Registry");
        }
        int undoGroup = Undo.GetCurrentGroup();

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
                    Door door = Undo.AddComponent<Door>(go);
                    var so = new SerializedObject(door);
                    var rotation = so.FindProperty("openRotationAmount");
                    if (rotation != null)
                    {
                        rotation.floatValue = DoorOpenRotation;
                        so.ApplyModifiedProperties();
                    }
                }

                if (animator == null) animator = Undo.AddComponent<Animator>(go);
                if (animator.runtimeAnimatorController != doorController)
                {
                    Undo.RecordObject(animator, "Assign Door Animator Controller");
                    animator.runtimeAnimatorController = doorController;
                }

                EditorUtility.SetDirty(go);
            }
        }

        if (apply) Undo.CollapseUndoOperations(undoGroup);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[SetupInteractables] " + (apply ? "applied" : "DRY RUN — nothing changed")
                    + " over " + visited.Count + " objects");
        sb.AppendLine("  Door script:  " + scriptAdded + " to add, " + scriptAlready + " already present");
        sb.AppendLine("  Animator:     " + animatorAdded + " to add, " + controllerAssigned
                    + " needing the controller assigned");
        sb.AppendLine("  by type:");
        foreach (var kv in byType) sb.AppendLine("      " + kv.Key + "   x" + kv.Value);

        if (unknownScripts.Count > 0)
        {
            sb.AppendLine("  UNKNOWN script values in the registry (nothing was attached):");
            foreach (var kv in unknownScripts) sb.AppendLine("      \"" + kv.Key + "\"  x" + kv.Value);
        }

        Debug.Log(sb.ToString());
    }

    [MenuItem("FriWorld/Generate/Interactables From Registry", true)]
    [MenuItem("FriWorld/Generate/Interactables - Report Only (dry run)", true)]
    private static bool ValidateSelection()
        => Selection.gameObjects != null && Selection.gameObjects.Length > 0;
}
