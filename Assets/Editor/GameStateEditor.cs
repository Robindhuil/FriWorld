#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameState))]
public class GameStateEditor : Editor
{
    private static int boolDomainIndex;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameState gameState = (GameState)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Game State Data", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Bool States Section
        EditorGUILayout.LabelField("Boolean States", EditorStyles.boldLabel);
        foreach (GameState.BoolStateDomain domain in System.Enum.GetValues(typeof(GameState.BoolStateDomain)))
        {
            var states = gameState.GetBoolStates(domain);
            EditorGUILayout.LabelField($"  {domain} ({states.Count})", EditorStyles.boldLabel);
            if (states.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in states)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));

                    bool newValue = EditorGUILayout.Toggle(kvp.Value, GUILayout.Width(20));
                    if (newValue != kvp.Value)
                    {
                        gameState.SetBool(kvp.Key, newValue, domain);
                        EditorUtility.SetDirty(gameState);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        gameState.RemoveBool(kvp.Key, domain);
                        EditorUtility.SetDirty(gameState);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("  (Empty)", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space(10);

        // Int States Section
        EditorGUILayout.LabelField($"Integer States ({gameState.intStates.Count})", EditorStyles.boldLabel);
        if (gameState.intStates.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var kvp in gameState.intStates)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(200));
                
                int newValue = EditorGUILayout.IntField(kvp.Value, GUILayout.Width(60));
                if (newValue != kvp.Value)
                {
                    gameState.intStates[kvp.Key] = newValue;
                    EditorUtility.SetDirty(gameState);
                }
                
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    gameState.intStates.Remove(kvp.Key);
                    EditorUtility.SetDirty(gameState);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.LabelField("  (Empty)", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(10);

        // Utility Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Print All to Console"))
        {
            gameState.DebugPrintAllState();
        }
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear All State"))
        {
            if (EditorUtility.DisplayDialog("Clear All State",
                "Are you sure you want to clear all state data?", "Yes", "No"))
            {
                gameState.ClearAllState();
                EditorUtility.SetDirty(gameState);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Add New Entry Section
        EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        string[] domainNames = System.Enum.GetNames(typeof(GameState.BoolStateDomain));
        boolDomainIndex = EditorGUILayout.Popup("Bool Domain", boolDomainIndex, domainNames);
        GameState.BoolStateDomain selectedDomain = (GameState.BoolStateDomain)boolDomainIndex;
        
        if (GUILayout.Button("Add Bool State"))
        {
            string key = "NewBoolKey";
            int counter = 1;
            while (gameState.ContainsBoolKey(key, selectedDomain))
            {
                key = $"NewBoolKey_{counter++}";
            }
            gameState.SetBool(key, false, selectedDomain);
            EditorUtility.SetDirty(gameState);
        }
        
        if (GUILayout.Button("Add Int State"))
        {
            string key = "NewIntKey";
            int counter = 1;
            while (gameState.intStates.ContainsKey(key))
            {
                key = $"NewIntKey_{counter++}";
            }
            gameState.intStates[key] = 0;
            EditorUtility.SetDirty(gameState);
        }
        
        EditorGUI.indentLevel--;

        // Force repaint in play mode to see live updates
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif
