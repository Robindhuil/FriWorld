using UnityEngine;

public class Door : Interactable
{
    [SerializeField]
    private GameObject door;
    private AudioClip openSound;
    private AudioClip closeSound;
    private AudioSource audioSource;
    private bool isOpen;
    private Collider[] doorColliders;

    void Start()
    {
        door = gameObject;
        doorColliders = door.GetComponentsInChildren<Collider>();

        audioSource = door.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = door.AddComponent<AudioSource>();
        }

        audioSource.volume = 0.4f;


        openSound = Resources.Load<AudioClip>("sounds/effects/door_open_sound");
        closeSound = Resources.Load<AudioClip>("sounds/effects/door_close_sound");

        if (openSound == null || closeSound == null)
        {
            Debug.LogWarning("[Door] Zvukové súbory pre otváranie/zatváranie dverí neboli nájdené v Resources/Sounds/Effects.");
        }
    }

    protected override void Interact()
    {
        isOpen = !isOpen;
        door.GetComponent<Animator>().SetBool("IsOpen", isOpen);

        if (audioSource != null)
        {
            audioSource.clip = isOpen ? openSound : closeSound;
            audioSource.Play();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

            foreach (var doorCol in doorColliders)
            {
                foreach (var playerCol in playerColliders)
                {
                    Physics.IgnoreCollision(doorCol, playerCol, isOpen);
                }
            }
        }
    }
}
