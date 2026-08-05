using UnityEngine;

public class HexGridSanityTest : MonoBehaviour
{
    [SerializeField] private GameObject hexTilePrefab;
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private float spacingMultiplier = 1f;

    private void Start()
    {
        float scaleFactor = CalculateScaleFactor();
        Vector3 spawnScale = Vector3.one * scaleFactor;
        float spacing = hexSize * spacingMultiplier;

        HexCoord center = new HexCoord(0, 0);
        SpawnTile(center, spawnScale, spacing);

        for (int direction = 0; direction < 6; direction++)
        {
            HexCoord neighbor = center.GetNeighbor(direction);
            SpawnTile(neighbor, spawnScale, spacing);
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
    }
}