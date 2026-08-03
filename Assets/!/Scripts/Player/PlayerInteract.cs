using UnityEngine;
using UnityEngine.Rendering;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask; // set it to obstacles and interactables layers in inspector
    private PlayerUI playerUI;
    private InputManager inputManager;

    private Outline currentOutlinedObject;
    private GameObject outlinedGO;               // which object the outline is currently on
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;

    void Start()
    {
        cam = GetComponent<PlayerLook>().Cam;
        playerUI = FindFirstObjectByType<UIManager>().playerUI;
        inputManager = GetComponent<InputManager>();
    }

    void Update()
    {
        playerUI.UpdateText(string.Empty);

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, distance, mask, QueryTriggerInteraction.Ignore)
            && hitInfo.collider != null && !hitInfo.collider.isTrigger)
        {
            Collider col = hitInfo.collider;

            if (col.TryGetComponent(out Interactable interactable))
            {
                playerUI.UpdateText(interactable.PromptMessage);
                UpdateOutline(col.gameObject);

                if (inputManager.OnFoot.Interact.triggered)
                    interactable.BaseInteract();
                return;
            }

            if (col.TryGetComponent(out Npc npc) && npc.CanCommunicate)
            {
                playerUI.UpdateText(npc.NpcName);
                UpdateOutline(col.gameObject);

                if (inputManager.OnFoot.Interact.triggered)
                    npc.StartDialogue();
                return;
            }
        }

        // no hit / non-interactable -> make sure nothing stays highlighted
        UpdateOutline(null);
    }

    /// <summary>
    /// Applies/clears the outline only when the targeted object CHANGES.
    /// Avoids the per-frame enable/disable + ForceUpdateMaterials churn (was the GC + CPU cost),
    /// while the visible highlight stays identical.
    /// </summary>
    private void UpdateOutline(GameObject obj)
    {
        if (obj == outlinedGO) return; // same target as last frame -> nothing to do

        // clear previous
        if (currentOutlinedObject != null)
        {
            try { currentOutlinedObject.enabled = false; }
            catch (MissingReferenceException) { }
            currentOutlinedObject = null;
        }

        outlinedGO = obj;
        if (obj == null) return;
        if (((1 << obj.layer) & interactableLayer) == 0) return;

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null && !meshFilter.sharedMesh.isReadable)
        {
            Debug.LogWarning($"[PlayerInteract] Mesh on {obj.name} is not readable, skipping outline.");
            return;
        }

        if (!obj.TryGetComponent(out Outline outline))
            outline = obj.AddComponent<Outline>();

        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = highlightColor;
        outline.OutlineWidth = outlineWidth;
        outline.ForceUpdateMaterials();   // now runs once per target change, not every frame
        outline.enabled = true;

        currentOutlinedObject = outline;
    }
}
