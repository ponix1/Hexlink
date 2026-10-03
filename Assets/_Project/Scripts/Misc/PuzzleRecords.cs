using UnityEngine;

public static class PuzzleRecords
{
    private const string KeyPrefix = "Hexlink.Best.";
    private const int Unset = int.MaxValue;

    public readonly struct Result
    {
        public readonly int Instructions;
        public readonly int Cycles;
        public readonly int Processors;
        public readonly int Sum;
        public readonly int PrevInstructions;
        public readonly int PrevCycles;
        public readonly int PrevProcessors;
        public readonly int PrevSum;
        public readonly bool NewInstructions;
        public readonly bool NewCycles;
        public readonly bool NewProcessors;
        public readonly bool NewSum;

        public bool AnyImproved => NewInstructions || NewCycles || NewProcessors || NewSum;

        public Result(int instructions, int cycles, int processors, int sum,
                      int prevInstructions, int prevCycles, int prevProcessors, int prevSum,
                      bool newInstructions, bool newCycles, bool newProcessors, bool newSum)
        {
            Instructions = instructions;
            Cycles = cycles;
            Processors = processors;
            Sum = sum;
            PrevInstructions = prevInstructions;
            PrevCycles = prevCycles;
            PrevProcessors = prevProcessors;
            PrevSum = prevSum;
            NewInstructions = newInstructions;
            NewCycles = newCycles;
            NewProcessors = newProcessors;
            NewSum = newSum;
        }
    }

    public static void TryGet(string puzzleId, out int instructions, out int cycles, out int processors, out int sum)
    {
        instructions = PlayerPrefs.GetInt(Key(puzzleId, "Instr"), Unset);
        cycles = PlayerPrefs.GetInt(Key(puzzleId, "Cycles"), Unset);
        processors = PlayerPrefs.GetInt(Key(puzzleId, "Procs"), Unset);
        sum = PlayerPrefs.GetInt(Key(puzzleId, "Sum"), Unset);
    }

    public static Result Submit(string puzzleId, int instructions, int cycles, int processors)
    {
        int sum = instructions + cycles + processors;

        if (string.IsNullOrEmpty(puzzleId)) return default;

        string instrKey = Key(puzzleId, "Instr");
        string cyclesKey = Key(puzzleId, "Cycles");
        string procsKey = Key(puzzleId, "Procs");
        string sumKey = Key(puzzleId, "Sum");

        int prevInstructions = PlayerPrefs.GetInt(instrKey, Unset);
        int prevCycles = PlayerPrefs.GetInt(cyclesKey, Unset);
        int prevProcessors = PlayerPrefs.GetInt(procsKey, Unset);
        int prevSum = PlayerPrefs.GetInt(sumKey, Unset);

        bool newInstructions = Lower(instrKey, instructions);
        bool newCycles = Lower(cyclesKey, cycles);
        bool newProcessors = Lower(procsKey, processors);
        bool newSum = Lower(sumKey, sum);

        if (newInstructions || newCycles || newProcessors || newSum)
        {
            PlayerPrefs.Save();
            Debug.Log($"Records updated for '{puzzleId}' (instr {instructions}, cycles {cycles}, procs {processors}, sum {sum}).");
        }

        return new Result(instructions, cycles, processors, sum,
                          prevInstructions, prevCycles, prevProcessors, prevSum,
                          newInstructions, newCycles, newProcessors, newSum);
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
