using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SolutionPickerPopup
{
    private const float PanelWidth = 940f;
    private const float PanelHeight = 520f;
    private const float TileWidth = 420f;
    private const float TileHeight = 96f;

    private static readonly Color SelectedFillColor = new Color(0.360f, 0.380f, 0.440f);
    private static readonly Color SolvedGreen = new Color(0.30f, 0.78f, 0.47f);

    private const string GrayHex = "ADB4BE";
    private const string LightHex = "E8EAED";

    private static PuzzleData puzzle;
    private static System.Action<int> onOpen;
    private static System.Action onNew;
    private static GameObject root;
    private static readonly List<SavedSolution> entries = new List<SavedSolution>();
    private static int selectedIndex = -1;
    private static float deleteArmedAt = -100f;

    private static Transform gridContent;
    private static Button openButton;
    private static Button renameButton;
    private static Button deleteButton;
    private static TextMeshProUGUI deleteLabel;
    private static TextMeshProUGUI emptyLabel;
    private static GameObject renameRow;
    private static TMP_InputField renameInput;

    public static void Show(PuzzleData puzzleData, System.Action<int> openHandler, System.Action newHandler)
    {
        Close();

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null || puzzleData == null) return;

        puzzle = puzzleData;
        onOpen = openHandler;
        onNew = newHandler;
        selectedIndex = -1;
        deleteArmedAt = -100f;

        root = new GameObject("SolutionPickerPopup", typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        StretchFull(root.GetComponent<RectTransform>());

        Image backdrop = root.GetComponent<Image>();
        backdrop.color = HexlinkTheme.BackdropDim;
        Button backdropButton = root.GetComponent<Button>();
        backdropButton.transition = Button.Transition.None;
        backdropButton.onClick.AddListener(Close);

        GameObject border = new GameObject("Border", typeof(RectTransform));
        border.transform.SetParent(root.transform, false);
        ChamferedImage borderChamfer = border.AddComponent<ChamferedImage>();
        borderChamfer.Chamfer = 18f;
        borderChamfer.color = HexlinkTheme.Border;
        borderChamfer.raycastTarget = true;
        RectTransform borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0.5f, 0.5f);
        borderRT.anchorMax = new Vector2(0.5f, 0.5f);
        borderRT.pivot = new Vector2(0.5f, 0.5f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(border.transform, false);
        ChamferedImage panelChamfer = panel.AddComponent<ChamferedImage>();
        panelChamfer.Chamfer = 18f;
        panelChamfer.color = HexlinkTheme.Panel;
        panelChamfer.raycastTarget = true;
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(4f, 4f);
        panelRT.offsetMax = new Vector2(-4f, -4f);

        GameObject title = CreateTMP("Title", panel.transform, puzzleData.puzzleTitle, 24, FontStyles.Bold);
        title.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextLight;
        title.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -16f);
        titleRT.sizeDelta = new Vector2(0f, 34f);

        GameObject best = CreateTMP("Best", panel.transform, "Best: -", 20, FontStyles.Bold);
        TextMeshProUGUI bestTMP = best.GetComponent<TextMeshProUGUI>();
        bestTMP.color = HexlinkTheme.TextGray;
        bestTMP.raycastTarget = false;
        bestTMP.enableWordWrapping = false;
        RectTransform bestRT = best.GetComponent<RectTransform>();
        bestRT.anchorMin = new Vector2(0f, 1f);
        bestRT.anchorMax = new Vector2(1f, 1f);
        bestRT.pivot = new Vector2(0.5f, 1f);
        bestRT.anchoredPosition = new Vector2(0f, -58f);
        bestRT.sizeDelta = new Vector2(0f, 28f);

        PuzzleRecords.TryGet(puzzleData.puzzleID, out int bestInstr, out int bestCycles, out int bestProcs, out int bestSum);
        bestTMP.text = $"BEST   {Value("Instr", bestInstr)} \u00B7 {Value("Cycles", bestCycles)} \u00B7 {Value("Procs", bestProcs)} \u00B7 {Value("Sum", bestSum)}";

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panel.transform, false);
        divider.GetComponent<Image>().color = HexlinkTheme.Divider;
        divider.GetComponent<Image>().raycastTarget = false;
        RectTransform dividerRT = divider.GetComponent<RectTransform>();
        dividerRT.anchorMin = new Vector2(0f, 1f);
        dividerRT.anchorMax = new Vector2(1f, 1f);
        dividerRT.pivot = new Vector2(0.5f, 1f);
        dividerRT.anchoredPosition = new Vector2(0f, -96f);
        dividerRT.sizeDelta = new Vector2(-56f, 2f);

        GameObject scroll = new GameObject("ScrollView", typeof(RectTransform));
        scroll.transform.SetParent(panel.transform, false);
        ChamferedImage scrollFrame = scroll.AddComponent<ChamferedImage>();
        scrollFrame.Chamfer = 12f;
        scrollFrame.color = HexlinkTheme.CellFrame;
        scrollFrame.raycastTarget = true;
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(28f, 112f);
        scrollRT.offsetMax = new Vector2(-28f, -104f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scroll.transform, false);
        ChamferedImage viewportChamfer = viewport.AddComponent<ChamferedImage>();
        viewportChamfer.Chamfer = 12f;
        viewportChamfer.color = HexlinkTheme.Panel;
        viewportChamfer.raycastTarget = false;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = true;
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(3f, 3f);
        viewportRT.offsetMax = new Vector2(-3f, -3f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(TileWidth, TileHeight);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        gridContent = content.transform;

        emptyLabel = CreateTMP("EmptyLabel", panel.transform, "No solutions yet - click New", 15, FontStyles.Normal).GetComponent<TextMeshProUGUI>();
        emptyLabel.color = HexlinkTheme.TextGray;
        emptyLabel.raycastTarget = false;
        RectTransform emptyRT = emptyLabel.GetComponent<RectTransform>();
        emptyRT.anchorMin = new Vector2(0f, 0f);
        emptyRT.anchorMax = new Vector2(1f, 1f);
        emptyRT.offsetMin = new Vector2(28f, 112f);
        emptyRT.offsetMax = new Vector2(-28f, -104f);
        emptyLabel.alignment = TextAlignmentOptions.Midline;

        BuildRenameRow(panel.transform);

        openButton = CreateChamferButton(panel.transform, "OpenButton", "Open", HexlinkTheme.Accent, HexlinkTheme.Border, -320f, 150f).GetComponent<Button>();
        openButton.onClick.AddListener(OpenClicked);

        Button newButton = CreateChamferButton(panel.transform, "NewButton", "New", HexlinkTheme.Ghost, HexlinkTheme.TextLight, -160f, 150f).GetComponent<Button>();
        newButton.onClick.AddListener(NewClicked);

        renameButton = CreateChamferButton(panel.transform, "RenameButton", "Rename", HexlinkTheme.Ghost, HexlinkTheme.TextLight, 0f, 150f).GetComponent<Button>();
        renameButton.onClick.AddListener(RenameClicked);

        GameObject deleteGO = CreateChamferButton(panel.transform, "DeleteButton", "Delete", HexlinkTheme.Ghost, HexlinkTheme.TextLight, 160f, 150f);
        deleteButton = deleteGO.GetComponent<Button>();
        deleteButton.onClick.AddListener(DeleteClicked);
        deleteLabel = deleteGO.GetComponentInChildren<TextMeshProUGUI>();

        Button returnButton = CreateChamferButton(panel.transform, "ReturnButton", "Return", HexlinkTheme.Ghost, HexlinkTheme.TextLight, 320f, 150f).GetComponent<Button>();
        returnButton.onClick.AddListener(Close);

        Refresh();
    }

    public static void Close()
    {
        if (root != null)
        {
            UnityEngine.Object.Destroy(root);
            root = null;
        }
        puzzle = null;
        onOpen = null;
        onNew = null;
        entries.Clear();
        selectedIndex = -1;
        gridContent = null;
        openButton = null;
        renameButton = null;
        deleteButton = null;
        deleteLabel = null;
        emptyLabel = null;
        renameRow = null;
        renameInput = null;
    }

    private static void Refresh()
    {
        if (root == null || puzzle == null) return;

        entries.Clear();
        entries.AddRange(SolutionStore.LoadAll(puzzle.puzzleID));

        if (selectedIndex >= entries.Count) selectedIndex = -1;

        if (gridContent != null)
        {
            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(gridContent.GetChild(i).gameObject);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                CreateTile(gridContent, i, entries[i]);
            }
        }

        if (emptyLabel != null)
        {
            emptyLabel.gameObject.SetActive(entries.Count == 0);
        }

        bool selected = selectedIndex >= 0 && selectedIndex < entries.Count;
        if (openButton != null) openButton.interactable = selected;
        if (renameButton != null) renameButton.interactable = selected;
        if (deleteButton != null) deleteButton.interactable = selected;

        bool armed = Time.realtimeSinceStartup - deleteArmedAt < 4f;
        if (deleteLabel != null) deleteLabel.text = armed ? "Sure?" : "Delete";
    }

    private static void CreateTile(Transform parent, int index, SavedSolution entry)
    {
        bool selected = index == selectedIndex;
        bool solved = entry.solved;

        GameObject tile = new GameObject("Solution_" + index, typeof(RectTransform));
        tile.transform.SetParent(parent, false);

        ChamferedImage frame = tile.AddComponent<ChamferedImage>();
        frame.Chamfer = 14f;
        frame.color = selected ? HexlinkTheme.Accent : (solved ? SolvedGreen : HexlinkTheme.CellFrame);
        frame.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(tile.transform, false);
        ChamferedImage fillChamfer = fill.AddComponent<ChamferedImage>();
        fillChamfer.Chamfer = 14f;
        fillChamfer.color = selected ? SelectedFillColor : HexlinkTheme.CellFill;
        fillChamfer.raycastTarget = true;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);

        Button button = tile.AddComponent<Button>();
        button.targetGraphic = fillChamfer;
        HexlinkTheme.ApplyHoverTint(button);
        button.onClick.AddListener(() => Select(index));

        GameObject nameGO = CreateTMP("Name", tile.transform, (solved ? "\u2713 " : "") + entry.name, 22, FontStyles.Bold);
        TextMeshProUGUI nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
        nameTMP.color = solved ? SolvedGreen : HexlinkTheme.TextLight;
        nameTMP.alignment = TextAlignmentOptions.Midline;
        nameTMP.raycastTarget = false;
        nameTMP.enableWordWrapping = false;
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, -8f);
        nameRT.sizeDelta = new Vector2(-16f, 32f);

        SolutionStore.ComputeMetrics(entry, out int instr, out int cycles, out int procs, out int sum);
        GameObject metrics = CreateTMP("Metrics", tile.transform,
            $"{Value("Instr", instr)} \u00B7 {Value("Cycles", cycles)} \u00B7 {Value("Procs", procs)} \u00B7 {Value("Sum", sum)}", 18, FontStyles.Bold);
        TextMeshProUGUI metricsTMP = metrics.GetComponent<TextMeshProUGUI>();
        metricsTMP.color = HexlinkTheme.TextGray;
        metricsTMP.raycastTarget = false;
        metricsTMP.enableWordWrapping = false;
        RectTransform metricsRT = metrics.GetComponent<RectTransform>();
        metricsRT.anchorMin = new Vector2(0f, 0f);
        metricsRT.anchorMax = new Vector2(1f, 0f);
        metricsRT.pivot = new Vector2(0.5f, 0f);
        metricsRT.anchoredPosition = new Vector2(0f, 8f);
        metricsRT.sizeDelta = new Vector2(-16f, 26f);
    }

    private static void BuildRenameRow(Transform panel)
    {
        renameRow = new GameObject("RenameRow", typeof(RectTransform));
        renameRow.transform.SetParent(panel, false);
        RectTransform rowRT = renameRow.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0f);
        rowRT.anchorMax = new Vector2(0.5f, 0f);
        rowRT.pivot = new Vector2(0.5f, 0f);
        rowRT.anchoredPosition = new Vector2(0f, 68f);
        rowRT.sizeDelta = new Vector2(872f, 38f);

        GameObject inputGO = new GameObject("RenameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(renameRow.transform, false);
        Image inputBG = inputGO.GetComponent<Image>();
        inputBG.color = HexlinkTheme.CellFrame;
        renameInput = inputGO.GetComponent<TMP_InputField>();
        renameInput.targetGraphic = inputBG;
        renameInput.characterLimit = 40;
        RectTransform inputRT = inputGO.GetComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = new Vector2(4f, 2f);
        inputRT.offsetMax = new Vector2(-148f, -2f);

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(inputGO.transform, false);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(10f, 2f);
        textAreaRT.offsetMax = new Vector2(-10f, -2f);

        GameObject textGO = CreateTMP("Text", textArea.transform, "", 14, FontStyles.Normal);
        TextMeshProUGUI textTMP = textGO.GetComponent<TextMeshProUGUI>();
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;
        textTMP.raycastTarget = false;
        StretchFull(textGO.GetComponent<RectTransform>());
        renameInput.textComponent = textTMP;

        GameObject placeholderGO = CreateTMP("Placeholder", textArea.transform, "Solution name", 14, FontStyles.Italic);
        TextMeshProUGUI placeholderTMP = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderTMP.color = HexlinkTheme.TextGray;
        placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderTMP.raycastTarget = false;
        StretchFull(placeholderGO.GetComponent<RectTransform>());
        renameInput.placeholder = placeholderTMP;

        GameObject confirm = CreateChamferButton(renameRow.transform, "ConfirmButton", "Confirm", HexlinkTheme.Accent, HexlinkTheme.Border, 0f, 0f);
        RectTransform confirmRT = confirm.GetComponent<RectTransform>();
        confirmRT.anchorMin = new Vector2(1f, 0.5f);
        confirmRT.anchorMax = new Vector2(1f, 0.5f);
        confirmRT.pivot = new Vector2(1f, 0.5f);
        confirmRT.anchoredPosition = new Vector2(-8f, 0f);
        confirmRT.sizeDelta = new Vector2(124f, 38f);
        confirm.GetComponent<Button>().onClick.AddListener(ConfirmRename);

        GameObject cancel = CreateChamferButton(renameRow.transform, "CancelButton", "Cancel", HexlinkTheme.Ghost, HexlinkTheme.TextLight, 0f, 0f);
        RectTransform cancelRT = cancel.GetComponent<RectTransform>();
        cancelRT.anchorMin = new Vector2(1f, 0.5f);
        cancelRT.anchorMax = new Vector2(1f, 0.5f);
        cancelRT.pivot = new Vector2(1f, 0.5f);
        cancelRT.anchoredPosition = new Vector2(-140f, 0f);
        cancelRT.sizeDelta = new Vector2(124f, 38f);
        cancel.GetComponent<Button>().onClick.AddListener(CancelRename);

        renameRow.SetActive(false);
    }

    private static void Select(int index)
    {
        selectedIndex = index;
        deleteArmedAt = -100f;
        Refresh();
    }

    private static void OpenClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count) return;
        int index = selectedIndex;
        System.Action<int> handler = onOpen;
        Close();
        handler?.Invoke(index);
    }

    private static void NewClicked()
    {
        System.Action handler = onNew;
        Close();
        handler?.Invoke();
    }

    private static void RenameClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count) return;
        if (renameRow == null || renameInput == null) return;

        renameInput.text = entries[selectedIndex].name;
        renameRow.SetActive(true);
        renameInput.ActivateInputField();
    }

    private static void ConfirmRename()
    {
        if (renameInput == null || puzzle == null) return;
        if (selectedIndex < 0 || selectedIndex >= entries.Count) return;

        if (SolutionStore.Rename(puzzle.puzzleID, selectedIndex, renameInput.text))
        {
            renameRow.SetActive(false);
        }
        Refresh();
    }

    private static void CancelRename()
    {
        if (renameRow != null) renameRow.SetActive(false);
    }

    private static void DeleteClicked()
    {
        if (puzzle == null) return;
        if (selectedIndex < 0 || selectedIndex >= entries.Count) return;

        if (Time.realtimeSinceStartup - deleteArmedAt < 4f)
        {
            SolutionStore.Delete(puzzle.puzzleID, selectedIndex);
            selectedIndex = -1;
            deleteArmedAt = -100f;
            if (renameRow != null) renameRow.SetActive(false);
        }
        else
        {
            deleteArmedAt = Time.realtimeSinceStartup;
        }
        Refresh();
    }

    private static GameObject CreateChamferButton(Transform parent, string name, string label, Color color, Color textColor, float x, float width)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Button));
        button.transform.SetParent(parent, false);

        ChamferedImage chamfer = button.AddComponent<ChamferedImage>();
        chamfer.Chamfer = 12f;
        chamfer.color = color;
        chamfer.raycastTarget = true;

        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = chamfer;
        HexlinkTheme.ApplyHoverTint(buttonComponent);

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, 12f);
        rt.sizeDelta = new Vector2(width, 50f);

        GameObject text = CreateTMP("Label", button.transform, label, 18, FontStyles.Bold);
        text.GetComponent<TextMeshProUGUI>().color = textColor;
        text.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        StretchFull(text.GetComponent<RectTransform>());
        return button;
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

    private static string Value(string label, int value)
    {
        string text = value == int.MaxValue ? "\u2014" : value.ToString();
        return $"<color=#{GrayHex}>{label}</color> <color=#{LightHex}>{text}</color>";
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
