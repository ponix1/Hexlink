using UnityEngine;

public class HexTileSelector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private TileInventoryUI inventoryUI;
    [SerializeField] private HexTileLabelController labelController;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock propertyBlock;
    private MeshRenderer hoveredRenderer;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        UpdateHover();
        CheckForClick();
    }

    private void UpdateHover()
    {
        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HexTileIdentity identity = hit.collider.GetComponentInParent<HexTileIdentity>();
            if (identity != null)
            {
                MeshRenderer renderer = identity.GetComponentInChildren<MeshRenderer>();
                if (renderer != hoveredRenderer)
                {
                    ClearHover();
                    SetHover(renderer);
                }
                return;
            }
        }

        ClearHover();
    }

    private void CheckForClick()
    {
        if (Input.GetMouseButtonDown(0) && hoveredRenderer != null)
        {
            HexTileIdentity identity = hoveredRenderer.GetComponentInParent<HexTileIdentity>();
            string tileToPlace = inventoryUI.CurrentSelectedTile;

            if (identity != null && !string.IsNullOrEmpty(tileToPlace))
            {
                TileData tileData = TileDataFactory.CreateFromSymbol(tileToPlace);
                if (tileData != null)
                {
                    labelController.boardState.PlaceTile(identity.coordinate, tileData);
                }
            }
        }
    }

    private void SetHover(MeshRenderer renderer)
    {
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorID, highlightColor);
        renderer.SetPropertyBlock(propertyBlock);
        hoveredRenderer = renderer;
    }

    private void ClearHover()
    {
        if (hoveredRenderer != null)
        {
            propertyBlock.Clear();
            hoveredRenderer.SetPropertyBlock(propertyBlock);
            hoveredRenderer = null;
        }
    }
}