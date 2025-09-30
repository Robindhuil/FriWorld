using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    public PlayerInput.OnFootActions onFoot;
    public PlayerInput.DialogueUiActions dialogueUI;

    private PlayerMotor motor;
    private PlayerLook look;

    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        dialogueUI = playerInput.DialogueUi;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        onFoot.Jump.performed += ctx => motor.Jump();

        onFoot.Crouch.performed += ctx => motor.ToggleCrouch();
        onFoot.Sprint.started += ctx => motor.StartSprint();
        onFoot.Sprint.canceled += ctx => motor.StopSprint();
    }

    void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        onFoot.Enable();
    }

    private void OnDisable()
    {
        onFoot.Disable();
    }

    public void SwitchToOnFootActions()
    {
        dialogueUI.Disable();
        onFoot.Enable();
    }

    public void SwitchToDialogueUIActions()
    {
        onFoot.Disable();
        dialogueUI.Enable();
    }
}
