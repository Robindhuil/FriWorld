using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Navigator.Tests
{
    public class DoorPassageTests
    {
        private const float Step = 0.25f;

        // A closed door in the plane x = 0, 1 m wide (z from -0.5 to 0.5), on a floor at y = 0.
        private static DoorLeaf Door() => new DoorLeaf
        {
            center = new Vector3(0f, 1f, 0f),
            normal = Vector3.right,
            along = Vector3.forward,
            halfWidth = 0.5f,
            bottom = 0f,
        };

        private static List<Vector3> Path(Vector3 from, Vector3 to)
        {
            var points = new List<Vector3>();
            int n = Mathf.RoundToInt(Vector3.Distance(from, to) / Step);
            for (int i = 0; i <= n; i++)
                points.Add(Vector3.Lerp(from, to, (float)i / n));
            return points;
        }

        [Test]
        public void PathThroughTheDoorwayPassesWhereItCrossesTheDoorPlane()
        {
            var path = Path(new Vector3(-5f, 0f, 0.2f), new Vector3(5f, 0f, 0.2f));

            Assert.IsTrue(DoorPassage.TryFindPass(path, Step, Door(), out float pass, out Vector3 direction));
            Assert.AreEqual(5f, pass, 0.01f);
            Assert.AreEqual(1f, Vector3.Dot(direction, Vector3.right), 0.001f);
        }

        [Test]
        public void PathCrossingTheDoorPlaneBesideTheDoorDoesNotPass()
        {
            var path = Path(new Vector3(-5f, 0f, 1.5f), new Vector3(5f, 0f, 1.5f));

            Assert.IsFalse(DoorPassage.TryFindPass(path, Step, Door(), out _, out _));
        }

        [Test]
        public void PathAlongTheCorridorPastTheDoorDoesNotPass()
        {
            var path = Path(new Vector3(-1f, 0f, -5f), new Vector3(-1f, 0f, 5f));

            Assert.IsFalse(DoorPassage.TryFindPass(path, Step, Door(), out _, out _));
        }

        [Test]
        public void PathThroughTheSameSpotOnTheFloorAboveDoesNotPass()
        {
            var path = Path(new Vector3(-5f, 3.6f, 0f), new Vector3(5f, 3.6f, 0f));

            Assert.IsFalse(DoorPassage.TryFindPass(path, Step, Door(), out _, out _));
        }

        [Test]
        public void PassedDoorIsClosedFarAheadOpenAsTheCameraPassesAndClosedFarBehind()
        {
            Assert.AreEqual(0f, DoorPassage.RouteOpening(10f - 3.5f, 10f), 1e-4f);
            Assert.AreEqual(1f, DoorPassage.RouteOpening(10f, 10f), 1e-4f);
            Assert.AreEqual(0f, DoorPassage.RouteOpening(10f + 4f, 10f), 1e-4f);
        }

        [Test]
        public void PassedDoorIsFullyOpenAMetreBeforeTheCameraReachesIt()
        {
            Assert.AreEqual(1f, DoorPassage.RouteOpening(10f - 1f, 10f), 1e-4f);

            float halfway = DoorPassage.RouteOpening(10f - 2f, 10f);
            Assert.Greater(halfway, 0f);
            Assert.Less(halfway, 1f);
        }

        [Test]
        public void RoomDoorStaysClosedUntilTheLastMetresAndIsOpenAtTheEnd()
        {
            Assert.AreEqual(0f, DoorPassage.TargetOpening(20f - 3f, 20f), 1e-4f);
            Assert.AreEqual(1f, DoorPassage.TargetOpening(20f, 20f), 1e-4f);
        }

        [Test]
        public void DoorOpensAwayFromTheCameraTheWayFriWorldsDoorScriptDoes()
        {
            // Hinge at z = -0.5, leaf reaching to z = +0.5. A positive yaw swings it towards +x,
            // and FriWorld's Door picks DoorRotation -90.9 when that is away from whoever comes.
            var hinge = new Vector3(0f, 0f, -0.5f);
            var center = new Vector3(0f, 1f, 0f);

            Assert.AreEqual(-90.9f, DoorPassage.OpenRotation(hinge, center, Vector3.right), 1e-4f);
            Assert.AreEqual(90.9f, DoorPassage.OpenRotation(hinge, center, Vector3.left), 1e-4f);
        }

        [Test]
        public void RoomDoorIsTheOneTheAnchorFaces()
        {
            var anchor = new Vector3(-RoomAnchors.DoorDistance, 0f, 0f);

            Assert.IsTrue(DoorPassage.IsRoomDoor(Door(), anchor, Vector3.right));
            Assert.IsFalse(DoorPassage.IsRoomDoor(Door(), anchor + new Vector3(0f, 0f, 3f), Vector3.right));
            Assert.IsFalse(DoorPassage.IsRoomDoor(Door(), anchor + new Vector3(0f, 3.6f, 0f), Vector3.right));
        }

        [Test]
        public void MeasuresALeafFromItsMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                go.transform.position = new Vector3(2f, 1f, 3f);
                go.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                go.transform.localScale = new Vector3(0.05f, 2f, 0.9f);   // thin along x, 0.9 m wide

                Assert.IsTrue(DoorLeaf.TryMeasure(go.transform, out DoorLeaf leaf));
                Assert.AreEqual(0.45f, leaf.halfWidth, 1e-3f);
                Assert.AreEqual(0f, leaf.bottom, 1e-3f);
                Assert.AreEqual(0f, Vector3.Distance(leaf.center, new Vector3(2f, 1f, 3f)), 1e-3f);
                Assert.AreEqual(1f, Mathf.Abs(Vector3.Dot(leaf.normal, go.transform.right)), 1e-3f);
                Assert.AreEqual(1f, Mathf.Abs(Vector3.Dot(leaf.along, go.transform.forward)), 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
