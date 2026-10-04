using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class BackButtonSpawner
{
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
        TrySpawn();
    }

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

        Canvas mainCanvas = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas.isRootCanvas)
            {
                mainCanvas = canvas;
                break;
            }
        }
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

        SceneSwitcher switcher = button.AddComponent<SceneSwitcher>();
        switcher.sceneToLoad = target;
        buttonComponent.onClick.AddListener(switcher.LoadScene);

        ThemeSwitcher.ApplyToSubtree(button.transform);
    }
}
