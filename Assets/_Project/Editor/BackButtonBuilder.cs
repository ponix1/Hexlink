using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using TMPro;
using UnityEngine.SceneManagement;

public class BackButtonBuilder
{
    private static readonly Color ControlColor = new Color(0.23f, 0.23f, 0.27f);
    private static readonly Color ControlHighlight = new Color(0.55f, 0.55f, 0.62f);
    private static readonly Color ControlPressed = new Color(0.3f, 0.3f, 0.35f);

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

    [MenuItem("Hexlink/Build Back Button")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play Mode first - anything built during Play Mode cannot be saved and is lost on stop.");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        string target = BackTargetFor(currentScene);
        if (target == null)
        {
            Debug.LogError($"No back target defined for scene '{currentScene}' (the Main Menu is the root screen).");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the open scene.");
            return;
        }

        Transform existing = canvas.transform.Find("BackButton");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject button = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(canvas.transform, false);
        Image image = button.GetComponent<Image>();
        image.color = ControlColor;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        buttonComponent.transition = Button.Transition.ColorTint;
        ColorBlock colors = buttonComponent.colors;
        colors.highlightedColor = ControlHighlight;
        colors.pressedColor = ControlPressed;
        colors.selectedColor = ControlHighlight;
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
        UnityEventTools.AddPersistentListener(buttonComponent.onClick, switcher.LoadScene);

        Selection.activeGameObject = button;
        Debug.Log($"BackButton built: '{currentScene}' -> '{target}'. Remember to save the scene.");
    }
}
