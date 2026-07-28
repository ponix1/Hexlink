using UnityEngine;

public abstract class PuzzleData : ScriptableObject
{
    public string puzzleID;
    public string puzzleTitle;

    public abstract string GetDisplayTarget();
}