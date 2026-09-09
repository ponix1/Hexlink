using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ColourBlindApplier
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => ApplyToScene();
        ApplyToScene();
    }

    public static void ApplyToScene()
    {
        if (!GameOptions.ColourBlindMode) return;

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (button.transition != Button.Transition.ColorTint) continue;

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
