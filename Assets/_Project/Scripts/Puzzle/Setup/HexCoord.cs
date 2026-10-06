using UnityEngine;

// Axial coords (q, r) for the pointed-top hex grid, plus the math for turning them
// into world positions. It's a struct implementing IEquatable so we can use it as a
// dictionary key without boxing. [Serializable] means puzzle assets can hold it too.
[System.Serializable]
public struct HexCoord : System.IEquatable<HexCoord>
{
    // q is the column, r the diagonal row.
    public int q;
    public int r;

    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    // The six neighbour offsets, same for every tile on the board.
    private static readonly HexCoord[] directions = new HexCoord[]
    {
        new HexCoord(1, 0), new HexCoord(1, -1), new HexCoord(0, -1),
        new HexCoord(-1, 0), new HexCoord(-1, 1), new HexCoord(0, 1)
    };

    // Neighbour in direction 0-5.
    public HexCoord GetNeighbor(int direction)
    {
        HexCoord d = directions[direction];
        return new HexCoord(q + d.q, r + d.r);
    }

    // Axial to world. Pointed-top layout, so each +1 in r nudges x by half a hex
    // width and moves z along by 1.5 * hexSize.
    public Vector3 ToWorldPosition(float hexSize)
    {
        float worldX = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) * 0.5f * r);
        float worldZ = hexSize * (1.5f * r);
        return new Vector3(worldX, 0f, worldZ);
    }

    // Equality + hash, so this works as a dictionary key.
    public bool Equals(HexCoord other)
    {
        return q == other.q && r == other.r;
    }

    // Boxed fallback for when something compares us as a plain object.
    public override bool Equals(object obj)
    {
        return obj is HexCoord other && Equals(other);
    }

    // Combine both fields so equal coords always hash the same.
    public override int GetHashCode()
    {
        return System.HashCode.Combine(q, r);
    }
}