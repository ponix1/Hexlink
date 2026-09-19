using System.Collections.Generic;
using UnityEngine;

public class GameContentRegistry : ScriptableObject
{
    public List<WorldData> worlds = new List<WorldData>();

    public static GameContentRegistry Load()
    {
        return Resources.Load<GameContentRegistry>("GameContent");
    }
}
