// Level 1 interactive tutorial. TutorialLauncher auto-spawns TutorialRunner in
// Puzzle_Play for puzzle "L-1" (until completed once); TutorialRunner walks the
// player through tiles, the instruction keys (SPACE/Z/C/D) and Play using step
// popups, input gating via TutorialGate, and board-state validation.
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
        Debug.Log($"TutorialLauncher: scene=Puzzle_Play puzzle={(puzzle != null ? puzzle.puzzleID : "null")} tutorialDone={complete} runnerExists={exists}");

        if (puzzle == null || puzzle.puzzleID != "L-1") return;
        if (complete) return;
        if (exists) return;

        new GameObject("LevelTutorial").AddComponent<TutorialRunner>();
        Debug.Log("TutorialLauncher: tutorial spawned.");
    }
}

public class TutorialRunner : MonoBehaviour
{
    // Fixed blue outline so tutorial popups are instantly recognisable.
    private static readonly Color OutlineBlue = new Color(0.204f, 0.765f, 1f, 0.95f);

    // One tutorial popup. ContinueButton steps advance on click; otherwise
    // Done() is polled in Update until it returns true.
    private class Step
    {
        // Popup body text.
        public string Text;
        // Optional italic hint line.
        public string Hint;
        // Anchor name: "BOARD", "GridPanel", "TilePalette" or "PlayButton".
        public string Anchor;
        // True = show a Continue button; false = wait for Done.
        public bool ContinueButton;
        // Applies this step's TutorialGate locks.
        public System.Action Locks;
        // Completion predicate polled in Update.
        public System.Func<bool> Done;
    }

    // Scene systems the tutorial observes, locks, and validates against.
    private Canvas canvas;
    private TileInventoryUI inventoryUI;
    private GridController grid;
    private HexTileLabelController labelController;
    private InstructionAuthoringController authoring;

    // Popup UI pieces, built in BuildPopup.
    private GameObject popupRoot;
    private GameObject highlight;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI waitingText;
    private Button continueButton;
    private ArrowImage arrow;

    // The scripted steps and the one currently shown (-1 = not started).
    private readonly List<Step> steps = new List<Step>();
    private int stepIndex = -1;

    // Progress flags set by event handlers; reset per step (per run for Play steps).
    private bool executionStarted;
    private bool executionFinished;
    private bool puzzleWon;
    private bool sawCellSelection;
    private bool boardChangedSinceStep;
    private bool gridChangedSinceStep;

    // Lock everything the instant the runner exists, before scene refs resolve.
    private void Awake()
    {
        TutorialGate.Active = true;
        LockAll();
    }

    // Resolve refs (abort if missing), hook gameplay events, build steps + popup.
    private void Start()
    {
        canvas = FindAnyObjectByType<Canvas>();
        inventoryUI = FindAnyObjectByType<TileInventoryUI>();
        grid = FindAnyObjectByType<GridController>();
        labelController = FindAnyObjectByType<HexTileLabelController>();
        authoring = FindAnyObjectByType<InstructionAuthoringController>();

        if (canvas == null || inventoryUI == null || grid == null || labelController == null || authoring == null)
        {
            Debug.LogError($"TutorialRunner: missing refs - canvas={canvas != null} inventory={inventoryUI != null} grid={grid != null} labels={labelController != null} authoring={authoring != null}");
            EndTutorial(false);
            return;
        }

        Debug.Log("TutorialRunner: refs OK, building steps.");

        ExecutionEngine.ExecutionStarted += OnExecutionStarted;
        ExecutionEngine.ExecutionFinished += OnExecutionFinished;
        ExecutionEngine.PuzzleWon += OnPuzzleWon;
        grid.OnCellSelected += OnCellSelected;
        grid.OnGridChanged += OnGridChanged;
        labelController.boardState.OnCellChanged += OnBoardChanged;

        BuildSteps();
        BuildPopup();
        inventoryUI.RefreshPaletteAvailability();
        StartCoroutine(BeginNextFrame());
    }

