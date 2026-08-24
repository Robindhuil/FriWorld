using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WindowLightsSetup : EditorWindow
{
    private string windowKeyword = "window";
    private float lightIntensity = 3.5f;
    private float lightRange = 8f;
    private Color lightColor = new Color(1f, 0.88f, 0.65f); // warm daylight
    private float lightOffset = 0.5f; // distance inside from window
    private bool previewOnly = true;

    [MenuItem("FriWorld/Lighting/Setup Window Lights")]
    public static void ShowWindow()
    {
        GetWindow<WindowLightsSetup>("Window Lights");
    }

    private void OnGUI()
    {
        GUILayout.Label("Window Light Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        windowKeyword = EditorGUILayout.TextField("Window keyword", windowKeyword);
        lightIntensity = EditorGUILayout.FloatField("Light Intensity", lightIntensity);
        lightRange = EditorGUILayout.FloatField("Light Range", lightRange);
        lightColor = EditorGUILayout.ColorField("Light Color", lightColor);
        lightOffset = EditorGUILayout.FloatField("Inward Offset", lightOffset);

        EditorGUILayout.Space();
        previewOnly = EditorGUILayout.Toggle("Preview only (count)", previewOnly);

        EditorGUILayout.Space();

        if (GUILayout.Button("Find Windows & Add Lights"))
            Execute();

        if (GUILayout.Button("Remove All Window Lights"))
            RemoveAll();
    }

    private void Execute()
    {
        var windows = FindWindowObjects();
        Debug.Log($"[WindowLights] Found {windows.Count} window objects");

        if (previewOnly)
        {
            foreach (var w in windows)
                Debug.Log($"  - {w.name} at {w.transform.position}");
            EditorUtility.DisplayDialog("Window Lights", $"Found {windows.Count} windows.\nUncheck 'Preview only' and run again to add lights.", "OK");
            return;
        }

        var parent = new GameObject("WindowLights");
        Undo.RegisterCreatedObjectUndo(parent, "Add Window Lights");

        int added = 0;
        foreach (var w in windows)
        {
            if (HasWindowLight(w)) continue;

            var go = new GameObject($"WindowLight_{w.name}");
            go.transform.SetParent(parent.transform);

            // Place light slightly inside (offset in -forward direction of window)
            Vector3 pos = w.transform.position + Vector3.up * 0.5f;
            // try to nudge inward using window's forward
            pos -= w.transform.forward * lightOffset;
            go.transform.position = pos;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = lightColor;
            light.intensity = lightIntensity;
            light.range = lightRange;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.3f;

            added++;
        }

        Debug.Log($"[WindowLights] Added {added} lights.");
        EditorUtility.DisplayDialog("Window Lights", $"Added {added} point lights near windows.\nRebake lightmaps to include them in GI.", "OK");
    }

    private List<GameObject> FindWindowObjects()
    {
        var result = new List<GameObject>();
        var keyword = windowKeyword.ToLower();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.name == null || go.hideFlags != HideFlags.None) continue;
            if (go.name.ToLower().Contains(keyword))
                result.Add(go);
        }
        return result;
    }

    private bool HasWindowLight(GameObject window)
    {
        // check if a WindowLight_ already exists near this window
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
            if (l.name.Contains(window.name)) return true;
        return false;
    }

    private void RemoveAll()
    {
        var go = GameObject.Find("WindowLights");
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
            Debug.Log("[WindowLights] Removed all window lights.");
        }
        else
        {
            EditorUtility.DisplayDialog("Window Lights", "No 'WindowLights' object found.", "OK");
        }
    }
}
