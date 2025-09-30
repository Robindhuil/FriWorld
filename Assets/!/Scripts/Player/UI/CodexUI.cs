using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Audio;

public class CodexUI : BaseUi
{
    private Canvas codexCanvas;
    private GameObject categoryList;
    private GameObject subList;
    private TextMeshProUGUI infoName;
    private TextMeshProUGUI infoDesc;
    private RawImage photo;
    public bool IsMenuOn { get; set; }

    private Dictionary<string, GameObject> categoryButtons = new();
    private Button lastCategoryButton;
    private Button lastSubButton;
    private GameObject prefabButton;
    private MonoBehaviour runner;

    private GameObject videoPanel;
    private VideoPlayer videoPlayer;
    private RawImage videoDisplay;
    private RenderTexture videoRenderTexture;
    private AudioMixer mixer;
    private float previousMusicVolume;

    public CodexUI(Canvas canvas, GameObject categoryList, GameObject subList, TextMeshProUGUI infoName, TextMeshProUGUI infoDesc,
        RawImage photo, GameObject prefabButton, MonoBehaviour runner, AudioMixer mixer)
    {
        codexCanvas = canvas;
        this.categoryList = categoryList;
        this.subList = subList;
        this.infoName = infoName;
        this.infoDesc = infoDesc;
        this.photo = photo;
        this.prefabButton = prefabButton;
        this.runner = runner;
        this.mixer = mixer;
        codexCanvas.gameObject.SetActive(false);

        if (Codex.Instance == null)
        {
            Debug.LogError("[CodexUI] Codex.Instance je null!");
            return;
        }

        Codex.Instance.OnCodexUpdated += RefreshUI;
        CreateCategoryButtons();

        CreateVideoPlayerUI();
    }

    private void RefreshUI()
    {
        foreach (Transform child in categoryList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        var uniqueCategories = Codex.Instance.GetUnlockedCategories().Distinct().ToList();

        foreach (var category in uniqueCategories)
        {
            runner.StartCoroutine(CreateButton(category, categoryList.transform, true));
        }

        ResetAll();
    }

    private void CreateCategoryButtons()
    {
        foreach (var category in Codex.Instance.GetUnlockedCategories())
        {
            runner.StartCoroutine(CreateButton(category, categoryList.transform, true));
        }
    }
    private void ShowSubList(string category)
    {
        foreach (Transform child in subList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        var uniqueEntries = Codex.Instance.GetUnlockedEntries(category)
            .GroupBy(e => e.name)
            .Select(g => g.First())
            .ToList();

        foreach (var entry in uniqueEntries)
        {
            string buttonText = RemoveTitles(entry.name);
            runner.StartCoroutine(CreateButton(buttonText, subList.transform, false, entry));
        }
    }

    private void ShowEntryDetails(CodexEntry entry)
    {
        if (infoName != null) infoName.text = entry.name;
        if (infoDesc != null) infoDesc.text = entry.description;

        if (photo != null)
        {
            photo.texture = null;
        }

        if (entry.category == "Programovanie" && !string.IsNullOrEmpty(entry.video))
        {
            Button videoButton = photo.GetComponent<Button>();
            if (videoButton == null)
            {
                videoButton = photo.gameObject.AddComponent<Button>();
            }
            videoButton.onClick.RemoveAllListeners();
            videoButton.onClick.AddListener(() => PlayVideo(entry.video));

            if (!string.IsNullOrEmpty(entry.photo))
            {
                Texture2D texture = Resources.Load<Texture2D>(entry.photo);
                if (texture != null)
                {
                    photo.texture = texture;
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
                photo.texture = texture;
            }
            else
            {
                Debug.LogError($"[CodexUI] Obrázok {entry.photo} sa nenašiel v Resources!");
            }
        }
    }


    private IEnumerator CreateButton(string text, Transform parent, bool isCategoryButton, CodexEntry entry = null)
    {
        yield return null;

        foreach (Transform existingButton in parent)
        {
            if (existingButton.name == text)
            {
                yield break;
            }
        }
        GameObject buttonObj = Object.Instantiate(prefabButton, parent);
        buttonObj.name = text;

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[CodexUI] Prefab button does not contain a Button component!");
            yield break;
        }

        button.onClick.AddListener(() => HighlightButton(button, isCategoryButton));
        if (entry != null)
        {
            button.onClick.AddListener(() => ShowEntryDetails(entry));
        }
        else
        {
            button.onClick.AddListener(() => ShowSubList(text));
        }

        TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.text = text;
            btnText.enableAutoSizing = true;
            btnText.fontSizeMin = 10;
            btnText.fontSizeMax = 40;
        }

        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(-30f, 45f);

            int buttonIndex = parent.childCount - 1;
            float paddingTop = -15f;
            float paddingBot = 15f;
            float buttonSpacing = 5f;

            rectTransform.anchoredPosition = new Vector2(0, paddingTop - buttonIndex * (45f + buttonSpacing));

            RectTransform contentRectTransform = parent.GetComponent<RectTransform>();
            contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, (45f + buttonSpacing) * parent.childCount + buttonSpacing + paddingBot);
        }

