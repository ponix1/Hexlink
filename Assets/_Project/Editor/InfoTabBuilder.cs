using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class InfoTabBuilder
{
    // Shared sizing so header numbers line up with the cells beneath them.
    // Chip/cell metrics derive from the Interface size option so instruction
    // chips scale up or down with the rest of the UI on rebuild.
    private const float RowLabelWidth = 40f;

    private static float ChipSize = 30f;
    private static float CellWidth = 136f;
    private static float CellHeight = 74f;

    private static readonly Color BorderColor = new Color(0.071f, 0.075f, 0.090f);
    private static readonly Color PanelColor = new Color(0.118f, 0.125f, 0.149f);
    private static readonly Color StripColor = new Color(0.138f, 0.145f, 0.184f);
    private static readonly Color CellFrameColor = new Color(0.106f, 0.114f, 0.137f);
    private static readonly Color CellFillColor = new Color(0.165f, 0.176f, 0.204f);
    private static readonly Color DividerColor = new Color(0.227f, 0.247f, 0.278f);
    private static readonly Color TextLightColor = new Color(0.910f, 0.918f, 0.929f);
    private static readonly Color ChipTextColor = new Color(0.961f, 0.965f, 0.973f);
    private static readonly Color TextGrayColor = new Color(0.604f, 0.627f, 0.667f);
    private static readonly Color AccentColor    = new Color(0.486f, 0.616f, 0.651f);
    private static readonly Color GhostButtonColor = new Color(0.165f, 0.176f, 0.204f);

    [MenuItem("Hexlink/Build Info Tab")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        ChipSize = Mathf.Max(24f, Mathf.Round(30f * GameOptions.UiScale));
        CellWidth = Mathf.Ceil(ChipSize * 4f + 16f);
        CellHeight = Mathf.Ceil(ChipSize * 2f + 14f);

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
        infoTab.GetComponent<Image>().color = BorderColor;

        // --- Content (inset layer) ---
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
        content.transform.SetParent(infoTab.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(4f, 4f);
        contentRT.offsetMax = new Vector2(-4f, -4f);
        content.GetComponent<Image>().color = PanelColor;

        // --- GridPanel: the 2D scrollable grid (processors x time-step columns).
        // This is now the ONLY thing inside Content â€” it fills the entire tab,
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
        // horizontally as columns are added, so this container must NOT force-expand child width â€”
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

        // --- ColumnHeaderRow: ACTIVE (not a template) â€” the numbers-across-the-top row (1, 2, 3...).
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
        headerCellLE.preferredWidth = CellWidth;
        headerCellLE.preferredHeight = 24f;
        GameObject headerNumberText = CreateTMP("NumberText", headerCellTemplate.transform, "1", 14, FontStyles.Normal);
        TextMeshProUGUI headerTMP = headerNumberText.GetComponent<TextMeshProUGUI>();
        headerTMP.alignment = TextAlignmentOptions.Center;
        headerTMP.color = TextGrayColor;
        RectTransform headerNumberRT = headerNumberText.GetComponent<RectTransform>();
        headerNumberRT.anchorMin = Vector2.zero;
        headerNumberRT.anchorMax = Vector2.one;
        headerNumberRT.offsetMin = Vector2.zero;
        headerNumberRT.offsetMax = Vector2.zero;
        headerCellTemplate.SetActive(false);

        // --- ProcessorRowTemplate: disabled stamp, duplicated per processor. Contains only the
        // row label + empty HorizontalLayoutGroup â€” cells are appended into it separately at
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
        rowTemplate.GetComponent<LayoutElement>().preferredHeight = CellHeight;

        GameObject rowLabel = CreateTMP("RowLabel", rowTemplate.transform, "P1", 14, FontStyles.Normal);
        rowLabel.GetComponent<TextMeshProUGUI>().color = TextGrayColor;
        LayoutElement rowLabelLE = rowLabel.GetComponent<LayoutElement>();
        rowLabelLE.preferredWidth = RowLabelWidth;
        rowLabelLE.preferredHeight = CellHeight;

        rowTemplate.SetActive(false);

        // --- CellTemplate: disabled stamp, duplicated into a ProcessorRowTemplate copy for every
        // (processor, column) pair that needs one. Holds at most ONE instruction, rendered as a
        // token sequence (e.g. [square "4"][arrow][empty square]) inside InstructionContainer.
        GameObject cellTemplate = new GameObject("CellTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cellTemplate.transform.SetParent(gridContent.transform, false);
        Image cellImage = cellTemplate.GetComponent<Image>();
        cellImage.color = CellFrameColor;
        Button cellButton = cellTemplate.GetComponent<Button>();
        cellButton.targetGraphic = cellImage;
        cellButton.transition = Button.Transition.None;
        LayoutElement cellLE = cellTemplate.GetComponent<LayoutElement>();
        cellLE.preferredWidth = CellWidth;
        cellLE.preferredHeight = CellHeight;

        // Fill: inset background layer. The root image acts as the state outline frame
        // (2px visible ring); runtime code colours both via GridController.ApplyCellVisual.
        GameObject cellFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        cellFill.transform.SetParent(cellTemplate.transform, false);
        cellFill.GetComponent<Image>().color = CellFillColor;
        RectTransform cellFillRT = cellFill.GetComponent<RectTransform>();
        cellFillRT.anchorMin = Vector2.zero;
        cellFillRT.anchorMax = Vector2.one;
        cellFillRT.offsetMin = new Vector2(2f, 2f);
        cellFillRT.offsetMax = new Vector2(-2f, -2f);

        // Empty â€” runtime code appends instruction token rows here (each row a horizontal
        // strip of chips, wrapping to a new row after four chips).
        GameObject instructionContainer = new GameObject("InstructionContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        instructionContainer.transform.SetParent(cellTemplate.transform, false);
        VerticalLayoutGroup instrVLG = instructionContainer.GetComponent<VerticalLayoutGroup>();
        instrVLG.childAlignment = TextAnchor.MiddleCenter;
        instrVLG.childForceExpandWidth = false;
        instrVLG.childForceExpandHeight = false;
        instrVLG.childControlWidth = true;
        instrVLG.childControlHeight = true;
        instrVLG.spacing = 2f;
        RectTransform instrRT = instructionContainer.GetComponent<RectTransform>();
        instrRT.anchorMin = Vector2.zero;
        instrRT.anchorMax = Vector2.one;
        instrRT.offsetMin = new Vector2(3f, 3f);
        instrRT.offsetMax = new Vector2(-3f, -3f);

        // Subtle column divider on the right edge.
        GameObject cellRightBorder = new GameObject("RightBorder", typeof(RectTransform), typeof(Image));
        cellRightBorder.transform.SetParent(cellTemplate.transform, false);
        cellRightBorder.GetComponent<Image>().color = DividerColor;
        RectTransform borderRT = cellRightBorder.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(1f, 0f);
        borderRT.anchorMax = new Vector2(1f, 1f);
        borderRT.pivot = new Vector2(1f, 0.5f);
        borderRT.sizeDelta = new Vector2(1f, 0f);
        borderRT.anchoredPosition = Vector2.zero;

        cellTemplate.SetActive(false);

        // --- SquareTokenTemplate: disabled stamp, duplicated per instruction operand.
        // Small white square + number, carries a HexCoord for hover-highlighting the 3D tile.
        GameObject squareTokenTemplate = new GameObject("SquareTokenTemplate", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        squareTokenTemplate.transform.SetParent(gridContent.transform, false);
        squareTokenTemplate.GetComponent<Image>().color = CellFillColor;
        LayoutElement squareTokenLE = squareTokenTemplate.GetComponent<LayoutElement>();
        squareTokenLE.preferredWidth = ChipSize;
        squareTokenLE.preferredHeight = ChipSize;
        GameObject squareTokenText = CreateTMP("NumberText", squareTokenTemplate.transform, "0", 17, FontStyles.Bold);
        TextMeshProUGUI squareTokenTMP = squareTokenText.GetComponent<TextMeshProUGUI>();
        squareTokenTMP.alignment = TextAlignmentOptions.Center;
        squareTokenTMP.color = ChipTextColor;
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
        arrowTokenLE.preferredWidth = ChipSize;
        arrowTokenLE.preferredHeight = ChipSize;
        GameObject arrowTokenText = CreateTMP("ArrowText", arrowTokenTemplate.transform, "\u2192", 17, FontStyles.Normal);
        TextMeshProUGUI arrowTokenTMP = arrowTokenText.GetComponent<TextMeshProUGUI>();
        arrowTokenTMP.alignment = TextAlignmentOptions.Center;
        arrowTokenTMP.color = TextGrayColor;
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
        collapseImage.color = GhostButtonColor;
        Button collapseBtn = collapseButton.GetComponent<Button>();
        collapseBtn.targetGraphic = collapseImage;
        ApplyHoverTint(collapseBtn);
        RectTransform collapseRT = collapseButton.GetComponent<RectTransform>();
        collapseRT.anchorMin = new Vector2(0f, 1f);
        collapseRT.anchorMax = new Vector2(0f, 1f);
        collapseRT.pivot = new Vector2(0f, 1f);
        collapseRT.anchoredPosition = new Vector2(6f, 24f);
        collapseRT.sizeDelta = new Vector2(90f, 22f);
        GameObject collapseText = CreateTMP("Label", collapseButton.transform, "Hide", 13, FontStyles.Bold);
        collapseText.GetComponent<TextMeshProUGUI>().color = TextLightColor;
        TextMeshProUGUI collapseTMP = collapseText.GetComponent<TextMeshProUGUI>();
        collapseTMP.alignment = TextAlignmentOptions.Center;
        RectTransform collapseTextRT = collapseText.GetComponent<RectTransform>();
        collapseTextRT.anchorMin = Vector2.zero;
        collapseTextRT.anchorMax = Vector2.one;
        collapseTextRT.offsetMin = Vector2.zero;
        collapseTextRT.offsetMax = Vector2.zero;

        // --- Control strip: dedicated button column on the tab's right edge. ---
        GameObject controlStrip = new GameObject("ControlStrip", typeof(RectTransform), typeof(Image));
        controlStrip.transform.SetParent(infoTab.transform, false);
        controlStrip.GetComponent<Image>().color = StripColor;
        RectTransform stripRT = controlStrip.GetComponent<RectTransform>();
        stripRT.anchorMin = new Vector2(1f, 0f);
        stripRT.anchorMax = new Vector2(1f, 1f);
        stripRT.pivot = new Vector2(1f, 0.5f);
        stripRT.anchoredPosition = Vector2.zero;
        stripRT.sizeDelta = new Vector2(320f, 0f);

        // --- MetricsPanel: live instruction/cycle/processor counts + personal bests,
        // pinned to the top of the strip (mirroring the Hide button's corner placement). ---
        GameObject metricsPanel = new GameObject("MetricsPanel", typeof(RectTransform), typeof(Image));
        metricsPanel.transform.SetParent(controlStrip.transform, false);
        metricsPanel.GetComponent<Image>().color = new Color(0.125f, 0.133f, 0.165f);
        RectTransform metricsRT = metricsPanel.GetComponent<RectTransform>();
        metricsRT.anchorMin = new Vector2(0.5f, 1f);
        metricsRT.anchorMax = new Vector2(0.5f, 1f);
        metricsRT.pivot = new Vector2(0.5f, 1f);
        metricsRT.anchoredPosition = new Vector2(0f, -8f);
        metricsRT.sizeDelta = new Vector2(300f, 44f);

        GameObject liveMetricsLabel = CreateTMP("LiveLabel", metricsPanel.transform, "Instr 0  \u00B7  Cycles 0  \u00B7  Procs 0", 14, FontStyles.Bold);
        liveMetricsLabel.GetComponent<TextMeshProUGUI>().color = TextLightColor;
        RectTransform liveMetricsRT = liveMetricsLabel.GetComponent<RectTransform>();
        liveMetricsRT.anchorMin = new Vector2(0f, 1f);
        liveMetricsRT.anchorMax = new Vector2(1f, 1f);
        liveMetricsRT.pivot = new Vector2(0.5f, 1f);
        liveMetricsRT.anchoredPosition = new Vector2(0f, -5f);
        liveMetricsRT.sizeDelta = new Vector2(-8f, 20f);

        GameObject bestMetricsLabel = CreateTMP("BestLabel", metricsPanel.transform, "Best \u2014  \u00B7  \u2014  \u00B7  \u2014", 12, FontStyles.Normal);
        bestMetricsLabel.GetComponent<TextMeshProUGUI>().color = TextGrayColor;
        RectTransform bestMetricsRT = bestMetricsLabel.GetComponent<RectTransform>();
        bestMetricsRT.anchorMin = new Vector2(0f, 1f);
        bestMetricsRT.anchorMax = new Vector2(1f, 1f);
        bestMetricsRT.pivot = new Vector2(0.5f, 1f);
        bestMetricsRT.anchoredPosition = new Vector2(0f, -25f);
        bestMetricsRT.sizeDelta = new Vector2(-8f, 18f);

        GameObject playButton = CreateStripButton(controlStrip.transform, "PlayButton", "Play", -56f, true);
        GameObject pauseButton = CreateStripButton(controlStrip.transform, "PauseButton", "Pause", -100f, false);
        GameObject stepButton = CreateStripButton(controlStrip.transform, "StepButton", "Step", -144f, false);
        GameObject resetButton = CreateStripButton(controlStrip.transform, "ResetButton", "Reset", -188f, false);

        GameObject speedLabel = CreateTMP("SpeedLabel", controlStrip.transform, "1.0x", 12, FontStyles.Bold);
        speedLabel.GetComponent<TextMeshProUGUI>().color = TextGrayColor;
        RectTransform speedLabelRT = speedLabel.GetComponent<RectTransform>();
        speedLabelRT.anchorMin = new Vector2(0.5f, 0f);
        speedLabelRT.anchorMax = new Vector2(0.5f, 0f);
        speedLabelRT.pivot = new Vector2(0.5f, 0f);
        speedLabelRT.anchoredPosition = new Vector2(0f, 40f);
        speedLabelRT.sizeDelta = new Vector2(300f, 16f);

        GameObject speedSlider = CreateSlider(controlStrip.transform);
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

        InfoTabMetrics metrics = infoTab.AddComponent<InfoTabMetrics>();
        SerializedObject metricsSO = new SerializedObject(metrics);
        metricsSO.FindProperty("gridController").objectReferenceValue = gridController;
        metricsSO.FindProperty("liveLabel").objectReferenceValue = liveMetricsLabel.GetComponent<TextMeshProUGUI>();
        metricsSO.FindProperty("bestLabel").objectReferenceValue = bestMetricsLabel.GetComponent<TextMeshProUGUI>();
        metricsSO.ApplyModifiedProperties();

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

        ExecutionEngine engine = infoTab.AddComponent<ExecutionEngine>();
        SerializedObject engineSO = new SerializedObject(engine);
        engineSO.FindProperty("gridController").objectReferenceValue = gridController;
        HexTileLabelController labelController = Object.FindFirstObjectByType<HexTileLabelController>();
        engineSO.FindProperty("labelController").objectReferenceValue = labelController;
        engineSO.FindProperty("hexGridSpawner").objectReferenceValue = Object.FindFirstObjectByType<HexGridSpawner>();
        engineSO.FindProperty("playButton").objectReferenceValue = playButton.GetComponent<Button>();
        engineSO.FindProperty("pauseButton").objectReferenceValue = pauseButton.GetComponent<Button>();
        engineSO.FindProperty("stepButton").objectReferenceValue = stepButton.GetComponent<Button>();
        engineSO.FindProperty("resetButton").objectReferenceValue = resetButton.GetComponent<Button>();
        engineSO.FindProperty("speedSlider").objectReferenceValue = speedSlider.GetComponent<Slider>();
        engineSO.FindProperty("speedLabel").objectReferenceValue = speedLabel.GetComponent<TextMeshProUGUI>();
        engineSO.FindProperty("inventoryUI").objectReferenceValue = Object.FindFirstObjectByType<TileInventoryUI>();
        engineSO.FindProperty("metricsDisplay").objectReferenceValue = metrics;
        engineSO.FindProperty("authoringController").objectReferenceValue = authoring;
        engineSO.FindProperty("nodePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Node.prefab");
        SerializedObject labelControllerSO = new SerializedObject(labelController);
        engineSO.FindProperty("tileLabelPrefab").objectReferenceValue = labelControllerSO.FindProperty("tileLabelPrefab").objectReferenceValue;
        engineSO.ApplyModifiedProperties();

        Selection.activeGameObject = infoTab;
        Debug.Log("InfoTab built successfully. GridPanel fills the entire tab. GridContent holds " +
                   "ColumnHeaderRow (active) plus HeaderCellTemplate / ProcessorRowTemplate / CellTemplate / " +
                   "SquareTokenTemplate / ArrowTokenTemplate (all disabled stamps) â€” instantiate these at runtime to grow the grid. " +
                   "GridController + InstructionAuthoringController + ExecutionEngine attached to InfoTab with references wired.");
    }

    private static GameObject CreateStripButton(Transform parent, string name, string label, float yOffset, bool primary)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>();
        image.color = primary ? AccentColor : GhostButtonColor;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        ApplyHoverTint(buttonComponent);

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta = new Vector2(300f, 38f);

        GameObject text = CreateTMP("Label", button.transform, label, 13, FontStyles.Bold);
        text.GetComponent<TextMeshProUGUI>().color = primary ? BorderColor : TextLightColor;
        StretchFull(text.GetComponent<RectTransform>());
        return button;
    }

    private static void ApplyHoverTint(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static GameObject CreateSlider(Transform parent)
    {
        GameObject sliderGO = new GameObject("SpeedSlider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0f);
        sliderRT.anchorMax = new Vector2(0.5f, 0f);
        sliderRT.pivot = new Vector2(0.5f, 0f);
        sliderRT.anchoredPosition = new Vector2(0f, 8f);
        sliderRT.sizeDelta = new Vector2(300f, 24f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGO.transform, false);
        background.GetComponent<Image>().color = new Color(0.204f, 0.220f, 0.247f);
        RectTransform backgroundRT = background.GetComponent<RectTransform>();
        backgroundRT.anchorMin = new Vector2(0f, 0.5f);
        backgroundRT.anchorMax = new Vector2(1f, 0.5f);
        backgroundRT.pivot = new Vector2(0.5f, 0.5f);
        backgroundRT.sizeDelta = new Vector2(0f, 8f);
        backgroundRT.anchoredPosition = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRT.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(-10f, 8f);
        fillAreaRT.anchoredPosition = Vector2.zero;

        // The Slider component drives this rect's anchorMax.x between 0 and 1 as the value changes.
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = AccentColor;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(8f, 0f);
        handleAreaRT.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        handle.GetComponent<Image>().color = new Color(0.780f, 0.804f, 0.839f);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 0.5f);
        handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(16f, 16f);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.25f;
        slider.maxValue = 3f;
        slider.value = 1f;

        return sliderGO;
    }

    private static GameObject CreateTabButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>();
        image.color = Color.white;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(90f, 22f);

        GameObject text = CreateTMP("Label", button.transform, label, 13, FontStyles.Bold);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        return button;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(fontSize * GameOptions.UiScale));
        tmp.fontStyle = style;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = tmp.fontSize * 1.4f;
        return go;
    }
}
