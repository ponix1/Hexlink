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
                }
                break;

            case State.AwaitingOperationTile:
                if (AreAdjacent(subject, coord) && labelController.boardState.GetTile(coord) is OperationTileData)
                {
                    operationTile = coord;
                    operands.Clear();
                    state = State.AwaitingOperands;
                }
                break;

            case State.AwaitingOperands:
                if (AreAdjacent(operationTile, coord))
                {
                    operands.Add(coord);
                }
                break;
        }
    }

    private void CommitPending()
    {
        if (state == State.AwaitingMoveTarget && hasMoveTarget)
        {
            gridController.PlaceInstruction(gridController.SelectedProcessor, gridController.SelectedColumn,
                new MoveInstructionData { Source = subject, Destination = moveTarget });
            Reset();
        }
        else if (state == State.AwaitingOperands)
        {
            OperationTileData opData = labelController.boardState.GetTile(operationTile) as OperationTileData;
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
