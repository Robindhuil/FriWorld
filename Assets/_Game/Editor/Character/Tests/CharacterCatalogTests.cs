using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterCatalogTests
    {
        static CharacterCatalog Build()
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso", "legs" };
            catalog.colorClasses = new[] { "torso", "legs" };
            catalog.tags = new[] { "casual" };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorClass = 0, id = "navy" },
                new ColorwayEntry { colorClass = 0, id = "rust" },
                new ColorwayEntry { colorClass = 1, id = "denim" },
            };
            catalog.colorwayStart = new[] { 0, 2, 3 };

            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry { slotClass = 0, objectName = "shirt_1" },
                    new PresetEntry { slotClass = 0, objectName = "t-shirt_1" },
                    new PresetEntry { slotClass = 1, objectName = "pants_1" },
                },
                presetStart = new[] { 0, 2, 3 },
                slotMaps = new[]
                {
                    new RendererSlotMap { objectName = "male_body_chest" },
                },
            };
            catalog.female = new GenderBundle();

            return catalog;
        }

        [Test]
        public void CountsPresetsPerSlotClass()
        {
            var catalog = Build();
            Assert.AreEqual(2, catalog.PresetCount(Gender.Male, 0));
            Assert.AreEqual(1, catalog.PresetCount(Gender.Male, 1));
        }

        [Test]
        public void IndexesPresetsWithinTheirSlotClass()
        {
            var catalog = Build();
            Assert.AreEqual("t-shirt_1", catalog.Preset(Gender.Male, 0, 1).objectName);
            Assert.AreEqual("pants_1", catalog.Preset(Gender.Male, 1, 0).objectName);
        }

        [Test]
        public void CountsAndIndexesColorwaysPerColourClass()
        {
            var catalog = Build();
            Assert.AreEqual(2, catalog.ColorwayCount(0));
            Assert.AreEqual(1, catalog.ColorwayCount(1));
            Assert.AreEqual("denim", catalog.Colorway(1, 0).id);
        }

        [Test]
        public void FindsASlotMapByObjectName()
        {
            var catalog = Build();
            Assert.IsNotNull(catalog.SlotMap(Gender.Male, "male_body_chest"));
            Assert.IsNull(catalog.SlotMap(Gender.Male, "no_such_object"));
        }

        [Test]
        public void AnEmptyBundleAnswersZeroInsteadOfThrowing()
        {
            var catalog = Build();
            Assert.AreEqual(0, catalog.PresetCount(Gender.Female, 0));
        }
    }
}
