using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click creation of the <see cref="FeatureFlagConfig"/> asset at
/// <c>Assets/Resources/FeatureFlags.asset</c> (the path <see cref="Features"/> loads).
/// </summary>
static class FeatureFlagConfigCreator
{
    const string Dir = "Assets/Resources";
    const string Path = "Assets/Resources/FeatureFlags.asset";

    [MenuItem("FriWorld/Feature Flags/Create Config Asset")]
    static void Create()
    {
        var existing = AssetDatabase.LoadAssetAtPath<FeatureFlagConfig>(Path);
        if (existing != null)
        {
            Debug.Log("[FeatureFlags] Config already exists at " + Path);
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        if (!AssetDatabase.IsValidFolder(Dir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var cfg = ScriptableObject.CreateInstance<FeatureFlagConfig>();
        AssetDatabase.CreateAsset(cfg, Path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
        Debug.Log("[FeatureFlags] Created " + Path + " — add your flags in the inspector.");
    }
}
