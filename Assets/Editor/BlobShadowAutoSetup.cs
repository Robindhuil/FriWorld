using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Bulk-adds (or removes) BlobShadow on all NON-static objects that have a renderer.
// It attaches ONE BlobShadow at the top-most non-static ancestor of each renderer,
// so a multi-part character gets a single blob, not one per body part.
//
// NOTE: objects spawned at RUNTIME (via spawners) won't be covered by a scene pass —
// for those, add BlobShadow to the NPC PREFAB once (select the prefab and use
// "Add to Selection") so every spawned copy gets it.
public class BlobShadowAutoSetup : EditorWindow
{
    private LayerMask groundMask = ~0;
    private float size = 1.1f;
    private float strength = 0.5f;
    private string npcTag = "npc";

    [MenuItem("FriWorld/BlobShadow: Auto Setup")]
    public static void Open() => GetWindow<BlobShadowAutoSetup>("BlobShadow Setup");

    private void OnGUI()
    {
        GUILayout.Label("Auto BlobShadow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Adds one BlobShadow to every object with the given tag.\n" +
            "Set Ground Mask to ground/building layers (exclude character layers).",
            MessageType.Info);

        npcTag = EditorGUILayout.TagField("Target Tag", npcTag);
        groundMask = LayerMaskField("Ground Mask", groundMask);
        size = EditorGUILayout.FloatField("Size", size);
        strength = EditorGUILayout.Slider("Strength", strength, 0f, 1f);

        EditorGUILayout.Space();
        if (GUILayout.Button($"Add to all '{npcTag}' objects in scene")) AddToScene();
        if (GUILayout.Button("Add to Selection (objects / prefabs)")) AddToSelection();
        EditorGUILayout.Space();
        if (GUILayout.Button("Remove ALL BlobShadows in scene")) RemoveAll();
    }

    private static bool IsStatic(GameObject go)
    {
        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        return (flags & StaticEditorFlags.ContributeGI) != 0
            || (flags & StaticEditorFlags.BatchingStatic) != 0;
    }

    private Transform GetDynamicRoot(Transform t)
    {
        var root = t;
        while (root.parent != null && !IsStatic(root.parent.gameObject))
            root = root.parent;
        return root;
    }

    private void Configure(BlobShadow b)
    {
        b.groundMask = groundMask;
        b.size = size;
        b.strength = strength;
    }

    private void AddToScene()
    {
        GameObject[] tagged;
        try
        {
            tagged = GameObject.FindGameObjectsWithTag(npcTag);
        }
        catch
        {
            EditorUtility.DisplayDialog("BlobShadow",
                $"Tag '{npcTag}' is not defined in the project. Create it in the Tags & Layers settings, " +
                "or pick an existing tag.", "OK");
            return;
        }

        int added = 0;
        foreach (var go in tagged)
        {
            if (go.GetComponent<BlobShadow>() != null) continue;
            var b = Undo.AddComponent<BlobShadow>(go);
            Configure(b);
            EditorUtility.SetDirty(go);
            added++;
        }

        Debug.Log($"[BlobShadow] Added to {added} '{npcTag}' objects.");
        EditorUtility.DisplayDialog("BlobShadow",
            $"Added BlobShadow to {added} '{npcTag}' objects.\n\n" +
            "For runtime-spawned NPCs, also add it to their PREFAB (select prefab → 'Add to Selection').",
            "OK");
    }

    private void AddToSelection()
    {
        int added = 0;
        foreach (var obj in Selection.gameObjects)
        {
            if (obj.GetComponent<BlobShadow>() != null) continue;
            var b = Undo.AddComponent<BlobShadow>(obj);
            Configure(b);
            EditorUtility.SetDirty(obj);
            added++;
        }
        Debug.Log($"[BlobShadow] Added to {added} selected objects.");
    }

    private void RemoveAll()
    {
        var all = Object.FindObjectsByType<BlobShadow>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in all) Undo.DestroyObjectImmediate(b);
        Debug.Log($"[BlobShadow] Removed {all.Length} BlobShadows.");
    }

    // simple LayerMask field helper
    private static LayerMask LayerMaskField(string label, LayerMask mask)
    {
        var layers = new List<string>();
        var indices = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            string n = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(n)) { layers.Add(n); indices.Add(i); }
        }
        int field = 0;
        for (int i = 0; i < indices.Count; i++)
            if ((mask.value & (1 << indices[i])) != 0) field |= (1 << i);

        field = EditorGUILayout.MaskField(label, field, layers.ToArray());

        int newMask = 0;
        for (int i = 0; i < indices.Count; i++)
            if ((field & (1 << i)) != 0) newMask |= (1 << indices[i]);
        mask.value = newMask;
        return mask;
    }
}
