using UnityEngine;
using UnityEngine.AI;

namespace FriWorld.Navigator
{
    /// <summary>
    /// Flies the camera from the reception to the door of one room, controlled like a video.
    ///
    /// The start is wherever the camera is placed in the scene — position and rotation. It is
    /// remembered before the first flight, so every <see cref="Go"/> begins there again.
    /// <see cref="Go"/> computes the whole flight once; every frame only sets the camera to the
    /// pose at the current time, and the doors on the way to how open they are then
    /// (<see cref="NavigatorDoors"/>). The public methods take at most one string or float so the
    /// web page can call them through <c>SendMessage("Navigator", …)</c>; what the flight does goes
    /// back to the page through <see cref="NavigatorPage"/>.
    /// </summary>
    public class NavigatorController : MonoBehaviour
    {
        [SerializeField] private RoomAnchors anchors;
        [SerializeField] private Camera flyCamera;

        [Tooltip("Room the flight goes to in the editor, e.g. RA101. The web build takes it from ?room= in the URL instead.")]
        [SerializeField] private string roomCode = "RA101";

        [Tooltip("Outside the web build: fly to Room Code on start.")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private CameraTrack.Settings track = CameraTrack.Settings.Default;

        private CameraTrack current;
        private NavigatorDoors doors;
        private float time;
        private float speed = 1f;
        private bool playing;

        private readonly NavigatorProgress progress = new NavigatorProgress();
        private bool jumped;

        private bool startCaptured;
        private Vector3 startPosition;
        private Quaternion startRotation;

        public string RoomCode => roomCode;
        public float Duration => current != null ? current.Duration : 0f;
        public float CurrentTime => time;
        public bool IsPlaying => playing;

        private void Awake()
        {
            CaptureStart();
            // In Awake, so the doors' own Door scripts are off before their Start runs.
            doors = new NavigatorDoors();
        }

        private void Start()
        {
            // A link like …/index.html?room=RA101 starts its flight right away. Without one the web
            // build waits for the page to call Go, and never flies to the editor's test room.
            // Application.platform and not FriWorld's PlatformFlags: only a real web player has a
            // page URL, and FriWorld's code is out of this assembly's reach anyway.
            if (RoomLink.TryGetCode(Application.absoluteURL, out string code))
                Go(code);
            else if (playOnStart && Application.platform != RuntimePlatform.WebGLPlayer)
                Go(roomCode);
        }

        /// <summary>Plans the flight to <paramref name="code"/> and starts it from the beginning.</summary>
        public void Go(string code)
        {
            if (anchors == null || flyCamera == null)
            {
                Debug.LogError("[Navigator] RoomAnchors or camera is not assigned.", this);
                NavigatorPage.Error(NavigatorPage.NotSetUp);
                return;
            }

            if (!anchors.TryGet(code, out RoomAnchors.Anchor anchor))
            {
                Debug.LogError($"[Navigator] Unknown room code '{code}'.", this);
                NavigatorPage.Error(NavigatorPage.UnknownRoom);
                return;
            }

            CaptureStart();
            var filter = new NavMeshQueryFilter { agentTypeID = anchors.agentTypeID, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(startPosition, out NavMeshHit floor, track.eyeHeight + 1f, filter))
            {
                Debug.LogError("[Navigator] The camera is not placed above the Navigator navmesh.", this);
                NavigatorPage.Error(NavigatorPage.StartOffNavMesh);
                return;
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(floor.position, anchor.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError($"[Navigator] No complete path to {anchor.code} ({path.status}). Is the Navigator navmesh baked?", this);
                NavigatorPage.Error(NavigatorPage.NoPath);
                return;
            }

            current = CameraTrack.Build(path.corners, startPosition, startRotation, anchor.facing, track, filter);
            doors.Plan(current, anchor.code);
            roomCode = anchor.code;
            time = 0f;
            playing = true;
            progress.Reset();
            NavigatorPage.Ready(current.Duration);
            Apply();
        }

        public void Play()
        {
            if (current == null)
                return;
            if (time >= current.Duration)
                time = 0f;
            playing = true;
        }

        public void Pause()
        {
            playing = false;
        }

        /// <summary>Jumps to <paramref name="seconds"/> into the flight; keeps playing or paused.</summary>
        public void Seek(float seconds)
        {
            if (current == null)
                return;
            time = Mathf.Clamp(seconds, 0f, current.Duration);
            jumped = true;
            Apply();
        }

        /// <summary>Playback rate: 1 is normal, 2 twice as fast, 0.5 half.</summary>
        public void SetSpeed(float rate)
        {
            speed = Mathf.Max(0f, rate);
        }

        private void Update()
        {
            if (current == null)
                return;

            if (playing)
            {
                // Smoothed: a raw deltaTime that jitters around the display's refresh moves the
                // camera in uneven steps, which reads as a shake even though every pose is on the path.
                time += Time.smoothDeltaTime * speed;
                if (time >= current.Duration)
                {
                    time = current.Duration;
                    playing = false;
                }

                Apply();
            }

            // Also while paused: the page has to hear about a pause, a resume or a jump.
            NavigatorProgress.Report report = progress.Next(Time.unscaledTime, time, current.Duration, playing, jumped);
            jumped = false;
            if (report.time)
                NavigatorPage.Progress(time, playing);
            if (report.ended)
                NavigatorPage.Ended();
        }

        private void CaptureStart()
        {
            if (startCaptured || flyCamera == null)
                return;
            startPosition = flyCamera.transform.position;
            startRotation = flyCamera.transform.rotation;
            startCaptured = true;
        }

        private void Apply()
        {
            current.Evaluate(time, out Vector3 position, out Quaternion rotation);
            flyCamera.transform.SetPositionAndRotation(position, rotation);
            doors.Apply(current.DistanceAt(time));
        }
    }
}
