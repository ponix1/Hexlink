using UnityEngine;

[System.Serializable]
public struct HexCoord : System.IEquatable<HexCoord>
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

    public Vector3 ToWorldPosition(float hexSize)
    {
        float worldX = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) * 0.5f * r);
        float worldZ = hexSize * (1.5f * r);
        return new Vector3(worldX, 0f, worldZ);
    }

    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(q, r);
    }
}