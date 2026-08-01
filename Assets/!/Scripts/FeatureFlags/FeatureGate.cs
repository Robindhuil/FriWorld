using UnityEngine;

/// <summary>
/// Enables/disables this GameObject at runtime based on a <see cref="FeatureId"/>.
/// Use for experimental / dynamically-toggled features (the object still ships in the
/// build, it is just (de)activated). For per-platform stripping that removes the object
/// from the build entirely, use <see cref="PlatformGate"/> instead.
///
/// The object must start ACTIVE in the scene for this to run.
/// </summary>
public class FeatureGate : MonoBehaviour
{
    [SerializeField] private FeatureId flag;

    [SerializeField, Tooltip("Active when the flag is ON (true) or when it is OFF (false).")]
    private bool activeWhenOn = true;

    private void Awake()
    {
        gameObject.SetActive(Features.On(flag) == activeWhenOn);
    }
}
