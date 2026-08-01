using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// For every selected object whose name contains "door", adds/configures a ComponentGate:
/// target = DesktopOnly, strips the object's Door + Animator + AudioSource on web, and
/// moves it to layer 7 (Obstacle) when stripped. Tools > Setup Door Gates.
/// </summary>
public static class DoorGateSetup
{
    [MenuItem("Tools/Setup Door Gates")]
    private static void Setup()
    {
        int count = 0;
        foreach (var sel in Selection.gameObjects)
            foreach (var t in sel.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.ToLowerInvariant().Contains("door"))
                    continue;

                var go = t.gameObject;
                var gate = go.GetComponent<ComponentGate>();
                if (gate == null)
                    gate = Undo.AddComponent<ComponentGate>(go);
                Undo.RecordObject(gate, "Setup Door Gate");

                gate.target = PlatformGate.Target.DesktopOnly;
                gate.changeLayerWhenStripped = true;
                gate.strippedLayer = 7; // Obstacle

                gate.components = new List<Component>();
                var door = go.GetComponent<Door>();
                var anim = go.GetComponent<Animator>();
                var audio = go.GetComponent<AudioSource>();
                if (door != null) gate.components.Add(door);
                if (anim != null) gate.components.Add(anim);
                if (audio != null) gate.components.Add(audio);

                EditorUtility.SetDirty(gate);
                count++;
            }

        Debug.Log($"[DoorGateSetup] Configured ComponentGate on {count} door object(s).");
    }
}
