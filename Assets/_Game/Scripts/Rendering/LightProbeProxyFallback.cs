using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drops every Light Probe Proxy Volume back to plain probe blending on devices that cannot
/// run one.
///
/// Doors are lit from probes, and a single blended probe is one sample taken at the renderer's
/// bounds centre. Measured across the 284 doors, the interpolated probe varies 2.10x on average
/// inside a door's own volume and up to 15.86x on the worst one — a door standing in a threshold
/// has daylight on one side and a dark corridor on the other, and one sample throws that away.
/// A proxy volume samples a small grid instead, so the door is bright where the light is.
///
/// LPPV needs 3D texture support. It is there on desktop and on WebGL 2, but the check is about
/// the device rather than the platform, so it belongs at runtime rather than in a build-time
/// gate. Without the fallback an unsupported device renders the door unlit, which is far worse
/// than the single sample this gives it back.
/// </summary>
public static class LightProbeProxyFallback
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        if (LightProbeProxyVolume.isFeatureSupported) return;

        var volumes = Object.FindObjectsByType<LightProbeProxyVolume>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (volumes.Length == 0) return;

        int downgraded = 0;
        for (int i = 0; i < volumes.Length; i++)
        {
            // The renderer decides where its probe data comes from, so it is the thing that has
            // to change; disabling the volume alone would leave the renderer asking for one.
            var renderer = volumes[i].GetComponent<Renderer>();
            if (renderer != null && renderer.lightProbeUsage == LightProbeUsage.UseProxyVolume)
            {
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                downgraded++;
            }
            volumes[i].enabled = false;
        }

        Debug.Log("[LightProbeProxyFallback] Proxy volumes are not supported on this device. "
            + downgraded + " renderers fell back to blended probes.");
    }
}
