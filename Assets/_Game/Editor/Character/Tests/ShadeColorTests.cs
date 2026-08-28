using FriWorld.Character.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FriWorld.Character.Tests
{
    public class ShadeColorTests
    {
        [Test]
        public void TheShadeIsDarkerThanTheBase()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f);
            var shade = ShadeColor.Derive(baseColor, 0.62f, 1.12f);

            Color.RGBToHSV(baseColor, out _, out _, out float baseValue);
            Color.RGBToHSV(shade, out _, out _, out float shadeValue);

            Assert.Less(shadeValue, baseValue);
        }

        [Test]
        public void TheHueSurvives()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f);
            var shade = ShadeColor.Derive(baseColor, 0.62f, 1.12f);

            Color.RGBToHSV(baseColor, out float baseHue, out _, out _);
            Color.RGBToHSV(shade, out float shadeHue, out _, out _);

            Assert.AreEqual(baseHue, shadeHue, 0.002f);
        }

        [Test]
        public void AlphaIsCarriedOverUntouched()
        {
            var baseColor = new Color(0.4f, 0.3f, 0.7f, 0.5f);
            Assert.AreEqual(0.5f, ShadeColor.Derive(baseColor, 0.62f, 1.12f).a, 0.0001f);
        }

        [Test]
        public void AGreyStaysGrey()
        {
            // Saturation 0 multiplied by anything is still 0, so a neutral never picks up a tint.
            var shade = ShadeColor.Derive(new Color(0.6f, 0.6f, 0.6f), 0.62f, 1.5f);

            Assert.AreEqual(shade.r, shade.g, 0.0001f);
            Assert.AreEqual(shade.g, shade.b, 0.0001f);
        }

        [Test]
        public void SaturationIsClampedNotWrapped()
        {
            var shade = ShadeColor.Derive(new Color(1f, 0f, 0f), 0.9f, 4f);

            Color.RGBToHSV(shade, out _, out float saturation, out _);
            Assert.LessOrEqual(saturation, 1f);
        }
    }
}
