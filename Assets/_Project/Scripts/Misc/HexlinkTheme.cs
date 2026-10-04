using UnityEngine;
using UnityEngine.UI;

public class HexlinkPalette
{
    // Surfaces & chrome
    public Color Border;
    public Color Panel;
    public Color Strip;
    public Color CellFrame;
    public Color CellFill;
    public Color Divider;
    public Color Ghost;
    public Color BackdropDim;
    public Color Knob;

    // Text & accents
    public Color TextLight;
    public Color ChipText;
    public Color TextGray;
    public Color Accent;
    public Color AccentText;
    public string TextLightHex;
    public string TextGrayHex;

    // Grid cell states
    public Color SelectedFrame;
    public Color SelectedFill;
    public Color NormalFillOdd;
    public Color HoverFill;
    public Color HoverFillOdd;
    public Color RunningFill;
    public Color ErrorFrame;
    public Color ErrorFill;
    public Color PendingFrame;
    public Color PendingFill;

    // Colour-blind variants
    public Color SelectedFrameCB;
    public Color SelectedFillCB;
    public Color ErrorFrameCB;
    public Color ErrorFillCB;
    public Color PendingFrameCB;
    public Color PendingFillCB;

    // Instruction chips
    public Color ChipSubject;
    public Color ChipOperation;
    public Color ChipOperand;

    // Popups
    public Color SelectedTileFill;

    // Button hover tint
    public Color HoverHighlight;
    public Color HoverPressed;
    public Color HoverDisabled;
}

public static class HexlinkTheme
{
    public static readonly HexlinkPalette Dark = new HexlinkPalette
    {
        Border = new Color(0.165f, 0.176f, 0.204f),
        Panel = new Color(0.220f, 0.231f, 0.267f),
        Strip = new Color(0.243f, 0.255f, 0.310f),
        CellFrame = new Color(0.196f, 0.208f, 0.243f),
        CellFill = new Color(0.286f, 0.302f, 0.353f),
        Divider = new Color(0.420f, 0.451f, 0.510f),
        Ghost = new Color(0.286f, 0.302f, 0.353f),
        BackdropDim = new Color(0f, 0f, 0f, 0.5f),
        Knob = new Color(0.95f, 0.95f, 0.95f),

        TextLight = new Color(0.910f, 0.918f, 0.929f),
        ChipText = new Color(0.961f, 0.965f, 0.973f),
        TextGray = new Color(0.680f, 0.706f, 0.745f),
        Accent = new Color(0.580f, 0.740f, 0.780f),
        AccentText = new Color(0.165f, 0.176f, 0.204f),
        TextLightHex = "E8EAED",
        TextGrayHex = "ADB4BE",

        SelectedFrame = new Color(0.580f, 0.740f, 0.780f),
        SelectedFill = new Color(0.230f, 0.302f, 0.322f),
        NormalFillOdd = new Color(0.318f, 0.333f, 0.388f),
        HoverFill = new Color(0.353f, 0.369f, 0.424f),
        HoverFillOdd = new Color(0.380f, 0.396f, 0.451f),
        RunningFill = new Color(0.294f, 0.365f, 0.392f),
        ErrorFrame = new Color(0.780f, 0.490f, 0.490f),
        ErrorFill = new Color(0.373f, 0.220f, 0.220f),
        PendingFrame = new Color(0.980f, 0.780f, 0.320f),
        PendingFill = new Color(0.573f, 0.439f, 0.173f),

        SelectedFrameCB = new Color(0.561f, 0.651f, 0.788f),
        SelectedFillCB = new Color(0.231f, 0.267f, 0.322f),
        ErrorFrameCB = new Color(0.753f, 0.541f, 0.333f),
        ErrorFillCB = new Color(0.322f, 0.267f, 0.208f),
        PendingFrameCB = new Color(0.720f, 0.620f, 0.950f),
        PendingFillCB = new Color(0.443f, 0.384f, 0.604f),

        ChipSubject = new Color(0.384f, 0.412f, 0.463f),
        ChipOperation = new Color(0.322f, 0.459f, 0.510f),
        ChipOperand = new Color(0.424f, 0.459f, 0.510f),

        SelectedTileFill = new Color(0.360f, 0.380f, 0.440f),

        HoverHighlight = new Color(1.18f, 1.18f, 1.18f),
        HoverPressed = new Color(0.84f, 0.84f, 0.84f),
        HoverDisabled = new Color(0.5f, 0.5f, 0.5f, 0.5f),
    };

