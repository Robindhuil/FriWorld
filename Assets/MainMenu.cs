using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    #region UI References
    [Header("Gameplay Settings UI")]
    public Slider sensitivitySlider;
    public Toggle invertYToggle;

    [Header("Video Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle vSyncToggle;
    public TMP_Dropdown qualityDropdown;
    public Slider brightnessSlider;

    [Header("Audio Settings UI")]
    [SerializeField] private AudioMixer audioMixer;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Music Info UI")]
    [SerializeField] private BackgroundMusic backgroundMusic;
    [SerializeField] private GameObject musicPanel;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;
    private float lastUrlClickTime;
    private const float URL_CLICK_COOLDOWN = 1.0f;

    #endregion

    #region Private Variables
    private List<Vector2Int> availableResolutions;
    private Button authorButton;
    private Button songNameButton;
    private bool urlOpened = false;
    private PlayerLook playerLook;
    private readonly Vector2Int[] customResolutions = new Vector2Int[]
    {
        new Vector2Int(1280, 720),   // HD
        new Vector2Int(1600, 900),   // HD+
        new Vector2Int(1920, 1080),  // Full HD
        new Vector2Int(2560, 1440),  // QHD
        new Vector2Int(3840, 2160)   // 4K
    };
    #endregion

    #region Initialization
    private void Awake()
    {
        Screen.SetResolution(1920, 1080, true);

        InitializePlayerLook();
        VerifyUIElements();
        InitializeGameplaySettings();
        InitializeVideoSettings();
        InitializeAudioSettings();
        InitializeQualitySettings();
        InitializeFadeImage();
        InitializeMusicSystem();
        InitializeMusicPanel();
    }

    private void InitializePlayerLook()
    {
        if (playerLook == null)
        {
            playerLook = FindFirstObjectByType<PlayerLook>();
            if (playerLook == null)
            {
                Debug.LogWarning("PlayerLook reference not found!");
            }
        }
    }

    private void VerifyUIElements()
    {
        if (sensitivitySlider == null) Debug.LogError("Sensitivity Slider not assigned!");
        if (invertYToggle == null) Debug.LogError("Invert Y Toggle not assigned!");
        if (resolutionDropdown == null) Debug.LogError("Resolution Dropdown not assigned!");
        if (fullscreenToggle == null) Debug.LogError("Fullscreen Toggle not assigned!");
        if (vSyncToggle == null) Debug.LogError("VSync Toggle not assigned!");
        if (qualityDropdown == null) Debug.LogError("Quality Dropdown not assigned!");
        if (brightnessSlider == null) Debug.LogError("Brightness Slider not assigned!");
        if (masterVolumeSlider == null) Debug.LogError("Master Volume Slider not assigned!");
    }

    private void InitializeGameplaySettings()
    {
        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 50f;
        sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        playerLook?.SetSensitivity(sensitivitySlider.value);

        invertYToggle.isOn = PlayerPrefs.GetInt("InvertMouseY", 0) == 1;
        playerLook?.SetInvertY(invertYToggle.isOn);
    }

    private void InitializeVideoSettings()
    {
        availableResolutions = new List<Vector2Int>(customResolutions);

        availableResolutions.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));

        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            string option = $"{availableResolutions[i].x} x {availableResolutions[i].y}";
            options.Add(option);

            if (availableResolutions[i].x == Screen.currentResolution.width &&
                availableResolutions[i].y == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);
        resolutionDropdown.RefreshShownValue();
        OnResolutionChanged(resolutionDropdown.value);

        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = fullscreenToggle.isOn;

        vSyncToggle.isOn = PlayerPrefs.GetInt("VSync", 0) == 1;
        QualitySettings.vSyncCount = vSyncToggle.isOn ? 1 : 0;

        qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        qualityDropdown.RefreshShownValue();
        QualitySettings.SetQualityLevel(qualityDropdown.value);

        brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 0.5f);
    }

    private void InitializeAudioSettings()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        masterVolumeSlider.value = masterVol;
        musicVolumeSlider.value = musicVol;
        sfxVolumeSlider.value = sfxVol;

        SetAudioLevel("MasterVolume", masterVol);
        SetAudioLevel("MusicVolume", musicVol);
        SetAudioLevel("SFXVolume", sfxVol);
    }

    private void InitializeQualitySettings()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        int savedQualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        qualityDropdown.value = savedQualityLevel;
        qualityDropdown.RefreshShownValue();
        QualitySettings.SetQualityLevel(savedQualityLevel);
    }

    private void InitializeFadeImage()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(true);
        }
    }

    private void InitializeMusicSystem()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void InitializeMusicPanel()
    {
        if (backgroundMusic != null && musicPanel != null)
        {
            authorButton = musicPanel.transform.Find("Author")?.GetComponent<Button>();
            songNameButton = musicPanel.transform.Find("SongName")?.GetComponent<Button>();

            if (authorButton != null && songNameButton != null)
            {
                authorButton.onClick.RemoveAllListeners();
                songNameButton.onClick.RemoveAllListeners();

                authorButton.onClick.AddListener(() => OnMusicInfoClicked());
                songNameButton.onClick.AddListener(() => OnMusicInfoClicked());
            }
        }
    }
    #endregion

    #region UI Callbacks
    public void OnSensitivityChanged(float value)
    {
        playerLook?.SetSensitivity(value);
        Debug.Log($"Mouse sensitivity changed to: {value}");
    }

    public void OnInvertYChanged(bool isInverted)
    {
        PlayerPrefs.SetInt("InvertMouseY", isInverted ? 1 : 0);
        playerLook?.SetInvertY(isInverted);
        Debug.Log($"Invert Y axis: {isInverted}");
    }

    public void OnResolutionChanged(int index)
    {
        Vector2Int res = availableResolutions[index];
        Screen.SetResolution(res.x, res.y, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", index);
        Debug.Log($"Resolution changed to: {res.x}x{res.y}");
    }

    public void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        Debug.Log($"Fullscreen: {isFullscreen}");
    }

    public void OnVSyncToggle(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isOn ? 1 : 0);
        Debug.Log($"VSync: {isOn}");
    }

    public void OnGraphicsQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("QualityLevel", index);
        Debug.Log($"Graphics quality changed to: {QualitySettings.names[index]}");
    }

    public void OnBrightnessChanged(float value)
    {
        PlayerPrefs.SetFloat("Brightness", value);
        Debug.Log($"Brightness changed to: {value}");
    }

    public void OnMasterVolumeChanged(float value)
    {
        Debug.Log($"Master volume changed to: {value}");
    }

    public void OnMusicVolumeChanged(float value)
    {
        Debug.Log($"Music volume changed to: {value}");
    }

    public void OnSFXVolumeChanged(float value)
    {
        Debug.Log($"SFX volume changed to: {value}");
    }

    private void OnMusicInfoClicked()
    {
        if (Time.time - lastUrlClickTime < URL_CLICK_COOLDOWN) return;
        if (backgroundMusic == null) return;

        var currentSong = backgroundMusic.CurrentSong;
        if (currentSong?.urls == null) return;

        foreach (var url in currentSong.urls.Distinct())
        {
            Application.OpenURL(url);
        }

        lastUrlClickTime = Time.time;
    }
    #endregion

    #region Utility Methods
    private void SetAudioLevel(string exposedParam, float sliderValue)
    {
        float dB = sliderValue > 0 ? Mathf.Log10(sliderValue) * 20 : -80f;
        audioMixer.SetFloat(exposedParam, dB);
    }

    private IEnumerator ResetUrlOpened(float delay)
    {
        yield return new WaitForSeconds(delay);
        urlOpened = false;
    }
    #endregion

    #region Settings Apply Methods
    public void ApplyGameplaySettings()
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("InvertMouseY", invertYToggle.isOn ? 1 : 0);

        playerLook?.SetSensitivity(sensitivitySlider.value);
        playerLook?.SetInvertY(invertYToggle.isOn);

        PlayerPrefs.Save();
        Debug.Log("Gameplay settings saved and applied.");
    }

    public void ApplyVideoSettings()
    {
        int index = resolutionDropdown.value;
        Vector2Int res = availableResolutions[index];
        Screen.SetResolution(res.x, res.y, fullscreenToggle.isOn);

        QualitySettings.vSyncCount = vSyncToggle.isOn ? 1 : 0;
        QualitySettings.SetQualityLevel(qualityDropdown.value);

        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("VSync", vSyncToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
        PlayerPrefs.SetFloat("Brightness", brightnessSlider.value);

        PlayerPrefs.Save();
        Debug.Log("Video settings saved and applied.");
    }

    public void ApplyAudioSettings()
    {
        SetAudioLevel("MasterVolume", masterVolumeSlider.value);
        SetAudioLevel("MusicVolume", musicVolumeSlider.value);
        SetAudioLevel("SFXVolume", sfxVolumeSlider.value);

        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        PlayerPrefs.Save();
        Debug.Log("Audio settings saved and applied.");
    }
    #endregion

    #region Scene Management
    public void PlayGame()
    {
        StartCoroutine(PlayGameRoutine());
    }

    private IEnumerator PlayGameRoutine()
    {
        float t = 0f;
        Color color = fadeImage != null ? fadeImage.color : Color.black;
        float startVolume = musicSource != null ? musicSource.volume : 1f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float normalized = t / fadeDuration;

            if (fadeImage != null)
            {
                color.a = Mathf.Lerp(0f, 1f, normalized);
                fadeImage.color = color;
            }

            if (musicSource != null)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, normalized);
            }

            yield return null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        SceneManager.LoadSceneAsync(1).allowSceneActivation = true;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

    #region Music Panel
    public void ShowMusicPanel()
    {
        if (backgroundMusic == null || musicPanel == null) return;

        var currentSong = backgroundMusic.CurrentSong;
        if (currentSong == null) return;

        if (authorButton != null)
        {
            var authorText = authorButton.GetComponentInChildren<TextMeshProUGUI>();
            if (authorText != null) authorText.text = currentSong.author ?? "Unknown Author";
        }

        if (songNameButton != null)
        {
            var songText = songNameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (songText != null) songText.text = currentSong.songName ?? "Unknown Song";
        }
    }

    public void OpenUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
    }
    #endregion

    #region Reset Settings
    public void ResetToDefaults()
    {
        sensitivitySlider.value = 1.0f;
        invertYToggle.isOn = false;

        resolutionDropdown.value = 0;
        fullscreenToggle.isOn = true;
        vSyncToggle.isOn = false;
        qualityDropdown.value = QualitySettings.names.Length - 1;
        brightnessSlider.value = 0.5f;

        masterVolumeSlider.value = 1.0f;
        musicVolumeSlider.value = 1.0f;
        sfxVolumeSlider.value = 1.0f;

        ApplyGameplaySettings();
        ApplyVideoSettings();
        ApplyAudioSettings();
    }
    #endregion
}