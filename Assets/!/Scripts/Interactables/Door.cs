using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    private GameObject door;

    [SerializeField]
    private SoundEffectId openSoundId = SoundEffectId.Interactable_DoorOpen;

    [SerializeField]
    private SoundEffectId closeSoundId = SoundEffectId.Interactable_DoorClose;

    [SerializeField]
    private float autoCloseDelaySeconds = 2f;

    [SerializeField]
    private float openRotationAmount = 90.9f;

    private float rotationTransitionDurationSeconds = 0.1f;

    [SerializeField]
    private bool invertOpenDirection;

    [SerializeField]
    private string npcTag = "npc";

    [SerializeField]
    private bool detectNavMeshAgentsWithoutColliders = true;

    [SerializeField]
    private float navMeshDetectionPadding = 0.1f;

    private NpcProximityDetector npcDetector;
    private Animator doorAnimator;
    private Collider[] doorColliders;

    private bool isOpen;
    private Coroutine autoCloseCoroutine;
    private float closeAtTime;
    private float currentDoorRotation;
    private float targetDoorRotation;
    private Vector3[] doorProbePointsLocal = System.Array.Empty<Vector3>();

    private const string DoorRotationParam = "DoorRotation";
    private const float ClosedDoorRotation = 0f;

    void Start()
    {
        door = gameObject;
        doorAnimator = door.GetComponent<Animator>();
        BuildDoorProbePoints();
        currentDoorRotation = ClosedDoorRotation;
        targetDoorRotation = ClosedDoorRotation;

        if (doorAnimator != null)
        {
            doorAnimator.SetFloat(DoorRotationParam, currentDoorRotation);
            doorAnimator.SetBool("IsOpen", false);
        }

        doorColliders = door.GetComponentsInChildren<Collider>();

        if (GlobalSoundEffectRegistry.Instance == null)
        {
            Debug.LogWarning("[Door] GlobalSoundEffectRegistry nebol najdeny v scene.");
        }

        npcDetector = new NpcProximityDetector(
            gameObject,
            npcTag,
            detectNavMeshAgentsWithoutColliders,
            navMeshDetectionPadding
        );
        npcDetector.OnNpcEntered += RegisterNpcEntry;
    }

    void Update()
    {
        UpdateDoorRotationTransition();
        npcDetector?.Tick();
    }

    private void OnDestroy()
    {
        npcDetector?.Dispose();
    }

    private Bounds GetDoorMeshBoundsInSpace(Transform targetSpace)
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Vector3 localDoorPosition = targetSpace.InverseTransformPoint(door.transform.position);
            return new Bounds(localDoorPosition, Vector3.one);
        }

        bool hasBounds = false;
        Bounds combinedLocalBounds = new Bounds();

        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.bounds;
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localCorner = targetSpace.InverseTransformPoint(worldCorner);

                        if (!hasBounds)
                        {
                            combinedLocalBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            combinedLocalBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        return combinedLocalBounds;
    }

    private void RegisterNpcEntry(Vector3 sourcePosition)
    {
        if (!isOpen)
        {
            SetDoorRotationFromSource(sourcePosition);
            OpenDoor();
        }

        if (autoCloseCoroutine == null || closeAtTime <= Time.time)
        {
            closeAtTime = Time.time + autoCloseDelaySeconds;
        }
        else
        {
            closeAtTime += autoCloseDelaySeconds;
        }

        if (autoCloseCoroutine == null)
        {
            autoCloseCoroutine = StartCoroutine(AutoCloseWhenTimerExpires());
        }
    }

    private IEnumerator AutoCloseWhenTimerExpires()
    {
        while (Time.time < closeAtTime)
        {
            yield return null;
        }

        CloseDoor();
        autoCloseCoroutine = null;
    }

    private void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", true);
        }

        PlayDoorSound(openSoundId);
        UpdatePlayerDoorCollision(true);
    }

    private void CloseDoor()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        targetDoorRotation = ClosedDoorRotation;

        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", false);
        }

        PlayDoorSound(closeSoundId);
        UpdatePlayerDoorCollision(false);
    }

    private void PlayDoorSound(SoundEffectId soundId)
    {
        AudioClip clip = GlobalSoundEffectRegistry.GetRandom(soundId);
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void UpdatePlayerDoorCollision(bool ignore)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

        foreach (Collider doorCol in doorColliders)
        {
            foreach (Collider playerCol in playerColliders)
            {
                Physics.IgnoreCollision(doorCol, playerCol, ignore);
            }
        }
    }

    private void Reset()
    {
        PlaySoundEffect = true;
        PromptMessage = "'E' Open the door";
        MaxHearableRange = 3f;
    }

    protected override void Interact()
    {
        if (isOpen)
        {
            CloseDoor();
            closeAtTime = 0f;

            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetDoorRotationFromSource(player.transform.position);
            }
            else
            {
                targetDoorRotation = openRotationAmount;
            }

            OpenDoor();
        }
    }

    private void SetDoorRotationFromSource(Vector3 sourcePosition)
    {
        float directionalScore = GetOpenDirectionScore(sourcePosition);
        float selectedRotation = directionalScore >= 0f ? -openRotationAmount : openRotationAmount;

        if (Mathf.Abs(directionalScore) <= 0.0001f)
        {
            Vector3 toSource = sourcePosition - door.transform.position;
            toSource.y = 0f;
            if (toSource.sqrMagnitude <= 0.0001f)
            {
                selectedRotation = openRotationAmount;
            }
            else
            {
                float side = Vector3.Dot(door.transform.right, toSource.normalized);
                selectedRotation = side >= 0f ? -openRotationAmount : openRotationAmount;
            }
        }

        if (invertOpenDirection)
        {
            selectedRotation *= -1f;
        }

        targetDoorRotation = selectedRotation;
    }

    private void UpdateDoorRotationTransition()
    {
        if (doorAnimator == null)
        {
            return;
        }

        // Idle door: already at target — skip the per-frame animator write (most doors, most frames).
        if (currentDoorRotation == targetDoorRotation)
        {
            return;
        }

        float totalRange = Mathf.Max(1f, Mathf.Abs(openRotationAmount));
        float maxStepPerSecond = totalRange / rotationTransitionDurationSeconds;

        currentDoorRotation = Mathf.MoveTowards(
            currentDoorRotation,
            targetDoorRotation,
            maxStepPerSecond * Time.deltaTime
        );

        doorAnimator.SetFloat(DoorRotationParam, currentDoorRotation);
    }

    private void BuildDoorProbePoints()
    {
        Bounds localBounds = GetDoorMeshBoundsInSpace(door.transform);

        Vector3 center = localBounds.center;
        float minX = localBounds.min.x;
        float maxX = localBounds.max.x;
        float minZ = localBounds.min.z;
        float maxZ = localBounds.max.z;

        doorProbePointsLocal = new[]
        {
            center,
            new Vector3(minX, center.y, minZ),
            new Vector3(minX, center.y, maxZ),
            new Vector3(maxX, center.y, minZ),
            new Vector3(maxX, center.y, maxZ),
        };
    }

    private float GetOpenDirectionScore(Vector3 sourcePosition)
    {
        if (doorProbePointsLocal == null || doorProbePointsLocal.Length == 0)
        {
            return 0f;
        }

        Vector3 sourceFlat = sourcePosition;
        sourceFlat.y = door.transform.position.y;
        Vector3 pivot = door.transform.position;

        float accumulatedScore = 0f;

        foreach (Vector3 pointLocal in doorProbePointsLocal)
        {
            Vector3 worldPoint = door.transform.TransformPoint(pointLocal);

            Vector3 hingeToPoint = worldPoint - pivot;
            hingeToPoint.y = 0f;
            if (hingeToPoint.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            Vector3 awayFromSource = worldPoint - sourceFlat;
            awayFromSource.y = 0f;
            if (awayFromSource.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            // Tangent direction for positive Y rotation around hinge pivot.
            Vector3 positiveTangent = Vector3.Cross(Vector3.up, hingeToPoint).normalized;
            Vector3 awayDir = awayFromSource.normalized;
            accumulatedScore += Vector3.Dot(positiveTangent, awayDir);
        }

        return accumulatedScore;
    }
}
