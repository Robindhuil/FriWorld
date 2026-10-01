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
    /// </summary>
    public sealed class NavigatorDoors
    {
        private const string DoorTag = "Door";
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

        /// <summary>Measures every door once, while all of them are still closed.</summary>
        public NavigatorDoors()
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(DoorTag))
            {
                if (DoorLeaf.TryMeasure(go.transform, out DoorLeaf leaf))
                    doors.Add(new Door { animator = go.GetComponent<Animator>(), name = go.name, hinge = go.transform.position, leaf = leaf });
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

        private void Close()
        {
            foreach (var door in opened)
            {
                if (door.animator != null)
                    door.animator.SetFloat(RotationParameter, 0f);
            }
        }
    }
}
