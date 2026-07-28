using UnityEngine;
using TMPro;

public class PuzzleCardView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI puzzleNameText;
    [SerializeField] private TextMeshProUGUI targetNumText;
    [SerializeField] private TextMeshProUGUI starsText;

    public void Setup(PuzzleData data)
    {
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
}