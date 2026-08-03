using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Auto-generates a grid of Light Probes filling the building volume so that dynamic
// objects (NPCs, props, doors) pick up the baked GI — they darken in dark rooms,
// warm up where it's warm, and stop looking flat/disconnected from static geometry.
// Probes that would land inside solid geometry (walls/floors) are skipped via a
// collider check, to avoid black/garbage probe data.
//
// After generating: REBAKE lighting so the probes capture GI.
public class LightProbePlacer : EditorWindow
{
    private float spacing = 4f;          // horizontal grid step
    private float verticalSpacing = 2.5f; // vertical grid step (covers floors)
    private float padding = 1f;
    private bool boundByStaticOnly = true;
    private bool skipInsideColliders = true;
    private float insideCheckRadius = 0.3f;

    [MenuItem("FriWorld/Generate Light Probes")]
    public static void Open() => GetWindow<LightProbePlacer>("Light Probes");

    private void OnGUI()
    {
        GUILayout.Label("Auto Light Probe Grid", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        spacing = EditorGUILayout.FloatField("Horizontal Spacing", spacing);
        verticalSpacing = EditorGUILayout.FloatField("Vertical Spacing", verticalSpacing);
        padding = EditorGUILayout.FloatField("Padding", padding);
        boundByStaticOnly = EditorGUILayout.Toggle("Bounds: static (GI) only", boundByStaticOnly);
        skipInsideColliders = EditorGUILayout.Toggle("Skip probes inside walls", skipInsideColliders);
        if (skipInsideColliders)
            insideCheckRadius = EditorGUILayout.FloatField("  Wall check radius", insideCheckRadius);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Probe Grid")) Generate();
        if (GUILayout.Button("Remove Generated Probes")) Remove();
    }

    private void Generate()
    {
        var renderers = Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool has = false;
        Bounds b = new Bounds();
        foreach (var r in renderers)
        {
            if (boundByStaticOnly)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0) continue;
            }
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }

        if (!has)
        {
            EditorUtility.DisplayDialog("Light Probes", "No renderers found for bounds.", "OK");
            return;
        }

        b.Expand(padding * 2f);

        var positions = new List<Vector3>();
        int skipped = 0;
        for (float x = b.min.x; x <= b.max.x; x += spacing)
            for (float z = b.min.z; z <= b.max.z; z += spacing)
                for (float y = b.min.y; y <= b.max.y; y += verticalSpacing)
                {
                    var p = new Vector3(x, y, z);
                    if (skipInsideColliders &&
                        Physics.CheckSphere(p, insideCheckRadius, ~0, QueryTriggerInteraction.Ignore))
                    {
                        skipped++;
                        continue;
                    }
                    positions.Add(p);
                }

        if (positions.Count == 0)
        {
            EditorUtility.DisplayDialog("Light Probes",
                "0 probes generated (all skipped). Try larger spacing or disable wall-skip.", "OK");
            return;
        }

        if (positions.Count > 60000 &&
            !EditorUtility.DisplayDialog("Light Probes",
                $"{positions.Count} probes is a lot (slow bake). Continue?", "Continue", "Cancel"))
            return;

        var existing = GameObject.Find("AutoLightProbes");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var go = new GameObject("AutoLightProbes");
        go.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(go, "Create Light Probes");
        var group = go.AddComponent<LightProbeGroup>();
        group.probePositions = positions.ToArray(); // local == world (group at origin)
        EditorUtility.SetDirty(go);

        Debug.Log($"[LightProbes] Created {positions.Count} probes (skipped {skipped} inside walls). REBAKE now.");
        EditorUtility.DisplayDialog("Light Probes",
            $"Created {positions.Count} probes (skipped {skipped} inside walls).\n\nNow REBAKE lighting.",
            "OK");
    }

    private void Remove()
    {
        var go = GameObject.Find("AutoLightProbes");
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
            Debug.Log("[LightProbes] Removed AutoLightProbes.");
        }
    }
}
