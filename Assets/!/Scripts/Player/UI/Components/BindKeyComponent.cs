using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class BindKeyComponent : VisualElement
{
    private const string UxmlPath = "UI/BindKey";

    private Label actionLabel;
    private Label bindingLabel;
    private Button resetButton;
    
    private ControlsSettings controlsSettings;
    private string actionName;
    private int bindingIndex;
    
    private string originalLabelText;
    private bool isRebinding = false;

    public string ActionName
    {
        get => actionName;
        set
        {
            actionName = value;
            if (actionLabel != null)
                actionLabel.text = value;
        }
    }

    public string CurrentKey
    {
        get => bindingLabel?.text;
        set
        {
            if (bindingLabel != null && !isRebinding)
                bindingLabel.text = value;
        }
    }

    public BindKeyComponent()
    {
        var visualTree = Resources.Load<VisualTreeAsset>(UxmlPath);
        if (visualTree == null)
        {
            Debug.LogError($"BindKey.uxml not found in Resources/{UxmlPath}");
            return;
        }

        visualTree.CloneTree(this);

        actionLabel = this.Q<Label>("ActionLabel");
        bindingLabel = this.Q<Label>("CurrentBindingLabel");
        resetButton = this.Q<Button>("ResetToDefaultButton");

        if (actionLabel == null || bindingLabel == null)
        {
            Debug.LogError("BindKey: Could not find required labels inside UXML.");
            return;
        }

        // Make the binding label clickable for rebinding
        bindingLabel.RegisterCallback<ClickEvent>(OnBindingLabelClicked);
        
        // Add visual feedback on hover
        bindingLabel.RegisterCallback<MouseEnterEvent>(evt => 
        {
            if (!isRebinding)
                bindingLabel.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        });
        
        bindingLabel.RegisterCallback<MouseLeaveEvent>(evt => 
        {
            if (!isRebinding)
                bindingLabel.style.backgroundColor = new Color(0, 0, 0, 0.2f);
        });
        
        // Setup reset button
        if (resetButton != null)
        {
            resetButton.clicked += OnResetButtonClicked;
        }
        else
        {
            Debug.LogWarning("BindKey: Reset button not found in UXML.");
        }
    }

    /// <summary>
    /// Initialize the component with ControlsSettings and action information
    /// </summary>
    public void Initialize(ControlsSettings settings, string action, int binding = 0)
    {
        controlsSettings = settings;
        actionName = action;
        bindingIndex = binding;
        
        if (controlsSettings != null)
        {
            // Subscribe to rebind events
            controlsSettings.OnRebindComplete += OnRebindComplete;
            controlsSettings.OnRebindCancelled += OnRebindCancelled;
            
            // Update the display
            UpdateBindingDisplay();
        }
    }

    /// <summary>
    /// Update the binding label to show the current key
    /// </summary>
    public void UpdateBindingDisplay()
    {
        if (controlsSettings == null || string.IsNullOrEmpty(actionName))
            return;
            
        string displayString = controlsSettings.GetBindingDisplayString(actionName, bindingIndex);
        if (bindingLabel != null && !isRebinding)
        {
            bindingLabel.text = displayString;
        }
    }

    /// <summary>
    /// Called when the binding label is clicked - starts rebinding
    /// </summary>
    private void OnBindingLabelClicked(ClickEvent evt)
    {
        if (controlsSettings == null || string.IsNullOrEmpty(actionName) || isRebinding)
            return;
        
        StartRebinding();
    }

    /// <summary>
    /// Start the rebinding process
    /// </summary>
    private void StartRebinding()
    {
        isRebinding = true;
        originalLabelText = bindingLabel.text;
        
        // Update UI to show waiting for input
        bindingLabel.text = "Press any key...";
        bindingLabel.style.backgroundColor = new Color(0.5f, 0.3f, 0, 0.5f);
        
        // Disable reset button during rebinding
        if (resetButton != null)
            resetButton.SetEnabled(false);
        
        // Start the rebinding operation
        controlsSettings.StartRebinding(actionName, bindingIndex);
    }

    /// <summary>
    /// Called when rebinding completes successfully
    /// </summary>
    private void OnRebindComplete(string completedAction, int completedBinding)
    {
        // Only handle if this is for our action and binding
        if (completedAction != actionName || completedBinding != bindingIndex)
            return;
        
        isRebinding = false;
        
        // Update the display with the new binding
        UpdateBindingDisplay();
        
        // Reset UI state
        bindingLabel.style.backgroundColor = new Color(0, 0, 0, 0.2f);
        
        if (resetButton != null)
            resetButton.SetEnabled(true);
    }

    /// <summary>
    /// Called when rebinding is cancelled
    /// </summary>
    private void OnRebindCancelled()
    {
        if (!isRebinding)
            return;
        
        isRebinding = false;
        
        // Restore original text
        if (bindingLabel != null)
            bindingLabel.text = originalLabelText;
        
        // Reset UI state
        bindingLabel.style.backgroundColor = new Color(0, 0, 0, 0.2f);
        
        if (resetButton != null)
            resetButton.SetEnabled(true);
    }

    /// <summary>
    /// Called when the reset button is clicked
    /// </summary>
    private void OnResetButtonClicked()
    {
        if (controlsSettings == null || string.IsNullOrEmpty(actionName))
            return;
        
        // Reset this binding to default
        controlsSettings.ResetBinding(actionName, bindingIndex);
        
        // Update the display
        UpdateBindingDisplay();
    }

    public void Cleanup()
    {
        // Unsubscribe from events
        if (controlsSettings != null)
        {
            controlsSettings.OnRebindComplete -= OnRebindComplete;
            controlsSettings.OnRebindCancelled -= OnRebindCancelled;
        }
        
        // Unregister callbacks
        if (bindingLabel != null)
        {
            bindingLabel.UnregisterCallback<ClickEvent>(OnBindingLabelClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.clicked -= OnResetButtonClicked;
        }
    }
}