// InstructionAuthoringController — the state machine that turns click/keypress input
// into program instructions for the InfoTab (columns = time steps, rows = processors).
// Consumes HexTileSelector clicks and writes finished instructions via GridController.
using System.Collections.Generic;
using UnityEngine;

// State flow: Idle → SubjectSelected (click a tile) → AwaitingMoveTarget (Z) or
// AwaitingOperationTile (C) → AwaitingOperands. Invalid clicks restart the subject
// instead of dead-ending the flow.
public class InstructionAuthoringController : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexTileSelector tileSelector;

    // One value per authoring stage; transitions live in Update/HandleTileClicked.
    private enum State { Idle, SubjectSelected, AwaitingMoveTarget, AwaitingOperationTile, AwaitingOperands }

    private State state = State.Idle;
    // Tile the instruction reads from / moves from.
    private HexCoord subject;
    // The operator hex the subject merges onto.
    private HexCoord operationTile;
    private HexCoord moveTarget;
    private bool hasMoveTarget;
    // List, not HashSet: insertion order = left-to-right evaluation order at runtime.
    private List<HexCoord> operands = new List<HexCoord>();

    // True while an instruction is mid-authoring; HexTileSelector routes clicks to
    // authoring (not placement) based on this.
    public bool IsBusy => state != State.Idle;

    // Tutorial read access: true after C was pressed and the operator tile
    // was picked - i.e. the operand selection stage.
    public bool IsAwaitingOperands => state == State.AwaitingOperands;

    public GridController GridController => gridController;

    public void CancelPending()
    {
        if (state != State.Idle)
        {
            Reset();
            Debug.Log("Instruction cancelled - palette selection takes priority.");
        }
    }

    public void ExitInputMode()
    {
        CancelPending();
        gridController.DeselectCell();
    }

    private void OnEnable()
    {
        tileSelector.OnTileClicked += HandleTileClicked;
    }

    private void OnDisable()
    {
        tileSelector.OnTileClicked -= HandleTileClicked;
    }

    // Global keys: Enter = new processor row, Escape = cancel everything.
    // Per-state keys: Z/C/Space from SubjectSelected, D commits (see CommitPending).
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (TutorialGate.Active && !TutorialGate.AllowProcessorKey) return;
            gridController.AddProcessorRow();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Reset();
            if (gridController != null) gridController.ClearPendingChange();
            return;
        }

        switch (state)
        {
            case State.SubjectSelected:
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (TutorialGate.Active && !TutorialGate.AllowMoveKey) return;
                    state = State.AwaitingMoveTarget;
                    if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, tileSelector.GetExistingNeighbors(subject));
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    if (TutorialGate.Active && !TutorialGate.AllowOperationKey) return;
                    state = State.AwaitingOperationTile;
                    if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, tileSelector.GetAdjacentOperationTiles(subject));
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (TutorialGate.Active && !TutorialGate.AllowSpaceKey) return;
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
                if (Input.GetKeyDown(KeyCode.D))
                {
                    if (TutorialGate.Active && !TutorialGate.AllowCommitKey) return;
                    CommitPending();
                }
                break;
        }
    }

    // Routes a 3D hex click (via HexTileSelector.OnTileClicked) through the states.
    private void HandleTileClicked(HexCoord coord)
    {
        switch (state)
        {
            case State.Idle:
            case State.SubjectSelected:
                if (gridController.SelectedColumn < 0)
                {
                    Debug.Log("Select a cell in the coding grid first.");
                    break;
                }

                subject = coord;
                operands.Clear();
                hasMoveTarget = false;
                state = State.SubjectSelected;
                if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, null);
                Debug.Log($"Subject set: {coord.q},{coord.r}. Press Z (move), C (operation) or Space (select).");
                break;

            case State.AwaitingMoveTarget:
                if (AreAdjacent(subject, coord))
                {
                    moveTarget = coord;
                    hasMoveTarget = true;
                    Debug.Log($"Move target set: {coord.q},{coord.r}. Press D to commit.");
                }
                else if (!TryRestartSubject(coord))
                {
                    Debug.Log("Destination must be adjacent to the source tile.");
                }
                break;

            case State.AwaitingOperationTile:
                if (AreAdjacent(subject, coord) && labelController.boardState.GetTile(coord) is OperationTileData opData)
                {
                    operationTile = coord;
                    operands.Clear();

                    bool unary = opData.operation == OperationTileData.OperationType.Factorial
                              || opData.operation == OperationTileData.OperationType.SquareRoot;
                    if (unary)
                    {
                        // Unary operations take no operands - clicking one commits
                        // the instruction outright, no operand stage, no extra prompt.
                        gridController.PlaceInstruction(gridController.SelectedProcessor, gridController.SelectedColumn,
                            new OperationInstructionData
                            {
                                Source = subject,
                                OperationTile = operationTile,
                                Operation = opData.operation,
                                AdditionalOperands = new List<HexCoord>()
                            });
                        Reset();
                    }
                    else
                    {
                        state = State.AwaitingOperands;
                        if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, GetOperandCandidates());
                        Debug.Log("Operation tile set. Click operand tiles (adjacent to it), then press D.");
                    }
                }
                else if (!TryRestartSubject(coord))
                {
                    Debug.Log("Must click an operation tile adjacent to the source tile.");
                }
                break;

            case State.AwaitingOperands:
                if (coord.Equals(subject))
                {
                    Debug.Log("The subject can't also be an operand.");
                }
                else if (operands.Contains(coord))
                {
                    Debug.Log("That tile is already an operand.");
                }
                else if (AreAdjacent(operationTile, coord))
                {
                    // Append order = evaluation order at runtime (see operands field).
                    operands.Add(coord);
                    if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, GetOperandCandidates());
                    Debug.Log($"Operand added: {coord.q},{coord.r} ({operands.Count} total).");
                }
                else if (!TryRestartSubject(coord))
                {
                    Debug.Log("Operands must be adjacent to the operation tile.");
                }
                break;
        }
    }

    // Still-clickable operands: op-tile neighbors minus subject and already-chosen ones.
    private List<HexCoord> GetOperandCandidates()
    {
        List<HexCoord> result = new List<HexCoord>();
        if (tileSelector == null) return result;

        foreach (HexCoord neighbor in tileSelector.GetExistingNeighbors(operationTile))
        {
            if (neighbor.Equals(subject)) continue;
            if (operands.Contains(neighbor)) continue;
            result.Add(neighbor);
        }
        return result;
    }

    // A click on an existing but invalid tile re-targets the subject instead of
    // dead-ending; returns false for empty space so the caller can log the real miss.
    private bool TryRestartSubject(HexCoord coord)
    {
        if (labelController.boardState.GetTile(coord) == null) return false;

        subject = coord;
        operands.Clear();
        hasMoveTarget = false;
        state = State.SubjectSelected;
        if (tileSelector != null) tileSelector.ShowAuthoringPrompt(subject, null);
        Debug.Log($"Subject changed to {coord.q},{coord.r}. Press Z (move), C (operation) or Space (select).");
        return true;
    }

    // D key: write the finished Move/Operation instruction into the selected cell.
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

            bool unary = opData.operation == OperationTileData.OperationType.Factorial
                      || opData.operation == OperationTileData.OperationType.SquareRoot;
            if (!unary && operands.Count == 0)
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
        if (tileSelector != null) tileSelector.ClearAuthoringPrompt();
    }

    // Hex adjacency: b is one of a's six immediate neighbors.
    private bool AreAdjacent(HexCoord a, HexCoord b)
    {
        for (int i = 0; i < 6; i++)
        {
            if (a.GetNeighbor(i).Equals(b)) return true;
        }
        return false;
    }
}
