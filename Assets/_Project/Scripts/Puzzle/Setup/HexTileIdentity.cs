using UnityEngine;

// Tiny tag component: links a spawned hex GameObject back to its grid coordinate.
// Assigned by HexGridSpawner; read by systems that hit a hex (raycasts, etc.).
public class HexTileIdentity : MonoBehaviour
{
    // Grid position of this hex.
    public HexCoord coordinate;
}