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
    [SerializeField] private Color selectedColor = new Color(0.635f, 0.843f, 0.890f);

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

    public int SelectedProcessor { get; private set; }
    public int SelectedColumn { get; private set; }

    private void Start()
    {
        AddProcessorRow();
        AddColumn();
        SelectCell(0, 0);
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
