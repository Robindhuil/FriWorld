using UnityEngine;

[System.Serializable]
public class SoundEffectReference
{
    [SerializeField]
    private SoundEffectId id;

    public SoundEffectReference() { }

    public SoundEffectReference(SoundEffectId id)
    {
        this.id = id;
    }

    public AudioClip ResolveClip()
    {
        GlobalSoundEffectRegistry registry = GlobalSoundEffectRegistry.Instance;
        if (registry == null)
        {
            return null;
        }

        return registry.GetClip(id);
    }
}

public class PlayerMovementSounds : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource footstepsSound;

    [SerializeField]
    private AudioSource sprintSound;

    [SerializeField]
    private AudioSource jumpStartSound;

    [SerializeField]
    private AudioSource jumpEndSound;

    [Header("Sound References")]
    [SerializeField]
    private SoundEffectReference footstepsReference = new SoundEffectReference(
        SoundEffectId.Foley_Walk
    );

    [SerializeField]
    private SoundEffectReference sprintReference = new SoundEffectReference(
        SoundEffectId.Foley_Sprint
    );

    [SerializeField]
    private SoundEffectReference jumpStartReference = new SoundEffectReference(
        SoundEffectId.Foley_JumpStart
    );

    [SerializeField]
    private SoundEffectReference jumpEndReference = new SoundEffectReference(
        SoundEffectId.Foley_JumpEnd
    );

    [Header("Ground Check")]
    [SerializeField]
    private float raycastDistance = 0.3f;

    [SerializeField]
    private Transform raycastOrigin;

    [SerializeField]
    private LayerMask groundLayers;

    private bool wasGrounded = true;
    private InputManager inputManager;
    private PlayerInput.OnFootActions onFoot;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager != null)
        {
            onFoot = inputManager.OnFoot;
        }
        else
        {
            Debug.LogWarning(
                "[PlayerMovementSounds] InputManager not found. Movement sounds will not sync to input."
            );
        }
    }

    private void Start()
    {
        ConfigureAudioSourcesFromReferences();
    }

    private void OnValidate()
    {
        ConfigureAudioSourcesFromReferences();
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

        Debug.DrawRay(
            raycastOrigin.position,
            Vector3.down * raycastDistance,
            grounded ? Color.green : Color.red
        );

        return grounded;
    }

    private void ConfigureAudioSourcesFromReferences()
    {
        ApplySoundReference(footstepsSound, footstepsReference);
        ApplySoundReference(sprintSound, sprintReference);
        ApplySoundReference(jumpStartSound, jumpStartReference);
        ApplySoundReference(jumpEndSound, jumpEndReference);
    }

    private void ApplySoundReference(AudioSource source, SoundEffectReference soundReference)
    {
        if (source == null || soundReference == null)
        {
            return;
        }

        AudioClip clip = soundReference.ResolveClip();
        if (clip != null)
        {
            source.clip = clip;
        }
    }
}
