using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Modal to reorder the selected objects in the Hierarchy by name, ascending or
/// descending (per parent). FriWorld > Utilities > Sort Selected By Name.
/// </summary>
public class SortSelectedByName : EditorWindow
{
    private enum Dir { Ascending, Descending }

    private Dir dir = Dir.Ascending;
    private bool naturalSort = true;

    [MenuItem("FriWorld/Utilities/Sort Selected By Name")]
    private static void Open()
    {
        var w = GetWindow<SortSelectedByName>(true, "Sort Selected");
        w.minSize = new Vector2(280, 120);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Reorders the selected objects in the Hierarchy by name.", MessageType.Info);

        dir = (Dir)EditorGUILayout.EnumPopup("Direction", dir);
        naturalSort = EditorGUILayout.Toggle(new GUIContent("Natural sort",
            "Numeric-aware: lamp_2 before lamp_10. Off = plain text order."), naturalSort);

        EditorGUILayout.LabelField("Selected", Selection.transforms.Length.ToString());

        using (new EditorGUI.DisabledScope(Selection.transforms.Length == 0))
            if (GUILayout.Button("Sort"))
                Sort();
    }

    private void Sort()
    {
        IComparer<string> cmp = naturalSort ? new NaturalComparer() : (IComparer<string>)System.StringComparer.OrdinalIgnoreCase;

        int total = 0;
        foreach (var group in Selection.transforms.GroupBy(t => t.parent))
        {
            var ordered = group.OrderBy(t => t.name, cmp).AsEnumerable();
            if (dir == Dir.Descending)
                ordered = ordered.Reverse();
            var items = ordered.ToList();

            int start = group.Min(t => t.GetSiblingIndex());
            if (group.Key != null)
                Undo.RegisterFullObjectHierarchyUndo(group.Key.gameObject, "Sort selected by name");

            for (int i = 0; i < items.Count; i++)
                items[i].SetSiblingIndex(start + i);

            total += items.Count;
        }

        Debug.Log($"[SortSelected] Sorted {total} object(s) {dir}{(naturalSort ? " (natural)" : "")}.");
        Close();
    }

    /// <summary>Numeric-aware string comparer (lamp_2 &lt; lamp_10).</summary>
    private class NaturalComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
                {
                    int sx = ix; while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                    int sy = iy; while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                    string nx = x.Substring(sx, ix - sx).TrimStart('0');
                    string ny = y.Substring(sy, iy - sy).TrimStart('0');
                    if (nx.Length != ny.Length) return nx.Length - ny.Length;
                    int c = string.CompareOrdinal(nx, ny);
                    if (c != 0) return c;
                }
                else
                {
                    int c = char.ToLowerInvariant(x[ix]).CompareTo(char.ToLowerInvariant(y[iy]));
                    if (c != 0) return c;
                    ix++; iy++;
                }
            }
            return (x.Length - ix) - (y.Length - iy);
        }
    }
}
