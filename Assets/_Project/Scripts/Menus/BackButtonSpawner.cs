// BackButtonSpawner: runtime-spawns a Back (or "Leave") button on every scene that has an
// entry in the hardcoded back-navigation map, using a code-built SceneSwitcher.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class BackButtonSpawner
{
    // Hardcoded back-navigation map; null target = scene gets no Back button.
    // Puzzle_Play normally returns to Puzzle_Select but honours a dynamic return scene.
    private static string BackTargetFor(string sceneName)
    {
        switch (sceneName)
        {
            case "Puzzle_Play":
                return string.IsNullOrEmpty(PuzzleSelection.ReturnScene)
                    ? "Puzzle_Select"
                    : PuzzleSelection.ReturnScene;
            case "Puzzle_Select": return "Puzzle_Level";
            case "Level_Select": return "Puzzle_Level";
            case "Puzzle_Level": return "Main Menu";
            default: return null;
        }
    }

    // Runtime-spawner pattern: the button must exist in play mode regardless of scene edits.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
        TrySpawn();
    }

    // Idempotent: bails if any canvas already carries a BackButton.
    private static void TrySpawn()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string target = BackTargetFor(sceneName);
        if (target == null) return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.transform.Find("BackButton") != null) return;
        }

        // Attach to the scene's own root canvas — never a persistent one.
        Canvas mainCanvas = UIRoot.FindSceneCanvas();
        if (mainCanvas == null) return;

        GameObject button = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(mainCanvas.transform, false);
        Image image = button.GetComponent<Image>();
        image.color = HexlinkTheme.Ghost;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        HexlinkTheme.ApplyHoverTint(buttonComponent);

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta = new Vector2(110f, 34f);

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        // Gameplay scene says "Leave"; menus get the usual back arrow.
        tmp.text = sceneName == "Puzzle_Play" ? "Leave" : "\u2190 Back";
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(15f * GameOptions.UiScale));
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Code wiring: runtime-built buttons can't rely on Inspector-assigned listeners.
        SceneSwitcher switcher = button.AddComponent<SceneSwitcher>();
        switcher.sceneToLoad = target;
        buttonComponent.onClick.AddListener(switcher.LoadScene);

        ThemeSwitcher.ApplyToSubtree(button.transform);
    }
}