    public static readonly HexlinkPalette Light = new HexlinkPalette
    {
        Border = new Color(0.776f, 0.800f, 0.863f),        // #C6CCDC
        Panel = new Color(0.945f, 0.953f, 0.976f),        // #F1F3F9
        Strip = new Color(0.906f, 0.918f, 0.953f),        // #E7EAF3
        CellFrame = new Color(0.863f, 0.878f, 0.925f),    // #DCE0EC
        CellFill = new Color(0.914f, 0.925f, 0.961f),     // #E9ECF5
        Divider = new Color(0.725f, 0.757f, 0.831f),      // #B9C1D4
        Ghost = new Color(0.914f, 0.925f, 0.961f),        // #E9ECF5
        BackdropDim = new Color(0f, 0f, 0f, 0.5f),
        Knob = new Color(1f, 1f, 1f),                      // #FFFFFF

        TextLight = new Color(0.200f, 0.231f, 0.306f),    // #333B4E
        ChipText = new Color(0.165f, 0.192f, 0.263f),     // #2A3143
        TextGray = new Color(0.420f, 0.455f, 0.533f),     // #6B7488
        Accent = new Color(0.306f, 0.557f, 0.627f),       // #4E8EA0
        AccentText = new Color(0.957f, 0.969f, 0.980f),   // #F4F7FA
        TextLightHex = "333B4E",
        TextGrayHex = "6B7488",

        SelectedFrame = new Color(0.306f, 0.557f, 0.627f),  // #4E8EA0
        SelectedFill = new Color(0.827f, 0.906f, 0.925f),   // #D3E7EC
        NormalFillOdd = new Color(0.922f, 0.937f, 0.969f),  // #EBEFF7
        HoverFill = new Color(0.886f, 0.902f, 0.949f),      // #E2E6F2
        HoverFillOdd = new Color(0.867f, 0.886f, 0.941f),   // #DDE2F0
        RunningFill = new Color(0.863f, 0.922f, 0.937f),    // #DCEBEF
        ErrorFrame = new Color(0.769f, 0.416f, 0.353f),     // #C46A5A
        ErrorFill = new Color(0.953f, 0.851f, 0.827f),      // #F3D9D3
        PendingFrame = new Color(0.851f, 0.663f, 0.243f),   // #D9A93E
        PendingFill = new Color(0.961f, 0.906f, 0.773f),    // #F5E7C5

        SelectedFrameCB = new Color(0.561f, 0.651f, 0.788f), // #8FA6C9
        SelectedFillCB = new Color(0.863f, 0.902f, 0.957f),  // #DCE6F4
        ErrorFrameCB = new Color(0.710f, 0.475f, 0.227f),    // #B5793A
        ErrorFillCB = new Color(0.949f, 0.882f, 0.808f),     // #F2E1CE
        PendingFrameCB = new Color(0.561f, 0.475f, 0.788f),  // #8F79C9
        PendingFillCB = new Color(0.894f, 0.863f, 0.965f),   // #E4DCF6

        ChipSubject = new Color(0.788f, 0.816f, 0.878f),     // #C9D0E0
        ChipOperation = new Color(0.769f, 0.835f, 0.863f),   // #C4D5DC
        ChipOperand = new Color(0.725f, 0.757f, 0.831f),     // #B9C1D4

        SelectedTileFill = new Color(0.851f, 0.922f, 0.941f), // #D9EBF0

        HoverHighlight = new Color(1.06f, 1.06f, 1.06f),
        HoverPressed = new Color(0.92f, 0.92f, 0.92f),
        HoverDisabled = new Color(0.62f, 0.62f, 0.62f, 0.45f),
    };

