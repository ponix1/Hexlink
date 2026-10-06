// Per-puzzle completion flags, persisted via PlayerPrefs.
using UnityEngine;

// Static completion flags keyed by puzzleID. PlayerPrefs (not a JSON file)
// suffices here: it's one bool per puzzle and survives restarts for free.
public static class PuzzleProgress
{
    // Key namespace so puzzle flags never collide with other saved prefs.
    private const string KeyPrefix = "Hexlink.Complete.";

    // False for null/empty ids so callers can pass unkeyed puzzles safely.
    public static bool IsComplete(string puzzleId)
    {
        return !string.IsNullOrEmpty(puzzleId) && PlayerPrefs.GetInt(KeyPrefix + puzzleId, 0) == 1;
    }

    // Flag a puzzle done. Early-out keeps it idempotent; Save() writes to
    // disk immediately so progress survives a crash right after a win.
    public static void MarkComplete(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId) || IsComplete(puzzleId)) return;

        PlayerPrefs.SetInt(KeyPrefix + puzzleId, 1);
        PlayerPrefs.Save();
        Debug.Log($"Puzzle complete: {puzzleId}");
    }
}
