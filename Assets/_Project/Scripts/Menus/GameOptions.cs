using UnityEngine;

public static class GameOptions
{
    private const string KeyPrefix = "Hexlink.";

    // 0 = Dark, 1 = Light, 2 = Glass. Migrates from the legacy LightTheme bool.
    public static int ThemeIndex
    {
        get
        {
            if (PlayerPrefs.GetInt(KeyPrefix + "ThemeSet", 0) == 1)
            {
                return PlayerPrefs.GetInt(KeyPrefix + "Theme", 0);
            }
            return PlayerPrefs.GetInt(KeyPrefix + "LightTheme", 0) == 1 ? 1 : 0;
        }
        set
        {
            PlayerPrefs.SetInt(KeyPrefix + "Theme", value);
            PlayerPrefs.SetInt(KeyPrefix + "ThemeSet", 1);
        }
    }

    // Glass design outline appearance. Defaults: #34C2FF at 30% opacity.
    public static float GlassOutlineAlpha
    {
        get => PlayerPrefs.GetFloat(KeyPrefix + "GlassOutlineAlpha", 0.30f);
        set => PlayerPrefs.SetFloat(KeyPrefix + "GlassOutlineAlpha", Mathf.Clamp01(value));
    }

    public static string GlassOutlineHex
    {
        get => PlayerPrefs.GetString(KeyPrefix + "GlassOutlineColor", "34C2FF");
        set => PlayerPrefs.SetString(KeyPrefix + "GlassOutlineColor", value);
    }

    public static Color GlassOutlineColor
    {
        get
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + GlassOutlineHex, out color)
                ? color
                : new Color(0.204f, 0.765f, 1f);
        }
    }

    public static bool TrySetGlassOutlineHex(string text)
    {
        string hex = text.Trim().TrimStart('#');
        if (hex.Length != 6) return false;

        Color color;
        if (!ColorUtility.TryParseHtmlString("#" + hex, out color)) return false;

        GlassOutlineHex = hex.ToUpper();
        return true;
    }

    public static bool ColourBlindMode
    {
        get => PlayerPrefs.GetInt(KeyPrefix + "ColourBlind2", 0) == 1;
        set => PlayerPrefs.SetInt(KeyPrefix + "ColourBlind2", value ? 1 : 0);
    }

    public static bool ReducedMotion
    {
        get => PlayerPrefs.GetInt(KeyPrefix + "ReducedMotion2", 0) == 1;
        set => PlayerPrefs.SetInt(KeyPrefix + "ReducedMotion2", value ? 1 : 0);
    }

    public static bool ConfirmTileChanges
    {
        get => PlayerPrefs.GetInt(KeyPrefix + "ConfirmTileChanges", 1) == 1;
        set => PlayerPrefs.SetInt(KeyPrefix + "ConfirmTileChanges", value ? 1 : 0);
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt(KeyPrefix + "VSync", 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyPrefix + "VSync", value ? 1 : 0);
            QualitySettings.vSyncCount = value ? 1 : 0;
        }
    }

    public static float PanSpeed
    {
        get => PlayerPrefs.GetFloat(KeyPrefix + "PanSpeed", 0.02f);
        set => PlayerPrefs.SetFloat(KeyPrefix + "PanSpeed", value);
    }

    public static float OrbitSpeed
    {
        get => PlayerPrefs.GetFloat(KeyPrefix + "OrbitSpeed", 5f);
        set => PlayerPrefs.SetFloat(KeyPrefix + "OrbitSpeed", value);
    }

    public static float UiScale
    {
        get => PlayerPrefs.GetFloat(KeyPrefix + "UiScale", 1f);
        set => PlayerPrefs.SetFloat(KeyPrefix + "UiScale", value);
    }

    public static float ExecutionSpeed
    {
        get => PlayerPrefs.GetFloat(KeyPrefix + "ExecutionSpeed", 1f);
        set => PlayerPrefs.SetFloat(KeyPrefix + "ExecutionSpeed", Mathf.Clamp(value, 0.25f, 3f));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ApplySystemSettings()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
    }
}
