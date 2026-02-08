using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Audio;

public class CodexUI : BaseUi
{
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement codexUIElement;
    private ListView categoriesList;
    private ListView subCategoriesList;
    private ScrollView infoScrollView;
    private Label entryNameLabel;
    private Image pictureElement;
    public bool IsMenuOn { get; set; }

    private Button lastCategoryButton;
    private Button lastSubButton;
    private MonoBehaviour runner;

    private VisualElement videoPanel;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private AudioMixer mixer;
    private float previousMusicVolume;

    public CodexUI(UIDocument document, MonoBehaviour runner, AudioMixer mixer)
    {
        this.uiDocument = document;
        this.runner = runner;
        this.mixer = mixer;
        
        rootElement = uiDocument.rootVisualElement;
        codexUIElement = rootElement.Q<VisualElement>("CodexUI");
        categoriesList = rootElement.Q<ListView>("CategoriesList");
        subCategoriesList = rootElement.Q<ListView>("SubCategoriesList");
        infoScrollView = rootElement.Q<ScrollView>("InfoList");
        entryNameLabel = rootElement.Q<Label>("EntryName");
        pictureElement = rootElement.Q<Image>("Picture");
        
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.None;
        }

        if (Codex.Instance == null)
        {
            Debug.LogError("[CodexUI] Codex.Instance je null!");
            return;
        }

        SetupCategoriesList();
        SetupSubCategoriesList();

