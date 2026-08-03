/// <summary>
/// Where a feature flag is allowed to be active. Combined with the flag's
/// <c>enabled</c> bool by <see cref="Features"/> at runtime.
/// </summary>
public enum FlagScope
{
    /// <summary>Active on every platform (when enabled).</summary>
    All,
    /// <summary>Active only in standalone/editor builds, never on WebGL.</summary>
    DesktopOnly,
    /// <summary>Active only on WebGL.</summary>
    WebOnly,
    /// <summary>Active only inside the Unity editor.</summary>
    EditorOnly,
}
