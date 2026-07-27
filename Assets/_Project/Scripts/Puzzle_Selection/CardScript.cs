using UnityEngine;
using TMPro;

public class PuzzleCardView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI puzzleNameText;
    [SerializeField] private TextMeshProUGUI targetNumText;
    [SerializeField] private TextMeshProUGUI starsText;

    public void Setup(PuzzleData data)
    {
        puzzleNameText.text = data.PuzzleName;
        targetNumText.text = data.TargetNumber.ToString();
        starsText.text = new string('★', data.StarRating);
    }
}