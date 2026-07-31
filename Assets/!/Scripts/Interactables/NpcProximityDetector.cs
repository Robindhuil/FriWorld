using System;
using UnityEngine;

/// <summary>
/// Fires an event when an NPC enters a proximity zone around a target object (e.g. a door).
///
/// Detection is fully EVENT-DRIVEN: a trigger BoxCollider is built around the target and
/// <see cref="HandleTriggerEnter"/> fires when an NPC collider enters it. The physics
/// broadphase does the spatial work — zero per-frame cost on the door side.
///
/// Requirement: NPCs must have a Collider AND a (kinematic) Rigidbody. Without a Rigidbody
/// Unity sends no trigger messages — which is the whole reason the old per-frame NavMeshAgent
/// polling existed. That polling is gone now (it cost ~5ms + GC spread across 282 doors).
///
/// Plain C# class — instantiate manually, call <see cref="Dispose"/> on cleanup.
/// <see cref="Tick"/> is now a no-op, kept so existing callers stay valid.
/// </summary>
public class NpcProximityDetector
{
    /// <summary>Fired whenever an NPC enters the proximity zone (world position of the NPC).</summary>
    public event Action<Vector3> OnNpcEntered;

    private readonly string npcTag;
    private readonly GameObject boundsSource;

    private BoxCollider npcTriggerCollider;
    private GameObject npcTriggerObject;

    private const float TriggerSizePadding = 0.1f;
    private const float TriggerHeight = 1f;

    // detectNavMeshAgents / navMeshPadding are kept in the signature for call-site
    // compatibility but are no longer used (detection is event-driven now).
    public NpcProximityDetector(
        GameObject boundsSource,
        string npcTag = "npc",
        bool detectNavMeshAgents = true,
        float navMeshPadding = 0.1f)
    {
        this.boundsSource = boundsSource;
        this.npcTag = npcTag;

        BuildTriggerCollider();
    }

    /// <summary>No-op. Detection is event-driven via the trigger (see <see cref="HandleTriggerEnter"/>).</summary>
    public void Tick() { }

    /// <summary>Call from the owning MonoBehaviour's OnDestroy to clean up the trigger object.</summary>
    public void Dispose()
    {
        if (npcTriggerObject == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(npcTriggerObject);
        else
            UnityEngine.Object.DestroyImmediate(npcTriggerObject);
    }

    private void BuildTriggerCollider()
    {
        string triggerName = $"{boundsSource.name}_NpcTrigger_{boundsSource.GetInstanceID()}";
        Transform siblingParent = boundsSource.transform.parent;

        npcTriggerObject = new GameObject(triggerName);
        Transform triggerTransform = npcTriggerObject.transform;
        triggerTransform.SetParent(siblingParent, worldPositionStays: true);
        triggerTransform.rotation = Quaternion.Euler(0f, boundsSource.transform.eulerAngles.y, 0f);
        triggerTransform.localScale = Vector3.one;

        Bounds localBounds = ComputeMeshBoundsInSpace(triggerTransform);
        triggerTransform.position = triggerTransform.TransformPoint(localBounds.center);

        npcTriggerCollider = npcTriggerObject.AddComponent<BoxCollider>();
        npcTriggerCollider.isTrigger = true;
        npcTriggerCollider.center = Vector3.zero;
        npcTriggerCollider.size = new Vector3(
            Mathf.Max(0.01f, localBounds.size.x + TriggerSizePadding),
            TriggerHeight,
            Mathf.Max(0.01f, localBounds.size.z + TriggerSizePadding)
        );

        // No Rigidbody here on purpose: the NPCs carry the (kinematic) Rigidbody, so this
        // stays a cheap Static Trigger Collider.
        TriggerForwarder forwarder = npcTriggerObject.AddComponent<TriggerForwarder>();
        forwarder.OnTriggerEnterAction = HandleTriggerEnter;
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (!IsNpcByTag(other.transform))
            return;

        OnNpcEntered?.Invoke(other.transform.position);
    }

    private bool IsNpcByTag(Transform t)
    {
        if (t.CompareTag(npcTag))
            return true;

        if (t.root != null && t.root.CompareTag(npcTag))
            return true;

        Transform parent = t.parent;
        return parent != null && parent.CompareTag(npcTag);
    }

    private Bounds ComputeMeshBoundsInSpace(Transform targetSpace)
    {
        Renderer[] renderers = boundsSource.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Vector3 localPos = targetSpace.InverseTransformPoint(boundsSource.transform.position);
            return new Bounds(localPos, Vector3.one);
        }

        bool hasBounds = false;
        Bounds result = default;

        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(x, y, z));
                        Vector3 local = targetSpace.InverseTransformPoint(corner);

                        if (!hasBounds)
                        {
                            result = new Bounds(local, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            result.Encapsulate(local);
                        }
                    }
                }
            }
        }

        return result;
    }
}
