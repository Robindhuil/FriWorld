using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class TypeRegistryTests
    {
        [Test]
        public void NullFieldsSurviveTheJsonRoundTrip()
        {
            const string json = @"{ ""types"": [ { ""name"": ""sun_lamp"", ""collider"": null, ""layer"": null, ""occluder"": null } ] }";

            var registry = TypeRegistry.FromJson(json);
            var entry = registry.Find("sun_lamp");

            Assert.IsNotNull(entry);
            Assert.IsNull(entry.collider, "null must stay null, not become an empty string");
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void AFullyFilledEntryCountsAsDecided()
        {
            const string json = @"{ ""types"": [ { ""name"": ""wall"", ""collider"": ""mesh"", ""layer"": ""obstacle"", ""occluder"": ""auto"" } ] }";

            var entry = TypeRegistry.FromJson(json).Find("wall");

            Assert.IsTrue(entry.IsDecided);
            Assert.AreEqual("mesh", entry.collider);
        }

        [Test]
        public void ExplicitNoneIsDecidedAndDifferentFromNull()
        {
            const string json = @"{ ""types"": [ { ""name"": ""Cedulka"", ""collider"": ""none"", ""layer"": ""keep"", ""occluder"": ""no"" } ] }";

            var entry = TypeRegistry.FromJson(json).Find("Cedulka");

            Assert.IsTrue(entry.IsDecided, "'I decided nothing' is not the same as 'I have not decided'");
            Assert.AreEqual("none", entry.collider);
        }

        [Test]
        public void LookupIsExactNotSubstring()
        {
            const string json = @"{ ""types"": [ { ""name"": ""lamp"", ""collider"": ""none"", ""layer"": ""noObstacle"", ""occluder"": ""no"" } ] }";

            var registry = TypeRegistry.FromJson(json);

            Assert.IsNotNull(registry.Find("lamp"));
            Assert.IsNull(registry.Find("sun_lamp"), "substring matching is the bug this replaces");
        }

        [Test]
        public void SeededEntriesAreUndecided()
        {
            var registry = TypeRegistry.FromJson(@"{ ""types"": [] }");

            registry.Seed("new_thing");

            var entry = registry.Find("new_thing");
            Assert.IsNotNull(entry);
            Assert.IsNull(entry.collider);
            Assert.IsFalse(entry.IsDecided);
        }

        [Test]
        public void SeedingDoesNotOverwriteAnExistingEntry()
        {
            const string json = @"{ ""types"": [ { ""name"": ""wall"", ""collider"": ""mesh"", ""layer"": ""obstacle"", ""occluder"": ""auto"" } ] }";
            var registry = TypeRegistry.FromJson(json);

            registry.Seed("wall");

            Assert.AreEqual("mesh", registry.Find("wall").collider);
        }

        [Test]
        public void PrefixesRoundTrip()
        {
            const string json = @"{ ""prefixes"": [ ""ra100_corridor"", ""rb254"" ] }";

            var prefixes = TypeRegistry.PrefixesFromJson(json);

            Assert.AreEqual(2, prefixes.Count);
            Assert.Contains("ra100_corridor", prefixes);
        }
    }
}
