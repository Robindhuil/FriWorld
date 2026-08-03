using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Build-time platform stripping for <see cref="PlatformGate"/>. Runs on a TEMPORARY
/// copy of each scene during a build (never touches your saved scenes), so objects
/// gated to the "wrong" platform are removed from that build entirely.
///
/// Desktop build → WebOnly objects removed. WebGL build → DesktopOnly objects removed.
/// Does nothing when entering play mode (report is null there), so the editor keeps
/// showing everything.
/// </summary>
class FeatureFlagBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // report == null => processing for play mode / asset bundles, not a real build.
        if (report == null)
            return;

        bool isWeb = report.summary.platform == BuildTarget.WebGL;
        bool Keep(PlatformGate.Target t) => t == PlatformGate.Target.WebOnly ? isWeb : !isWeb;

        // 1) Whole-object gates: remove mismatched GameObjects; drop the gate component
        //    from the ones we keep (it has done its job).
        int strippedObjects = 0;
        foreach (var gate in Object.FindObjectsByType<PlatformGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (gate == null)
                continue;
            if (!Keep(gate.target))
            {
                gate.RescueExcluded();         // reparent kept children out of the doomed subtree
                Object.DestroyImmediate(gate.gameObject);
                strippedObjects++;
            }
            else
            {
                Object.DestroyImmediate(gate); // kept object — remove the leftover gate component
            }
        }

        // 2) Component gates: keep the GameObject, remove only the listed components.
        int strippedComponents = 0;
        foreach (var cg in Object.FindObjectsByType<ComponentGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cg == null)
                continue;
            if (!Keep(cg.target))
            {
                // Scripts first — they often [RequireComponent] the built-in components
                // below, which cannot be removed while the requiring script is present.
                foreach (var c in cg.components)
                    if (c is MonoBehaviour)
                    {
                        Object.DestroyImmediate(c);
                        strippedComponents++;
                    }
                foreach (var c in cg.components)
                    if (c != null && !(c is MonoBehaviour))
                    {
                        Object.DestroyImmediate(c);
                        strippedComponents++;
                    }
                cg.ApplyLayerChange(); // e.g. move off the Interactable layer
            }
            Object.DestroyImmediate(cg); // the gate itself is a build-time helper
        }

        if (strippedObjects > 0 || strippedComponents > 0)
            Debug.Log($"[FeatureFlags] Scene '{scene.name}' (web={isWeb}): stripped {strippedObjects} " +
                      $"object(s) and {strippedComponents} component(s).");
    }
}
