using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class InfoTabBuilder
{
    // Shared sizing so header numbers line up with the cells beneath them.
    private const float RowLabelWidth = 40f;
    private const float CellSize = 60f;

    [MenuItem("Hexlink/Build Info Tab")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        // Preserve the user's current InfoTab placement across rebuilds.
        RectTransform previousTab = canvas.transform.Find("InfoTab") as RectTransform;
        Vector2 savedAnchorMin = previousTab != null ? previousTab.anchorMin : new Vector2(0f, 0f);
        Vector2 savedAnchorMax = previousTab != null ? previousTab.anchorMax : new Vector2(1f, 0f);
        Vector2 savedPivot = previousTab != null ? previousTab.pivot : new Vector2(0.5f, 0f);
        Vector2 savedPosition = previousTab != null ? previousTab.anchoredPosition : Vector2.zero;
        Vector2 savedSize = previousTab != null ? previousTab.sizeDelta : new Vector2(0f, 220f);
        if (previousTab != null)
        {
            Object.DestroyImmediate(previousTab.gameObject);
        }

        // --- InfoTab (black border layer) ---
        GameObject infoTab = new GameObject("InfoTab", typeof(RectTransform), typeof(Image));
        infoTab.transform.SetParent(canvas.transform, false);
        RectTransform infoTabRT = infoTab.GetComponent<RectTransform>();
        infoTabRT.anchorMin = savedAnchorMin;
        infoTabRT.anchorMax = savedAnchorMax;
        infoTabRT.pivot = savedPivot;
        infoTabRT.anchoredPosition = savedPosition;
        infoTabRT.sizeDelta = savedSize;
        infoTab.GetComponent<Image>().color = Color.black;

        // --- Content (white inset layer) ---
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
        content.transform.SetParent(infoTab.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(4f, 4f);
        contentRT.offsetMax = new Vector2(-4f, -4f);
        content.GetComponent<Image>().color = Color.white;

        // --- GridPanel: the 2D scrollable grid (processors x time-step columns).
        // This is now the ONLY thing inside Content — it fills the entire tab,
        // matching the reference image (no side panel, no divider).
        GameObject gridPanel = new GameObject("GridPanel", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        gridPanel.transform.SetParent(content.transform, false);
        RectTransform gridPanelRT = gridPanel.GetComponent<RectTransform>();
        gridPanelRT.anchorMin = Vector2.zero;
        gridPanelRT.anchorMax = Vector2.one;
        gridPanelRT.offsetMin = new Vector2(10f, 10f);
        gridPanelRT.offsetMax = new Vector2(-10f, -10f);
        gridPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // transparent, needed for raycast area

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(gridPanel.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // near-invisible, required for Mask
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        // GridContent: rows stack vertically (one per processor). Each row's own width grows
        // horizontally as columns are added, so this container must NOT force-expand child width —
        // otherwise every row would stretch to the viewport width instead of hugging its own content,
        // which would break horizontal scrolling.
        GameObject gridContent = new GameObject("GridContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        gridContent.transform.SetParent(viewport.transform, false);
        RectTransform gridContentRT = gridContent.GetComponent<RectTransform>();
        gridContentRT.anchorMin = new Vector2(0f, 1f);
        gridContentRT.anchorMax = new Vector2(0f, 1f);
        gridContentRT.pivot = new Vector2(0f, 1f);

        VerticalLayoutGroup gridVLG = gridContent.GetComponent<VerticalLayoutGroup>();
        gridVLG.childAlignment = TextAnchor.UpperLeft;
        gridVLG.childForceExpandWidth = false;
        gridVLG.childForceExpandHeight = false;
        gridVLG.childControlWidth = true;
        gridVLG.childControlHeight = true;
        gridVLG.spacing = 2f;

        ContentSizeFitter gridCSF = gridContent.GetComponent<ContentSizeFitter>();
        gridCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        gridCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = gridPanel.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRT;
        scrollRect.content = gridContentRT;
        scrollRect.horizontal = true; // columns can extend rightward
        scrollRect.vertical = true;   // processor rows can extend downward
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // --- ColumnHeaderRow: ACTIVE (not a template) — the numbers-across-the-top row (1, 2, 3...).
        // Starts with just the leading spacer; runtime code appends a HeaderCellTemplate copy
        // per column, keeping it in sync whenever a new column is added to any processor row.
        GameObject headerRow = new GameObject("ColumnHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerRow.transform.SetParent(gridContent.transform, false);
        HorizontalLayoutGroup headerHLG = headerRow.GetComponent<HorizontalLayoutGroup>();
        headerHLG.childAlignment = TextAnchor.MiddleLeft;
        headerHLG.childForceExpandWidth = false;
        headerHLG.childForceExpandHeight = false;
        headerHLG.childControlWidth = true;
        headerHLG.childControlHeight = true;
        headerHLG.spacing = 2f;
        headerRow.GetComponent<LayoutElement>().preferredHeight = 24f;

        // Leading spacer so column 1's header sits above column 1's cells, not above the row labels.
        GameObject headerSpacer = new GameObject("HeaderSpacer", typeof(RectTransform), typeof(LayoutElement));
        headerSpacer.transform.SetParent(headerRow.transform, false);
        headerSpacer.GetComponent<LayoutElement>().preferredWidth = RowLabelWidth;

        // --- HeaderCellTemplate: disabled stamp, duplicated per column into ColumnHeaderRow. ---
        GameObject headerCellTemplate = new GameObject("HeaderCellTemplate", typeof(RectTransform), typeof(LayoutElement));
        headerCellTemplate.transform.SetParent(gridContent.transform, false);
        LayoutElement headerCellLE = headerCellTemplate.GetComponent<LayoutElement>();
        headerCellLE.preferredWidth = CellSize;
        headerCellLE.preferredHeight = 24f;
        GameObject headerNumberText = CreateTMP("NumberText", headerCellTemplate.transform, "1", 14, FontStyles.Normal);
        TextMeshProUGUI headerTMP = headerNumberText.GetComponent<TextMeshProUGUI>();
        headerTMP.alignment = TextAlignmentOptions.Center;
        RectTransform headerNumberRT = headerNumberText.GetComponent<RectTransform>();
        headerNumberRT.anchorMin = Vector2.zero;
        headerNumberRT.anchorMax = Vector2.one;
        headerNumberRT.offsetMin = Vector2.zero;
        headerNumberRT.offsetMax = Vector2.zero;
        headerCellTemplate.SetActive(false);

        // --- ProcessorRowTemplate: disabled stamp, duplicated per processor. Contains only the
        // row label + empty HorizontalLayoutGroup — cells are appended into it separately at
        // runtime (one CellTemplate copy per column), since column count varies per row/puzzle-state.
        GameObject rowTemplate = new GameObject("ProcessorRowTemplate", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowTemplate.transform.SetParent(gridContent.transform, false);
        HorizontalLayoutGroup rowHLG = rowTemplate.GetComponent<HorizontalLayoutGroup>();
        rowHLG.childAlignment = TextAnchor.MiddleLeft;
        rowHLG.childForceExpandWidth = false;
        rowHLG.childForceExpandHeight = false;
        rowHLG.childControlWidth = true;
        rowHLG.childControlHeight = true;
        rowHLG.spacing = 2f;
        rowTemplate.GetComponent<LayoutElement>().preferredHeight = CellSize;

        GameObject rowLabel = CreateTMP("RowLabel", rowTemplate.transform, "P1", 14, FontStyles.Normal);
        LayoutElement rowLabelLE = rowLabel.GetComponent<LayoutElement>();
        rowLabelLE.preferredWidth = RowLabelWidth;
        rowLabelLE.preferredHeight = CellSize;

        rowTemplate.SetActive(false);

        // --- CellTemplate: disabled stamp, duplicated into a ProcessorRowTemplate copy for every
        // (processor, column) pair that needs one. Holds at most ONE instruction, rendered as a
        // token sequence (e.g. [square "4"][arrow][empty square]) inside InstructionContainer.
        GameObject cellTemplate = new GameObject("CellTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cellTemplate.transform.SetParent(gridContent.transform, false);
        Image cellImage = cellTemplate.GetComponent<Image>();
        cellImage.color = Color.white;
        Button cellButton = cellTemplate.GetComponent<Button>();
        cellButton.targetGraphic = cellImage;
        LayoutElement cellLE = cellTemplate.GetComponent<LayoutElement>();
        cellLE.preferredWidth = CellSize;
        cellLE.preferredHeight = CellSize;

        // Empty — runtime code appends instruction token prefabs here (subject icon / arrow /
        // destination icon) once the player commits an instruction into this cell.
        GameObject instructionContainer = new GameObject("InstructionContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        instructionContainer.transform.SetParent(cellTemplate.transform, false);
        HorizontalLayoutGroup instrHLG = instructionContainer.GetComponent<HorizontalLayoutGroup>();
        instrHLG.childAlignment = TextAnchor.MiddleCenter;
        instrHLG.childForceExpandWidth = false;
        instrHLG.childForceExpandHeight = false;
        instrHLG.childControlWidth = true;
        instrHLG.childControlHeight = true;
        instrHLG.spacing = 2f;
        RectTransform instrRT = instructionContainer.GetComponent<RectTransform>();
        instrRT.anchorMin = Vector2.zero;
        instrRT.anchorMax = Vector2.one;
        instrRT.offsetMin = Vector2.zero;
        instrRT.offsetMax = Vector2.zero;

        // Right-edge divider, standing in for the dashed column rule in the reference image.
        // Solid for now — swap for a dashed sprite/texture later if you want the exact look.
        GameObject cellRightBorder = new GameObject("RightBorder", typeof(RectTransform), typeof(Image));
        cellRightBorder.transform.SetParent(cellTemplate.transform, false);
        cellRightBorder.GetComponent<Image>().color = Color.black;
        RectTransform borderRT = cellRightBorder.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(1f, 0f);
        borderRT.anchorMax = new Vector2(1f, 1f);
        borderRT.pivot = new Vector2(1f, 0.5f);
        borderRT.sizeDelta = new Vector2(2f, 0f);
        borderRT.anchoredPosition = Vector2.zero;

        cellTemplate.SetActive(false);

        // --- SquareTokenTemplate: disabled stamp, duplicated per instruction operand.
        // Small white square + number, carries a HexCoord for hover-highlighting the 3D tile.
        GameObject squareTokenTemplate = new GameObject("SquareTokenTemplate", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        squareTokenTemplate.transform.SetParent(gridContent.transform, false);
        squareTokenTemplate.GetComponent<Image>().color = Color.white;
        LayoutElement squareTokenLE = squareTokenTemplate.GetComponent<LayoutElement>();
        squareTokenLE.preferredWidth = 18f;
        squareTokenLE.preferredHeight = 18f;
        GameObject squareTokenText = CreateTMP("NumberText", squareTokenTemplate.transform, "0", 11, FontStyles.Bold);
        TextMeshProUGUI squareTokenTMP = squareTokenText.GetComponent<TextMeshProUGUI>();
        squareTokenTMP.alignment = TextAlignmentOptions.Center;
        RectTransform squareTokenTextRT = squareTokenText.GetComponent<RectTransform>();
        squareTokenTextRT.anchorMin = Vector2.zero;
        squareTokenTextRT.anchorMax = Vector2.one;
        squareTokenTextRT.offsetMin = Vector2.zero;
        squareTokenTextRT.offsetMax = Vector2.zero;
        InstructionToken squareTokenComponent = squareTokenTemplate.AddComponent<InstructionToken>();
        SerializedObject tokenSO = new SerializedObject(squareTokenComponent);
        tokenSO.FindProperty("label").objectReferenceValue = squareTokenTMP;
        tokenSO.ApplyModifiedProperties();
        squareTokenTemplate.SetActive(false);

        // --- ArrowTokenTemplate: disabled stamp, decorative "move" arrow between squares. ---
        GameObject arrowTokenTemplate = new GameObject("ArrowTokenTemplate", typeof(RectTransform), typeof(LayoutElement));
        arrowTokenTemplate.transform.SetParent(gridContent.transform, false);
        LayoutElement arrowTokenLE = arrowTokenTemplate.GetComponent<LayoutElement>();
        arrowTokenLE.preferredWidth = 18f;
        arrowTokenLE.preferredHeight = 18f;
        GameObject arrowTokenText = CreateTMP("ArrowText", arrowTokenTemplate.transform, "\u2192", 14, FontStyles.Normal);
        TextMeshProUGUI arrowTokenTMP = arrowTokenText.GetComponent<TextMeshProUGUI>();
        arrowTokenTMP.alignment = TextAlignmentOptions.Center;
        RectTransform arrowTokenTextRT = arrowTokenText.GetComponent<RectTransform>();
        arrowTokenTextRT.anchorMin = Vector2.zero;
        arrowTokenTextRT.anchorMax = Vector2.one;
        arrowTokenTextRT.offsetMin = Vector2.zero;
        arrowTokenTextRT.offsetMax = Vector2.zero;
        arrowTokenTemplate.SetActive(false);

        // --- CollapseButton: toggles the InfoTab between full height and a slim strip. ---
        GameObject collapseButton = new GameObject("CollapseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        collapseButton.transform.SetParent(infoTab.transform, false);
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
        TextMeshProUGUI collapseTMP = collapseText.GetComponent<TextMeshProUGUI>();
        collapseTMP.alignment = TextAlignmentOptions.Center;
        RectTransform collapseTextRT = collapseText.GetComponent<RectTransform>();
        collapseTextRT.anchorMin = Vector2.zero;
        collapseTextRT.anchorMax = Vector2.one;
        collapseTextRT.offsetMin = Vector2.zero;
        collapseTextRT.offsetMax = Vector2.zero;

        // --- Runtime controllers, wired entirely in code (Inspector wiring doesn't survive Instantiate). ---
        HexTileSelector selector = Object.FindFirstObjectByType<HexTileSelector>();
        if (selector != null)
        {
            SerializedObject selectorSO = new SerializedObject(selector);
            if (selectorSO.FindProperty("hexGridSpawner").objectReferenceValue == null)
            {
                selectorSO.FindProperty("hexGridSpawner").objectReferenceValue = Object.FindFirstObjectByType<HexGridSpawner>();
                selectorSO.ApplyModifiedProperties();
            }
        }

        GridController gridController = infoTab.AddComponent<GridController>();
        SerializedObject gridSO = new SerializedObject(gridController);
        gridSO.FindProperty("labelController").objectReferenceValue = Object.FindFirstObjectByType<HexTileLabelController>();
        gridSO.FindProperty("tileSelector").objectReferenceValue = selector;
        gridSO.FindProperty("inventoryUI").objectReferenceValue = Object.FindFirstObjectByType<TileInventoryUI>();
        gridSO.FindProperty("gridContent").objectReferenceValue = gridContentRT;
        gridSO.FindProperty("columnHeaderRow").objectReferenceValue = headerRow.GetComponent<RectTransform>();
        gridSO.FindProperty("processorRowTemplate").objectReferenceValue = rowTemplate;
        gridSO.FindProperty("cellTemplate").objectReferenceValue = cellTemplate;
        gridSO.FindProperty("headerCellTemplate").objectReferenceValue = headerCellTemplate;
        gridSO.FindProperty("squareTokenTemplate").objectReferenceValue = squareTokenTemplate;
        gridSO.FindProperty("arrowTokenTemplate").objectReferenceValue = arrowTokenTemplate;
        gridSO.FindProperty("collapseButton").objectReferenceValue = collapseButton;
        gridSO.ApplyModifiedProperties();

        InstructionAuthoringController authoring = infoTab.AddComponent<InstructionAuthoringController>();
        SerializedObject authoringSO = new SerializedObject(authoring);
        authoringSO.FindProperty("gridController").objectReferenceValue = gridController;
        authoringSO.FindProperty("labelController").objectReferenceValue = Object.FindFirstObjectByType<HexTileLabelController>();
        authoringSO.FindProperty("tileSelector").objectReferenceValue = selector;
        authoringSO.ApplyModifiedProperties();

        if (selector != null)
        {
            SerializedObject selectorSO2 = new SerializedObject(selector);
            selectorSO2.FindProperty("authoringController").objectReferenceValue = authoring;
            selectorSO2.ApplyModifiedProperties();
        }

        Selection.activeGameObject = infoTab;
        Debug.Log("InfoTab built successfully. GridPanel fills the entire tab. GridContent holds " +
                   "ColumnHeaderRow (active) plus HeaderCellTemplate / ProcessorRowTemplate / CellTemplate / " +
                   "SquareTokenTemplate / ArrowTokenTemplate (all disabled stamps) — instantiate these at runtime to grow the grid. " +
                   "GridController + InstructionAuthoringController attached to InfoTab with references wired.");
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
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.4f;
        return go;
    }
}