using UnityEngine;

public class PlayerMovementSounds : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepsSound;
    public AudioSource sprintSound;
    public AudioSource jumpStartSound;
    public AudioSource jumpEndSound;

    [Header("Ground Check")]
    public float raycastDistance = 0.3f;
    public Transform raycastOrigin;
    public LayerMask groundLayers;

    private bool wasGrounded = true;
    private InputManager inputManager;
    private PlayerInput.OnFootActions onFoot;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null)
        {
            onFoot = inputManager.onFoot;
        }
        else
        {
            Debug.LogWarning("[PlayerMovementSounds] InputManager not found. Movement sounds will not sync to input.");
        }
    }

    void Update()
    {
        if (inputManager == null)
        {
            return;
        }

        if (!onFoot.enabled)
        {
            DisableMovementSounds();
            return;
        }

        Vector2 moveInput = onFoot.Movement.ReadValue<Vector2>();
        bool isMoving = moveInput.sqrMagnitude > 0.001f;

        bool isGrounded = CheckIfGrounded();

        HandleJumpSounds(isGrounded);

        HandleMovementSounds(isMoving, isGrounded);

        wasGrounded = isGrounded;
    }

    void HandleJumpSounds(bool isGrounded)
    {
        if (onFoot.Jump.WasPressedThisFrame() && isGrounded)
        {
            jumpStartSound.Play();
        }

        if (isGrounded && !wasGrounded)
        {
            jumpEndSound.Play();
        }
    }


    void HandleMovementSounds(bool isMoving, bool isGrounded)
    {
        if (isMoving && isGrounded)
        {
            if (onFoot.Sprint.IsPressed())
            {
                footstepsSound.enabled = false;
                sprintSound.enabled = true;
            }
            else
            {
                footstepsSound.enabled = true;
                sprintSound.enabled = false;
            }
        }
        else
        {
            footstepsSound.enabled = false;
            sprintSound.enabled = false;
        }
    }

    void DisableMovementSounds()
    {
        footstepsSound.enabled = false;
        sprintSound.enabled = false;
    }

    bool CheckIfGrounded()
    {
        bool grounded = Physics.Raycast(
            raycastOrigin.position,
            Vector3.down,
            raycastDistance,
            groundLayers
        );

        Debug.DrawRay(raycastOrigin.position, Vector3.down * raycastDistance,
                    grounded ? Color.green : Color.red);

        return grounded;
    }
}