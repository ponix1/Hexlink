using UnityEngine;
using UnityEngine.EventSystems;

public class HexTileSelector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private TileInventoryUI inventoryUI;
    [SerializeField] private HexTileLabelController labelController;
    [SerializeField] private HexGridSpawner hexGridSpawner;
    [SerializeField] private InstructionAuthoringController authoringController;

    // Reached through the authoring controller (already wired in existing scenes)
    // so this logic never depends on a fresh InfoTab rebuild.
    private GridController GridController => authoringController != null ? authoringController.GridController : null;

    public event System.Action<HexCoord> OnTileClicked;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock propertyBlock;
    private MeshRenderer hoveredRenderer;
    private bool uiHighlightActive;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Keyboard-submit would re-trigger the last clicked UI button when the player
        // presses Space/Enter for instruction authoring — disable navigation entirely.
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = false;
        }
    }

    private void Update()
    {
        if (authoringController != null && authoringController.IsBusy
            && inventoryUI != null && !string.IsNullOrEmpty(inventoryUI.CurrentSelectedTile))
        {
            authoringController.CancelPending();
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (!uiHighlightActive) ClearHover();
            return;
        }

        UpdateHover();
        CheckForClick();
    }

    private void UpdateHover()
    {
        if (uiHighlightActive) return;

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
        if (Input.GetMouseButtonDown(0) && hoveredRenderer == null)
        {
            if (authoringController != null)
            {
                authoringController.ExitInputMode();
            }
            return;
        }

        if (hoveredRenderer == null)
        {
            return;
        }

        HexTileIdentity identity = hoveredRenderer.GetComponentInParent<HexTileIdentity>();

        if (Input.GetMouseButtonDown(2))
        {
            if (identity != null && labelController.boardState.HasTile(identity.coordinate))
            {
                labelController.boardState.RemoveTile(identity.coordinate);
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (identity == null) return;
            if (Input.GetMouseButton(1)) return;

            bool authoringBusy = authoringController != null && authoringController.IsBusy;
            string tileToPlace = inventoryUI != null ? inventoryUI.CurrentSelectedTile : "";
            if (!authoringBusy && !string.IsNullOrEmpty(tileToPlace))
            {
                TileData tileData = TileDataFactory.CreateFromSymbol(tileToPlace);
                if (tileData == null) return;

                TileData existing = labelController.boardState.GetTile(identity.coordinate);
                GridController grid = GridController;

                if (existing != null && grid != null && grid.HasInstructionsReferencing(identity.coordinate))
                {
                    if (grid.IsPendingChange(identity.coordinate))
                    {
                        grid.ClearPendingChange();
                        Debug.Log("Change confirmed - instructions updated.");
                        labelController.boardState.PlaceTile(identity.coordinate, tileData);
                    }
                    else
                    {
                        grid.SetPendingChange(identity.coordinate);
                    }
                    return;
                }

                if (grid != null) grid.ClearPendingChange();
                labelController.boardState.PlaceTile(identity.coordinate, tileData);
            }
            else
            {
                OnTileClicked?.Invoke(identity.coordinate);
            }
        }
    }

    public void HighlightTile(HexCoord coord)
    {
        if (hexGridSpawner == null) return;
        if (!hexGridSpawner.SpawnedTiles.TryGetValue(coord, out GameObject hexTile)) return;

        MeshRenderer renderer = hexTile.GetComponentInChildren<MeshRenderer>();
        if (renderer == null) return;

        if (renderer != hoveredRenderer) ClearHover();
        SetHover(renderer);
        uiHighlightActive = true;
    }

    public void ClearTileHighlight()
    {
        uiHighlightActive = false;
        ClearHover();
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