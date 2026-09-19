using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GridController : MonoBehaviour
{
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexTileSelector tileSelector;
    [SerializeField] private TileInventoryUI inventoryUI;
    [SerializeField] private RectTransform gridContent;
    [SerializeField] private RectTransform columnHeaderRow;
    [SerializeField] private GameObject processorRowTemplate;
    [SerializeField] private GameObject cellTemplate;
    [SerializeField] private GameObject headerCellTemplate;
    [SerializeField] private GameObject squareTokenTemplate;
    [SerializeField] private GameObject arrowTokenTemplate;
    [SerializeField] private Button collapseButton;

    private static readonly Color ChipSubjectColor = new Color(0.290f, 0.318f, 0.361f);
    private static readonly Color ChipOperationColor = new Color(0.227f, 0.353f, 0.400f);
    private static readonly Color ChipOperandColor = new Color(0.325f, 0.353f, 0.404f);
    private const int TokensPerRow = 4;

    private Color selectedFrame = new Color(0.486f, 0.616f, 0.651f);
    private Color selectedFill = new Color(0.110f, 0.169f, 0.188f);
    private Color errorFrame = new Color(0.690f, 0.439f, 0.439f);
    private Color errorFill = new Color(0.227f, 0.133f, 0.133f);
    private Color pendingFrame = new Color(0.980f, 0.780f, 0.320f);
    private Color pendingFill = new Color(0.545f, 0.420f, 0.160f);
    private Color normalFrame = new Color(0.106f, 0.114f, 0.137f);
    private Color normalFill = new Color(0.165f, 0.176f, 0.204f);
    private Color normalFillOdd = new Color(0.192f, 0.212f, 0.247f);
    private Color hoverFill = new Color(0.204f, 0.220f, 0.247f);
    private Color hoverFillOdd = new Color(0.227f, 0.243f, 0.271f);
    private Color runningFill = new Color(0.165f, 0.227f, 0.251f);
    private Color headerTextColor = new Color(0.604f, 0.627f, 0.667f);

    private HexCoord? pendingChangeCoord;

    public struct CellInstruction
    {
        public int Processor;
        public int Column;
        public InstructionData Instruction;
    }

    private const float CollapsedHeight = 30f;

    private class ProcessorRow
    {
        public GameObject Root;
        public List<Cell> Cells = new List<Cell>();
    }

    private class Cell
    {
        public Image FrameImage;
        public Image FillImage;
        public RectTransform InstructionContainer;
        public InstructionData Instruction;
        public bool Hovered;
        public bool Error;
    }

    private List<ProcessorRow> rows = new List<ProcessorRow>();
    private List<TextMeshProUGUI> headerLabels = new List<TextMeshProUGUI>();
    private int columnCount;
    private int activeColumn = -1;
    private RectTransform tabRect;
    private TextMeshProUGUI collapseLabel;
    private bool collapsed;
    private float expandedHeight;
    private float targetHeight;
    private float chipRowHeight = 32f;

    public int SelectedProcessor { get; private set; }
    public int SelectedColumn { get; private set; }
    public int ColumnCount => columnCount;
    public int ProcessorCount => rows.Count;

    public event System.Action OnCellSelected;
    public event System.Action OnGridChanged;

    private void RaiseGridChanged()
    {
        OnGridChanged?.Invoke();
    }

    public List<CellInstruction> GetColumnInstructions(int columnIndex)
    {
        List<CellInstruction> result = new List<CellInstruction>();
        for (int p = 0; p < rows.Count; p++)
        {
            if (columnIndex < rows[p].Cells.Count && rows[p].Cells[columnIndex].Instruction != null)
            {
                result.Add(new CellInstruction
                {
                    Processor = p,
                    Column = columnIndex,
                    Instruction = rows[p].Cells[columnIndex].Instruction
                });
            }
        }
        return result;
    }

    public void GetMetrics(out int instructions, out int cycles, out int processors)
    {
        instructions = 0;
        int lastUsedColumn = -1;
        processors = 0;

        for (int p = 0; p < rows.Count; p++)
        {
            bool rowUsed = false;
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                if (rows[p].Cells[c].Instruction == null) continue;
                instructions++;
                rowUsed = true;
                if (c > lastUsedColumn) lastUsedColumn = c;
            }
            if (rowUsed) processors++;
        }

        // The grid keeps a trailing empty column for the next instruction, so
        // cycles are measured to the last column that actually holds one.
        cycles = lastUsedColumn + 1;
    }

    public void FlagError(int processorIndex, int columnIndex)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell != null)
        {
            cell.Error = true;
            ApplyCellVisual(processorIndex, columnIndex);
        }
    }

    public void ClearErrors()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                if (!rows[p].Cells[c].Error) continue;
                rows[p].Cells[c].Error = false;
                ApplyCellVisual(p, c);
            }
        }
    }

    private void Start()
    {
        tabRect = (RectTransform)transform;
        expandedHeight = tabRect.sizeDelta.y;
        targetHeight = expandedHeight;

        LayoutElement chipLayout = squareTokenTemplate != null ? squareTokenTemplate.GetComponent<LayoutElement>() : null;
        float chipSize = chipLayout != null ? chipLayout.preferredHeight : 30f;
        chipRowHeight = Mathf.Max(26f, chipSize + 2f);

        if (GameOptions.ColourBlindMode)
        {
            selectedFrame = new Color(0.561f, 0.651f, 0.788f);
            selectedFill = new Color(0.118f, 0.153f, 0.208f);
            errorFrame = new Color(0.753f, 0.541f, 0.333f);
            errorFill = new Color(0.208f, 0.153f, 0.094f);
            pendingFrame = new Color(0.720f, 0.620f, 0.950f);
            pendingFill = new Color(0.330f, 0.270f, 0.490f);
        }

        if (collapseButton != null)
        {
            collapseLabel = collapseButton.GetComponentInChildren<TextMeshProUGUI>();
            collapseButton.onClick.AddListener(ToggleCollapsed);
        }

        AddProcessorRow();
        AddColumn();
        SelectCell(0, 0);

        StartCoroutine(RestoreWhenReady());
    }

    private IEnumerator RestoreWhenReady()
    {
        // HexGridSpawner populates its tiles in Start(); script order between components
        // is undefined, so wait a frame before restoring tiles that need hexes to exist.
        yield return null;
        RestoreSavedSolution();
    }

    public void GrowTo(int processorCount, int columnCountTarget)
    {
        while (rows.Count < processorCount) AddProcessorRow();
        while (columnCount < columnCountTarget) AddColumn();
    }

    public void ResizeTo(int processorCount, int columnCountTarget)
    {
        processorCount = Mathf.Max(1, processorCount);
        columnCountTarget = Mathf.Max(1, columnCountTarget);
        GrowTo(processorCount, columnCountTarget);

        while (rows.Count > processorCount)
        {
            ProcessorRow row = rows[rows.Count - 1];
            rows.RemoveAt(rows.Count - 1);
            if (row.Root != null) Destroy(row.Root.gameObject);
        }

        while (columnCount > columnCountTarget)
        {
            columnCount--;
            if (headerLabels.Count > 0)
            {
                TextMeshProUGUI header = headerLabels[headerLabels.Count - 1];
                headerLabels.RemoveAt(headerLabels.Count - 1);
                if (header != null && header.transform.parent != null)
                {
                    Destroy(header.transform.parent.gameObject);
                }
            }
            for (int p = 0; p < rows.Count; p++)
            {
                if (rows[p].Cells.Count > columnCount)
                {
                    Cell cell = rows[p].Cells[rows[p].Cells.Count - 1];
                    rows[p].Cells.RemoveAt(rows[p].Cells.Count - 1);
                    if (cell.FrameImage != null) Destroy(cell.FrameImage.gameObject);
                }
            }
        }

        if (SelectedProcessor >= rows.Count || SelectedColumn >= columnCount)
        {
            SelectCell(0, 0);
        }
    }

    public void SetInstruction(int processorIndex, int columnIndex, InstructionData instruction)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell == null) return;

        cell.Instruction = instruction;
        foreach (Transform token in cell.InstructionContainer)
        {
            Destroy(token.gameObject);
        }
        RenderInstruction(cell, instruction);
        RaiseGridChanged();
    }

    private void RestoreSavedSolution()
    {
        if (PuzzleSelection.SelectedPuzzle == null) return;

        SavedSolution saved = SolutionStore.LoadNewest(PuzzleSelection.SelectedPuzzle.puzzleID);
        if (saved == null) return;

        ApplySolution(saved);
        Debug.Log($"Restored saved solution '{saved.name}' for '{saved.puzzleID}'.");
    }

    public void ApplySolution(SavedSolution saved)
    {
        if (saved == null) return;

        ResizeTo(saved.processors, saved.columns);
        WipeCurrent();

        foreach (SavedTile tile in saved.tiles)
        {
            TileData data = TileDataFactory.CreateFromSymbol(tile.symbol);
            if (data is FinalTileData finalTile)
            {
                finalTile.targetNumber = tile.target;
            }
            if (data != null)
            {
                labelController.boardState.PlaceTile(new HexCoord(tile.q, tile.r), data);
            }
        }

        foreach (SavedInstruction instruction in saved.instructions)
        {
            InstructionData data = SolutionStore.ToInstructionData(instruction);
            if (data != null)
            {
                SetInstruction(instruction.processor, instruction.column, data);
            }
        }

        ClearPendingChange();
        SelectCell(0, 0);
    }

    public void ClearAll()
    {
        WipeCurrent();
        ClearPendingChange();
        SelectCell(0, 0);
    }

    private void WipeCurrent()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                Cell cell = rows[p].Cells[c];
                cell.Instruction = null;
                foreach (Transform token in cell.InstructionContainer)
                {
                    Destroy(token.gameObject);
                }
            }
        }

        List<HexCoord> occupied = new List<HexCoord>(labelController.boardState.AllTiles.Keys);
        foreach (HexCoord coord in occupied)
        {
            labelController.boardState.RemoveTile(coord);
        }

        RaiseGridChanged();
    }

    private void OnEnable()
    {
        if (labelController != null)
        {
            labelController.boardState.OnCellChanged += HandleBoardCellChanged;
        }
    }

    private void OnDisable()
    {
        if (labelController != null)
        {
            labelController.boardState.OnCellChanged -= HandleBoardCellChanged;
        }
    }

    private void HandleBoardCellChanged(HexCoord coord)
    {
        TileData changedTile = labelController.boardState.GetTile(coord);

        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                Cell cell = rows[p].Cells[c];
                if (cell.Instruction == null || !ReferencesCoord(cell.Instruction, coord)) continue;

                // Select/spawn instructions only track number tiles - never re-render them
                // for an operation tile (you can't spawn an operation). Removals still clear them.
                if (cell.Instruction is SelectInstructionData && changedTile != null && !(changedTile is NumberTileData)) continue;

                foreach (Transform token in cell.InstructionContainer)
                {
                    Destroy(token.gameObject);
                }
                RenderInstruction(cell, cell.Instruction);
            }
        }
    }

    public bool HasInstructionsReferencing(HexCoord coord)
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                if (rows[p].Cells[c].Instruction != null && ReferencesCoord(rows[p].Cells[c].Instruction, coord))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void SetPendingChange(HexCoord coord)
    {
        ClearPendingChange();
        pendingChangeCoord = coord;
        RefreshAllCellVisuals();

        Debug.Log("This change affects existing instructions - click the tile again to confirm.");
    }

    public bool IsPendingChange(HexCoord coord)
    {
        return pendingChangeCoord.HasValue && pendingChangeCoord.Value.Equals(coord);
    }

    public void ClearPendingChange()
    {
        if (!pendingChangeCoord.HasValue) return;
        pendingChangeCoord = null;
        RefreshAllCellVisuals();
    }

    private void RefreshAllCellVisuals()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                ApplyCellVisual(p, c);
            }
        }
    }

    private void ApplyCellVisual(int p, int c)
    {
        Cell cell = GetCell(p, c);
        if (cell == null) return;

        bool pending = pendingChangeCoord.HasValue && cell.Instruction != null
            && ReferencesCoord(cell.Instruction, pendingChangeCoord.Value);

        Color frame;
        Color fill;
        if (cell.Error)
        {
            frame = errorFrame;
            fill = errorFill;
        }
        else if (pending)
        {
            frame = pendingFrame;
            fill = pendingFill;
        }
        else if (p == SelectedProcessor && c == SelectedColumn)
        {
            frame = selectedFrame;
            fill = selectedFill;
        }
        else if (c == activeColumn)
        {
            frame = normalFrame;
            fill = runningFill;
        }
        else if (cell.Hovered)
        {
            frame = normalFrame;
            fill = (c & 1) == 1 ? hoverFillOdd : hoverFill;
        }
        else
        {
            frame = normalFrame;
            fill = (c & 1) == 1 ? normalFillOdd : normalFill;
        }

        cell.FrameImage.color = frame;
        cell.FillImage.color = fill;
    }

    private static bool ReferencesCoord(InstructionData instruction, HexCoord coord)
    {
        if (instruction is MoveInstructionData move) return move.Source.Equals(coord) || move.Destination.Equals(coord);
        if (instruction is SelectInstructionData select) return select.Source.Equals(coord);
        if (instruction is OperationInstructionData op)
        {
            return op.Source.Equals(coord) || op.OperationTile.Equals(coord) || op.AdditionalOperands.Contains(coord);
        }
        return false;
    }

    private void Update()
    {
        if (tabRect != null && Mathf.Abs(tabRect.sizeDelta.y - targetHeight) > 0.1f)
        {
            Vector2 size = tabRect.sizeDelta;
            size.y = Mathf.Lerp(size.y, targetHeight, 10f * Time.deltaTime);
            tabRect.sizeDelta = size;
        }
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        targetHeight = collapsed ? CollapsedHeight : expandedHeight;
        if (collapseLabel != null)
        {
            collapseLabel.text = collapsed ? "Show" : "Hide";
        }
    }

    public void AddProcessorRow()
    {
        GameObject rowObj = Instantiate(processorRowTemplate, gridContent);
        rowObj.SetActive(true);
        rowObj.name = "ProcessorRow_" + (rows.Count + 1);
        TextMeshProUGUI rowLabelText = rowObj.transform.Find("RowLabel").GetComponent<TextMeshProUGUI>();
        rowLabelText.text = "P" + (rows.Count + 1);

        ProcessorRow row = new ProcessorRow { Root = rowObj };
        for (int c = 0; c < columnCount; c++)
        {
            row.Cells.Add(CreateCell(row, rows.Count, c));
        }
        rows.Add(row);

        CellClickHandler rowMenu = rowLabelText.gameObject.AddComponent<CellClickHandler>();
        rowMenu.OnClick = eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                int index = rows.IndexOf(row);
                if (index >= 0) ShowProcessorMenu(index, eventData.position);
            }
        };

        RaiseGridChanged();
    }

    public void AddColumn()
    {
        columnCount++;

        GameObject headerCell = Instantiate(headerCellTemplate, columnHeaderRow);
        headerCell.SetActive(true);
        headerCell.name = "Header_" + columnCount;
        TextMeshProUGUI headerLabel = headerCell.transform.Find("NumberText").GetComponent<TextMeshProUGUI>();
        headerLabel.text = columnCount.ToString();
        headerLabels.Add(headerLabel);

        for (int p = 0; p < rows.Count; p++)
        {
            rows[p].Cells.Add(CreateCell(rows[p], p, columnCount - 1));
        }

        CellClickHandler columnMenu = headerCell.AddComponent<CellClickHandler>();
        columnMenu.OnClick = eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                int index = headerLabels.IndexOf(headerLabel);
                if (index >= 0) ShowColumnMenu(index, eventData.position);
            }
        };

        RaiseGridChanged();
    }

    private void ShowProcessorMenu(int processorIndex, Vector2 position)
    {
        GridContextMenu.Show(position, new (string, System.Action)[]
        {
            ("Clear processor", () => ClearProcessorRow(processorIndex)),
            ("Delete processor", () => DeleteProcessorRow(processorIndex)),
            ("Clear all", ClearAllInstructions)
        });
    }

    private void ShowColumnMenu(int columnIndex, Vector2 position)
    {
        GridContextMenu.Show(position, new (string, System.Action)[]
        {
            ("Clear column", () => ClearColumn(columnIndex)),
            ("Delete column", () => DeleteColumn(columnIndex)),
            ("Clear all", ClearAllInstructions)
        });
    }

    public void ClearProcessorRow(int processorIndex)
    {
        if (processorIndex < 0 || processorIndex >= rows.Count) return;

        for (int c = 0; c < rows[processorIndex].Cells.Count; c++)
        {
            ClearCell(processorIndex, c);
        }
        ClearPendingChange();
    }

    public void ClearColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= columnCount) return;

        for (int p = 0; p < rows.Count; p++)
        {
            ClearCell(p, columnIndex);
        }
        ClearPendingChange();
    }

    public void ClearAllInstructions()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                ClearCell(p, c);
            }
        }
        ClearPendingChange();
        RefreshAllCellVisuals();
    }

    public void DeleteProcessorRow(int processorIndex)
    {
        if (rows.Count <= 1) return;
        if (processorIndex < 0 || processorIndex >= rows.Count) return;

        ProcessorRow row = rows[processorIndex];
        rows.RemoveAt(processorIndex);
        if (row.Root != null) Destroy(row.Root.gameObject);

        RenumberRows();

        if (SelectedProcessor >= rows.Count)
        {
            SelectedProcessor = rows.Count - 1;
            SelectedColumn = Mathf.Clamp(SelectedColumn, 0, columnCount - 1);
        }
        RaiseGridChanged();
        RefreshAllCellVisuals();
    }

    public void DeleteColumn(int columnIndex)
    {
        if (columnCount <= 1) return;
        if (columnIndex < 0 || columnIndex >= columnCount) return;

        ClearActiveColumn();

        TextMeshProUGUI header = headerLabels[columnIndex];
        headerLabels.RemoveAt(columnIndex);
        if (header != null && header.transform.parent != null)
        {
            Destroy(header.transform.parent.gameObject);
        }

        columnCount--;
        for (int p = 0; p < rows.Count; p++)
        {
            List<Cell> cells = rows[p].Cells;
            if (columnIndex < cells.Count)
            {
                Cell cell = cells[columnIndex];
                cells.RemoveAt(columnIndex);
                if (cell.FrameImage != null) Destroy(cell.FrameImage.gameObject);
            }
        }
        RenumberHeaders();

        if (SelectedColumn >= columnCount)
        {
            SelectedColumn = columnCount - 1;
            SelectedProcessor = Mathf.Clamp(SelectedProcessor, 0, rows.Count - 1);
        }
        RaiseGridChanged();
        RefreshAllCellVisuals();
    }

    private void RenumberRows()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            TextMeshProUGUI label = rows[p].Root != null
                ? rows[p].Root.transform.Find("RowLabel").GetComponent<TextMeshProUGUI>()
                : null;
            if (label != null) label.text = "P" + (p + 1);
        }
    }

    private void RenumberHeaders()
    {
        for (int c = 0; c < headerLabels.Count; c++)
        {
            if (headerLabels[c] != null) headerLabels[c].text = (c + 1).ToString();
        }
    }

    public void SetActiveColumn(int columnIndex)
    {
        ClearActiveColumn();
        if (columnIndex >= 0 && columnIndex < headerLabels.Count)
        {
            headerLabels[columnIndex].color = selectedFrame;
            headerLabels[columnIndex].fontStyle = FontStyles.Bold;
            activeColumn = columnIndex;
        }
        RefreshAllCellVisuals();
    }

    public void ClearActiveColumn()
    {
        if (activeColumn >= 0 && activeColumn < headerLabels.Count)
        {
            headerLabels[activeColumn].color = headerTextColor;
            headerLabels[activeColumn].fontStyle = FontStyles.Normal;
        }
        activeColumn = -1;
        RefreshAllCellVisuals();
    }

    private Cell CreateCell(ProcessorRow row, int processorIndex, int columnIndex)
    {
        GameObject cellObj = Instantiate(cellTemplate, row.Root.transform);
        cellObj.SetActive(true);
        cellObj.name = "Cell_" + (processorIndex + 1) + "_" + (columnIndex + 1);

        Cell cell = new Cell
        {
            FrameImage = cellObj.GetComponent<Image>(),
            FillImage = cellObj.transform.Find("Fill").GetComponent<Image>(),
            InstructionContainer = cellObj.transform.Find("InstructionContainer") as RectTransform
        };
        cellObj.GetComponent<Button>().onClick.AddListener(() => SelectCell(processorIndex, columnIndex));

        CellClickHandler clickHandler = cellObj.AddComponent<CellClickHandler>();
        clickHandler.OnClick = eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                ClearCell(processorIndex, columnIndex);
            }
        };
        clickHandler.OnHover = hovered => SetCellHovered(processorIndex, columnIndex, hovered);

        ApplyCellVisual(processorIndex, columnIndex);
        return cell;
    }

    private void SetCellHovered(int processorIndex, int columnIndex, bool hovered)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell == null || cell.Hovered == hovered) return;
        cell.Hovered = hovered;
        ApplyCellVisual(processorIndex, columnIndex);
    }

    public void ClearCell(int processorIndex, int columnIndex)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell == null) return;

        foreach (Transform token in cell.InstructionContainer)
        {
            Destroy(token.gameObject);
        }
        cell.Instruction = null;
        RaiseGridChanged();
    }

    public void SelectCell(int processorIndex, int columnIndex)
    {
        if (inventoryUI != null) inventoryUI.DeselectTile();

        int prevP = SelectedProcessor;
        int prevC = SelectedColumn;

        SelectedProcessor = processorIndex;
        SelectedColumn = columnIndex;

        ApplyCellVisual(prevP, prevC);
        ApplyCellVisual(processorIndex, columnIndex);

        OnCellSelected?.Invoke();
    }

    public void DeselectCell()
    {
        int prevP = SelectedProcessor;
        int prevC = SelectedColumn;
        SelectedProcessor = -1;
        SelectedColumn = -1;
        ApplyCellVisual(prevP, prevC);
    }

    public void PlaceInstruction(int processorIndex, int columnIndex, InstructionData instruction)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell == null || cell.Instruction != null) return;

        cell.Instruction = instruction;
        RenderInstruction(cell, instruction);

        if (columnIndex == columnCount - 1)
        {
            AddColumn();
        }
        SelectCell(processorIndex, columnIndex + 1);
        RaiseGridChanged();
    }

    public void ClearGrid()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                ClearCell(p, c);
            }
        }
        SelectCell(0, 0);
    }

    private class TokenFlow
    {
        private readonly RectTransform container;
        private readonly float rowHeight;
        private RectTransform currentRow;
        private int count;

        public TokenFlow(RectTransform container, float rowHeight)
        {
            this.container = container;
            this.rowHeight = rowHeight;
        }

        public RectTransform NextSlot()
        {
            if (currentRow == null || count >= TokensPerRow)
            {
                GameObject row = new GameObject("TokenRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(container, false);
                HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.spacing = 2f;
                LayoutElement le = row.GetComponent<LayoutElement>();
                le.minHeight = rowHeight;
                le.preferredHeight = rowHeight;
                currentRow = row.GetComponent<RectTransform>();
                count = 0;
            }
            count++;
            return currentRow;
        }
    }

    private void RenderInstruction(Cell cell, InstructionData instruction)
    {
        TokenFlow flow = new TokenFlow(cell.InstructionContainer, chipRowHeight);

        if (instruction is MoveInstructionData move)
        {
            SpawnSquareToken(flow, move.Source, ChipSubjectColor);
            SpawnArrowToken(flow);
            SpawnSquareToken(flow, move.Destination, ChipSubjectColor);
        }
        else if (instruction is SelectInstructionData select)
        {
            SpawnSquareToken(flow, select.Source, ChipSubjectColor, "{{{0}}}");
        }
        else if (instruction is OperationInstructionData op)
        {
            if (op.Operation == OperationTileData.OperationType.SquareRoot)
            {
                SpawnSquareRootToken(flow, op.Source);
                return;
            }

            SpawnSquareToken(flow, op.Source, ChipSubjectColor);
            if (op.AdditionalOperands.Count == 0)
            {
                SpawnSquareToken(flow, op.OperationTile, ChipOperationColor);
            }
            for (int i = 0; i < op.AdditionalOperands.Count; i++)
            {
                SpawnSquareToken(flow, op.OperationTile, ChipOperationColor);
                SpawnSquareToken(flow, op.AdditionalOperands[i], ChipOperandColor);
            }
        }
    }

    private void SpawnSquareToken(TokenFlow flow, HexCoord coord, Color chipColor, string format = "{0}")
    {
        GameObject token = Instantiate(squareTokenTemplate, flow.NextSlot());
        token.SetActive(true);
        token.GetComponent<Image>().color = chipColor;

        InstructionToken instructionToken = token.GetComponent<InstructionToken>();
        instructionToken.Setup(tileSelector, coord);

        TileData tile = labelController.boardState.GetTile(coord);
        instructionToken.SetText(tile != null ? string.Format(format, tile.GetDisplayValue()) : "");
    }

    private void SpawnArrowToken(TokenFlow flow)
    {
        GameObject token = Instantiate(arrowTokenTemplate, flow.NextSlot());
        token.SetActive(true);
    }

    private void SpawnSquareRootToken(TokenFlow flow, HexCoord coord)
    {
        RectTransform row = flow.NextSlot();
        float chipSize = Mathf.Max(24f, chipRowHeight - 2f);
        GameObject wrapper = new GameObject("SquareRootToken", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        wrapper.transform.SetParent(row, false);
        HorizontalLayoutGroup wrapperHLG = wrapper.GetComponent<HorizontalLayoutGroup>();
        wrapperHLG.childAlignment = TextAnchor.MiddleLeft;
        wrapperHLG.childForceExpandWidth = false;
        wrapperHLG.childForceExpandHeight = false;
        wrapperHLG.childControlWidth = true;
        wrapperHLG.childControlHeight = true;
        wrapperHLG.spacing = 0f;
        LayoutElement wrapperLE = wrapper.AddComponent<LayoutElement>();
        wrapperLE.preferredWidth = chipSize + 4f;
        wrapperLE.preferredHeight = chipSize;

        GameObject radical = new GameObject("RadicalSign", typeof(RectTransform), typeof(TextMeshProUGUI));
        radical.transform.SetParent(wrapper.transform, false);
        TextMeshProUGUI radicalTMP = radical.GetComponent<TextMeshProUGUI>();
        radicalTMP.text = "\u221A";
        radicalTMP.fontSize = Mathf.Round(chipSize * 0.67f);
        radicalTMP.color = new Color(0.910f, 0.918f, 0.929f);
        radicalTMP.alignment = TextAlignmentOptions.Center;
        LayoutElement radicalLE = radical.AddComponent<LayoutElement>();
        radicalLE.preferredWidth = Mathf.Round(chipSize * 0.45f);
        radicalLE.preferredHeight = chipSize;

        GameObject square = Instantiate(squareTokenTemplate, wrapper.transform);
        square.SetActive(true);
        square.GetComponent<Image>().color = ChipSubjectColor;
        InstructionToken instructionToken = square.GetComponent<InstructionToken>();
        instructionToken.Setup(tileSelector, coord);
        TileData tile = labelController.boardState.GetTile(coord);
        instructionToken.SetText(tile != null ? tile.GetDisplayValue() : "");

        GameObject overline = new GameObject("Overline", typeof(RectTransform), typeof(Image));
        overline.transform.SetParent(square.transform, false);
        overline.GetComponent<Image>().color = new Color(0.910f, 0.918f, 0.929f);
        RectTransform overlineRT = overline.GetComponent<RectTransform>();
        overlineRT.anchorMin = new Vector2(0f, 1f);
        overlineRT.anchorMax = new Vector2(1f, 1f);
        overlineRT.pivot = new Vector2(0.5f, 1f);
        overlineRT.sizeDelta = new Vector2(0f, 2f);
        overlineRT.anchoredPosition = Vector2.zero;
    }

    private Cell GetCell(int processorIndex, int columnIndex)
    {
        if (processorIndex < 0 || processorIndex >= rows.Count) return null;
        if (columnIndex < 0 || columnIndex >= rows[processorIndex].Cells.Count) return null;
        return rows[processorIndex].Cells[columnIndex];
    }
}

public class CellClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action<PointerEventData> OnClick;
    public System.Action<bool> OnHover;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover?.Invoke(false);
    }
}

