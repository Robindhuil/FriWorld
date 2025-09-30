using UnityEngine;

public class PlayerMovementSounds : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepsSound;
    public AudioSource sprintSound;
    public AudioSource jumpStartSound;
    public AudioSource jumpEndSound;

    [Header("Ground Check")]
    public float raycastDistance = 0.3f;
    public Transform raycastOrigin;
    public LayerMask groundLayers;

    private bool wasGrounded = true;

    void Update()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                       Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        bool isGrounded = CheckIfGrounded();

        HandleJumpSounds(isGrounded);

        HandleMovementSounds(isMoving, isGrounded);

        wasGrounded = isGrounded;
    }

    void HandleJumpSounds(bool isGrounded)
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpStartSound.Play();
        }

        if (isGrounded && !wasGrounded)
        {
            jumpEndSound.Play();
        }
    }


    void HandleMovementSounds(bool isMoving, bool isGrounded)
    {
        if (isMoving && isGrounded)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                footstepsSound.enabled = false;
                sprintSound.enabled = true;
            }
            else
            {
                footstepsSound.enabled = true;
                sprintSound.enabled = false;
            }
        }
        else
        {
            footstepsSound.enabled = false;
            sprintSound.enabled = false;
        }
    }

    bool CheckIfGrounded()
    {
        bool grounded = Physics.Raycast(
            raycastOrigin.position,
            Vector3.down,
            raycastDistance,
            groundLayers
        );

        Debug.DrawRay(raycastOrigin.position, Vector3.down * raycastDistance,
                    grounded ? Color.green : Color.red);

        return grounded;
    }
}