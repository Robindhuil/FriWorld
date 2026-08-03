using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a GameObject (or a whole room via its ROOT — the entire child subtree goes with
/// it) as belonging to a single platform. At build time <c>FeatureFlagBuildProcessor</c>
/// physically removes objects whose <see cref="target"/> does not match the build platform.
///
/// Use <see cref="exclude"/> to keep specific children even when the object is stripped —
/// e.g. a room you strip for web that still contains a door you want to keep. Those objects
/// are reparented out (keeping world position) before the rest of the subtree is removed,
/// so you don't have to put a gate on every single object.
///
/// In the editor the object stays visible so you can keep working on it; use
/// Tools > Feature Flags > Preview to simulate a platform (note: the edit-mode preview hides
/// the whole subtree — for an accurate check of excludes, use Play mode).
/// </summary>
public class PlatformGate : MonoBehaviour
{
    public enum Target
    {
        /// <summary>Kept only in desktop/standalone builds; stripped from WebGL.</summary>
        DesktopOnly,
        /// <summary>Kept only in WebGL builds; stripped from desktop.</summary>
        WebOnly,
    }

    [Tooltip("Which build this object (and its children) is included in.")]
    public Target target = Target.DesktopOnly;

    [Tooltip("Children to KEEP even when this object is stripped (e.g. a door inside a room " +
             "you strip for web). They are reparented out — keeping world position — before " +
             "the rest of the subtree is removed.")]
    public List<GameObject> exclude = new List<GameObject>();

    private void Awake()
    {
        // The build processor applies this at build time. It does NOT run in editor play
        // mode, so mirror it here so play mode matches the build exactly.
        if (!PlatformFlags.Keep(target))
        {
            RescueExcluded();
            Destroy(gameObject);   // stripped platform: remove the whole object
        }
        else
        {
            Destroy(this);         // kept: the gate has done its job, drop it
        }
    }

    /// <summary>
    /// Reparents the <see cref="exclude"/> objects out of this subtree (to this object's
    /// parent, keeping world position) so they survive when the subtree is removed.
    /// </summary>
    public void RescueExcluded()
    {
        Transform newParent = transform.parent;
        foreach (var go in exclude)
            if (go != null)
                go.transform.SetParent(newParent, worldPositionStays: true);
    }
}
