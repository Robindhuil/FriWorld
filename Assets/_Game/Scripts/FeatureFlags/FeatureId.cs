/// <summary>
/// Named feature flags queried from code via <see cref="Features.On"/> and by
/// <see cref="FeatureGate"/> components. Add new entries here, then configure them
/// in the FeatureFlags config asset (Tools > Feature Flags > Create Config Asset).
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
}
