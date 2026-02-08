using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using UnityEngine.Audio;

public class VideoPlayerUI : BaseUi
{
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement backgroundBase;
    private VisualElement videoPlayerBackground;
    private Label videoNameLabel;
    private Button videoCloseButton;
    private Button playPauseButton;
    private Image videoPlayerImage;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private AudioMixer mixer;
    private float previousMusicVolume;
    private bool isVideoPlaying = false;

    public bool IsVideoPlayerOpen => videoPlayerBackground != null && videoPlayerBackground.style.display == DisplayStyle.Flex;

    public VideoPlayerUI(UIDocument document, AudioMixer mixer)
    {
        this.uiDocument = document;
        this.mixer = mixer;
        
        rootElement = uiDocument.rootVisualElement;
        backgroundBase = rootElement.Q<VisualElement>("BackgroundBase");
        
        SetupVideoPlayer();
    }

    private void SetupVideoPlayer()
    {
        // Query video player UI elements from UXML
        videoPlayerBackground = rootElement.Q<VisualElement>("VideoPlayerBackground");
        videoNameLabel = rootElement.Q<Label>("VideoName");
        videoCloseButton = rootElement.Q<Button>("CloseButton");
        playPauseButton = rootElement.Q<Button>("PlayPauseButton");
        videoPlayerImage = rootElement.Q<Image>("VideoPlayer");

        if (videoPlayerBackground != null)
        {
            videoPlayerBackground.style.display = DisplayStyle.None;
        }

        // Setup VideoPlayer component
        GameObject videoPlayerObj = new GameObject("VideoPlayerComponent");
        videoPlayerObj.transform.SetParent(uiDocument.transform);
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
            Debug.LogError("[VideoPlayerUI] Video Player Error: " + message);
        };

        videoPlayer.loopPointReached += OnVideoFinished;

        // Wire up buttons
        if (videoCloseButton != null)
        {
            videoCloseButton.clicked += CloseVideoPlayerUI;
        }

        if (playPauseButton != null)
        {
            playPauseButton.clicked += TogglePlayPause;
            playPauseButton.text = "Play";
        }

        // Apply video texture to image
        if (videoPlayerImage != null)
        {
            videoPlayerImage.image = videoRenderTexture;
        }
    }

    public void PlayVideo(string videoFileName)
    {
        if (videoPlayerBackground == null || videoPlayer == null)
        {
            Debug.LogError("[VideoPlayerUI] Video player UI not initialized!");
            return;
        }

        LowerMusicForVideo();
        
        // Hide BackgroundBase and show video player UI
        if (backgroundBase != null)
        {
            backgroundBase.style.display = DisplayStyle.None;
        }
        videoPlayerBackground.style.display = DisplayStyle.Flex;

        // Set video name (remove file extension)
        if (videoNameLabel != null)
        {
            string displayName = System.IO.Path.GetFileNameWithoutExtension(videoFileName);
            videoNameLabel.text = displayName;
        }

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        videoPath = "file://" + videoPath;
#endif

        if (!System.IO.File.Exists(videoPath))
        {
            Debug.LogError($"[VideoPlayerUI] Video file {videoFileName} not found at path: {videoPath}");
            videoPlayerBackground.style.display = DisplayStyle.None;
            if (backgroundBase != null)
            {
                backgroundBase.style.display = DisplayStyle.Flex;
            }
            return;
        }

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.url = videoPath;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoPlayerBackground != null && videoPlayerBackground.style.display == DisplayStyle.Flex)
        {
            vp.Play();
            isVideoPlaying = true;
            if (playPauseButton != null)
            {
                playPauseButton.text = "Pause";
            }
        }
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    private void TogglePlayPause()
    {
        if (videoPlayer == null) return;

        if (isVideoPlaying)
        {
            videoPlayer.Pause();
            isVideoPlaying = false;
            if (playPauseButton != null)
            {
                playPauseButton.text = "Play";
            }
        }
        else
        {
            videoPlayer.Play();
            isVideoPlaying = true;
            if (playPauseButton != null)
            {
                playPauseButton.text = "Pause";
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
        if (playPauseButton != null)
        {
            playPauseButton.text = "Play";
        }
    }

    private void CloseVideoPlayerUI()
    {
        RestoreMusicAfterVideo();
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        
        isVideoPlaying = false;
        if (playPauseButton != null)
        {
            playPauseButton.text = "Play";
        }
        
        if (videoPlayerBackground != null)
        {
            videoPlayerBackground.style.display = DisplayStyle.None;
        }
        
        // Show BackgroundBase again
        if (backgroundBase != null)
        {
            backgroundBase.style.display = DisplayStyle.Flex;
        }
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

    public override void CloseWindow()
    {
        if (IsVideoPlayerOpen)
        {
            CloseVideoPlayerUI();
        }
    }

    public override void OpenWindow()
    {
        // Video player is opened via PlayVideo method
    }
}
