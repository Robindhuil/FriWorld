using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime entry point for feature flags. Usage:
/// <code>if (Features.On(FeatureId.DebugOverlay)) { ... }</code>
/// Resolves each flag from the <see cref="FeatureFlagConfig"/> asset in Resources,
/// combining its <c>enabled</c> bool with its <see cref="FlagScope"/> for the
/// current platform. Unknown/absent flags are treated as OFF.
/// </summary>
public static class Features
{
    /// <summary>Resources path of the config asset (Assets/Resources/FeatureFlags.asset).</summary>
    public const string ResourcePath = "FeatureFlags";

    static Dictionary<FeatureId, FeatureFlagConfig.Flag> _flags;

    /// <summary>True when running on the WebGL player.</summary>
    public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

    public static bool On(FeatureId id)
    {
        EnsureLoaded();
        if (!_flags.TryGetValue(id, out var flag) || !flag.enabled)
            return false;
        return ScopeMatches(flag.scope);
    }

    /// <summary>Drop the cached config so the next query reloads it (e.g. after edits).</summary>
    public static void Reload() => _flags = null;

    static void EnsureLoaded()
    {
        if (_flags != null)
            return;

        _flags = new Dictionary<FeatureId, FeatureFlagConfig.Flag>();
        var cfg = Resources.Load<FeatureFlagConfig>(ResourcePath);
        if (cfg == null)
        {
            Debug.LogWarning($"[Features] No FeatureFlagConfig at Resources/{ResourcePath}. " +
                             "All flags default OFF. Create it via Tools > Feature Flags > Create Config Asset.");
            return;
        }

        foreach (var f in cfg.flags)
            _flags[f.id] = f; // last one wins on duplicate ids
    }

    static bool ScopeMatches(FlagScope scope)
    {
        switch (scope)
        {
            case FlagScope.All:         return true;
            case FlagScope.WebOnly:     return IsWeb;
            case FlagScope.DesktopOnly: return !IsWeb;
            case FlagScope.EditorOnly:  return Application.isEditor;
            default:                    return false;
        }
    }
}
