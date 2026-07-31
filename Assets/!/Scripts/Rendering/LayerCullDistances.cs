using UnityEngine;

/// <summary>
/// Per-layer distance culling ("poor man's LOD"): objects on the listed detail layers
/// stop rendering beyond <see cref="detailCullDistance"/>. Use for small props (lamps,
/// signs, furniture) that you can't make out from afar anyway — they stop adding draw
/// calls / triangles when you look at the building from a distance. The big structural
/// geometry (walls, building) is on other layers and keeps the full far-clip distance.
///
/// This needs NO reduced-detail meshes (unlike true mesh LOD). It only culls; it does
/// not swap geometry. Tune <see cref="detailCullDistance"/> and the layer list to taste.
/// Spherical culling is used so the cut is distance-based (consistent while you look around).
/// </summary>
[RequireComponent(typeof(Camera))]
public class LayerCullDistances : MonoBehaviour
{
    [Tooltip("Detail layers to cull at distance. Structural layers (walls/building) must NOT be here.")]
    [SerializeField] private string[] detailLayerNames = { "NoObstacle", "Interactable" };

    [Tooltip("Objects on the detail layers stop rendering beyond this distance (metres).")]
    [SerializeField] private float detailCullDistance = 60f;

    private void OnEnable()
    {
        Apply();
    }

    /// <summary>Re-applies the cull distances. Call after changing the fields at runtime.</summary>
    public void Apply()
    {
        var cam = GetComponent<Camera>();
        if (cam == null) return;

        // 0 = "use the camera's far clip" (no per-layer cull) for every layer by default.
        float[] distances = new float[32];

        foreach (string layerName in detailLayerNames)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0 && layer < 32)
                distances[layer] = Mathf.Max(0f, detailCullDistance);
        }

        cam.layerCullSpherical = true;
        cam.layerCullDistances = distances;
    }
}
