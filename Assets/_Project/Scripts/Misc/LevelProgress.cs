using System.Collections.Generic;

public static class LevelProgress
{
    public static List<PuzzleData> GetLevels()
    {
        GameContentRegistry registry = GameContentRegistry.Load();
        return registry != null && registry.levels != null
            ? registry.levels
            : new List<PuzzleData>();
    }

    public static bool IsUnlocked(int index)
    {
        List<PuzzleData> levels = GetLevels();
        if (index < 0 || index >= levels.Count) return false;
        if (index == 0) return true;
        return PuzzleProgress.IsComplete(levels[index - 1].puzzleID);
    }

    public static int CompletedCount()
    {
        List<PuzzleData> levels = GetLevels();
        int complete = 0;
        foreach (PuzzleData level in levels)
        {
            if (PuzzleProgress.IsComplete(level.puzzleID)) complete++;
        }
        return complete;
    }

    // Index of the level the player is currently on: the first one that is not
    // complete. Returns -1 when every level is complete.
    public static int CurrentIndex()
    {
        List<PuzzleData> levels = GetLevels();
        for (int i = 0; i < levels.Count; i++)
        {
            if (!PuzzleProgress.IsComplete(levels[i].puzzleID)) return i;
        }
        return -1;
    }

    public static bool TryGetNext(PuzzleData current, out PuzzleData next)
    {
        next = null;
        if (current == null) return false;

        List<PuzzleData> levels = GetLevels();
        int index = levels.IndexOf(current);
        if (index < 0 || index + 1 >= levels.Count) return false;

        if (!IsUnlocked(index + 1)) return false;

        next = levels[index + 1];
        return true;
    }
}
