using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Detects when NPCs enter a proximity zone around a target object and fires an event.
/// Supports both physics-collider-based detection and NavMeshAgent polling (for agents without colliders).
/// Plain C# class — instantiate manually, call <see cref="Tick"/> each frame, call <see cref="Dispose"/> on cleanup.
/// </summary>
public class NpcProximityDetector
{
    /// <summary>Fired whenever an NPC enters the proximity zone (world position of the NPC).</summary>
    public event Action<Vector3> OnNpcEntered;

    private readonly string npcTag;
    private readonly bool detectNavMeshAgents;
    private readonly float navMeshPadding;
    private readonly GameObject boundsSource;

    private BoxCollider npcTriggerCollider;
    private GameObject npcTriggerObject;

    private readonly HashSet<int> previousNpcsInside = new HashSet<int>();
    private readonly HashSet<int> currentNpcsInside = new HashSet<int>();
    private NavMeshAgent[] npcAgentsCache = Array.Empty<NavMeshAgent>();
    private float nextNpcCacheRefreshTime;

    private const float NpcCacheRefreshInterval = 0.5f;
    private const float TriggerSizePadding = 0.1f;
    private const float TriggerHeight = 1f;

    public NpcProximityDetector(
        GameObject boundsSource,
        string npcTag = "npc",
        bool detectNavMeshAgents = true,
        float navMeshPadding = 0.1f)
    {
        this.boundsSource = boundsSource;
        this.npcTag = npcTag;
        this.detectNavMeshAgents = detectNavMeshAgents;
        this.navMeshPadding = navMeshPadding;

        BuildTriggerCollider();
        RefreshNpcAgentsCache();
    }

    /// <summary>Call this every frame from the owning MonoBehaviour's Update.</summary>
    public void Tick()
    {
        if (detectNavMeshAgents)
            DetectNpcEntriesFromNavMeshAgents();
    }

    /// <summary>Call this from the owning MonoBehaviour's OnDestroy to clean up the trigger object.</summary>
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

        TriggerForwarder forwarder = npcTriggerObject.AddComponent<TriggerForwarder>();
        forwarder.OnTriggerEnterAction = HandleTriggerEnter;
    }

    private void HandleTriggerEnter(Collider other)
    {
        if (!IsNpcByTag(other.transform))
            return;

        OnNpcEntered?.Invoke(other.transform.position);
    }

    private void DetectNpcEntriesFromNavMeshAgents()
    {
        if (!detectNavMeshAgents || npcTriggerCollider == null)
            return;

        if (Time.time >= nextNpcCacheRefreshTime)
            RefreshNpcAgentsCache();

        currentNpcsInside.Clear();

        foreach (NavMeshAgent agent in npcAgentsCache)
        {
            if (agent == null || !IsNpcByTag(agent.transform))
                continue;

            if (!IsInsideTrigger(agent))
                continue;

            int id = agent.GetInstanceID();
            currentNpcsInside.Add(id);

            if (!previousNpcsInside.Contains(id))
                OnNpcEntered?.Invoke(agent.transform.position);
        }

        previousNpcsInside.Clear();
        previousNpcsInside.UnionWith(currentNpcsInside);
    }

    private bool IsInsideTrigger(NavMeshAgent agent)
    {
        Bounds bounds = npcTriggerCollider.bounds;
        float padding = Mathf.Max(0f, agent.radius + navMeshPadding);
        Vector3 p = agent.transform.position;

        return p.x >= bounds.min.x - padding
            && p.x <= bounds.max.x + padding
            && p.z >= bounds.min.z - padding
            && p.z <= bounds.max.z + padding;
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

    private void RefreshNpcAgentsCache()
    {
        npcAgentsCache = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        nextNpcCacheRefreshTime = Time.time + NpcCacheRefreshInterval;
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
