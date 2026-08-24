/// <summary>
/// Named feature flags queried from code via <see cref="Features.On"/> and by
/// <see cref="FeatureGate"/> components. Add new entries here, then configure them
/// in the FeatureFlags config asset (FriWorld > Feature Flags > Create Config Asset).
///
/// NOTE: this is for CODE/experimental toggles. Pure platform gating of whole
/// scene objects/rooms is done with <see cref="PlatformGate"/> instead (build-time
/// strip), which does not need a FeatureId.
/// </summary>
public enum FeatureId
{
    // --- examples; rename/extend freely ---
    ExperimentalOcclusionCulling,
    DebugOverlay,
    WebFlatLighting,

    /// <summary>
    /// Full-screen post effects that cost real fill rate: Bloom, Depth of Field, Motion Blur.
    /// Off on Web (integrated GPUs are bandwidth-bound and these are several full-screen
    /// passes each). Colour grading / tonemapping are NOT covered by this flag — they fold
    /// into the single uber post pass and are effectively free, and they are the game's look.
    /// Queried through <see cref="WebRenderDefaults.HeavyPostProcessing"/>, never directly.
    /// </summary>
    HeavyPostProcessing,
}
