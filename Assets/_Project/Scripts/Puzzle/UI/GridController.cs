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
    [SerializeField] private Color selectedColor = new Color(0.635f, 0.843f, 0.890f);
    [SerializeField] private Color errorColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color pendingChangeColor = Color.yellow;
    [SerializeField] private Color normalCellColor = Color.white;

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
        public Image Image;
        public RectTransform InstructionContainer;
        public InstructionData Instruction;
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

    public int SelectedProcessor { get; private set; }
    public int SelectedColumn { get; private set; }
    public int ColumnCount => columnCount;
    public int ProcessorCount => rows.Count;

    public event System.Action OnCellSelected;

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

    public void FlagError(int processorIndex, int columnIndex)
    {
        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell != null)
        {
            cell.Image.color = errorColor;
        }
    }

    public void ClearErrors()
    {
        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                rows[p].Cells[c].Image.color = (p == SelectedProcessor && c == SelectedColumn) ? selectedColor : normalCellColor;
            }
        }
    }

    private void Start()
    {
        tabRect = (RectTransform)transform;
        expandedHeight = tabRect.sizeDelta.y;
        targetHeight = expandedHeight;

        if (GameOptions.ColourBlindMode)
        {
            selectedColor = new Color(0.36f, 0.63f, 0.94f);
            errorColor = new Color(0.85f, 0.45f, 0.13f);
            pendingChangeColor = new Color(0.55f, 0.45f, 0.85f);
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

        GrowTo(Mathf.Max(1, saved.processors), Mathf.Max(1, saved.columns));
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

        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                if (rows[p].Cells[c].Instruction != null && ReferencesCoord(rows[p].Cells[c].Instruction, coord))
                {
                    rows[p].Cells[c].Image.color = pendingChangeColor;
                }
            }
        }

        Debug.Log("This change affects existing instructions - click the tile again to confirm.");
    }

    public bool IsPendingChange(HexCoord coord)
    {
        return pendingChangeCoord.HasValue && pendingChangeCoord.Value.Equals(coord);
    }

    public void ClearPendingChange()
    {
        if (!pendingChangeCoord.HasValue) return;

        HexCoord coord = pendingChangeCoord.Value;
        pendingChangeCoord = null;

        for (int p = 0; p < rows.Count; p++)
        {
            for (int c = 0; c < rows[p].Cells.Count; c++)
            {
                if (rows[p].Cells[c].Instruction != null && ReferencesCoord(rows[p].Cells[c].Instruction, coord))
                {
                    RestoreCellColor(rows[p].Cells[c], p, c);
                }
            }
        }
    }

    private void RestoreCellColor(Cell cell, int p, int c)
    {
        cell.Image.color = (p == SelectedProcessor && c == SelectedColumn) ? selectedColor : normalCellColor;
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
        rowObj.transform.Find("RowLabel").GetComponent<TextMeshProUGUI>().text = "P" + (rows.Count + 1);

        ProcessorRow row = new ProcessorRow { Root = rowObj };
        for (int c = 0; c < columnCount; c++)
        {
            row.Cells.Add(CreateCell(row, rows.Count, c));
        }
        rows.Add(row);
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
    }

    public void SetActiveColumn(int columnIndex)
    {
        ClearActiveColumn();
        if (columnIndex >= 0 && columnIndex < headerLabels.Count)
        {
            headerLabels[columnIndex].color = selectedColor;
            headerLabels[columnIndex].fontStyle = FontStyles.Bold;
            activeColumn = columnIndex;
        }
    }

    public void ClearActiveColumn()
    {
        if (activeColumn >= 0 && activeColumn < headerLabels.Count)
        {
            headerLabels[activeColumn].color = Color.black;
            headerLabels[activeColumn].fontStyle = FontStyles.Normal;
        }
        activeColumn = -1;
    }

    private Cell CreateCell(ProcessorRow row, int processorIndex, int columnIndex)
    {
        GameObject cellObj = Instantiate(cellTemplate, row.Root.transform);
        cellObj.SetActive(true);
        cellObj.name = "Cell_" + (processorIndex + 1) + "_" + (columnIndex + 1);

        Cell cell = new Cell
        {
            Image = cellObj.GetComponent<Image>(),
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
        return cell;
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
    }

    public void SelectCell(int processorIndex, int columnIndex)
    {
        if (inventoryUI != null) inventoryUI.DeselectTile();

        Cell previous = GetCell(SelectedProcessor, SelectedColumn);
        if (previous != null) previous.Image.color = normalCellColor;

        SelectedProcessor = processorIndex;
        SelectedColumn = columnIndex;

        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell != null) cell.Image.color = selectedColor;

        OnCellSelected?.Invoke();
    }

    public void DeselectCell()
    {
        Cell previous = GetCell(SelectedProcessor, SelectedColumn);
        if (previous != null) previous.Image.color = normalCellColor;
        SelectedProcessor = -1;
        SelectedColumn = -1;
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

    private void RenderInstruction(Cell cell, InstructionData instruction)
    {
        Transform container = cell.InstructionContainer;

        if (instruction is MoveInstructionData move)
        {
            SpawnSquareToken(container, move.Source);
            SpawnArrowToken(container);
            SpawnSquareToken(container, move.Destination);
        }
        else if (instruction is SelectInstructionData select)
        {
            SpawnSquareToken(container, select.Source, "{{{0}}}");
        }
        else if (instruction is OperationInstructionData op)
        {
            if (op.Operation == OperationTileData.OperationType.SquareRoot)
            {
                SpawnSquareRootToken(container, op.Source);
                return;
            }

            SpawnSquareToken(container, op.Source);
            if (op.AdditionalOperands.Count == 0)
            {
                SpawnSquareToken(container, op.OperationTile);
            }
            for (int i = 0; i < op.AdditionalOperands.Count; i++)
            {
                SpawnSquareToken(container, op.OperationTile);
                SpawnSquareToken(container, op.AdditionalOperands[i]);
            }
        }
    }

    private void SpawnSquareToken(Transform container, HexCoord coord, string format = "{0}")
    {
        GameObject token = Instantiate(squareTokenTemplate, container);
        token.SetActive(true);

        InstructionToken instructionToken = token.GetComponent<InstructionToken>();
        instructionToken.Setup(tileSelector, coord);

        TileData tile = labelController.boardState.GetTile(coord);
        instructionToken.SetText(tile != null ? string.Format(format, tile.GetDisplayValue()) : "");
    }

    private void SpawnArrowToken(Transform container)
    {
        GameObject token = Instantiate(arrowTokenTemplate, container);
        token.SetActive(true);
    }

    private void SpawnSquareRootToken(Transform container, HexCoord coord)
    {
        GameObject wrapper = new GameObject("SquareRootToken", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        wrapper.transform.SetParent(container, false);
        HorizontalLayoutGroup wrapperHLG = wrapper.GetComponent<HorizontalLayoutGroup>();
        wrapperHLG.childAlignment = TextAnchor.MiddleLeft;
        wrapperHLG.childForceExpandWidth = false;
        wrapperHLG.childForceExpandHeight = false;
        wrapperHLG.childControlWidth = true;
        wrapperHLG.childControlHeight = true;
        wrapperHLG.spacing = 0f;
        LayoutElement wrapperLE = wrapper.AddComponent<LayoutElement>();
        wrapperLE.preferredWidth = 30f;
        wrapperLE.preferredHeight = 18f;

        GameObject radical = new GameObject("RadicalSign", typeof(RectTransform), typeof(TextMeshProUGUI));
        radical.transform.SetParent(wrapper.transform, false);
        TextMeshProUGUI radicalTMP = radical.GetComponent<TextMeshProUGUI>();
        radicalTMP.text = "\u221A";
        radicalTMP.fontSize = 15;
        radicalTMP.color = Color.black;
        radicalTMP.alignment = TextAlignmentOptions.Center;
        LayoutElement radicalLE = radical.AddComponent<LayoutElement>();
        radicalLE.preferredWidth = 12f;
        radicalLE.preferredHeight = 18f;

        GameObject square = Instantiate(squareTokenTemplate, wrapper.transform);
        square.SetActive(true);
        InstructionToken instructionToken = square.GetComponent<InstructionToken>();
        instructionToken.Setup(tileSelector, coord);
        TileData tile = labelController.boardState.GetTile(coord);
        instructionToken.SetText(tile != null ? tile.GetDisplayValue() : "");

        GameObject overline = new GameObject("Overline", typeof(RectTransform), typeof(Image));
        overline.transform.SetParent(square.transform, false);
        overline.GetComponent<Image>().color = Color.black;
        RectTransform overlineRT = overline.GetComponent<RectTransform>();
        overlineRT.anchorMin = new Vector2(0f, 1f);
        overlineRT.anchorMax = new Vector2(1f, 1f);
        overlineRT.pivot = new Vector2(0.5f, 1f);
        overlineRT.sizeDelta = new Vector2(0f, 1.5f);
        overlineRT.anchoredPosition = Vector2.zero;
    }

    private Cell GetCell(int processorIndex, int columnIndex)
    {
        if (processorIndex < 0 || processorIndex >= rows.Count) return null;
        if (columnIndex < 0 || columnIndex >= rows[processorIndex].Cells.Count) return null;
        return rows[processorIndex].Cells[columnIndex];
    }
}

public class CellClickHandler : MonoBehaviour, IPointerClickHandler
{
    public System.Action<PointerEventData> OnClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }
}

