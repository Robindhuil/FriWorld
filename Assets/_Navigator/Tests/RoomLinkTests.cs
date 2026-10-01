using NUnit.Framework;

namespace FriWorld.Navigator.Tests
{
    public class RoomLinkTests
    {
        [Test]
        public void ReadsTheRoomFromTheQuery()
        {
            Assert.IsTrue(RoomLink.TryGetCode("https://hub.example/navigator/index.html?room=RA101", out string code));
            Assert.AreEqual("RA101", code);
        }

        [Test]
        public void FindsTheRoomAmongOtherParametersAndIgnoresTheFragment()
        {
            Assert.IsTrue(RoomLink.TryGetCode("http://localhost:8000/?lang=sk&room=rb308#top", out string code));
            Assert.AreEqual("rb308", code);
        }

        [Test]
        public void DecodesAnEscapedRoom()
        {
            Assert.IsTrue(RoomLink.TryGetCode("http://localhost:8000/?room=ra%20101", out string escaped));
            Assert.AreEqual("ra 101", escaped);
            Assert.IsTrue(RoomLink.TryGetCode("http://localhost:8000/?room=ra+101", out string plus));
            Assert.AreEqual("ra 101", plus);
        }

        [Test]
        public void ParameterNameIgnoresCase()
        {
            Assert.IsTrue(RoomLink.TryGetCode("http://localhost:8000/?Room=RA101", out string code));
            Assert.AreEqual("RA101", code);
        }

        [Test]
        public void NoRoomWithoutTheParameterOrWithAnEmptyOne()
        {
            Assert.IsFalse(RoomLink.TryGetCode("https://hub.example/navigator/index.html", out _));
            Assert.IsFalse(RoomLink.TryGetCode("https://hub.example/navigator/index.html?room=", out _));
            Assert.IsFalse(RoomLink.TryGetCode("https://hub.example/?lang=sk", out _));
            Assert.IsFalse(RoomLink.TryGetCode("", out _));
            Assert.IsFalse(RoomLink.TryGetCode(null, out _));
        }
    }
}
