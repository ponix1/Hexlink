// SolutionSession - per-run solution manager: auto-saves grid/board edits into the
// active solution slot (debounced), restores/switches solutions via the picker
// popup, and marks them solved on win. Auto-spawned in Puzzle_Play scenes.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Boots the session, a "Solutions" button (top-right) and the "TARGET: <n>"
// readout (top-centre) onto the canvas of every scene.
public static class SolutionSessionSpawner
{
    // Install the spawn hook for every scene load (and run it once now).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Spawn();
        Spawn();
    }

    // Only in Puzzle_Play, and only once per canvas (idempotent).
    private static void Spawn()
    {
        if (SceneManager.GetActiveScene().name != "Puzzle_Play") return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.transform.Find("SolutionSession") != null) return;

        GameObject sessionGO = new GameObject("SolutionSession", typeof(RectTransform));
        sessionGO.transform.SetParent(canvas.transform, false);
        SolutionSession session = sessionGO.AddComponent<SolutionSession>();

        GameObject button = new GameObject("SolutionsButton", typeof(RectTransform), typeof(Button));
        button.transform.SetParent(canvas.transform, false);

        ChamferedImage chamfer = button.AddComponent<ChamferedImage>();
        chamfer.Chamfer = 12f;
        chamfer.color = HexlinkTheme.Ghost;
        chamfer.raycastTarget = true;

        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = chamfer;
        HexlinkTheme.ApplyHoverTint(buttonComponent);
        buttonComponent.onClick.AddListener(session.OpenPopup);

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        // Sits flush left of the Back button (110 wide at -10,-10).
        rt.anchoredPosition = new Vector2(-130f, -10f);
        rt.sizeDelta = new Vector2(160f, 34f);

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = "Solutions";
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(14f * GameOptions.UiScale));
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        ThemeSwitcher.ApplyToSubtree(button.transform);

        SpawnTargetLabel(canvas);
    }

    // "TARGET: <number>" readout pinned to the top-centre of the screen so the
    // player always sees the score they need to reach.
    private static void SpawnTargetLabel(Canvas canvas)
    {
        GameObject targetGO = new GameObject("TargetLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        targetGO.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI targetText = targetGO.GetComponent<TextMeshProUGUI>();
        PuzzleData puzzle = PuzzleSelection.SelectedPuzzle;
        if (puzzle == null)
        {
            targetGO.SetActive(false);
            return;
        }

        targetText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(HexlinkTheme.TextGray)}>TARGET:</color> " +
                          $"<color=#{ColorUtility.ToHtmlStringRGB(HexlinkTheme.Accent)}>{puzzle.GetDisplayTarget()}</color>";
        targetText.fontSize = Mathf.Max(8f, Mathf.Round(16f * GameOptions.UiScale));
        targetText.fontStyle = FontStyles.Bold;
        targetText.color = HexlinkTheme.TextLight;
        targetText.alignment = TextAlignmentOptions.Center;
        targetText.raycastTarget = false;
        targetText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform targetRT = targetGO.GetComponent<RectTransform>();
        targetRT.anchorMin = new Vector2(0.5f, 1f);
        targetRT.anchorMax = new Vector2(0.5f, 1f);
        targetRT.pivot = new Vector2(0.5f, 1f);
        targetRT.anchoredPosition = new Vector2(0f, -10f);
        targetRT.sizeDelta = new Vector2(300f, 30f);

        ThemeSwitcher.ApplyToSubtree(targetGO.transform);
    }
}

// Tracks the active solution slot and pushes edits to SolutionStore after a
// short debounce; also owns opening the SolutionPickerPopup.
public class SolutionSession : MonoBehaviour
{
    // Debounce window after the last edit before flushing to the store.
    private const float AutoSaveDelay = 1f;

    private GridController grid;
    private HexTileLabelController labelController;
    // All saved solutions for this puzzle, in store order.
    private List<SavedSolution> entries = new List<SavedSolution>();
    // Active slot in entries; -1 = fresh unsaved solution (first save creates it).
    private int index = -1;
    // Guards programmatic wipes/applies so they never trigger auto-save.
    private bool suppressAutoSave;
    // Edit pending + Time.time when the debounce started (-1 = idle).
    private bool dirty;
    private float saveQueuedAt = -1f;

    private string PuzzleID => PuzzleSelection.SelectedPuzzle != null ? PuzzleSelection.SelectedPuzzle.puzzleID : null;

