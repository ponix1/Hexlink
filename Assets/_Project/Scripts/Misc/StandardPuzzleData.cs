using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Puzzle_", menuName = "Game/Puzzles/Standard Puzzle")]
public class StandardPuzzleData : PuzzleData
{
    public int targetScore;
    public List<int> availableNumbers;
    public List<string> availableOperations;
    public int layout;

    [Range(1, 5)]
    public int starRating = 1;

    public override string GetDisplayTarget()
    {
        return targetScore.ToString();
    }
}