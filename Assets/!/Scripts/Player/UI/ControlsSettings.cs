using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

public class ControlsSettings : MonoBehaviour
{
    [Header("Input Action Assets")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Runtime References")]
    [SerializeField] private InputManager inputManager;
    
    private InputActionMap onFootMap;
    private InputActionMap onFootDefaultMap;
    
    // Track created UI components for cleanup
    private List<BindKeyComponent> bindKeyComponents = new List<BindKeyComponent>();
    
    // Event that fires when a rebind operation completes
    public event Action<string, int> OnRebindComplete;
    
    // Event that fires when a rebind is cancelled or fails
    public event Action OnRebindCancelled;
    
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    
    private void Awake()
    {
        // Try to find InputManager in the scene to use its runtime asset instance
        if (inputManager == null)
        {
            inputManager = FindAnyObjectByType<InputManager>();
        }
        
        // If we have an InputManager, try to use its asset (the actual runtime instance being used)
        if (inputManager != null)
        {
            InputActionAsset runtimeAsset = inputManager.GetInputActionAsset();
            if (runtimeAsset != null)
            {
                inputActions = runtimeAsset;
                Debug.Log("ControlsSettings: Using InputActionAsset from InputManager (runtime instance)");
            }
            else
            {
                // InputManager found but asset not ready yet (Awake timing) - will use serialized asset
                Debug.Log("ControlsSettings: Using serialized InputActionAsset. InputManager will sync on rebind.");
            }
        }
        else if (inputActions != null)
        {
            Debug.Log("ControlsSettings: No InputManager found, using serialized InputActionAsset. This is normal for menu scenes.");
        }
        
        InitializeActionMaps();
    }
    
    /// <summary>
    /// Initialize references to the OnFoot and OnFootDefault action maps
    /// </summary>
    private void InitializeActionMaps()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is not assigned in ControlsSettings!");
            return;
        }
        
        onFootMap = inputActions.FindActionMap("OnFoot");
        onFootDefaultMap = inputActions.FindActionMap("OnFootDefault");
        
        if (onFootMap == null)
        {
            Debug.LogError("OnFoot action map not found!");
        }
        
