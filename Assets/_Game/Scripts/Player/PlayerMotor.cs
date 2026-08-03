using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("Rýchlosť pohybu hráča.")]
    private float walkSpeed = 5f;

    [SerializeField, Tooltip("Rýchlosť šprintu hráča.")]
    private float sprintSpeed = 8f;

    [SerializeField, Tooltip("Gravitácia aplikovaná na hráča.")]
    private float gravity = -9.81f;

    [SerializeField, Tooltip("Výška skoku hráča.")]
    private float jumpHeight = 3f;

    [Header("Crouch Settings")]
    [SerializeField, Tooltip("Výška hráča počas kľaku.")]
    private float crouchHeight = 1f;

    [SerializeField, Tooltip("Výška hráča v stoji.")]
    private float standHeight = 2f;

    [SerializeField, Tooltip("Trvanie animácie kľaku (v sekundách).")]
    private float crouchDuration = 1f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    private bool isCrouchTransitioning;
    private float crouchTimer;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
    }

    private void Update()
    {
        HandleCrouchTransition();
        ApplyGravity();
    }

    /// <summary>
    /// Spracuje pohyb hráča na základe vstupu.
    /// </summary>
    /// <param name="input">Dvojrozmerný vstup pohybu (x - do strán, y - dopredu/dozadu).</param>
    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        controller.Move(transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Aktivuje alebo deaktivuje šprint.
    /// </summary>
    public void ToggleSprint()
    {
        isSprinting = !isSprinting;
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    }

    /// <summary>
    /// Umožní hráčovi skočiť, ak je na zemi.
    /// </summary>
    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    /// <summary>
    /// Aktivuje alebo deaktivuje kľak s plynulým prechodom.
    /// </summary>
    public void ToggleCrouch()
    {
        if (!isCrouchTransitioning)
        {
            isCrouching = !isCrouching;
            isCrouchTransitioning = true;
            crouchTimer = 0f;
        }
    }

    /// <summary>
    /// Spracuje plynulý prechod medzi kľakom a stoji.
    /// </summary>
    private void HandleCrouchTransition()
    {
        if (!isCrouchTransitioning) return;

        crouchTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(crouchTimer / crouchDuration);
        progress *= progress;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, progress);

        if (progress >= 1f)
        {
            isCrouchTransitioning = false;
        }
    }

    /// <summary>
    /// Aplikuje gravitáciu na hráča a spracuje kontakt so zemou.
    /// </summary>
    private void ApplyGravity()
    {
        isGrounded = CheckGrounded();

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Skontroluje, či je hráč na zemi pomocou raycastu.
    /// </summary>
    private bool CheckGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, (controller.height / 2f) + 0.2f);
    }

    public void StartSprint()
    {
        isSprinting = true;
        currentSpeed = sprintSpeed;
    }

    public void StopSprint()
    {
        isSprinting = false;
        currentSpeed = walkSpeed;
    }
}
