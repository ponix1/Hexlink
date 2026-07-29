using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PuzzleCardView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI puzzleNameText;
    [SerializeField] private TextMeshProUGUI targetNumText;
    [SerializeField] private TextMeshProUGUI starsText;
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

        puzzleNameText.text = data.puzzleTitle;
        targetNumText.text = data.GetDisplayTarget();

        if (data is StandardPuzzleData standardData)
        {
            starsText.text = new string('*', standardData.starRating);
        }
        else
        {
            starsText.text = "";
        }
    }

    private void OnCardClicked()
    {
        PuzzleSelection.SelectedPuzzle = currentData;
        SceneManager.LoadScene(puzzleSceneName);
    }
}