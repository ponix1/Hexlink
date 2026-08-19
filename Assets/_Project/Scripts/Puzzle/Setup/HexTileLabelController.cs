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

        GameObject placedTile = Instantiate(tileLabelPrefab, hexTile.transform);
        placedTile.transform.localPosition = new Vector3(0f, 0f, 1.99f);
        placedTile.transform.rotation = Quaternion.Euler(-90f, 180f, 0f);

        Extruded3DText labelText = placedTile.GetComponentInChildren<Extruded3DText>();
        if (labelText != null)
        {
            labelText.SetText(tile.GetDisplayValue());
        }

        activeLabels[coord] = placedTile;
    }
}