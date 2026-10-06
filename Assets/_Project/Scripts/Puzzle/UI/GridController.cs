// GridController - runtime brain of the InfoTab, the bottom-anchored spreadsheet UI
// where columns are time steps, rows are processors, and each cell renders one
// instruction as square token icons. The hierarchy is authored by InfoTabBuilder
// (editor); this controller stamps rows/columns/cells at runtime and exposes the
// program to the execution engine.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Builds the grid, manages selection/placement, renders tokens, tints cells for
// errors and pending changes, animates collapse, and restores saved solutions.
public class GridController : MonoBehaviour
{
    // Serialized refs and templates are wired by InfoTabBuilder at edit time.
    [SerializeField] private HexTileLabelController labelController;
    // Highlights the 3D hex when one of its tokens is hovered.
    [SerializeField] private HexTileSelector tileSelector;
    // Palette model; selecting a cell always deselects the current tile.
    [SerializeField] private TileInventoryUI inventoryUI;
    [SerializeField] private RectTransform gridContent;

    // Root of the runtime-managed cell area - used by ThemeSwitcher to scope
    // which graphics must not be structurally converted (they are repainted
    // by this controller on every state change).
    public RectTransform GridContentRoot => gridContent;
    // Parent of the numbered column header cells.
    [SerializeField] private RectTransform columnHeaderRow;
    // Disabled stamps cloned for every row / cell / header / token at runtime.
    [SerializeField] private GameObject processorRowTemplate;
    [SerializeField] private GameObject cellTemplate;
    [SerializeField] private GameObject headerCellTemplate;
    [SerializeField] private GameObject squareTokenTemplate;
    [SerializeField] private GameObject arrowTokenTemplate;
    // Toggles tab collapse (height lerp toward CollapsedHeight).
    [SerializeField] private Button collapseButton;

    // Theme shims for chip role colors (subject / operation / operand).
    private static Color ChipSubjectColor => HexlinkTheme.ChipSubject;
    private static Color ChipOperationColor => HexlinkTheme.ChipOperation;
    private static Color ChipOperandColor => HexlinkTheme.ChipOperand;
    // Square tokens per visual row before wrapping inside a cell.
    private const int TokensPerRow = 4;

    // Cell tint palette (selected / error / pending / running / hover / zebra).
    private Color selectedFrame => HexlinkTheme.SelectedFrame;
    private Color selectedFill => HexlinkTheme.SelectedFill;
    private Color errorFrame => HexlinkTheme.ErrorFrame;
    private Color errorFill => HexlinkTheme.ErrorFill;
    private Color pendingFrame => HexlinkTheme.PendingFrame;
    private Color pendingFill => HexlinkTheme.PendingFill;
    private Color normalFrame => HexlinkTheme.CellFrame;
    private Color normalFill => HexlinkTheme.CellFill;
    private Color normalFillOdd => HexlinkTheme.NormalFillOdd;
    private Color hoverFill => HexlinkTheme.HoverFill;
    private Color hoverFillOdd => HexlinkTheme.HoverFillOdd;
    private Color runningFill => HexlinkTheme.RunningFill;
    private Color headerTextColor => HexlinkTheme.TextGray;

    // Board coord whose replacement is awaiting the yellow second-click confirm.
    private HexCoord? pendingChangeCoord;

    // One instruction plus its grid address (Processor = row, Column = time step);
    // the unit the execution engine consumes per column.
    public struct CellInstruction
    {
        public int Processor;
        public int Column;
        public InstructionData Instruction;
    }

    private const float CollapsedHeight = 30f;

    // One processor: row root plus its cells in column order.
    private class ProcessorRow
    {
        public GameObject Root;
        public List<Cell> Cells = new List<Cell>();
    }

    // Runtime state of a single cell: images, held instruction, UI flags.
    private class Cell
    {
        public Image FrameImage;
        public Image FillImage;
        // Parent of the stamped token icons (destroyed and rebuilt on re-render).
        public RectTransform InstructionContainer;
        public InstructionData Instruction;
        public bool Hovered;
        // Set by FlagError -> red tint until ClearErrors.
        public bool Error;
    }

    // Processor rows in order (index = P label - 1).
    private List<ProcessorRow> rows = new List<ProcessorRow>();
    // Column header labels (index = column number - 1).
    private List<TextMeshProUGUI> headerLabels = new List<TextMeshProUGUI>();
    // Allocated columns; a trailing empty column is always kept for the next instruction.
    private int columnCount;
    // Column the engine is currently executing (running tint), -1 when idle.
    private int activeColumn = -1;
    // Collapse bookkeeping; height is lerped toward targetHeight in Update.
    private RectTransform tabRect;
    private TextMeshProUGUI collapseLabel;
    private bool collapsed;
    private float expandedHeight;
    private float targetHeight;
    // Token row height, derived from the square token template's layout element.
    private float chipRowHeight = 32f;

