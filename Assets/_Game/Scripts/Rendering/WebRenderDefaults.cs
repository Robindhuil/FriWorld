using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Render policy that differs on the Web build.
///
/// Web runs on whatever GPU the browser picked — frequently an integrated one that shares
/// (often single-channel) system memory. Those are bandwidth-bound, so the things that hurt
/// are full-screen passes and per-sample cost, not scene complexity. Anything expressible in
/// an asset lives in RP_Web.asset / the "Web" quality level; this class holds the parts that
/// can only be decided at runtime.
/// </summary>
public static class WebRenderDefaults
{
    /// <summary>
    /// Whether Bloom / Depth of Field / Motion Blur may run. Each is several full-screen
    /// passes, which is exactly what an integrated GPU cannot afford.
    ///
    /// The <see cref="FeatureId.HeavyPostProcessing"/> flag is the source of truth, so this
    /// stays designer-controlled. The fallback only applies when the flag is absent from the
    /// config: on, except on Web. That way a missing FeatureFlags asset degrades to sane
    /// behaviour instead of stripping the effects from the desktop build too.
    ///
    /// Colour grading and tonemapping are deliberately NOT gated — they ride along in the
    /// single uber post pass at negligible cost and they are the game's look.
    /// </summary>
    public static bool HeavyPostProcessing
        => Features.On(FeatureId.HeavyPostProcessing, fallback: !PlatformFlags.IsWeb);

    /// <summary>Name of the quality level that carries the Web pipeline asset.</summary>
    public const string WebQualityLevelName = "Web";

    /// <summary>
    /// Index of the <see cref="WebQualityLevelName"/> quality level, or -1 if it is gone.
    /// Every other level points at a desktop-authored pipeline asset, so selecting one on Web
    /// silently discards the whole Web render configuration.
    /// </summary>
    public static int WebQualityLevel
    {
        get
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
                if (names[i] == WebQualityLevelName)
                    return i;
            return -1;
        }
    }

    /// <summary>
    /// Applies the per-camera bits. On Web this swaps MSAA (disabled in RP_Web.asset, because
    /// it multiplies the bandwidth of every colour and depth sample) for FXAA, which is a
    /// single cheap post pass and keeps edges from crawling. Desktop is left alone — it gets
    /// MSAA from its own pipeline asset.
    /// </summary>
    public static void ApplyToCamera(Camera cam)
    {
        if (cam == null || !PlatformFlags.IsWeb)
            return;

        var data = cam.GetUniversalAdditionalCameraData();
        if (data == null)
            return;

        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        data.antialiasingQuality = AntialiasingQuality.Low;
    }
}
