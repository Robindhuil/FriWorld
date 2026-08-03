using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

public class VideoSettings : MonoBehaviour
{
    [Header("Player Preferences")]
    [SerializeField] private PlayerPreferenceSettingsManager prefsManager;

    private Volume ppVolume;

    #region Resolution Data
    
    // Common resolutions based on Steam Hardware Survey
    private static readonly Resolution[] CommonResolutions = new Resolution[]
    {
        new Resolution { width = 1920, height = 1080 },  // 16:9 - 59.70%
        new Resolution { width = 2560, height = 1440 },  // 16:9 - 16.02%
        new Resolution { width = 1366, height = 768 },   // 16:9 - 4.01%
        new Resolution { width = 3840, height = 2160 },  // 16:9 - 3.78%
        new Resolution { width = 2560, height = 1600 },  // 16:10 - 3.33%
    };

    private List<Resolution> availableResolutions;
    private Resolution currentResolution;

    #endregion

    #region Resolution Management

    private void Awake()
    {
        InitializeResolutions();
    }

    /// <summary>
    /// Initializes the available resolutions list
    /// </summary>
    private void InitializeResolutions()
    {
        // Get all supported resolutions from the system
        Resolution[] systemResolutions = Screen.resolutions;
        
        // Create a list to store unique resolutions
        availableResolutions = new List<Resolution>();

        // Add common resolutions that are supported by the system
        foreach (var commonRes in CommonResolutions)
        {
            // Check if this resolution is supported by the system
            bool isSupported = systemResolutions.Any(sysRes => 
                sysRes.width == commonRes.width && sysRes.height == commonRes.height);

            if (isSupported && !availableResolutions.Any(r => 
                r.width == commonRes.width && r.height == commonRes.height))
            {
                availableResolutions.Add(commonRes);
            }
        }

        // If no common resolutions are available, add all system resolutions
        if (availableResolutions.Count == 0)
        {
            availableResolutions = systemResolutions
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.First())
                .OrderByDescending(r => r.width * r.height)
                .ToList();
        }

        currentResolution = Screen.currentResolution;

        // Ensure the system's native resolution is always present in the list
        bool nativePresent = availableResolutions.Any(r =>
            r.width == currentResolution.width && r.height == currentResolution.height);
        if (!nativePresent)
            availableResolutions.Insert(0, currentResolution);

