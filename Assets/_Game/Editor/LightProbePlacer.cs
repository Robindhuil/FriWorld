using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Generates the Light Probe field that lights every dynamic object — doors, NPCs, props.
///
/// Probes are placed on the NavMesh, not on a world-space lattice. The lattice was the reason
/// dynamic objects looked lit differently from the static geometry beside them: it ran from the
/// building's bounding box in fixed 2.5 m steps, so its twelve heights (-5.2, -2.7, -0.2, 2.3,
/// 4.8 …) had no relation to where the floors actually are. One storey happened to get a probe
/// 0.3 m above its floor, the next one got its nearest probe 1.8 m up, and a door standing on
/// that floor was then lit from mid-air with none of the bounce off the floor in front of it.
/// The NavMesh is by definition the set of surfaces things stand on, so probes derived from it
/// sit at the same height above every floor in the building.
///
/// Two probes per column, low and high, bracket a door: they are 1.87 m to 2.51 m tall here, so
/// the interpolation the renderer does at its bounds centre lands between the pair rather than
/// extrapolating past the top one.
///
/// The old collider check is gone as well. It dropped any probe within 0.3 m of any collider,
/// which in a furnished building meant deleting them out of doorways and around furniture — the
/// exact places where a door or an NPC needs one. A point sampled from the NavMesh is in open
/// walkable space already.
///
/// After generating: REBAKE lighting, or the probes hold no GI at all.
/// </summary>
public class LightProbePlacer : EditorWindow
{
    private const string GroupName = "AutoLightProbes";

    private float spacing = 3f;        // horizontal step between probe columns
    private float lowHeight = 0.3f;    // just off the floor, catches the bounce off it
    private float highHeight = 1.9f;   // above a door's centre, head height for NPCs
    private float floorMergeDistance = 0.5f;   // two hits closer than this are the same floor

    public const float DefaultSpacing = 3f;
    public const float DefaultLowHeight = 0.3f;
    public const float DefaultHighHeight = 1.9f;
    public const float DefaultFloorMergeDistance = 0.5f;

    [MenuItem("FriWorld/Lighting/Light Probes…")]
    public static void Open() => GetWindow<LightProbePlacer>("Light Probes");

    [MenuItem("FriWorld/Lighting/Generate Light Probes")]
    private static void GenerateWithDefaults()
        => Generate(DefaultSpacing, DefaultLowHeight, DefaultHighHeight, DefaultFloorMergeDistance);

    private void OnGUI()
    {
        GUILayout.Label("Light probes on the NavMesh", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        spacing = EditorGUILayout.FloatField("Column spacing", spacing);
        lowHeight = EditorGUILayout.FloatField("Low probe height", lowHeight);
        highHeight = EditorGUILayout.FloatField("High probe height", highHeight);
        floorMergeDistance = EditorGUILayout.FloatField("Floor merge distance", floorMergeDistance);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Needs a baked NavMesh. Rebake lighting afterwards, otherwise the probes carry no GI.",
            MessageType.Info);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Probe Grid"))
            Generate(spacing, lowHeight, highHeight, floorMergeDistance);
        if (GUILayout.Button("Remove Generated Probes")) Remove();
    }

    /// <summary>Every walkable height under one (x, z) column, one entry per storey.</summary>
    private static void CollectFloors(
        NavMeshTriangulation tri,
        List<int> candidateTriangles,
        float x,
        float z,
        float mergeDistance,
        List<float> into)
    {
        into.Clear();
        for (int t = 0; t < candidateTriangles.Count; t++)
        {
            int i = candidateTriangles[t] * 3;
            Vector3 a = tri.vertices[tri.indices[i]];
            Vector3 b = tri.vertices[tri.indices[i + 1]];
            Vector3 c = tri.vertices[tri.indices[i + 2]];

            if (!TryHeightAt(a, b, c, x, z, out float y)) continue;

            bool merged = false;
            for (int k = 0; k < into.Count; k++)
                if (Mathf.Abs(into[k] - y) < mergeDistance) { merged = true; break; }
            if (!merged) into.Add(y);
        }
    }

    /// <summary>Barycentric test on the XZ projection; the height comes out of the same weights.</summary>
    private static bool TryHeightAt(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float y)
    {
        y = 0f;
        float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
        if (Mathf.Abs(d) < 1e-6f) return false;   // degenerate seen straight down

        float w0 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
        float w1 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
        float w2 = 1f - w0 - w1;
        if (w0 < 0f || w1 < 0f || w2 < 0f) return false;

        y = w0 * a.y + w1 * b.y + w2 * c.y;
        return true;
    }

    /// <summary>Rebuilds the probe group. Public so the pipeline can call it without the window.</summary>
    public static int Generate(float spacing, float lowHeight, float highHeight, float floorMergeDistance)
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int triangleCount = tri.indices.Length / 3;
        if (triangleCount == 0)
        {
            EditorUtility.DisplayDialog("Light Probes",
                "The NavMesh is empty, so there is nothing to place probes on. Bake the NavMesh first.",
                "OK");
            return 0;
        }

        // Bucket the triangles by XZ cell once, otherwise every column would be tested against
        // all 20 000 of them.
        var bounds = new Bounds(tri.vertices[0], Vector3.zero);
        for (int i = 1; i < tri.vertices.Length; i++) bounds.Encapsulate(tri.vertices[i]);