    // Bind scene refs, consume any pending slot choice, register auto-save hooks.
    private IEnumerator Start()
    {
        grid = FindAnyObjectByType<GridController>();
        labelController = FindAnyObjectByType<HexTileLabelController>();

        // GridController restores the newest solution after one frame; wait two so
        // pending selections can replace it before auto-save engages.
        yield return null;
        yield return null;

        int pending = SolutionStore.PendingIndex;
        SolutionStore.PendingIndex = SolutionStore.PendingNone;

        entries = PuzzleID != null ? SolutionStore.LoadAll(PuzzleID) : new List<SavedSolution>();

        if (pending == SolutionStore.PendingNew)
        {
            index = -1;
            dirty = false;
            if (grid != null)
            {
                suppressAutoSave = true;
                grid.ClearAll();
                suppressAutoSave = false;
            }
        }
        else if (pending >= 0 && pending < entries.Count)
        {
            index = pending;
            ApplyCurrent();
        }
        else
        {
            index = entries.Count - 1;
        }

        if (grid != null) grid.OnGridChanged += QueueAutoSave;
        if (labelController != null) labelController.boardState.OnCellChanged += QueueAutoSave;
    }

    // Flush the queued auto-save once the debounce has elapsed.
    private void Update()
    {
        if (saveQueuedAt >= 0f && Time.time - saveQueuedAt >= AutoSaveDelay)
        {
            FlushAutoSave();
        }
    }

    // Final flush + unhook on teardown.
    private void OnDisable()
    {
        FlushAutoSave();
        if (grid != null) grid.OnGridChanged -= QueueAutoSave;
        if (labelController != null) labelController.boardState.OnCellChanged -= QueueAutoSave;
    }

    // Board-change adapter (event signature) for the parameterless queue.
    private void QueueAutoSave(HexCoord coord)
    {
        QueueAutoSave();
    }

    private void QueueAutoSave()
    {
        if (suppressAutoSave) return;
        dirty = true;
        saveQueuedAt = Time.time;
    }

    // Persist now: update the active slot, or create a new one when index is -1.
    private void FlushAutoSave()
    {
        saveQueuedAt = -1f;
        if (!dirty || PuzzleID == null || grid == null || labelController == null) return;

        if (index >= entries.Count) index = -1;

        if (index >= 0)
        {
            SavedSolution capture = SolutionStore.Capture(PuzzleSelection.SelectedPuzzle, labelController.boardState, grid);
            if (SolutionStore.UpdateEntry(PuzzleID, index, capture))
            {
                dirty = false;
            }
        }
        else
        {
            SavedSolution saved = SolutionStore.Save(PuzzleSelection.SelectedPuzzle, labelController.boardState, grid);
            if (saved != null)
            {
                entries = SolutionStore.LoadAll(PuzzleID);
                index = entries.Count - 1;
                dirty = false;
            }
        }
    }

    // Load the active entry into the grid without triggering auto-save.
    private void ApplyCurrent()
    {
        if (grid == null || index < 0 || index >= entries.Count) return;

        suppressAutoSave = true;
        grid.ApplySolution(entries[index]);
        suppressAutoSave = false;

        dirty = false;
        Debug.Log($"SolutionSession: applied '{entries[index].name}' ({index + 1}/{entries.Count}).");
    }

    // Flush first, then show the picker; callbacks open/apply a choice or start fresh.
    public void OpenPopup()
    {
        if (PuzzleID == null || PuzzleSelection.SelectedPuzzle == null) return;

        FlushAutoSave();
        entries = SolutionStore.LoadAll(PuzzleID);
        if (index >= entries.Count) index = -1;

        SolutionPickerPopup.Show(PuzzleSelection.SelectedPuzzle,
            openIndex =>
            {
                if (PuzzleID == null || grid == null) return;
                entries = SolutionStore.LoadAll(PuzzleID);
                index = -1;
                if (openIndex >= 0 && openIndex < entries.Count)
                {
                    index = openIndex;
                    ApplyCurrent();
                }
            },
            () =>
            {
                if (grid == null) return;
                FlushAutoSave();
                suppressAutoSave = true;
                grid.ClearAll();
                suppressAutoSave = false;
                index = -1;
                dirty = false;
            });
    }

    // Flag the active solution as solved in the store (called on win).
    public void MarkCurrentSolved()
    {
        if (PuzzleID == null) return;

        FlushAutoSave();
        entries = SolutionStore.LoadAll(PuzzleID);

        if (index < 0 || index >= entries.Count)
        {
            index = entries.Count - 1;
        }

        if (index >= 0)
        {
            SolutionStore.MarkSolved(PuzzleID, index);
            entries = SolutionStore.LoadAll(PuzzleID);
        }
    }
}
