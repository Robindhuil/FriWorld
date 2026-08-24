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

    /// <summary>
    /// True when this is the Web target. Delegates to <see cref="PlatformFlags"/> so that in
    /// the editor it follows the active build target rather than the runtime platform —
    /// otherwise a Web-scoped flag would resolve one way in play mode and the other way in
    /// the actual build, which is exactly the parity <see cref="PlatformGate"/> guarantees.
    /// </summary>
    public static bool IsWeb => PlatformFlags.IsWeb;

    public static bool On(FeatureId id)
    {
        EnsureLoaded();
        if (!_flags.TryGetValue(id, out var flag) || !flag.enabled)
            return false;
        return ScopeMatches(flag.scope);
    }

    /// <summary>
    /// Same as <see cref="On(FeatureId)"/>, except that a flag which is not in the config at
    /// all (missing asset, entry not added yet) resolves to <paramref name="fallback"/>
    /// instead of OFF. Use this for flags whose safe default is ON, so a missing config
    /// cannot silently strip a feature from a shipping build. A flag that IS configured
    /// always wins — the fallback only covers "nobody has said anything about this".
    /// </summary>
    public static bool On(FeatureId id, bool fallback)
    {
        EnsureLoaded();
        if (!_flags.TryGetValue(id, out var flag))
            return fallback;
        return flag.enabled && ScopeMatches(flag.scope);
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
                             "All flags default OFF. Create it via FriWorld > Feature Flags > Create Config Asset.");
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
