using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renames all selected objects to "{baseName}_{n}" with an incrementing number.
/// Tools > Sequential Rename.
/// </summary>
public class SequentialRenamer : EditorWindow
{
    private string baseName = "object";
    private int start = 1;
    private bool sortByHierarchy = true;

    [MenuItem("Tools/Sequential Rename")]
    private static void Open()
    {
        // utility window = floating, modal-ish
        var w = GetWindow<SequentialRenamer>(true, "Sequential Rename");
        w.minSize = new Vector2(300, 120);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Renames every selected object to  <name>_<number>  incrementing.", MessageType.Info);

        baseName = EditorGUILayout.TextField("Base name", baseName);
        start = EditorGUILayout.IntField("Start number", start);
        sortByHierarchy = EditorGUILayout.Toggle(new GUIContent("Sort by hierarchy",
            "Number in hierarchy order (siblings top→bottom). Off = current selection order."), sortByHierarchy);

        EditorGUILayout.LabelField("Selected", Selection.gameObjects.Length.ToString());

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0 || string.IsNullOrEmpty(baseName)))
            if (GUILayout.Button("Rename"))
                Rename();
    }

    private void Rename()
    {
        var objs = Selection.gameObjects.ToList();
        if (sortByHierarchy)
            objs = objs.OrderBy(o => o.transform.GetSiblingIndex()).ToList();

        int n = start;
        foreach (var go in objs)
        {
            Undo.RecordObject(go, "Sequential Rename");
            go.name = $"{baseName}_{n}";
            EditorUtility.SetDirty(go);
            n++;
        }

        Debug.Log($"[SequentialRename] Renamed {objs.Count} object(s): '{baseName}_{start}' .. '{baseName}_{n - 1}'.");
        Close();
    }
}
