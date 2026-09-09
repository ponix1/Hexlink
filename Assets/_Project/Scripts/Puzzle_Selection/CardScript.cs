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
    [SerializeField] private GameObject completeBadge;
    [SerializeField] private string puzzleSceneName;

    private PuzzleData currentData;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnCardClicked);
    }

    public void Setup(PuzzleData data)
    {
        currentData = data;

        bool complete = PuzzleProgress.IsComplete(data.puzzleID);
        puzzleNameText.text = (complete ? "\u2713 " : "") + data.puzzleTitle;
        puzzleNameText.color = complete ? new Color(0.25f, 0.7f, 0.4f) : Color.black;
        targetNumText.text = data.GetDisplayTarget();

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
    }

    private void OnCardClicked()
    {
        PuzzleSelection.SelectedPuzzle = currentData;
        SceneManager.LoadScene(puzzleSceneName);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PuzzlePreview.Show(currentData);
    }
}