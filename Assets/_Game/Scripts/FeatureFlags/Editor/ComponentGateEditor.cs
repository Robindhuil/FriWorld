using UnityEditor;
using UnityEngine;

/// <summary>
/// Cleaner inspector for <see cref="ComponentGate"/>: readable labels and a real layer
/// dropdown that only appears when "change layer" is ticked.
/// </summary>
[CustomEditor(typeof(ComponentGate))]
class ComponentGateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var target     = serializedObject.FindProperty("target");
        var components  = serializedObject.FindProperty("components");
        var changeLayer = serializedObject.FindProperty("changeLayerWhenStripped");
        var layer       = serializedObject.FindProperty("strippedLayer");

        EditorGUILayout.PropertyField(target, new GUIContent("Keep on platform",
            "Components below are kept only on this platform and stripped on the other."));

        EditorGUILayout.PropertyField(components, new GUIContent("Strip these components"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(changeLayer, new GUIContent("Change layer when stripped",
            "Also move this object off its layer when stripped (e.g. off 'Interactable')."));

        if (changeLayer.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(layer, new GUIContent("Change to layer")); // [Layer] -> dropdown
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
