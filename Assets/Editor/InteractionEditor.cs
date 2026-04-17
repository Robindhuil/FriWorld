using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[CustomEditor(typeof(Interactable), true)]
public class InteractableEditor : Editor
{
    SerializedProperty useEventsProp;
    SerializedProperty promptMessageProp;
    SerializedProperty playSoundEffectProp;
    SerializedProperty audioSourceProp;
    SerializedProperty soundClipProp;
    SerializedProperty maxHearableRangeProp;
    SerializedProperty echoEnvironmentProp;

    private void OnEnable()
    {
        useEventsProp = serializedObject.FindProperty("useEvents");
        promptMessageProp = serializedObject.FindProperty("promptMessage");
        playSoundEffectProp = serializedObject.FindProperty("playSoundEffect");
        audioSourceProp = serializedObject.FindProperty("audioSource");
        soundClipProp = serializedObject.FindProperty("soundClip");
        maxHearableRangeProp = serializedObject.FindProperty("maxHearableRange");
        echoEnvironmentProp = serializedObject.FindProperty("echoEnvironment");

        // Covers Reset(), script-side assignments, and prefab instantiation:
        // defer so OnValidate has already run and added the AudioSource.
        var t = (Interactable)target;
        EditorApplication.delayCall += () =>
        {
            if (t == null)
                return;
            var src = t.GetComponent<AudioSource>();
            if (src != null)
                TryAssignSFXMixerGroup(src);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(useEventsProp);
        EditorGUILayout.PropertyField(promptMessageProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Optional Sound Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(playSoundEffectProp);
        bool soundToggled = EditorGUI.EndChangeCheck() && playSoundEffectProp.boolValue;

        bool isEnabled = playSoundEffectProp.boolValue;

        if (isEnabled)
        {
            EditorGUILayout.PropertyField(audioSourceProp);
            EditorGUILayout.PropertyField(soundClipProp);
            EditorGUILayout.PropertyField(maxHearableRangeProp);
            EditorGUILayout.PropertyField(echoEnvironmentProp);

            if (audioSourceProp.objectReferenceValue == null)
            {
                if (GUILayout.Button("Pridať AudioSource"))
                {
                    serializedObject.ApplyModifiedProperties();
                    var interactable = (Interactable)target;
                    AudioSource newSource = interactable.gameObject.AddComponent<AudioSource>();
                    audioSourceProp.objectReferenceValue = newSource;
                    serializedObject.ApplyModifiedProperties();
                    TryAssignSFXMixerGroup(newSource);
                    return;
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("---- Ďalšie polia ----", EditorStyles.boldLabel);

        DrawPropertiesExcluding(
            serializedObject,
            "useEvents",
            "promptMessage",
            "playSoundEffect",
            "audioSource",
            "soundClip",
            "maxHearableRange",
            "echoEnvironment",
            "m_Script"
        );

        serializedObject.ApplyModifiedProperties();

        // Delay so OnValidate has time to add the AudioSource before we assign the mixer group
        if (soundToggled)
        {
            var t = (Interactable)target;
            EditorApplication.delayCall += () =>
            {
                if (t == null)
                    return;
                var src = t.GetComponent<AudioSource>();
                if (src != null)
                    TryAssignSFXMixerGroup(src);
            };
        }
    }

    private static void TryAssignSFXMixerGroup(AudioSource source)
    {
        if (source == null || source.outputAudioMixerGroup != null)
            return;

        foreach (string guid in AssetDatabase.FindAssets("t:AudioMixer"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (mixer == null)
                continue;

            // FindMatchingGroups does a substring match on the full group path (e.g. "Master/Sfx")
            var groups = mixer.FindMatchingGroups("Sfx");
            if (groups.Length == 0)
                continue;

            source.outputAudioMixerGroup = groups[0];
            EditorUtility.SetDirty(source);
            break;
        }
    }
}