        if (onFootDefaultMap == null)
        {
            Debug.LogError("OnFootDefault action map not found!");
        }
    }
    
    /// <summary>
    /// Start rebinding a specific action in the OnFoot map
    /// </summary>
    /// <param name="actionName">Name of the action to rebind (e.g., "Move", "Jump")</param>
    /// <param name="bindingIndex">Index of the binding to rebind (0 for first binding, etc.)</param>
    /// <param name="excludeMouse">Whether to exclude mouse input from rebinding</param>
    public void StartRebinding(string actionName, int bindingIndex = 0, bool excludeMouse = true)
    {
        if (onFootMap == null)
        {
            Debug.LogError("OnFoot action map is not initialized!");
            return;
        }
        
        InputAction action = onFootMap.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found in OnFoot map!");
            return;
        }
        
        // Make sure we're not already rebinding
        if (rebindOperation != null && rebindOperation.started)
        {
            rebindOperation.Cancel();
        }
        
        // Disable the action before rebinding
        action.Disable();
        
        // Start the rebinding operation
        rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(operation => RebindComplete(actionName, bindingIndex))
            .OnCancel(operation => RebindCancelled(action));
        
        // Exclude mouse if requested
        if (excludeMouse)
        {
            rebindOperation.WithControlsExcluding("Mouse");
        }
        
        // Start the rebinding
        rebindOperation.Start();
    }
    
    /// <summary>
    /// Called when rebinding completes successfully
    /// </summary>
    private void RebindComplete(string actionName, int bindingIndex)
    {
        // Clean up the rebinding operation
        rebindOperation.Dispose();
        rebindOperation = null;
        
        // Re-enable the action
        InputAction action = onFootMap.FindAction(actionName);
        if (action != null)
        {
            action.Enable();
        }
        
        // Save the rebind
        SaveBindingOverride(actionName, bindingIndex);
        
        // If InputManager exists in the scene, tell it to reload bindings for immediate effect
        if (inputManager != null)
        {
            inputManager.ReloadBindingOverrides();
        }
        
        // Notify listeners
        OnRebindComplete?.Invoke(actionName, bindingIndex);
        
        Debug.Log($"Rebind complete for {actionName} binding {bindingIndex}");
    }
    
    /// <summary>
    /// Called when rebinding is cancelled
    /// </summary>
    private void RebindCancelled(InputAction action)
    {
        // Clean up the rebinding operation
        rebindOperation.Dispose();
        rebindOperation = null;
        
        // Re-enable the action
        action?.Enable();
        
        // Notify listeners
        OnRebindCancelled?.Invoke();
        
        Debug.Log("Rebind cancelled");
    }
    
    /// <summary>
    /// Cancel an ongoing rebinding operation
    /// </summary>
    public void CancelRebinding()
    {
        if (rebindOperation != null && rebindOperation.started)
        {
            rebindOperation.Cancel();
        }
    }
    
    /// <summary>
    /// Reset a specific action binding to its default value from OnFootDefault
    /// </summary>
    /// <param name="actionName">Name of the action to reset</param>
    /// <param name="bindingIndex">Index of the binding to reset</param>
    public void ResetBinding(string actionName, int bindingIndex = 0)
    {
        if (onFootMap == null || onFootDefaultMap == null)
        {
            Debug.LogError("Action maps are not initialized!");
            return;
        }
        
        InputAction currentAction = onFootMap.FindAction(actionName);
        InputAction defaultAction = onFootDefaultMap.FindAction(actionName);
        
        if (currentAction == null || defaultAction == null)
        {
            Debug.LogError($"Action '{actionName}' not found in one of the action maps!");
            return;
        }
        
        // Check if bindingIndex is valid
        if (bindingIndex >= currentAction.bindings.Count)
        {
            Debug.LogError($"Binding index {bindingIndex} is out of range for action '{actionName}'!");
            return;
        }
        
        // Remove any override for this binding
        currentAction.RemoveBindingOverride(bindingIndex);
        
        // Get the default binding path from OnFootDefault
        string defaultPath = defaultAction.bindings[bindingIndex].effectivePath;
        
        // Apply the default path to OnFoot
        currentAction.ApplyBindingOverride(bindingIndex, defaultPath);
        
        // Save the reset
        SaveBindingOverride(actionName, bindingIndex);
        
        // If InputManager exists, reload bindings for immediate effect
        if (inputManager != null)
        {
            inputManager.ReloadBindingOverrides();
        }
        
        Debug.Log($"Reset binding {bindingIndex} for action '{actionName}' to default: {defaultPath}");
    }
    
    /// <summary>
    /// Reset all bindings for a specific action to defaults
    /// </summary>
    /// <param name="actionName">Name of the action to reset all bindings for</param>
    public void ResetAllBindingsForAction(string actionName)
    {
        if (onFootMap == null || onFootDefaultMap == null)
        {
            Debug.LogError("Action maps are not initialized!");
            return;
        }
        
        InputAction currentAction = onFootMap.FindAction(actionName);
        InputAction defaultAction = onFootDefaultMap.FindAction(actionName);
        
        if (currentAction == null || defaultAction == null)
        {
            Debug.LogError($"Action '{actionName}' not found in one of the action maps!");
            return;
        }
        
        // Reset each binding
        for (int i = 0; i < currentAction.bindings.Count; i++)
        {
            currentAction.RemoveBindingOverride(i);
            
            if (i < defaultAction.bindings.Count)
            {
                string defaultPath = defaultAction.bindings[i].effectivePath;
                currentAction.ApplyBindingOverride(i, defaultPath);
            }
        }
        
        // Save all resets
        for (int i = 0; i < currentAction.bindings.Count; i++)
        {
            SaveBindingOverride(actionName, i);
        }
        
        // If InputManager exists, reload bindings for immediate effect
        if (inputManager != null)
        {
            inputManager.ReloadBindingOverrides();
        }
        
        Debug.Log($"Reset all bindings for action '{actionName}' to defaults");
    }
    
    /// <summary>
    /// Reset all actions in the OnFoot map to their defaults from OnFootDefault
    /// </summary>
    public void ResetAllBindings()
    {
        if (onFootMap == null || onFootDefaultMap == null)
        {
            Debug.LogError("Action maps are not initialized!");
            return;
        }
        
        foreach (InputAction action in onFootMap.actions)
        {
            ResetAllBindingsForAction(action.name);
        }
        
        // Note: ResetAllBindingsForAction already calls ReloadBindingOverrides for each action
        // No need to call it again here
        
        Debug.Log("Reset all bindings to defaults");
    }
    
    /// <summary>
    /// Get the current binding path for a specific action and binding index
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <param name="bindingIndex">Index of the binding</param>
    /// <returns>The binding path or empty string if not found</returns>
    public string GetBindingPath(string actionName, int bindingIndex = 0)
    {
        if (onFootMap == null)
        {
            Debug.LogError("OnFoot action map is not initialized!");
            return string.Empty;
        }
        
        InputAction action = onFootMap.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count)
        {
            return string.Empty;
        }
        
        return action.bindings[bindingIndex].effectivePath;
    }
    
    /// <summary>
    /// Get a user-friendly display string for a binding
    /// </summary>
    /// <param name="actionName">Name of the action</param>
    /// <param name="bindingIndex">Index of the binding</param>
    /// <returns>Formatted display string</returns>
    public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
    {
        if (onFootMap == null)
        {
            return "Not Set";
        }
        
        InputAction action = onFootMap.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count)
        {
            return "Not Set";
        }
        
        return action.GetBindingDisplayString(bindingIndex);
    }
    
    /// <summary>
    /// Save all binding overrides to PlayerPrefs
    /// </summary>
    private void SaveBindingOverride(string actionName, int bindingIndex)
    {
        SaveAllBindingOverrides();
    }
    
    /// <summary>
    /// Save all binding overrides for the entire InputActionAsset
    /// </summary>
    private void SaveAllBindingOverrides()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is null, cannot save bindings!");
            return;
        }
        
        string rebinds = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputRebinds", rebinds);
        PlayerPrefs.Save();
        
        Debug.Log("Saved all binding overrides");
    }
    
    /// <summary>
    /// Load all saved binding overrides from PlayerPrefs
    /// </summary>
    public void LoadBindingOverrides()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is null, cannot load bindings!");
            return;
        }
        
        string rebinds = PlayerPrefs.GetString("InputRebinds", string.Empty);
        
        if (string.IsNullOrEmpty(rebinds))
        {
            Debug.Log("No saved binding overrides found");
            return;
        }
        
        inputActions.LoadBindingOverridesFromJson(rebinds);
        Debug.Log("Loaded binding overrides from PlayerPrefs");
    }
    
    /// <summary>
    /// Clear all saved binding overrides
    /// </summary>
    public void ClearSavedBindings()
    {
        if (inputActions == null)
        {
            return;
        }
        
        PlayerPrefs.DeleteKey("InputRebinds");
        PlayerPrefs.Save();
        
        // Also remove overrides from the asset
        inputActions.RemoveAllBindingOverrides();
        
        Debug.Log("Cleared all saved bindings");
    }
    
    /// <summary>
    /// Populate a container with BindKeyComponent instances for all actions in OnFoot action map
    /// </summary>
    /// <param name="container">The VisualElement container to add components to</param>
    /// <param name="includeComposites">If true, shows individual composite parts (e.g., WASD). If false, skips composite parts.</param>
    public void PopulateBindingsContainer(VisualElement container, bool includeComposites = true)
    {
        if (container == null)
        {
            Debug.LogError("Container is null!");
            return;
        }
        
        if (onFootMap == null)
        {
            Debug.LogError("OnFoot action map is not initialized!");
            return;
        }
        
        // Clear existing components first
        ClearBindingsContainer(container);
        
        // Iterate through all actions in OnFoot
        foreach (InputAction action in onFootMap.actions)
        {
            // Iterate through all bindings for this action
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                
                // Skip composite bindings if requested
                if (!includeComposites && binding.isComposite)
                {
                    continue;
                }
                
                // Skip part of composite bindings if we're not including composites
                if (!includeComposites && binding.isPartOfComposite)
                {
                    continue;
                }
                
                // Create and setup the component
                CreateBindKeyComponent(container, action.name, i, binding);
            }
        }
        
        Debug.Log($"Populated container with {bindKeyComponents.Count} key binding components");
    }
    
    /// <summary>
    /// Populate container with specific actions only
    /// </summary>
    /// <param name="container">The VisualElement container to add components to</param>
    /// <param name="actionNames">Array of action names to include</param>
    public void PopulateBindingsContainerForActions(VisualElement container, string[] actionNames)
    {
        if (container == null)
        {
            Debug.LogError("Container is null!");
            return;
        }
        
        if (onFootMap == null)
        {
            Debug.LogError("OnFoot action map is not initialized!");
            return;
        }
        
        if (actionNames == null || actionNames.Length == 0)
        {
            Debug.LogWarning("No action names provided!");
            return;
        }
        
        // Clear existing components first
        ClearBindingsContainer(container);
        
        // Iterate through specified actions
        foreach (string actionName in actionNames)
        {
            InputAction action = onFootMap.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"Action '{actionName}' not found in OnFoot map!");
                continue;
            }
            
            // For each action, create components for non-composite bindings
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                
                // Skip composite bindings themselves, but include their parts
                if (binding.isComposite)
                {
                    continue;
                }
                
                // Create and setup the component
                CreateBindKeyComponent(container, action.name, i, binding);
            }
        }
        
        Debug.Log($"Populated container with {bindKeyComponents.Count} key binding components for specified actions");
    }
    
    /// <summary>
    /// Create a single BindKeyComponent and add it to the container
    /// </summary>
    private void CreateBindKeyComponent(VisualElement container, string actionName, int bindingIndex, InputBinding binding)
    {
        // Create the component
        BindKeyComponent component = new BindKeyComponent();
        
        // Set the display name
        string displayName = GetDisplayNameForBinding(actionName, bindingIndex, binding);
        component.ActionName = displayName;
        
        // Initialize with this ControlsSettings instance
        component.Initialize(this, actionName, bindingIndex);
        
        // Add to container
        container.Add(component);
        
        // Track for cleanup
        bindKeyComponents.Add(component);
    }
    
    /// <summary>
    /// Get a user-friendly display name for a binding
    /// </summary>
    private string GetDisplayNameForBinding(string actionName, int bindingIndex, InputBinding binding)
    {
        // If it's part of a composite, use the part name
        if (binding.isPartOfComposite)
        {
            // Format: "Move - Up", "Move - Left", etc.
            return $"{actionName} - {binding.name}";
        }
        
        // For regular bindings, just use the action name
        return actionName;
    }
    
    /// <summary>
    /// Clear all BindKeyComponents from a container
    /// </summary>
    /// <param name="container">The container to clear components from</param>
    public void ClearBindingsContainer(VisualElement container)
    {
        if (container == null)
        {
            Debug.LogWarning("Container is null, cannot clear!");
            return;
        }
        
        // Cleanup and remove all components
        foreach (var component in bindKeyComponents)
        {
            component.Cleanup();
            container.Remove(component);
        }
        
        bindKeyComponents.Clear();
    }
    
    /// <summary>
    /// Update all created binding displays (useful after loading saved bindings)
    /// </summary>
    public void UpdateAllBindingDisplays()
    {
        foreach (var component in bindKeyComponents)
        {
            component.UpdateBindingDisplay();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up rebinding operation if still running
        if (rebindOperation != null && rebindOperation.started)
        {
            rebindOperation.Dispose();
        }
        
        // Clean up all components
        foreach (var component in bindKeyComponents)
        {
            component.Cleanup();
        }
        bindKeyComponents.Clear();
    }
}
