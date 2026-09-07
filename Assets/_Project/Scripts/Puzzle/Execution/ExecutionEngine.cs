using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExecutionEngine : MonoBehaviour
{
    [SerializeField] private GridController gridController;
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexGridSpawner hexGridSpawner;
    [SerializeField] private Button playButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject tileLabelPrefab;
    [SerializeField] private float nodeHeightY = 0.59f;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private float spawnDuration = 0.25f;
    [SerializeField] private float mergePulseDuration = 0.3f;

    private Dictionary<HexCoord, NumberCircle> circles = new Dictionary<HexCoord, NumberCircle>();
    private bool running;
    private float labelWorldScale = 1f;

    private void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (resetButton != null) resetButton.onClick.AddListener(ResetExecution);
    }

    public void Play()
    {
        if (running) return;
        if (nodePrefab == null || tileLabelPrefab == null)
        {
            Debug.LogError("ExecutionEngine: node prefab or tile label prefab missing (re-run Hexlink/Build Info Tab).");
            return;
        }
        StartCoroutine(RunProgram());
    }

    public void ResetExecution()
    {
        StopAllCoroutines();
        running = false;
        foreach (NumberCircle circle in circles.Values)
        {
            Destroy(circle.gameObject);
        }
        circles.Clear();
        gridController.ClearErrors();
    }

    private IEnumerator RunProgram()
    {
        running = true;
        InitGeometry();

        for (int column = 0; column < gridController.ColumnCount; column++)
        {
            List<GridController.CellInstruction> instructions = gridController.GetColumnInstructions(column);
            if (instructions.Count == 0) continue;

            // Commit phase: instructions may depend on each other (a move vacating the tile
            // an operation needs, a select creating a circle another instruction uses).
            // Retry blocked instructions until a fixpoint; errors are only final once no
            // further commits are possible.
            List<Attempt> attempts = new List<Attempt>();
            foreach (GridController.CellInstruction ci in instructions)
            {
                attempts.Add(new Attempt { CI = ci });
            }

            int remaining = attempts.Count;
            while (remaining > 0)
            {
                bool progress = false;
                foreach (Attempt attempt in attempts)
                {
                    if (attempt.Committed) continue;

                    if (TryCommit(attempt.CI, out IEnumerator animation, out string error))
                    {
                        attempt.Animation = animation;
                        attempt.Committed = true;
                        remaining--;
                        progress = true;
                    }
                    else
                    {
                        attempt.Error = error;
                    }
                }

                if (!progress) break;
            }

            if (remaining > 0 && TryCommitMoveCycle(attempts))
            {
                remaining = 0;
            }

            foreach (Attempt attempt in attempts)
            {
                if (!attempt.Committed) Fail(attempt.CI, attempt.Error);
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            foreach (Attempt attempt in attempts)
            {
                if (attempt.Committed && attempt.Animation != null) routines.Add(attempt.Animation);
            }

            if (routines.Count > 0)
            {
                yield return All(routines);
            }
        }

        running = false;
    }

    private class Attempt
    {
        public GridController.CellInstruction CI;
        public IEnumerator Animation;
        public string Error;
        public bool Committed;
    }

    private bool TryCommit(GridController.CellInstruction ci, out IEnumerator animation, out string error)
    {
        animation = null;
        if (ci.Instruction is SelectInstructionData select) return TryCommitSelect(ci, select, out animation, out error);
        if (ci.Instruction is MoveInstructionData move) return TryCommitMove(ci, move, out animation, out error);
        if (ci.Instruction is OperationInstructionData op) return TryCommitOperation(ci, op, out animation, out error);
        error = "Unknown instruction type.";
        return false;
    }

    private bool TryCommitSelect(GridController.CellInstruction ci, SelectInstructionData select, out IEnumerator animation, out string error)
    {
        animation = null;
        if (circles.ContainsKey(select.Source))
        {
            error = "Spawn failed - tile already occupied by a circle.";
            return false;
        }
        if (!hexGridSpawner.SpawnedTiles.ContainsKey(select.Source))
        {
            error = "Spawn failed - source tile does not exist.";
            return false;
        }
        if (!(labelController.boardState.GetTile(select.Source) is NumberTileData number))
        {
            error = "Spawn failed - source tile is not a number tile.";
            return false;
        }

        NumberCircle circle = CreateCircle(select.Source);
        circle.SetValue(number.value);
        circles[select.Source] = circle;
        animation = circle.SpawnAnimation(spawnDuration);
        error = null;
        return true;
    }

    private bool TryCommitMove(GridController.CellInstruction ci, MoveInstructionData move, out IEnumerator animation, out string error)
    {
        animation = null;
        if (!circles.TryGetValue(move.Source, out NumberCircle circle))
        {
            error = "Move failed - no circle on the source tile.";
            return false;
        }
        if (!hexGridSpawner.SpawnedTiles.ContainsKey(move.Destination))
        {
            error = "Move failed - destination tile does not exist.";
            return false;
        }
        if (circles.ContainsKey(move.Destination))
        {
            error = "Move failed - destination tile already occupied.";
            return false;
        }

        circles.Remove(move.Source);
        circles[move.Destination] = circle;
        circle.SetCoord(move.Destination);

        animation = MoveAndCheckWin(circle, move.Destination);
        error = null;
        return true;
    }

    private IEnumerator MoveAndCheckWin(NumberCircle circle, HexCoord destination)
    {
        yield return circle.MoveTo(Anchor(destination), moveDuration);
        CheckWin(destination, circle);
    }

    private bool TryCommitOperation(GridController.CellInstruction ci, OperationInstructionData op, out IEnumerator animation, out string error)
    {
        animation = null;
        OperationTileData opData = labelController.boardState.GetTile(op.OperationTile) as OperationTileData;
        if (opData == null)
        {
            error = "Operation failed - operation tile no longer exists.";
            return false;
        }
        if (!circles.TryGetValue(op.Source, out NumberCircle subject))
        {
            error = "Operation failed - no circle on the source tile.";
            return false;
        }
        if (circles.ContainsKey(op.OperationTile))
        {
            error = "Operation failed - operation tile already occupied.";
            return false;
        }

        List<NumberCircle> operandCircles = new List<NumberCircle>();
        foreach (HexCoord operandCoord in op.AdditionalOperands)
        {
            if (!circles.TryGetValue(operandCoord, out NumberCircle operandCircle))
            {
                error = $"Operation failed - no circle on operand tile {operandCoord.q},{operandCoord.r}.";
                return false;
            }
            operandCircles.Add(operandCircle);
        }

        int result = subject.Value;
        if (opData.operation == OperationTileData.OperationType.Factorial
            || opData.operation == OperationTileData.OperationType.SquareRoot)
        {
            result = ApplyUnary(opData.operation, result);
        }
        else
        {
            foreach (NumberCircle operandCircle in operandCircles)
            {
                if (opData.operation == OperationTileData.OperationType.Divide && operandCircle.Value == 0)
                {
                    error = "Operation failed - division by zero.";
                    return false;
                }
                result = Apply(opData.operation, result, operandCircle.Value);
            }
        }

        circles.Remove(op.Source);
        foreach (NumberCircle operandCircle in operandCircles)
        {
            circles.Remove(operandCircle.Coord);
        }
        circles[op.OperationTile] = subject;
        subject.SetCoord(op.OperationTile);

        animation = MergeCircles(subject, operandCircles, result);
        error = null;
        return true;
    }

    // Resolves move cycles/chains (swaps, rotations) that single-pass commits can't:
    // every destination must be empty or vacated by another move in the same group.
    private bool TryCommitMoveCycle(List<Attempt> attempts)
    {
        List<Attempt> pending = new List<Attempt>();
        foreach (Attempt attempt in attempts)
        {
            if (!attempt.Committed) pending.Add(attempt);
        }
        if (pending.Count == 0) return false;

        List<MoveInstructionData> moves = new List<MoveInstructionData>();
        foreach (Attempt attempt in pending)
        {
            if (!(attempt.CI.Instruction is MoveInstructionData move)) return false;
            moves.Add(move);
        }

        HashSet<HexCoord> sources = new HashSet<HexCoord>();
        HashSet<HexCoord> destinations = new HashSet<HexCoord>();
        foreach (MoveInstructionData move in moves)
        {
            if (!circles.ContainsKey(move.Source)) return false;
            if (!hexGridSpawner.SpawnedTiles.ContainsKey(move.Destination)) return false;
            if (!sources.Add(move.Source)) return false;
            if (!destinations.Add(move.Destination)) return false;
        }
        foreach (MoveInstructionData move in moves)
        {
            if (circles.ContainsKey(move.Destination) && !sources.Contains(move.Destination)) return false;
        }

        List<NumberCircle> movingCircles = new List<NumberCircle>();
        foreach (MoveInstructionData move in moves)
        {
            movingCircles.Add(circles[move.Source]);
        }
        foreach (MoveInstructionData move in moves)
        {
            circles.Remove(move.Source);
        }
        for (int i = 0; i < moves.Count; i++)
        {
            circles[moves[i].Destination] = movingCircles[i];
            movingCircles[i].SetCoord(moves[i].Destination);
        }

        for (int i = 0; i < pending.Count; i++)
        {
            pending[i].Animation = MoveAndCheckWin(movingCircles[i], moves[i].Destination);
            pending[i].Committed = true;
            pending[i].Error = null;
        }
        return true;
    }

    private IEnumerator MergeCircles(NumberCircle subject, List<NumberCircle> operandCircles, int result)
    {
        Vector3 target = Anchor(subject.Coord);

        List<IEnumerator> moves = new List<IEnumerator>();
        moves.Add(subject.MoveTo(target, moveDuration));
        foreach (NumberCircle operandCircle in operandCircles)
        {
            moves.Add(operandCircle.MoveTo(target, moveDuration));
        }
        yield return All(moves);

        foreach (NumberCircle operandCircle in operandCircles)
        {
            Destroy(operandCircle.gameObject);
        }
        subject.SetValue(result);
        yield return subject.MergePulse(mergePulseDuration);
    }

    private int Apply(OperationTileData.OperationType operation, int a, int b)
    {
        switch (operation)
        {
            case OperationTileData.OperationType.Add: return a + b;
            case OperationTileData.OperationType.Subtract: return a - b;
            case OperationTileData.OperationType.Multiply: return a * b;
            case OperationTileData.OperationType.Divide: return a / b;
            case OperationTileData.OperationType.Power: return Mathf.RoundToInt(Mathf.Pow(a, b));
            default: return a;
        }
    }

    private int ApplyUnary(OperationTileData.OperationType operation, int a)
    {
        if (operation == OperationTileData.OperationType.SquareRoot) return Mathf.RoundToInt(Mathf.Sqrt(a));

        int result = 1;
        for (int i = 2; i <= a; i++) result *= i;
        return result;
    }

    private void CheckWin(HexCoord coord, NumberCircle circle)
    {
        if (labelController.boardState.GetTile(coord) is FinalTileData final && final.targetNumber == circle.Value)
        {
            running = false;
            Debug.Log($"Puzzle solved! {circle.Value} reached the target tile.");
            StopAllCoroutines();
        }
    }

    private void Fail(GridController.CellInstruction ci, string message)
    {
        gridController.FlagError(ci.Processor, ci.Column);
        Debug.LogWarning($"[P{ci.Processor + 1}, col {ci.Column + 1}] {message}");
    }

    private NumberCircle CreateCircle(HexCoord coord)
    {
        GameObject go = Instantiate(nodePrefab);
        foreach (Collider collider in go.GetComponentsInChildren<Collider>())
        {
            Destroy(collider);
        }

        go.transform.SetPositionAndRotation(Anchor(coord), Quaternion.identity);

        NumberCircle circle = go.AddComponent<NumberCircle>();
        circle.Setup(coord, tileLabelPrefab, labelWorldScale);
        return circle;
    }

    private void InitGeometry()
    {
        IReadOnlyDictionary<HexCoord, GameObject> tiles = hexGridSpawner.SpawnedTiles;
        if (tiles.Count == 0) return;

        foreach (GameObject tile in tiles.Values)
        {
            labelWorldScale = tile.transform.localScale.x;
            break;
        }
    }

    private Vector3 Anchor(HexCoord coord)
    {
        Vector3 tilePos = hexGridSpawner.SpawnedTiles[coord].transform.position;
        return new Vector3(tilePos.x, nodeHeightY, tilePos.z);
    }

    private IEnumerator All(List<IEnumerator> routines)
    {
        int remaining = routines.Count;
        foreach (IEnumerator routine in routines)
        {
            StartCoroutine(Wrap(routine, () => remaining--));
        }
        while (remaining > 0)
        {
            yield return null;
        }
    }

    private IEnumerator Wrap(IEnumerator routine, System.Action done)
    {
        yield return routine;
        done();
    }
}
