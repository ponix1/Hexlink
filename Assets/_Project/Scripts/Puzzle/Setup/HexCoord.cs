using UnityEngine;

[System.Serializable]
public struct HexCoord
{
    public int q;
    public int r;

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    // The six fixed neighbour offsets from the diagram — same everywhere on the board
    private static readonly HexCoord[] directions = new HexCoord[]
    {
        new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
        new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1)
    };

    public HexCoord GetNeighbor(int direction)
    {
        HexCoord d = directions[direction];
        return new HexCoord(q + d.q, r + d.r);
    }
}