using System.Collections.Generic;
using FriWorld.Navigator.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Navigator.Tests
{
    public class RoomPointDoorwayTests
    {
        // A closed door in a wall along x, 0.9 m wide, with its floor at the given height.
        private static DoorLeaf Door(float x, float z, float floor) => new DoorLeaf
        {
            center = new Vector3(x, floor + 1f, z),
            normal = Vector3.forward,
            along = Vector3.right,
            halfWidth = 0.45f,
            bottom = floor,
        };

        [Test]
        public void PicksTheDoorThePointStandsIn()
        {
            var doors = new List<DoorLeaf> { Door(3f, 0f, 7f), Door(0f, 0f, 7f) };

            Assert.IsTrue(NavigatorBake.TryFindDoorway(new Vector3(0.1f, 7f, 0.08f), doors, out int index));
            Assert.AreEqual(1, index);
        }

        [Test]
        public void IgnoresTheDoorRightAboveOnTheNextFloor()
        {
            var doors = new List<DoorLeaf> { Door(0f, 0f, 10.6f), Door(0.6f, 0f, 7f) };

            Assert.IsTrue(NavigatorBake.TryFindDoorway(new Vector3(0f, 7f, 0f), doors, out int index));
            Assert.AreEqual(1, index);
        }

        [Test]
        public void FindsNoDoorWhenThePointStandsAwayFromAll()
        {
            var doors = new List<DoorLeaf> { Door(0f, 0f, 7f) };

            Assert.IsFalse(NavigatorBake.TryFindDoorway(new Vector3(0f, 7f, 2.6f), doors, out _));
        }
    }
}
