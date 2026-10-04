using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class SolutionSessionSpawner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Spawn();
        Spawn();
    }

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
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
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
    }
}

public class SolutionSession : MonoBehaviour
{
    private const float AutoSaveDelay = 1f;

    private GridController grid;
    private HexTileLabelController labelController;
    private List<SavedSolution> entries = new List<SavedSolution>();
    private int index = -1;
    private bool suppressAutoSave;
    private bool dirty;
    private float saveQueuedAt = -1f;

    private string PuzzleID => PuzzleSelection.SelectedPuzzle != null ? PuzzleSelection.SelectedPuzzle.puzzleID : null;

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

    private void Update()
    {
        if (saveQueuedAt >= 0f && Time.time - saveQueuedAt >= AutoSaveDelay)
        {
            FlushAutoSave();
        }
    }

    private void OnDisable()
    {
        FlushAutoSave();
        if (grid != null) grid.OnGridChanged -= QueueAutoSave;
        if (labelController != null) labelController.boardState.OnCellChanged -= QueueAutoSave;
    }

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

    private void ApplyCurrent()
    {
        if (grid == null || index < 0 || index >= entries.Count) return;

        suppressAutoSave = true;
        grid.ApplySolution(entries[index]);
        suppressAutoSave = false;

        dirty = false;
        Debug.Log($"SolutionSession: applied '{entries[index].name}' ({index + 1}/{entries.Count}).");
    }

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