        Debug.Log($"VideoSettings: Found {availableResolutions.Count} available resolutions");
    }

    /// <summary>
    /// Sets the screen resolution
    /// </summary>
    /// <param name="width">Screen width</param>
    /// <param name="height">Screen height</param>
    /// <param name="fullscreenMode">Fullscreen mode</param>
    public void SetResolution(int width, int height, FullScreenMode fullscreenMode)
    {
        Screen.SetResolution(width, height, fullscreenMode);
        currentResolution = new Resolution { width = width, height = height };
        Debug.Log($"Resolution changed to: {width}x{height}, Mode: {fullscreenMode}");
    }

    /// <summary>
    /// Sets the screen resolution by index
    /// </summary>
    /// <param name="resolutionIndex">Index of the resolution in available resolutions list</param>
    /// <param name="fullscreenMode">Fullscreen mode</param>
    public void SetResolution(int resolutionIndex, FullScreenMode fullscreenMode)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Count)
        {
            Debug.LogError($"Invalid resolution index: {resolutionIndex}");
            return;
        }

        Resolution res = availableResolutions[resolutionIndex];
        SetResolution(res.width, res.height, fullscreenMode);
    }

    /// <summary>
    /// Sets the screen resolution using current fullscreen mode
    /// </summary>
    /// <param name="resolutionIndex">Index of the resolution</param>
    public void SetResolution(int resolutionIndex)
    {
        SetResolution(resolutionIndex, Screen.fullScreenMode);
    }

    /// <summary>
    /// Gets the list of available resolutions
    /// </summary>
    /// <returns>List of available resolutions</returns>
    public List<Resolution> GetAvailableResolutions()
    {
        return new List<Resolution>(availableResolutions);
    }

    /// <summary>
    /// Gets the current resolution
    /// </summary>
    /// <returns>Current resolution</returns>
    public Resolution GetCurrentResolution()
    {
        return currentResolution;
    }

    /// <summary>
    /// Gets the index of the current resolution
    /// </summary>
    /// <returns>Index of current resolution, or -1 if not found</returns>
    public int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == currentResolution.width &&
                availableResolutions[i].height == currentResolution.height)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets resolution as a formatted string
    /// </summary>
    /// <param name="resolution">The resolution</param>
    /// <returns>Formatted string like "1920 x 1080"</returns>
    public string GetResolutionString(Resolution resolution)
    {
        return $"{resolution.width} x {resolution.height}";
    }

    /// <summary>
    /// Gets all available resolutions as formatted strings
    /// </summary>
    /// <returns>Array of resolution strings</returns>
    public string[] GetResolutionStrings()
    {
        return availableResolutions.Select(r => GetResolutionString(r)).ToArray();
    }

    #endregion

    #region Fullscreen Mode Management

    /// <summary>
    /// Sets the fullscreen mode
    /// </summary>
    /// <param name="mode">The fullscreen mode to set</param>
    public void SetFullscreenMode(FullScreenMode mode)
    {
        Screen.fullScreenMode = mode;
        Debug.Log($"Fullscreen mode changed to: {mode}");
    }

    /// <summary>
    /// Sets fullscreen mode by index
    /// 0 = Fullscreen Window, 1 = Exclusive Fullscreen, 2 = Windowed
    /// </summary>
    /// <param name="modeIndex">Index of the fullscreen mode</param>
    public void SetFullscreenMode(int modeIndex)
    {
        FullScreenMode mode = modeIndex switch
        {
            0 => FullScreenMode.FullScreenWindow,
            1 => FullScreenMode.ExclusiveFullScreen,
            2 => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        SetFullscreenMode(mode);
    }

    /// <summary>
    /// Gets the current fullscreen mode
    /// </summary>
    /// <returns>Current fullscreen mode</returns>
    public FullScreenMode GetFullscreenMode()
    {
        return Screen.fullScreenMode;
    }

    /// <summary>
    /// Gets the current fullscreen mode as index
    /// </summary>
    /// <returns>Index of the current fullscreen mode</returns>
    public int GetFullscreenModeIndex()
    {
        return Screen.fullScreenMode switch
        {
            FullScreenMode.FullScreenWindow => 0,
            FullScreenMode.ExclusiveFullScreen => 1,
            FullScreenMode.Windowed => 2,
            _ => 0
        };
    }

    /// <summary>
    /// Gets fullscreen mode options as strings
    /// </summary>
    /// <returns>Array of fullscreen mode names</returns>
    public string[] GetFullscreenModeStrings()
    {
        return new string[]
        {
            "Fullscreen Window",
            "Exclusive Fullscreen",
            "Windowed"
        };
    }

    #endregion

    #region VSync Management

    /// <summary>
    /// Sets VSync (0 = off, 1 = on, 2 = every second frame)
    /// </summary>
    /// <param name="vsyncCount">VSync count</param>
    public void SetVSync(int vsyncCount)
    {
        QualitySettings.vSyncCount = Mathf.Clamp(vsyncCount, 0, 2);
        Debug.Log($"VSync set to: {vsyncCount}");
    }

    /// <summary>
    /// Enables or disables VSync
    /// </summary>
    /// <param name="enabled">True to enable VSync</param>
    public void SetVSync(bool enabled)
    {
        SetVSync(enabled ? 1 : 0);
    }

    /// <summary>
    /// Gets the current VSync count
    /// </summary>
    /// <returns>Current VSync count</returns>
    public int GetVSync()
    {
        return QualitySettings.vSyncCount;
    }

    /// <summary>
    /// Checks if VSync is enabled
    /// </summary>
    /// <returns>True if VSync is enabled</returns>
    public bool IsVSyncEnabled()
    {
        return QualitySettings.vSyncCount > 0;
    }

    #endregion

    #region Quality Settings

    /// <summary>
    /// Sets the overall quality level
    /// </summary>
    /// <param name="qualityIndex">Quality level index</param>
    public void SetQualityLevel(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        Debug.Log($"Quality level set to: {QualitySettings.names[qualityIndex]}");
    }

    /// <summary>
    /// Gets the current quality level index
    /// </summary>
    /// <returns>Current quality level index</returns>
    public int GetQualityLevel()
    {
        return QualitySettings.GetQualityLevel();
    }

    /// <summary>
    /// Gets all available quality level names
    /// </summary>
    /// <returns>Array of quality level names</returns>
    public string[] GetQualityLevelNames()
    {
        return QualitySettings.names;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Applies saved video settings from PlayerPrefs
    /// </summary>
    public void ApplySavedSettings()
    {
        if (prefsManager == null)
        {
            Debug.LogWarning("PlayerPreferenceSettingsManager not assigned!");
            return;
        }

        prefsManager.EnsureInitialized();

        // Apply saved resolution
        SetResolution(prefsManager.ResolutionWidth, prefsManager.ResolutionHeight, 
            (FullScreenMode)prefsManager.FullscreenMode);
        
        // Apply saved fullscreen mode
        SetFullscreenMode(prefsManager.FullscreenMode);
        
        // Apply saved VSync
        SetVSync(prefsManager.VSync);
        
        // Apply saved quality level
        SetQualityLevel(prefsManager.QualityLevel);

        // Apply saved post processing
        SetBloom(prefsManager.Bloom);
        SetDepthOfField(prefsManager.DepthOfField);
        SetMotionBlur(prefsManager.MotionBlur);
        SetGamma(prefsManager.Gamma);
        SetGain(prefsManager.Gain);
        SetContrast(prefsManager.Contrast);
        SetSaturation(prefsManager.Saturation);

        Debug.Log("Applied saved video settings");
    }

    /// <summary>
    /// Gets the aspect ratio of a resolution
    /// </summary>
    /// <param name="resolution">The resolution</param>
    /// <returns>Aspect ratio as a string (e.g., "16:9")</returns>
    public string GetAspectRatio(Resolution resolution)
    {
        float ratio = (float)resolution.width / resolution.height;
        
        // Common aspect ratios
        if (Mathf.Approximately(ratio, 16f / 9f)) return "16:9";
        if (Mathf.Approximately(ratio, 16f / 10f)) return "16:10";
        if (Mathf.Approximately(ratio, 4f / 3f)) return "4:3";
        if (Mathf.Approximately(ratio, 21f / 9f)) return "21:9";
        
        return $"{resolution.width}:{resolution.height}";
    }

    #endregion

    #region UI Component Creation

    /// <summary>
    /// Creates and configures the Resolution selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateResolutionSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Resolution");

        // Create options list
        var options = new List<SelectionComponent.SelectionOption>();
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            var res = availableResolutions[i];
            options.Add(new SelectionComponent.SelectionOption
            {
                displayName = GetResolutionString(res),
                value = i.ToString(),
                data = res
            });
        }

        // Find current resolution index
        int currentIndex = GetCurrentResolutionIndex();
        if (currentIndex == -1) currentIndex = 0;

        // Find saved resolution or use current as default
        int savedIndex = -1;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == prefsManager.ResolutionWidth &&
                availableResolutions[i].height == prefsManager.ResolutionHeight)
            {
                savedIndex = i;
                break;
            }
        }
        if (savedIndex == -1) savedIndex = currentIndex;

        // Default is the system's native resolution
        int defaultIndex = 0;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == currentResolution.width &&
                availableResolutions[i].height == currentResolution.height)
            {
                defaultIndex = i;
                break;
            }
        }

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Select screen resolution");

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            int index = int.Parse(option.value);
            Resolution res = availableResolutions[index];
            SetResolution(res.width, res.height, Screen.fullScreenMode);
            prefsManager.SaveResolution(res.width, res.height);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetResolution();
            Resolution res = availableResolutions[defaultIndex];
            SetResolution(res.width, res.height, Screen.fullScreenMode);
            Debug.Log("Resolution reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures the Fullscreen Mode selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateFullscreenSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Fullscreen");

        // Create options list (simplified to On/Off)
        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption
            {
                displayName = "On",
                value = "0",
                data = FullScreenMode.FullScreenWindow
            },
            new SelectionComponent.SelectionOption
            {
                displayName = "Off",
                value = "2",
                data = FullScreenMode.Windowed
            }
        };

        // Determine current mode (any fullscreen mode counts as "On")
        int currentIndex = Screen.fullScreenMode == FullScreenMode.Windowed ? 1 : 0;
        int savedIndex = prefsManager.FullscreenMode == 2 ? 1 : 0;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_FULLSCREEN_MODE == 2 ? 1 : 0;

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Toggle fullscreen mode");

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            int modeValue = int.Parse(option.value);
            SetFullscreenMode(modeValue);
            prefsManager.SaveFullscreenMode(modeValue);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetFullscreenMode();
            SetFullscreenMode(PlayerPreferenceSettingsManager.DEFAULT_FULLSCREEN_MODE);
            Debug.Log("Fullscreen mode reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures the VSync selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateVSyncSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("VSync");

        // Create options list (On/Off)
        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption
            {
                displayName = "On",
                value = "1",
                data = 1
            },
            new SelectionComponent.SelectionOption
            {
                displayName = "Off",
                value = "0",
                data = 0
            }
        };

        int currentIndex = QualitySettings.vSyncCount > 0 ? 0 : 1;
        int savedIndex = prefsManager.VSync > 0 ? 0 : 1;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_VSYNC > 0 ? 0 : 1;

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Enable or disable vertical sync");

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            int vsyncValue = int.Parse(option.value);
            SetVSync(vsyncValue);
            prefsManager.SaveVSync(vsyncValue);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetVSync();
            SetVSync(PlayerPreferenceSettingsManager.DEFAULT_VSYNC);
            Debug.Log("VSync reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures the Quality Level selector component
    /// </summary>
    /// <param name="container">The visual element container to add the selector to</param>
    /// <returns>The configured SelectionComponent</returns>
    public SelectionComponent CreateQualitySelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Quality");

        // Create options list from available quality levels
        string[] qualityNames = GetQualityLevelNames();
        var options = new List<SelectionComponent.SelectionOption>();
        for (int i = 0; i < qualityNames.Length; i++)
        {
            options.Add(new SelectionComponent.SelectionOption
            {
                displayName = qualityNames[i],
                value = i.ToString(),
                data = i
            });
        }

        int currentQuality = GetQualityLevel();
        int savedQuality = prefsManager.QualityLevel;
        int defaultQuality = PlayerPreferenceSettingsManager.DEFAULT_QUALITY_LEVEL;

        // Clamp values to valid range
        currentQuality = Mathf.Clamp(currentQuality, 0, qualityNames.Length - 1);
        savedQuality = Mathf.Clamp(savedQuality, 0, qualityNames.Length - 1);
        defaultQuality = Mathf.Clamp(defaultQuality, 0, qualityNames.Length - 1);

        selectionComponent.Initialize(options, savedQuality, defaultQuality);
        selectionComponent.SetTooltipText("Select graphics quality preset");

        // Hook up value changed event
        selectionComponent.OnSelectionChanged += (option) =>
        {
            int qualityIndex = int.Parse(option.value);
            SetQualityLevel(qualityIndex);
            prefsManager.SaveQualityLevel(qualityIndex);
        };

        // Hook up reset to default event
        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetQualityLevel();
            SetQualityLevel(PlayerPreferenceSettingsManager.DEFAULT_QUALITY_LEVEL);
            Debug.Log("Quality level reset to default");
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    /// <summary>
    /// Creates and configures all video setting selectors at once
    /// </summary>
    /// <param name="container">The visual element container to add all selectors to</param>
    public void CreateAllVideoSelectors(VisualElement container)
    {
        CreateResolutionSelector(container);
        CreateFullscreenSelector(container);
        CreateVSyncSelector(container);
        CreateQualitySelector(container);
        CreateBloomSelector(container);
        CreateDepthOfFieldSelector(container);
        CreateMotionBlurSelector(container);
        CreateGammaSlider(container);
        CreateGainSlider(container);
        CreateContrastSlider(container);
        CreateSaturationSlider(container);
    }

    #endregion

    #region Post Processing

    public void SetPostProcessingVolume(Volume volume)
    {
        ppVolume = volume;
        if (prefsManager == null) return;
        prefsManager.EnsureInitialized();
        SetBloom(prefsManager.Bloom);
        SetDepthOfField(prefsManager.DepthOfField);
        SetMotionBlur(prefsManager.MotionBlur);
        SetGamma(prefsManager.Gamma);
        SetGain(prefsManager.Gain);
        SetContrast(prefsManager.Contrast);
        SetSaturation(prefsManager.Saturation);
    }

    public void SetBloom(bool enabled)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<Bloom>(out Bloom bloom))
            bloom.active = enabled;
    }

    public void SetDepthOfField(bool enabled)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<DepthOfField>(out DepthOfField dof))
            dof.active = enabled;
    }

    public void SetMotionBlur(bool enabled)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<MotionBlur>(out MotionBlur motionBlur))
            motionBlur.active = enabled;
    }

    // userValue range [0,2]: 1 = neutral (W=0), maps to W = userValue - 1
    public void SetGamma(float userValue)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<LiftGammaGain>(out LiftGammaGain lgg))
        {
            var v = lgg.gamma.value;
            v.w = userValue - 1f;
            lgg.gamma.value = v;
        }
    }

    public void SetGain(float userValue)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<LiftGammaGain>(out LiftGammaGain lgg))
        {
            var v = lgg.gain.value;
            v.w = userValue - 1f;
            lgg.gain.value = v;
        }
    }

    public void SetContrast(float urpValue)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments ca))
            ca.contrast.value = urpValue;
    }

    public void SetSaturation(float urpValue)
    {
        if (ppVolume != null && ppVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments ca))
            ca.saturation.value = urpValue;
    }

    public SelectionComponent CreateBloomSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Bloom");

        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption { displayName = "On", value = "1", data = true },
            new SelectionComponent.SelectionOption { displayName = "Off", value = "0", data = false }
        };

        int savedIndex = prefsManager.Bloom ? 0 : 1;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_BLOOM ? 0 : 1;

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Enable or disable bloom effect");

        selectionComponent.OnSelectionChanged += (option) =>
        {
            bool enabled = option.value == "1";
            SetBloom(enabled);
            prefsManager.SaveBloom(enabled);
        };

        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetBloom();
            SetBloom(PlayerPreferenceSettingsManager.DEFAULT_BLOOM);
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    public SelectionComponent CreateDepthOfFieldSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Depth of Field");

        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption { displayName = "On", value = "1", data = true },
            new SelectionComponent.SelectionOption { displayName = "Off", value = "0", data = false }
        };

        int savedIndex = prefsManager.DepthOfField ? 0 : 1;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_DEPTH_OF_FIELD ? 0 : 1;

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Enable or disable depth of field effect");

        selectionComponent.OnSelectionChanged += (option) =>
        {
            bool enabled = option.value == "1";
            SetDepthOfField(enabled);
            prefsManager.SaveDepthOfField(enabled);
        };

        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetDepthOfField();
            SetDepthOfField(PlayerPreferenceSettingsManager.DEFAULT_DEPTH_OF_FIELD);
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    public SelectionComponent CreateMotionBlurSelector(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var selectionComponent = new SelectionComponent();
        selectionComponent.SetNameText("Motion Blur");

        var options = new List<SelectionComponent.SelectionOption>
        {
            new SelectionComponent.SelectionOption { displayName = "On", value = "1", data = true },
            new SelectionComponent.SelectionOption { displayName = "Off", value = "0", data = false }
        };

        int savedIndex = prefsManager.MotionBlur ? 0 : 1;
        int defaultIndex = PlayerPreferenceSettingsManager.DEFAULT_MOTION_BLUR ? 0 : 1;

        selectionComponent.Initialize(options, savedIndex, defaultIndex);
        selectionComponent.SetTooltipText("Enable or disable motion blur effect");

        selectionComponent.OnSelectionChanged += (option) =>
        {
            bool enabled = option.value == "1";
            SetMotionBlur(enabled);
            prefsManager.SaveMotionBlur(enabled);
        };

        selectionComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetMotionBlur();
            SetMotionBlur(PlayerPreferenceSettingsManager.DEFAULT_MOTION_BLUR);
        };

        container.Add(selectionComponent);
        return selectionComponent;
    }

    public SliderComponent CreateGammaSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var slider = new SliderComponent();
        slider.Initialize("Gamma", 0f, 200f, PlayerPreferenceSettingsManager.DEFAULT_GAMMA * 100f, true);
        slider.TooltipText = "Adjust midtone brightness (gamma)";
        slider.SetValueWithoutNotify(prefsManager.Gamma * 100f);

        slider.OnValueChanged += (val) =>
        {
            float userValue = val / 100f;
            SetGamma(userValue);
            prefsManager.SaveGamma(userValue);
        };

        slider.OnResetToDefault += () =>
        {
            prefsManager.ResetGamma();
            SetGamma(PlayerPreferenceSettingsManager.DEFAULT_GAMMA);
        };

        container.Add(slider);
        return slider;
    }

    public SliderComponent CreateGainSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var slider = new SliderComponent();
        slider.Initialize("Brightness", 0f, 200f, PlayerPreferenceSettingsManager.DEFAULT_GAIN * 100f, true);
        slider.TooltipText = "Adjust highlight brightness (gain)";
        slider.SetValueWithoutNotify(prefsManager.Gain * 100f);

        slider.OnValueChanged += (val) =>
        {
            float userValue = val / 100f;
            SetGain(userValue);
            prefsManager.SaveGain(userValue);
        };

        slider.OnResetToDefault += () =>
        {
            prefsManager.ResetGain();
            SetGain(PlayerPreferenceSettingsManager.DEFAULT_GAIN);
        };

        container.Add(slider);
        return slider;
    }

    public SliderComponent CreateContrastSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        float defaultPct = (PlayerPreferenceSettingsManager.DEFAULT_CONTRAST + 100f) / 2f;

        var slider = new SliderComponent();
        slider.Initialize("Contrast", 0f, 100f, defaultPct, false);
        slider.SetLevelFormat("{0:0.#}%");
        slider.TooltipText = "Adjust image contrast";
        slider.SetValueWithoutNotify((prefsManager.Contrast + 100f) / 2f);

        slider.OnValueChanged += (val) =>
        {
            float urp = val * 2f - 100f;
            SetContrast(urp);
            prefsManager.SaveContrast(urp);
        };

        slider.OnResetToDefault += () =>
        {
            prefsManager.ResetContrast();
            SetContrast(PlayerPreferenceSettingsManager.DEFAULT_CONTRAST);
        };

        container.Add(slider);
        return slider;
    }

    public SliderComponent CreateSaturationSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        float defaultPct = (PlayerPreferenceSettingsManager.DEFAULT_SATURATION + 100f) / 2f;

        var slider = new SliderComponent();
        slider.Initialize("Saturation", 0f, 100f, defaultPct, false);
        slider.SetLevelFormat("{0:0.#}%");
        slider.TooltipText = "Adjust color saturation";
        slider.SetValueWithoutNotify((prefsManager.Saturation + 100f) / 2f);

        slider.OnValueChanged += (val) =>
        {
            float urp = val * 2f - 100f;
            SetSaturation(urp);
            prefsManager.SaveSaturation(urp);
        };

        slider.OnResetToDefault += () =>
        {
            prefsManager.ResetSaturation();
            SetSaturation(PlayerPreferenceSettingsManager.DEFAULT_SATURATION);
        };

        container.Add(slider);
        return slider;
    }

    #endregion
}
