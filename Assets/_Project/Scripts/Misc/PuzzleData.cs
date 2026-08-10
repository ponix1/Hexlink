using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class HexCellData
{
    public HexCoord coordinate;
}

public abstract class PuzzleData : ScriptableObject
{
    public string puzzleID;
    public string puzzleTitle;

    public abstract string GetDisplayTarget();

    [SerializeField] private List<HexCellData> layoutCells;

    public IReadOnlyList<HexCellData> LayoutCells => layoutCells;
}