    public static readonly HexlinkPalette Glass = new HexlinkPalette
    {
        // Outline design: fills are fully transparent (alpha ~0; a sliver above
        // zero so the remapper can never confuse them with invisible UI), bright
        // outlines carry the structure. Popups keep a translucent backing via
        // Panel + BackdropDim so text stays readable.
        Border = new Color(0.204f, 0.765f, 1.000f, 0.85f),
        Panel = new Color(0.05f, 0.07f, 0.11f, 0.12f),
        Strip = new Color(0.06f, 0.07f, 0.10f, 0.008f),
        CellFrame = new Color(0.204f, 0.765f, 1.000f, 0.10f),
        CellFill = new Color(0.04f, 0.05f, 0.08f, 0.008f),
        Divider = new Color(0.204f, 0.765f, 1.000f, 0.60f),
        Ghost = new Color(0.05f, 0.06f, 0.09f, 0.008f),
        BackdropDim = new Color(0f, 0f, 0f, 0.45f),
        Knob = new Color(0.97f, 0.97f, 0.98f),

        TextLight = new Color(0.96f, 0.97f, 0.98f),
        ChipText = new Color(0.99f, 0.995f, 1f),
        TextGray = new Color(0.72f, 0.76f, 0.83f),
        Accent = new Color(0.580f, 0.740f, 0.780f, 0.70f),
        AccentText = new Color(0.96f, 0.97f, 0.98f),
        TextLightHex = "F5F7FA",
        TextGrayHex = "B8C2D4",

        SelectedFrame = new Color(0.580f, 0.740f, 0.780f),
        SelectedFill = new Color(0.42f, 0.56f, 0.60f, 0.30f),
        NormalFillOdd = new Color(0.07f, 0.08f, 0.11f, 0.008f),
        HoverFill = new Color(0.12f, 0.16f, 0.22f, 0.18f),
        HoverFillOdd = new Color(0.10f, 0.13f, 0.18f, 0.18f),
        RunningFill = new Color(0.14f, 0.20f, 0.24f, 0.22f),
        ErrorFrame = new Color(0.88f, 0.55f, 0.52f),
        ErrorFill = new Color(0.50f, 0.18f, 0.15f, 0.22f),
        PendingFrame = new Color(1.00f, 0.82f, 0.36f),
        PendingFill = new Color(0.55f, 0.42f, 0.16f, 0.22f),

        SelectedFrameCB = new Color(0.561f, 0.651f, 0.788f),
        SelectedFillCB = new Color(0.28f, 0.34f, 0.46f, 0.30f),
        ErrorFrameCB = new Color(0.753f, 0.541f, 0.333f),
        ErrorFillCB = new Color(0.44f, 0.30f, 0.16f, 0.22f),
        PendingFrameCB = new Color(0.720f, 0.620f, 0.950f),
        PendingFillCB = new Color(0.42f, 0.34f, 0.60f, 0.22f),

        ChipSubject = new Color(0.12f, 0.15f, 0.20f, 0.28f),
        ChipOperation = new Color(0.10f, 0.17f, 0.20f, 0.28f),
        ChipOperand = new Color(0.14f, 0.16f, 0.20f, 0.28f),

        SelectedTileFill = new Color(0.42f, 0.56f, 0.60f, 0.30f),

        HoverHighlight = new Color(1.7f, 1.7f, 1.7f),
        HoverPressed = new Color(0.6f, 0.6f, 0.6f),
        HoverDisabled = new Color(0.5f, 0.5f, 0.5f, 0.35f),
    };

    private static readonly (Color dark, Color light, Color glass)[] RemapTriples =
    {
        (Dark.Border, Light.Border, Glass.Border),
        (Dark.Panel, Light.Panel, Glass.Panel),
        (Dark.Strip, Light.Strip, Glass.Strip),
        (Dark.CellFrame, Light.CellFrame, Glass.CellFrame),
        (Dark.CellFill, Light.CellFill, Glass.CellFill),
        (Dark.Divider, Light.Divider, Glass.Divider),
        (Dark.Ghost, Light.Ghost, Glass.Ghost),
        (Dark.BackdropDim, Light.BackdropDim, Glass.BackdropDim),
        (Dark.Knob, Light.Knob, Glass.Knob),
        (Dark.TextLight, Light.TextLight, Glass.TextLight),
        (Dark.ChipText, Light.ChipText, Glass.ChipText),
        (Dark.TextGray, Light.TextGray, Glass.TextGray),
        (Dark.Accent, Light.Accent, Glass.Accent),
        (Dark.AccentText, Light.AccentText, Glass.AccentText),
        (Dark.SelectedFrame, Light.SelectedFrame, Glass.SelectedFrame),
        (Dark.SelectedFill, Light.SelectedFill, Glass.SelectedFill),
        (Dark.NormalFillOdd, Light.NormalFillOdd, Glass.NormalFillOdd),
        (Dark.HoverFill, Light.HoverFill, Glass.HoverFill),
        (Dark.HoverFillOdd, Light.HoverFillOdd, Glass.HoverFillOdd),
        (Dark.RunningFill, Light.RunningFill, Glass.RunningFill),
        (Dark.ErrorFrame, Light.ErrorFrame, Glass.ErrorFrame),
        (Dark.ErrorFill, Light.ErrorFill, Glass.ErrorFill),
        (Dark.PendingFrame, Light.PendingFrame, Glass.PendingFrame),
        (Dark.PendingFill, Light.PendingFill, Glass.PendingFill),
        (Dark.SelectedFrameCB, Light.SelectedFrameCB, Glass.SelectedFrameCB),
        (Dark.SelectedFillCB, Light.SelectedFillCB, Glass.SelectedFillCB),
        (Dark.ErrorFrameCB, Light.ErrorFrameCB, Glass.ErrorFrameCB),
        (Dark.ErrorFillCB, Light.ErrorFillCB, Glass.ErrorFillCB),
        (Dark.PendingFrameCB, Light.PendingFrameCB, Glass.PendingFrameCB),
        (Dark.PendingFillCB, Light.PendingFillCB, Glass.PendingFillCB),
        (Dark.ChipSubject, Light.ChipSubject, Glass.ChipSubject),
        (Dark.ChipOperation, Light.ChipOperation, Glass.ChipOperation),
        (Dark.ChipOperand, Light.ChipOperand, Glass.ChipOperand),
        (Dark.SelectedTileFill, Light.SelectedTileFill, Glass.SelectedTileFill),
    };

