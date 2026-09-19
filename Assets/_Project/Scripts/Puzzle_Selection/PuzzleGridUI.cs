using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleGridUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private PuzzleCardView cardPrefab;

    private List<GameObject> spawnedCards = new List<GameObject>();

    public void Populate(List<PuzzleData> puzzles)
    {
        EnsureScrollableContent();

        foreach (GameObject card in spawnedCards)
        {
            Destroy(card);
        }
        spawnedCards.Clear();

        foreach (PuzzleData puzzle in puzzles)
        {
            PuzzleCardView newCard = Instantiate(cardPrefab, contentParent);
            newCard.Setup(puzzle);
            spawnedCards.Add(newCard.gameObject);
        }
    }

    private void EnsureScrollableContent()
    {
        if (contentParent == null) return;
        RectTransform rect = contentParent as RectTransform;
        if (rect == null) return;

        // Without a fitter the content rect keeps its authored fixed height, so the
        // ScrollRect sees nothing to scroll and puzzles past the first rows are
        // unreachable. Preferred-size lets the GridLayoutGroup drive the height,
        // so any number of puzzles scrolls.
        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.scrollSensitivity < 60f)
        {
            scrollRect.scrollSensitivity = 60f;
        }
    }
}