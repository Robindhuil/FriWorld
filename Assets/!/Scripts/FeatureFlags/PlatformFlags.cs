/// <summary>
/// Shared "is this the current platform?" helper used by <see cref="PlatformGate"/> and
/// <see cref="ComponentGate"/>. In the editor it follows the active build target (the
/// authoritative source; the UNITY_WEBGL define is unreliable there); in a real build it
/// uses the compile-time platform define.
/// </summary>
public static class PlatformFlags
{
    public static bool IsWeb
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL;
#elif UNITY_WEBGL
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>True if something gated to <paramref name="target"/> should be KEPT on the current platform.</summary>
    public static bool Keep(PlatformGate.Target target)
        => target == PlatformGate.Target.WebOnly ? IsWeb : !IsWeb;
}
