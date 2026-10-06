// Core puzzle data assets. PuzzleData is the abstract ScriptableObject base
// for every puzzle type; HexCellData marks one cell in its board layout.
using UnityEngine;
using System.Collections.Generic;

// One authored board cell, identified by its axial hex coordinate.
[System.Serializable]
public class HexCellData
{
    public HexCoord coordinate;
}

// Designer-authored puzzle definition. A ScriptableObject so each puzzle is
// an asset edited in the Inspector, not runtime data.
public abstract class PuzzleData : ScriptableObject
{
    // Stable unique key: used for PlayerPrefs progress and solution filenames.
    public string puzzleID;
    // Display name shown in menus.
    public string puzzleTitle;

    // Goal text for the UI (e.g. StandardPuzzleData returns its target score).
    public abstract string GetDisplayTarget();

    // Authored board layout: which hex cells exist on this puzzle's grid.
    // Private + read-only exposure so runtime code can never mutate the asset.
    [SerializeField] private List<HexCellData> layoutCells;

    public IReadOnlyList<HexCellData> LayoutCells => layoutCells;

    // Level map layout: authored node position in a 1920x1080 space centred on
    // (0,0). Only used by campaign levels; world puzzles leave this unset.
    public bool hasMapPosition;
    public Vector2 mapPosition;
}