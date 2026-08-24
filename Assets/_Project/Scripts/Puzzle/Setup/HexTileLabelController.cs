using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HexTileLabelController : MonoBehaviour
{
    [SerializeField] private HexGridSpawner hexGridSpawner;
    [SerializeField] private GameObject tileLabelPrefab;
    [SerializeField] private Color operationColor = new Color(0.635f, 0.843f, 0.890f); // A2D7E3

    public BoardState boardState = new BoardState();

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private Dictionary<HexCoord, GameObject> activeLabels = new Dictionary<HexCoord, GameObject>();
    private MaterialPropertyBlock colorBlock;

    private void Awake()
    {
        colorBlock = new MaterialPropertyBlock();
    }

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
        placedTile.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        Extruded3DText labelText = placedTile.GetComponentInChildren<Extruded3DText>();
        if (labelText != null)
        {
            labelText.SetText(tile.GetDisplayValue());
        }

        if (tile is OperationTileData)
        {
            ApplyOperationColor(placedTile);
        }

        activeLabels[coord] = placedTile;
    }

    private void ApplyOperationColor(GameObject placedTile)
    {
        foreach (MeshRenderer renderer in placedTile.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.GetComponent<TextMeshPro>() != null)
            {
                continue;
            }

            renderer.GetPropertyBlock(colorBlock);
            colorBlock.SetColor(BaseColorID, operationColor);
            renderer.SetPropertyBlock(colorBlock);
        }
    }
}