    public const int ThemeCount = 3;

    public static string ThemeName(int index)
    {
        switch (index)
        {
            case 1: return "Light";
            case 2: return "Transparent";
            default: return "Dark";
        }
    }

    public static HexlinkPalette Current
    {
        get
        {
            switch (GameOptions.ThemeIndex)
            {
                case 1: return Light;
                case 2: return Glass;
                default: return Dark;
            }
        }
    }

    public static Color Border => Current.Border;
    public static Color Panel => Current.Panel;
    public static Color Strip => Current.Strip;
    public static Color CellFrame => Current.CellFrame;
    public static Color CellFill => Current.CellFill;
    public static Color Divider => Current.Divider;
    public static Color Ghost => Current.Ghost;
    public static Color BackdropDim => Current.BackdropDim;
    public static Color Knob => Current.Knob;
    public static Color TextLight => Current.TextLight;
    public static Color ChipText => Current.ChipText;
    public static Color TextGray => Current.TextGray;
    public static Color Accent => Current.Accent;
    public static Color AccentText => Current.AccentText;
    public static string TextLightHex => Current.TextLightHex;
    public static string TextGrayHex => Current.TextGrayHex;

    public static Color NormalFillOdd => Current.NormalFillOdd;
    public static Color HoverFill => Current.HoverFill;
    public static Color HoverFillOdd => Current.HoverFillOdd;
    public static Color RunningFill => Current.RunningFill;
    public static Color ChipSubject => Current.ChipSubject;
    public static Color ChipOperation => Current.ChipOperation;
    public static Color ChipOperand => Current.ChipOperand;
    public static Color SelectedTileFill => Current.SelectedTileFill;

    public static Color SelectedFrame => GameOptions.ColourBlindMode ? Current.SelectedFrameCB : Current.SelectedFrame;
    public static Color SelectedFill => GameOptions.ColourBlindMode ? Current.SelectedFillCB : Current.SelectedFill;
    public static Color ErrorFrame => GameOptions.ColourBlindMode ? Current.ErrorFrameCB : Current.ErrorFrame;
    public static Color ErrorFill => GameOptions.ColourBlindMode ? Current.ErrorFillCB : Current.ErrorFill;
    public static Color PendingFrame => GameOptions.ColourBlindMode ? Current.PendingFrameCB : Current.PendingFrame;
    public static Color PendingFill => GameOptions.ColourBlindMode ? Current.PendingFillCB : Current.PendingFill;

    public static void ApplyHoverTint(Button button)
    {
        HexlinkPalette palette = Current;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = palette.HoverHighlight;
        colors.pressedColor = palette.HoverPressed;
        colors.selectedColor = Color.white;
        colors.disabledColor = palette.HoverDisabled;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    public static bool TryRemap(Color color, int themeIndex, out Color remapped)
    {
        foreach ((Color dark, Color light, Color glass) triple in RemapTriples)
        {
            if (Matches(color, triple.dark) || Matches(color, triple.light) || Matches(color, triple.glass))
            {
                switch (themeIndex)
                {
                    case 1: remapped = triple.light; return true;
                    case 2: remapped = triple.glass; return true;
                    default: remapped = triple.dark; return true;
                }
            }
        }
        remapped = color;
        return false;
    }

    private static bool Matches(Color a, Color b)
    {
        Color32 ca = a;
        Color32 cb = b;
        return ca.r == cb.r && ca.g == cb.g && ca.b == cb.b && ca.a == cb.a;
    }
}
