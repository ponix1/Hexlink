using UnityEngine;

public static class PuzzleProgress
{
    private const string KeyPrefix = "Hexlink.Complete.";

    public static bool IsComplete(string puzzleId)
    {
        return !string.IsNullOrEmpty(puzzleId) && PlayerPrefs.GetInt(KeyPrefix + puzzleId, 0) == 1;
    }

    public static void MarkComplete(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId) || IsComplete(puzzleId)) return;

        PlayerPrefs.SetInt(KeyPrefix + puzzleId, 1);
        PlayerPrefs.Save();
        Debug.Log($"Puzzle complete: {puzzleId}");
    }
}
