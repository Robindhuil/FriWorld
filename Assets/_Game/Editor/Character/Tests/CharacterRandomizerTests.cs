using FriWorld.Character;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class CharacterRandomizerTests
    {
        /// One slot class "torso" with three presets, one of them female-only and one pair that
        /// conflicts through the tag "bulky_torso"; one colour class with two colorways.
        static CharacterCatalog Build()
        {
            var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.slotClasses = new[] { "torso", "legs" };
            catalog.colorClasses = new[] { "torso" };
            catalog.tags = new[] { "bulky_torso" };

            catalog.colorways = new[]
            {
                new ColorwayEntry { colorClass = 0, id = "navy" },
                new ColorwayEntry { colorClass = 0, id = "rust" },
            };
            catalog.colorwayStart = new[] { 0, 2 };

            var presets = new[]
            {
                new PresetEntry { slotClass = 0, objectName = "shirt_1",
                                  gender = GenderGate.Any, tagMask = 1, weight = 1 },
                new PresetEntry { slotClass = 0, objectName = "t-shirt_1",
                                  gender = GenderGate.Any, weight = 1 },
                new PresetEntry { slotClass = 0, objectName = "blouse_1",
                                  gender = GenderGate.Female, weight = 1 },
                new PresetEntry { slotClass = 1, objectName = "backpack_strap_1",
                                  gender = GenderGate.Any, conflictMask = 1, weight = 1 },
                new PresetEntry { slotClass = 1, objectName = "pants_1",
                                  gender = GenderGate.Any, weight = 1 },
            };

            catalog.male = new GenderBundle { presets = presets, presetStart = new[] { 0, 3, 5 } };
            catalog.female = new GenderBundle { presets = presets, presetStart = new[] { 0, 3, 5 } };
            return catalog;
        }

        [Test]
        public void TheSameSeedGivesTheSameLook()
        {
            var catalog = Build();

            var a = CharacterRandomizer.Roll(1234, catalog, Gender.Male);
            var b = CharacterRandomizer.Roll(1234, catalog, Gender.Male);

            Assert.AreEqual(a.gender, b.gender);
            CollectionAssert.AreEqual(a.preset, b.preset);
            CollectionAssert.AreEqual(a.colorway, b.colorway);
        }

        [Test]
        public void DifferentSeedsEventuallyDiffer()
        {
            var catalog = Build();
            bool sawADifference = false;

            var first = CharacterRandomizer.Roll(0, catalog, Gender.Male);
            for (int seed = 1; seed < 50 && !sawADifference; seed++)
            {
                var other = CharacterRandomizer.Roll(seed, catalog, Gender.Male);
                for (int i = 0; i < first.preset.Length; i++)
                    if (first.preset[i] != other.preset[i]) sawADifference = true;
            }

            Assert.IsTrue(sawADifference, "50 seeds produced one single look");
        }

        [Test]
        public void AFemaleOnlyPresetNeverLandsOnAMaleBody()
        {
            var catalog = Build();

            for (int seed = 0; seed < 200; seed++)
            {
                var look = CharacterRandomizer.Roll(seed, catalog, Gender.Male);
                Assert.AreNotEqual("blouse_1",
                    catalog.Preset(Gender.Male, 0, look.preset[0]).objectName);
            }
        }

        [Test]
        public void ConflictingPresetsNeverAppearTogether()
        {
            var catalog = Build();

            for (int seed = 0; seed < 200; seed++)
            {
                var look = CharacterRandomizer.Roll(seed, catalog, Gender.Male);

                bool shirt = catalog.Preset(Gender.Male, 0, look.preset[0]).objectName == "shirt_1";
                bool strap = catalog.Preset(Gender.Male, 1, look.preset[1]).objectName
                             == "backpack_strap_1";

                Assert.IsFalse(shirt && strap, $"seed {seed} put the strap over the shirt");
            }
        }

        [Test]
        public void EveryColourClassGetsAColorway()
        {
            var catalog = Build();
            var look = CharacterRandomizer.Roll(7, catalog, Gender.Male);

            Assert.AreEqual(1, look.colorway.Length);
            Assert.Less(look.colorway[0], catalog.ColorwayCount(0));
        }

        [Test]
        public void ASlotClassWithNoLegalPresetIsLeftEmpty()
        {
            var catalog = Build();
            // Forbid everything in slot class 0 by giving the class a single gated preset the
            // rolled gender cannot wear.
            catalog.male = new GenderBundle
            {
                presets = new[]
                {
                    new PresetEntry { slotClass = 0, objectName = "blouse_1",
                                      gender = GenderGate.Female, weight = 1 },
                    new PresetEntry { slotClass = 1, objectName = "pants_1",
                                      gender = GenderGate.Any, weight = 1 },
                },
                presetStart = new[] { 0, 1, 2 },
            };

            var look = CharacterRandomizer.Roll(3, catalog, Gender.Male);

            Assert.AreEqual(CharacterAppearance.None, look.preset[0]);
            Assert.AreEqual(0, look.preset[1]);
        }
    }
}
