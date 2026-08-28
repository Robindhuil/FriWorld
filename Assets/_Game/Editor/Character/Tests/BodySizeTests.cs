using FriWorld.Character;
using NUnit.Framework;

namespace FriWorld.Character.Tests
{
    public class BodySizeTests
    {
        static BodySize Male() => new BodySize
        {
            modelHeight = 1.803f,
            mean = 1.80f,
            deviation = 0.07f,
            min = 1.70f,
            max = 1.90f,
        };

        [Test]
        public void TheBandEndsDecodeToMinAndMax()
        {
            var size = Male();
            Assert.AreEqual(1.70f, size.Metres(0), 0.0001f);
            Assert.AreEqual(1.90f, size.Metres(255), 0.0001f);
        }

        [Test]
        public void TheMiddleOfTheByteIsTheMiddleOfTheBand()
        {
            Assert.AreEqual(1.80f, Male().Metres(128), 0.002f);
        }

        [Test]
        public void QuantisingRoundTripsToWithinAMillimetre()
        {
            var size = Male();
            foreach (float metres in new[] { 1.70f, 1.735f, 1.80f, 1.862f, 1.90f })
                Assert.AreEqual(metres, size.Metres(size.Quantise(metres)), 0.001f, metres.ToString());
        }

        [Test]
        public void ScaleIsHeightOverTheModelsOwnHeight()
        {
            var size = Male();
            Assert.AreEqual(1.70f / 1.803f, size.ScaleFor(0), 0.0005f);
            Assert.AreEqual(1.90f / 1.803f, size.ScaleFor(255), 0.0005f);
        }

        [Test]
        public void AModelStandingAtTheMeanScalesAboutOneInTheMiddle()
        {
            // The reason modelHeight should sit near the mean: the scale then stays close to 1,
            // and a head only reads wrong once the scale strays.
            Assert.AreEqual(1f, Male().ScaleFor(128), 0.005f);
        }

        [Test]
        public void ABodyWithNoDeclaredSizeScalesOne()
        {
            var none = new BodySize();
            Assert.AreEqual(1f, none.ScaleFor(0), 0.0001f);
            Assert.AreEqual(1f, none.ScaleFor(255), 0.0001f);
        }

        [Test]
        public void ZeroDeviationAlwaysRollsTheMean()
        {
            var fixedHeight = Male();
            fixedHeight.deviation = 0f;

            byte first = fixedHeight.Roll(new System.Random(1));
            byte second = fixedHeight.Roll(new System.Random(2));

            Assert.AreEqual(first, second);
            Assert.AreEqual(1.80f, fixedHeight.Metres(first), 0.002f);
        }

        [Test]
        public void RollsStayInsideTheBand()
        {
            var size = Male();
            for (int seed = 0; seed < 500; seed++)
            {
                float metres = size.Metres(size.Roll(new System.Random(seed)));
                Assert.GreaterOrEqual(metres, size.min - 0.001f, "seed " + seed);
                Assert.LessOrEqual(metres, size.max + 0.001f, "seed " + seed);
            }
        }

        [Test]
        public void RollsDoNotPileUpOnTheBandEnds()
        {
            // Clamping instead of redrawing put about 15% of a crowd on exactly the shortest and
            // exactly the tallest value, which is a pile nobody has in real life.
            var size = Male();
            int atEnds = 0;

            for (int seed = 0; seed < 2000; seed++)
            {
                byte h = size.Roll(new System.Random(seed));
                if (h == 0 || h == 255) atEnds++;
            }

            Assert.Less(atEnds, 40, "too many characters landed exactly on the band ends");
        }

        [Test]
        public void RollsAverageOutToTheDeclaredMean()
        {
            var size = Male();
            float sum = 0f;

            for (int seed = 0; seed < 2000; seed++)
                sum += size.Metres(size.Roll(new System.Random(seed)));

            Assert.AreEqual(size.mean, sum / 2000f, 0.01f);
        }
    }
}
