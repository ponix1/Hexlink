using System;
using System.Collections.Generic;

public class BoardState
{
    private Dictionary<HexCoord, TileData> tiles = new Dictionary<HexCoord, TileData>();

    public event Action<HexCoord> OnCellChanged;

    public bool HasTile(HexCoord coord)
    {
        return tiles.ContainsKey(coord);
    }

    public TileData GetTile(HexCoord coord)
    {
        tiles.TryGetValue(coord, out TileData tile);
        return tile;
    }

    public void PlaceTile(HexCoord coord, TileData tile)
    {
        tiles[coord] = tile;
        OnCellChanged?.Invoke(coord);
    }

    public void RemoveTile(HexCoord coord)
    {
        if (tiles.Remove(coord))
        {
            OnCellChanged?.Invoke(coord);
        }
    }
}