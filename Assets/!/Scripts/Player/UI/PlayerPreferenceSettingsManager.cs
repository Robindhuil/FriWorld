using UnityEngine;

public class PlayerPreferenceSettingsManager : MonoBehaviour
{
    #region PlayerPrefs Keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SfxVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string FIRST_RUN_KEY = "FirstRun";
    #endregion

    #region Default Values
    public static readonly float DEFAULT_MASTER_VOLUME = 0.5f;
    public static readonly float DEFAULT_SFX_VOLUME = 0.5f;
    public static readonly float DEFAULT_MUSIC_VOLUME = 0.5f;
    #endregion

    #region Audio Settings Properties
    public float MasterVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public float MusicVolume { get; private set; }
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
    }
    #endregion

    #region Load All Preferences
    /// <summary>
    /// Loads all player preferences from PlayerPrefs
    /// </summary>
    public void LoadAllPreferences()
    {
        LoadAudioSettings();
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
