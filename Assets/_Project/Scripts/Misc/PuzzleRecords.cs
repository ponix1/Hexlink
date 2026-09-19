using UnityEngine;

public static class PuzzleRecords
{
    private const string KeyPrefix = "Hexlink.Best.";
    private const int Unset = int.MaxValue;

    public static void Submit(string puzzleId, int instructions, int cycles, int processors)
    {
        if (string.IsNullOrEmpty(puzzleId)) return;

        bool improved = false;
        improved |= Lower(Key(puzzleId, "Instr"), instructions);
        improved |= Lower(Key(puzzleId, "Cycles"), cycles);
        improved |= Lower(Key(puzzleId, "Procs"), processors);

        if (improved)
        {
            PlayerPrefs.Save();
            Debug.Log($"Records updated for '{puzzleId}' (instr {instructions}, cycles {cycles}, procs {processors}).");
        }
    }

    public static void TryGet(string puzzleId, out int instructions, out int cycles, out int processors)
    {
        instructions = PlayerPrefs.GetInt(Key(puzzleId, "Instr"), Unset);
        cycles = PlayerPrefs.GetInt(Key(puzzleId, "Cycles"), Unset);
        processors = PlayerPrefs.GetInt(Key(puzzleId, "Procs"), Unset);
    }

    private static bool Lower(string key, int value)
    {
        if (PlayerPrefs.GetInt(key, Unset) <= value) return false;
        PlayerPrefs.SetInt(key, value);
        return true;
    }

    private static string Key(string puzzleId, string metric)
    {
        return KeyPrefix + metric + "." + puzzleId;
    }
}
