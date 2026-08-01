using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only preview of <see cref="PlatformGate"/> in the open scene. Toggles the
/// active state of gated objects so you can see what a given platform build will
/// contain — WITHOUT building. Nothing here changes what actually ships (that is the
/// build processor's job); it only (de)activates objects for inspection.
///
/// Run "Preview: Desktop (show all)" before saving the scene so you don't accidentally
/// save objects left disabled by a Web preview.
/// </summary>
static class FeatureFlagPreview
{
    [MenuItem("Tools/Feature Flags/Preview: Web build")]
    static void PreviewWeb() => Apply(web: true);

    [MenuItem("Tools/Feature Flags/Preview: Desktop (show all)")]
    static void PreviewDesktop() => Apply(web: false);

    static void Apply(bool web)
    {
        var gates = Object.FindObjectsByType<PlatformGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int changed = 0;
        foreach (var g in gates)
        {
            bool keep = g.target == PlatformGate.Target.WebOnly ? web : !web;
            if (g.gameObject.activeSelf != keep)
            {
                Undo.RecordObject(g.gameObject, "Feature Flag Preview");
                g.gameObject.SetActive(keep);
                changed++;
            }
        }

        Debug.Log($"[FeatureFlags] Preview {(web ? "WEB" : "DESKTOP")}: toggled {changed} object(s) " +
                  $"across {gates.Length} PlatformGate(s).");
    }
}
