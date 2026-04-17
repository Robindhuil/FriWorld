using UnityEngine;
using UnityEngine.InputSystem;
public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    public PlayerInput.OnFootActions OnFoot { get; private set; }
    public PlayerInput.DialogueUiActions DialogueUI { get; private set; }
    public PlayerInput.InUIActions InUI { get; private set; }
        
    private PlayerMotor motor;
    private PlayerLook look;

    void Awake()
    {
        playerInput = new PlayerInput();
        
        // Load saved binding overrides IMMEDIATELY after instantiation
        LoadBindingOverrides();
        
        OnFoot = playerInput.OnFoot;
        DialogueUI = playerInput.DialogueUi;
        InUI = playerInput.InUI;
        motor = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        OnFoot.Jump.performed += ctx => motor.Jump();

        OnFoot.Crouch.performed += ctx => motor.ToggleCrouch();
        OnFoot.Sprint.started += ctx => motor.StartSprint();
        OnFoot.Sprint.canceled += ctx => motor.StopSprint();
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
        motor.ProcessMove(OnFoot.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        look.ProcessLook(OnFoot.Look.ReadValue<Vector2>());
    }

    public void SwitchToOnFootActions()
    {
        DialogueUI.Disable();
        InUI.Disable();
        OnFoot.Enable();
    }

    public void SwitchToDialogueUIActions()
    {
        OnFoot.Disable();
        InUI.Disable();
        DialogueUI.Enable();
    }

    public void SwitchToInUIActions()
    {
        OnFoot.Disable();
        DialogueUI.Disable();
        InUI.Enable();
    }
}
