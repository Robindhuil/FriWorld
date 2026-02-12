using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Player Preferences")]
    [SerializeField] private PlayerPreferenceSettingsManager prefsManager;

    [Header("Mixer Parameter Names")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string sfxVolumeParameter = "SfxVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";

    #region Volume Constants
    private const float MIN_DECIBEL = -80f;
    private const float MAX_DECIBEL = 0f;
    #endregion

    #region Set Volume Functions
    /// <summary>
    /// Sets the Master volume in the audio mixer
    /// </summary>
    /// <param name="value">Linear volume value (0-1)</param>
    public void SetMasterVolume(float value)
    {
        float decibels = ConvertToDecibel(value);
        audioMixer.SetFloat(masterVolumeParameter, decibels);
    }

    /// <summary>
    /// Sets the SFX volume in the audio mixer
    /// </summary>
    /// <param name="value">Linear volume value (0-1)</param>
    public void SetSfxVolume(float value)
    {
        float decibels = ConvertToDecibel(value);
        audioMixer.SetFloat(sfxVolumeParameter, decibels);
    }

    /// <summary>
    /// Sets the Music volume in the audio mixer
    /// </summary>
    /// <param name="value">Linear volume value (0-1)</param>
    public void SetMusicVolume(float value)
    {
        float decibels = ConvertToDecibel(value);
        audioMixer.SetFloat(musicVolumeParameter, decibels);
    }

    /// <summary>
    /// Sets all audio volumes at once
    /// </summary>
    /// <param name="masterVolume">Master volume (0-1)</param>
    /// <param name="sfxVolume">SFX volume (0-1)</param>
    /// <param name="musicVolume">Music volume (0-1)</param>
    public void SetAllVolumes(float masterVolume, float sfxVolume, float musicVolume)
    {
        SetMasterVolume(masterVolume);
        SetSfxVolume(sfxVolume);
        SetMusicVolume(musicVolume);
    }
    #endregion

    #region Get Volume Functions
    /// <summary>
    /// Gets the current Master volume from the audio mixer
    /// </summary>
    /// <returns>Linear volume value (0-1)</returns>
    public float GetMasterVolume()
    {
        audioMixer.GetFloat(masterVolumeParameter, out float decibels);
        return ConvertToLinear(decibels);
    }

    /// <summary>
    /// Gets the current SFX volume from the audio mixer
    /// </summary>
    /// <returns>Linear volume value (0-1)</returns>
    public float GetSfxVolume()
    {
        audioMixer.GetFloat(sfxVolumeParameter, out float decibels);
        return ConvertToLinear(decibels);
    }

    /// <summary>
    /// Gets the current Music volume from the audio mixer
    /// </summary>
    /// <returns>Linear volume value (0-1)</returns>
    public float GetMusicVolume()
    {
        audioMixer.GetFloat(musicVolumeParameter, out float decibels);
        return ConvertToLinear(decibels);
    }
    #endregion

    #region Conversion Functions
    /// <summary>
    /// Converts linear volume (0-1) to decibels for the audio mixer
    /// </summary>
    /// <param name="linearValue">Linear volume value (0-1)</param>
    /// <returns>Volume in decibels</returns>
    private float ConvertToDecibel(float linearValue)
    {
        // Clamp the value between 0 and 1
        linearValue = Mathf.Clamp01(linearValue);

        // If value is 0, return minimum decibel to mute
        if (linearValue <= 0f)
            return MIN_DECIBEL;

        // Convert to decibels: 20 * log10(value)
        return Mathf.Clamp(Mathf.Log10(linearValue) * 20f, MIN_DECIBEL, MAX_DECIBEL);
    }

    /// <summary>
    /// Converts decibels to linear volume (0-1)
    /// </summary>
    /// <param name="decibelValue">Volume in decibels</param>
    /// <returns>Linear volume value (0-1)</returns>
    private float ConvertToLinear(float decibelValue)
    {
        // If at minimum decibel, return 0
        if (decibelValue <= MIN_DECIBEL)
            return 0f;

        // Convert from decibels: 10^(db/20)
        return Mathf.Pow(10f, decibelValue / 20f);
    }
    #endregion

    #region Utility Functions
    /// <summary>
    /// Mutes all audio
    /// </summary>
    public void MuteAll()
    {
        SetAllVolumes(0f, 0f, 0f);
    }

    /// <summary>
    /// Unmutes all audio to maximum
    /// </summary>
    public void UnmuteAll()
    {
        SetAllVolumes(1f, 1f, 1f);
    }
    #endregion

    #region UI Slider Setup
    /// <summary>
    /// Creates and configures the Master Volume slider component
    /// </summary>
    /// <param name="container">The visual element container to add the slider to</param>
    /// <returns>The configured SliderComponent</returns>
    public SliderComponent CreateMasterVolumeSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var sliderComponent = new SliderComponent();
        
        // Configure slider properties
        sliderComponent.Initialize(
            name: "Master Volume",
            min: 0f,
            max: 100f,
            defaultVal: PlayerPreferenceSettingsManager.DEFAULT_MASTER_VOLUME * 100f,
            wholeNumbers: true
        );

        sliderComponent.TooltipText = "Controls the overall game volume";

        // Load saved value and convert from 0-1 to 0-100
        float savedVolume = prefsManager.MasterVolume * 100f;
        sliderComponent.SetValueWithoutNotify(savedVolume);

        // Apply the loaded value to the audio mixer
        SetMasterVolume(prefsManager.MasterVolume);

        // Hook up value changed event
        sliderComponent.OnValueChanged += (value) =>
        {
            // Convert from 0-100 to 0-1
            float linearValue = value / 100f;
            SetMasterVolume(linearValue);
            prefsManager.SaveMasterVolume(linearValue);
        };

        // Hook up reset to default event
        sliderComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetMasterVolume();
            SetMasterVolume(PlayerPreferenceSettingsManager.DEFAULT_MASTER_VOLUME);
            Debug.Log("Master Volume reset to default");
        };

        // Add to container
        container.Add(sliderComponent);

        return sliderComponent;
    }

    /// <summary>
    /// Creates and configures the SFX Volume slider component
    /// </summary>
    /// <param name="container">The visual element container to add the slider to</param>
    /// <returns>The configured SliderComponent</returns>
    public SliderComponent CreateSfxVolumeSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var sliderComponent = new SliderComponent();
        
        // Configure slider properties
        sliderComponent.Initialize(
            name: "SFX Volume",
            min: 0f,
            max: 100f,
            defaultVal: PlayerPreferenceSettingsManager.DEFAULT_SFX_VOLUME * 100f,
            wholeNumbers: true
        );

        sliderComponent.TooltipText = "Controls sound effects volume";

        // Load saved value and convert from 0-1 to 0-100
        float savedVolume = prefsManager.SfxVolume * 100f;
        sliderComponent.SetValueWithoutNotify(savedVolume);

        // Apply the loaded value to the audio mixer
        SetSfxVolume(prefsManager.SfxVolume);

        // Hook up value changed event
        sliderComponent.OnValueChanged += (value) =>
        {
            // Convert from 0-100 to 0-1
            float linearValue = value / 100f;
            SetSfxVolume(linearValue);
            prefsManager.SaveSfxVolume(linearValue);
        };

        // Hook up reset to default event
        sliderComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetSfxVolume();
            SetSfxVolume(PlayerPreferenceSettingsManager.DEFAULT_SFX_VOLUME);
            Debug.Log("SFX Volume reset to default");
        };

        // Add to container
        container.Add(sliderComponent);

        return sliderComponent;
    }

    /// <summary>
    /// Creates and configures the Music Volume slider component
    /// </summary>
    /// <param name="container">The visual element container to add the slider to</param>
    /// <returns>The configured SliderComponent</returns>
    public SliderComponent CreateMusicVolumeSlider(VisualElement container)
    {
        if (prefsManager == null)
        {
            Debug.LogError("PlayerPreferenceSettingsManager is not assigned!");
            return null;
        }

        var sliderComponent = new SliderComponent();
        
        // Configure slider properties
        sliderComponent.Initialize(
            name: "Music Volume",
            min: 0f,
            max: 100f,
            defaultVal: PlayerPreferenceSettingsManager.DEFAULT_MUSIC_VOLUME * 100f,
            wholeNumbers: true
        );

        sliderComponent.TooltipText = "Controls background music volume";

        // Load saved value and convert from 0-1 to 0-100
        float savedVolume = prefsManager.MusicVolume * 100f;
        sliderComponent.SetValueWithoutNotify(savedVolume);

        // Apply the loaded value to the audio mixer
        SetMusicVolume(prefsManager.MusicVolume);

        // Hook up value changed event
        sliderComponent.OnValueChanged += (value) =>
        {
            // Convert from 0-100 to 0-1
            float linearValue = value / 100f;
            SetMusicVolume(linearValue);
            prefsManager.SaveMusicVolume(linearValue);
        };

        // Hook up reset to default event
        sliderComponent.OnResetToDefault += () =>
        {
            prefsManager.ResetMusicVolume();
            SetMusicVolume(PlayerPreferenceSettingsManager.DEFAULT_MUSIC_VOLUME);
            Debug.Log("Music Volume reset to default");
        };

        // Add to container
        container.Add(sliderComponent);

        return sliderComponent;
    }

    /// <summary>
    /// Creates and configures all audio sliders at once
    /// </summary>
    /// <param name="container">The visual element container to add all sliders to</param>
    public void CreateAllAudioSliders(VisualElement container)
    {
        CreateMasterVolumeSlider(container);
        CreateSfxVolumeSlider(container);
        CreateMusicVolumeSlider(container);
    }
    #endregion
}
