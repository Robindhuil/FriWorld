using System;

namespace FriWorld.Character.Editor
{
    /// <summary>One material slot declared by a material name on the mesh.</summary>
    public readonly struct MaterialSlot
    {
        /// <summary>The keyword between "char_" and the numeric suffix. Not necessarily a colour
        /// class — that is the catalog's call.</summary>
        public readonly string ColorClass;

        /// <summary>1..9. Which base colour of the class this slot wants.</summary>
        public readonly int BaseKey;

        /// <summary>0 for the base colour, 1 for the first darker shade.</summary>
        public readonly int ShadeLevel;

        public MaterialSlot(string colorClass, int baseKey, int shadeLevel)
        {
            ColorClass = colorClass;
            BaseKey = baseKey;
            ShadeLevel = shadeLevel;
        }

        public override string ToString() => "char_" + ColorClass + "_" + BaseKey
            + (ShadeLevel > 0 ? ShadeLevel.ToString() : string.Empty);
    }

    /// <summary>
    /// Reads "char_&lt;class&gt;_&lt;key&gt;" off a material name.
    ///
    /// The name on the mesh is a declaration of intent, not a colour: it says which slot of which
    /// class this material fills. The colour arrives from the colorway at bake time.
    ///
    /// This is syntax only. Whether the keyword is a real colour class is decided by the catalog
    /// — that split is what lets "char_leather_1" be a valid name that simply never gets
    /// recoloured, while "char_torzo_1" shows up as a typo instead of silently working.
    /// </summary>
    public static class MaterialSlotKey
    {
        const string Prefix = "char_";

        public static bool TryParse(string materialName, out MaterialSlot slot)
        {
            slot = default;

            if (string.IsNullOrEmpty(materialName)) return false;
            if (!materialName.StartsWith(Prefix, StringComparison.Ordinal)) return false;

            int split = materialName.LastIndexOf('_');

            // The underscore has to come after at least one character of class name, otherwise
            // we matched the one inside "char_" itself.
            if (split < Prefix.Length) return false;

            string colorClass = materialName.Substring(Prefix.Length, split - Prefix.Length);
            string digits = materialName.Substring(split + 1);

            if (colorClass.Length == 0) return false;
            if (digits.Length < 1 || digits.Length > 2) return false;

            foreach (char c in digits)
                if (c < '0' || c > '9') return false;

            int baseKey = digits[0] - '0';
            if (baseKey < 1) return false;

            int shadeLevel = digits.Length == 1 ? 0 : digits[1] - '0';

            // "10" is not "colour ten", it is a malformed shade. Nine base colours is the cap.
            if (digits.Length == 2 && shadeLevel < 1) return false;

            slot = new MaterialSlot(colorClass, baseKey, shadeLevel);
            return true;
        }
    }
}
