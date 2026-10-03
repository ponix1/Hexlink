using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class WinPopup
{
    private const float PanelWidth = 520f;
    private const float PanelHeight = 430f;
    private const float NameWidth = 170f;
    private const float ValueWidth = 75f;
    private const float TagWidth = 115f;

    private static readonly Color RowColor = HexlinkTheme.CellFrame;
    private static readonly Color GoldColor = new Color(0.85f, 0.68f, 0.28f);

    private static GameObject root;

    public static void Show(PuzzleRecords.Result result)
    {
        Close();

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        root = new GameObject("WinPopup", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        StretchFull(root.GetComponent<RectTransform>());

        Image backdrop = root.GetComponent<Image>();
        backdrop.color = HexlinkTheme.BackdropDim;
        Button backdropButton = root.GetComponent<Button>();
        backdropButton.transition = Button.Transition.None;
        backdropButton.onClick.AddListener(Close);

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(root.transform, false);
        border.GetComponent<Image>().color = HexlinkTheme.Border;
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.5f, 0.5f);
        borderRT.anchorMax = new Vector2(0.5f, 0.5f);
        borderRT.pivot = new Vector2(0.5f, 0.5f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = HexlinkTheme.Panel;
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(4f, 4f);
        panelRT.offsetMax = new Vector2(-4f, -4f);

        GameObject title = CreateTMP("Title", panel.transform, "Puzzle Solved!", 30, FontStyles.Bold);
        title.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextLight;
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -14f);
        titleRT.sizeDelta = new Vector2(0f, 40f);

        GameObject subtitle = CreateTMP("Subtitle", panel.transform,
            result.AnyImproved ? "New personal best!" : "Solution recorded", 15, FontStyles.Bold);
        subtitle.GetComponent<TextMeshProUGUI>().color = result.AnyImproved ? GoldColor : HexlinkTheme.TextGray;
        RectTransform subtitleRT = subtitle.GetComponent<RectTransform>();
        subtitleRT.anchorMin = new Vector2(0f, 1f);
        subtitleRT.anchorMax = new Vector2(1f, 1f);
        subtitleRT.pivot = new Vector2(0.5f, 1f);
        subtitleRT.anchoredPosition = new Vector2(0f, -56f);
        subtitleRT.sizeDelta = new Vector2(0f, 20f);

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panel.transform, false);
        divider.GetComponent<Image>().color = HexlinkTheme.Divider;
        RectTransform dividerRT = divider.GetComponent<RectTransform>();
        dividerRT.anchorMin = new Vector2(0f, 1f);
        dividerRT.anchorMax = new Vector2(1f, 1f);
        dividerRT.pivot = new Vector2(0.5f, 1f);
        dividerRT.anchoredPosition = new Vector2(0f, -84f);
        dividerRT.sizeDelta = new Vector2(-56f, 2f);

        GameObject rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rows.transform.SetParent(panel.transform, false);
        RectTransform rowsRT = rows.GetComponent<RectTransform>();
        rowsRT.anchorMin = new Vector2(0f, 1f);
        rowsRT.anchorMax = new Vector2(1f, 1f);
        rowsRT.pivot = new Vector2(0.5f, 1f);
        rowsRT.anchoredPosition = new Vector2(0f, -92f);
        rowsRT.sizeDelta = new Vector2(0f, 190f);
        VerticalLayoutGroup rowsVLG = rows.GetComponent<VerticalLayoutGroup>();
        rowsVLG.childControlWidth = true;
        rowsVLG.childControlHeight = true;
        rowsVLG.childForceExpandWidth = false;
        rowsVLG.childForceExpandHeight = false;
        rowsVLG.spacing = 4f;
        rowsVLG.padding = new RectOffset(28, 28, 4, 4);

        CreateHeaderRow(rows.transform);
        CreateMetricRow(rows.transform, "Instructions", result.Instructions, result.PrevInstructions, result.NewInstructions);
        CreateMetricRow(rows.transform, "Cycles", result.Cycles, result.PrevCycles, result.NewCycles);
        CreateMetricRow(rows.transform, "Processors", result.Processors, result.PrevProcessors, result.NewProcessors);
        CreateMetricRow(rows.transform, "Sum", result.Sum, result.PrevSum, result.NewSum);

        GameObject puzzlesButton = CreateButton(panel.transform, "PuzzlesButton", "Puzzles", HexlinkTheme.Ghost, HexlinkTheme.TextLight);
        puzzlesButton.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("Puzzle_Select"));
        RectTransform puzzlesRT = puzzlesButton.GetComponent<RectTransform>();
        puzzlesRT.anchorMin = new Vector2(0.5f, 0f);
        puzzlesRT.anchorMax = new Vector2(0.5f, 0f);
        puzzlesRT.pivot = new Vector2(0.5f, 0f);
        puzzlesRT.anchoredPosition = new Vector2(-80f, 14f);
        puzzlesRT.sizeDelta = new Vector2(150f, 36f);

        GameObject continueButton = CreateButton(panel.transform, "ContinueButton", "Continue", HexlinkTheme.Accent, HexlinkTheme.Border);
        continueButton.GetComponent<Button>().onClick.AddListener(Close);
        RectTransform continueRT = continueButton.GetComponent<RectTransform>();
        continueRT.anchorMin = new Vector2(0.5f, 0f);
        continueRT.anchorMax = new Vector2(0.5f, 0f);
        continueRT.pivot = new Vector2(0.5f, 0f);
        continueRT.anchoredPosition = new Vector2(80f, 14f);
        continueRT.sizeDelta = new Vector2(150f, 36f);

        if (!GameOptions.ReducedMotion)
        {
            Fader fader = root.AddComponent<Fader>();
            fader.Begin(root.GetComponent<CanvasGroup>(), borderRT, 0.15f);
        }
    }

    public static void Close()
    {
        if (root != null)
        {
            UnityEngine.Object.Destroy(root);
            root = null;
        }
    }

    private static GameObject CreateRow(Transform parent, string name, float height)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().preferredHeight = height;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 0f;
        return row;
    }

    private static void CreateHeaderRow(Transform parent)
    {
        GameObject row = CreateRow(parent, "Header", 24f);

        GameObject name = CreateTMP("Name", row.transform, "Metric", 13, FontStyles.Bold);
        name.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        name.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextGray;
        name.AddComponent<LayoutElement>().preferredWidth = NameWidth;

        GameObject yours = CreateTMP("Yours", row.transform, "Yours", 13, FontStyles.Bold);
        yours.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextGray;
        yours.AddComponent<LayoutElement>().preferredWidth = ValueWidth;

        GameObject best = CreateTMP("Best", row.transform, "Best", 13, FontStyles.Bold);
        best.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextGray;
        best.AddComponent<LayoutElement>().preferredWidth = ValueWidth;

        GameObject tag = CreateTMP("Tag", row.transform, "", 13, FontStyles.Bold);
        tag.AddComponent<LayoutElement>().preferredWidth = TagWidth;
    }

    private static void CreateMetricRow(Transform parent, string label, int yours, int best, bool improved)
    {
        GameObject row = CreateRow(parent, "Row_" + label, 34f);
        row.AddComponent<Image>().color = RowColor;

        GameObject name = CreateTMP("Name", row.transform, label, 16, FontStyles.Normal);
        name.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        name.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextLight;
        name.AddComponent<LayoutElement>().preferredWidth = NameWidth;

        GameObject yoursLabel = CreateTMP("Yours", row.transform, yours.ToString(), 16, FontStyles.Bold);
        yoursLabel.GetComponent<TextMeshProUGUI>().color = improved ? GoldColor : HexlinkTheme.TextLight;
        yoursLabel.AddComponent<LayoutElement>().preferredWidth = ValueWidth;

        GameObject bestLabel = CreateTMP("Best", row.transform, improved ? yours.ToString() : Format(best), 16, FontStyles.Normal);
        bestLabel.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextGray;
        bestLabel.AddComponent<LayoutElement>().preferredWidth = ValueWidth;

        GameObject tag = CreateTMP("Tag", row.transform, improved ? "NEW BEST" : "", 13, FontStyles.Bold);
        tag.GetComponent<TextMeshProUGUI>().color = GoldColor;
        tag.AddComponent<LayoutElement>().preferredWidth = TagWidth;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Color color, Color textColor)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
        button.GetComponent<Image>().color = color;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = button.GetComponent<Image>();
        buttonComponent.transition = Button.Transition.ColorTint;
        ColorBlock colors = buttonComponent.colors;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f);
        colors.selectedColor = Color.white;
        buttonComponent.colors = colors;

        GameObject text = CreateTMP("Label", button.transform, label, 14, FontStyles.Bold);
        text.GetComponent<TextMeshProUGUI>().color = textColor;
        StretchFull(text.GetComponent<RectTransform>());
        return button;
    }

    private static string Format(int value)
    {
        return value == int.MaxValue ? "\u2014" : value.ToString();
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(fontSize * GameOptions.UiScale));
        tmp.fontStyle = style;
        tmp.color = HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().preferredHeight = tmp.fontSize * 1.4f;
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private class Fader : MonoBehaviour
    {
        private CanvasGroup group;
        private Transform scaleTarget;
        private float duration;
        private float progress;
        private Vector3 targetScale;

        public void Begin(CanvasGroup canvasGroup, Transform scale, float seconds)
        {
            group = canvasGroup;
            scaleTarget = scale;
            duration = Mathf.Max(0.01f, seconds);
            targetScale = scaleTarget.localScale;
            group.alpha = 0f;
            scaleTarget.localScale = targetScale * 0.95f;
        }

        private void Update()
        {
            progress = Mathf.Min(1f, progress + Time.unscaledDeltaTime / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            group.alpha = eased;
            scaleTarget.localScale = Vector3.Lerp(targetScale * 0.95f, targetScale, eased);
            if (progress >= 1f)
            {
                Destroy(this);
            }
        }
    }
}
