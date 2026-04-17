using UnityEngine;
using System.Collections.Generic;

public class ButtonColorScheme
{
    private static ButtonColorScheme _instance;
    public static ButtonColorScheme Instance => _instance ??= new ButtonColorScheme();

    [System.Serializable]
    public class ButtonColorSet
    {
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }
        public Color HoverBackgroundColor { get; set; }
        public Color HoverTextColor { get; set; }
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
                    BackgroundColor = HexToColor("FBB800"),
                    TextColor = Color.black,
                    HoverBackgroundColor = HexToColor("E8A500"),
                    HoverTextColor = Color.black
                }
            },
            {
                ButtonType.Secondary,
                new ButtonColorSet
                {
                    BackgroundColor = HexToColor("0483B2"),
                    TextColor = Color.white,
                    HoverBackgroundColor = HexToColor("036A94"),
                    HoverTextColor = Color.white
                }
            },
            {
                ButtonType.Success,
                new ButtonColorSet
                {
                    BackgroundColor = HexToColor("12A800"),
                    TextColor = Color.white,
                    HoverBackgroundColor = HexToColor("0E8600"),
                    HoverTextColor = Color.black
                }
            },
            {
                ButtonType.Danger,
                new ButtonColorSet
                {
                    BackgroundColor = HexToColor("AD0000"),
                    TextColor = Color.white,
                    HoverBackgroundColor = HexToColor("8B0000"),
                    HoverTextColor = Color.white
                }
            },
            {
                ButtonType.Ghost,
                new ButtonColorSet
                {
                    BackgroundColor = new Color(1f, 1f, 1f, 0f),
                    TextColor = Color.white,
                    HoverBackgroundColor = new Color(1f, 1f, 1f, 0.1f),
                    HoverTextColor = Color.white
                }
            },
            {
                ButtonType.Faded,
                new ButtonColorSet
                {
                    BackgroundColor = HexToColor("666666"),
                    TextColor = Color.white,
                    HoverBackgroundColor = HexToColor("777777"),
                    HoverTextColor = Color.white
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
        return GetColorSet(buttonType).BackgroundColor;
    }

    public Color GetTextColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).TextColor;
    }

    public Color GetHoverBackgroundColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).HoverBackgroundColor;
    }

    public Color GetHoverTextColor(ButtonType buttonType)
    {
        return GetColorSet(buttonType).HoverTextColor;
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
