using UnityEngine;
using UnityEngine.Rendering;

// Cheap fake contact shadow: a soft dark oval projected on the ground beneath the
// object. Works everywhere (indoors too) regardless of real lights, in realtime,
// and conforms to sloped ground. Attach to an NPC / player / door root.
//
// Set "Ground Mask" to the ground/building layers (exclude the character's own
// layer) so the downward ray finds the floor, not the character itself.
[ExecuteAlways]
[DisallowMultipleComponent]
public class BlobShadow : MonoBehaviour
{
    [Tooltip("Diameter of the shadow oval in meters.")]
    public float size = 1.1f;

    [Tooltip("How far down to search for ground.")]
    public float castDistance = 4f;

    [Range(0f, 1f), Tooltip("Darkness of the shadow at contact.")]
    public float strength = 0.5f;

    [Tooltip("Which layers count as ground. Exclude the character's own layer.")]
    public LayerMask groundMask = ~0;

    private GameObject blob;
    private Transform blobT;
    private MeshRenderer blobMR;
    private Material mat;
    private static Texture2D sharedTex;

    private void OnEnable() { EnsureBlob(); }
    private void OnDisable() { DestroyBlob(); }

    private void EnsureBlob()
    {
        if (blob != null) return;

        blob = GameObject.CreatePrimitive(PrimitiveType.Quad);
        blob.name = "_BlobShadow";
        blob.hideFlags = HideFlags.HideAndDontSave;

        var col = blob.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        blobT = blob.transform;
        blobMR = blob.GetComponent<MeshRenderer>();
        blobMR.shadowCastingMode = ShadowCastingMode.Off;
        blobMR.receiveShadows = false;
        blobMR.lightProbeUsage = LightProbeUsage.Off;
        blobMR.reflectionProbeUsage = ReflectionProbeUsage.Off;

        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(sh);
        // configure as alpha-blended transparent
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f); // double sided
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetTexture("_BaseMap", GetTex());
        mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, strength));
        blobMR.sharedMaterial = mat;
    }

    private void DestroyBlob()
    {
        if (blob == null) return;
        if (Application.isPlaying) Destroy(blob);
        else DestroyImmediate(blob);
        blob = null;
    }

    private static Texture2D GetTex()
    {
        if (sharedTex != null) return sharedTex;
        const int s = 64;
        sharedTex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f;
                float dy = (y + 0.5f) / s - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 center .. 1 edge
                float a = Mathf.Clamp01(1f - d);
                a *= a; // soft falloff
                sharedTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        sharedTex.Apply();
        return sharedTex;
    }

    private void LateUpdate()
    {
        if (blob == null) EnsureBlob();

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            castDistance + 0.5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (!blob.activeSelf) blob.SetActive(true);
            blobT.position = hit.point + hit.normal * 0.02f;
            blobT.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            blobT.localScale = new Vector3(size, size, 1f);

            float fade = 1f - Mathf.Clamp01(hit.distance / (castDistance + 0.5f));
            mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, strength * Mathf.Clamp01(fade + 0.25f)));
        }
        else if (blob.activeSelf)
        {
            blob.SetActive(false);
        }
    }
}