    private IEnumerator BeginNextFrame()
    {
        yield return null;
        yield return null;

        // Start from a clean slate: wipe any autosaved tiles and instructions
        // so every step validates a fresh action rather than pre-existing
        // state (which would instantly cascade through all steps).
        List<HexCoord> coords = new List<HexCoord>(labelController.boardState.AllTiles.Keys);
        foreach (HexCoord coord in coords)
        {
            labelController.boardState.RemoveTile(coord);
        }
        grid.ClearGrid();

        yield return null;
        boardChangedSinceStep = false;
        gridChangedSinceStep = false;
        sawCellSelection = false;

        NextStep();
    }

    private void OnExecutionStarted() { executionStarted = true; }
    private void OnExecutionFinished() { executionFinished = true; }
    private void OnPuzzleWon() { puzzleWon = true; }
    private void OnCellSelected() { sawCellSelection = true; }
    private void OnGridChanged() { gridChangedSinceStep = true; }
    private void OnBoardChanged(HexCoord coord) { boardChangedSinceStep = true; }

    // Unhook events; destroying the runner ends the tutorial without completing it.
    private void OnDestroy()
    {
        ExecutionEngine.ExecutionStarted -= OnExecutionStarted;
        ExecutionEngine.ExecutionFinished -= OnExecutionFinished;
        ExecutionEngine.PuzzleWon -= OnPuzzleWon;
        if (grid != null)
        {
            grid.OnCellSelected -= OnCellSelected;
            grid.OnGridChanged -= OnGridChanged;
        }
        if (labelController != null) labelController.boardState.OnCellChanged -= OnBoardChanged;
        EndTutorial(false);
    }

    // Poll the active step's Done predicate; run the idle pulse/breathe effects.
    private void Update()
    {
        if (stepIndex >= 0 && stepIndex < steps.Count && steps[stepIndex].Done != null)
        {
            if (steps[stepIndex].Done()) NextStep();
        }

        if (highlight != null && !GameOptions.ReducedMotion)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.02f;
            highlight.transform.localScale = Vector3.one * pulse;
        }

