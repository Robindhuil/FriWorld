using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Text;

// Lets light pass through windows during lightmap bake by disabling shadow casting
// on renderers that use a glass material. The lightmapper treats shadow-casting
// meshes as opaque occluders regardless of material transparency, so this is the
// reliable way to flood interiors with sunlight through windows.
public class GlassShadowSetup : EditorWindow
{
    // Material name fragments that identify glass (case-insensitive)
    private static readonly string[] GlassKeywords = { "glass", "sklo" };

    [MenuItem("FriWorld/Lighting/Glass: Disable Shadow Casting")]
    public static void DisableGlassShadows()
    {
        Run(ShadowCastingMode.Off);
    }

    [MenuItem("FriWorld/Lighting/Glass: Restore Shadow Casting")]
    public static void EnableGlassShadows()
    {
        Run(ShadowCastingMode.On);
    }

    private static bool IsGlassName(string n)
    {
        n = n.ToLowerInvariant();
        foreach (var k in GlassKeywords)
            if (n.Contains(k)) return true;
        return false;
    }

    private static void Run(ShadowCastingMode mode)
    {
        var renderers = Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int changed = 0;
        var pure = new List<Renderer>();
        var mixed = new StringBuilder();
        int mixedCount = 0;

        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            bool hasGlass = false;
            bool hasNonGlass = false;
            foreach (var m in mats)
            {
                if (m == null) { hasNonGlass = true; continue; }
                if (IsGlassName(m.name)) hasGlass = true;
                else hasNonGlass = true;
            }

            if (!hasGlass) continue;

            if (hasNonGlass)
            {
                // Renderer mixes glass with other materials on one mesh -> can't
                // isolate shadow casting. Report it so we handle manually.
                mixedCount++;
                if (mixedCount <= 30)
                    mixed.AppendLine($"   {GetPath(r.transform)}  [{MatNames(mats)}]");
                continue;
            }

            // Glass-only renderer -> safe to toggle
            pure.Add(r);
        }

        if (pure.Count > 0)
        {
            Undo.RecordObjects(pure.ToArray(), "Toggle Glass Shadows");
            foreach (var r in pure)
            {
                r.shadowCastingMode = mode;
                EditorUtility.SetDirty(r);
                changed++;
            }
        }

        Debug.Log($"[GlassShadows] Set Cast Shadows = {mode} on {changed} glass-only renderers.");
        if (mixedCount > 0)
        {
            Debug.LogWarning(
                $"[GlassShadows] {mixedCount} renderers mix glass WITH other materials " +
                $"on the same mesh (can't isolate). Listed below:\n{mixed}");
        }

        EditorUtility.DisplayDialog("Glass Shadows",
            $"Cast Shadows = {mode} applied to {changed} glass-only renderers.\n\n" +
            (mixedCount > 0
                ? $"{mixedCount} mixed renderers were SKIPPED (see Console warning).\n\n"
                : "") +
            "Now rebake lighting (Generate Lighting).",
            "OK");
    }

    private static string MatNames(Material[] mats)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < mats.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(mats[i] != null ? mats[i].name : "null");
        }
        return sb.ToString();
    }

    private static string GetPath(Transform t)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }
}
