using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FriWorld.Navigator.Editor
{
    /// <summary>
    /// The bake steps of the Navigator, in the order they run. They work on the open
    /// FriNavigator scene and only write to the scene and to <c>Assets/_Navigator/Data/</c> —
    /// never to the shared FriBuilding prefab.
    /// </summary>
    public static class NavigatorBake
    {
        private const string ScenePath = "Assets/_Navigator/Scenes/FriNavigator.unity";
        private const string DataFolder = "Assets/_Navigator/Data";
        private const string NavMeshAssetPath = DataFolder + "/NavigatorNavMesh.asset";
        public const string AnchorsAssetPath = DataFolder + "/RoomAnchors.asset";
        private const string ReflectionAssetPath = DataFolder + "/NavigatorReflection.exr";
        private const string SurfaceObjectName = "NavigatorNavMesh";
        private const string BuildingName = "FriBuilding";

        // Humanoid: the same agent FriWorld's player navmesh uses, baked here into our own data.
        private const int AgentTypeID = 0;

        // FriWorld's hand-placed point per room code, under the building.
        private const string RoomPointsName = "RoomPoints";
        private const string DoorTag = "Door";

        /// <summary>Farthest a room point stands from the leaf of the door it marks (m).</summary>
        public const float DoorwayReach = 1f;

        // Floors are over 3.5 m apart; a door this far above or below the point is on another one.
        private const float FloorTolerance = 1f;

        [MenuItem("Navigator/1 — Bake NavMesh", priority = 1)]
        public static void BakeNavMesh()
        {
            if (!TryGetScene(out Scene scene, out Transform building))
                return;

            // FriWorld's own surfaces would load their data here too and compete for the same
            // agent type. Switched off in this scene only; the prefab is untouched.
            Transform worldNavMesh = building.Find("NavMesh");
            if (worldNavMesh != null && worldNavMesh.gameObject.activeSelf)
            {
                worldNavMesh.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(worldNavMesh.gameObject);
            }

            var surfaceObject = scene.GetRootGameObjects().FirstOrDefault(g => g.name == SurfaceObjectName)
                                ?? new GameObject(SurfaceObjectName);
            // No ?? here: in the editor a missing component is a fake null, not a C# null.
            var surface = surfaceObject.GetComponent<NavMeshSurface>();
            if (surface == null)
                surface = surfaceObject.AddComponent<NavMeshSurface>();
            surface.agentTypeID = AgentTypeID;
            surface.collectObjects = CollectObjects.All;
            surface.layerMask = LayerMask.GetMask("Obstacle", "Nav");   // floors and blockers, as FriWorld bakes them
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.defaultArea = 0;
            surface.BuildNavMesh();

            EnsureDataFolder();
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null)
                AssetDatabase.DeleteAsset(NavMeshAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshAssetPath);

            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var tri = NavMesh.CalculateTriangulation();
            Debug.Log($"[Navigator] NavMesh baked: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangles → {NavMeshAssetPath}");
        }

        [MenuItem("Navigator/2 — Bake Room Anchors", priority = 2)]
        public static void BakeRoomAnchors()
        {
            if (!TryGetScene(out Scene scene, out Transform building))
                return;

            var filter = new NavMeshQueryFilter { agentTypeID = AgentTypeID, areaMask = NavMesh.AllAreas };
            if (NavMesh.CalculateTriangulation().vertices.Length == 0)
            {
                Debug.LogError("[Navigator] No navmesh loaded. Run Navigator → 1 — Bake NavMesh first.");
                return;
            }

            // Where each flight ends is FriWorld's RoomPoints: one point per room code, placed by hand
            // at the room's entrance — for an office reached through another room, at that room's
            // door in the corridor. The point's name is the code, whatever the containers and signs
            // around it are called.
            Transform roomPoints = building.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == RoomPointsName);
            if (roomPoints == null)
            {
                Debug.LogError($"[Navigator] {RoomPointsName} is not under {BuildingName}.");
                return;
            }

            // The start is wherever the Navigator camera is placed; paths are checked from below it.
            if (!TryFindStart(scene, filter, out Vector3 start))
                return;

            var doors = new List<Transform>();
            var leaves = new List<DoorLeaf>();
            foreach (Transform t in building.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag(DoorTag) && DoorLeaf.TryMeasure(t, out DoorLeaf leaf))
                {
                    doors.Add(t);
                    leaves.Add(leaf);
                }
            }

            var report = new StringBuilder();
            var anchors = new List<RoomAnchors.Anchor>();
            var onPoint = new List<string>();
            var unreachable = new List<string>();
            var path = new NavMeshPath();

            foreach (Transform point in roomPoints.GetComponentsInChildren<Transform>(true)
                                                  .Where(t => t != roomPoints && t.childCount == 0)
                                                  .OrderBy(t => t.name))
            {
                var anchor = new RoomAnchors.Anchor { code = RoomAnchors.Normalize(point.name) };
                bool found;
                if (TryFindDoorway(point.position, leaves, out int door))
                {
                    found = TryAnchorAtDoor(doors[door], leaves[door], start, filter, path, ref anchor);
                }
                else
                {
                    found = TryAnchorOnPoint(point.position, start, filter, path, ref anchor);
                    onPoint.Add(anchor.code);
                }

                if (found)
                    anchors.Add(anchor);
                else
                    unreachable.Add(anchor.code);
            }

            EnsureDataFolder();
            var asset = AssetDatabase.LoadAssetAtPath<RoomAnchors>(AnchorsAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RoomAnchors>();
                AssetDatabase.CreateAsset(asset, AnchorsAssetPath);
            }

            asset.agentTypeID = AgentTypeID;
            asset.anchors = anchors;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            report.Insert(0, $"[Navigator] Anchored {anchors.Count} rooms from {RoomPointsName} → {AnchorsAssetPath}\n");
            if (unreachable.Count > 0)
                report.AppendLine($"UNREACHABLE ({unreachable.Count}): {string.Join(" ", unreachable)}");
            if (onPoint.Count > 0)
                report.AppendLine($"NO DOOR AT THE POINT ({onPoint.Count}), the flight ends on it facing the way it came: {string.Join(" ", onPoint)}");

            if (unreachable.Count > 0)
                Debug.LogWarning(report.ToString());
            else
                Debug.Log(report.ToString());
        }

        [MenuItem("Navigator/3 — Bake Sky Reflection", priority = 3)]
        public static void BakeSkyReflection()
        {
            if (!TryGetScene(out Scene scene, out _))
                return;

            // Without baked lighting Unity generates no sky reflection, and the skybox texture used
            // as a custom one has no convolved mips, so every surface, however rough, mirrors the
            // clouds. A probe that sees nothing but the sky bakes the cubemap Unity would generate.
            var probeObject = new GameObject("SkyReflectionProbe");
            try
            {
                var probe = probeObject.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Baked;
                probe.resolution = 128;
                probe.hdr = true;
                probe.cullingMask = 0;
                probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                EnsureDataFolder();
                if (!Lightmapping.BakeReflectionProbe(probe, ReflectionAssetPath))
                {
                    Debug.LogError("[Navigator] Baking the sky reflection failed.");
                    return;
                }
            }
            finally
            {
                Object.DestroyImmediate(probeObject);
            }

            // Custom, not Skybox: Skybox without baked lighting data makes URP's
            // ReflectionProbeManager throw and the frame comes out blank.
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionAssetPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Navigator] Sky reflection baked → {ReflectionAssetPath}");
        }

        /// <summary>The navmesh point below the camera the NavigatorController flies.</summary>
        private static bool TryFindStart(Scene scene, NavMeshQueryFilter filter, out Vector3 start)
        {
            start = default;
            var controller = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<NavigatorController>(true))
                .FirstOrDefault();
            var camera = controller != null
                ? new SerializedObject(controller).FindProperty("flyCamera").objectReferenceValue as Camera
                : null;
            if (camera == null)
            {
                Debug.LogError("[Navigator] No NavigatorController with a camera in the scene; where that camera stands is the start.");
                return false;
            }

            if (!NavMesh.SamplePosition(camera.transform.position, out NavMeshHit hit, 3f, filter))
            {
                Debug.LogError($"[Navigator] The camera at {camera.transform.position} is not above the navmesh.");
                return false;
            }

            start = hit.position;
            return true;
        }

        /// <summary>
        /// The door a room point stands in: the nearest leaf on the point's floor, at most
        /// <see cref="DoorwayReach"/> away. The points are placed by hand on the threshold, a few
        /// centimetres from the leaf; one farther out marks no door.
        /// </summary>
        public static bool TryFindDoorway(Vector3 point, IReadOnlyList<DoorLeaf> doors, out int index)
        {
            index = -1;
            float nearest = DoorwayReach;
            for (int i = 0; i < doors.Count; i++)
            {
                if (Mathf.Abs(doors[i].bottom - point.y) > FloorTolerance)
                    continue;
                float distance = Flat(doors[i].center - point).magnitude;
                if (distance > nearest)
                    continue;
                nearest = distance;
                index = i;
            }

            return index >= 0;
        }

        /// <summary>
        /// Stands <see cref="RoomAnchors.DoorDistance"/> in front of the door, on whichever side the
        /// start reaches first, facing it. For an office behind another room that is the corridor
        /// side of that room's door.
        /// </summary>
        private static bool TryAnchorAtDoor(Transform door, DoorLeaf leaf, Vector3 start, NavMeshQueryFilter filter,
                                            NavMeshPath path, ref RoomAnchors.Anchor anchor)
        {
            bool found = false;
            float best = float.MaxValue;
            foreach (float side in new[] { 1f, -1f })
            {
                // Far enough back that the door and its sign fit in the last frame.
                Vector3 probe = leaf.center + leaf.normal * (RoomAnchors.DoorDistance * side) + Vector3.down;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.2f, filter))
                    continue;
                if (Mathf.Abs(hit.position.y - probe.y) > 0.8f || Flat(hit.position - probe).magnitude > 0.7f)
                    continue;   // snapped to another floor or through the wall
                if (Physics.Linecast(hit.position + Vector3.up * 1.6f, leaf.center, out RaycastHit blocker) && !blocker.transform.IsChildOf(door))
                    continue;   // the door cannot be seen from there, e.g. across a narrow corridor
                if (!NavMesh.CalculatePath(start, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                    continue;

                float length = PathLength(path);
                if (length >= best)
                    continue;
                best = length;
                found = true;
                anchor.position = hit.position;
                anchor.facing = Flat(leaf.center - hit.position).normalized;
            }

            return found;
        }

        /// <summary>A point that marks no door: the flight ends on it, facing the way it came.</summary>
        private static bool TryAnchorOnPoint(Vector3 point, Vector3 start, NavMeshQueryFilter filter,
                                             NavMeshPath path, ref RoomAnchors.Anchor anchor)
        {
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 1f, filter))
                return false;
            if (!NavMesh.CalculatePath(start, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                return false;

            Vector3[] corners = path.corners;
            Vector3 arrival = corners.Length >= 2 ? Flat(corners[corners.Length - 1] - corners[corners.Length - 2]) : Vector3.zero;
            anchor.position = hit.position;
            anchor.facing = arrival.sqrMagnitude > 1e-6f ? arrival.normalized : Vector3.forward;
            return true;
        }

        private static float PathLength(NavMeshPath path)
        {
            float length = 0f;
            var corners = path.corners;
            for (int i = 1; i < corners.Length; i++)
                length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private static bool TryGetScene(out Scene scene, out Transform building)
        {
            scene = SceneManager.GetActiveScene();
            building = null;
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[Navigator] Open {ScenePath} first (active: {scene.path}).");
                return false;
            }

            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == BuildingName);
            if (root == null)
            {
                Debug.LogError($"[Navigator] {BuildingName} is not in the scene.");
                return false;
            }

            building = root.transform;
            return true;
        }

        private static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/_Navigator", "Data");
        }
    }
}
