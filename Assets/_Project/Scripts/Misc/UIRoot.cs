// UIRoot: resolves the canvas that runtime-spawned UI should attach to. Only root
// canvases in the active scene qualify — canvases on DontDestroyOnLoad objects
// (like the SceneTransition fade overlay) belong to no scene and must never end
// up as the parent of scene UI, or that UI dies with the overlay.

using UnityEngine;
using UnityEngine.SceneManagement;

public static class UIRoot
{
    public static Canvas FindSceneCanvas()
    {
        Scene active = SceneManager.GetActiveScene();
        Canvas[] canvases = Object.FindObjectsByType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.isRootCanvas && canvas.gameObject.scene == active) return canvas;
        }
        return null;
    }
}