        var buckets = new Dictionary<long, List<int>>();
        for (int t = 0; t < triangleCount; t++)
        {
            int i = t * 3;
            Vector3 a = tri.vertices[tri.indices[i]];
            Vector3 b = tri.vertices[tri.indices[i + 1]];
            Vector3 c = tri.vertices[tri.indices[i + 2]];
            int minX = Cell(Mathf.Min(a.x, Mathf.Min(b.x, c.x)), spacing);
            int maxX = Cell(Mathf.Max(a.x, Mathf.Max(b.x, c.x)), spacing);
            int minZ = Cell(Mathf.Min(a.z, Mathf.Min(b.z, c.z)), spacing);
            int maxZ = Cell(Mathf.Max(a.z, Mathf.Max(b.z, c.z)), spacing);
            for (int cx = minX; cx <= maxX; cx++)
                for (int cz = minZ; cz <= maxZ; cz++)
                {
                    long key = Key(cx, cz);
                    if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
                    list.Add(t);
                }
        }

        var positions = new List<Vector3>();
        var floors = new List<float>(8);
        var empty = new List<int>();
        int columns = 0;

        for (float x = bounds.min.x; x <= bounds.max.x; x += spacing)
            for (float z = bounds.min.z; z <= bounds.max.z; z += spacing)
            {
                if (!buckets.TryGetValue(Key(Cell(x, spacing), Cell(z, spacing)), out var candidates)) candidates = empty;
                if (candidates.Count == 0) continue;

                CollectFloors(tri, candidates, x, z, floorMergeDistance, floors);
                if (floors.Count == 0) continue;

                columns++;
                for (int f = 0; f < floors.Count; f++)
                {
                    positions.Add(new Vector3(x, floors[f] + lowHeight, z));
                    positions.Add(new Vector3(x, floors[f] + highHeight, z));
                }
            }

        if (positions.Count == 0)
        {
            EditorUtility.DisplayDialog("Light Probes",
                "0 probes generated. Is the column spacing larger than the walkable area?", "OK");
            return 0;
        }

        int gridProbes = positions.Count;
        int rescued = AddColumnsForDynamicRenderers(positions, spacing, lowHeight, highHeight);

        var existing = GameObject.Find(GroupName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var go = new GameObject(GroupName);
        go.transform.position = Vector3.zero;   // keeps local == world for probePositions
        Undo.RegisterCreatedObjectUndo(go, "Create Light Probes");
        var group = go.AddComponent<LightProbeGroup>();
        group.probePositions = positions.ToArray();
        EditorUtility.SetDirty(go);

        Debug.Log($"[LightProbes] {positions.Count} probes at floor +{lowHeight} m and +{highHeight} m: "
            + $"{gridProbes} over {columns} NavMesh columns {spacing} m apart, plus {rescued} under dynamic "
            + "renderers the NavMesh does not reach. REBAKE now.");
        return positions.Count;
    }

    /// <summary>
    /// Guarantees a probe column under everything the probes exist for.
    ///
    /// The grid only covers walkable ground, and 25 of the 284 doors turned out to stand where
    /// the NavMesh does not reach — service rooms, exterior doors, anything an agent never walks
    /// through. Those are still dynamic renderers that have to be lit, and a door left to
    /// extrapolate from probes two rooms away is exactly the mismatch this whole pass is meant
    /// to remove. The rule is deliberately about dynamic renderers rather than about doors:
    /// a prop with no probe near it has the same problem.
    /// </summary>
    private static int AddColumnsForDynamicRenderers(
        List<Vector3> positions, float spacing, float lowHeight, float highHeight)
    {
        // Existing columns hashed by XZ cell, so the near-duplicate test stays cheap.
        var occupied = new Dictionary<long, List<Vector3>>();
        foreach (var p in positions)
        {
            long key = Key(Cell(p.x, spacing), Cell(p.z, spacing));
            if (!occupied.TryGetValue(key, out var list)) occupied[key] = list = new List<Vector3>();
            list.Add(p);
        }

        float minSeparation = spacing * 0.5f;
        int added = 0;

        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject, StaticEditorFlags.ContributeGI)) continue;

            Vector3 centre = r.bounds.center;
            float floor = Physics.Raycast(centre, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : r.bounds.min.y;

            bool covered = false;
            for (int cx = -1; cx <= 1 && !covered; cx++)
                for (int cz = -1; cz <= 1 && !covered; cz++)
                {
                    if (!occupied.TryGetValue(Key(Cell(centre.x, spacing) + cx, Cell(centre.z, spacing) + cz), out var near))
                        continue;
                    foreach (var p in near)
                        if (Mathf.Abs(p.y - (floor + lowHeight)) < 1f
                            && new Vector2(p.x - centre.x, p.z - centre.z).sqrMagnitude < minSeparation * minSeparation)
                        { covered = true; break; }
                }
            if (covered) continue;

            var low = new Vector3(centre.x, floor + lowHeight, centre.z);
            var high = new Vector3(centre.x, floor + highHeight, centre.z);
            positions.Add(low);
            positions.Add(high);
            added += 2;

            long k = Key(Cell(low.x, spacing), Cell(low.z, spacing));
            if (!occupied.TryGetValue(k, out var bucket)) occupied[k] = bucket = new List<Vector3>();
            bucket.Add(low);
            bucket.Add(high);
        }

        return added;
    }

    private static int Cell(float v, float step) => Mathf.FloorToInt(v / Mathf.Max(step, 0.01f));

    private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

    private static void Remove()
    {
        var go = GameObject.Find(GroupName);
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
            Debug.Log("[LightProbes] Removed " + GroupName + ".");
        }
    }
}
