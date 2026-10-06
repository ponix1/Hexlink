// Saves solutions to disk as JSON, one file per puzzle under
// persistentDataPath/solutions. Saving only ever adds an entry — nothing gets
// touched unless the player explicitly renames/updates/deletes it. All the IO is
// wrapped in try/catch and goes through a temp file, so a crash mid-save can't
// wreck what's already there.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// A hex coordinate as it appears in the save file.
[Serializable]
public class SavedCoord
{
    public int q;
    public int r;
}

// One tile on the saved board.
[Serializable]
public class SavedTile
{
    public int q;
    public int r;
    // The tile as a symbol string — "5", "+", "_" etc. Keeping tiles as text makes
    // the JSON small and readable, and TileDataFactory can rebuild any tile from it.
    public string symbol;
    // Target number for final tiles. Ignore unless symbol is "_".
    public int target;
}

// Instructions get flattened into this because JsonUtility can't do inheritance —
// every possible field sits side by side and the "type" string says which matter.
[Serializable]
public class SavedInstruction
{
    public int processor;
    public int column;
    // "move", "select" or "op".
    public string type;
    public int sourceQ;
    public int sourceR;
    public int destQ;
    public int destR;
    public int opQ;
    public int opR;
    public int operation;
    public List<SavedCoord> operands = new List<SavedCoord>();
}

// A whole solution: the board tiles plus the program.
[Serializable]
public class SavedSolution
{
    // Player-facing name, unique within the file.
    public string name;
    // When it was last saved/renamed, for display only.
    public string timestamp;
    // Which puzzle this belongs to (kept in the entry too, so a single
    // entry is self-describing outside its file).
    public string puzzleID;
    // Set once this entry has actually won, so the UI can badge it.
    public bool solved;
    // How big the grid was, so loading can rebuild the same layout.
    public int processors;
    public int columns;
    public List<SavedTile> tiles = new List<SavedTile>();
    public List<SavedInstruction> instructions = new List<SavedInstruction>();
}

// Top level of the file. A list rather than a single solution, so multiple saves
// can stack up and adding a new one never touches the old ones.
[Serializable]
public class SavedSolutionFile
{
    public List<SavedSolution> entries = new List<SavedSolution>();
}

// Does all the file IO. Everything sits in try/catch — if a save or load goes wrong
// we log it and carry on rather than crashing or leaving a half-written file.
public static class SolutionStore
{
    // Magic values for PendingIndex: nothing pending vs. a save-as-new pending.
    public const int PendingNone = -2;
    public const int PendingNew = -1;

    // Set by the save UI so popups know which entry we're acting on.
    public static int PendingIndex = PendingNone;

    // Everything goes in one folder, one json per puzzle.
    private static string DirectoryPath => Path.Combine(Application.persistentDataPath, "solutions");

    // Grab the current board + program and save them as a new entry.
    public static SavedSolution Save(PuzzleData puzzle, BoardState board, GridController grid, string name = null)
    {
        if (puzzle == null || board == null || grid == null) return null;

        try
        {
            SavedSolution solution = Capture(puzzle, board, grid);
            SavedSolutionFile file = LoadFile(puzzle.puzzleID);
            // Note we re-read the file rather than caching it — the engine can
            // save (on a win) without this class knowing, so the file is the
            // only reliable source of truth.

            // Default name is "Solution N" — bump N until we hit one that isn't taken.
            if (string.IsNullOrEmpty(name))
            {
                int number = file.entries.Count + 1;
                name = $"Solution {number}";
                while (EntryNamed(file, name))
                {
                    number++;
                    name = $"Solution {number}";
                }
            }
            solution.name = name;
            solution.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            file.entries.Add(solution);
            WriteFile(puzzle.puzzleID, file);
            Debug.Log($"Solution '{solution.name}' saved for '{puzzle.puzzleID}' ({solution.tiles.Count} tiles, {solution.instructions.Count} instructions).");
            return solution;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to save solution - {e.Message}");
            return null;
        }
    }

    // Overwrite an entry in place. Keeps the old name and solved flag.
    public static bool UpdateEntry(string puzzleID, int index, SavedSolution solution)
    {
        if (string.IsNullOrEmpty(puzzleID) || solution == null) return false;

        try
        {
            SavedSolutionFile file = LoadFile(puzzleID);
            if (index < 0 || index >= file.entries.Count) return false;

            solution.name = file.entries[index].name;
            solution.solved = file.entries[index].solved;
            solution.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            file.entries[index] = solution;
            WriteFile(puzzleID, file);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to update solution - {e.Message}");
            return false;
        }
    }

    // Rename. Blank or duplicate names get rejected.
    public static bool Rename(string puzzleID, int index, string newName)
    {
        if (string.IsNullOrEmpty(puzzleID) || string.IsNullOrWhiteSpace(newName)) return false;

        try
        {
            SavedSolutionFile file = LoadFile(puzzleID);
            if (index < 0 || index >= file.entries.Count) return false;

            string name = newName.Trim();
            if (name.Length == 0) return false;
            for (int i = 0; i < file.entries.Count; i++)
            {
                if (i != index && file.entries[i].name == name) return false;
            }

            file.entries[index].name = name;
            file.entries[index].timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            WriteFile(puzzleID, file);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to rename solution - {e.Message}");
            return false;
        }
    }