        if (waitingText != null && waitingText.gameObject.activeSelf && !GameOptions.ReducedMotion)
        {
            float breathe = 0.55f + Mathf.Sin(Time.unscaledTime * 2.0f) * 0.25f;
            Color color = HexlinkTheme.TextGray;
            waitingText.color = new Color(color.r, color.g, color.b, breathe);
        }
    }

    // Advance to the next step; running past the last one completes the tutorial.
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

    // Reset progress flags, apply locks, refresh and position the popup.
    private void ApplyStep(Step step)
    {
        sawCellSelection = false;
        boardChangedSinceStep = false;
        gridChangedSinceStep = false;
        step.Locks?.Invoke();
        inventoryUI.RefreshPaletteAvailability();

        bodyText.text = step.Text;
        hintText.text = step.Hint ?? "";
        hintText.gameObject.SetActive(!string.IsNullOrEmpty(step.Hint));
        continueButton.gameObject.SetActive(step.ContinueButton);
        waitingText.gameObject.SetActive(!step.ContinueButton);

        PositionForAnchor(step.Anchor);
        HighlightAnchor(step.Anchor);
        ThemeSwitcher.ApplyToSubtree(popupRoot.transform);
    }

    // Optionally mark done, release the gate, tear down popup and runner.
    private void EndTutorial(bool markCompleted)
    {
        if (markCompleted) TutorialLauncher.MarkTutorialCompleted();
        stepIndex = -1;
        TutorialGate.UnlockAll();
        if (inventoryUI != null) inventoryUI.RefreshPaletteAvailability();
        if (popupRoot != null) Destroy(popupRoot);
        DestroyHighlight();
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- locks

    private void LockAll()
    {
        SetLocks(null, false, false, false, false, false, false, false, false);
    }

    // Write one step's permissions into the shared TutorialGate.
    private void SetLocks(string tile, bool placement, bool authoring, bool removal, bool play,
        bool space, bool operation, bool move, bool commit)
    {
        TutorialGate.AllowedTile = tile;
        TutorialGate.AllowPlacement = placement;
        TutorialGate.AllowAuthoring = authoring;
        TutorialGate.AllowRemoval = removal;
        TutorialGate.AllowPlay = play;
        TutorialGate.AllowSpaceKey = space;
        TutorialGate.AllowOperationKey = operation;
        TutorialGate.AllowMoveKey = move;
        TutorialGate.AllowCommitKey = commit;
        TutorialGate.AllowProcessorKey = false;
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

    // Position-independent: any cell holding the instruction type counts, so
    // the tutorial never gets stuck because the player used a different cell.
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

    // ----------------------------------------------------------------- steps

    // The scripted sequence: each Step is one popup with its Locks (what input
    // is allowed) and its Done test (what advances to the next popup).
    private void BuildSteps()
    {
        // ---- intro: name each part
        steps.Add(new Step
        {
            Text = "This is the HEX GRID - your board. You place number tiles and operator tiles onto it.",
            Anchor = "BOARD",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "This is the CODING INTERFACE. Each cell holds ONE instruction. Your program runs one column per step, left to right.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "This is the TILE PALETTE. Click a tile here to pick it up, then click a hex to place it.",
            Anchor = "TilePalette",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "This is PLAY. It runs your program and the nodes move on the board.",
            Anchor = "PlayButton",
            ContinueButton = true,
            Locks = LockAll
        });

        // ---- place the tiles
        steps.Add(new Step
        {
            Text = "Goal: turn 1 + 1 into 2 and deliver it to a finish tile. First, the tiles.",
            Anchor = "TilePalette",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Place a 1. Click 1 in the palette, then click any hex.",
            Anchor = "TilePalette",
            Locks = () => SetLocks("1", true, false, false, false, false, false, false, false),
            Done = () => boardChangedSinceStep && CountTiles(IsNumberOne) >= 1
        });
        steps.Add(new Step
        {
            Text = "Place the second 1 on another hex.",
            Anchor = "TilePalette",
            Locks = () => SetLocks("1", true, false, false, false, false, false, false, false),
            Done = () => boardChangedSinceStep && CountTiles(IsNumberOne) >= 2
        });
        steps.Add(new Step
        {
            Text = "Place a + between them. It must touch both 1s.",
            Hint = "Middle-click a placed tile to remove it.",
            Anchor = "TilePalette",
            Locks = () => SetLocks("+", true, false, true, false, false, false, false, false),
            Done = () => boardChangedSinceStep && PlusAdjacentToBothOnes()
        });

        // ---- how coding works
        steps.Add(new Step
        {
            Text = "HOW CODING WORKS: click a cell - that is where your instruction will go. Then click a tile on the board - it becomes the SUBJECT of your instruction. You then have three options.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "SPACE spawns the subject's number as a node. Z moves the node sitting on it. C operates the node sitting on it. You'll use each one in a moment.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Click the first cell. Whatever you build next will be written here.",
            Anchor = "GridPanel",
            Locks = () => SetLocks(null, false, true, false, false, false, false, false, false),
            Done = () => sawCellSelection
        });
        steps.Add(new Step
        {
            Text = "Click one of the 1s on the board, then press SPACE. Every tile that takes part in the sum needs its own node - so you'll spawn both 1s.",
            Hint = "SPACE = spawn the number",
            Anchor = "BOARD",
            Locks = () => SetLocks(null, false, true, false, false, true, false, false, false),
            Done = () => gridChangedSinceStep && CountInstructions<SelectInstructionData>() >= 1
        });
        steps.Add(new Step
        {
            Text = "Now click the OTHER 1, then press SPACE again. 1 + 1 needs a node on both tiles - without the second node the operation will fail.",
            Hint = "SPACE = spawn the number",
            Anchor = "BOARD",
            Locks = () => SetLocks(null, false, true, false, false, true, false, false, false),
            Done = () => gridChangedSinceStep && CountInstructions<SelectInstructionData>() >= 2
        });

        // ---- the operation
        steps.Add(new Step
        {
            Text = "Nothing happens yet - nodes only appear when you press Play. Now the math. C operates the node on a tile: it highlights every adjacent OPERATOR you can use (making 1+), then every adjacent tile WITH A NODE you can operate with (making 1+1).",
            Anchor = "BOARD",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Click the same 1, press C, then click the + it highlights.",
            Hint = "C = operate on the node",
            Anchor = "BOARD",
            Locks = () => SetLocks(null, false, true, false, false, false, true, false, true),
            Done = () => authoring != null && authoring.IsAwaitingOperands
        });
        steps.Add(new Step
        {
            Text = "Now it highlights the operands - click the OTHER 1, then press D to confirm the instruction.",
            Hint = "D = confirm",
            Anchor = "BOARD",
            Locks = () => SetLocks(null, false, true, false, false, false, true, false, true),
            Done = () => gridChangedSinceStep && AnyCellHas<OperationInstructionData>()
        });

        // ---- first run
        steps.Add(new Step
        {
            Text = "Press Play. The first two columns spawn nodes on both 1s. The third merges 1+1 into 2 and moves it onto the + tile.",
            Anchor = "PlayButton",
            // Unlock only Play; reset engine flags so Done watches THIS run.
            Locks = () =>
            {
                SetLocks(null, false, false, false, true, false, false, false, false);
                executionStarted = false;
                executionFinished = false;
            },
            Done = () => executionStarted && executionFinished && !puzzleWon
        });

        // ---- finish tile + delivery
        steps.Add(new Step
        {
            Text = "The 2 is sitting on the + tile. It needs a destination: place the finish tile. Click _ in the palette and place it on any empty hex - it wants the target number, 2.",
            Anchor = "TilePalette",
            Locks = () => SetLocks("_", true, false, false, false, false, false, false, false),
            Done = () => boardChangedSinceStep && HasFinalTile(out _)
        });
        steps.Add(new Step
        {
            Text = "Z moves a node. Click the + tile (where the 2 sits), press Z - every adjacent hex lights up as a move target. Click the finish tile, then press D.",
            Hint = "Z = move, D = confirm",
            Anchor = "BOARD",
            Locks = () => SetLocks(null, false, true, false, false, false, false, true, true),
            Done = () => gridChangedSinceStep && HasMoveToFinal()
        });
        steps.Add(new Step
        {
            Text = "Press Play. The 2 moves onto the finish tile - and the level is solved.",
            Anchor = "PlayButton",
            Locks = () =>
            {
                SetLocks(null, false, false, false, true, false, false, false, false);
                puzzleWon = false;
            },
            // Winning the level completes the tutorial even if the recap is skipped.
            Done = () =>
            {
                if (puzzleWon)
                {
                    TutorialLauncher.MarkTutorialCompleted();
                    return true;
                }
                return false;
            }
        });

        // ---- controls recap, one key per popup
        steps.Add(new Step
        {
            Text = "Controls recap - SPACE: spawn the subject tile's number as a node.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Controls recap - C: operate on the node. Pick an adjacent operator, then adjacent operands.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Controls recap - Z: move the node to any adjacent hex.",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
        steps.Add(new Step
        {
            Text = "Controls recap - D confirms. Enter adds a processor row, Esc cancels, middle-click deletes a tile. Good luck!",
            Anchor = "GridPanel",
            ContinueButton = true,
            Locks = LockAll
        });
    }
    // ------------------------------------------------------------------- UI

    // Construct the popup: border, panel, body/hint, Continue/Skip, waiting label, arrow.
    private void BuildPopup()
    {
        popupRoot = new GameObject("TutorialPopup", typeof(RectTransform));
        popupRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = popupRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(430f, 200f);

        GameObject border = new GameObject("Border", typeof(RectTransform));
        border.transform.SetParent(popupRoot.transform, false);
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
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.offsetMin = new Vector2(3f, 3f);
        panelRT.offsetMax = new Vector2(-3f, -3f);

        GameObject bodyGO = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGO.transform.SetParent(panel.transform, false);
        bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
        bodyText.fontSize = Mathf.Max(8f, Mathf.Round(15f * GameOptions.UiScale));
        bodyText.color = HexlinkTheme.TextLight;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 1f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.pivot = new Vector2(0.5f, 1f);
        bodyRT.anchoredPosition = new Vector2(0f, -16f);
        bodyRT.sizeDelta = new Vector2(-32f, 108f);

        GameObject hintGO = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(panel.transform, false);
        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        hintText.fontSize = Mathf.Max(8f, Mathf.Round(12f * GameOptions.UiScale));
        hintText.fontStyle = FontStyles.Italic;
        hintText.color = HexlinkTheme.TextGray;
        hintText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 40f);
        hintRT.sizeDelta = new Vector2(-32f, 26f);

        continueButton = CreateButton(panel.transform, "ContinueButton", "Continue", new Vector2(-70f, 8f), 120f, 32f, true);
        continueButton.onClick.AddListener(NextStep);

        Button skipButton = CreateButton(panel.transform, "SkipButton", "Skip", new Vector2(72f, 8f), 90f, 28f, false);
        skipButton.onClick.AddListener(() => EndTutorial(false));

        GameObject waitingGO = new GameObject("WaitingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        waitingGO.transform.SetParent(panel.transform, false);
        waitingText = waitingGO.GetComponent<TextMeshProUGUI>();
        waitingText.text = "waiting for you...";
        waitingText.fontSize = Mathf.Max(8f, Mathf.Round(12f * GameOptions.UiScale));
        waitingText.fontStyle = FontStyles.Italic;
        waitingText.color = HexlinkTheme.TextGray;
        waitingText.alignment = TextAlignmentOptions.Left;
        RectTransform waitingRT = waitingGO.GetComponent<RectTransform>();
        waitingRT.anchorMin = new Vector2(0f, 0f);
        waitingRT.anchorMax = new Vector2(0f, 0f);
        waitingRT.pivot = new Vector2(0f, 0f);
        waitingRT.anchoredPosition = new Vector2(16f, 10f);
        waitingRT.sizeDelta = new Vector2(200f, 24f);

        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(popupRoot.transform, false);
        arrow = arrowGO.AddComponent<ArrowImage>();
        arrow.color = OutlineBlue;
        arrow.raycastTarget = false;
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.sizeDelta = new Vector2(22f, 18f);
    }

    // Procedural button helper; accent = filled style, else ghost style.
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
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(14f * GameOptions.UiScale));
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = accent ? HexlinkTheme.AccentText : HexlinkTheme.TextLight;
        Stretch(textGO.GetComponent<RectTransform>());
        return button;
    }

    // Place the popup beside its anchor (right/above/below/left) and aim the arrow.
    private void PositionForAnchor(string anchorName)
    {
        RectTransform canvasRT = (RectTransform)canvas.transform;
        Rect anchorRect = GetAnchorRect(anchorName);

        Vector2 size = ((RectTransform)popupRoot.transform).sizeDelta;
        Vector2 half = canvasRT.rect.size * 0.5f;
        const float gap = 60f;

        // Canvas-local coordinates are centred on the canvas centre. Try the
        // four sides and keep the first that fits entirely on screen, so the
        // popup never ends up covering the thing it points at.
        string direction;
        Vector2 center;

        bool fitsRight = anchorRect.xMax + gap + size.x <= half.x;
        bool fitsAbove = anchorRect.yMax + gap + size.y <= half.y;
        bool fitsBelow = anchorRect.yMin - gap - size.y >= -half.y;
        bool fitsLeft = anchorRect.xMin - gap - size.x >= -half.x;

        if (fitsRight)
        {
            direction = "right";
            center = new Vector2(anchorRect.xMax + gap + size.x * 0.5f, anchorRect.center.y);
        }
        else if (fitsAbove)
        {
            direction = "above";
            center = new Vector2(anchorRect.center.x, anchorRect.yMax + gap + size.y * 0.5f);
        }
        else if (fitsBelow)
        {
            direction = "below";
            center = new Vector2(anchorRect.center.x, anchorRect.yMin - gap - size.y * 0.5f);
        }
        else if (fitsLeft)
        {
            direction = "left";
            center = new Vector2(anchorRect.xMin - gap - size.x * 0.5f, anchorRect.center.y);
        }
        else
        {
            direction = "above";
            center = new Vector2(anchorRect.center.x, anchorRect.yMax + gap + size.y * 0.5f);
        }

        center.x = Mathf.Clamp(center.x, -half.x + size.x * 0.5f + 8f, half.x - size.x * 0.5f - 8f);
        center.y = Mathf.Clamp(center.y, -half.y + size.y * 0.5f + 8f, half.y - size.y * 0.5f - 8f);

        ((RectTransform)popupRoot.transform).anchoredPosition = center;
        popupRoot.transform.SetAsLastSibling();

        // Aim the arrow at the anchor from the popup's near edge.
        RectTransform arrowRT = (RectTransform)arrow.transform;
        float clampX = size.x * 0.5f - 24f;
        float clampY = size.y * 0.5f - 24f;
        switch (direction)
        {
            case "right":
                arrowRT.anchorMin = new Vector2(0f, 0.5f);
                arrowRT.anchorMax = new Vector2(0f, 0.5f);
                arrowRT.pivot = new Vector2(0.5f, 0.5f);
                arrowRT.anchoredPosition = new Vector2(-10f, Mathf.Clamp(anchorRect.center.y - center.y, -clampY, clampY));
                arrowRT.localEulerAngles = new Vector3(0f, 0f, 90f);
                break;
            case "left":
                arrowRT.anchorMin = new Vector2(1f, 0.5f);
                arrowRT.anchorMax = new Vector2(1f, 0.5f);
                arrowRT.pivot = new Vector2(0.5f, 0.5f);
                arrowRT.anchoredPosition = new Vector2(10f, Mathf.Clamp(anchorRect.center.y - center.y, -clampY, clampY));
                arrowRT.localEulerAngles = new Vector3(0f, 0f, 270f);
                break;
            case "below":
                arrowRT.anchorMin = new Vector2(0.5f, 1f);
                arrowRT.anchorMax = new Vector2(0.5f, 1f);
                arrowRT.pivot = new Vector2(0.5f, 0.5f);
                arrowRT.anchoredPosition = new Vector2(Mathf.Clamp(anchorRect.center.x - center.x, -clampX, clampX), 10f);
                arrowRT.localEulerAngles = new Vector3(0f, 0f, 0f);
                break;
            default: // above
                arrowRT.anchorMin = new Vector2(0.5f, 0f);
                arrowRT.anchorMax = new Vector2(0.5f, 0f);
                arrowRT.pivot = new Vector2(0.5f, 0.5f);
                arrowRT.anchoredPosition = new Vector2(Mathf.Clamp(anchorRect.center.x - center.x, -clampX, clampX), -10f);
                arrowRT.localEulerAngles = new Vector3(0f, 0f, 180f);
                break;
        }
    }

    // Screen-space rect of the anchor, converted into canvas-local
    // coordinates (origin = canvas centre).
    private Rect GetAnchorRect(string anchorName)
    {
        RectTransform canvasRT = (RectTransform)canvas.transform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (anchorName == "BOARD")
        {
            HexGridSpawner spawner = FindAnyObjectByType<HexGridSpawner>();
            Vector3 world = spawner != null ? spawner.transform.position : Vector3.zero;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(Camera.main, world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, point, uiCamera, out Vector2 local))
            {
                return new Rect(local.x - 40f, local.y - 40f, 80f, 80f);
            }
            return new Rect(0f, 0f, 80f, 80f);
        }

        Transform anchor = FindDeep(canvas.transform, anchorName);
        if (anchor == null) return new Rect(0f, 0f, 80f, 80f);

        Vector3[] corners = new Vector3[4];
        ((RectTransform)anchor).GetWorldCorners(corners);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, uiCamera, out Vector2 local)) continue;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // Accent ring around the anchored element; BOARD is world-space, so skip it.
    private void HighlightAnchor(string anchorName)
    {
        if (anchorName == "BOARD") return;

        Transform anchor = FindDeep(canvas.transform, anchorName);
        if (anchor == null) return;

        highlight = new GameObject("TutorialHighlight", typeof(RectTransform));
        highlight.transform.SetParent(anchor, false);
        ChamferedImage ring = highlight.AddComponent<ChamferedImage>();
        ring.Chamfer = 18f;
        ring.OutlineThickness = 5f;
        ring.color = HexlinkTheme.Accent;
        ring.raycastTarget = false;
        RectTransform rt = highlight.GetComponent<RectTransform>();
        Stretch(rt);
        rt.offsetMin = new Vector2(-8f, -8f);
        rt.offsetMax = new Vector2(8f, 8f);
    }

    private void DestroyHighlight()
    {
        if (highlight != null)
        {
            Destroy(highlight);
            highlight = null;
        }
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
