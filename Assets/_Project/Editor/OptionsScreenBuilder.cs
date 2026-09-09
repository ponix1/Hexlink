using UnityEngine;
using UnityEditor;

public class OptionsScreenBuilder
{
    [MenuItem("Hexlink/Build Options Screen (preview)")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode first - the options screen is auto-spawned at runtime anyway.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the open scene.");
            return;
        }

        Transform existing = canvas.transform.Find("OptionsScreen");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject screen = OptionsScreenFactory.Build(canvas);
        screen.SetActive(false);
        Selection.activeGameObject = screen;
        Debug.Log("OptionsScreen preview built (for layout tweaking only). The real screen auto-spawns " +
                   "in the Main Menu at runtime - you do not need to build or save anything.");
    }
}
