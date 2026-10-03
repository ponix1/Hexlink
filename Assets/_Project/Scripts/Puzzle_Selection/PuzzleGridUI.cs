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

        RefreshGridLayout();
    }

    private void RefreshGridLayout()
    {
        if (contentParent == null) return;
        RectTransform contentRT = contentParent as RectTransform;
        GridLayoutGroup grid = contentRT != null ? contentRT.GetComponent<GridLayoutGroup>() : null;
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (contentRT == null || grid == null || scrollRect == null || scrollRect.viewport == null) return;

        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, contentRT.sizeDelta.y);
        grid.childAlignment = TextAnchor.UpperCenter;

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);

        const float targetCardWidth = 340f;
        const float cardAspect = 232f / 340f;
        float available = scrollRect.viewport.rect.width - grid.padding.horizontal;

        int columns = Mathf.Max(1, Mathf.RoundToInt(available / targetCardWidth));
        float cardWidth = (available - (columns - 1) * grid.spacing.x) / columns;
        if (cardWidth > targetCardWidth * 1.25f)
        {
            columns++;
            cardWidth = (available - (columns - 1) * grid.spacing.x) / columns;
        }
        cardWidth = Mathf.Min(cardWidth, targetCardWidth * 1.25f);

        grid.cellSize = new Vector2(cardWidth, cardWidth * cardAspect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
    }
}