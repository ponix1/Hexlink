// Level 1 interactive tutorial. TutorialLauncher auto-spawns TutorialRunner in
// Puzzle_Play for puzzle "L-1" (until completed once). The runner docks a panel
// flush against the right edge of the screen: a TUTORIAL header, short
// explanation text, then the page's TASKS - each row flips green the moment
// the player does it, and the thing a task refers to gets a thick pulsing
// accent ring. Info pages additionally dim the whole screen except the panel
// and the highlighted element (spotlight). A dimmed Next button unlocks once
// every task is green. Input stays gated via TutorialGate, re-locked per task.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Spawns the Level 1 tutorial when the player enters Level 1 until the
// tutorial itself has been completed once. Uses its own PlayerPrefs key so a
// pre-tutorial completion of the level never blocks it.
public static class TutorialLauncher
{
    public const string DoneKey = "Hexlink.TutorialDone";

    public static bool TutorialCompleted()
    {
        return PlayerPrefs.GetInt(DoneKey, 0) == 1;
    }

    public static void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Spawn();
        Spawn();
    }

    private static void Spawn()
    {
        if (SceneManager.GetActiveScene().name != "Puzzle_Play") return;

        PuzzleData puzzle = PuzzleSelection.SelectedPuzzle;
        bool complete = TutorialCompleted();
        bool exists = Object.FindAnyObjectByType<TutorialRunner>() != null;

        if (puzzle == null || puzzle.puzzleID != "L-1") return;
        if (complete) return;
        if (exists) return;

        new GameObject("LevelTutorial").AddComponent<TutorialRunner>();
    }
}

