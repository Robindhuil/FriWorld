using UnityEngine;

namespace FriWorld.Character.Editor
{
    /// <summary>
    /// Derives the darker shade of a base colour.
    ///
    /// In HSV, not by multiplying RGB: multiplying RGB washes the hue towards whichever channel
    /// was already dominant, so a warm red goes brown. Dropping value and nudging saturation up
    /// is what a fold in cloth actually does to a colour.
    ///
    /// The factors come from the colour class, because a fold in fabric and a strand of hair are
    /// not the same number.
    /// </summary>
    public static class ShadeColor
    {
        public static Color Derive(Color baseColor, float value, float saturation)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            s = Mathf.Clamp01(s * saturation);
            v = Mathf.Clamp01(v * value);

            var derived = Color.HSVToRGB(h, s, v);
            derived.a = baseColor.a;
            return derived;
        }
    }
}
