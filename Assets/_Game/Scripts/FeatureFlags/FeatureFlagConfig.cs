using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central, designer-editable list of feature flags. One asset lives at
/// <c>Assets/Resources/FeatureFlags.asset</c> so <see cref="Features"/> can load it
/// at runtime. Create it via FriWorld > Feature Flags > Create Config Asset.
/// </summary>
[CreateAssetMenu(fileName = "FeatureFlags", menuName = "Config/Feature Flags")]
public class FeatureFlagConfig : ScriptableObject
{
    [Serializable]
    public struct Flag
    {
        public FeatureId id;
        [Tooltip("Master on/off. Experimental features usually start OFF.")]
        public bool enabled;
        [Tooltip("Which platform(s) the flag may be active on (AND-ed with 'enabled').")]
        public FlagScope scope;
        [TextArea(1, 3)] public string note;
    }

    public List<Flag> flags = new List<Flag>();
}
