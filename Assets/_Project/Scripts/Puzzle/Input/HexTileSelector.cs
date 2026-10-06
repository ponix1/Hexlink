// HexTileSelector — the 3D board's input hub. Per-frame Physics raycast for hover,
// click routing (palette placement vs. instruction authoring), middle-click removal,
// plus the pulsing/lifted "authoring prompt" visuals and scriptable UI highlights.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// All mouse input to the hex board flows through here. Hover tinting uses a
// MaterialPropertyBlock (_BaseColor) so shared materials stay batched under URP;
// placement/removal is delegated to BoardState, authoring goes via OnTileClicked.
public class HexTileSelector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private TileInventoryUI inventoryUI;
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexGridSpawner hexGridSpawner;
    [SerializeField] private InstructionAuthoringController authoringController;

    // Reached through the authoring controller (already wired in existing scenes)
    // so this logic never depends on a fresh InfoTab rebuild.
    private GridController GridController => authoringController != null ? authoringController.GridController : null;

    // Raised on left-click when no palette tile is selected (authoring subscribes).
    public event System.Action<HexCoord> OnTileClicked;

    // URP lit shader color property; written via MaterialPropertyBlock, not materials.
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // Visual state of one hex currently owned by the authoring prompt (pulse + lift).
    private class AuthoringHex
    {
        public HexCoord Coord;
        public Transform Root;
        public MeshRenderer Renderer;
        public Color BaseColor;
        public Color PulseColor;
        public float Phase;
        public float LiftHeight;
    }

    private MaterialPropertyBlock propertyBlock;
    // Hex under the mouse (null = none); click routing depends on it.
    private MeshRenderer hoveredRenderer;
    // True while a scripted UI highlight (HighlightTile) owns the tint — hover stands down.
    private bool uiHighlightActive;
    // Set only when we tinted the hover renderer, so we never wipe a tint we don't own.
    private bool hoverTintApplied;

    private readonly List<AuthoringHex> authoringHexes = new List<AuthoringHex>();
    // Rest (spawn) positions per tile root, remembered so lifted tiles can glide home.
    private readonly Dictionary<Transform, Vector3> homePositions = new Dictionary<Transform, Vector3>();
    // While true the authoring prompt owns tile colors — hover tracks but doesn't tint.
    private bool authoringPromptActive;

    private Color subjectBase = new Color(0.612f, 0.784f, 0.831f);
    private Color subjectPulse = new Color(0.761f, 0.878f, 0.918f);
    private const float SubjectLift = 0.18f;
    private const float CandidateLift = 0.07f;
    private const float PulseSpeed = 3f;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Keyboard-submit would re-trigger the last clicked UI button when the player
        // presses Space/Enter for instruction authoring — disable navigation entirely.
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = false;
        }

        if (GameOptions.ColourBlindMode)
        {
            highlightColor = new Color(0f, 0.85f, 1f);
            subjectBase = new Color(0.498f, 0.702f, 0.910f);
            subjectPulse = new Color(0.659f, 0.804f, 0.949f);
        }
    }

    private void Update()
    {
        // Selecting a palette tile mid-instruction cancels authoring — placement wins.
        if (authoringController != null && authoringController.IsBusy
            && inventoryUI != null && !string.IsNullOrEmpty(inventoryUI.CurrentSelectedTile))
        {
            authoringController.CancelPending();
        }

        UpdateAuthoringPrompt();
        UpdateLiftManagement();

        // Pointer is over uGUI — board hover and clicks stand down for this frame.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (!uiHighlightActive) ClearHover();
            return;
        }

        UpdateHover();
        CheckForClick();
    }

    private void UpdateHover()
    {
        if (uiHighlightActive) return;

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        MeshRenderer target = null;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HexTileIdentity identity = hit.collider.GetComponentInParent<HexTileIdentity>();
            if (identity != null)
            {
                target = identity.GetComponentInChildren<MeshRenderer>();
            }
        }

        if (target == hoveredRenderer) return;

        ClearHover();
        if (target != null)
        {
            // Always track which hex is under the mouse (clicks depend on it), but
            // while an authoring prompt owns the tile colors, only track - don't tint.
            hoveredRenderer = target;
            if (!authoringPromptActive)
            {
                SetHover(target);
            }
        }
    }

    // Left-click: place the selected palette tile, or raise OnTileClicked for authoring.
    // Middle-click: remove the hovered tile.
    private void CheckForClick()
    {
        // Left-click on empty space exits instruction input mode.
        if (Input.GetMouseButtonDown(0) && hoveredRenderer == null)
        {
            if (authoringController != null)
            {
                authoringController.ExitInputMode();
            }
            return;
        }

        if (hoveredRenderer == null)
        {
            return;
        }

        HexTileIdentity identity = hoveredRenderer.GetComponentInParent<HexTileIdentity>();

        if (Input.GetMouseButtonDown(2))
        {
            if (TutorialGate.Active && !TutorialGate.AllowRemoval) return;
            if (identity != null && labelController.boardState.HasTile(identity.coordinate))
            {
                labelController.boardState.RemoveTile(identity.coordinate);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (identity == null) return;
            // LMB+RMB together is the camera-pan chord — don't treat as a click.
            if (Input.GetMouseButton(1)) return;

            bool authoringBusy = authoringController != null && authoringController.IsBusy;
            string tileToPlace = inventoryUI != null ? inventoryUI.CurrentSelectedTile : "";
            if (!authoringBusy && !string.IsNullOrEmpty(tileToPlace))
            {
                if (TutorialGate.Active && !TutorialGate.AllowPlacement) return;
                TileData tileData = TileDataFactory.CreateFromSymbol(tileToPlace);
                if (tileData == null) return;

                if (tileData is FinalTileData)
                {
                    tileData = new FinalTileData { targetNumber = GetPuzzleTarget() };
                }

                TileData existing = labelController.boardState.GetTile(identity.coordinate);
                GridController grid = GridController;

                // Replacing a tile that instructions reference needs a double-click:
                // first click arms a pending change (yellow tint), second confirms.
                if (GameOptions.ConfirmTileChanges && existing != null && grid != null && grid.HasInstructionsReferencing(identity.coordinate))
                {
                    if (grid.IsPendingChange(identity.coordinate))
                    {
                        grid.ClearPendingChange();
                        Debug.Log("Change confirmed - instructions updated.");
                        labelController.boardState.PlaceTile(identity.coordinate, tileData);
                        if (inventoryUI != null) inventoryUI.DeselectTile();
                    }
                    else
                    {
                        grid.SetPendingChange(identity.coordinate);
                    }
                    return;
                }

                if (grid != null) grid.ClearPendingChange();
                labelController.boardState.PlaceTile(identity.coordinate, tileData);
                if (inventoryUI != null) inventoryUI.DeselectTile();
            }
            else
            {
                if (TutorialGate.Active && !TutorialGate.AllowAuthoring) return;
                OnTileClicked?.Invoke(identity.coordinate);
            }
        }
    }

    private static int GetPuzzleTarget()
    {
        if (PuzzleSelection.SelectedPuzzle is StandardPuzzleData standard)
        {
            return standard.targetScore;
        }
        return 0;
    }

    // Scripted highlight for UI tokens (not mouse-driven); hover must not fight it.
    public void HighlightTile(HexCoord coord)
    {
        if (authoringPromptActive) return;
        if (hexGridSpawner == null) return;
        if (!hexGridSpawner.SpawnedTiles.TryGetValue(coord, out GameObject hexTile)) return;

        MeshRenderer renderer = hexTile.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        if (renderer != hoveredRenderer) ClearHover();
        SetHover(renderer);
        uiHighlightActive = true;
    }

    // Release the scripted highlight; hover re-acquires naturally afterwards.
    public void ClearTileHighlight()
    {
        uiHighlightActive = false;
        ClearHover();
    }

    // (Re)builds the prompt visuals for the current authoring stage: subject-only
    // (candidates == null) or a set of clickable candidates. Replaces any old prompt.
    public void ShowAuthoringPrompt(HexCoord subject, List<HexCoord> candidates)
    {
        ClearAuthoringPrompt();
        if (hexGridSpawner == null) return;

        int phaseIndex = 0;
        if (candidates != null)
        {
            foreach (HexCoord candidate in candidates)
            {
                AddAuthoringHex(candidate, subjectBase, subjectPulse, phaseIndex * 0.35f, CandidateLift);
                phaseIndex++;
            }
        }

        // One colour for everything, and highlight means "clickable right now":
        // the subject rises only while it is the active selection (candidates == null).
        // A stage that passes an empty list has no options - show nothing rather
        // than falsely lifting the subject as if it were the thing to click.
        if (candidates == null)
        {
            AddAuthoringHex(subject, subjectBase, subjectPulse, 0f, SubjectLift);
        }
        authoringPromptActive = authoringHexes.Count > 0;
    }

    public void ClearAuthoringPrompt()
    {
        foreach (AuthoringHex hex in authoringHexes)
        {
            if (hex.Renderer != null)
            {
                propertyBlock.Clear();
                hex.Renderer.SetPropertyBlock(propertyBlock);
            }
        }
        authoringHexes.Clear();
        authoringPromptActive = false;

        // Let the hover system re-acquire whatever hex is under the mouse right now.
        // Lifted tiles need no bookkeeping here - UpdateLiftManagement glides anything
        // the prompt no longer owns back down to its home position.
        hoveredRenderer = null;
        hoverTintApplied = false;
    }

    private Vector3 GetHomePosition(Transform root)
    {
        // The board position tiles spawn at, captured the first time we ever lift a
        // tile. Recording it once means a hex that re-enters a prompt while still
        // mid-lift can never poison its own "rest height" with a lifted position.
        if (!homePositions.TryGetValue(root, out Vector3 home))
        {
            home = root.position;
            homePositions[root] = home;
        }
        return home;
    }

    private void AddAuthoringHex(HexCoord coord, Color baseColor, Color pulseColor, float phase, float liftHeight)
    {
        if (!hexGridSpawner.SpawnedTiles.TryGetValue(coord, out GameObject hexTile)) return;
        MeshRenderer renderer = hexTile.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        Transform root = hexTile.transform;
        GetHomePosition(root);

        // Dedupe by coord: re-showing a hex in a later stage refreshes it, not stacks it.
        authoringHexes.RemoveAll(existing => existing.Coord.Equals(coord));
        authoringHexes.Add(new AuthoringHex
        {
            Coord = coord,
            Root = root,
            Renderer = renderer,
            BaseColor = baseColor,
            PulseColor = pulseColor,
            Phase = phase,
            LiftHeight = liftHeight
        });
    }

    // Animate prompt hexes: sine-pulse between base/pulse color (phase-staggered) and
    // glide each root up to home + lift. ReducedMotion freezes both effects.
    private void UpdateAuthoringPrompt()
    {
        if (!authoringPromptActive) return;

        float time = Time.time;
        bool reducedMotion = GameOptions.ReducedMotion;

        foreach (AuthoringHex hex in authoringHexes)
        {
            if (hex.Renderer == null) continue;

            float blend = reducedMotion ? 0f : 0.5f + 0.5f * Mathf.Sin(time * PulseSpeed + hex.Phase);
            propertyBlock.SetColor(BaseColorID, Color.Lerp(hex.BaseColor, hex.PulseColor, blend));
            hex.Renderer.SetPropertyBlock(propertyBlock);

            if (!reducedMotion && hex.Root != null)
            {
                Vector3 lifted = GetHomePosition(hex.Root) + Vector3.up * hex.LiftHeight;
                hex.Root.position = Vector3.Lerp(hex.Root.position, lifted, 8f * Time.deltaTime);
            }
        }
    }

    private void UpdateLiftManagement()
    {
        // Janitor pass: anything the active prompt doesn't own glides back to its
        // home position. Descent is the default state, so a tile can never get
        // stranded mid-air by a missed transition - there is nothing to enroll in.
        HashSet<Transform> managed = null;
        if (authoringPromptActive && authoringHexes.Count > 0)
        {
            managed = new HashSet<Transform>();
            foreach (AuthoringHex hex in authoringHexes)
            {
                if (hex.Root != null) managed.Add(hex.Root);
            }
        }

        bool reducedMotion = GameOptions.ReducedMotion;
        List<Transform> pruned = null;

        foreach (KeyValuePair<Transform, Vector3> entry in homePositions)
        {
            Transform root = entry.Key;
            if (root == null)
            {
                if (pruned == null) pruned = new List<Transform>();
                pruned.Add(root);
                continue;
            }

            if (managed != null && managed.Contains(root)) continue;

            Vector3 home = entry.Value;
            if (root.position == home) continue;

            if (reducedMotion)
            {
                root.position = home;
            }
            else
            {
                root.position = Vector3.Lerp(root.position, home, 10f * Time.deltaTime);
                if ((root.position - home).sqrMagnitude < 0.0001f)
                {
                    root.position = home;
                }
            }
        }

        if (pruned != null)
        {
            foreach (Transform root in pruned)
            {
                homePositions.Remove(root);
            }
        }
    }

    // The six hex neighbors of coord that actually have a spawned tile.
    public List<HexCoord> GetExistingNeighbors(HexCoord coord)
    {
        List<HexCoord> result = new List<HexCoord>();
        for (int i = 0; i < 6; i++)
        {
            HexCoord neighbor = coord.GetNeighbor(i);
            if (hexGridSpawner != null && hexGridSpawner.SpawnedTiles.ContainsKey(neighbor))
            {
                result.Add(neighbor);
            }
        }
        return result;
    }

    // Neighbors of coord that hold an operation tile (C-stage candidates).
    public List<HexCoord> GetAdjacentOperationTiles(HexCoord coord)
    {
        List<HexCoord> result = new List<HexCoord>();
        if (labelController == null) return result;

        foreach (HexCoord neighbor in GetExistingNeighbors(coord))
        {
            if (labelController.boardState.GetTile(neighbor) is OperationTileData)
            {
                result.Add(neighbor);
            }
        }
        return result;
    }

    // Apply the hover tint via the property block and remember that we applied it.
    private void SetHover(MeshRenderer renderer)
    {
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorID, highlightColor);
        renderer.SetPropertyBlock(propertyBlock);
        hoveredRenderer = renderer;
        hoverTintApplied = true;
    }

    private void ClearHover()
    {
        if (hoveredRenderer != null)
        {
            if (hoverTintApplied)
            {
                propertyBlock.Clear();
                hoveredRenderer.SetPropertyBlock(propertyBlock);
            }
            hoveredRenderer = null;
            hoverTintApplied = false;
        }
    }
}
