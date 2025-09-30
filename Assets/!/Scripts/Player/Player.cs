using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerManagment PlayerManagment { get; set; }
    [SerializeField] private Transform respawnPoint;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    void Awake()
    {
        PlayerManagment = new PlayerManagment(this);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        lastPosition = transform.position;
        PlayerManagment.journal.LoadQuests();


    }

    void Update()
    {
        float delta = Vector3.Distance(transform.position, lastPosition);
        accumulatedDistance += delta;

        if (accumulatedDistance >= 1f)
        {
            int metersWalked = Mathf.FloorToInt(accumulatedDistance);
            PlayerManagment.stats.AddWalk(metersWalked);
            accumulatedDistance -= metersWalked;
        }

        lastPosition = transform.position;
    }

    public void Respawn()
    {
        Transform player = transform;
        GameObject playerg = player.gameObject;
        playerg.SetActive(false);
        player.position = respawnPoint.position;
        playerg.SetActive(true);
    }
}
