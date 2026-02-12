using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public enum MenuType
{
    MainMenu,
    GameplayMenu
}

public class UniversalMenu : MonoBehaviour
{
    [Header("Menu Configuration")]
    [SerializeField] private MenuType menuType = MenuType.GameplayMenu;
    
    [Header("Scene Settings (Main Menu Only)")]
    [SerializeField] private string sceneToLoad = "";
    
    [Header("UI Documents")]
    [SerializeField] private UIDocument menuDocument;
    
    [Header("References (Gameplay Menu Only)")]
    [SerializeField] private UIManager uiManager;
    
    [Header("Settings References")]
    [SerializeField] private AudioSettings audioSettings;
    
    [Header("References (Optional)")]
    [SerializeField] private BackgroundMusic backgroundMusic;
    [SerializeField] private GlobalButtonClickSound buttonClickSound;
    
    private VisualElement backgroundImage;
    private VisualElement menuWindow;
    private VisualElement settingsWindow;
    private Button playResumeButton;
    private Button optionsButton;
    private Button exitButton;
    private Button closeButton;
    private Button respawnButton;
    private Label versionLabel;
    private Label authorNameLabel;
    private Label songNameLabel;
    
    // Settings Tab Buttons
    private Button gameTabButton;
    private Button videoTabButton;
    private Button audioTabButton;
    private Button controlsTabButton;
    
    // Settings Windows
    private VisualElement gameWindow;
    private VisualElement videoWindow;
    private VisualElement audioWindow;
    private VisualElement controlsWindow;
    
    // Settings Content Containers
    private VisualElement audioSettingsElements;
    
    private bool isMenuOpen = false;
    
    void Awake()
    {
        InitializeUIElements();
        ConfigureMenuForType();
        RegisterButtonCallbacks();
    }
    
    void Start()
    {
        // Initialize settings UI
        InitializeSettingsUI();
        
        // MainMenu is always visible at start, Gameplay menu starts hidden
        if (menuType == MenuType.MainMenu)
        {
            ShowEntireMenu();
            ShowMenu();
            HideSettings();
            UpdateMusicInfo();
            isMenuOpen = true;
        }
        else
        {
            HideEntireMenu();
        }
    }
    
    private void InitializeUIElements()
    {
        if (menuDocument == null)
        {
            Debug.LogError("UIDocument not assigned to UniversalMenu!");
            return;
        }
        
        var root = menuDocument.rootVisualElement;
        
        // Get the background image (parent of everything)
        backgroundImage = root.Q<VisualElement>("BackgroundImage");
        
        // Get main windows
        menuWindow = root.Q<VisualElement>("MenuWindow");
        settingsWindow = root.Q<VisualElement>("SettingsWindow");
        
        // Get menu buttons
        playResumeButton = root.Q<Button>("PlayResumeButton");
        optionsButton = root.Q<Button>("OptionsButton");
        exitButton = root.Q<Button>("ExitButton");
        respawnButton = root.Q<Button>("RespawnButton");
        
        // Get settings close button
        closeButton = root.Q<Button>("CloseButton");
        
        // Get settings tab buttons
        gameTabButton = root.Q<Button>("GameButton");
        videoTabButton = root.Q<Button>("VideoButton");
        audioTabButton = root.Q<Button>("AudioButton");
        controlsTabButton = root.Q<Button>("ControlsButton");
        
        // Get settings windows
        gameWindow = root.Q<VisualElement>("GameWindow");
        videoWindow = root.Q<VisualElement>("VideoWindow");
        audioWindow = root.Q<VisualElement>("AudioWindow");
        controlsWindow = root.Q<VisualElement>("ControlsWindow");
        
        // Get settings content containers
        audioSettingsElements = root.Q<VisualElement>("AudioSettingsElements");
        
        // Get labels
        versionLabel = root.Q<Label>("VersionNumber");
        authorNameLabel = root.Q<Label>("AuthorName");
        songNameLabel = root.Q<Label>("SongName");
        
        // Set version
        if (versionLabel != null)
        {
            versionLabel.text = $"Version {Application.version}";
        }
    }
    
