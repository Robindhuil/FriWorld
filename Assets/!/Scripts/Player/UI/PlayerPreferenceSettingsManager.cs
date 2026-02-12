using UnityEngine;

public class PlayerPreferenceSettingsManager : MonoBehaviour
{
    #region PlayerPrefs Keys
    // Audio Keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SfxVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    
    // Video Keys
    private const string RESOLUTION_WIDTH_KEY = "ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "ResolutionHeight";
    private const string FULLSCREEN_MODE_KEY = "FullscreenMode";
    private const string VSYNC_KEY = "VSync";
    private const string QUALITY_LEVEL_KEY = "QualityLevel";
    
    // Game Keys
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string INVERT_X_KEY = "InvertX";
    private const string INVERT_Y_KEY = "InvertY";
    
    private const string FIRST_RUN_KEY = "FirstRun";
    #endregion

    #region Default Values
    // Audio Defaults
    public static readonly float DEFAULT_MASTER_VOLUME = 0.5f;
    public static readonly float DEFAULT_SFX_VOLUME = 0.5f;
    public static readonly float DEFAULT_MUSIC_VOLUME = 0.5f;
    
    // Video Defaults
    public static readonly int DEFAULT_RESOLUTION_WIDTH = 1920;
    public static readonly int DEFAULT_RESOLUTION_HEIGHT = 1080;
    public static readonly int DEFAULT_FULLSCREEN_MODE = 0; // FullScreenWindow
    public static readonly int DEFAULT_VSYNC = 1; // On
    public static readonly int DEFAULT_QUALITY_LEVEL = 2; // Medium (typically)
    
    // Game Defaults
    public static readonly float DEFAULT_MOUSE_SENSITIVITY = 800f; // DPI (400-1600 range)
    public static readonly bool DEFAULT_INVERT_X = false;
    public static readonly bool DEFAULT_INVERT_Y = false;
    #endregion

    #region Audio Settings Properties
    public float MasterVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public float MusicVolume { get; private set; }
    #endregion
    
    #region Video Settings Properties
    public int ResolutionWidth { get; private set; }
    public int ResolutionHeight { get; private set; }
    public int FullscreenMode { get; private set; }
    public int VSync { get; private set; }
    public int QualityLevel { get; private set; }
    #endregion
    
    #region Game Settings Properties
    public float MouseSensitivity { get; private set; }
    public bool InvertX { get; private set; }
    public bool InvertY { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializePreferences();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes preferences - loads existing values or sets defaults on first run
    /// </summary>
    private void InitializePreferences()
    {
        if (IsFirstRun())
        {
            SetAllDefaults();
            MarkFirstRunComplete();
        }
        else
        {
            LoadAllPreferences();
        }
    }

    /// <summary>
    /// Checks if this is the first time the game is run
    /// </summary>
    /// <returns>True if first run, false otherwise</returns>
    private bool IsFirstRun()
    {
        return !PlayerPrefs.HasKey(FIRST_RUN_KEY);
    }

    /// <summary>
    /// Marks that the first run has been completed
    /// </summary>
    private void MarkFirstRunComplete()
    {
        PlayerPrefs.SetInt(FIRST_RUN_KEY, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Sets all settings to default values and saves them
    /// </summary>
    private void SetAllDefaults()
    {
        SaveMasterVolume(DEFAULT_MASTER_VOLUME);
        SaveSfxVolume(DEFAULT_SFX_VOLUME);
        SaveMusicVolume(DEFAULT_MUSIC_VOLUME);
        SaveResolution(DEFAULT_RESOLUTION_WIDTH, DEFAULT_RESOLUTION_HEIGHT);
        SaveFullscreenMode(DEFAULT_FULLSCREEN_MODE);
        SaveVSync(DEFAULT_VSYNC);
        SaveQualityLevel(DEFAULT_QUALITY_LEVEL);
        SaveMouseSensitivity(DEFAULT_MOUSE_SENSITIVITY);
        SaveInvertX(DEFAULT_INVERT_X);
        SaveInvertY(DEFAULT_INVERT_Y);
    }
    #endregion

    #region Load All Preferences
    /// <summary>
    /// Loads all player preferences from PlayerPrefs
    /// </summary>
    public void LoadAllPreferences()
    {
        LoadAudioSettings();
        LoadVideoSettings();
        LoadGameSettings();
    }
    #endregion

    #region Individual Load Functions
    /// <summary>
    /// Loads the Master Volume preference
    /// </summary>
    public void LoadMasterVolume()
    {
        MasterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
    }

    /// <summary>
    /// Loads the SFX Volume preference
    /// </summary>
    public void LoadSfxVolume()
    {
        SfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
    }

    /// <summary>
    /// Loads the Music Volume preference
    /// </summary>
    public void LoadMusicVolume()
    {
        MusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
    }
    
    /// <summary>
    /// Loads the Resolution Width preference
    /// </summary>
    public void LoadResolutionWidth()
    {
        ResolutionWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY, DEFAULT_RESOLUTION_WIDTH);
    }
    
    /// <summary>
    /// Loads the Resolution Height preference
    /// </summary>
    public void LoadResolutionHeight()
    {
        ResolutionHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY, DEFAULT_RESOLUTION_HEIGHT);
    }
    
    /// <summary>
    /// Loads both Resolution Width and Height preferences
    /// </summary>
    public void LoadResolution()
    {
        LoadResolutionWidth();
        LoadResolutionHeight();
    }
    
    /// <summary>
    /// Loads the Fullscreen Mode preference
    /// </summary>
    public void LoadFullscreenMode()
    {
        FullscreenMode = PlayerPrefs.GetInt(FULLSCREEN_MODE_KEY, DEFAULT_FULLSCREEN_MODE);
    }
    
    /// <summary>
    /// Loads the VSync preference
    /// </summary>
    public void LoadVSync()
    {
        VSync = PlayerPrefs.GetInt(VSYNC_KEY, DEFAULT_VSYNC);
    }
    
    /// <summary>
    /// Loads the Quality Level preference
    /// </summary>
    public void LoadQualityLevel()
    {
        QualityLevel = PlayerPrefs.GetInt(QUALITY_LEVEL_KEY, DEFAULT_QUALITY_LEVEL);
    }
    
    /// <summary>
    /// Loads the Mouse Sensitivity preference
    /// </summary>
    public void LoadMouseSensitivity()
    {
        MouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, DEFAULT_MOUSE_SENSITIVITY);
    }
    
