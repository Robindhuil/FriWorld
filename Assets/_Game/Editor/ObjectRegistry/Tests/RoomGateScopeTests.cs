using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.ObjectRegistry.Tests
{
    /// <summary>
    /// Covers the rule that decides what counts as an area, and the nesting test the appliers
    /// rely on to refuse gating a container of areas. Both are pure enough to exercise on a
    /// throwaway hierarchy — no prefab, no scene.
    /// </summary>
    public class RoomGateScopeTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            root = null;
        }

        /// <summary>Builds "a/b/c" style paths under a fresh root and returns it.</summary>
        static GameObject Tree(params string[] paths)
        {
            var made = new GameObject("root");
            foreach (var path in paths)
            {
                Transform parent = made.transform;
                foreach (var name in path.Split('/'))
                {
                    Transform existing = parent.Find(name);
                    if (existing == null)
                    {
                        var go = new GameObject(name);
                        go.transform.SetParent(parent);
                        existing = go.transform;
                    }
                    parent = existing;
                }
            }
            return made;
        }

        static List<string> Prefixes(params string[] p) => new List<string>(p);

        static List<string> NamesOf(List<AreaMatch> matches)
        {
            var names = new List<string>();
            foreach (var m in matches) names.Add(m.area);
            return names;
        }

        static AreaMatch Named(List<AreaMatch> matches, string area)
        {
            foreach (var m in matches) if (m.area == area) return m;
            Assert.Fail("no area named " + area);
            return default;
        }

        [Test]
        public void FindsApprovedContainersAtAnyDepth()
        {
            // Objects/rc holds rooms directly; Objects/ra puts a floor level in between.
            root = Tree("Objects/rc/rc000_buffet/lamp",
                        "Objects/ra/ra0/ra001/chair");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("rc000_buffet", "ra001"), null);

            CollectionAssert.AreEquivalent(new[] { "rc000_buffet", "ra001" }, NamesOf(matches));
        }

        [Test]
        public void NumberedSiblingsAreSeparateAreas()
        {
            root = Tree("Objects/ra100_corridor_1/lamp", "Objects/ra100_corridor_2/lamp");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra100_corridor_1", "ra100_corridor_2"), null);

            Assert.AreEqual(2, matches.Count,
                "corridor 1 and corridor 2 are different places and decide for themselves");
        }

        [Test]
        public void ContainersNobodyApprovedAreNotAreas()
        {
            root = Tree("Objects/ra001/ra001_lamp/bulb");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra001"), null);

            CollectionAssert.AreEqual(new[] { "ra001" }, NamesOf(matches),
                "a lamp group inside a room is not a room");
        }

        [Test]
        public void ALeafIsNeverAnArea()
        {
            // ra001 is approved, but here it holds nothing — an area is a container.
            root = Tree("Objects/ra001");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra001"), null);

            CollectionAssert.IsEmpty(matches);
        }

        [Test]
        public void MatchingIgnoresCaseAndSurroundingWhitespace()
        {
            root = Tree("Objects/RA100_Corridor_2/lamp");
            root.transform.Find("Objects").GetChild(0).name = " ra100_corridor_2 ";

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra100_corridor_2"), null);

            Assert.AreEqual(1, matches.Count,
                "a stray space out of Blender must not silently skip a whole room");
            Assert.AreEqual("ra100_corridor_2", matches[0].area, "the key is stored trimmed");
        }

        [Test]
        public void CarriesThePlatformDecision()
        {
            root = Tree("Objects/ra001/chair", "Objects/ra002/chair");
            var platforms = RoomPlatforms.FromJson(
                @"{ ""rooms"": [ { ""room"": ""ra001"", ""platform"": ""desktopOnly"" } ] }");

            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra001", "ra002"), platforms);

            Assert.AreEqual(RoomPlatforms.DesktopOnly, Named(matches, "ra001").platform);
            Assert.IsNull(Named(matches, "ra002").platform, "undecided must stay null");
        }

        [Test]
        public void ContainsAnotherAreaSpotsAnAreaInsideAnArea()
        {
            root = Tree("rb_basement/rb_basement_room_1/wall");
            var matches = RoomGateScope.Match(root.transform,
                                              Prefixes("rb_basement", "rb_basement_room_1"), null);

            Assert.IsTrue(RoomGateScope.ContainsAnotherArea(Named(matches, "rb_basement"), matches),
                "gating rb_basement would strip the inner rooms and void their own decisions");
            Assert.IsFalse(
                RoomGateScope.ContainsAnotherArea(Named(matches, "rb_basement_room_1"), matches));
        }

        [Test]
        public void ContainsAnotherAreaIgnoresSiblings()
        {
            root = Tree("Objects/ra001/chair", "Objects/ra002/chair");
            var matches = RoomGateScope.Match(root.transform.Find("Objects"),
                                              Prefixes("ra001", "ra002"), null);

            Assert.IsFalse(RoomGateScope.ContainsAnotherArea(Named(matches, "ra001"), matches),
                "an area next to another area is not nesting");
        }

        [Test]
        public void AreaNamesDeduplicatesAcrossBranches()
        {
            // The same room name exists in both trees — Objects holds its furniture,
            // fri_building holds its walls. RoomPlatforms wants one row for it.
            root = Tree("Objects/ra001/chair", "fri_building/ra001/wall");

            var names = RoomGateScope.AreaNames(root, Prefixes("ra001"));

            CollectionAssert.AreEqual(new[] { "ra001" }, names);
        }

        [Test]
        public void AreaNamesIsSorted()
        {
            root = Tree("Objects/ra003/chair", "Objects/ra001/chair", "Objects/ra002/chair");

            var names = RoomGateScope.AreaNames(root, Prefixes("ra001", "ra002", "ra003"));

            CollectionAssert.AreEqual(new[] { "ra001", "ra002", "ra003" }, names);
        }

        [Test]
        public void MatchKeepsEveryOccurrenceEvenWhenAreaNamesDedupes()
        {
            // AreaNames answers "which rows does the file need"; Match answers "which objects do
            // I write to". The second must not lose the twin in the other branch.
            root = Tree("Objects/ra001/chair", "fri_building/ra001/wall");

            var all = RoomGateScope.Match(root.transform, Prefixes("ra001"), null);

            Assert.AreEqual(2, all.Count);
            Assert.AreNotSame(all[0].transform, all[1].transform);
        }
    }
}
