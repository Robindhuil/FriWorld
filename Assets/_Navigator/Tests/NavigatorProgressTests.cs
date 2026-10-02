using NUnit.Framework;

namespace FriWorld.Navigator.Tests
{
    public class NavigatorProgressTests
    {
        private const float Duration = 40f;

        [Test]
        public void ReportsTheTimeAFewTimesASecondWhilePlaying()
        {
            var progress = new NavigatorProgress();

            Assert.IsTrue(progress.Next(0f, 0f, Duration, playing: true, jumped: false).time);
            Assert.IsFalse(progress.Next(0.1f, 0.1f, Duration, playing: true, jumped: false).time);
            Assert.IsTrue(progress.Next(NavigatorProgress.Interval, 0.2f, Duration, playing: true, jumped: false).time);
        }

        [Test]
        public void ReportsAtOnceWhenPausedOrResumed()
        {
            var progress = new NavigatorProgress();
            progress.Next(0f, 0f, Duration, playing: true, jumped: false);

            Assert.IsTrue(progress.Next(0.05f, 0.05f, Duration, playing: false, jumped: false).time);
            Assert.IsTrue(progress.Next(0.08f, 0.05f, Duration, playing: true, jumped: false).time);
        }

        [Test]
        public void StaysQuietWhilePaused()
        {
            var progress = new NavigatorProgress();
            progress.Next(0f, 5f, Duration, playing: false, jumped: false);

            Assert.IsFalse(progress.Next(3f, 5f, Duration, playing: false, jumped: false).time);
        }

        [Test]
        public void ReportsAJumpAtOnce()
        {
            var progress = new NavigatorProgress();
            progress.Next(0f, 0f, Duration, playing: true, jumped: false);

            Assert.IsTrue(progress.Next(0.05f, 20f, Duration, playing: true, jumped: true).time);
        }

        [Test]
        public void ReportsTheEndOnceForEachTimeTheFlightGetsThere()
        {
            var progress = new NavigatorProgress();
            progress.Next(0f, 39.9f, Duration, playing: true, jumped: false);

            var end = progress.Next(0.5f, Duration, Duration, playing: false, jumped: false);
            Assert.IsTrue(end.ended);
            Assert.IsTrue(end.time);
            Assert.IsFalse(progress.Next(1f, Duration, Duration, playing: false, jumped: false).ended);

            progress.Next(2f, 0f, Duration, playing: true, jumped: false);
            Assert.IsTrue(progress.Next(50f, Duration, Duration, playing: false, jumped: false).ended);
        }

        [Test]
        public void ANewFlightIsReportedFromItsStart()
        {
            var progress = new NavigatorProgress();
            progress.Next(0f, Duration, Duration, playing: false, jumped: false);

            progress.Reset();

            Assert.IsTrue(progress.Next(0.01f, 0f, 30f, playing: true, jumped: false).time);
        }
    }
}
