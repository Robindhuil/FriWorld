using System.Collections.Generic;
using FriWorld.ObjectRegistry;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates colliders from the object type registry.
///
/// Which collider an object gets is decided by <c>ObjectTypes.json</c>, looked up by the type
/// key derived from the object's name. An object whose type is unknown, or known but not yet
/// filled in, is left exactly as it is and listed in the summary — it never inherits the
/// behaviour of a similarly named type. See
/// <c>docs/decisions/2026-08-04-object-type-registry.md</c>.
/// </summary>
public static class GenerateColliders
{
    [MenuItem("FriWorld/Generate/Colliders From Registry")]
    private static void GenerateFromSelectedHierarchy()
    {
        GameObject[] selectedRoots = Selection.gameObjects;
        if (selectedRoots == null || selectedRoots.Length == 0)
        {
            Debug.LogWarning("[GenerateColliders] No objects selected in Hierarchy.");
            return;
        }

        var prefixes = TypeRegistry.LoadPrefixes(ObjectRegistryMenu.PrefixesPath);
        var registry = TypeRegistry.Load(ObjectRegistryMenu.TypesPath);
        if (registry.types.Count == 0)
        {
            Debug.LogError("[GenerateColliders] " + ObjectRegistryMenu.TypesPath
                + " is empty. Run FriWorld > Registry > Seed Missing Types From Selection first, "
                + "otherwise every object would be skipped.");
            return;
        }

        int meshAdded = 0, boxAdded = 0, sphereAdded = 0;
        int outdatedRemoved = 0, ignored = 0, alreadyHad = 0;

        List<string> unresolved = new List<string>();
        List<string> badValues = new List<string>();
        HashSet<Transform> visited = new HashSet<Transform>();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Generate Colliders From Registry");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject root in selectedRoots)
        {
            if (root == null) continue;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !visited.Add(t)) continue;

                GameObject go = t.gameObject;

                // Only objects that actually carry geometry. A grouping transform named like a
                // wall would previously have been given a MeshCollider with no mesh behind it.
                if (!HasMeshSource(go)) continue;

                string typeKey = ObjectTypeKey.Derive(go.name, prefixes);
                var entry = registry.Find(typeKey);

                if (entry == null || !entry.IsDecided)
                {
                    unresolved.Add(typeKey + "   " + GetHierarchyPath(t));
                    continue;
                }

                switch (entry.collider)
                {
                    case "none":
                        outdatedRemoved += RemoveManagedColliders(go, true, true, true);
                        ignored++;
                        break;

                    case "mesh":
                        outdatedRemoved += RemoveManagedColliders(go, false, true, true);
                        if (go.GetComponent<MeshCollider>() == null)
                        {
                            Undo.AddComponent<MeshCollider>(go);
                            meshAdded++;
                        }
                        else alreadyHad++;
                        break;

                    case "box":
                        outdatedRemoved += RemoveManagedColliders(go, true, false, true);
                        BoxCollider box = go.GetComponent<BoxCollider>();
                        if (box == null)
                        {
                            box = Undo.AddComponent<BoxCollider>(go);
                            boxAdded++;
                        }
                        else alreadyHad++;

                        if (TryGetLocalBounds(go, out Bounds boxBounds))
                        {
                            box.center = boxBounds.center;
                            box.size = boxBounds.size;
                        }
                        break;

                    case "sphere":
                        outdatedRemoved += RemoveManagedColliders(go, true, true, false);
                        SphereCollider sphere = go.GetComponent<SphereCollider>();
                        if (sphere == null)
                        {
                            sphere = Undo.AddComponent<SphereCollider>(go);
                            sphereAdded++;
                        }
                        else alreadyHad++;

                        if (TryGetLocalBounds(go, out Bounds sphereBounds))
                        {
                            sphere.center = sphereBounds.center;
                            sphere.radius = sphereBounds.extents.magnitude;
                        }
                        break;

                    default:
                        badValues.Add(typeKey + "   collider=\"" + entry.collider + "\"");
                        break;
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"[GenerateColliders] Completed. Processed: {visited.Count}, "
                + $"Added Mesh: {meshAdded}, Added Box: {boxAdded}, Added Sphere: {sphereAdded}, "
                + $"Removed Outdated: {outdatedRemoved}, Set To None: {ignored}, "
                + $"Already Had Target Collider: {alreadyHad}, Left Untouched: {unresolved.Count}"
        );

        if (unresolved.Count > 0)
        {
            Debug.LogWarning("[GenerateColliders] " + unresolved.Count
                + " objects were left untouched because their type is unknown or undecided.\n"
                + "Run FriWorld > Registry > Report On Selection to see them grouped, or fix "
                + "them in " + ObjectRegistryMenu.TypesPath + ":\n"
                + string.Join("\n", unresolved.ToArray()));
        }

        if (badValues.Count > 0)
        {
            Debug.LogError("[GenerateColliders] Unrecognised collider values (expected none, mesh, "
                + "box or sphere):\n" + string.Join("\n", badValues.ToArray()));
        }
    }

