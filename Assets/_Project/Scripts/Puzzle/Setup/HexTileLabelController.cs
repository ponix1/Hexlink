using UnityEngine;
using System.Collections.Generic;

public class HexTileLabelController : MonoBehaviour
{
    [SerializeField] private HexGridSpawner hexGridSpawner;
    [SerializeField] private GameObject tileLabelPrefab;

    public BoardState boardState = new BoardState();

    private Dictionary<HexCoord, GameObject> activeLabels = new Dictionary<HexCoord, GameObject>();

    private void OnEnable()
    {
        boardState.OnCellChanged += HandleCellChanged;
    }

    private void OnDisable()
    {
        boardState.OnCellChanged -= HandleCellChanged;
    }

    private void HandleCellChanged(HexCoord coord)
    {
        if (activeLabels.TryGetValue(coord, out GameObject existingLabel))
        {
            Destroy(existingLabel);
            activeLabels.Remove(coord);
        }

        TileData tile = boardState.GetTile(coord);
        if (tile == null)
        {
            return;
        }

        if (!hexGridSpawner.SpawnedTiles.TryGetValue(coord, out GameObject hexTile))
        {
            return;
        }

        GameObject label = Instantiate(tileLabelPrefab, hexTile.transform);
        label.transform.localPosition = new Vector3(0f, 0f, 2.08f);

        TMPro.TextMeshPro textMesh = label.GetComponent<TMPro.TextMeshPro>();
        textMesh.text = tile.GetDisplayValue();

        activeLabels[coord] = label;
    }
}