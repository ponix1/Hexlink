// The standard math puzzle: reach a target score from the given numbers.
using System.Collections.Generic;
using UnityEngine;

// Concrete puzzle type: win by making a final tile hit targetScore.
[CreateAssetMenu(fileName = "Puzzle_", menuName = "Game/Puzzles/Standard Puzzle")]
public class StandardPuzzleData : PuzzleData
{
    // Win target: the final tile must equal this score.
    public int targetScore;
    // Number tiles the player may place on the board.
    public List<int> availableNumbers;
    // Operation symbols ("+", "-", ...) barred for this puzzle.
    public List<string> disabledOperations;

    // Difficulty shown in menus, 1 (easy) to 5 (hard).
    [Range(1, 5)]
    public int starRating = 1;

    public override string GetDisplayTarget()
    {
        return targetScore.ToString();
    }
}