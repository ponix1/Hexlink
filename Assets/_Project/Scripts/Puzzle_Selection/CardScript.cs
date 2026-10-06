// One puzzle card in the Puzzle_Select grid (PuzzleCardView, file CardScript.cs).
// Shows title/target/stars + completion state; hover updates the 3D preview;
// click picks a solution and loads the puzzle scene.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class PuzzleCardView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private TextMeshProUGUI puzzleNameText;
    [SerializeField] private TextMeshProUGUI targetNumText;
    [SerializeField] private TextMeshProUGUI starsText;
    // Optional checkmark object shown when the puzzle is complete.
    [SerializeField] private GameObject completeBadge;
    // Scene loaded when this card is clicked.
    [SerializeField] private string puzzleSceneName;

    // The puzzle this card displays.
    private PuzzleData currentData;
    private Button button;

    // Wire the card's button click.
    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnCardClicked);
    }

    // Fill all visuals from the PuzzleData and apply completion styling.
    public void Setup(PuzzleData data)
    {
        currentData = data;

        // Completed puzzles get a checkmark prefix + green title
        // (PuzzleProgress is PlayerPrefs-backed).
        bool complete = PuzzleProgress.IsComplete(data.puzzleID);
        puzzleNameText.text = (complete ? "\u2713 " : "") + data.puzzleTitle;
        puzzleNameText.color = complete ? new Color(0.30f, 0.78f, 0.47f) : HexlinkTheme.TextLight;
        targetNumText.text = data.GetDisplayTarget();

        // Star rating only exists on StandardPuzzleData.
        if (data is StandardPuzzleData standardData)
        {
            string stars = "";
            for (int i = 0; i < standardData.starRating; i++) stars += "* ";
            starsText.text = stars.Trim();
        }
        else
        {
            starsText.text = "";
        }

        if (completeBadge != null)
        {
            completeBadge.SetActive(complete);
        }

        // Re-apply the active theme's colors to the refreshed texts.
        ThemeSwitcher.ApplyToSubtree(transform);
    }

    // Publish the selection, let the player pick a solution, then load the puzzle.
    private void OnCardClicked()
    {
        // Statics survive the scene load into the puzzle scene.
        PuzzleSelection.SelectedPuzzle = currentData;
        PuzzleSelection.ReturnScene = "Puzzle_Select";
        // Popup picks a saved solution index (or starts fresh); the choice
        // crosses scenes via SolutionStore.PendingIndex.
        SolutionPickerPopup.Show(currentData,
            index =>
            {
                SolutionStore.PendingIndex = index;
                SceneManager.LoadScene(puzzleSceneName);
            },
            () =>
            {
                SolutionStore.PendingIndex = SolutionStore.PendingNew;
                SceneManager.LoadScene(puzzleSceneName);
            });
    }

    // Hovering a card swaps the 3D preview to that puzzle.
    public void OnPointerEnter(PointerEventData eventData)
    {
        PuzzlePreview.Show(currentData);
    }
}