    // Delete one entry by index.
    public static bool Delete(string puzzleID, int index)
    {
        if (string.IsNullOrEmpty(puzzleID)) return false;

        try
        {
            SavedSolutionFile file = LoadFile(puzzleID);
            if (index < 0 || index >= file.entries.Count) return false;

            file.entries.RemoveAt(index);
            WriteFile(puzzleID, file);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to delete solution - {e.Message}");
            return false;
        }
    }

    // Mark solved. Already solved still counts as success so callers don't double-handle it.
    public static bool MarkSolved(string puzzleID, int index)
    {
        if (string.IsNullOrEmpty(puzzleID)) return false;

        try
        {
            SavedSolutionFile file = LoadFile(puzzleID);
            if (index < 0 || index >= file.entries.Count) return false;
            if (file.entries[index].solved) return true;

            file.entries[index].solved = true;
            WriteFile(puzzleID, file);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to mark solution solved - {e.Message}");
            return false;
        }
    }

    // Work out the score numbers from the instruction list.
    public static void ComputeMetrics(SavedSolution solution, out int instructions, out int cycles, out int processors, out int sum)
    {
        instructions = 0;
        cycles = 0;
        processors = 0;

        if (solution == null || solution.instructions == null)
        {
            sum = 0;
            return;
        }

        HashSet<int> usedProcessors = new HashSet<int>();
        int maxColumn = -1;
        foreach (SavedInstruction saved in solution.instructions)
        {
            if (string.IsNullOrEmpty(saved.type)) continue;

            instructions++;
            usedProcessors.Add(saved.processor);
            if (saved.column > maxColumn) maxColumn = saved.column;
        }

        // Cycles is the deepest column + 1, processors is how many rows got used,
        // and the sum is just all three added together.
        cycles = maxColumn + 1;
        processors = usedProcessors.Count;
        sum = instructions + cycles + processors;
    }

    // Every entry saved for this puzzle. Empty list if there are none.
    public static List<SavedSolution> LoadAll(string puzzleID)
    {
        return LoadFile(puzzleID).entries;
    }

    // Newest entry (we always append to the end), or null.
    public static SavedSolution LoadNewest(string puzzleID)
    {
        List<SavedSolution> entries = LoadFile(puzzleID).entries;
        return entries.Count > 0 ? entries[entries.Count - 1] : null;
    }

    // Snapshots the live board and program exactly as they are right now.
    public static SavedSolution Capture(PuzzleData puzzle, BoardState board, GridController grid)
    {
        SavedSolution solution = new SavedSolution
        {
            puzzleID = puzzle != null ? puzzle.puzzleID : "",
            processors = grid != null ? grid.ProcessorCount : 1,
            columns = grid != null ? grid.ColumnCount : 1
        };

        if (board != null)
        {
            foreach (KeyValuePair<HexCoord, TileData> entry in board.AllTiles)
            {
                // Unknown tile type — skip it rather than fail the whole save.
                string symbol = SymbolFor(entry.Value);
                if (symbol == null) continue;

                SavedTile saved = new SavedTile { q = entry.Key.q, r = entry.Key.r, symbol = symbol };
                if (entry.Value is FinalTileData final)
                {
                    saved.target = final.targetNumber;
                }
                solution.tiles.Add(saved);
            }
        }

        // Walk the columns in order so instructions come out in program order.
        if (grid != null)
        {
            for (int column = 0; column < grid.ColumnCount; column++)
            {
                foreach (GridController.CellInstruction cellInstruction in grid.GetColumnInstructions(column))
                {
                    solution.instructions.Add(ToSavedInstruction(cellInstruction));
                }
            }
        }

        return solution;
    }

    // Reads the file for a puzzle. A missing or broken file just means "no
    // solutions yet" — callers never have to deal with exceptions here.
    private static SavedSolutionFile LoadFile(string puzzleID)
    {
        try
        {
            string path = Path.Combine(DirectoryPath, FileName(puzzleID));
            if (!File.Exists(path)) return new SavedSolutionFile();

            string json = File.ReadAllText(path);
            // Try the current list format first.
            SavedSolutionFile file = JsonUtility.FromJson<SavedSolutionFile>(json);
            if (file != null && file.entries.Count > 0) return file ?? new SavedSolutionFile();

            // Old save format had a single solution at the root. Fold it into a list.
            SavedSolution legacy = JsonUtility.FromJson<SavedSolution>(json);
            if (legacy != null && (legacy.tiles.Count > 0 || legacy.instructions.Count > 0))
            {
                legacy.name = "Solution 1";
                file = new SavedSolutionFile();
                file.entries.Add(legacy);
                return file;
            }
        }
        catch (Exception e)
        {
            // File is corrupt. Park it as '.bad' instead of deleting — never throw
            // away someone's data over a parse error.
            Quarantine(puzzleID);
            Debug.LogWarning($"SolutionStore: solutions file was unreadable and has been set aside as '.bad' - {e.Message}");
        }

        return new SavedSolutionFile();
    }

