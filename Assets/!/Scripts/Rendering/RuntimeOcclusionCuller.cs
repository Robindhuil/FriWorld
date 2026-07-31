using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime CPU occlusion culling for what Unity's baked Umbra OC won't do here:
/// renders a group only if the camera can actually SEE it. Occluders = wall colliders
/// (the <see cref="occluderMask"/> layer, e.g. "Obstacle"); occludees = grouped objects
/// (rooms). Each tick a few rays go from the camera toward a group's bounds — if every
/// ray is blocked by a wall, the group's renderers are switched off.
///
/// Only <see cref="Renderer.enabled"/> is toggled, so colliders/scripts keep running
/// (a hidden room's walls still occlude others, the player won't fall through floors).
///
/// Setup: put on the player camera. By default it auto-collects every object that has a
/// component named <see cref="autoFindComponentName"/> ("RoomDisplay") as a group, and uses
/// the "Obstacle" layer as occluders — so it usually works with zero configuration.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RuntimeOcclusionCuller : MonoBehaviour
{
    [Header("Occluders (walls)")]
    [Tooltip("Layers that block sight. Must have colliders. Auto-set to 'Obstacle' if left empty.")]
    [SerializeField] private LayerMask occluderMask;

    [Header("What to cull (occludees)")]
    [Tooltip("Auto-collect every object that has a component with THIS name as a group (e.g. rooms via 'RoomDisplay'). Leave empty to use the manual list only.")]
    [SerializeField] private string autoFindComponentName = "RoomDisplay";
    [Tooltip("Extra groups (each root = one cullable group). Used on top of the auto-found ones.")]
    [SerializeField] private List<Transform> groupRoots = new List<Transform>();

    [Header("Tuning")]
    [SerializeField] private float updateInterval = 0.15f;
    [SerializeField] private float positionThreshold = 0.25f;
    [SerializeField] private float angleThreshold = 2f;
    [Tooltip("Shrinks the bounds sample points so wall-hugging objects aren't false-culled.")]
    [SerializeField] private float boundsPadding = 0.1f;

    private static readonly Vector3[] SampleDirs =
    {
        new Vector3( 0,  0,  0),
        new Vector3( 1,  1,  1), new Vector3( 1,  1, -1),
        new Vector3( 1, -1,  1), new Vector3( 1, -1, -1),
        new Vector3(-1,  1,  1), new Vector3(-1,  1, -1),
        new Vector3(-1, -1,  1), new Vector3(-1, -1, -1),
    };

    private class Group { public Renderer[] renderers; public bool visible = true; }

    private Camera cam;
    private readonly List<Group> groups = new List<Group>();
    private readonly Plane[] frustum = new Plane[6];
    private float nextUpdateTime;
    private Vector3 lastPos;
    private Quaternion lastRot;
    private bool firstPass = true;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (occluderMask == 0)
        {
            int obstacle = LayerMask.NameToLayer("Obstacle");
            if (obstacle >= 0) occluderMask = 1 << obstacle;
        }

        CollectGroups();
    }

    public void CollectGroups()
    {
        groups.Clear();
        var roots = new List<Transform>();

        if (!string.IsNullOrEmpty(autoFindComponentName))
        {
            Type t = FindType(autoFindComponentName);
            if (t != null)
            {
                foreach (var o in UnityEngine.Object.FindObjectsByType(t, FindObjectsSortMode.None))
                {
                    if (o is Component c) roots.Add(c.transform);
                }
            }
            else
            {
                Debug.LogWarning("[RuntimeOcclusionCuller] Component '" + autoFindComponentName + "' not found.");
            }
        }

        foreach (var r in groupRoots)
            if (r != null) roots.Add(r);

        foreach (var root in roots)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0) groups.Add(new Group { renderers = rends });
        }

        firstPass = true;
    }

    private void LateUpdate()
    {
        if (cam == null || groups.Count == 0) return;
        if (Time.unscaledTime < nextUpdateTime) return;
        nextUpdateTime = Time.unscaledTime + updateInterval;

        Vector3 pos = cam.transform.position;
        Quaternion rot = cam.transform.rotation;
        bool moved = (pos - lastPos).sqrMagnitude > positionThreshold * positionThreshold
                     || Quaternion.Angle(rot, lastRot) > angleThreshold;
        if (!moved && !firstPass) return;
        lastPos = pos; lastRot = rot; firstPass = false;

        GeometryUtility.CalculateFrustumPlanes(cam, frustum);

        foreach (var g in groups)
        {
            bool shouldShow = ComputeVisible(g, pos);
            if (shouldShow != g.visible)
            {
                for (int i = 0; i < g.renderers.Length; i++)
                    if (g.renderers[i] != null) g.renderers[i].enabled = shouldShow;
                g.visible = shouldShow;
            }
        }
    }

    private bool ComputeVisible(Group g, Vector3 camPos)
    {
        Bounds b = g.renderers[0].bounds;
        for (int i = 1; i < g.renderers.Length; i++) b.Encapsulate(g.renderers[i].bounds);

        if (b.Contains(camPos)) return true;                       // inside -> visible
        if (!GeometryUtility.TestPlanesAABB(frustum, b)) return false; // off-screen
        return !IsFullyOccluded(b, camPos);
    }

    private bool IsFullyOccluded(Bounds b, Vector3 camPos)
    {
        Vector3 ext = Vector3.Max(b.extents - Vector3.one * boundsPadding, Vector3.zero);
        Vector3 c = b.center;
        for (int i = 0; i < SampleDirs.Length; i++)
        {
            Vector3 p = c + Vector3.Scale(ext, SampleDirs[i]);
            Vector3 dir = p - camPos;
            float dist = dir.magnitude;
            if (dist <= 0.01f) return false;
            if (!Physics.Raycast(camPos, dir / dist, dist - 0.01f, occluderMask, QueryTriggerInteraction.Ignore))
                return false; // a sample is visible -> not fully occluded
        }
        return true;
    }

    private static Type FindType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(name);
            if (t != null) return t;
            Type[] types;
            try { types = asm.GetTypes(); } catch { continue; }
            foreach (var tt in types) if (tt.Name == name) return tt;
        }
        return null;
    }

    private void OnDisable()
    {
        foreach (var g in groups)
            for (int i = 0; i < g.renderers.Length; i++)
                if (g.renderers[i] != null) g.renderers[i].enabled = true;
    }
}
