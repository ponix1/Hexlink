using UnityEngine;
using UnityEngine.UI;

public static class HexlinkTheme
{
    public static readonly Color Border = new Color(0.165f, 0.176f, 0.204f);
    public static readonly Color Panel = new Color(0.220f, 0.231f, 0.267f);
    public static readonly Color Strip = new Color(0.243f, 0.255f, 0.310f);
    public static readonly Color CellFrame = new Color(0.196f, 0.208f, 0.243f);
    public static readonly Color CellFill = new Color(0.286f, 0.302f, 0.353f);
    public static readonly Color Divider = new Color(0.420f, 0.451f, 0.510f);
    public static readonly Color TextLight = new Color(0.910f, 0.918f, 0.929f);
    public static readonly Color ChipText = new Color(0.961f, 0.965f, 0.973f);
    public static readonly Color TextGray = new Color(0.680f, 0.706f, 0.745f);
    public static readonly Color Accent = new Color(0.580f, 0.740f, 0.780f);
    public static readonly Color Ghost = new Color(0.286f, 0.302f, 0.353f);
    public static readonly Color BackdropDim = new Color(0f, 0f, 0f, 0.5f);

    public static void ApplyHoverTint(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }
}
