using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Visual half of BoardState: reacts to OnCellChanged by spawning/removing the
// "placed tile" prefab (hexagon mesh + Extruded3DText label) on the matching hex,
// and tinting operation/final tiles. Number tiles keep the prefab's default look.
public class HexTileLabelController : MonoBehaviour
{
    [SerializeField] private HexGridSpawner hexGridSpawner;
    // Prefab instantiated on a hex when a tile is placed (hexagon mesh + 3D text).
    [SerializeField] private GameObject tileLabelPrefab;
    [SerializeField] private Color operationColor = new Color(0.635f, 0.843f, 0.890f); // A2D7E3
    [SerializeField] private Color finalTileColor = new Color(0.522f, 0.522f, 0.522f); // 858585

    // This scene's board state — the single source of truth for tile placement.
    public BoardState boardState = new BoardState();

    // URP shaders use _BaseColor; built-in's _Color would silently do nothing.
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    // One label instance per occupied cell, so it can be destroyed when the cell changes.
    private Dictionary<HexCoord, GameObject> activeLabels = new Dictionary<HexCoord, GameObject>();
    // Lets us tint instances without duplicating materials.
    private MaterialPropertyBlock colorBlock;

    private void Awake()
    {
        colorBlock = new MaterialPropertyBlock();
    }

    // Subscribe on enable so the view stays reactively in sync with the board.
    private void OnEnable()
    {
        boardState.OnCellChanged += HandleCellChanged;
    }

    private void OnDisable()
    {
        boardState.OnCellChanged -= HandleCellChanged;
    }

    // Full destroy-then-respawn per change: a replaced tile rebuilds its visual cleanly.
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
        // Sit just above the hex face to avoid z-fighting.
        placedTile.transform.localPosition = new Vector3(0f, 0f, 1.99f);
        // Deliberately overwrites the instantiated prefab's rotation — the label
        // must lie flat (-90 X) in world space regardless of prefab authoring.
        placedTile.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        Extruded3DText labelText = placedTile.GetComponentInChildren<Extruded3DText>();
        if (labelText != null)
        {
            labelText.SetText(tile.GetDisplayValue());
        }

        // Tint by tile subtype; number tiles are left at the prefab's default colors.
        if (tile is OperationTileData)
        {
            ApplyTileColor(placedTile, operationColor);
        }
        else if (tile is FinalTileData)
        {
            ApplyTileColor(placedTile, finalTileColor);
        }

        activeLabels[coord] = placedTile;
    }

    // Tint every mesh renderer except the TMP layers (text keeps its own colors).
    private void ApplyTileColor(GameObject placedTile, Color color)
    {
        foreach (MeshRenderer renderer in placedTile.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.GetComponent<TextMeshPro>() != null)
            {
                continue;
            }

            renderer.GetPropertyBlock(colorBlock);
            colorBlock.SetColor(BaseColorID, color);
            renderer.SetPropertyBlock(colorBlock);
        }
    }
}