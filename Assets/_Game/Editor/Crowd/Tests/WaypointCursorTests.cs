using FriWorld.Crowd;
using NUnit.Framework;

namespace FriWorld.Crowd.Tests
{
    public class WaypointCursorTests
    {
        [Test]
        public void SequentialWrapsAround()
        {
            var cursor = new WaypointCursor(3, randomOrder: false, new System.Random(1));
            cursor.Reset(2);

            Assert.AreEqual(0, cursor.Next());
            Assert.AreEqual(1, cursor.Next());
            Assert.AreEqual(2, cursor.Next());
            Assert.AreEqual(0, cursor.Next());
        }

        [Test]
        public void RandomStaysInRange()
        {
            var cursor = new WaypointCursor(5, randomOrder: true, new System.Random(7));

            for (int i = 0; i < 200; i++)
            {
                int index = cursor.Next();
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 5);
            }
        }

        [Test]
        public void RandomNeverRepeatsTheCurrentPoint()
        {
            // Being sent to the point you are already standing on reads as a frozen NPC, not as
            // a pause, so it has to be impossible rather than merely unlikely.
            var cursor = new WaypointCursor(4, randomOrder: true, new System.Random(3));

            int previous = cursor.Next();
            for (int i = 0; i < 200; i++)
            {
                int index = cursor.Next();
                Assert.AreNotEqual(previous, index, "step " + i);
                previous = index;
            }
        }

        [Test]
        public void RandomEventuallyVisitsEveryPoint()
        {
            var cursor = new WaypointCursor(6, randomOrder: true, new System.Random(11));
            var seen = new bool[6];

            for (int i = 0; i < 400; i++) seen[cursor.Next()] = true;

            for (int i = 0; i < 6; i++) Assert.IsTrue(seen[i], "never visited " + i);
        }

        [Test]
        public void OnePointIsAlwaysThatPoint()
        {
            // With nowhere else to go, repeating is the only option and must not spin forever.
            var cursor = new WaypointCursor(1, randomOrder: true, new System.Random(3));

            Assert.AreEqual(0, cursor.Next());
            Assert.AreEqual(0, cursor.Next());
        }

        [Test]
        public void NoPointsGivesMinusOne()
        {
            Assert.AreEqual(-1, new WaypointCursor(0, true, new System.Random(1)).Next());
        }

        [Test]
        public void TheSameSeedWalksTheSameRoute()
        {
            var a = new WaypointCursor(6, true, new System.Random(99));
            var b = new WaypointCursor(6, true, new System.Random(99));

            for (int i = 0; i < 50; i++) Assert.AreEqual(a.Next(), b.Next(), "step " + i);
        }
    }
}
