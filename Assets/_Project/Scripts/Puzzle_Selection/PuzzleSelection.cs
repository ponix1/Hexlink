// Cross-scene carrier for the puzzle the player picked. Static class because
// plain C# statics survive scene loads (no DontDestroyOnLoad needed).
public static class PuzzleSelection
{
    // PuzzleData (ScriptableObject) the puzzle scene reads to build its level.
    public static PuzzleData SelectedPuzzle;
    // Scene the in-game Back button returns to ("Puzzle_Select" / "Level_Select").
    public static string ReturnScene;
}
