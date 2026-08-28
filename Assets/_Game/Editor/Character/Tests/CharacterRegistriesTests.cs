using System.IO;
using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class CharacterRegistriesTests
    {
        string temp;

        [SetUp]
        public void SetUp() => temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(temp)) File.Delete(temp);
        }

        void Write(string json) => File.WriteAllText(temp, json);

        [Test]
        public void ReadsAColourClassWithAShade()
        {
            Write(@"{ ""colorClasses"": [
                { ""name"": ""torso"", ""mainColors"": 2,
                  ""shadeValue"": 0.62, ""shadeSaturation"": 1.12 } ],
                ""slotClasses"": [ ""torso"" ] }");

            var registry = CharacterRegistries.LoadFrom<ClassRegistry>(temp);

            Assert.AreEqual(1, registry.colorClasses.Count);
            Assert.AreEqual("torso", registry.colorClasses[0].name);
            Assert.AreEqual(2, registry.colorClasses[0].mainColors);
            Assert.AreEqual(0.62f, registry.colorClasses[0].shadeValue.Value, 0.0001f);
            Assert.AreEqual(1, registry.slotClasses.Count);
        }

        [Test]
        public void AShadelessClassKeepsNullNotZero()
        {
            // Zero would read as "multiply value by 0", i.e. black. Null means "no shade".
            Write(@"{ ""colorClasses"": [
                { ""name"": ""eye"", ""mainColors"": 1,
                  ""shadeValue"": null, ""shadeSaturation"": null } ],
                ""slotClasses"": [] }");

            var registry = CharacterRegistries.LoadFrom<ClassRegistry>(temp);

            Assert.IsFalse(registry.colorClasses[0].shadeValue.HasValue);
        }

        [Test]
        public void ReadsAColorway()
        {
            Write(@"{ ""colorways"": [
                { ""colorClass"": ""torso"", ""slot"": 2, ""id"": ""navy"",
                  ""displayName"": ""Tmavomodrá"", ""color"": ""#243B6B"" } ] }");

            var registry = CharacterRegistries.LoadFrom<ColorwayRegistry>(temp);

            Assert.AreEqual("navy", registry.colorways[0].id);
            Assert.AreEqual(2, registry.colorways[0].slot);
            Assert.AreEqual("#243B6B", registry.colorways[0].color);
        }

        [Test]
        public void ReadsAPresetAndMapsTheObjectKeyword()
        {
            // "object" is a C# keyword, so the field is objectName and JsonProperty bridges it.
            Write(@"{ ""presets"": [
                { ""slotClass"": ""torso"", ""object"": ""shirt_1"",
                  ""displayName"": ""Košeľa"", ""gender"": ""any"",
                  ""hides"": [ ""chest"", ""abdomen"" ],
                  ""tags"": [ ""formal"" ], ""conflicts"": [], ""weight"": 3 } ] }");

            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(temp);

            Assert.AreEqual("shirt_1", registry.presets[0].objectName);
            Assert.AreEqual(2, registry.presets[0].hides.Count);
            Assert.AreEqual(3, registry.presets[0].weight);
        }

        [Test]
        public void AMissingFileGivesAnEmptyRegistryNotAnException()
        {
            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(
                Path.Combine(Path.GetTempPath(), "no-such-file.json"));

            Assert.IsNotNull(registry);
            Assert.AreEqual(0, registry.presets.Count);
        }

        [Test]
        public void AnAbsentSlotIsTheMainColour()
        {
            Write(@"{ ""colorways"": [
                { ""colorClass"": ""legs"", ""id"": ""denim"", ""color"": ""#3A4A63"" } ] }");

            Assert.AreEqual(1, CharacterRegistries.LoadFrom<ColorwayRegistry>(temp).colorways[0].slot);
        }

        [Test]
        public void AnAbsentWeightDefaultsToOne()
        {
            Write(@"{ ""presets"": [
                { ""slotClass"": ""torso"", ""object"": ""t-shirt_1"" } ] }");

            var registry = CharacterRegistries.LoadFrom<PresetRegistry>(temp);

            Assert.AreEqual(1, registry.presets[0].weight);
        }
    }
}
