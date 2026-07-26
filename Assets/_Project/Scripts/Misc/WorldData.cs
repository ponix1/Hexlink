using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "World_", menuName = "Game/World Data")]
public class WorldData : ScriptableObject
{
    public string worldName;
    public List<PuzzleData> puzzles;   // World B's asset drags in Puzzle_B1...B6 here
}