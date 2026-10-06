// Asset listing the puzzles that make up one world.
using System.Collections.Generic;
using UnityEngine;

// Container for a world's puzzle list (used by world select / progression).
[CreateAssetMenu(fileName = "World_", menuName = "Game/World Data")]
public class WorldData : ScriptableObject
{
    public string worldName;
    public List<PuzzleData> puzzles;   // World B's asset drags in Puzzle_B1...B6 here
}