        AddHoverEffect(buttonObj);
    }

    private void AddHoverEffect(GameObject buttonObj)
    {
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<EventTrigger>();
        }

        Button button = buttonObj.GetComponent<Button>();

        EventTrigger.Entry entryEnter = new() { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((_) =>
        {
            Image image = buttonObj.GetComponent<Image>();
            if (image != null && button != lastCategoryButton && button != lastSubButton)
            {
                image.color = new Color(1, 1, 1, 0.7f);
            }
        });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new() { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((_) =>
        {
            Image image = buttonObj.GetComponent<Image>();
            if (image != null && button != lastCategoryButton && button != lastSubButton)
            {
                image.color = Color.clear;
            }
        });
        trigger.triggers.Add(entryExit);
    }

    private void HighlightButton(Button clickedButton, bool isCategoryButton)
    {
        if (isCategoryButton)
        {
            if (lastCategoryButton != null)
            {
                ResetButtonColor(lastCategoryButton);
            }
            lastCategoryButton = clickedButton;
        }
        else
        {
            if (lastSubButton != null)
            {
                ResetButtonColor(lastSubButton);
            }
            lastSubButton = clickedButton;
        }

        Image image = clickedButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(1f, 0.5f, 0f, 0.8f);
        }
    }

    private void ResetButtonColor(Button button)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.clear;
        }
    }

    private void ResetAll()
    {
        if (infoName != null) infoName.text = "";
        if (infoDesc != null) infoDesc.text = "";
        if (photo != null) photo.texture = null;

        if (lastCategoryButton != null)
        {
            ResetButtonColor(lastCategoryButton);
            lastCategoryButton = null;
        }
        if (lastSubButton != null)
        {
            ResetButtonColor(lastSubButton);
            lastSubButton = null;
        }

        foreach (Transform child in subList.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public override void CloseWindow()
    {
        codexCanvas.gameObject.SetActive(false);
        IsMenuOn = false;
    }

    public override void OpenWindow()
    {
        codexCanvas.gameObject.SetActive(true);
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

        videoPanel = new GameObject("VideoPanel");
        videoPanel.transform.SetParent(codexCanvas.transform, false);
        RectTransform panelRect = videoPanel.AddComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UnityEngine.UI.Outline outline = videoPanel.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(5, -5);

        GameObject rawImageObj = new GameObject("VideoDisplay");
        rawImageObj.transform.SetParent(videoPanel.transform, false);
        videoDisplay = rawImageObj.AddComponent<RawImage>();
        RectTransform imageRect = rawImageObj.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        videoPlayer = videoPanel.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoRenderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = videoRenderTexture;
        videoDisplay.texture = videoRenderTexture;

        AudioSource audioSource = videoPanel.AddComponent<AudioSource>();
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

        GameObject closeButtonObj = new GameObject("CloseButton", typeof(RectTransform));
        closeButtonObj.transform.SetParent(videoPanel.transform, false);
        Button closeButton = closeButtonObj.AddComponent<Button>();
        RectTransform closeRect = closeButtonObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-30, -30);
        closeRect.sizeDelta = new Vector2(60, 30);
        closeButton.onClick.AddListener(CloseVideoPlayerUI);

        GameObject closeButtonTextObj = new GameObject("CloseButtonText", typeof(RectTransform));
        closeButtonTextObj.transform.SetParent(closeButtonObj.transform, false);
        TextMeshProUGUI closeButtonText = closeButtonTextObj.AddComponent<TextMeshProUGUI>();
        closeButtonText.text = "X";
        closeButtonText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = closeButtonTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        videoPlayer.errorReceived += (source, message) =>
        {
            Debug.LogError("[CodexUI] Video Player Error: " + message);
        };

        videoPanel.SetActive(false);
    }


    private void PlayVideo(string videoFileName)
    {
        LowerMusicForVideo();
        if (videoPanel == null)
            CreateVideoPlayerUI();

        videoPanel.SetActive(true);

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
    videoPath = "file://" + videoPath;
#endif

        if (!System.IO.File.Exists(videoPath))
        {
            Debug.LogError($"[CodexUI] Video file {videoFileName} not found at path: {videoPath}");
            videoPanel.SetActive(false);
            return;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.url = videoPath;
        videoPlayer.Prepare();
    }


    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoPanel.activeSelf)
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
            videoPanel.SetActive(false);
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
