using UnityEngine;
using System.Collections.Generic;

// Spawns the 3D hex grid from the selected puzzle (PuzzleData ScriptableObject's
// LayoutCells) and tags each instance with a HexTileIdentity so other systems can
// resolve a hex GameObject back to its HexCoord.
public class HexGridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject hexTilePrefab;
    // Point-to-point hex radius (half the hex width); drives both scale and spacing.
    [SerializeField] private float hexSize = 1f;
    // 1 = hexes touch; >1 adds a visible gap between them.
    [SerializeField] private float spacingMultiplier = 1f;

    // Coord → spawned hex instance; queried by HexTileLabelController to parent labels.
    private Dictionary<HexCoord, GameObject> spawnedTiles = new Dictionary<HexCoord, GameObject>();

    public IReadOnlyDictionary<HexCoord, GameObject> SpawnedTiles => spawnedTiles;
    public GameObject HexTilePrefab => hexTilePrefab;

    // Reads the puzzle chosen in the previous scene; expects it to be set before Start runs.
    private void Start()
    {
        PuzzleData puzzleData = PuzzleSelection.SelectedPuzzle;

        if (puzzleData == null)
        {
            Debug.LogError("HexGridSpawner: No puzzle was selected before this scene loaded.");
            return;
        }

        float scaleFactor = CalculateScaleFactor();
        Vector3 spawnScale = Vector3.one * scaleFactor;
        float spacing = hexSize * spacingMultiplier;

        foreach (HexCellData cell in puzzleData.LayoutCells)
        {
            SpawnTile(cell.coordinate, spawnScale, spacing);
        }
    }

    // Rescales the prefab so its point-to-point size matches 2 * hexSize.
    // max(x, y) tolerates the hex mesh being authored in either orientation.
    private float CalculateScaleFactor()
    {
        MeshFilter meshFilter = hexTilePrefab.GetComponentInChildren<MeshFilter>();
        Vector3 rawSize = meshFilter.sharedMesh.bounds.size;
        float rawPointToPoint = Mathf.Max(rawSize.x, rawSize.y);
        float desiredPointToPoint = 2f * hexSize;
        return desiredPointToPoint / rawPointToPoint;
    }

    private void SpawnTile(HexCoord coord, Vector3 scale, float spacing)
    {
        // Keep the prefab's authored rotation (not identity) so mesh orientation is preserved.
        GameObject tile = Instantiate(hexTilePrefab, coord.ToWorldPosition(spacing), hexTilePrefab.transform.rotation);
        tile.transform.localScale = scale;

        // Tag the instance with its coordinate so other systems can identify this hex.
        HexTileIdentity identity = tile.AddComponent<HexTileIdentity>();
        identity.coordinate = coord;

        spawnedTiles[coord] = tile;
    }

}