    /// <summary>
    /// Loads the Invert X preference
    /// </summary>
    public void LoadInvertX()
    {
        InvertX = PlayerPrefs.GetInt(INVERT_X_KEY, DEFAULT_INVERT_X ? 1 : 0) == 1;
    }
    
    /// <summary>
    /// Loads the Invert Y preference
    /// </summary>
    public void LoadInvertY()
    {
        InvertY = PlayerPrefs.GetInt(INVERT_Y_KEY, DEFAULT_INVERT_Y ? 1 : 0) == 1;
    }
    #endregion

    #region Individual Save Functions
    /// <summary>
    /// Saves the Master Volume preference
    /// </summary>
    /// <param name="value">Volume value (typically 0-1)</param>
    public void SaveMasterVolume(float value)
    {
        MasterVolume = value;
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Saves the SFX Volume preference
    /// </summary>
    /// <param name="value">Volume value (typically 0-1)</param>
    public void SaveSfxVolume(float value)
    {
        SfxVolume = value;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Saves the Music Volume preference
    /// </summary>
    /// <param name="value">Volume value (typically 0-1)</param>
    public void SaveMusicVolume(float value)
    {
        MusicVolume = value;
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Resolution preference
    /// </summary>
    /// <param name="width">Resolution width</param>
    /// <param name="height">Resolution height</param>
    public void SaveResolution(int width, int height)
    {
        ResolutionWidth = width;
        ResolutionHeight = height;
        PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, width);
        PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, height);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Fullscreen Mode preference
    /// </summary>
    /// <param name="mode">Fullscreen mode (0 = FullScreenWindow, 1 = ExclusiveFullScreen, 2 = Windowed)</param>
    public void SaveFullscreenMode(int mode)
    {
        FullscreenMode = mode;
        PlayerPrefs.SetInt(FULLSCREEN_MODE_KEY, mode);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the VSync preference
    /// </summary>
    /// <param name="vsync">VSync count (0 = off, 1 = on, 2 = every second frame)</param>
    public void SaveVSync(int vsync)
    {
        VSync = vsync;
        PlayerPrefs.SetInt(VSYNC_KEY, vsync);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Quality Level preference
    /// </summary>
    /// <param name="level">Quality level index</param>
    public void SaveQualityLevel(int level)
    {
        QualityLevel = level;
        PlayerPrefs.SetInt(QUALITY_LEVEL_KEY, level);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Mouse Sensitivity preference
    /// </summary>
    /// <param name="sensitivity">Mouse sensitivity (400-1600 DPI range)</param>
    public void SaveMouseSensitivity(float sensitivity)
    {
        MouseSensitivity = Mathf.Clamp(sensitivity, 400f, 1600f);
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, MouseSensitivity);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Invert X preference
    /// </summary>
    /// <param name="invert">True to invert X axis</param>
    public void SaveInvertX(bool invert)
    {
        InvertX = invert;
        PlayerPrefs.SetInt(INVERT_X_KEY, invert ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Saves the Invert Y preference
    /// </summary>
    /// <param name="invert">True to invert Y axis</param>
    public void SaveInvertY(bool invert)
    {
        InvertY = invert;
        PlayerPrefs.SetInt(INVERT_Y_KEY, invert ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion

    #region Reset Functions
    /// <summary>
    /// Resets Master Volume to default value
    /// </summary>
    public void ResetMasterVolume()
    {
        SaveMasterVolume(DEFAULT_MASTER_VOLUME);
    }

    /// <summary>
    /// Resets SFX Volume to default value
    /// </summary>
    public void ResetSfxVolume()
    {
        SaveSfxVolume(DEFAULT_SFX_VOLUME);
    }

    /// <summary>
    /// Resets Music Volume to default value
    /// </summary>
    public void ResetMusicVolume()
    {
        SaveMusicVolume(DEFAULT_MUSIC_VOLUME);
    }

    /// <summary>
    /// Resets all audio settings to default values
    /// </summary>
    public void ResetAudioSettings()
    {
        ResetMasterVolume();
        ResetSfxVolume();
        ResetMusicVolume();
    }
    
    /// <summary>
    /// Resets Resolution to default value
    /// </summary>
    public void ResetResolution()
    {
        SaveResolution(DEFAULT_RESOLUTION_WIDTH, DEFAULT_RESOLUTION_HEIGHT);
    }
    
    /// <summary>
    /// Resets Fullscreen Mode to default value
    /// </summary>
    public void ResetFullscreenMode()
    {
        SaveFullscreenMode(DEFAULT_FULLSCREEN_MODE);
    }
    
    /// <summary>
    /// Resets VSync to default value
    /// </summary>
    public void ResetVSync()
    {
        SaveVSync(DEFAULT_VSYNC);
    }
    
    /// <summary>
    /// Resets Quality Level to default value
    /// </summary>
    public void ResetQualityLevel()
    {
        SaveQualityLevel(DEFAULT_QUALITY_LEVEL);
    }
    
    /// <summary>
    /// Resets all video settings to default values
    /// </summary>
    public void ResetVideoSettings()
    {
        ResetResolution();
        ResetFullscreenMode();
        ResetVSync();
        ResetQualityLevel();
    }
    
    /// <summary>
    /// Resets Mouse Sensitivity to default value
    /// </summary>
    public void ResetMouseSensitivity()
    {
        SaveMouseSensitivity(DEFAULT_MOUSE_SENSITIVITY);
    }
    
    /// <summary>
    /// Resets Invert X to default value
    /// </summary>
    public void ResetInvertX()
    {
        SaveInvertX(DEFAULT_INVERT_X);
    }
    
    /// <summary>
    /// Resets Invert Y to default value
    /// </summary>
    public void ResetInvertY()
    {
        SaveInvertY(DEFAULT_INVERT_Y);
    }
    
    /// <summary>
    /// Resets all game settings to default values
    /// </summary>
    public void ResetGameSettings()
    {
        ResetMouseSensitivity();
        ResetInvertX();
        ResetInvertY();
    }
    #endregion

    #region Utility Functions
    /// <summary>
    /// Loads all audio settings from PlayerPrefs
    /// </summary>
    private void LoadAudioSettings()
    {
        LoadMasterVolume();
        LoadSfxVolume();
        LoadMusicVolume();
    }
    
    /// <summary>
    /// Loads all video settings from PlayerPrefs
    /// </summary>
    private void LoadVideoSettings()
    {
        LoadResolution();
        LoadFullscreenMode();
        LoadVSync();
        LoadQualityLevel();
    }
    
    /// <summary>
    /// Loads all game settings from PlayerPrefs
    /// </summary>
    private void LoadGameSettings()
    {
        LoadMouseSensitivity();
        LoadInvertX();
        LoadInvertY();
    }

    /// <summary>
    /// Checks if a specific preference has been saved
    /// </summary>
    public bool HasPreference(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// Deletes all saved preferences and reinitializes to defaults
    /// </summary>
    public void DeleteAllPreferences()
    {
        PlayerPrefs.DeleteAll();
        SetAllDefaults();
    }
    #endregion
}
