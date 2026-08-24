using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class RoomPlatformsTests
    {
        [Test]
        public void AMissingPlatformMeansUndecided()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra102"" } ] }";

            var entry = RoomPlatforms.FromJson(json).Find("ra102");

            Assert.IsNotNull(entry);
            Assert.IsNull(entry.platform);
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void AllIsDecidedAndDifferentFromUndecided()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra101"", ""platform"": ""all"" } ] }";

            var entry = RoomPlatforms.FromJson(json).Find("ra101");

            Assert.IsTrue(entry.IsDecided, "'no gate here' is not the same as 'not decided'");
            Assert.AreEqual(RoomPlatforms.All, entry.platform);
        }

        [Test]
        public void LookupIsExactNotSubstring()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra100_corridor"", ""platform"": ""all"" } ] }";

            var platforms = RoomPlatforms.FromJson(json);

            Assert.IsNotNull(platforms.Find("ra100_corridor"));
            Assert.IsNull(platforms.Find("ra100_corridor_1"),
                "corridor 1 is its own area and must not inherit a prefix-shaped entry");
        }

        [Test]
        public void PlatformOfIsNullForAnUnknownArea()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [] }");

            Assert.IsNull(platforms.PlatformOf("ra102"));
        }

        [Test]
        public void UndecidedEntriesAreWrittenWithoutAPlatformField()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [ { ""room"": ""ra102"" } ] }");

            StringAssert.DoesNotContain("platform", platforms.ToJson(),
                "a written null would be indistinguishable from a deliberate decision");
        }

        [Test]
        public void UndecidedEntriesFloatToTheTop()
        {
            const string json = @"{ ""rooms"": [
                { ""room"": ""aaa"", ""platform"": ""all"" },
                { ""room"": ""zzz"" } ] }";

            var text = RoomPlatforms.FromJson(json).ToJson();

            Assert.Less(text.IndexOf("zzz"), text.IndexOf("aaa"),
                "a freshly synced area must be the first thing in the file");
        }

        [Test]
        public void DecidedEntriesAreOrderedAlphabetically()
        {
            const string json = @"{ ""rooms"": [
                { ""room"": ""zzz"", ""platform"": ""all"" },
                { ""room"": ""aaa"", ""platform"": ""all"" } ] }";

            var text = RoomPlatforms.FromJson(json).ToJson();

            Assert.Less(text.IndexOf("aaa"), text.IndexOf("zzz"));
        }

        [Test]
        public void OnlyTheThreeKnownPlatformValuesAreValid()
        {
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("all"));
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("desktopOnly"));
            Assert.IsTrue(RoomPlatforms.IsValidPlatform("webOnly"));
            Assert.IsFalse(RoomPlatforms.IsValidPlatform("desktop"));
            Assert.IsFalse(RoomPlatforms.IsValidPlatform(null));
        }

        [Test]
        public void ReconcileAddsUnknownAreasAsUndecided()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [] }");

            var result = platforms.Reconcile(new[] { "ra102", "ra103" });

            CollectionAssert.AreEqual(new[] { "ra102", "ra103" }, result.added);
            Assert.IsFalse(platforms.Find("ra102").IsDecided);
        }

        [Test]
        public void ReconcileNeverOverwritesAnExistingDecision()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra102"", ""platform"": ""desktopOnly"" } ] }";
            var platforms = RoomPlatforms.FromJson(json);

            var result = platforms.Reconcile(new[] { "ra102", "ra103" });

            Assert.AreEqual(RoomPlatforms.DesktopOnly, platforms.Find("ra102").platform);
            CollectionAssert.AreEqual(new[] { "ra103" }, result.added);
        }

        [Test]
        public void ReconcileKeepsAndReportsAreasThatNoLongerExist()
        {
            const string json = @"{ ""rooms"": [ { ""room"": ""ra999"", ""platform"": ""all"" } ] }";
            var platforms = RoomPlatforms.FromJson(json);

            var result = platforms.Reconcile(new[] { "ra102" });

            CollectionAssert.AreEqual(new[] { "ra999" }, result.orphans);
            Assert.IsNotNull(platforms.Find("ra999"),
                "dropping a decision because a container was briefly renamed is a one-way loss");
        }

        [Test]
        public void ReconcileTreatsNumberedSiblingsAsSeparateAreas()
        {
            var platforms = RoomPlatforms.FromJson(@"{ ""rooms"": [] }");

            platforms.Reconcile(new[] { "ra100_corridor_1", "ra100_corridor_2" });
            platforms.Find("ra100_corridor_1").platform = RoomPlatforms.All;

            Assert.AreEqual(RoomPlatforms.All, platforms.PlatformOf("ra100_corridor_1"));
            Assert.IsNull(platforms.PlatformOf("ra100_corridor_2"),
                "corridor 2 is a different place and must not inherit corridor 1's decision");
        }
    }
}
