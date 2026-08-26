using System.Collections.Generic;
using UnityEngine;

public class InstructionAuthoringController : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexTileSelector tileSelector;

    private enum State { Idle, SubjectSelected, AwaitingMoveTarget, AwaitingOperationTile, AwaitingOperands }

    private State state = State.Idle;
    private HexCoord subject;
    private HexCoord operationTile;
    private HexCoord moveTarget;
    private bool hasMoveTarget;
    private List<HexCoord> operands = new List<HexCoord>();

    public bool IsBusy => state != State.Idle;

    private void OnEnable()
    {
        tileSelector.OnTileClicked += HandleTileClicked;
    }

    private void OnDisable()
    {
        tileSelector.OnTileClicked -= HandleTileClicked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            gridController.AddProcessorRow();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Reset();
            return;
        }

        switch (state)
        {
            case State.SubjectSelected:
                if (Input.GetKeyDown(KeyCode.Z)) state = State.AwaitingMoveTarget;
                else if (Input.GetKeyDown(KeyCode.C)) state = State.AwaitingOperationTile;
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (labelController.boardState.GetTile(subject) is NumberTileData)
                    {
                        gridController.PlaceInstruction(gridController.SelectedProcessor, gridController.SelectedColumn,
                            new SelectInstructionData { Source = subject });
                        Reset();
                    }
                    else
                    {
                        Debug.Log("Space-select requires a number tile.");
                    }
                }
                break;

            case State.AwaitingMoveTarget:
            case State.AwaitingOperands:
                if (Input.GetKeyDown(KeyCode.D)) CommitPending();
                break;
        }
    }

    private void HandleTileClicked(HexCoord coord)
    {
        switch (state)
        {
            case State.Idle:
                subject = coord;
                operands.Clear();
                hasMoveTarget = false;
                state = State.SubjectSelected;
                break;

            case State.AwaitingMoveTarget:
                if (AreAdjacent(subject, coord))
                {
                    moveTarget = coord;
                    hasMoveTarget = true;
                    Debug.Log($"Move target set: {coord.q},{coord.r}. Press D to commit.");
                }
                else
                {
                    Debug.Log("Destination must be adjacent to the source tile.");
                }
                break;

            case State.AwaitingOperationTile:
                if (AreAdjacent(subject, coord) && labelController.boardState.GetTile(coord) is OperationTileData)
                {
                    operationTile = coord;
                    operands.Clear();
                    state = State.AwaitingOperands;
                    Debug.Log("Operation tile set. Click operand tiles (adjacent to it), then press D.");
                }
                else
                {
                    Debug.Log("Must click an operation tile adjacent to the source tile.");
                }
                break;

            case State.AwaitingOperands:
                if (AreAdjacent(operationTile, coord))
                {
                    operands.Add(coord);
                    Debug.Log($"Operand added: {coord.q},{coord.r} ({operands.Count} total).");
                }
                else
                {
                    Debug.Log("Operands must be adjacent to the operation tile.");
                }
                break;
        }
    }

    private void CommitPending()
    {
        if (state == State.AwaitingMoveTarget)
        {
            if (!hasMoveTarget)
            {
                Debug.Log("Move incomplete — click an adjacent destination tile first.");
                return;
            }

            gridController.PlaceInstruction(gridController.SelectedProcessor, gridController.SelectedColumn,
                new MoveInstructionData { Source = subject, Destination = moveTarget });
            Reset();
        }
        else if (state == State.AwaitingOperands)
        {
            OperationTileData opData = labelController.boardState.GetTile(operationTile) as OperationTileData;
            if (opData == null) return;

            if (opData.operation != OperationTileData.OperationType.Factorial && operands.Count == 0)
            {
                Debug.Log($"{opData.GetDisplayValue()} needs at least one operand tile. Click one adjacent to the operation tile, or press Escape to cancel.");
                return;
            }

            gridController.PlaceInstruction(gridController.SelectedProcessor, gridController.SelectedColumn,
                new OperationInstructionData
                {
                    Source = subject,
                    OperationTile = operationTile,
                    Operation = opData.operation,
                    AdditionalOperands = new List<HexCoord>(operands)
                });
            Reset();
        }
    }

    private void Reset()
    {
        state = State.Idle;
        operands.Clear();
        hasMoveTarget = false;
    }

    private bool AreAdjacent(HexCoord a, HexCoord b)
    {
        for (int i = 0; i < 6; i++)
        {
            if (a.GetNeighbor(i).Equals(b)) return true;
        }
        return false;
    }
}
