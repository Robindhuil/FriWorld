using System.IO;
using FriWorld.Navigator.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Navigator.Tests
{
    public class NavigatorBuildTests
    {
        private string output;

        [SetUp]
        public void CreateOutput()
        {
            output = Path.Combine(Path.GetTempPath(), "NavigatorBuildTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(output, "Build"));
            File.WriteAllText(Path.Combine(output, "Build", "Web.data"), "data");
            Directory.CreateDirectory(Path.Combine(output, "StreamingAssets", "videos"));
            File.WriteAllText(Path.Combine(output, "StreamingAssets", "videos", "historia.mp4"), "video");
            Directory.CreateDirectory(Path.Combine(output, "FriWorld_BurstDebugInformation_DoNotShip"));
            File.WriteAllText(Path.Combine(output, "FriWorld_BurstDebugInformation_DoNotShip", "lib.txt"), "symbols");
        }

        [TearDown]
        public void DeleteOutput()
        {
            if (Directory.Exists(output))
                Directory.Delete(output, true);
        }

        [Test]
        public void DropsTheVideosAndBurstSymbolsButKeepsTheBuild()
        {
            NavigatorBuild.RemoveUnused(output);

            Assert.IsFalse(Directory.Exists(Path.Combine(output, "StreamingAssets", "videos")));
            Assert.IsFalse(Directory.Exists(Path.Combine(output, "FriWorld_BurstDebugInformation_DoNotShip")));
            Assert.IsTrue(File.Exists(Path.Combine(output, "Build", "Web.data")));
        }

        [Test]
        public void KeepsOtherStreamingAssets()
        {
            File.WriteAllText(Path.Combine(output, "StreamingAssets", "Rooms.json"), "{}");

            NavigatorBuild.RemoveUnused(output);

            Assert.IsTrue(File.Exists(Path.Combine(output, "StreamingAssets", "Rooms.json")));
        }

        [Test]
        public void RemovesStreamingAssetsLeftEmpty()
        {
            NavigatorBuild.RemoveUnused(output);

            Assert.IsFalse(Directory.Exists(Path.Combine(output, "StreamingAssets")));
        }

        [Test]
        public void ListsTheRoomCodesItKnowsBesideTheBuild()
        {
            var anchors = ScriptableObject.CreateInstance<RoomAnchors>();
            anchors.anchors.Add(new RoomAnchors.Anchor { code = "RB101" });
            anchors.anchors.Add(new RoomAnchors.Anchor { code = "RA101" });

            NavigatorBuild.WriteRooms(output, anchors);

            Assert.AreEqual("[\"RA101\",\"RB101\"]", File.ReadAllText(Path.Combine(output, NavigatorBuild.RoomsFile)));
            Object.DestroyImmediate(anchors);
        }
    }
}