    private void ConfigureMenuForType()
    {
        // Configure button text and visibility based on menu type
        if (playResumeButton != null)
        {
            playResumeButton.text = menuType == MenuType.MainMenu ? "Play" : "Resume";
        }
        
        // Hide respawn button in MainMenu
        if (respawnButton != null)
        {
            if (menuType == MenuType.MainMenu)
            {
                respawnButton.style.display = DisplayStyle.None;
            }
            else
            {
                respawnButton.style.display = DisplayStyle.Flex;
            }
        }
        
        // Validate required references for Gameplay Menu
        if (menuType == MenuType.GameplayMenu)
        {
            if (uiManager == null)
            {
                Debug.LogWarning("UIManager reference not assigned! Required for Gameplay Menu mode.");
            }
        }
        
        // Validate scene for MainMenu
        if (menuType == MenuType.MainMenu)
        {
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogWarning("Scene to load not assigned! Required for MainMenu mode.");
            }
        }
    }
    
    private void RegisterButtonCallbacks()
    {
        if (playResumeButton != null)
            playResumeButton.clicked += OnPlayResumeClicked;
        
        if (optionsButton != null)
            optionsButton.clicked += OnOptionsClicked;
        
        if (exitButton != null)
            exitButton.clicked += OnExitClicked;
        
        if (respawnButton != null)
            respawnButton.clicked += OnRespawnClicked;
        
        if (closeButton != null)
            closeButton.clicked += OnCloseSettingsClicked;
        
        // Register settings tab callbacks
        if (gameTabButton != null)
            gameTabButton.clicked += () => SwitchToTab(SettingsTab.Game);
        
        if (videoTabButton != null)
            videoTabButton.clicked += () => SwitchToTab(SettingsTab.Video);
        
        if (audioTabButton != null)
            audioTabButton.clicked += () => SwitchToTab(SettingsTab.Audio);
        
        if (controlsTabButton != null)
            controlsTabButton.clicked += () => SwitchToTab(SettingsTab.Controls);
    }
    
    public void OpenMenu()
    {
        // MainMenu can't be opened/closed - it's always visible
        if (menuType == MenuType.MainMenu)
        {
            Debug.LogWarning("Cannot open MainMenu - it's always visible");
            return;
        }
        
        if (backgroundImage == null || menuWindow == null) return;
        
        ShowEntireMenu();
        ShowMenu();
        HideSettings();
        UpdateMusicInfo();
        isMenuOpen = true;
    }
    
    public void CloseMenu()
    {
        // MainMenu can't be closed
        if (menuType == MenuType.MainMenu)
        {
            Debug.LogWarning("Cannot close MainMenu");
            return;
        }
        
        if (backgroundImage == null) return;
        
        HideEntireMenu();
        isMenuOpen = false;
    }
    
    private void ShowEntireMenu()
    {
        if (backgroundImage != null)
        {
            backgroundImage.style.display = DisplayStyle.Flex;
        }
    }
    
    private void HideEntireMenu()
    {
        if (backgroundImage != null)
        {
            backgroundImage.style.display = DisplayStyle.None;
        }
    }
    
    private void ShowMenu()
    {
        if (menuWindow != null)
        {
            menuWindow.style.display = DisplayStyle.Flex;
        }
    }
    
    private void HideMenu()
    {
        if (menuWindow != null)
        {
            menuWindow.style.display = DisplayStyle.None;
        }
    }
    
    private void ShowSettings()
    {
        if (settingsWindow != null)
        {
            settingsWindow.style.display = DisplayStyle.Flex;
        }
    }
    
    private void HideSettings()
    {
        if (settingsWindow != null)
        {
            settingsWindow.style.display = DisplayStyle.None;
        }
    }
    
    public void UpdateMusicInfo()
    {
        if (backgroundMusic == null) return;
        
        var currentSong = backgroundMusic.CurrentSong;
        if (currentSong == null) return;
        
        if (authorNameLabel != null)
        {
            authorNameLabel.text = currentSong.author ?? "Unknown Author";
        }
        
        if (songNameLabel != null)
        {
            songNameLabel.text = currentSong.songName ?? "Unknown Song";
        }
    }
    
    #region Button Callbacks
    
    private void OnPlayResumeClicked()
    {
        PlayButtonSound();
        Debug.Log($"{(menuType == MenuType.MainMenu ? "Play" : "Resume")} button clicked");
        
        if (menuType == MenuType.MainMenu)
        {
            // Main menu "Play" - load the game scene
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogError("Scene to load is not assigned!");
            }
        }
        else
        {
            // Gameplay menu "Resume" - close menu and return to game
            CloseMenu();
            
            if (uiManager != null)
            {
                uiManager.OpenPlayerUI();
            }
        }
    }
    
    private void OnOptionsClicked()
    {
        PlayButtonSound();
        Debug.Log("Options button clicked");
        // Hide the menu and show settings window
        // Background remains visible in both cases
        HideMenu();
        ShowSettings();
        // Set default tab to Game
        SwitchToTab(SettingsTab.Game);
    }
    
    private void OnExitClicked()
    {
        PlayButtonSound();
        Debug.Log("Exit button clicked");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void OnRespawnClicked()
    {
        PlayButtonSound();
        Debug.Log("Respawn button clicked");
        
        // Respawn only available in Gameplay Menu
        if (menuType == MenuType.MainMenu)
        {
            Debug.LogWarning("Respawn not available in MainMenu");
            return;
        }

        Player player = FindAnyObjectByType<Player>();
        
        if (player != null)
        {
            player.Respawn();
            CloseMenu();
            
            if (uiManager != null)
            {
                uiManager.OpenPlayerUI();
            }
        }
        else
        {
            Debug.LogWarning("Player not found in scene!");
        }
    }
    
    private void OnCloseSettingsClicked()
    {
        PlayButtonSound();
        Debug.Log("Close settings button clicked");
        HideSettings();
        ShowMenu();
    }
    
    #endregion
    
    #region Settings Initialization
    
    private void InitializeSettingsUI()
    {
        // Initialize Audio Settings
        if (audioSettings != null && audioSettingsElements != null)
        {
            audioSettings.CreateAllAudioSliders(audioSettingsElements);
            Debug.Log("Audio settings UI initialized");
        }
        else
        {
            if (audioSettings == null)
                Debug.LogWarning("AudioSettings component not assigned!");
            if (audioSettingsElements == null)
                Debug.LogWarning("AudioSettingsElements container not found in UI!");
        }
    }
    
    #endregion
    
    #region Settings Tab Management
    
    private enum SettingsTab
    {
        Game,
        Video,
        Audio,
        Controls
    }
    
    private void SwitchToTab(SettingsTab tab)
    {
        PlayButtonSound();
        
        // Hide all windows
        if (gameWindow != null) gameWindow.style.display = DisplayStyle.None;
        if (videoWindow != null) videoWindow.style.display = DisplayStyle.None;
        if (audioWindow != null) audioWindow.style.display = DisplayStyle.None;
        if (controlsWindow != null) controlsWindow.style.display = DisplayStyle.None;
        
        // Show selected window
        switch (tab)
        {
            case SettingsTab.Game:
                if (gameWindow != null) gameWindow.style.display = DisplayStyle.Flex;
                Debug.Log("Switched to Game settings");
                break;
            case SettingsTab.Video:
                if (videoWindow != null) videoWindow.style.display = DisplayStyle.Flex;
                Debug.Log("Switched to Video settings");
                break;
            case SettingsTab.Audio:
                if (audioWindow != null) audioWindow.style.display = DisplayStyle.Flex;
                Debug.Log("Switched to Audio settings");
                break;
            case SettingsTab.Controls:
                if (controlsWindow != null) controlsWindow.style.display = DisplayStyle.Flex;
                Debug.Log("Switched to Controls settings");
                break;
        }
    }
    
    #endregion
    
    #region Audio
    
    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.PlayClickSound();
        }
    }
    
    #endregion
    
    public bool IsMenuOpen => isMenuOpen;
    public MenuType Type => menuType;
    public bool IsMainMenu => menuType == MenuType.MainMenu;
}
