// MainMenuButtons: hooks up the Main Menu buttons that aren't wired in the scene.
// The Exit button quits the game (stopping play mode in the editor), and the
// Creator button is greyed out until the in-game level creator exists.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuButtons
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Hook();
        Hook();
    }

    private static void Hook()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu") return;

        Canvas canvas = UIRoot.FindSceneCanvas();
        if (canvas == null) return;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == "Exit")
            {
                // Remove-then-add keeps the hook idempotent if Hook runs twice.
                button.onClick.RemoveListener(Quit);
                button.onClick.AddListener(Quit);
            }
            else if (button.gameObject.name == "Creator")
            {
                // The Button's own disabled tint does the greying; this also
                // survives the scene being resaved from the editor.
                button.interactable = false;
            }
        }
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        // Application.Quit does nothing in play mode; stop it properly instead.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
