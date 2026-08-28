using System.Collections.Generic;
using FriWorld.Character;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class BodySectionNamesTests
    {
        [Test]
        public void ParsesEverySectionKey()
        {
            string[] keys =
            {
                "neck", "chest", "abdomen", "hips",
                "upperarm_L", "upperarm_R", "forearm_L", "forearm_R",
                "hand_L", "hand_R", "thigh_L", "thigh_R",
                "calf_L", "calf_R", "foot_L", "foot_R",
            };

            foreach (string key in keys)
            {
                Assert.IsTrue(BodySectionNames.TryParseKey(key, out var section), key);
                Assert.AreNotEqual(BodySection.None, section, key);
            }
        }

        [Test]
        public void StripsTheBodyPrefixOffAnObjectName()
        {
            Assert.IsTrue(BodySectionNames.TryParseObject("male_body_upperarm_L", out var male));
            Assert.IsTrue(BodySectionNames.TryParseObject("female_body_upperarm_L", out var female));

            // Both bodies land on the same section, which is the whole point: hides masks in
            // CharacterPresets.json are written once and apply to either body.
            Assert.AreEqual(BodySection.UpperArmL, male);
            Assert.AreEqual(male, female);
        }

        [Test]
        public void AnObjectNameWithoutTheBodyPrefixIsReadAsAKey()
        {
            Assert.IsTrue(BodySectionNames.TryParseObject("chest", out var section));
            Assert.AreEqual(BodySection.Chest, section);
        }

        [Test]
        public void OnlyTheLastBodyMarkerCounts()
        {
            // A container could conceivably be called "body" too; the key is whatever follows
            // the final _body_, never the first one.
            Assert.IsTrue(BodySectionNames.TryParseObject("body_male_body_chest", out var section));
            Assert.AreEqual(BodySection.Chest, section);
        }

        [Test]
        public void TheHeadIsNotASection()
        {
            // male_body_head exists in the prefab today as a stand-in until head presets are
            // modelled. It is a slot class, so it must never resolve to a hideable section.
            Assert.IsFalse(BodySectionNames.TryParseObject("male_body_head", out _));
        }

        [Test]
        public void RejectsAnUnknownKey()
        {
            Assert.IsFalse(BodySectionNames.TryParseKey("torso", out _));
            Assert.IsFalse(BodySectionNames.TryParseKey("", out _));
            Assert.IsFalse(BodySectionNames.TryParseKey(null, out _));
        }

        [Test]
        public void TheSideSuffixIsCaseSensitive()
        {
            // Blender writes _L and _R. Accepting _l would let two objects claim one section
            // and the second would silently win.
            Assert.IsFalse(BodySectionNames.TryParseKey("upperarm_l", out _));
            Assert.IsFalse(BodySectionNames.TryParseObject("male_body_upperarm_l", out _));
        }

        [Test]
        public void SixteenSectionsWithDistinctBits()
        {
            var seen = new List<BodySection>();
            int count = 0;

            foreach (var entry in BodySectionNames.All)
            {
                Assert.IsFalse(seen.Contains(entry.section), entry.key);
                seen.Add(entry.section);
                count++;
            }

            Assert.AreEqual(16, count);
        }

        [Test]
        public void KeyOfRoundTrips()
        {
            foreach (var entry in BodySectionNames.All)
                Assert.AreEqual(entry.key, BodySectionNames.KeyOf(entry.section));
        }
    }
}
