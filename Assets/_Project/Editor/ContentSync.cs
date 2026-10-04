using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Keeps world/puzzle content in sync with the folder convention:
//   Assets/_Project/Assets/Worlds/World_X.asset        - one WorldData per world
//   Assets/_Project/Assets/Worlds/World_X_Puzzles/     - that world's puzzles
//   Assets/_Project/Assets/Levels/Level_N.asset        - campaign levels, in order
// Puzzle lists, level lists, puzzle IDs, titles and the world registry are derived
// from file names, so nothing about content ever needs to be wired up by hand.
public static class ContentSync
{
    private const string WorldsRoot = "Assets/_Project/Assets/Worlds";
    private const string LevelsRoot = "Assets/_Project/Assets/Levels";
    private const string RegistryPath = "Assets/Resources/GameContent.asset";

    [MenuItem("Hexlink/Sync World Content")]
    public static void SyncAll()
    {
        List<WorldData> worldOrder = new List<WorldData>();
        List<PuzzleData> levelOrder = new List<PuzzleData>();
        List<string> allPuzzlePaths = new List<string>();
        List<string> managedPuzzlePaths = new List<string>();
        int renamedCount = 0;
        int correctedCount = 0;
        int linkedCount = 0;

        string[] worldPaths = Array.FindAll(
            AssetDatabase.FindAssets("t:WorldData", new[] { WorldsRoot }),
            guid => Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guid)).Replace("\\", "/") == WorldsRoot);

        Array.Sort(worldPaths, (a, b) => NaturalCompare(
            Path.GetFileName(AssetDatabase.GUIDToAssetPath(a)),
            Path.GetFileName(AssetDatabase.GUIDToAssetPath(b))));

        foreach (string worldGuid in worldPaths)
        {
            string worldPath = AssetDatabase.GUIDToAssetPath(worldGuid);
            string worldKey = Path.GetFileNameWithoutExtension(worldPath);
            string suffix = worldKey.StartsWith("World_") ? worldKey.Substring("World_".Length) : worldKey;
            string puzzlesFolder = $"{WorldsRoot}/{worldKey}_Puzzles";

            WorldData world = AssetDatabase.LoadAssetAtPath<WorldData>(worldPath);
            if (world == null) continue;

            List<PuzzleData> puzzles = new List<PuzzleData>();
            if (Directory.Exists(puzzlesFolder))
            {
                string[] puzzleGuids = AssetDatabase.FindAssets("t:PuzzleData", new[] { puzzlesFolder });
                List<string> puzzlePaths = new List<string>();
                foreach (string puzzleGuid in puzzleGuids)
                {
                    puzzlePaths.Add(AssetDatabase.GUIDToAssetPath(puzzleGuid));
                }
                puzzlePaths.Sort(NaturalCompare);

                foreach (string puzzlePath in puzzlePaths)
                {
                    string desiredName = EnsureNumberedName(puzzlePath, puzzlesFolder, "Puzzle_", ref renamedCount);
                    if (desiredName == null) continue;

                    string renumberedPath = $"{puzzlesFolder}/{desiredName}.asset";
                    PuzzleData puzzle = AssetDatabase.LoadAssetAtPath<PuzzleData>(renumberedPath);
                    if (puzzle == null) continue;

                    int number = ParseTrailingNumber(desiredName);
                    string desiredID = $"W{suffix}-{number}";
                    string desiredTitle = $"{suffix}-{number}";

                    if (puzzle.puzzleID != desiredID || puzzle.puzzleTitle != desiredTitle)
                    {
                        if (!string.IsNullOrEmpty(puzzle.puzzleID) && puzzle.puzzleID != desiredID)
                        {
                            Debug.Log($"ContentSync: {desiredName} id '{puzzle.puzzleID}' -> '{desiredID}'.");
                        }
                        puzzle.puzzleID = desiredID;
                        puzzle.puzzleTitle = desiredTitle;
                        EditorUtility.SetDirty(puzzle);
                        correctedCount++;
                    }

                    puzzles.Add(puzzle);
                    managedPuzzlePaths.Add(renumberedPath);
                }
            }

            world.worldName = $"World {suffix}";
            world.puzzles = puzzles;
            EditorUtility.SetDirty(world);
            worldOrder.Add(world);
            linkedCount += puzzles.Count;

            Debug.Log($"ContentSync: {world.worldName} -> {puzzles.Count} puzzles.");
        }

        foreach (string puzzleGuid in AssetDatabase.FindAssets("t:PuzzleData", new[] { WorldsRoot }))
        {
            allPuzzlePaths.Add(AssetDatabase.GUIDToAssetPath(puzzleGuid));
        }
        foreach (string puzzlePath in allPuzzlePaths)
        {
            if (!managedPuzzlePaths.Contains(puzzlePath)
                && !puzzlePath.StartsWith(LevelsRoot))
            {
                Debug.LogWarning($"ContentSync: '{puzzlePath}' is not inside any World_X_Puzzles folder - it will not appear in the game.");
            }
        }

        levelOrder = SyncLevels(ref renamedCount, ref correctedCount);

        SaveRegistry(worldOrder, levelOrder);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"ContentSync done: {worldOrder.Count} worlds, {linkedCount} puzzles linked, {levelOrder.Count} levels linked, {renamedCount} renamed, {correctedCount} ids/titles corrected.");
    }

    // Scans the Levels folder and returns the campaign levels in file order,
    // keeping names (Level_N), ids (L-N) and titles (Level N) derived from the file name.
    private static List<PuzzleData> SyncLevels(ref int renamedCount, ref int correctedCount)
    {
        List<PuzzleData> levels = new List<PuzzleData>();

        if (!Directory.Exists(LevelsRoot)) return levels;

        string[] levelPaths = new List<string>(
            AssetDatabase.FindAssets("t:PuzzleData", new[] { LevelsRoot }))
            .ConvertAll(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();
        Array.Sort(levelPaths, NaturalCompare);

        foreach (string levelPath in levelPaths)
        {
            string folder = Path.GetDirectoryName(levelPath).Replace("\\", "/");
            string desiredName = EnsureNumberedName(levelPath, folder, "Level_", ref renamedCount);
            if (desiredName == null) continue;

            PuzzleData level = AssetDatabase.LoadAssetAtPath<PuzzleData>($"{folder}/{desiredName}.asset");
            if (level == null) continue;

            int number = ParseTrailingNumber(desiredName);
            string desiredID = $"L-{number}";
            string desiredTitle = $"Level {number}";

            if (level.puzzleID != desiredID || level.puzzleTitle != desiredTitle)
            {
                if (!string.IsNullOrEmpty(level.puzzleID) && level.puzzleID != desiredID)
                {
                    Debug.Log($"ContentSync: {desiredName} id '{level.puzzleID}' -> '{desiredID}'.");
                }
                level.puzzleID = desiredID;
                level.puzzleTitle = desiredTitle;
                EditorUtility.SetDirty(level);
                correctedCount++;
            }

            levels.Add(level);
        }

        return levels;
    }

    private static void SaveRegistry(List<WorldData> worldOrder, List<PuzzleData> levelOrder)
    {
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
        }

        GameContentRegistry registry = AssetDatabase.LoadAssetAtPath<GameContentRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<GameContentRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        registry.worlds = worldOrder;
        registry.levels = levelOrder;
        EditorUtility.SetDirty(registry);
    }

    // Returns the file name the puzzle asset should carry, renaming default-named
    // assets ("Puzzle_", "Puzzle", "Level_", "Level") to the next free number in
    // the folder. Returns null when the asset at that path is not loadable as a puzzle.
    private static string EnsureNumberedName(string puzzlePath, string puzzlesFolder, string prefix, ref int renamedCount)
    {
        string fileName = Path.GetFileNameWithoutExtension(puzzlePath);
        if (ParseTrailingNumber(fileName) > 0) return fileName;

        HashSet<int> used = new HashSet<int>();
        foreach (string guid in AssetDatabase.FindAssets("t:PuzzleData", new[] { puzzlesFolder }))
        {
            int existing = ParseTrailingNumber(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            if (existing > 0) used.Add(existing);
        }

        int next = 1;
        while (used.Contains(next)) next++;

        string newName = $"{prefix}{next}";
        string error = AssetDatabase.RenameAsset(puzzlePath, newName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"ContentSync: could not rename '{puzzlePath}' to '{newName}': {error}");
            return null;
        }
        renamedCount++;

        Debug.Log($"ContentSync: renamed '{fileName}' -> '{newName}'.");
        return newName;
    }

    private static int ParseTrailingNumber(string fileName)
    {
        Match match = Regex.Match(fileName, @"(\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }

    // Orders names with embedded numbers the way humans expect:
    // Puzzle_2 before Puzzle_10, World_A before World_B before World_10.
    private static int NaturalCompare(string a, string b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        int ia = 0;
        int ib = 0;
        while (ia < a.Length && ib < b.Length)
        {
            if (char.IsDigit(a[ia]) && char.IsDigit(b[ib]))
            {
                int ja = ia;
                while (ja < a.Length && char.IsDigit(a[ja])) ja++;
                int jb = ib;
                while (jb < b.Length && char.IsDigit(b[jb])) jb++;

                long na = long.Parse(a.Substring(ia, ja - ia));
                long nb = long.Parse(b.Substring(ib, jb - ib));
                if (na != nb) return na.CompareTo(nb);

                ia = ja;
                ib = jb;
            }
            else
            {
                int result = string.Compare(a[ia].ToString(), b[ib].ToString(), StringComparison.Ordinal);
                if (result != 0) return result;
                ia++;
                ib++;
            }
        }

        return (a.Length - ia).CompareTo(b.Length - ib);
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (TouchesContent(importedAssets) || TouchesContent(deletedAssets) || TouchesContent(movedAssets) || TouchesContent(movedFromAssetPaths))
        {
            EditorApplication.delayCall += SyncAll;
        }
    }

    private static bool TouchesContent(string[] paths)
    {
        if (paths == null) return false;
        foreach (string path in paths)
        {
            if (!string.IsNullOrEmpty(path)
                && (path.StartsWith(WorldsRoot) || path.StartsWith(LevelsRoot)))
            {
                return true;
            }
        }
        return false;
    }
}
