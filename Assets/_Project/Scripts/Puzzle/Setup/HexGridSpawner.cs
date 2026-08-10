using UnityEngine;

public class HexGridSpawner : MonoBehaviour
{
    [SerializeField] private GameObject hexTilePrefab;
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private float spacingMultiplier = 1f;

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
        GameObject tile = Instantiate(hexTilePrefab, coord.ToWorldPosition(spacing), hexTilePrefab.transform.rotation);
        tile.transform.localScale = scale;

        HexTileIdentity identity = tile.AddComponent<HexTileIdentity>();
        identity.coordinate = coord;
    }
    
}