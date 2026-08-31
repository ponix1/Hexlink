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
    private int columnCount;
    private RectTransform tabRect;
    private TextMeshProUGUI collapseLabel;
    private bool collapsed;
    private float expandedHeight;
    private float targetHeight;

    public int SelectedProcessor { get; private set; }
    public int SelectedColumn { get; private set; }

    private void Start()
    {
        tabRect = (RectTransform)transform;
        expandedHeight = tabRect.sizeDelta.y;
        targetHeight = expandedHeight;

        if (collapseButton != null)
        {
            collapseLabel = collapseButton.GetComponentInChildren<TextMeshProUGUI>();
            collapseButton.onClick.AddListener(ToggleCollapsed);
        }

        AddProcessorRow();
        AddColumn();
        SelectCell(0, 0);
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
        headerCell.transform.Find("NumberText").GetComponent<TextMeshProUGUI>().text = columnCount.ToString();

        for (int p = 0; p < rows.Count; p++)
        {
            rows[p].Cells.Add(CreateCell(rows[p], p, columnCount - 1));
        }
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
        if (previous != null) previous.Image.color = Color.white;

        SelectedProcessor = processorIndex;
        SelectedColumn = columnIndex;

        Cell cell = GetCell(processorIndex, columnIndex);
        if (cell != null) cell.Image.color = selectedColor;
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
