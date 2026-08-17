using UnityEngine;

public class HexTileSelector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private TileInventoryUI inventoryUI;

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
                // Now you have both the Hex location AND the Tile to place!
                Debug.Log($"Placing '{tileToPlace}' at hex: {identity.gameObject.name}");
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