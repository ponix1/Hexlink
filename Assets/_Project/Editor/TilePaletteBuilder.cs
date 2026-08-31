using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public class TilePaletteBuilder
{
    private const float PanelWidth = 150f;
    private const float PanelHeight = 440f;
    private const float CellSize = 40f;
    private const float GridPadding = 4f;
    private const float LabelHeight = 22f;

    [MenuItem("Hexlink/Build Tile Palette")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        TileInventoryUI inventoryUI = Object.FindFirstObjectByType<TileInventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("No TileInventoryUI found in scene.");
            return;
        }

        // Preserve the user's current TilePalette placement across rebuilds, then remove
        // old palette objects (catches "(Clone)"/"(1)" duplicates from manual rearranging).
        RectTransform previousPalette = canvas.transform.Find("TilePalette") as RectTransform;
        Vector2 savedAnchorMin = previousPalette != null ? previousPalette.anchorMin : new Vector2(0f, 1f);
        Vector2 savedAnchorMax = previousPalette != null ? previousPalette.anchorMax : new Vector2(0f, 1f);
        Vector2 savedPivot = previousPalette != null ? previousPalette.pivot : new Vector2(0f, 1f);
        Vector2 savedPosition = previousPalette != null ? previousPalette.anchoredPosition : new Vector2(20f, -20f);
        Vector2 savedSize = previousPalette != null ? previousPalette.sizeDelta : new Vector2(PanelWidth, PanelHeight);
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name == "TilePalette" || child.name.StartsWith("TilePalette ("))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
        DestroyIfExists(canvas.transform, "TilePanel");
        DestroyIfExists(canvas.transform, "TilePanel (1)");
        DestroyIfExists(canvas.transform, "TilePanel (2)");
        DestroyIfExists(canvas.transform, "ToggleTabsButton");

        // --- TilePalette root: black border layer, anchored top-left, inset from the edge. ---
        GameObject palette = new GameObject("TilePalette", typeof(RectTransform), typeof(Image));
        palette.transform.SetParent(canvas.transform, false);
        palette.GetComponent<Image>().color = Color.black;
        RectTransform paletteRT = palette.GetComponent<RectTransform>();
        paletteRT.anchorMin = savedAnchorMin;
        paletteRT.anchorMax = savedAnchorMax;
        paletteRT.pivot = savedPivot;
        paletteRT.anchoredPosition = savedPosition;
        paletteRT.sizeDelta = savedSize;

        // --- Inner white panel (border effect, matching InfoTab style). ---
        GameObject inner = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(palette.transform, false);
        inner.GetComponent<Image>().color = Color.white;
        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(3f, 3f);
        innerRT.offsetMax = new Vector2(-3f, -3f);

        // --- CollapseButton: top-left corner strip, identical mechanism to the InfoTab's. ---
        GameObject collapseButton = new GameObject("CollapseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        collapseButton.transform.SetParent(palette.transform, false);
        Image collapseImage = collapseButton.GetComponent<Image>();
        collapseImage.color = Color.white;
        Button collapseBtn = collapseButton.GetComponent<Button>();
        collapseBtn.targetGraphic = collapseImage;
        RectTransform collapseRT = collapseButton.GetComponent<RectTransform>();
        collapseRT.anchorMin = new Vector2(0f, 1f);
        collapseRT.anchorMax = new Vector2(0f, 1f);
        collapseRT.pivot = new Vector2(0f, 1f);
        collapseRT.anchoredPosition = new Vector2(6f, 24f);
        collapseRT.sizeDelta = new Vector2(90f, 22f);
        GameObject collapseText = CreateTMP("Label", collapseButton.transform, "Hide", 13, FontStyles.Bold);
        StretchFull(collapseText.GetComponent<RectTransform>());

        // --- PaletteScroll: vertical-only scroll area, below the collapse strip. ---
        GameObject scroll = new GameObject("PaletteScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scroll.transform.SetParent(inner.transform, false);
        scroll.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(2f, 22f);
        scrollRT.offsetMax = new Vector2(-2f, -6f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scroll.transform, false);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        StretchFull(viewport.GetComponent<RectTransform>());

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup contentVLG = content.GetComponent<VerticalLayoutGroup>();
        contentVLG.childAlignment = TextAnchor.UpperCenter;
        contentVLG.childForceExpandWidth = false;
        contentVLG.childForceExpandHeight = false;
        contentVLG.childControlWidth = false;
        contentVLG.childControlHeight = false;
        contentVLG.spacing = 8f;
        ContentSizeFitter contentCSF = content.GetComponent<ContentSizeFitter>();
        contentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // --- Sections (explicit sizes — no nested width-control chains). ---
        BuildSection(content.transform, "Numbers", inventoryUI,
            new string[][] { new[] {"0","0"}, new[] {"1","1"}, new[] {"2","2"}, new[] {"3","3"}, new[] {"4","4"},
                             new[] {"5","5"}, new[] {"6","6"}, new[] {"7","7"}, new[] {"8","8"}, new[] {"9","9"} });
        BuildSection(content.transform, "Operations", inventoryUI,
            new string[][] { new[] {"+","+"}, new[] {"-","-"}, new[] {"\u00D7","*"}, new[] {"\u00F7","/"}, new[] {"^","^"}, new[] {"!","!"}, new[] {"\u221A","\u221A"} });
        BuildSection(content.transform, "Special", inventoryUI,
            new string[][] { new[] {"_","_"} });

        // --- SelectedText: shows the current palette pick, pinned to the panel bottom. ---
        GameObject selectedText = CreateTMP("SelectedText", inner.transform, "Selected: none", 12, FontStyles.Normal);
        RectTransform selectedRT = selectedText.GetComponent<RectTransform>();
        selectedRT.anchorMin = new Vector2(0f, 0f);
        selectedRT.anchorMax = new Vector2(1f, 0f);
        selectedRT.pivot = new Vector2(0.5f, 0f);
        selectedRT.anchoredPosition = new Vector2(0f, 3f);
        selectedRT.sizeDelta = new Vector2(0f, 16f);

        // --- Wire TileInventoryUI so the existing infrastructure keeps working. ---
        SerializedObject invSO = new SerializedObject(inventoryUI);
        invSO.FindProperty("collapsiblePanel").objectReferenceValue = palette;
        invSO.FindProperty("currentSelectionText").objectReferenceValue = selectedText.GetComponent<TextMeshProUGUI>();
        invSO.ApplyModifiedProperties();

        TilePaletteController controller = palette.AddComponent<TilePaletteController>();
        SerializedObject ctrlSO = new SerializedObject(controller);
        ctrlSO.FindProperty("collapseButton").objectReferenceValue = collapseBtn;
        ctrlSO.FindProperty("collapseLabel").objectReferenceValue = collapseText.GetComponent<TextMeshProUGUI>();
        ctrlSO.ApplyModifiedProperties();

        Selection.activeGameObject = palette;
        Debug.Log("TilePalette built: vertical top-left panel, scrollable Numbers / Operations / Special sections, " +
                   "InfoTab-style corner Hide/Show collapse button.");
    }

    private static void BuildSection(Transform content, string title, TileInventoryUI inventoryUI, string[][] tiles)
    {
        float innerWidth = PanelWidth - 10f;
        int columns = 3;
        int rows = Mathf.CeilToInt(tiles.Length / (float)columns);
        float gridWidth = GridPadding * 2f + columns * CellSize + (columns - 1) * GridPadding;
        float gridHeight = GridPadding * 2f + rows * CellSize + (rows - 1) * GridPadding;

        GameObject section = new GameObject(title + "Section", typeof(RectTransform), typeof(VerticalLayoutGroup));
        section.transform.SetParent(content, false);
        RectTransform sectionRT = section.GetComponent<RectTransform>();
        sectionRT.sizeDelta = new Vector2(innerWidth, LabelHeight + 4f + gridHeight);
        VerticalLayoutGroup sectionVLG = section.GetComponent<VerticalLayoutGroup>();
        sectionVLG.childAlignment = TextAnchor.UpperCenter;
        sectionVLG.childForceExpandWidth = false;
        sectionVLG.childForceExpandHeight = false;
        sectionVLG.childControlWidth = false;
        sectionVLG.childControlHeight = false;
        sectionVLG.spacing = 4f;

        GameObject sectionLabel = CreateTMP("SectionLabel", section.transform, title, 13, FontStyles.Bold);
        sectionLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(innerWidth, LabelHeight);

        GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(section.transform, false);
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(gridWidth, gridHeight);
        GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(CellSize, CellSize);
        glg.spacing = new Vector2(GridPadding, GridPadding);
        glg.padding = new RectOffset(4, 4, 4, 4);
        glg.childAlignment = TextAnchor.UpperLeft;

        foreach (string[] tile in tiles)
        {
            CreateTileButton(grid.transform, tile[0], tile[1], inventoryUI);
        }
    }

    private static void CreateTileButton(Transform parent, string display, string symbol, TileInventoryUI inventoryUI)
    {
        GameObject button = new GameObject("Tile_" + display, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>();
        image.color = Color.white;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        GameObject label = CreateTMP("Label", button.transform, display, 16, FontStyles.Bold);
        StretchFull(label.GetComponent<RectTransform>());

        // Persistent listener so it survives scene save; runtime AddListener would not.
        UnityEventTools.AddStringPersistentListener(buttonComponent.onClick, inventoryUI.SelectTile, symbol);
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().preferredHeight = fontSize * 1.4f;
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void DestroyIfExists(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }
}
