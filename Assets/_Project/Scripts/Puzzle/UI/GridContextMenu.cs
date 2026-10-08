// GridContextMenu - generic right-click context menu (used by InfoTab row/column
// headers): label/action entries near the cursor; any click outside closes it.
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GridContextMenu
{
    // Current menu instance (only one at a time).
    private static GameObject menuRoot;

    // Build the menu at the screen position, clamped to stay on-canvas.
    public static void Show(Vector2 screenPosition, (string Label, Action Action)[] entries)
    {
        Close();

        Canvas canvas = UIRoot.FindSceneCanvas();
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

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        ChamferedImage panelChamfer = panel.AddComponent<ChamferedImage>();
        panelChamfer.Chamfer = 12f;
        panelChamfer.color = HexlinkTheme.Border;
        panelChamfer.raycastTarget = true;
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
        inner.GetComponent<Image>().color = HexlinkTheme.Panel;
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
        ThemeSwitcher.ApplyToSubtree(root.transform);
    }

    public static void Close()
    {
        if (menuRoot != null)
        {
            UnityEngine.Object.Destroy(menuRoot);
            menuRoot = null;
        }
    }

    // One menu button: closes the menu, then runs the action.
    private static void CreateEntry(Transform parent, string label, Action action)
    {
        GameObject button = new GameObject("Entry_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        button.transform.SetParent(parent, false);
            Image image = button.GetComponent<Image>();
            image.color = HexlinkTheme.Ghost;
        button.GetComponent<LayoutElement>().preferredHeight = 24f;

        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        buttonComponent.transition = Button.Transition.ColorTint;
        HexlinkTheme.ApplyHoverTint(buttonComponent);
        buttonComponent.onClick.AddListener(() => { Close(); action(); });

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
            tmp.fontSize = 13;
            tmp.color = HexlinkTheme.TextLight;
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
