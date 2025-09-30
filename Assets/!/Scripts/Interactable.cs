using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public bool useEvents;
    [SerializeField] public string promptMessage;

    [Header("Optional Sound Settings")]
    [SerializeField] private bool playSoundEffect = false;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundClip;

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

    private void OnValidate()
    {
        if (playSoundEffect)
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }
    }
}
