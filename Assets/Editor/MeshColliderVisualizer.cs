using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MeshColliderVisualizer
{
    private const string MenuPath = "Tools/Debug/Toggle Mesh Collider Visualizer";
    private const string PrefKey = "FriWorld.MeshColliderVisualizer.Enabled";
    private const GizmoType GizmoDrawMask =
        GizmoType.Active
        | GizmoType.InSelectionHierarchy
        | GizmoType.NonSelected
        | GizmoType.Selected;

    private static bool _isEnabled;
    private static readonly Color WireColor = new Color(0f, 0.85f, 1f, 0.9f);

    static MeshColliderVisualizer()
    {
        _isEnabled = EditorPrefs.GetBool(PrefKey, false);
        Menu.SetChecked(MenuPath, _isEnabled);
    }

    [MenuItem(MenuPath)]
    private static void ToggleVisualizer()
    {
        _isEnabled = !_isEnabled;
        EditorPrefs.SetBool(PrefKey, _isEnabled);

        Menu.SetChecked(MenuPath, _isEnabled);
        SceneView.RepaintAll();

        Debug.Log(
            $"[MeshColliderVisualizer] Mesh collider drawing {(_isEnabled ? "enabled" : "disabled")}"
        );
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateToggleVisualizer()
    {
        Menu.SetChecked(MenuPath, _isEnabled);
        return true;
    }

    [DrawGizmo(GizmoDrawMask)]
    private static void DrawMeshColliderGizmo(MeshCollider meshCollider, GizmoType gizmoType)
    {
        if (!_isEnabled || meshCollider == null || !meshCollider.enabled)
        {
            return;
        }

        if (!meshCollider.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!IsInCurrentSelection(meshCollider.transform))
        {
            return;
        }

        Mesh mesh = meshCollider.sharedMesh;
        if (mesh == null)
        {
            return;
        }

        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = WireColor;
        Gizmos.matrix = meshCollider.transform.localToWorldMatrix;
        Gizmos.DrawWireMesh(mesh);

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static bool IsInCurrentSelection(Transform transform)
    {
        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms == null || selectedTransforms.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < selectedTransforms.Length; i++)
        {
            Transform selected = selectedTransforms[i];
            if (selected == null)
            {
                continue;
            }

            if (transform == selected || transform.IsChildOf(selected))
            {
                return true;
            }
        }

        return false;
    }
}
