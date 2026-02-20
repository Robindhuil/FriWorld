using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    public PlayerInput.OnFootActions onFoot;
    public PlayerInput.DialogueUiActions dialogueUI;
    public PlayerInput.InUIActions inUI;
        
    private PlayerMotor motor;
    private PlayerLook look;

    void Awake()
    {
        playerInput = new PlayerInput();
        
        // Load saved binding overrides IMMEDIATELY after instantiation
        LoadBindingOverrides();
        
        onFoot = playerInput.OnFoot;
        dialogueUI = playerInput.DialogueUi;
        inUI = playerInput.InUI;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        onFoot.Jump.performed += ctx => motor.Jump();

        onFoot.Crouch.performed += ctx => motor.ToggleCrouch();
        onFoot.Sprint.started += ctx => motor.StartSprint();
        onFoot.Sprint.canceled += ctx => motor.StopSprint();
    }
    
    /// <summary>
    /// Load saved binding overrides from PlayerPrefs
    /// </summary>
    private void LoadBindingOverrides()
    {
        string rebinds = PlayerPrefs.GetString("InputRebinds", string.Empty);
        
        if (!string.IsNullOrEmpty(rebinds))
        {
            playerInput.asset.LoadBindingOverridesFromJson(rebinds);
            Debug.Log("InputManager: Loaded input binding overrides");
        }
    }
    
    /// <summary>
    /// Reload binding overrides - call this when rebinds change during runtime
    /// </summary>
    public void ReloadBindingOverrides()
    {
        LoadBindingOverrides();
        Debug.Log("InputManager: Reloaded binding overrides");
    }
    
    /// <summary>
    /// Get the InputActionAsset being used by this player
    /// </summary>
    public InputActionAsset GetInputActionAsset()
    {
        return playerInput?.asset;
    }

    void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        look.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }

    public void SwitchToOnFootActions()
    {
        dialogueUI.Disable();
        inUI.Disable();
        onFoot.Enable();
    }

    public void SwitchToDialogueUIActions()
    {
        onFoot.Disable();
        inUI.Disable();
        dialogueUI.Enable();
    }

    public void SwitchToInUIActions()
    {
        onFoot.Disable();
        dialogueUI.Disable();
        inUI.Enable();
    }
}
