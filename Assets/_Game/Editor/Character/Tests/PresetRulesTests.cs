using FriWorld.Character;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class PresetRulesTests
    {
        static PresetEntry Preset(GenderGate gate, int tags, int conflicts) =>
            new PresetEntry { gender = gate, tagMask = tags, conflictMask = conflicts };

        [Test]
        public void AnyPassesForBothGenders()
        {
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Any, Gender.Male));
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Any, Gender.Female));
        }

        [Test]
        public void AGatedPresetOnlyPassesForItsGender()
        {
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Male, Gender.Male));
            Assert.IsFalse(PresetRules.GenderAllows(GenderGate.Male, Gender.Female));
            Assert.IsTrue(PresetRules.GenderAllows(GenderGate.Female, Gender.Female));
            Assert.IsFalse(PresetRules.GenderAllows(GenderGate.Female, Gender.Male));
        }

        [Test]
        public void APresetIsRejectedWhenSomethingAlreadyChosenForbidsItsTag()
        {
            // Already wearing a jacket that forbids "bulky_torso"; a backpack tagged
            // "bulky_torso" must not pass.
            var backpack = Preset(GenderGate.Any, tags: 1, conflicts: 0);

            Assert.IsFalse(PresetRules.IsAllowed(backpack, Gender.Male,
                takenTags: 0, forbiddenTags: 1));
        }

        [Test]
        public void APresetIsRejectedWhenItForbidsSomethingAlreadyChosen()
        {
            // The same rule seen from the other side: order of picking must not change the
            // outcome, which is why both masks are tested on every candidate.
            var jacket = Preset(GenderGate.Any, tags: 0, conflicts: 1);

            Assert.IsFalse(PresetRules.IsAllowed(jacket, Gender.Male,
                takenTags: 1, forbiddenTags: 0));
        }

        [Test]
        public void UnrelatedTagsDoNotCollide()
        {
            var preset = Preset(GenderGate.Any, tags: 0x2, conflicts: 0x8);

            Assert.IsTrue(PresetRules.IsAllowed(preset, Gender.Male,
                takenTags: 0x4, forbiddenTags: 0x1));
        }

        [Test]
        public void PickWeightedRespectsTheBoundaries()
        {
            var weights = new[] { 1, 3 };   // total 4: [0, 1) then [1, 4)

            Assert.AreEqual(0, PresetRules.PickWeighted(weights, 0.0));
            Assert.AreEqual(0, PresetRules.PickWeighted(weights, 0.2499));
            Assert.AreEqual(1, PresetRules.PickWeighted(weights, 0.25));
            Assert.AreEqual(1, PresetRules.PickWeighted(weights, 0.9999));
        }

        [Test]
        public void PickWeightedTreatsAllZeroWeightsAsTheFirstEntry()
        {
            Assert.AreEqual(0, PresetRules.PickWeighted(new[] { 0, 0 }, 0.7));
        }

        [Test]
        public void PickWeightedReturnsMinusOneOnAnEmptyList()
        {
            Assert.AreEqual(-1, PresetRules.PickWeighted(new int[0], 0.5));
        }
    }
}
