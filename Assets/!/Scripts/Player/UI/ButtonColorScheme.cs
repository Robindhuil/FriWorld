using UnityEngine;
using System.Collections.Generic;

public class ButtonColorScheme
{
    private static ButtonColorScheme _instance;
    public static ButtonColorScheme Instance => _instance ??= new ButtonColorScheme();

    [System.Serializable]
    public class ButtonColorSet
    {
        public Color backgroundColor;
        public Color textColor;
        public Color hoverBackgroundColor;
        public Color hoverTextColor;
    }

    public enum ButtonType
    {
        Primary,      // Yellow bg-FBB800, black text
        Secondary,    // Blue bg-0483B2, white text
        Success,      // Green bg-12A800, white text
        Danger,       // Red bg-AD0000, white text
        Ghost,        // Clear bg, white text
        Faded         // Grey bg, white text (disabled state)
    }

    private readonly Dictionary<ButtonType, ButtonColorSet> _colorSchemes;

    private ButtonColorScheme()
    {
        _colorSchemes = new Dictionary<ButtonType, ButtonColorSet>
        {
            {
                ButtonType.Primary,
                new ButtonColorSet
                {
                    backgroundColor = HexToColor("FBB800"),
                    textColor = Color.black,
                    hoverBackgroundColor = HexToColor("E8A500"),
                    hoverTextColor = Color.black
                }
            },
            {
                ButtonType.Secondary,
                new ButtonColorSet
                {
                    backgroundColor = HexToColor("0483B2"),
                    textColor = Color.white,
                    hoverBackgroundColor = HexToColor("036A94"),
                    hoverTextColor = Color.white
                }
            },
            {
                ButtonType.Success,
                new ButtonColorSet
                {
                    backgroundColor = HexToColor("12A800"),
                    textColor = Color.white,
                    hoverBackgroundColor = HexToColor("0E8600"),
                    hoverTextColor = Color.black
                }
            },
            {
                ButtonType.Danger,
                new ButtonColorSet
                {
                    backgroundColor = HexToColor("AD0000"),
                    textColor = Color.white,
                    hoverBackgroundColor = HexToColor("8B0000"),
                    hoverTextColor = Color.white
                }
            },
            {
                ButtonType.Ghost,
                new ButtonColorSet
                {
                    backgroundColor = new Color(1f, 1f, 1f, 0f),
                    textColor = Color.white,
                    hoverBackgroundColor = new Color(1f, 1f, 1f, 0.1f),
                    hoverTextColor = Color.white
                }
            },
            {
                ButtonType.Faded,
                new ButtonColorSet
                {
                    backgroundColor = HexToColor("666666"),
                    textColor = Color.white,
                    hoverBackgroundColor = HexToColor("777777"),
                    hoverTextColor = Color.white
                }
            }
        };
    }

    public ButtonColorSet GetColorSet(ButtonType buttonType)
    {
        if (_colorSchemes.TryGetValue(buttonType, out var colorSet))
        {
            return colorSet;
        }

        Debug.LogWarning($"Color set not found for button type: {buttonType}");
        return _colorSchemes[ButtonType.Primary];
    }

    public Color GetBackgroundColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).backgroundColor;
    }

    public Color GetTextColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).textColor;
    }

    public Color GetHoverBackgroundColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).hoverBackgroundColor;
    }

    public Color GetHoverTextColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).hoverTextColor;
    }

    private Color HexToColor(string hex)
    {
        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        Debug.LogError($"Failed to parse color: {hex}");
        return Color.white;
    }
}
