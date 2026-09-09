using UnityEngine;

public static class GameOptions
{
    private const string KeyPrefix = "Hexlink.";

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
