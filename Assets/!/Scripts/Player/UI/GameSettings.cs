using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GameSettings : MonoBehaviour
{
    [Header("Player Preferences")]
    [SerializeField] private PlayerPreferenceSettingsManager prefsManager;

    [Header("Player References")]
    [SerializeField] private PlayerLook playerLook;

    #region Sensitivity Constants
    private const float MIN_SENSITIVITY = 400f;
    private const float MAX_SENSITIVITY = 1600f;
    #endregion

    #region Sensitivity Management

    /// <summary>
    /// Sets the mouse sensitivity
    /// </summary>
    /// <param name="sensitivity">Sensitivity value (400-1600 DPI)</param>
    public void SetMouseSensitivity(float sensitivity)
    {
        float clampedSensitivity = Mathf.Clamp(sensitivity, MIN_SENSITIVITY, MAX_SENSITIVITY);
        
        if (playerLook != null)
        {
            playerLook.SetSensitivity(clampedSensitivity);
        }
        
        Debug.Log($"Mouse sensitivity set to: {clampedSensitivity} DPI");
    }

    /// <summary>
    /// Gets the current mouse sensitivity
    /// </summary>
    /// <returns>Current sensitivity value</returns>
    public float GetMouseSensitivity()
    {
        if (playerLook != null)
        {
            return playerLook.GetSensitivity();
        }
        return PlayerPreferenceSettingsManager.DEFAULT_MOUSE_SENSITIVITY;
    }

    #endregion

    #region Invert Axis Management

    /// <summary>
    /// Sets the X-axis inversion
    /// </summary>
    /// <param name="invert">True to invert X-axis</param>
    public void SetInvertX(bool invert)
    {
        if (playerLook != null)
        {
            playerLook.SetInvertX(invert);
        }
        
        Debug.Log($"Invert X set to: {invert}");
    }

    /// <summary>
    /// Sets the Y-axis inversion
    /// </summary>
    /// <param name="invert">True to invert Y-axis</param>
    public void SetInvertY(bool invert)
    {
        if (playerLook != null)
        {
            playerLook.SetInvertY(invert);
        }
        
        Debug.Log($"Invert Y set to: {invert}");
    }

    /// <summary>
    /// Gets the current X-axis inversion state
    /// </summary>
    /// <returns>True if X-axis is inverted</returns>
    public bool GetInvertX()
    {
        if (playerLook != null)
        {
            return playerLook.GetInvertX();
        }
        return PlayerPreferenceSettingsManager.DEFAULT_INVERT_X;
    }

    /// <summary>
    /// Gets the current Y-axis inversion state
    /// </summary>
    /// <returns>True if Y-axis is inverted</returns>
    public bool GetInvertY()
    {
        if (playerLook != null)
        {
            return playerLook.GetInvertY();
        }
        return PlayerPreferenceSettingsManager.DEFAULT_INVERT_Y;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Applies saved game settings from PlayerPrefs
    /// </summary>
    public void ApplySavedSettings()
    {
        if (prefsManager == null)
        {
            Debug.LogWarning("PlayerPreferenceSettingsManager not assigned!");
            return;
        }

        if (playerLook == null)
        {
            Debug.LogWarning("PlayerLook not assigned!");
            return;
        }

        // Apply saved sensitivity
        SetMouseSensitivity(prefsManager.MouseSensitivity);
        
        // Apply saved invert settings
        SetInvertX(prefsManager.InvertX);
        SetInvertY(prefsManager.InvertY);
        
        Debug.Log("Applied saved game settings");
    }

    #endregion

    #region UI Component Creation

    /// <summary>
    /// Creates and configures the Mouse Sensitivity slider component
    /// </summary>
    /// <param name="container">The visual element container to add the slider to</param>
    /// <returns>The configured SliderComponent</returns>
    public SliderComponent CreateMouseSensitivitySlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var sliderComponent = new SliderComponent();
        
        // Configure slider properties
        sliderComponent.Initialize(
            name: "Mouse Sensitivity",
            min: MIN_SENSITIVITY,
            max: MAX_SENSITIVITY,
            defaultVal: PlayerPreferenceSettingsManager.DEFAULT_MOUSE_SENSITIVITY,
            wholeNumbers: true
        );

        sliderComponent.TooltipText = "Adjust mouse sensitivity (400-1600 DPI)";

        // Load saved value
        float savedSensitivity = prefsManager.MouseSensitivity;
        sliderComponent.SetValueWithoutNotify(savedSensitivity);

        // Apply the loaded value to PlayerLook
        SetMouseSensitivity(savedSensitivity);

        // Custom level format to show DPI instead of %
        sliderComponent.SetLevelFormat("{0:0} DPI");

        // Hook up value changed event
        sliderComponent.OnValueChanged += (value) =>
        {
            SetMouseSensitivity(value);
            prefsManager.SaveMouseSensitivity(value);
            // Update display format
            sliderComponent.SetLevelFormat("{0:0} DPI");
        };

        // Hook up reset to default event
        sliderComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetMouseSensitivity();
            SetMouseSensitivity(PlayerPreferenceSettingsManager.DEFAULT_MOUSE_SENSITIVITY);
            Debug.Log("Mouse sensitivity reset to default");
        };

        // Add to container
        container.Add(sliderComponent);

        return sliderComponent;
    }

    /// <summary>
    /// Creates and configures the Invert X selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateInvertXSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Invert X-Axis");

        // Create options list (On/Off)
        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption
            {
                displayName = "Off",
                value = "false",
                data = false
            },
            new SelectionComponent.SelectionOption
            {
                displayName = "On",
                value = "true",
                data = true
            }
        };

        // Determine current and saved state
        bool savedInvertX = prefsManager.InvertX;
        int currentIndex = savedInvertX ? 1 : 0;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_INVERT_X ? 1 : 0;

        selectionComponent.Initialize(options, currentIndex, defaultIndex);
        selectionComponent.SetTooltipText("Invert horizontal mouse movement");

        // Apply saved setting
        SetInvertX(savedInvertX);

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            bool invertValue = bool.Parse(option.value);
            SetInvertX(invertValue);
            prefsManager.SaveInvertX(invertValue);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetInvertX();
            SetInvertX(PlayerPreferenceSettingsManager.DEFAULT_INVERT_X);
            Debug.Log("Invert X reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures the Invert Y selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateInvertYSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Invert Y-Axis");

        // Create options list (On/Off)
        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption
            {
                displayName = "Off",
                value = "false",
                data = false
            },
            new SelectionComponent.SelectionOption
            {
                displayName = "On",
                value = "true",
                data = true
            }
        };

        // Determine current and saved state
        bool savedInvertY = prefsManager.InvertY;
        int currentIndex = savedInvertY ? 1 : 0;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_INVERT_Y ? 1 : 0;

        selectionComponent.Initialize(options, currentIndex, defaultIndex);
        selectionComponent.SetTooltipText("Invert vertical mouse movement");

        // Apply saved setting
        SetInvertY(savedInvertY);

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            bool invertValue = bool.Parse(option.value);
            SetInvertY(invertValue);
            prefsManager.SaveInvertY(invertValue);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetInvertY();
            SetInvertY(PlayerPreferenceSettingsManager.DEFAULT_INVERT_Y);
            Debug.Log("Invert Y reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures all game setting components at once
    /// </summary>
    /// <param name="container">The visual element container to add all components to</param>
    public void CreateAllGameSettings(VisualElement container)
    {
        CreateMouseSensitivitySlider(container);
        CreateInvertXSelector(container);
        CreateInvertYSelector(container);
    }

    #endregion
}
