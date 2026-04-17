using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField]
    private bool useEvents;

    [SerializeField]
    private string promptMessage;
    public string PromptMessage
    {
        get => promptMessage;
        set => promptMessage = value;
    }

    [Header("Optional Sound Settings")]
    [SerializeField]
    private bool playSoundEffect = false;
    public bool PlaySoundEffect
    {
        get => playSoundEffect;
        set => playSoundEffect = value;
    }

    [SerializeField]
    protected AudioSource audioSource;

    [SerializeField]
    private AudioClip soundClip;

    [SerializeField]
    private float maxHearableRange = 5f;
    public float MaxHearableRange
    {
        get => maxHearableRange;
        set => maxHearableRange = value;
    }

    [SerializeField]
    private EchoEnvironment echoEnvironment = EchoEnvironment.None;

    public void BaseInteract()
    {
        if (useEvents)
        {
            GetComponent<InteractionEvent>()?.onInteract.Invoke();
        }

        if (playSoundEffect && audioSource != null && soundClip != null)
        {
            audioSource.PlayOneShot(soundClip);
        }

        Interact();
    }

    protected virtual void Interact() { }

    protected void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        SpatialAudioUtility.Configure3D(
            audioSource,
            maxDistance: maxHearableRange,
            echo: echoEnvironment
        );
    }

    private void OnValidate()
    {
        if (playSoundEffect)
            EnsureAudioSource();
    }

    public void SetPromptMessage(string message)
    {
        promptMessage = message;
    }
}
