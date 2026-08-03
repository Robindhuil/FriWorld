using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniversalMenu))]
public class UniversalMenuEditor : Editor
{
    SerializedProperty menuTypeProperty;
    SerializedProperty sceneToLoadProperty;
    SerializedProperty menuDocumentProperty;
    SerializedProperty uiManagerProperty;
    SerializedProperty audioSettingsProperty;
    SerializedProperty videoSettingsProperty;
    SerializedProperty gameSettingsProperty;
    SerializedProperty controlsSettingsProperty;
    SerializedProperty ppVolumeProperty;
    SerializedProperty backgroundMusicProperty;
    SerializedProperty buttonClickSoundProperty;

    private void OnEnable()
    {
        menuTypeProperty = serializedObject.FindProperty("menuType");
        sceneToLoadProperty = serializedObject.FindProperty("sceneToLoad");
        menuDocumentProperty = serializedObject.FindProperty("menuDocument");
        uiManagerProperty = serializedObject.FindProperty("uiManager");
        audioSettingsProperty = serializedObject.FindProperty("audioSettings");
        videoSettingsProperty = serializedObject.FindProperty("videoSettings");
        gameSettingsProperty = serializedObject.FindProperty("gameSettings");
        controlsSettingsProperty = serializedObject.FindProperty("controlsSettings");
        ppVolumeProperty = serializedObject.FindProperty("ppVolume");
        backgroundMusicProperty = serializedObject.FindProperty("backgroundMusic");
        buttonClickSoundProperty = serializedObject.FindProperty("buttonClickSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Menu Configuration Header
        EditorGUILayout.PropertyField(menuTypeProperty);

        EditorGUILayout.Space();

        MenuType currentMenuType = (MenuType)menuTypeProperty.enumValueIndex;

        // Scene Settings (MainMenu only)
        if (currentMenuType == MenuType.MainMenu)
        {
            EditorGUILayout.PropertyField(sceneToLoadProperty, new GUIContent("Scene To Load"));
            EditorGUILayout.Space();
        }

        // UI Documents Header
        EditorGUILayout.PropertyField(menuDocumentProperty);

        // Show/Hide fields based on menu type
        if (currentMenuType == MenuType.GameplayMenu)
        {
            // Gameplay Menu Only References
            EditorGUILayout.PropertyField(uiManagerProperty);
            EditorGUILayout.Space();
        }

        // Settings References (shown for both menu types)
        EditorGUILayout.PropertyField(audioSettingsProperty);
        EditorGUILayout.PropertyField(videoSettingsProperty);
        EditorGUILayout.PropertyField(gameSettingsProperty);
        EditorGUILayout.PropertyField(controlsSettingsProperty);

        EditorGUILayout.Space();

        // PP Volume (shown for both menu types)
        EditorGUILayout.LabelField("Post Processing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ppVolumeProperty, new GUIContent("PP Volume"));
        if (currentMenuType == MenuType.MainMenu)
        {
            EditorGUILayout.HelpBox(
                "Main Menu: PP Volume je volitelny. Nastavenia sa ulozia do PlayerPrefs a aplikuju pri starte gameplay scene.",
                MessageType.Info);
        }

        EditorGUILayout.Space();

        // Optional References
        EditorGUILayout.PropertyField(backgroundMusicProperty);
        EditorGUILayout.PropertyField(buttonClickSoundProperty, new GUIContent("Button Click Sound"));

        serializedObject.ApplyModifiedProperties();
    }
}
