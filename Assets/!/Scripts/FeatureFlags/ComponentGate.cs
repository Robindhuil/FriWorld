using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Platform-gates specific COMPONENTS on a GameObject that must exist on both platforms.
/// e.g. a door that is interactable on desktop but not on web: strip its Door script,
/// Animator and AudioSource, and optionally move it off the "Interactable" layer so the
/// player's interaction raycast (which shows the outline + prompt) ignores it.
///
/// Real build (platform mismatch): listed components are removed + layer changed.
/// Editor play mode (mismatch): the same is applied at Awake, so play mode matches the build.
/// </summary>
public class ComponentGate : MonoBehaviour
{
    [Tooltip("Keep the listed components only on this platform; strip them on the other.")]
    public PlatformGate.Target target = PlatformGate.Target.DesktopOnly;

    [Tooltip("Components REMOVED when the platform doesn't match 'target'. " +
             "Drag any component here — scripts, Animator, AudioSource, colliders, renderers...")]
    public List<Component> components = new List<Component>();

    [Tooltip("When components are stripped, also move this object (and its children) to another " +
             "layer — e.g. off 'Interactable' so the interaction raycast ignores it.")]
    public bool changeLayerWhenStripped = false;

    [Layer]
    [Tooltip("Layer to move the object to when stripped.")]
    public int strippedLayer = 0; // 0 = Default

    private void Awake()
    {
        if (!PlatformFlags.Keep(target))
        {
            // Match the build: actually REMOVE the components (not just disable them), so
            // GetComponent<>() returns null here exactly like it will in the real build.
            foreach (var c in components)
                if (c != null)
                    Destroy(c);

            ApplyLayerChange();
        }

        Destroy(this); // the gate has done its job — remove it (matches the build)
    }

    /// <summary>Moves the object (and its whole subtree) to the configured layer, if enabled.</summary>
    public void ApplyLayerChange()
    {
        if (!changeLayerWhenStripped)
            return;

        SetLayerRecursively(gameObject, strippedLayer);
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
