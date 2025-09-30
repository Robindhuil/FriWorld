using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Interactable), true)]
public class InteractableEditor : Editor
{
    SerializedProperty useEventsProp;
    SerializedProperty promptMessageProp;
    SerializedProperty playSoundEffectProp;
    SerializedProperty audioSourceProp;
    SerializedProperty soundClipProp;

    private void OnEnable()
    {
        useEventsProp = serializedObject.FindProperty("useEvents");
        promptMessageProp = serializedObject.FindProperty("promptMessage");
        playSoundEffectProp = serializedObject.FindProperty("playSoundEffect");
        audioSourceProp = serializedObject.FindProperty("audioSource");
        soundClipProp = serializedObject.FindProperty("soundClip");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(useEventsProp);
        EditorGUILayout.PropertyField(promptMessageProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Optional Sound Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playSoundEffectProp);

        if (playSoundEffectProp.boolValue)
        {
            EditorGUILayout.PropertyField(audioSourceProp);
            EditorGUILayout.PropertyField(soundClipProp);

            if (audioSourceProp.objectReferenceValue == null)
            {
                if (GUILayout.Button("Pridať AudioSource"))
                {
                    var interactable = (Interactable)target;
                    AudioSource newSource = interactable.gameObject.AddComponent<AudioSource>();
                    audioSourceProp.objectReferenceValue = newSource;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---- Ďalšie polia ----", EditorStyles.boldLabel);

        DrawPropertiesExcluding(serializedObject,
            "useEvents",
            "promptMessage",
            "playSoundEffect",
            "audioSource",
            "soundClip",
            "m_Script"
        );

        serializedObject.ApplyModifiedProperties();
    }
}
