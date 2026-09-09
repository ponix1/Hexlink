using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class SolutionBarSpawner
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

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        if (canvas.transform.Find("SolutionBar") != null) return;

        GameObject bar = new GameObject("SolutionBar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
        bar.transform.SetParent(canvas.transform, false);
        bar.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.21f);
        RectTransform barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.5f, 1f);
        barRT.anchorMax = new Vector2(0.5f, 1f);
        barRT.pivot = new Vector2(0.5f, 1f);
        barRT.anchoredPosition = new Vector2(0f, -8f);
        barRT.sizeDelta = new Vector2(400f, 34f);

        HorizontalLayoutGroup layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 6f;
        layout.padding = new RectOffset(6, 6, 4, 4);

        CreateButton(bar.transform, "NewButton", "New", 52f).onClick.AddListener(() => bar.GetComponent<SolutionBarController>().NewSolution());
        CreateButton(bar.transform, "PrevButton", "\u2039", 34f).onClick.AddListener(() => bar.GetComponent<SolutionBarController>().Cycle(-1));
        GameObject label = CreateLabel(bar.transform, "SolutionLabel", "No solutions");
        CreateButton(bar.transform, "NextButton", "\u203A", 34f).onClick.AddListener(() => bar.GetComponent<SolutionBarController>().Cycle(1));
        CreateButton(bar.transform, "SaveButton", "Save", 64f).onClick.AddListener(() => bar.GetComponent<SolutionBarController>().SaveCurrent());

        bar.AddComponent<SolutionBarController>();
    }

    private static Button CreateButton(Transform parent, string name, string label, float width)
    {
        GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(parent, false);
        Image image = button.GetComponent<Image>();
        image.color = Color.white;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        buttonComponent.transition = Button.Transition.ColorTint;
        LayoutElement le = button.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 26f;

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return buttonComponent;
    }

    private static GameObject CreateLabel(Transform parent, string name, string text)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 13;
        tmp.color = new Color(0.91f, 0.91f, 0.91f);
        tmp.alignment = TextAlignmentOptions.Center;
        LayoutElement le = label.AddComponent<LayoutElement>();
        le.preferredWidth = 170f;
        le.flexibleWidth = 1f;
        le.preferredHeight = 26f;
        return label;
    }
}

public class SolutionBarController : MonoBehaviour
{
    private GridController grid;
    private HexTileLabelController labelController;
    private TextMeshProUGUI label;
    private List<SavedSolution> entries = new List<SavedSolution>();
    private int index = -1;
    private bool freshMode;

    private IEnumerator Start()
    {
        grid = FindFirstObjectByType<GridController>();
        labelController = FindFirstObjectByType<HexTileLabelController>();
        label = transform.Find("SolutionLabel").GetComponent<TextMeshProUGUI>();

        // GridController waits a frame before auto-restoring; wait two so the label
        // reflects whatever was restored.
        yield return null;
        yield return null;
        Refresh();
    }

    private string PuzzleID => PuzzleSelection.SelectedPuzzle != null ? PuzzleSelection.SelectedPuzzle.puzzleID : null;

    private void Refresh()
    {
        entries = PuzzleID != null ? SolutionStore.LoadAll(PuzzleID) : new List<SavedSolution>();
        if (index >= entries.Count) index = entries.Count - 1;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (label == null) return;
        if (freshMode)
        {
            label.text = "New solution (unsaved)";
        }
        else if (entries.Count == 0)
        {
            label.text = "No solutions";
        }
        else
        {
            if (index < 0) index = entries.Count - 1;
            label.text = $"{entries[index].name}  ({index + 1}/{entries.Count})";
        }
    }

    public void NewSolution()
    {
        if (grid == null) return;

        freshMode = true;
        grid.ClearAll();
        UpdateLabel();
    }

    public void Cycle(int direction)
    {
        Refresh();
        if (entries.Count == 0)
        {
            label.text = "No solutions";
            return;
        }

        freshMode = false;
        if (index < 0 || index >= entries.Count) index = entries.Count - 1;
        index = (index + direction + entries.Count) % entries.Count;
        ApplyCurrent();
    }

    public void SaveCurrent()
    {
        if (PuzzleID == null || grid == null || labelController == null) return;

        SolutionStore.Save(PuzzleSelection.SelectedPuzzle, labelController.boardState, grid);
        freshMode = false;
        Refresh();
        index = entries.Count - 1;
        UpdateLabel();
    }

    private void ApplyCurrent()
    {
        if (grid == null || index < 0 || index >= entries.Count) return;
        grid.ApplySolution(entries[index]);
        Debug.Log($"SolutionBar: applied '{entries[index].name}' ({index + 1}/{entries.Count}).");
        UpdateLabel();
    }
}
