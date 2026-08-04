using System.Collections.Generic;
using NUnit.Framework;

namespace FriWorld.ObjectRegistry.Tests
{
    public class ObjectTypeKeyTests
    {
        static List<string> Prefixes(params string[] p) => new List<string>(p);

        [Test]
        public void StripsPrefixAndTrailingInstanceNumber()
        {
            var key = ObjectTypeKey.Derive("ra000_cleaners_room_ceiling_1",
                                           Prefixes("ra000_cleaners_room"));
            Assert.AreEqual("ceiling", key);
        }

        [Test]
        public void StripsTheRoomsOwnLeadingInstanceNumber()
        {
            // Without this, door_frame fragments into 1_door_frame, 2_door_frame, ...
            var key = ObjectTypeKey.Derive("ra100_corridor_1_door_frame_2",
                                           Prefixes("ra100_corridor"));
            Assert.AreEqual("door_frame", key);
        }

        [Test]
        public void KeepsMultiWordTypesIntact()
        {
            var key = ObjectTypeKey.Derive("rb254_window_1_glass_1", Prefixes("rb254"));
            Assert.AreEqual("window_1_glass", key);
        }

        [Test]
        public void SkipsAPrefixThatWouldLeaveOnlyANumber()
        {
            // "lamp_2" must not become "2" just because a prefix "lamp" exists.
            var key = ObjectTypeKey.Derive("lamp_2", Prefixes("lamp"));
            Assert.AreEqual("lamp", key);
        }

        [Test]
        public void PrefersTheLongestMatchingPrefix()
        {
            var key = ObjectTypeKey.Derive("ra100_corridor_2_radiator_3",
                                           Prefixes("ra100", "ra100_corridor"));
            Assert.AreEqual("radiator", key);
        }

        [Test]
        public void LeavesTheNameAloneWhenNoPrefixMatches()
        {
            var key = ObjectTypeKey.Derive("rb308_nav_1", Prefixes("ra100"));
            Assert.AreEqual("rb308_nav", key);
        }

        [Test]
        public void IsCaseInsensitiveOnThePrefix()
        {
            var key = ObjectTypeKey.Derive("RA100_Corridor_wall_1", Prefixes("ra100_corridor"));
            Assert.AreEqual("wall", key);
        }

        [Test]
        public void HandlesNullAndEmptyNames()
        {
            Assert.AreEqual("", ObjectTypeKey.Derive(null, Prefixes("x")));
            Assert.AreEqual("", ObjectTypeKey.Derive("", Prefixes("x")));
        }
    }
}
