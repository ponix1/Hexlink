using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Puzzle_", menuName = "Game/Puzzles/Standard Puzzle")]
public class StandardPuzzleData : PuzzleData
{
    public int targetScore;
    public List<int> availableNumbers;
    public List<string> availableOperations;
    public int layout; // Change this to a custom datatype possibly
}