public class TutorialRunner : MonoBehaviour
{
    // Fixed blue outline so the tutorial panel is instantly recognisable.
    private static readonly Color OutlineBlue = new Color(0.204f, 0.765f, 1f, 0.95f);
    // Task-complete green (same shade the solutions window uses for solved).
    private static readonly Color TaskGreen = new Color(0.30f, 0.78f, 0.47f);
    // Spotlight scrim for info pages; clicks pass through untouched.
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);

    // Panel geometry, in canvas units measured from the top of the panel.
    private const float PanelWidth = 460f;
    private const float HeaderTop = 14f;
    private const float HeaderHeight = 36f;
    private const float BodyTop = HeaderTop + HeaderHeight + 10f;
    private const float BodyHeight = 150f;
    private const float DividerY = BodyTop + BodyHeight + 12f;
    private const float TasksHeaderY = DividerY + 12f;
    private const float RowsY = TasksHeaderY + 20f + 8f;
    private const float RowStep = 30f;
    private const float FooterHeight = 64f;
    private const float InfoHeight = BodyTop + BodyHeight + 20f + FooterHeight;
    // Spotlight hole padding around the highlighted element.
    private const float HolePadding = 12f;

    // One task row: what to do, what to highlight while it's open, and the
    // completion predicate polled in Update.
    private class TaskItem
    {
        public string Label;
        // Transform name to ring while this task is the first open one.
        public string Anchor;
        // Completion predicate polled in Update.
        public System.Func<bool> Done;
    }

    // One tutorial page. Info pages explain (with the screen dimmed to a
    // spotlight) and wait for Next; task pages keep the screen normal and
    // unlock Next only when every task is green. Locks receives the index of
    // the first incomplete task so permissions tighten/loosen per task.
    // AutoAdvance pages move on by themselves once every task is green (used
    // after a win so the next instruction lands before the results popup).
    private class Step
    {
        public string Text;
        // Highlighted on info pages (and as a fallback on task pages).
        public string Anchor;
        // True = explanation only, no tasks.
        public bool InfoOnly;
        // True = advance the moment every task is green (no Next click).
        public bool AutoAdvance;
        // Permissions for the first incomplete task index.
        public System.Action<int> Locks;
        public List<TaskItem> Tasks = new List<TaskItem>();
    }

    // Scene systems the tutorial observes, locks, and validates against.
    private Canvas canvas;
    private TileInventoryUI inventoryUI;
    private GridController grid;
    private HexTileLabelController labelController;
    private InstructionAuthoringController authoring;

    // Panel UI pieces, built once in BuildPanel.
    private GameObject panelRoot;
    private RectTransform panelRT;
    private TextMeshProUGUI bodyText;
    private GameObject tasksHeader;
    private GameObject divider;
    private RectTransform taskRows;
    private Button nextButton;
    private CanvasGroup nextGroup;
    private GameObject highlight;

    // Spotlight dim: four band images framing a hole around the highlight.
    private GameObject dimRoot;
    private readonly Image[] dimBands = new Image[4];
    // What the dim hole exposes: a UI transform, or the world-space board.
    private Transform dimHoleTarget;
    private bool dimHoleBoard;
    // Anchor the current ring targets; retried in Update so late-spawning
    // targets (e.g. the results popup's Continue button) get ringed too.
    private string currentAnchorName;

    // Per-page task row labels, index-aligned with the active step's tasks.
    private readonly List<TextMeshProUGUI> taskLabels = new List<TextMeshProUGUI>();
    // Completion flags for the active page's tasks.
    private bool[] taskDone = new bool[0];

    // The scripted pages and the one currently shown (-1 = not started).
    private readonly List<Step> steps = new List<Step>();
    private int stepIndex = -1;

    // Progress flags set by event handlers; reset per page.
    private bool executionStarted;
    private bool executionFinished;
    private bool puzzleWon;
    private bool sawCellSelection;
    private bool boardChangedSinceStep;
    private bool newSolutionRequested;
    // Latches once the results popup has been seen, so the "click Continue"
    // task can wait for it to open and then close.
    private bool seenWinPopup;
    // Set the first time the player wins while the tutorial is live; winning
    // the level always counts as completing the tutorial, even if skipped after.
    private bool wonOnce;

    // Lock everything the instant the runner exists, before scene refs resolve.
    private void Awake()
    {
        TutorialGate.Active = true;
        LockAll();
    }

    // Resolve refs (abort if missing), hook gameplay events, build steps + panel.
    private void Start()
    {
        canvas = UIRoot.FindSceneCanvas();
        inventoryUI = FindAnyObjectByType<TileInventoryUI>();
        grid = FindAnyObjectByType<GridController>();
        labelController = FindAnyObjectByType<HexTileLabelController>();
        authoring = FindAnyObjectByType<InstructionAuthoringController>();

        if (canvas == null || inventoryUI == null || grid == null || labelController == null || authoring == null)
        {
            EndTutorial(false);
            return;
        }

        ExecutionEngine.ExecutionStarted += OnExecutionStarted;
        ExecutionEngine.ExecutionFinished += OnExecutionFinished;
        ExecutionEngine.PuzzleWon += OnPuzzleWon;
        SolutionPickerPopup.NewRequested += OnNewSolutionRequested;
        grid.OnCellSelected += OnCellSelected;
        labelController.boardState.OnCellChanged += OnBoardChanged;

        BuildSteps();
        BuildPanel();
        BuildDim();
        StartCoroutine(BeginNextFrame());
    }

    private IEnumerator BeginNextFrame()
    {
        yield return null;
        yield return null;

        // Start from a clean slate: wipe any autosaved tiles and instructions
        // so every task validates a fresh action rather than pre-existing
        // state (which would instantly cascade through all pages).
        List<HexCoord> coords = new List<HexCoord>(labelController.boardState.AllTiles.Keys);
        foreach (HexCoord coord in coords)
        {
            labelController.boardState.RemoveTile(coord);
        }
        grid.ClearGrid();

        yield return null;
        NextStep();
    }

    private void OnExecutionStarted() { executionStarted = true; }
    private void OnExecutionFinished() { executionFinished = true; }
    private void OnPuzzleWon() { puzzleWon = true; wonOnce = true; }
    private void OnCellSelected() { sawCellSelection = true; }
    private void OnBoardChanged(HexCoord coord) { boardChangedSinceStep = true; }
    private void OnNewSolutionRequested() { newSolutionRequested = true; }

    // Unhook events; destroying the runner ends the tutorial without completing it.
    private void OnDestroy()
    {
        ExecutionEngine.ExecutionStarted -= OnExecutionStarted;
        ExecutionEngine.ExecutionFinished -= OnExecutionFinished;
        ExecutionEngine.PuzzleWon -= OnPuzzleWon;
        SolutionPickerPopup.NewRequested -= OnNewSolutionRequested;
        if (grid != null)
        {
            grid.OnCellSelected -= OnCellSelected;
        }
        if (labelController != null) labelController.boardState.OnCellChanged -= OnBoardChanged;
        EndTutorial(false);
    }

    // Poll the active page's tasks; pulse the ring; track the results popup;
    // keep the spotlight hole on its target.
    private void Update()
    {
        PollTasks();

        if (!seenWinPopup && FindDeep(canvas.transform, "WinPopup") != null) seenWinPopup = true;

        // Popups spawn as the last sibling, which would bury the tutorial
        // panel under their backdrops - keep the panel on top instead.
        if (panelRoot != null
            && panelRoot.transform.GetSiblingIndex() != panelRoot.transform.parent.childCount - 1)
        {
            panelRoot.transform.SetAsLastSibling();
        }

        // The ring target may not exist yet when the page applies (popups
        // spawn late) or may have been rebuilt - keep retrying cheaply.
        if (highlight == null && dimHoleTarget == null && !dimHoleBoard
            && !string.IsNullOrEmpty(currentAnchorName))
        {
            HighlightAnchor(currentAnchorName);
        }

        if (highlight != null && !GameOptions.ReducedMotion)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.05f;
            highlight.transform.localScale = Vector3.one * pulse;
        }

        if (dimRoot != null && dimRoot.activeSelf) RefreshDim();
    }

    // Flip any freshly satisfied task to green and refresh locks/Next/highlight.
    // Tasks complete strictly in order: only the first open task is polled, so
    // a later task whose predicate happens to hold at page start (e.g.
    // "delete the processor you added" while it is still deleted) never
    // short-circuits the sequence.
    private void PollTasks()
    {
        if (stepIndex < 0 || stepIndex >= steps.Count) return;
        Step step = steps[stepIndex];
        if (step.InfoOnly) return;

        for (int i = 0; i < step.Tasks.Count; i++)
        {
            if (taskDone[i]) continue;

            if (step.Tasks[i].Done != null && step.Tasks[i].Done())
            {
                taskDone[i] = true;
                RefreshTaskState();

                // Win pages flip straight to the next page so the instruction
                // ("Good job - click CONTINUE") is on screen before the
                // results popup appears and steals focus.
                if (step.AutoAdvance)
                {
                    bool allDone = true;
                    for (int t = 0; t < taskDone.Length; t++)
                    {
                        if (!taskDone[t]) { allDone = false; break; }
                    }
                    if (allDone) NextStep();
                }
            }
            return;
        }
    }

    // Advance to the next page; running past the last one completes the tutorial.
    private void NextStep()
    {
        stepIndex++;
        DestroyHighlight();

        if (stepIndex >= steps.Count)
        {
            EndTutorial(true);
            return;
        }

        ApplyStep(steps[stepIndex]);
    }

    // Reset per-page state, rebuild the task rows, apply locks and sizing.
    private void ApplyStep(Step step)
    {
        sawCellSelection = false;
        boardChangedSinceStep = false;
        executionStarted = false;
        executionFinished = false;
        puzzleWon = false;
        newSolutionRequested = false;
        seenWinPopup = false;
        taskDone = new bool[step.Tasks.Count];

        bodyText.text = step.Text;

        bool showTasks = !step.InfoOnly;
        divider.SetActive(showTasks);
        tasksHeader.SetActive(showTasks);
        dimRoot.SetActive(step.InfoOnly);
        panelRT.sizeDelta = new Vector2(PanelWidth,
            step.InfoOnly ? InfoHeight : RowsY + step.Tasks.Count * RowStep + FooterHeight);

        RebuildTaskRows(step);
        RefreshTaskState();
        ThemeSwitcher.ApplyToSubtree(panelRoot.transform);
    }

    // Destroy last page's rows and stamp one label per task.
    private void RebuildTaskRows(Step step)
    {
        for (int i = taskLabels.Count - 1; i >= 0; i--)
        {
            if (taskLabels[i] != null) Destroy(taskLabels[i].gameObject);
        }
        taskLabels.Clear();

        if (step.InfoOnly) return;

        for (int i = 0; i < step.Tasks.Count; i++)
        {
            GameObject row = new GameObject("TaskRow_" + (i + 1), typeof(RectTransform), typeof(TextMeshProUGUI));
            row.transform.SetParent(taskRows, false);
            TextMeshProUGUI tmp = row.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = Mathf.Max(8f, Mathf.Round(16f * GameOptions.UiScale));
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            RectTransform rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 1f);
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0f, 1f);
            rowRT.offsetMin = new Vector2(20f, -(i + 1) * RowStep);
            rowRT.offsetMax = new Vector2(-20f, -i * RowStep - 4f);
            taskLabels.Add(tmp);
        }
    }

    // Rewrite every row's marker/colour, re-lock permissions for the first
    // open task, gate the Next button and re-aim the highlight ring.
    private void RefreshTaskState()
    {
        Step step = steps[stepIndex];

        int firstIncomplete = -1;
        for (int i = 0; i < taskDone.Length; i++)
        {
            if (!taskDone[i]) { firstIncomplete = i; break; }
        }

        for (int i = 0; i < taskLabels.Count; i++)
        {
            bool done = taskDone[i];
            TextMeshProUGUI tmp = taskLabels[i];
            tmp.text = (done ? "\u2713" : "\u25CB") + "  " + step.Tasks[i].Label;
            tmp.color = done ? TaskGreen : HexlinkTheme.TextLight;
            tmp.fontStyle = done ? FontStyles.Bold : FontStyles.Normal;
        }

        ApplyLocks(step, firstIncomplete);
        SetNextEnabled(step.InfoOnly || firstIncomplete < 0);
        UpdateHighlight(step, firstIncomplete);
    }

    // Pages never see -1 from their own lambda; a finished page locks down.
    private void ApplyLocks(Step step, int firstIncomplete)
    {
        if (firstIncomplete >= 0 && step.Locks != null)
        {
            step.Locks(firstIncomplete);
        }
        else
        {
            LockAll();
        }
        inventoryUI.RefreshPaletteAvailability();
    }

    // ---------------------------------------------------------------- locks

    private void LockAll()
    {
        SetLocks(null, false, false, false, false, false, false, false, false);
    }

    // Write one task's permissions into the shared TutorialGate.
    private void SetLocks(string[] tiles, bool placement, bool authoringAllowed, bool removal, bool play,
        bool space, bool operation, bool move, bool commit, bool processorKey = false)
    {
        TutorialGate.AllowedTiles.Clear();
        if (tiles != null)
        {
            foreach (string tile in tiles) TutorialGate.AllowedTiles.Add(tile);
        }
        TutorialGate.AllowPlacement = placement;
        TutorialGate.AllowAuthoring = authoringAllowed;
        TutorialGate.AllowRemoval = removal;
        TutorialGate.AllowPlay = play;
        TutorialGate.AllowSpaceKey = space;
        TutorialGate.AllowOperationKey = operation;
        TutorialGate.AllowMoveKey = move;
        TutorialGate.AllowCommitKey = commit;
        TutorialGate.AllowProcessorKey = processorKey;
    }

    // ----------------------------------------------------------- validation

    // Count board tiles matching a predicate.
    private int CountTiles(System.Func<TileData, bool> match)
    {
        int count = 0;
        foreach (KeyValuePair<HexCoord, TileData> pair in labelController.boardState.AllTiles)
        {
            if (match(pair.Value)) count++;
        }
        return count;
    }

    private bool IsNumberOne(TileData tile)
    {
        return tile is NumberTileData && tile.GetDisplayValue() == "1";
    }

    private bool IsPlus(TileData tile)
    {
        return tile is OperationTileData && tile.GetDisplayValue() == "+";
    }

    // True when a + tile is adjacent to both placed 1s.
    private bool PlusAdjacentToBothOnes()
    {
        HexCoord? plusCoord = null;
        List<HexCoord> ones = new List<HexCoord>();

        foreach (KeyValuePair<HexCoord, TileData> pair in labelController.boardState.AllTiles)
        {
            if (IsPlus(pair.Value)) plusCoord = pair.Key;
            if (IsNumberOne(pair.Value)) ones.Add(pair.Key);
        }

        if (plusCoord == null || ones.Count < 2) return false;

        HexCoord plus = plusCoord.Value;
        foreach (HexCoord one in ones)
        {
            if (!IsAdjacent(plus, one)) return false;
        }
        return true;
    }

    // Axial hex-neighbour test (the six valid (dq,dr) offsets).
    private static bool IsAdjacent(HexCoord a, HexCoord b)
    {
        int dq = a.q - b.q;
        int dr = a.r - b.r;
        return (dq == 1 && dr == 0) || (dq == -1 && dr == 0)
            || (dq == 0 && dr == 1) || (dq == 0 && dr == -1)
            || (dq == 1 && dr == -1) || (dq == -1 && dr == 1);
    }

    // Locate the finish tile, if one has been placed.
    private bool HasFinalTile(out HexCoord finalCoord)
    {
        foreach (KeyValuePair<HexCoord, TileData> pair in labelController.boardState.AllTiles)
        {
            if (pair.Value is FinalTileData)
            {
                finalCoord = pair.Key;
                return true;
            }
        }
        finalCoord = default;
        return false;
    }

    // True when any authored Move instruction targets the finish tile.
    private bool HasMoveToFinal()
    {
        if (!HasFinalTile(out HexCoord finalCoord)) return false;

        for (int p = 0; p < grid.ProcessorCount; p++)
        {
            for (int c = 0; c < grid.ColumnCount; c++)
            {
                if (grid.GetInstructionAt(p, c) is MoveInstructionData move
                    && move.Destination.Equals(finalCoord))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Position-independent: any cell holding the instruction type counts.
    private bool AnyCellHas<T>() where T : InstructionData
    {
        for (int p = 0; p < grid.ProcessorCount; p++)
        {
            for (int c = 0; c < grid.ColumnCount; c++)
            {
                if (grid.GetInstructionAt(p, c) is T) return true;
            }
        }
        return false;
    }

    // Count instructions of a type across every processor/column.
    private int CountInstructions<T>() where T : InstructionData
    {
        int count = 0;
        for (int p = 0; p < grid.ProcessorCount; p++)
        {
            for (int c = 0; c < grid.ColumnCount; c++)
            {
                if (grid.GetInstructionAt(p, c) is T) count++;
            }
        }
        return count;
    }

    // Count instructions of a type inside one time-step column.
    private int CountInColumn<T>(int columnIndex) where T : InstructionData
    {
        int count = 0;
        for (int p = 0; p < grid.ProcessorCount; p++)
        {
            if (grid.GetInstructionAt(p, columnIndex) is T) count++;
        }
        return count;
    }

    // The Solutions window is a procedurally built canvas child - find by name.
    private bool SolutionsPopupOpen()
    {
        return FindDeep(canvas.transform, "SolutionPickerPopup") != null;
    }

    // All four tiles the optimisation rebuild needs are on the board.
    private bool OptimizationTilesPlaced()
    {
        return CountTiles(IsNumberOne) >= 2 && PlusAdjacentToBothOnes() && HasFinalTile(out _);
    }

    // ----------------------------------------------------------------- pages

    // The scripted sequence: each Step is one panel page with its Locks (what
    // input the first open task permits) and per-task Done tests.
    private void BuildSteps()
    {
        // ---- intro: name each part, one page each
        steps.Add(new Step
        {
            Text = "This is the HEX GRID. You place tiles here - your program moves numbers across it.",
            Anchor = "BOARD",
            InfoOnly = true
        });
        steps.Add(new Step
        {
            Text = "This is the TARGET: build this number and deliver it to a finish tile. Here it's 2.",
            Anchor = "TargetLabel",
            InfoOnly = true
        });
        steps.Add(new Step
        {
            Text = "This is the CODING INTERFACE. One instruction per cell. It runs left to right, one column per step.",
            Anchor = "GridPanel",
            InfoOnly = true
        });
        steps.Add(new Step
        {
            Text = "This is the TILE PALETTE. Click a tile here, then a hex to place it.",
            Anchor = "TilePalette",
            InfoOnly = true
        });
        steps.Add(new Step
        {
            Text = "This is PLAY - it runs your program.",
            Anchor = "PlayButton",
            InfoOnly = true
        });

        // ---- place the tiles
        steps.Add(new Step
        {
            Text = "Build 1 + 1. Middle-click a placed tile to delete it.",
            Anchor = "TilePalette",
            Tasks =
            {
                new TaskItem { Label = "Select 1 from the palette", Anchor = "Tile_1",
                    Done = () => inventoryUI.CurrentSelectedTile == "1" },
                new TaskItem { Label = "Place the 1 on any hex", Anchor = "BOARD",
                    Done = () => boardChangedSinceStep && CountTiles(IsNumberOne) >= 1 },
                new TaskItem { Label = "Place a second 1 on another hex", Anchor = "Tile_1",
                    Done = () => boardChangedSinceStep && CountTiles(IsNumberOne) >= 2 },
                new TaskItem { Label = "Place a + touching both 1s", Anchor = "Tile_+",
                    Done = () => boardChangedSinceStep && PlusAdjacentToBothOnes() }
            },
            Locks = first => SetLocks(
                first < 3 ? new[] { "1" } : new[] { "+" },
                true, false, first >= 1, false, false, false, false, false)
        });

        // ---- authoring
        steps.Add(new Step
        {
            Text = "Click a cell, then a tile - that's the subject. SPACE spawns it as a node. Middle-click a cell to clear its instruction.",
            Anchor = "GridPanel",
            Tasks =
            {
                new TaskItem { Label = "Click the first cell (P1, column 1)", Anchor = "GridPanel",
                    Done = () => sawCellSelection },
                new TaskItem { Label = "Spawn a node: click a 1, press SPACE", Anchor = "GridPanel",
                    Done = () => CountInstructions<SelectInstructionData>() >= 1 },
                new TaskItem { Label = "Spawn the second 1's node too", Anchor = "GridPanel",
                    Done = () => CountInstructions<SelectInstructionData>() >= 2 },
                new TaskItem { Label = "Press C on a 1, then click the +", Anchor = "BOARD",
                    Done = () => authoring != null && authoring.IsAwaitingOperands },
                new TaskItem { Label = "Click the other 1, press D to confirm", Anchor = "GridPanel",
                    Done = () => AnyCellHas<OperationInstructionData>() }
            },
            Locks = first =>
            {
                if (first <= 0) SetLocks(null, false, true, false, false, false, false, false, false);
                else if (first <= 2) SetLocks(null, false, true, false, false, true, false, false, false);
                else SetLocks(null, false, true, false, false, false, true, false, true);
            }
        });

        // ---- first run
        steps.Add(new Step
        {
            Text = "Press PLAY and watch 1+1 become 2.",
            Anchor = "PlayButton",
            Tasks =
            {
                new TaskItem { Label = "Press Play", Anchor = "PlayButton",
                    Done = () => executionStarted && executionFinished && !puzzleWon }
            },
            Locks = first => SetLocks(null, false, false, false, true, false, false, false, false)
        });

        // ---- grid editing tools (told, not tasked)
        steps.Add(new Step
        {
            Text = "More tools: ENTER adds a processor. Right-click a row or column label to clear or delete it. Middle-click empties a cell.",
            Anchor = "GridPanel",
            InfoOnly = true
        });

        // ---- metrics
        steps.Add(new Step
        {
            Text = "Your metrics: INSTR, CYCLES, PROCS. Bests are saved for every puzzle.",
            Anchor = "MetricsPanel",
            InfoOnly = true
        });

        // ---- finish tile + delivery + first win
        steps.Add(new Step
        {
            Text = "The 2 needs a destination: place the _ finish tile, then Z-move onto it (D confirms).",
            Anchor = "TilePalette",
            AutoAdvance = true,
            Tasks =
            {
                new TaskItem { Label = "Place the _ finish tile on an empty hex", Anchor = "Tile__",
                    Done = () => boardChangedSinceStep && HasFinalTile(out _) },
                new TaskItem { Label = "Move: click the +, press Z, click _, then D", Anchor = "GridPanel",
                    Done = () => HasMoveToFinal() },
                new TaskItem { Label = "Press Play - WIN", Anchor = "PlayButton",
                    Done = () => puzzleWon }
            },
            Locks = first =>
            {
                if (first == 0) SetLocks(new[] { "_" }, true, false, false, false, false, false, false, false);
                else if (first == 1) SetLocks(null, false, true, false, false, false, false, true, true);
                else SetLocks(null, false, false, false, true, false, false, false, false);
            }
        });

        // ---- good job: close the results popup
        steps.Add(new Step
        {
            Text = "Good job! Click CONTINUE on the results screen.",
            Anchor = "GridPanel",
            Tasks =
            {
                new TaskItem { Label = "Click CONTINUE", Anchor = "ContinueButton",
                    Done = () => seenWinPopup && FindDeep(canvas.transform, "WinPopup") == null }
            }
        });

        // ---- the solutions window
        steps.Add(new Step
        {
            Text = "SOLUTIONS saves every attempt with its metrics. Your bests live here.",
            Anchor = "SolutionsButton",
            Tasks =
            {
                new TaskItem { Label = "Open the Solutions window", Anchor = "SolutionsButton",
                    Done = () => SolutionsPopupOpen() },
                new TaskItem { Label = "Click NEW for a fresh solution", Anchor = null,
                    Done = () => newSolutionRequested }
            }
        });

        // ---- optimisation: same-cycle spawns, inside the new solution
        steps.Add(new Step
        {
            Text = "Rows are processors - they run in parallel. Columns are cycles - one per step. Put both 1-spawns in the SAME column on two processors: 4 cycles becomes 3.",
            Anchor = "TilePalette",
            AutoAdvance = true,
            Tasks =
            {
                new TaskItem { Label = "Re-place the tiles: 1, 1, + and _", Anchor = "TilePalette",
                    Done = () => OptimizationTilesPlaced() },
                new TaskItem { Label = "Press ENTER for a second processor", Anchor = "GridPanel",
                    Done = () => grid.ProcessorCount >= 2 },
                new TaskItem { Label = "Spawn both 1s in column 1 (one per row)", Anchor = "GridPanel",
                    Done = () => CountInColumn<SelectInstructionData>(0) >= 2 },
                new TaskItem { Label = "Column 2: operate (C) 1 + 1 = 2", Anchor = "GridPanel",
                    Done = () => CountInColumn<OperationInstructionData>(1) >= 1 },
                new TaskItem { Label = "Column 3: move (Z) the 2 onto _", Anchor = "GridPanel",
                    Done = () => CountInColumn<MoveInstructionData>(2) >= 1 && HasMoveToFinal() },
                new TaskItem { Label = "Press Play - WIN in 3 cycles", Anchor = "PlayButton",
                    Done = () => puzzleWon }
            },
            Locks = first =>
            {
                if (first == 0) SetLocks(new[] { "1", "+", "_" }, true, false, true, false, false, false, false, false);
                else if (first == 1) SetLocks(null, false, false, false, false, false, false, false, false, true);
                else if (first == 2) SetLocks(null, false, true, false, false, true, false, false, false);
                else if (first == 3) SetLocks(null, false, true, false, false, false, true, false, true);
                else if (first == 4) SetLocks(null, false, true, false, false, false, false, true, true);
                else SetLocks(null, false, false, false, true, false, false, false, false);
            }
        });

        // ---- core loop finale
        steps.Add(new Step
        {
            Text = "That's the loop: solve it, then optimise it. Chase your bests in SOLUTIONS. Good luck!",
            Anchor = "MetricsPanel",
            InfoOnly = true
        });
    }
    // ------------------------------------------------------------------- UI

    // Construct the docked panel: outline, TUTORIAL header, body, divider,
    // TASKS header, rows, Next + Skip.
    private void BuildPanel()
    {
        panelRoot = new GameObject("TutorialPanel", typeof(RectTransform));
        panelRoot.transform.SetParent(canvas.transform, false);
        panelRoot.transform.SetAsLastSibling();
        panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0.5f);
        panelRT.anchorMax = new Vector2(1f, 0.5f);
        panelRT.pivot = new Vector2(1f, 0.5f);
        panelRT.anchoredPosition = new Vector2(0f, 0f);
        panelRT.sizeDelta = new Vector2(PanelWidth, InfoHeight);

        GameObject border = new GameObject("Border", typeof(RectTransform));
        border.transform.SetParent(panelRoot.transform, false);
        ChamferedImage borderChamfer = border.AddComponent<ChamferedImage>();
        borderChamfer.Chamfer = 18f;
        borderChamfer.OutlineThickness = 4f;
        borderChamfer.color = OutlineBlue;
        Stretch(border.GetComponent<RectTransform>());

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(border.transform, false);
        ChamferedImage panelChamfer = panel.AddComponent<ChamferedImage>();
        panelChamfer.Chamfer = 18f;
        panelChamfer.color = HexlinkTheme.Panel;
        Stretch(panel.GetComponent<RectTransform>());
        RectTransform fillRT = panel.GetComponent<RectTransform>();
        fillRT.offsetMin = new Vector2(3f, 3f);
        fillRT.offsetMax = new Vector2(-3f, -3f);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "TUTORIAL";
        titleTMP.fontSize = Mathf.Max(8f, Mathf.Round(26f * GameOptions.UiScale));
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = OutlineBlue;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -HeaderTop);
        titleRT.sizeDelta = new Vector2(-32f, HeaderHeight);

        GameObject bodyGO = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGO.transform.SetParent(panel.transform, false);
        bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
        bodyText.fontSize = Mathf.Max(8f, Mathf.Round(18f * GameOptions.UiScale));
        bodyText.color = HexlinkTheme.TextLight;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 1f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.anchoredPosition = new Vector2(0f, -BodyTop);
        bodyRT.sizeDelta = new Vector2(-32f, BodyHeight);

        divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panel.transform, false);
        Image dividerImage = divider.GetComponent<Image>();
        dividerImage.color = HexlinkTheme.Divider;
        dividerImage.raycastTarget = false;
        RectTransform dividerRT = divider.GetComponent<RectTransform>();
        dividerRT.anchorMin = new Vector2(0f, 1f);
        dividerRT.anchorMax = new Vector2(1f, 1f);
        dividerRT.pivot = new Vector2(0.5f, 1f);
        dividerRT.anchoredPosition = new Vector2(0f, -DividerY);
        dividerRT.sizeDelta = new Vector2(-28f, 2f);

        GameObject headerGO = new GameObject("TasksHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGO.transform.SetParent(panel.transform, false);
        tasksHeader = headerGO;
        TextMeshProUGUI headerTMP = headerGO.GetComponent<TextMeshProUGUI>();
        headerTMP.text = "TASKS";
        headerTMP.fontSize = Mathf.Max(8f, Mathf.Round(14f * GameOptions.UiScale));
        headerTMP.fontStyle = FontStyles.Bold;
        headerTMP.color = HexlinkTheme.TextGray;
        headerTMP.alignment = TextAlignmentOptions.Left;
        headerTMP.raycastTarget = false;
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, -TasksHeaderY);
        headerRT.sizeDelta = new Vector2(-32f, 20f);

        // Full-width container so rows anchor to the panel's left edge.
        GameObject rowsGO = new GameObject("TaskRows", typeof(RectTransform));
        rowsGO.transform.SetParent(panel.transform, false);
        taskRows = rowsGO.GetComponent<RectTransform>();
        taskRows.anchorMin = new Vector2(0f, 1f);
        taskRows.anchorMax = new Vector2(1f, 1f);
        taskRows.pivot = new Vector2(0.5f, 1f);
        taskRows.anchoredPosition = new Vector2(0f, -RowsY);
        taskRows.sizeDelta = new Vector2(0f, 0f);

        nextButton = CreateButton(panel.transform, "NextButton", "Next", new Vector2(-105f, 16f), 190f, 40f, true);
        nextButton.onClick.AddListener(NextStep);
        nextGroup = nextButton.gameObject.AddComponent<CanvasGroup>();

        Button skipButton = CreateButton(panel.transform, "SkipButton", "Skip", new Vector2(105f, 16f), 190f, 40f, false);
        skipButton.onClick.AddListener(() => EndTutorial(false));

        ThemeSwitcher.ApplyToSubtree(panelRoot.transform);
    }

    // Spotlight scrim for info pages: four black bands framing a hole around
    // the highlighted element. Sits directly below the panel; anything spawned
    // later (popups) still renders above it. Non-blocking (no raycasts).
    private void BuildDim()
    {
        dimRoot = new GameObject("TutorialDim", typeof(RectTransform));
        dimRoot.transform.SetParent(canvas.transform, false);
        Stretch(dimRoot.GetComponent<RectTransform>());

        for (int i = 0; i < dimBands.Length; i++)
        {
            GameObject band = new GameObject("DimBand_" + i, typeof(RectTransform), typeof(Image));
            band.transform.SetParent(dimRoot.transform, false);
            dimBands[i] = band.GetComponent<Image>();
            dimBands[i].color = DimColor;
            dimBands[i].raycastTarget = false;
            RectTransform bandRT = band.GetComponent<RectTransform>();
            bandRT.anchorMin = new Vector2(0.5f, 0.5f);
            bandRT.anchorMax = new Vector2(0.5f, 0.5f);
            bandRT.pivot = new Vector2(0.5f, 0.5f);
        }

        dimRoot.SetActive(false);
        panelRoot.transform.SetAsLastSibling();
    }

    // Re-lay the four bands so everything outside the hole is darkened. Runs
    // every frame while the dim is visible so it tracks moving/collapsing UI.
    private void RefreshDim()
    {
        RectTransform canvasRT = (RectTransform)canvas.transform;
        Vector2 half = canvasRT.rect.size * 0.5f;

        bool hasHole = false;
        Rect hole = default;

        if (dimHoleBoard)
        {
            HexGridSpawner spawner = FindAnyObjectByType<HexGridSpawner>();
            if (spawner != null)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(Camera.main, spawner.transform.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, point, null, out Vector2 local))
                {
                    hole = new Rect(local.x - 260f, local.y - 260f, 520f, 520f);
                    hasHole = true;
                }
            }
        }
        else if (dimHoleTarget != null)
        {
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3[] corners = new Vector3[4];
            ((RectTransform)dimHoleTarget).GetWorldCorners(corners);

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, uiCamera, out Vector2 local)) continue;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            hole = Rect.MinMaxRect(min.x - HolePadding, min.y - HolePadding, max.x + HolePadding, max.y + HolePadding);
            hasHole = true;
        }

        if (!hasHole)
        {
            // No target: dim everything except the panel (which sits above).
            PlaceBand(0, Vector2.zero, canvasRT.rect.size);
            PlaceBand(1, Vector2.zero, Vector2.zero);
            PlaceBand(2, Vector2.zero, Vector2.zero);
            PlaceBand(3, Vector2.zero, Vector2.zero);
            return;
        }

        // Top: full width above the hole.
        PlaceBand(0, new Vector2(0f, (hole.yMax + half.y) * 0.5f),
            new Vector2(half.x * 2f, Mathf.Max(0f, half.y - hole.yMax)));
        // Bottom: full width below the hole.
        PlaceBand(1, new Vector2(0f, (-half.y + hole.yMin) * 0.5f),
            new Vector2(half.x * 2f, Mathf.Max(0f, hole.yMin + half.y)));
        // Left / right: hole-height strips flanking the hole.
        PlaceBand(2, new Vector2((-half.x + hole.xMin) * 0.5f, hole.center.y),
            new Vector2(Mathf.Max(0f, hole.xMin + half.x), hole.height));
        PlaceBand(3, new Vector2((hole.xMax + half.x) * 0.5f, hole.center.y),
            new Vector2(Mathf.Max(0f, half.x - hole.xMax), hole.height));
    }

    // Size and centre one dim band (canvas-local coordinates).
    private void PlaceBand(int index, Vector2 center, Vector2 size)
    {
        if (dimBands[index] == null) return;
        RectTransform rt = dimBands[index].rectTransform;
        rt.anchoredPosition = center;
        rt.sizeDelta = size;
    }

    // Dim + disable Next while tasks remain, brighten it when the page is done.
    private void SetNextEnabled(bool enabled)
    {
        if (nextButton == null) return;
        nextButton.interactable = enabled;
        if (nextGroup != null) nextGroup.alpha = enabled ? 1f : 0.4f;
    }

    // Accent ring around whatever the current task refers to; BOARD is
    // world-space so it gets no ring (the dim hole exposes it instead), and a
    // missing name clears the ring.
    private void UpdateHighlight(Step step, int firstIncomplete)
    {
        string anchor = step.Anchor;

        if (firstIncomplete >= 0)
        {
            anchor = null;
            for (int i = firstIncomplete; i < step.Tasks.Count; i++)
            {
                if (!taskDone[i] && !string.IsNullOrEmpty(step.Tasks[i].Anchor))
                {
                    anchor = step.Tasks[i].Anchor;
                    break;
                }
            }
            if (anchor == null) anchor = step.Anchor;
        }

        HighlightAnchor(anchor);
    }

    // (Re)build the ring; BOARD / unknown names just clear it. Also records
    // the spotlight hole target for the info-page dim.
    private void HighlightAnchor(string anchorName)
    {
        DestroyHighlight();
        dimHoleTarget = null;
        dimHoleBoard = false;
        currentAnchorName = anchorName;

        if (string.IsNullOrEmpty(anchorName)) return;
        if (anchorName == "BOARD")
        {
            dimHoleBoard = true;
            return;
        }

        Transform anchor = FindDeep(canvas.transform, anchorName);
        if (anchor == null) return;
        dimHoleTarget = anchor;

        highlight = new GameObject("TutorialHighlight", typeof(RectTransform));
        highlight.transform.SetParent(anchor, false);
        ChamferedImage ring = highlight.AddComponent<ChamferedImage>();
        ring.Chamfer = 18f;
        ring.OutlineThickness = 8f;
        ring.color = HexlinkTheme.Accent;
        ring.raycastTarget = false;
        RectTransform rt = highlight.GetComponent<RectTransform>();
        Stretch(rt);
        rt.offsetMin = new Vector2(-10f, -10f);
        rt.offsetMax = new Vector2(10f, 10f);
    }

    private void DestroyHighlight()
    {
        if (highlight != null) Destroy(highlight);
        highlight = null;
    }

    // Optionally mark done, release the gate, tear down panel and runner.
    private void EndTutorial(bool markCompleted)
    {
        if (markCompleted || wonOnce) TutorialLauncher.MarkTutorialCompleted();
        stepIndex = -1;
        TutorialGate.UnlockAll();
        if (inventoryUI != null) inventoryUI.RefreshPaletteAvailability();
        if (panelRoot != null) Destroy(panelRoot);
        if (dimRoot != null) Destroy(dimRoot);
        DestroyHighlight();
        Destroy(gameObject);
    }

    // Procedural button helper; accent = filled style, else ghost style.
    // Label is centred - TMP defaults to top-left otherwise.
    private Button CreateButton(Transform parent, string name, string label, Vector2 position, float width, float height, bool accent)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);
        Image image = buttonGO.GetComponent<Image>();
        image.color = accent ? HexlinkTheme.Accent : HexlinkTheme.Ghost;
        Button button = buttonGO.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Button.Transition.ColorTint;
        HexlinkTheme.ApplyHoverTint(button);
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, height);

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(18f * GameOptions.UiScale));
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = accent ? HexlinkTheme.AccentText : HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        Stretch(textGO.GetComponent<RectTransform>());
        return button;
    }

    // Depth-first search for a transform by name.
    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // Stretch a rect to fill its parent.
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
