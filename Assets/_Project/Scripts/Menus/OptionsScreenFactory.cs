using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class OptionsScreenFactory
{
    private const float PanelWidth = 490f;
    private const float PanelHeight = 660f;
    private const float RowHeight = 36f;
    private const float LabelWidth = 220f;

    private static readonly Color BorderColor = new Color(0.29f, 0.29f, 0.33f);
    private static readonly Color PanelColor = new Color(0.14f, 0.14f, 0.16f);
    private static readonly Color ControlColor = new Color(0.23f, 0.23f, 0.27f);
    private static readonly Color ControlHighlight = new Color(0.55f, 0.55f, 0.62f);
    private static readonly Color ControlPressed = new Color(0.3f, 0.3f, 0.35f);
    private static readonly Color TextColor = new Color(0.91f, 0.91f, 0.91f);
    private static readonly Color MutedTextColor = new Color(0.6f, 0.6f, 0.65f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
        TrySpawn();
    }

    private static void TrySpawn()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu") return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("OptionsScreen");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        GameObject screen = Build(canvas);
        screen.SetActive(false);
    }

    public static GameObject Build(Canvas canvas)
    {
        GameObject screen = new GameObject("OptionsScreen", typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(canvas.transform, false);
        screen.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        RectTransform screenRT = screen.GetComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero;
        screenRT.anchorMax = Vector2.one;
        screenRT.offsetMin = Vector2.zero;
        screenRT.offsetMax = Vector2.zero;

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(screen.transform, false);
        border.GetComponent<Image>().color = BorderColor;
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.5f, 0.5f);
        borderRT.anchorMax = new Vector2(0.5f, 0.5f);
        borderRT.pivot = new Vector2(0.5f, 0.5f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = PanelColor;
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(3f, 3f);
        panelRT.offsetMax = new Vector2(-3f, -3f);

        GameObject title = CreateTMP("Title", panel.transform, "Options", 26, FontStyles.Bold);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -12f);
        titleRT.sizeDelta = new Vector2(0f, 36f);

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panel.transform, false);
        divider.GetComponent<Image>().color = BorderColor;
        RectTransform dividerRT = divider.GetComponent<RectTransform>();
        dividerRT.anchorMin = new Vector2(0f, 1f);
        dividerRT.anchorMax = new Vector2(1f, 1f);
        dividerRT.pivot = new Vector2(0.5f, 1f);
        dividerRT.anchoredPosition = new Vector2(0f, -56f);
        dividerRT.sizeDelta = new Vector2(-48f, 2f);

        GameObject rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        rows.transform.SetParent(panel.transform, false);
        RectTransform rowsRT = rows.GetComponent<RectTransform>();
        rowsRT.anchorMin = new Vector2(0f, 1f);
        rowsRT.anchorMax = new Vector2(1f, 1f);
        rowsRT.pivot = new Vector2(0.5f, 1f);
        rowsRT.anchoredPosition = new Vector2(0f, -66f);
        rowsRT.sizeDelta = new Vector2(0f, PanelHeight - 150f);
        VerticalLayoutGroup rowsVLG = rows.GetComponent<VerticalLayoutGroup>();
        rowsVLG.childControlWidth = true;
        rowsVLG.childControlHeight = true;
        rowsVLG.childForceExpandWidth = false;
        rowsVLG.childForceExpandHeight = false;
        rowsVLG.spacing = 8f;
        rowsVLG.padding = new RectOffset(24, 24, 10, 10);

        CreateSectionHeader(rows.transform, "ACCESSIBILITY");
        CreateToggleRow(rows.transform, "ColourBlindRow", "Colour-blind mode");
        CreateToggleRow(rows.transform, "ReducedMotionRow", "Reduced motion");
        CreateStepperRow(rows.transform, "UiScaleRow", "Interface size");

        CreateSectionHeader(rows.transform, "CAMERA");
        CreateStepperRow(rows.transform, "PanRow", "Pan speed");
        CreateStepperRow(rows.transform, "OrbitRow", "Orbit speed");

        CreateSectionHeader(rows.transform, "GAMEPLAY");
        CreateToggleRow(rows.transform, "ConfirmRow", "Confirm tile changes");
        CreateSpeedRow(rows.transform);

        CreateSectionHeader(rows.transform, "SYSTEM");
        CreateToggleRow(rows.transform, "VSyncRow", "VSync");

        GameObject closeButton = CreateControlButton(panel.transform, "CloseButton", "Close", 130f, 32f);
        RectTransform closeRT = closeButton.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.5f, 0f);
        closeRT.anchorMax = new Vector2(0.5f, 0f);
        closeRT.pivot = new Vector2(0.5f, 0f);
        closeRT.anchoredPosition = new Vector2(0f, 14f);

        OptionsMenuController controller = screen.AddComponent<OptionsMenuController>();

        Transform optionsButton = FindDeep(canvas.transform, "Options");
        if (optionsButton != null)
        {
            Button button = optionsButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(controller.Open);
            }
        }

        return screen;
    }

    private static void CreateSectionHeader(Transform parent, string text)
    {
        GameObject header = CreateTMP(text + "Header", parent, text, 12, FontStyles.Bold);
        header.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        header.GetComponent<TextMeshProUGUI>().color = MutedTextColor;
        header.AddComponent<LayoutElement>().preferredHeight = 20f;
    }

    private static GameObject CreateRow(Transform parent, string name)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8f;
        row.GetComponent<LayoutElement>().preferredHeight = RowHeight;
        return row;
    }

    private static void CreateRowLabel(Transform row, string text)
    {
        GameObject label = CreateTMP("Label", row, text, 15, FontStyles.Normal);
        label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        label.AddComponent<LayoutElement>().preferredWidth = LabelWidth;
    }

    private static void CreateSpacer(Transform row)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(row, false);
        LayoutElement le = spacer.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.preferredHeight = 0f;
    }

    private static void CreateToggleRow(Transform parent, string name, string label)
    {
        GameObject row = CreateRow(parent, name);
        CreateRowLabel(row.transform, label);
        CreateSpacer(row.transform);

        GameObject toggle = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggle.transform.SetParent(row.transform, false);
        toggle.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.41f);
        toggle.AddComponent<LayoutElement>().preferredWidth = 52f;
        Toggle toggleComponent = toggle.GetComponent<Toggle>();
        toggleComponent.transition = Toggle.Transition.None;
        toggleComponent.targetGraphic = toggle.GetComponent<Image>();
        toggleComponent.graphic = null;

        GameObject knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knob.transform.SetParent(toggle.transform, false);
        knob.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
        RectTransform knobRT = knob.GetComponent<RectTransform>();
        knobRT.anchorMin = new Vector2(0.5f, 0.5f);
        knobRT.anchorMax = new Vector2(0.5f, 0.5f);
        knobRT.pivot = new Vector2(0.5f, 0.5f);
        knobRT.sizeDelta = new Vector2(20f, 20f);
        knobRT.anchoredPosition = new Vector2(-12f, 0f);
    }

    private static void CreateStepperRow(Transform parent, string name, string label)
    {
        GameObject row = CreateRow(parent, name);
        CreateRowLabel(row.transform, label);
        CreateSpacer(row.transform);
        CreateControlButton(row.transform, "Minus", "\u2212", 34f, 30f);
        CreateValue(row.transform);
        CreateControlButton(row.transform, "Plus", "+", 34f, 30f);
    }

    private static void CreateSpeedRow(Transform parent)
    {
        GameObject row = CreateRow(parent, "SpeedRow");
        CreateRowLabel(row.transform, "Execution speed");
        CreateSpacer(row.transform);
        CreateControlButton(row.transform, "SpeedButton", "1x", 80f, 30f);
    }

    private static void CreateValue(Transform row)
    {
        GameObject box = new GameObject("Value", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(row.transform, false);
        box.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);
        LayoutElement boxLE = box.AddComponent<LayoutElement>();
        boxLE.preferredWidth = 70f;
        boxLE.preferredHeight = 30f;

        GameObject text = CreateTMP("Text", box.transform, "", 14, FontStyles.Bold);
        Stretch(text.GetComponent<RectTransform>());
    }

    private static GameObject CreateControlButton(Transform parent, string name, string label, float width, float height)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
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
        LayoutElement le = button.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;

        GameObject text = CreateTMP("Label", button.transform, label, 14, FontStyles.Bold);
        Stretch(text.GetComponent<RectTransform>());
        return button;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(fontSize * GameOptions.UiScale));
        tmp.fontStyle = style;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().preferredHeight = tmp.fontSize * 1.4f;
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
