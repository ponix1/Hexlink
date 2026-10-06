// Root content registry: every world and campaign level in one asset.
using System.Collections.Generic;
using UnityEngine;

// The single "GameContent" Resource asset that ties all content together.
public class GameContentRegistry : ScriptableObject
{
    // Worlds shown in world select.
    public List<WorldData> worlds = new List<WorldData>();
    // Linear campaign list; LevelProgress unlocks these in order.
    public List<PuzzleData> levels = new List<PuzzleData>();

    // Load the one registry asset from Resources/GameContent.
    public static GameContentRegistry Load()
    {
        return Resources.Load<GameContentRegistry>("GameContent");
    }
}