        Codex.Instance.OnCodexUpdated += RefreshUI;
        RefreshCategoryButtons();
    }

    private void SetupCategoriesList()
    {
        if (categoriesList == null) return;

        categoriesList.fixedItemHeight = 70; // 60px height + 5px top margin + 5px bottom margin
        categoriesList.makeItem = () => CreateListButton();
        categoriesList.bindItem = (element, index) => BindCategoryItem(element, index);
        categoriesList.selectionChanged += OnCategorySelected;
        categoriesList.itemsSource = new List<string>();
    }

    private void SetupSubCategoriesList()
    {
        if (subCategoriesList == null) return;

        subCategoriesList.fixedItemHeight = 70; // 60px height + 5px top margin + 5px bottom margin
        subCategoriesList.makeItem = () => CreateListButton();
        subCategoriesList.bindItem = (element, index) => BindSubCategoryItem(element, index);
        subCategoriesList.selectionChanged += OnSubCategorySelected;
        subCategoriesList.itemsSource = new List<CodexEntry>();
    }

    private VisualElement CreateListButton()
    {
        var button = new Button();
        button.AddToClassList("codexButton");
        
        button.RegisterCallback<MouseEnterEvent>(evt => OnButtonHoverEnter(evt.target as Button));
        button.RegisterCallback<MouseLeaveEvent>(evt => OnButtonHoverExit(evt.target as Button));
        
        return button;
    }


    private void BindCategoryItem(VisualElement element, int index)
    {
        if (element is Button button && categoriesList.itemsSource is List<string> categories)
        {
            if (index < categories.Count)
            {
                button.text = categories[index];
                button.userData = categories[index];
                
                // Clear previous click handlers
                button.clicked -= null;
                button.clicked += () => 
                {
                    categoriesList.selectedIndex = index;
                    ShowSubList(categories[index]);
                };
            }
        }
    }

    private void BindSubCategoryItem(VisualElement element, int index)
    {
        if (element is Button button && subCategoriesList.itemsSource is List<CodexEntry> entries)
        {
            if (index < entries.Count)
            {
                button.text = RemoveTitles(entries[index].name);
                button.userData = entries[index];
                
                // Clear previous click handlers
                button.clicked -= null;
                button.clicked += () => 
                {
                    subCategoriesList.selectedIndex = index;
                    ShowEntryDetails(entries[index]);
                };
            }
        }
    }

    private void OnCategorySelected(IEnumerable<object> selectedItems)
    {
        var selectedCategory = selectedItems.FirstOrDefault() as string;
        if (!string.IsNullOrEmpty(selectedCategory))
        {
            ShowSubList(selectedCategory);
        }
    }

    private void OnSubCategorySelected(IEnumerable<object> selectedItems)
    {
        var selectedEntry = selectedItems.FirstOrDefault() as CodexEntry;
        if (selectedEntry != null)
        {
            ShowEntryDetails(selectedEntry);
        }
    }

    private void OnButtonHoverEnter(Button button)
    {
        if (button != null && button != lastCategoryButton && button != lastSubButton)
        {
            button.style.backgroundColor = new Color(1, 1, 1, 0.7f);
        }
    }

    private void OnButtonHoverExit(Button button)
    {
        if (button != null && button != lastCategoryButton && button != lastSubButton)
        {
            button.style.backgroundColor = new Color(0, 0, 0, 0);
        }
    }

    private void RefreshUI()
    {
        RefreshCategoryButtons();
        ResetAll();
    }

    private void RefreshCategoryButtons()
    {
        var uniqueCategories = Codex.Instance.GetUnlockedCategories().Distinct().ToList();
        if (categoriesList != null)
        {
            categoriesList.itemsSource = uniqueCategories;
            categoriesList.Rebuild();
        }
    }
    private void ShowSubList(string category)
    {
        var uniqueEntries = Codex.Instance.GetUnlockedEntries(category)
            .GroupBy(e => e.name)
            .Select(g => g.First())
            .ToList();

        if (subCategoriesList != null)
        {
            subCategoriesList.itemsSource = uniqueEntries;
            subCategoriesList.Rebuild();
        }
    }

    private void ShowEntryDetails(CodexEntry entry)
    {
        // Set entry name in the dedicated label
        if (entryNameLabel != null)
        {
            entryNameLabel.text = !string.IsNullOrEmpty(entry.name) ? entry.name : "";
        }
        
        // Clear and populate ScrollView with description
        if (infoScrollView != null)
        {
            infoScrollView.Clear();
            
            if (!string.IsNullOrEmpty(entry.description))
            {
                var descLabel = new Label(entry.description);
                descLabel.style.color = Color.white;
                descLabel.style.fontSize = 20;
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.paddingTop = 10;
                descLabel.style.paddingBottom = 10;
                descLabel.style.paddingLeft = 10;
                descLabel.style.paddingRight = 10;
                infoScrollView.Add(descLabel);
            }
        }

        // Handle image
        if (pictureElement != null)
        {
            pictureElement.style.backgroundImage = null;
            pictureElement.UnregisterCallback<ClickEvent>(OnPictureClicked);

            if (entry.category == "Programovanie" && !string.IsNullOrEmpty(entry.video))
            {
                // Register click handler for video
                pictureElement.RegisterCallback<ClickEvent>(evt => PlayVideo(entry.video));

                if (!string.IsNullOrEmpty(entry.photo))
                {
                    Texture2D texture = Resources.Load<Texture2D>(entry.photo);
                    if (texture != null)
                    {
                        pictureElement.style.backgroundImage = new StyleBackground(texture);
                    }
                    else
                    {
                        Debug.Log($"[CodexUI] Obrázok {entry.photo} sa nenašiel v Resources!");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(entry.photo))
            {
                Texture2D texture = Resources.Load<Texture2D>(entry.photo);
                if (texture != null)
                {
                    pictureElement.style.backgroundImage = new StyleBackground(texture);
                }
                else
                {
                    Debug.LogError($"[CodexUI] Obrázok {entry.photo} sa nenašiel v Resources!");
                }
            }
        }
    }

    private void OnPictureClicked(ClickEvent evt)
    {
        // This will be set dynamically in ShowEntryDetails if needed
    }

    private void ResetAll()
    {
        if (entryNameLabel != null)
        {
            entryNameLabel.text = "";
        }
        
        if (infoScrollView != null)
        {
            infoScrollView.Clear();
        }

        if (pictureElement != null)
        {
            pictureElement.style.backgroundImage = null;
        }

        if (subCategoriesList != null)
        {
            subCategoriesList.itemsSource = new List<CodexEntry>();
            subCategoriesList.Rebuild();
        }

        if (categoriesList != null)
        {
            categoriesList.ClearSelection();
        }

        if (subCategoriesList != null)
        {
            subCategoriesList.ClearSelection();
        }

        lastCategoryButton = null;
        lastSubButton = null;
    }

    public override void CloseWindow()
    {
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.None;
        }
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        if (codexUIElement != null)
        {
            codexUIElement.style.display = DisplayStyle.Flex;
        }
        IsMenuOn = true;
        ResetAll();
    }

    private string RemoveTitles(string fullName)
    {
        string[] titles = { "doc.", "Ing.", "Mgr.", "PhD.", "RNDr.", "Bc.", "MUDr." };
        var nameParts = fullName.Split(' ').ToList();
        nameParts.RemoveAll(part => titles.Contains(part));
        for (int i = 0; i < nameParts.Count; i++)
        {
            nameParts[i] = nameParts[i].Replace(",", "");
        }
        return string.Join(" ", nameParts);
    }


    private void CreateVideoPlayerUI()
    {
        if (videoPanel != null)
            return;

        // Create video panel as overlay on the root canvas
        Canvas rootCanvas = uiDocument.GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            // Find a canvas in the parent hierarchy
            rootCanvas = uiDocument.GetComponentInParent<Canvas>();
        }

        if (rootCanvas == null)
        {
            Debug.LogError("[CodexUI] Cannot find Canvas for video player!");
            return;
        }

        videoPanel = new VisualElement();
        videoPanel.name = "VideoPanel";
        videoPanel.style.position = Position.Absolute;
        videoPanel.style.left = 0;
        videoPanel.style.top = 0;
        videoPanel.style.right = 0;
        videoPanel.style.bottom = 0;
        videoPanel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        videoPanel.style.alignItems = Align.Center;
        videoPanel.style.justifyContent = Justify.Center;

        // Create video display container
        var videoContainer = new VisualElement();
        videoContainer.name = "VideoContainer";
        videoContainer.style.width = new Length(80, LengthUnit.Percent);
        videoContainer.style.height = new Length(80, LengthUnit.Percent);
        videoContainer.style.backgroundColor = Color.black;
        videoContainer.style.borderTopLeftRadius = 10;
        videoContainer.style.borderTopRightRadius = 10;
        videoContainer.style.borderBottomLeftRadius = 10;
        videoContainer.style.borderBottomRightRadius = 10;

        // Create close button
        var closeButton = new UnityEngine.UIElements.Button(() => CloseVideoPlayerUI());
        closeButton.text = "X";
        closeButton.style.position = Position.Absolute;
        closeButton.style.top = 10;
        closeButton.style.right = 10;
        closeButton.style.width = 60;
        closeButton.style.height = 40;
        closeButton.style.fontSize = 25;
        closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        closeButton.style.color = Color.white;
        closeButton.style.borderTopLeftRadius = 5;
        closeButton.style.borderTopRightRadius = 5;
        closeButton.style.borderBottomLeftRadius = 5;
        closeButton.style.borderBottomRightRadius = 5;

        videoContainer.Add(closeButton);
        videoPanel.Add(videoContainer);

        // Setup VideoPlayer component (needs GameObject)
        GameObject videoPlayerObj = new GameObject("VideoPlayerComponent");
        videoPlayerObj.transform.SetParent(rootCanvas.transform);
        videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoRenderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = videoRenderTexture;

        // Setup audio
        AudioSource audioSource = videoPlayerObj.AddComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);

        if (mixer != null)
        {
            AudioMixerGroup[] audioMixerGroups = mixer.FindMatchingGroups("Sfx");
            if (audioMixerGroups.Length > 0)
            {
                audioSource.outputAudioMixerGroup = audioMixerGroups[0];
            }
        }

        videoPlayer.errorReceived += (source, message) =>
        {
            Debug.LogError("[CodexUI] Video Player Error: " + message);
        };

        // Apply video texture to container background
        videoContainer.style.backgroundImage = Background.FromRenderTexture(videoRenderTexture);
        videoContainer.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        videoContainer.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);

        videoPanel.style.display = DisplayStyle.None;
        rootElement.Add(videoPanel);
    }


    private void PlayVideo(string videoFileName)
    {
        LowerMusicForVideo();
        if (videoPanel == null)
            CreateVideoPlayerUI();

        videoPanel.style.display = DisplayStyle.Flex;

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
    videoPath = "file://" + videoPath;
#endif

        if (!System.IO.File.Exists(videoPath))
        {
            Debug.LogError($"[CodexUI] Video file {videoFileName} not found at path: {videoPath}");
            videoPanel.style.display = DisplayStyle.None;
            return;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.url = videoPath;
        videoPlayer.Prepare();
    }


    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoPanel != null && videoPanel.style.display == DisplayStyle.Flex)
        {
            vp.Play();
        }
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    private void CloseVideoPlayerUI()
    {
        RestoreMusicAfterVideo();
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        if (videoPanel != null)
            videoPanel.style.display = DisplayStyle.None;
    }

    private void LowerMusicForVideo()
    {
        previousMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);

        mixer.SetFloat("MusicVolume", -80f);
    }

    private void RestoreMusicAfterVideo()
    {
        mixer.SetFloat("MusicVolume", previousMusicVolume);
    }
}
