// Puzzle card grid for the Puzzle_Select screen.
// Spawns one card prefab per puzzle and keeps the ScrollRect/grid layout
// scrollable and responsive to the viewport width.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fills the scrolling card grid; WorldSelectUI calls Populate on every world change.
public class PuzzleGridUI : MonoBehaviour
{
    // ScrollRect content transform the cards are instantiated under.
    [SerializeField] private Transform contentParent;
    // Card prefab instantiated once per puzzle (prefab cards, not procedural UI).
    [SerializeField] private PuzzleCardView cardPrefab;

    // Cards from the last Populate, destroyed on the next rebuild.
    private List<GameObject> spawnedCards = new List<GameObject>();

    // Rebuild the grid: clear old cards, spawn one card per puzzle.
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

    // Make the content rect grow with the grid so the parent ScrollRect can scroll.
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

        // Guarantee a usable scroll speed even if the authored value is tiny.
        ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.scrollSensitivity < 60f)
        {
            scrollRect.scrollSensitivity = 60f;
        }

        RefreshGridLayout();
    }

    // Responsive layout: derive column count and cell size from the viewport width.
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

        // Round to a column count; if that would stretch cards >25% too wide,
        // spend the slack on one extra column instead.
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