    // Selection cursor (tinted cell) where the next instruction will land.
    public int SelectedProcessor { get; private set; }
    public int SelectedColumn { get; private set; }

    // Tutorial read access to the instruction grid.
    public int ProcessorCount => rows.Count;
    public int ColumnCount => columnCount;

    // Single-cell read used by the tutorial system.
    public InstructionData GetInstructionAt(int processorIndex, int columnIndex)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        return cell != null ? cell.Instruction : null;
    }

    // Fired when the selection cursor moves.
    public event System.Action OnCellSelected;
    // Fired after any edit (place/clear/grow/wipe); metrics strip and auto-save listen.
    public event System.Action OnGridChanged;

    private void RaiseGridChanged()
    {
        OnGridChanged?.Invoke();
    }

    // All instructions in one time-step column, in processor order - the engine's work for that step.
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

    // Live counters for the metrics strip; the trailing empty column is not a cycle.
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

    // Tint a cell red after a runtime failure.
    public void FlagError(int processorIndex, int columnIndex)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell != null)
        {
            cell.Error = true;
            ApplyCellVisual(processorIndex, columnIndex);
        }
    }

    // Clear every red error tint.
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

    // Capture heights, hook collapse, seed a 1x1 grid, select (0,0), queue solution restore.
    private void Start()
    {
        tabRect = (RectTransform)transform;
        expandedHeight = tabRect.sizeDelta.y;
        targetHeight = expandedHeight;

        LayoutElement chipLayout = squareTokenTemplate != null ? squareTokenTemplate.GetComponent<LayoutElement>() : null;
        float chipSize = chipLayout != null ? chipLayout.preferredHeight : 30f;
        chipRowHeight = Mathf.Max(26f, chipSize + 2f);

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

    // Expand to at least the requested size (never shrinks).
    public void GrowTo(int processorCount, int columnCountTarget)
    {
        while (rows.Count < processorCount) AddProcessorRow();
        while (columnCount < columnCountTarget) AddColumn();
    }

    // Grow or shrink to exact dimensions, destroying surplus rows/columns/cells.
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

    // Overwrite a cell's instruction and re-render its tokens (loading saved solutions).
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

    // Auto-restore the newest saved solution for the selected puzzle.
    private void RestoreSavedSolution()
    {
        if (PuzzleSelection.SelectedPuzzle == null) return;

        SavedSolution saved = SolutionStore.LoadNewest(PuzzleSelection.SelectedPuzzle.puzzleID);
        if (saved == null) return;

        ApplySolution(saved);
        Debug.Log($"Restored saved solution '{saved.name}' for '{saved.puzzleID}'.");
    }

    // Wipe-and-replace: resize, clear board + grid, place saved tiles, then instructions.
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

    // Full wipe: instructions + board tiles + pending change; cursor back to (0,0).
    public void ClearAll()
    {
        WipeCurrent();
        ClearPendingChange();
        SelectCell(0, 0);
    }

    // Destroy all tokens/instructions and remove every board tile.
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

    // Subscribe so tokens re-render when a referenced board tile changes.
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

    // Re-render every instruction that references the changed board cell.
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

    // True if any placed instruction touches this coord (drives the replace-confirm flow).
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

    // Arm the yellow confirm state: replacing this coord would affect existing instructions.
    public void SetPendingChange(HexCoord coord)
    {
        ClearPendingChange();
        pendingChangeCoord = coord;
        RefreshAllCellVisuals();

        Debug.Log("This change affects existing instructions - click the tile again to confirm.");
    }

    // True when coord is the one pending confirmation.
    public bool IsPendingChange(HexCoord coord)
    {
        return pendingChangeCoord.HasValue && pendingChangeCoord.Value.Equals(coord);
    }

    // Disarm the confirm state and restore normal tints.
    public void ClearPendingChange()
    {
        if (!pendingChangeCoord.HasValue) return;
        pendingChangeCoord = null;
        RefreshAllCellVisuals();
    }

    // Repaint every cell after a global tint-state change.
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

    // Repaint one cell; tint priority: error > pending > selected > running > hover > zebra.
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

    // Whether an instruction reads or writes the given hex coord.
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

    // Ease the tab height toward the collapse target.
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

    // Stamp a processor row with one cell per existing column; right-click its label opens the row menu.
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

    // Append a numbered column (header + cells); right-click the header opens the column menu.
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

    // Row context menu: clear/delete this processor or clear everything.
    private void ShowProcessorMenu(int processorIndex, Vector2 position)
    {
        GridContextMenu.Show(position, new (string, System.Action)[]
        {
            ("Clear processor", () => ClearProcessorRow(processorIndex)),
            ("Delete processor", () => DeleteProcessorRow(processorIndex)),
            ("Clear all", ClearAllInstructions)
        });
    }

    // Column context menu: clear/delete this column or clear everything.
    private void ShowColumnMenu(int columnIndex, Vector2 position)
    {
        GridContextMenu.Show(position, new (string, System.Action)[]
        {
            ("Clear column", () => ClearColumn(columnIndex)),
            ("Delete column", () => DeleteColumn(columnIndex)),
            ("Clear all", ClearAllInstructions)
        });
    }

    // Empty every cell in a row but keep the row.
    public void ClearProcessorRow(int processorIndex)
    {
        if (processorIndex < 0 || processorIndex >= rows.Count) return;

        for (int c = 0; c < rows[processorIndex].Cells.Count; c++)
        {
            ClearCell(processorIndex, c);
        }
        ClearPendingChange();
    }

    // Empty every cell in a column but keep the column.
    public void ClearColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= columnCount) return;

        for (int p = 0; p < rows.Count; p++)
        {
            ClearCell(p, columnIndex);
        }
        ClearPendingChange();
    }

    // Empty all cells but keep the grid dimensions.
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

    // Remove a whole row (always keep >= 1) and renumber P labels.
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

    // Remove a whole column (always keep >= 1) and renumber headers.
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

    // Highlight the column the engine is executing (bold header + running fill).
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

    // Clear the executing-column highlight.
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

    // Stamp a cell: frame/fill images, left-click select, middle-click clear, hover tracking.
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

    // Empty one cell (the middle-click path).
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

    // Move the cursor; also drops any palette selection so clicks don't place tiles.
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

    // Clear the cursor without moving it.
    public void DeselectCell()
    {
        int prevP = SelectedProcessor;
        int prevC = SelectedColumn;
        SelectedProcessor = -1;
        SelectedColumn = -1;
        ApplyCellVisual(prevP, prevC);
    }

    // Commit an instruction: render tokens, auto-grow if in the last column, advance the cursor.
    public void PlaceInstruction(int processorIndex, int columnIndex, InstructionData instruction)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell == null || cell.Instruction != null) return;

        cell.Instruction = instruction;
        RenderInstruction(cell, instruction);

        // Committing in the last column grows the grid so an empty next step always exists.
        if (columnIndex == columnCount - 1)
        {
            AddColumn();
        }
        SelectCell(processorIndex, columnIndex + 1);
        RaiseGridChanged();
    }

    // Empty all cells, keep dimensions, cursor back to (0,0).
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

    // Mini flow-layout: wraps stamped tokens into rows of TokensPerRow inside a cell.
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

    // Compose the icon sequence: move = src->dst; select = {src}; op = src [op operand]*;
    // square root is special-cased into a radical token.
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

    // Stamp one chip: tint by role, wire hover highlight, show the tile's value.
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

    // Arrow icon between a move's source and destination chips.
    private void SpawnArrowToken(TokenFlow flow)
    {
        GameObject token = Instantiate(arrowTokenTemplate, flow.NextSlot());
        token.SetActive(true);
    }

    // Build the sqrt token (radical + chip + overline) procedurally - no template exists.
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
        radicalTMP.color = HexlinkTheme.TextLight;
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
        overline.GetComponent<Image>().color = HexlinkTheme.TextLight;
        RectTransform overlineRT = overline.GetComponent<RectTransform>();
        overlineRT.anchorMin = new Vector2(0f, 1f);
        overlineRT.anchorMax = new Vector2(1f, 1f);
        overlineRT.pivot = new Vector2(0.5f, 1f);
        overlineRT.sizeDelta = new Vector2(0f, 2f);
        overlineRT.anchoredPosition = Vector2.zero;
    }

    // Bounds-checked cell access.
    private Cell GetCell(int processorIndex, int columnIndex)
    {
        if (processorIndex < 0 || processorIndex >= rows.Count) return null;
        if (columnIndex < 0 || columnIndex >= rows[processorIndex].Cells.Count) return null;
        return rows[processorIndex].Cells[columnIndex];
    }
}

// Forwards right/middle clicks and hover to assigned lambdas; uGUI Button only
// reports left-clicks, so cells and headers use this for their extra interactions.
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

