using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Audio;

public class MemorySign : Interactable
{
    [Header("Video Settings")]
    [SerializeField] private Canvas memorySignCanvas;
    [SerializeField] private RenderTexture renderTextureTemplate;
    [SerializeField] private string title;
    [SerializeField] private string videoFileName;
    [SerializeField] private AudioMixer globalMixer;

    private RawImage memorySignImage;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private bool isVideoPlaying = false;
    private float previousMusicVolume;

    void Start()
    {
        InitializeComponents();
        SetupVideoPlayer();
    }

    private void InitializeComponents()
    {
        if (memorySignCanvas == null)
            memorySignCanvas = GetComponentInChildren<Canvas>();

        if (memorySignCanvas != null)
        {
            memorySignImage = memorySignCanvas.GetComponentInChildren<RawImage>();
            var titleText = memorySignCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
                titleText.text = title;
        }

        videoPlayer = GetComponentInChildren<VideoPlayer>();

        if (renderTextureTemplate != null)
        {
            RenderTexture instantiatedRT = new RenderTexture(
                renderTextureTemplate.width,
                renderTextureTemplate.height,
                renderTextureTemplate.depth,
                renderTextureTemplate.format
            );
            instantiatedRT.Create();

            if (memorySignImage != null)
                memorySignImage.texture = instantiatedRT;

            if (videoPlayer != null)
            {
                videoPlayer.targetTexture = instantiatedRT;
                videoPlayer.playOnAwake = false;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();

                audioSource.spatialBlend = 1.0f;
                audioSource.dopplerLevel = 0f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 15f;
                audioSource.loop = false;
                audioSource.playOnAwake = false;

                videoPlayer.SetTargetAudioSource(0, audioSource);

                videoPlayer.loopPointReached += OnVideoFinished;
            }
        }

        if (memorySignImage != null)
            memorySignImage.enabled = false;
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer == null || string.IsNullOrEmpty(videoFileName))
            return;

#if UNITY_WEBGL
        string url = Application.streamingAssetsPath + "/videos/" + UnityWebRequest.EscapeURL(videoFileName);
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
#else
        string path = Path.Combine(Application.streamingAssetsPath, "videos", videoFileName);

        if (File.Exists(path))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = path;
        }
        else
        {
            Debug.LogError("[MemorySign] Video file not found at: " + path);
        }
#endif
    }

    protected override void Interact()
    {
        ToggleVideo();
    }

    private void ToggleVideo()
    {
        if (videoPlayer == null || memorySignCanvas == null)
            return;

        if (isVideoPlaying)
        {
            StopVideo();
        }
        else
        {
            PlayVideo();
        }
    }

    private void PlayVideo()
    {
        globalMixer.GetFloat("MusicVolume", out previousMusicVolume);
        globalMixer.SetFloat("MusicVolume", -80f);

        if (memorySignImage != null)
            memorySignImage.enabled = true;

        videoPlayer.Play();
        isVideoPlaying = true;
        promptMessage = "Stop";
    }

    private void StopVideo()
    {
        videoPlayer.Stop();

        if (memorySignImage != null)
            memorySignImage.enabled = false;

        isVideoPlaying = false;
        promptMessage = "Play";

        globalMixer.SetFloat("MusicVolume", previousMusicVolume);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StopVideo();
    }

    void OnDestroy()
    {
        if (isVideoPlaying && globalMixer != null)
        {
            globalMixer.SetFloat("MusicVolume", previousMusicVolume);
        }
    }
}
