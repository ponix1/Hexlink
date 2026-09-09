using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class BackButtonSpawner
{
    private static readonly Color ControlColor = new Color(0.23f, 0.23f, 0.27f);

    private static string BackTargetFor(string sceneName)
    {
        switch (sceneName)
        {
            case "Puzzle_Play": return "Puzzle_Select";
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
        string target = BackTargetFor(SceneManager.GetActiveScene().name);
        if (target == null) return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
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
        image.color = ControlColor;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        buttonComponent.transition = Button.Transition.ColorTint;
        ColorBlock colors = buttonComponent.colors;
        colors.highlightedColor = new Color(0.55f, 0.55f, 0.62f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.35f);
        colors.selectedColor = new Color(0.55f, 0.55f, 0.62f);
        buttonComponent.colors = colors;

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta = new Vector2(110f, 34f);

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = "\u2190 Back";
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(15f * GameOptions.UiScale));
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.91f, 0.91f, 0.91f);
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        SceneSwitcher switcher = button.AddComponent<SceneSwitcher>();
        switcher.sceneToLoad = target;
        buttonComponent.onClick.AddListener(switcher.LoadScene);
    }
}
