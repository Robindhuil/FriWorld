using FriWorld.Character.Editor;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class MaterialSlotKeyTests
    {
        [Test]
        public void ParsesABaseColourSlot()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_1", out var slot));
            Assert.AreEqual("torso", slot.ColorClass);
            Assert.AreEqual(1, slot.BaseKey);
            Assert.AreEqual(0, slot.ShadeLevel);
        }

        [Test]
        public void ParsesASecondaryColourSlot()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_2", out var slot));
            Assert.AreEqual(2, slot.BaseKey);
            Assert.AreEqual(0, slot.ShadeLevel);
        }

        [Test]
        public void ParsesTheDarkerShadeOfTheFirstColour()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_11", out var slot));
            Assert.AreEqual("torso", slot.ColorClass);
            Assert.AreEqual(1, slot.BaseKey);
            Assert.AreEqual(1, slot.ShadeLevel);
        }

        [Test]
        public void ParsesTheDarkerShadeOfTheSecondColour()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_21", out var slot));
            Assert.AreEqual(2, slot.BaseKey);
            Assert.AreEqual(1, slot.ShadeLevel);
        }

        [Test]
        public void ParsingIsSyntaxOnly_ClassMembershipIsSomebodyElsesJob()
        {
            // char_leather_1 is a perfectly well-formed name. That "leather" is not a colour
            // class is decided by the catalog, not here — mixing the two would make the parser
            // the place where art naming rules live.
            Assert.IsTrue(MaterialSlotKey.TryParse("char_leather_1", out var slot));
            Assert.AreEqual("leather", slot.ColorClass);
        }

        [Test]
        public void ParsesTheOtherNonClassKeywordsOnTheRealCharacter()
        {
            // Both of these sit on pants_3 in character_male and must survive untouched.
            Assert.IsTrue(MaterialSlotKey.TryParse("char_metal_1", out var metal));
            Assert.AreEqual("metal", metal.ColorClass);
        }

        [Test]
        public void KeepsMultiWordClassNamesIntact()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_upper_body_1", out var slot));
            Assert.AreEqual("upper_body", slot.ColorClass);
        }

        [Test]
        public void RejectsAMissingSuffix()
        {
            Assert.IsFalse(MaterialSlotKey.TryParse("char_skin", out _));
        }

        [Test]
        public void RejectsATenthBaseColour()
        {
            // "10" would be indistinguishable from "the darker shade of colour 1" at shade
            // level 0. Nine base colours per class is the documented ceiling.
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_10", out _));
        }

        [Test]
        public void RejectsNamesOutsideTheScheme()
        {
            Assert.IsFalse(MaterialSlotKey.TryParse("mt_floor_1", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_1x", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_111", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_torso_0", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("char_1", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse("", out _));
            Assert.IsFalse(MaterialSlotKey.TryParse(null, out _));
        }

        [Test]
        public void ParsesAShadeLevelAboveOne_TheCatalogRejectsItLater()
        {
            Assert.IsTrue(MaterialSlotKey.TryParse("char_torso_12", out var slot));
            Assert.AreEqual(2, slot.ShadeLevel);
        }
    }
}