    [MenuItem("FriWorld/Generate/Colliders From Registry", true)]
    private static bool ValidateGenerateFromSelectedHierarchy()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private static int RemoveManagedColliders(
        GameObject go,
        bool removeMesh,
        bool removeBox,
        bool removeSphere
    )
    {
        int removedCount = 0;

        if (removeMesh)
        {
            MeshCollider mesh = go.GetComponent<MeshCollider>();
            if (mesh != null)
            {
                Undo.DestroyObjectImmediate(mesh);
                removedCount++;
            }
        }

        if (removeBox)
        {
            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                Undo.DestroyObjectImmediate(box);
                removedCount++;
            }
        }

        if (removeSphere)
        {
            SphereCollider sphere = go.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Undo.DestroyObjectImmediate(sphere);
                removedCount++;
            }
        }

        return removedCount;
    }

    private static bool HasMeshSource(GameObject go)
    {
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return true;
        }

        SkinnedMeshRenderer skinned = go.GetComponent<SkinnedMeshRenderer>();
        if (skinned != null && skinned.sharedMesh != null)
        {
            return true;
        }

        return false;
    }

    private static bool TryGetLocalBounds(GameObject go, out Bounds localBounds)
    {
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            localBounds = meshFilter.sharedMesh.bounds;
            return true;
        }

        SkinnedMeshRenderer skinned = go.GetComponent<SkinnedMeshRenderer>();
        if (skinned != null)
        {
            localBounds = skinned.localBounds;
            return true;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            localBounds = WorldBoundsToLocalBounds(go.transform, renderer.bounds);
            return true;
        }

        localBounds = default;
        return false;
    }

    private static Bounds WorldBoundsToLocalBounds(Transform target, Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 ext = worldBounds.extents;

        Vector3[] worldCorners =
        {
            center + new Vector3(ext.x, ext.y, ext.z),
            center + new Vector3(ext.x, ext.y, -ext.z),
            center + new Vector3(ext.x, -ext.y, ext.z),
            center + new Vector3(ext.x, -ext.y, -ext.z),
            center + new Vector3(-ext.x, ext.y, ext.z),
            center + new Vector3(-ext.x, ext.y, -ext.z),
            center + new Vector3(-ext.x, -ext.y, ext.z),
            center + new Vector3(-ext.x, -ext.y, -ext.z),
        };

        Vector3 localMin = new Vector3(
            float.PositiveInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity
        );
        Vector3 localMax = new Vector3(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.NegativeInfinity
        );

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 local = target.InverseTransformPoint(worldCorners[i]);
            localMin = Vector3.Min(localMin, local);
            localMax = Vector3.Max(localMax, local);
        }

        Bounds result = new Bounds();
        result.SetMinMax(localMin, localMax);
        return result;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";

        string path = t.name;
        Transform current = t.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
