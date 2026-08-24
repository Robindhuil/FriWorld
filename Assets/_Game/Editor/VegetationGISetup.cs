using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

// Stops grass/vegetation from bouncing its saturated green into the building's
// baked lightmaps. It does this by removing the "Contribute GI" static flag from
// renderers that use a vegetation material. The grass still renders normally (vivid
// green) but is no longer used as a light-bounce source by the lightmapper.
// A rebake is required for the change to take effect.
public class VegetationGISetup : EditorWindow
{
    // Material name fragments that identify vegetation (case-insensitive)
    private static readonly string[] VegKeywords =
        { "grass", "bush", "leaf", "hedge", "veget", "trava", "ker", "plant", "flower" };

    [MenuItem("FriWorld/Lighting/Vegetation: Disable GI Contribution")]
    public static void DisableVegetationGI()
    {
        Run(false);
    }

    [MenuItem("FriWorld/Lighting/Vegetation: Restore GI Contribution")]
    public static void EnableVegetationGI()
    {
        Run(true);
    }

    private static bool IsVegName(string n)
    {
        n = n.ToLowerInvariant();
        foreach (var k in VegKeywords)
            if (n.Contains(k)) return true;
        return false;
    }

    private static void Run(bool contribute)
    {
        var renderers = Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int changed = 0;
        var hit = new List<GameObject>();
        var report = new StringBuilder();

        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            if (mats == null) continue;

            bool isVeg = false;
            foreach (var m in mats)
            {
                if (m != null && IsVegName(m.name)) { isVeg = true; break; }
            }
            if (!isVeg) continue;

            hit.Add(r.gameObject);
            if (changed < 40)
                report.AppendLine($"   {r.gameObject.name}");
            changed++;
        }

        if (hit.Count > 0)
        {
            Undo.RecordObjects(hit.ToArray(), "Toggle Vegetation GI");
            foreach (var go in hit)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if (contribute) flags |= StaticEditorFlags.ContributeGI;
                else            flags &= ~StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                EditorUtility.SetDirty(go);
            }
        }

        string state = contribute ? "ON" : "OFF";
        Debug.Log($"[VegetationGI] Set Contribute GI = {state} on {changed} vegetation renderers.\n{report}");
        EditorUtility.DisplayDialog("Vegetation GI",
            $"Contribute GI = {state} on {changed} vegetation objects.\n\n" +
            "Now REBAKE lighting — the green bounce will be gone from the building.",
            "OK");
    }
}