    // Write to a temp file first, then swap it in. If the game dies partway
    // through a write, the real file is still intact.
    private static void WriteFile(string puzzleID, SavedSolutionFile file)
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, FileName(puzzleID));
        string tempPath = path + ".tmp";

        // Write + swap. File.Replace keeps the old file alive until the new
        // one is fully in place.
        File.WriteAllText(tempPath, JsonUtility.ToJson(file, true));
        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    // Park an unreadable file as '.bad' so it isn't lost.
    private static void Quarantine(string puzzleID)
    {
        try
        {
            string path = Path.Combine(DirectoryPath, FileName(puzzleID));
            if (!File.Exists(path)) return;

            string badPath = path + ".bad";
            if (File.Exists(badPath)) File.Delete(badPath);
            File.Move(path, badPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: could not set aside unreadable file - {e.Message}");
        }
    }

    // Used by the auto-namer to check for name clashes.
    private static bool EntryNamed(SavedSolutionFile file, string name)
    {
        foreach (SavedSolution entry in file.entries)
        {
            if (entry.name == name) return true;
        }
        return false;
    }

    // InstructionData to the flat save record.
    private static SavedInstruction ToSavedInstruction(GridController.CellInstruction cellInstruction)
    {
        SavedInstruction saved = new SavedInstruction
        {
            processor = cellInstruction.Processor,
            column = cellInstruction.Column
        };

        // Only the fields the type actually uses get filled in; the rest stay
        // at defaults and are ignored on load.
        if (cellInstruction.Instruction is MoveInstructionData move)
        {
            saved.type = "move";
            saved.sourceQ = move.Source.q;
            saved.sourceR = move.Source.r;
            saved.destQ = move.Destination.q;
            saved.destR = move.Destination.r;
        }
        else if (cellInstruction.Instruction is SelectInstructionData select)
        {
            saved.type = "select";
            saved.sourceQ = select.Source.q;
            saved.sourceR = select.Source.r;
        }
        else if (cellInstruction.Instruction is OperationInstructionData op)
        {
            saved.type = "op";
            saved.sourceQ = op.Source.q;
            saved.sourceR = op.Source.r;
            saved.opQ = op.OperationTile.q;
            saved.opR = op.OperationTile.r;
            saved.operation = (int)op.Operation;
            foreach (HexCoord operand in op.AdditionalOperands)
            {
                saved.operands.Add(new SavedCoord { q = operand.q, r = operand.r });
            }
        }

        return saved;
    }

    // Save record back into a real InstructionData. Null if the type string is junk.
    public static InstructionData ToInstructionData(SavedInstruction saved)
    {
        switch (saved.type)
        {
            case "move":
                return new MoveInstructionData
                {
                    Source = new HexCoord(saved.sourceQ, saved.sourceR),
                    Destination = new HexCoord(saved.destQ, saved.destR)
                };

            case "select":
                return new SelectInstructionData
                {
                    Source = new HexCoord(saved.sourceQ, saved.sourceR)
                };

            case "op":
                OperationInstructionData op = new OperationInstructionData
                {
                    Source = new HexCoord(saved.sourceQ, saved.sourceR),
                    OperationTile = new HexCoord(saved.opQ, saved.opR),
                    Operation = (OperationTileData.OperationType)saved.operation
                };
                foreach (SavedCoord operand in saved.operands)
                {
                    op.AdditionalOperands.Add(new HexCoord(operand.q, operand.r));
                }
                return op;
        }

        return null;
    }

    // Tile to its symbol string ("5", "+", "_"...). Null if we don't recognise it.
    private static string SymbolFor(TileData tile)
    {
        if (tile is NumberTileData number) return number.value.ToString();
        if (tile is OperationTileData op) return OpSymbol(op.operation);
        if (tile is FinalTileData) return "_";
        return null;
    }

    // Operation enum to its symbol. \u221A is the sqrt radical character.
    private static string OpSymbol(OperationTileData.OperationType operation)
    {
        switch (operation)
        {
            case OperationTileData.OperationType.Add: return "+";
            case OperationTileData.OperationType.Subtract: return "-";
            case OperationTileData.OperationType.Multiply: return "*";
            case OperationTileData.OperationType.Divide: return "/";
            case OperationTileData.OperationType.Power: return "^";
            case OperationTileData.OperationType.Factorial: return "!";
            case OperationTileData.OperationType.SquareRoot: return "\u221A";
            default: return null;
        }
    }

    // puzzleID to a safe filename — anything Windows hates becomes an underscore.
    private static string FileName(string puzzleID)
    {
        if (string.IsNullOrEmpty(puzzleID)) puzzleID = "unnamed";
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] safe = puzzleID.ToCharArray();
        for (int i = 0; i < safe.Length; i++)
        {
            if (Array.IndexOf(invalid, safe[i]) >= 0) safe[i] = '_';
        }
        return new string(safe) + ".json";
    }
}
