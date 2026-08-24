using System;
using FriWorld.ObjectRegistry;
using UnityEngine;

/// <summary>
/// Runs a registry generator against the FriBuilding prefab asset.
///
/// The generators used to walk whatever was selected in the Hierarchy. Selecting the scene
/// instance meant every component they added became a prefab override, and a .blend reimport
/// wipes those — the same way it wiped 282 of the 283 door gates. Measured before this change:
/// 2671 added-component overrides sitting on the scene instance, mostly BoxCollider and
/// NavMeshModifier, all one reimport from being gone.
///
/// Working on the asset also means Undo does not apply, so the generators no longer register
/// undo steps. Git is the undo: run the tool, read the summary, check the diff.
///
/// See docs/decisions/2026-08-24-platform-gaty-v-prefabe.md.
/// </summary>
public static class PrefabTarget
{
    /// <summary>
    /// Opens the prefab, hands its root to <paramref name="body"/>, then saves and closes. The
    /// prefab is closed even when the body throws, because a leaked preview scene keeps the
    /// asset locked until the editor restarts.
    /// </summary>
    public static void Run(string tool, Action<GameObject> body)
    {
        GameObject contents = RoomGateScope.Open();
        if (contents == null)
        {
            Debug.LogError("[" + tool + "] could not open " + RoomGateScope.PrefabPath);
            return;
        }

        bool closed = false;
        try
        {
            body(contents);
            RoomGateScope.SaveAndClose(contents);
            closed = true;
        }
        finally
        {
            if (!closed) RoomGateScope.Close(contents);
        }
    }
}
