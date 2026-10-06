// ColourBlindApplier: when colour-blind mode is on, boosts hover/press contrast on every
// ColorTint button. Runs per scene load and on every options toggle.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ColourBlindApplier
{
    // Runtime-spawner pattern: re-apply per scene load so late-spawned buttons are covered.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => ApplyToScene();
        ApplyToScene();
    }

    // No-op when the mode is off (existing tints are left untouched).
    public static void ApplyToScene()
    {
        if (!GameOptions.ColourBlindMode) return;

        foreach (Button button in Object.FindObjectsByType<Button>())
        {
            if (button.transition != Button.Transition.ColorTint) continue;

            // DIM rather than brighten: overbright multiply did nothing on white images
            // (past bug), so highlight contrast comes from dimming instead.
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.pressedColor = new Color(0.45f, 0.45f, 0.45f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.35f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }
    }
}
