using System;
using System.Collections.Generic;

// Single source of truth for which tile (TileData) sits on which hex (HexCoord).
// Plain C# class, deliberately UI-free: visuals (HexTileLabelController, GridController)
// subscribe to OnCellChanged and update themselves reactively.
public class BoardState
{
    // HexCoord is a struct implementing IEquatable, so dictionary ops don't box/allocate.
    private Dictionary<HexCoord, TileData> tiles = new Dictionary<HexCoord, TileData>();

    // Fired with the affected cell after every place/remove; UI listens to stay in sync.
    public event Action<HexCoord> OnCellChanged;

    // Read-only view for queries; mutation happens only via PlaceTile/RemoveTile.
    public IReadOnlyDictionary<HexCoord, TileData> AllTiles => tiles;

    public bool HasTile(HexCoord coord)
    {
        return tiles.ContainsKey(coord);
    }

    // Returns null if the cell is empty.
    public TileData GetTile(HexCoord coord)
    {
        tiles.TryGetValue(coord, out TileData tile);
        return tile;
    }

    // Overwrites any existing tile at coord and notifies listeners.
    public void PlaceTile(HexCoord coord, TileData tile)
    {
        tiles[coord] = tile;
        OnCellChanged?.Invoke(coord);
    }

    // Notifies listeners only if a tile was actually removed.
    public void RemoveTile(HexCoord coord)
    {
        if (tiles.Remove(coord))
        {
            OnCellChanged?.Invoke(coord);
        }
    }
}