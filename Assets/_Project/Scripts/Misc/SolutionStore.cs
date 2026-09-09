using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SavedCoord
{
    public int q;
    public int r;
}

[Serializable]
public class SavedTile
{
    public int q;
    public int r;
    public string symbol;
    public int target;
}

[Serializable]
public class SavedInstruction
{
    public int processor;
    public int column;
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

[Serializable]
public class SavedSolution
{
    public string name;
    public string timestamp;
    public string puzzleID;
    public int processors;
    public int columns;
    public List<SavedTile> tiles = new List<SavedTile>();
    public List<SavedInstruction> instructions = new List<SavedInstruction>();
}

[Serializable]
public class SavedSolutionFile
{
    public List<SavedSolution> entries = new List<SavedSolution>();
}

public static class SolutionStore
{
    private static string DirectoryPath => Path.Combine(Application.persistentDataPath, "solutions");

    public static void Save(PuzzleData puzzle, BoardState board, GridController grid, string name = null)
    {
        if (puzzle == null || board == null || grid == null) return;

        try
        {
            SavedSolution solution = Capture(puzzle, board, grid);
            SavedSolutionFile file = LoadFile(puzzle.puzzleID);

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

            if (ContainsDuplicate(file, solution))
            {
                Debug.Log("SolutionStore: identical solution already saved - skipped.");
                return;
            }

            file.entries.Add(solution);

            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, FileName(puzzle.puzzleID)), JsonUtility.ToJson(file, true));
            Debug.Log($"Solution '{solution.name}' saved for '{puzzle.puzzleID}' ({solution.tiles.Count} tiles, {solution.instructions.Count} instructions).");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SolutionStore: failed to save solution - {e.Message}");
        }
    }

    public static List<SavedSolution> LoadAll(string puzzleID)
    {
        return LoadFile(puzzleID).entries;
    }

    public static SavedSolution LoadNewest(string puzzleID)
    {
        List<SavedSolution> entries = LoadFile(puzzleID).entries;
        return entries.Count > 0 ? entries[entries.Count - 1] : null;
    }

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

    private static SavedSolutionFile LoadFile(string puzzleID)
    {
        try
        {
            string path = Path.Combine(DirectoryPath, FileName(puzzleID));
            if (!File.Exists(path)) return new SavedSolutionFile();

            string json = File.ReadAllText(path);
            SavedSolutionFile file = JsonUtility.FromJson<SavedSolutionFile>(json);
            if (file != null && file.entries.Count > 0) return file ?? new SavedSolutionFile();

            // Legacy format: a single solution at the root. Migrate it into the list.
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
            Debug.LogWarning($"SolutionStore: failed to load solutions - {e.Message}");
        }

        return new SavedSolutionFile();
    }

    private static bool EntryNamed(SavedSolutionFile file, string name)
    {
        foreach (SavedSolution entry in file.entries)
        {
            if (entry.name == name) return true;
        }
        return false;
    }

    private static bool ContainsDuplicate(SavedSolutionFile file, SavedSolution candidate)
    {
        string candidateSignature = JsonUtility.ToJson(candidate.tiles) + JsonUtility.ToJson(candidate.instructions);
        foreach (SavedSolution entry in file.entries)
        {
            string signature = JsonUtility.ToJson(entry.tiles) + JsonUtility.ToJson(entry.instructions);
            if (signature == candidateSignature) return true;
        }
        return false;
    }

    private static SavedInstruction ToSavedInstruction(GridController.CellInstruction cellInstruction)
    {
        SavedInstruction saved = new SavedInstruction
        {
            processor = cellInstruction.Processor,
            column = cellInstruction.Column
        };

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

    private static string SymbolFor(TileData tile)
    {
        if (tile is NumberTileData number) return number.value.ToString();
        if (tile is OperationTileData op) return OpSymbol(op.operation);
        if (tile is FinalTileData) return "_";
        return null;
    }

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
