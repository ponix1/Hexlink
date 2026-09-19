using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GridContextMenu
{
    private static GameObject menuRoot;

    public static void Show(Vector2 screenPosition, (string Label, Action Action)[] entries)
    {
        Close();

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || entries == null || entries.Length == 0) return;

        GameObject root = new GameObject("GridContextMenu");
        root.transform.SetParent(canvas.transform, false);
        StretchFull(root.AddComponent<RectTransform>());

        // Invisible full-screen catch: any click outside the menu closes it and
        // is swallowed so it can't land on the board behind.
        GameObject blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
        blocker.transform.SetParent(root.transform, false);
        StretchFull(blocker.GetComponent<RectTransform>());
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        Button blockerButton = blocker.GetComponent<Button>();
        blockerButton.transition = Button.Transition.None;
        blockerButton.onClick.AddListener(Close);

        float width = 150f;
        float entryHeight = 26f;
        float height = entries.Length * entryHeight + 8f;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        panel.GetComponent<Image>().color = new Color(0.106f, 0.114f, 0.137f);
        RectTransform panelRT = panel.GetComponent<RectTransform>();

        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, screenPosition, camera, out Vector2 localPoint);

        RectTransform canvasRT = (RectTransform)canvas.transform;
        Vector2 canvasSize = canvasRT.rect.size;
        Vector2 anchored = localPoint + new Vector2(4f, -4f);
        anchored.x = Mathf.Clamp(anchored.x, -canvasSize.x / 2f + width / 2f, canvasSize.x / 2f - width / 2f);
        anchored.y = Mathf.Clamp(anchored.y, -canvasSize.y / 2f + height / 2f, canvasSize.y / 2f - height / 2f);

        panelRT.localPosition = anchored;
        panelRT.sizeDelta = new Vector2(width, height);
        panelRT.pivot = new Vector2(0.5f, 0.5f);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        inner.transform.SetParent(panel.transform, false);
        inner.GetComponent<Image>().color = new Color(0.165f, 0.176f, 0.204f);
        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(1f, 1f);
        innerRT.offsetMax = new Vector2(-1f, -1f);

        VerticalLayoutGroup layout = inner.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        layout.padding = new RectOffset(2, 2, 3, 3);

        foreach ((string label, Action action) in entries)
        {
            CreateEntry(inner.transform, label, action);
        }

        menuRoot = root;
    }

    public static void Close()
    {
        if (menuRoot != null)
        {
            UnityEngine.Object.Destroy(menuRoot);
            menuRoot = null;
        }
    }

    private static void CreateEntry(Transform parent, string label, Action action)
    {
        GameObject button = new GameObject("Entry_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>();
        image.color = new Color(0.208f, 0.220f, 0.247f);
        button.GetComponent<LayoutElement>().preferredHeight = 24f;

        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        ColorBlock colors = buttonComponent.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        buttonComponent.colors = colors;
        buttonComponent.onClick.AddListener(() => { Close(); action(); });

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13;
        tmp.color = new Color(0.910f, 0.918f, 0.929f);
        tmp.alignment = TextAlignmentOptions.Midline;
        RectTransform textRT = text.GetComponent<RectTransform>();
        StretchFull(textRT);
        textRT.offsetMin = new Vector2(8f, 0f);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
