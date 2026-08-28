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

            // torso carries two colours, legs one: three colour slots in all.
            catalog.colorSlotClass = new[] { 0, 0, 1 };
            catalog.colorSlotKey = new[] { 1, 2, 1 };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorSlot = 0, id = "navy" },
                new ColorwayEntry { colorSlot = 0, id = "rust" },
                new ColorwayEntry { colorSlot = 1, id = "cream" },
                new ColorwayEntry { colorSlot = 2, id = "denim" },
            };
            catalog.colorwayStart = new[] { 0, 2, 3, 4 };

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
        public void CountsAndIndexesColorwaysPerColourSlot()
        {
            var catalog = Build();
            Assert.AreEqual(2, catalog.ColorwayCount(0));
            Assert.AreEqual(1, catalog.ColorwayCount(1));
            Assert.AreEqual(1, catalog.ColorwayCount(2));
            Assert.AreEqual("denim", catalog.Colorway(2, 0).id);
        }

        [Test]
        public void TheTwoTorsoSlotsHaveSeparatePalettes()
        {
            // The whole point of slots over classes: a garment's secondary colour is not the
            // main one, and does not have to come from the same list.
            var catalog = Build();

            int main = catalog.ColorSlotIndex(0, 1);
            int secondary = catalog.ColorSlotIndex(0, 2);

            Assert.AreNotEqual(main, secondary);
            Assert.AreEqual("navy", catalog.Colorway(main, 0).id);
            Assert.AreEqual("cream", catalog.Colorway(secondary, 0).id);
        }

        [Test]
        public void AnUndeclaredColourSlotIsMinusOne()
        {
            Assert.AreEqual(-1, Build().ColorSlotIndex(1, 2));
        }

        [Test]
        public void NamesAColourSlotForReports()
        {
            Assert.AreEqual("torso 2", Build().ColorSlotName(1));
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
