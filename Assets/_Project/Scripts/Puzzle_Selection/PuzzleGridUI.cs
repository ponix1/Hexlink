using System.Collections.Generic;
using UnityEngine;

public class PuzzleGridUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private PuzzleCardView cardPrefab;

    private List<GameObject> spawnedCards = new List<GameObject>();

    public void Populate(List<PuzzleData> puzzles)
    {
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
}