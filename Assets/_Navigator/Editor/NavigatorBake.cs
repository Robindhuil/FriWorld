using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string AnchorsAssetPath = DataFolder + "/RoomAnchors.asset";
        private const string ReflectionAssetPath = DataFolder + "/NavigatorReflection.exr";
        private const string SurfaceObjectName = "NavigatorNavMesh";
        private const string BuildingName = "FriBuilding";

        // Humanoid: the same agent FriWorld's player navmesh uses, baked here into our own data.
        private const int AgentTypeID = 0;

        // A room with a code is a container named like "ra101"; its doors are "ra101_door_1", …
        private static readonly Regex RoomContainer = new Regex(@"^r[a-z]\d{3}$");

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

            var all = building.GetComponentsInChildren<Transform>(true);
            var report = new StringBuilder();

            // The start is wherever the Navigator camera is placed; paths are checked from below it.
            if (!TryFindStart(scene, filter, out Vector3 start))
                return;

            var anchors = new List<RoomAnchors.Anchor>();
            var noDoor = new List<string>();
            var unreachable = new List<string>();
            var path = new NavMeshPath();

            foreach (Transform room in all.Where(t => RoomContainer.IsMatch(t.name)).OrderBy(t => t.name))
            {
                var doorName = new Regex("^" + Regex.Escape(room.name) + @"_door_\d+$");
                var doors = room.Cast<Transform>().Where(c => doorName.IsMatch(c.name)).ToList();
                string code = RoomAnchors.Normalize(room.name);
                if (doors.Count == 0)
                {
                    noDoor.Add(code);
                    continue;
                }

                // Every door, both sides: the anchor is whichever point the start reaches first.
                bool found = false;
                float best = float.MaxValue;
                var anchor = new RoomAnchors.Anchor { code = code };
                foreach (Transform door in doors)
                {
                    if (!TryDoorFrame(door, out Vector3 center, out Vector3 normal))
                        continue;

                    foreach (float side in new[] { 1f, -1f })
                    {
                        // Far enough back that the door and its sign fit in the last frame.
                        Vector3 probe = center + normal * (RoomAnchors.DoorDistance * side) + Vector3.down;
                        if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.2f, filter))
                            continue;
                        if (Mathf.Abs(hit.position.y - probe.y) > 0.8f || Flat(hit.position - probe).magnitude > 0.7f)
                            continue;   // snapped to another floor or through the wall
                        if (Physics.Linecast(hit.position + Vector3.up * 1.6f, center, out RaycastHit blocker) && !blocker.transform.IsChildOf(door))
                            continue;   // the door cannot be seen from there, e.g. across a narrow corridor
                        if (!NavMesh.CalculatePath(start, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                            continue;

                        float length = PathLength(path);
                        if (length >= best)
                            continue;
                        best = length;
                        found = true;
                        anchor.position = hit.position;
                        anchor.facing = Flat(center - hit.position).normalized;
                    }
                }

                if (found)
                    anchors.Add(anchor);
                else
                    unreachable.Add(code);
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

            report.Insert(0, $"[Navigator] Anchored {anchors.Count} rooms → {AnchorsAssetPath}\n");
            if (unreachable.Count > 0)
                report.AppendLine($"UNREACHABLE ({unreachable.Count}): {string.Join(" ", unreachable)}");
            if (noDoor.Count > 0)
                report.AppendLine($"NO DOOR ({noDoor.Count}): {string.Join(" ", noDoor)}");

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

        /// <summary>World centre of a flat object (door leaf, sign) and the horizontal normal of its face.</summary>
        private static bool TryDoorFrame(Transform t, out Vector3 center, out Vector3 normal)
        {
            center = default;
            normal = default;
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            var filters = t.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
            if (renderers.Length == 0 || filters.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);
            center = bounds.center;

            // The thinnest axis of the biggest mesh is the one the face points along.
            MeshFilter main = filters.OrderByDescending(f => Vector3.Scale(f.sharedMesh.bounds.size, f.transform.lossyScale).sqrMagnitude).First();
            Vector3 size = Vector3.Scale(main.sharedMesh.bounds.size, main.transform.lossyScale);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            Vector3 axis = size.x <= size.y && size.x <= size.z ? Vector3.right
                         : size.y <= size.z ? Vector3.up
                         : Vector3.forward;
            normal = Flat(main.transform.TransformDirection(axis));
            if (normal.sqrMagnitude < 1e-6f)
                return false;
            normal.Normalize();
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
