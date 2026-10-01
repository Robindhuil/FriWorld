using System.Collections.Generic;
using UnityEngine;

namespace FriWorld.Navigator
{
    /// <summary>
    /// Opens the doors the flight goes through by setting DoorRotation on the Animator each door
    /// already has — the parameter FriWorld's Door script drives, so the leaf plays its own
    /// animation. Nothing is added to or changed on a door. The room's own door stays closed: the
    /// flight ends in front of it.
    ///
    /// Doors are found by their "Door" tag, which the object type registry gives to real doors
    /// only, never to frames. A door without an Animator — a desktop-only door in the web build —
    /// is not passable and stays closed.
    ///
    /// What the Navigator does not need is switched off at runtime; the assets stay as they are.
    /// FriWorld's Door script has no player or NPC to react to here, and its Start would build an
    /// NPC detector per door and warn 284 times that the sound registry is missing. The door
    /// Animators are set to Always Animate and would be evaluated every frame, closed and out of
    /// sight; only those of the current flight's doors run.
    /// </summary>
    public sealed class NavigatorDoors
    {
        private const string DoorTag = "Door";
        private const string DoorScript = "Door";   // FriWorld's, found by name: its assembly is out of reach
        private static readonly int RotationParameter = Animator.StringToHash("DoorRotation");

        private struct Door
        {
            public Animator animator;
            public string name;
            public Vector3 hinge;
            public DoorLeaf leaf;
        }

        private struct Opened
        {
            public Animator animator;
            public float pass;       // distance along the path where the camera goes through
            public float rotation;   // DoorRotation when fully open
        }

        private readonly List<Door> doors = new List<Door>();
        private readonly List<Opened> opened = new List<Opened>();

        /// <summary>
        /// Measures every door once, while all of them are still closed, and switches off its Door
        /// script and Animator. Must run in Awake, before the Door scripts' own Start.
        /// </summary>
        public NavigatorDoors()
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(DoorTag))
            {
                if (go.GetComponent(DoorScript) is Behaviour script)
                    script.enabled = false;

                var animator = go.GetComponent<Animator>();
                if (animator != null)
                    animator.enabled = false;

                if (DoorLeaf.TryMeasure(go.transform, out DoorLeaf leaf))
                    doors.Add(new Door { animator = animator, name = go.name, hinge = go.transform.position, leaf = leaf });
            }
        }

        /// <summary>Doors the current flight opens.</summary>
        public int OpenedCount => opened.Count;

        /// <summary>Picks the doors of one flight; those of the previous flight close.</summary>
        public void Plan(CameraTrack track, string roomCode)
        {
            Close();
            opened.Clear();
            foreach (var door in doors)
            {
                if (!DoorPassage.TryFindPass(track.Points, track.Step, door.leaf, out float pass, out Vector3 direction))
                    continue;

                if (door.animator == null)
                {
                    Debug.LogWarning($"[Navigator] The flight to {roomCode} goes through {door.name}, which has no Animator, so it stays closed.");
                    continue;
                }

                door.animator.enabled = true;
                opened.Add(new Opened
                {
                    animator = door.animator,
                    pass = pass,
                    rotation = DoorPassage.OpenRotation(door.hinge, door.leaf.center, direction),
                });
            }
        }

        /// <summary>Sets the flight's doors for the camera <paramref name="distance"/> along the path.</summary>
        public void Apply(float distance)
        {
            foreach (var door in opened)
                door.animator.SetFloat(RotationParameter, door.rotation * DoorPassage.RouteOpening(distance, door.pass));
        }

        /// <summary>
        /// Shuts the flight's doors and switches their Animators off again. The closed pose is
        /// applied first: a disabled Animator no longer writes the leaf, which would stay open.
        /// </summary>
        private void Close()
        {
            foreach (var door in opened)
            {
                if (door.animator == null)
                    continue;
                door.animator.SetFloat(RotationParameter, 0f);
                door.animator.Update(0f);
                door.animator.enabled = false;
            }
        }
    }
}
