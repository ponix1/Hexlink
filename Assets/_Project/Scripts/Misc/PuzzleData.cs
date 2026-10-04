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

    // Level map layout: authored node position in a 1920x1080 space centred on
    // (0,0). Only used by campaign levels; world puzzles leave this unset.
    public bool hasMapPosition;
    public Vector2 mapPosition;
}