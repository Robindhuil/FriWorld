using UnityEngine;
using UnityEngine.Rendering;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask;
    private PlayerUI playerUI;
    private InputManager inputManager;

    private Outline currentOutlinedObject;
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

        if (currentOutlinedObject != null)
        {
            if (currentOutlinedObject != null && currentOutlinedObject.gameObject != null)
            {
                try
                {
                    currentOutlinedObject.enabled = false;
                }
                catch (MissingReferenceException) { }
            }

            currentOutlinedObject = null;
        }


        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, distance, mask, QueryTriggerInteraction.Ignore))
        {
            if (hitInfo.collider == null || hitInfo.collider.isTrigger)
                return;

            if (hitInfo.collider.GetComponent<Interactable>() != null)
            {
                Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
                playerUI.UpdateText(interactable.promptMessage);

                if (((1 << hitInfo.collider.gameObject.layer) & interactableLayer) != 0)
                {
                    AddOutlineToObject(hitInfo.collider.gameObject);
                }

                if (inputManager.onFoot.Interact.triggered)
                {
                    interactable.BaseInteract();
                }
            }
            else if (hitInfo.collider.GetComponent<Npc>() != null)
            {
                Npc npc = hitInfo.collider.GetComponent<Npc>();
                if (npc.canCommunicate)
                {
                    playerUI.UpdateText(npc.NpcName);

                    if (((1 << hitInfo.collider.gameObject.layer) & interactableLayer) != 0)
                    {
                        AddOutlineToObject(hitInfo.collider.gameObject);
                    }

                    if (inputManager.onFoot.Interact.triggered)
                    {
                        npc.StartDialogue();
                    }
                }
            }
        }
    }

    private void AddOutlineToObject(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null && !meshFilter.sharedMesh.isReadable)
        {
            Debug.LogWarning($"[PlayerInteract] Mesh on {obj.name} is not readable, skipping outline.");
            return;
        }

        Outline outline = obj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }

        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineColor = highlightColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = true;

        currentOutlinedObject = outline;
    }

}