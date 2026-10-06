// TileInventoryUI - palette model for the tile palette: tracks which tile symbol
// (TileDataFactory format) is armed for placement and gates button availability.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Selection state only; buttons call SelectTile and the engine listens to
// OnTileSelected (used to auto-reset a running program).
public class TileInventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    // Panel holding the Tile_* buttons, shown/hidden by TogglePanel.
    [SerializeField] private GameObject collapsiblePanel;
    [SerializeField] private TextMeshProUGUI currentSelectionText;

    // This holds the data for what we want to place (e.g., "7", "+", "_")
    public string CurrentSelectedTile { get; private set; } = "";

    // Fired when a new symbol is armed; engine uses it for auto-reset.
    public event System.Action OnTileSelected;

    private bool isExpanded = true;

    private void Start()
    {
        RefreshPaletteAvailability();
    }

    // Escape or right-click (without left held) deselects the armed tile.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetMouseButtonDown(1) && !Input.GetMouseButton(0)))
        {
            DeselectTile();
        }
    }

    // Hook this up to a "Toggle Menu" button
    public void TogglePanel()
    {
        isExpanded = !isExpanded;
        collapsiblePanel.SetActive(isExpanded);
    }

    // Hook this up to all your individual tile buttons
    // Arms a symbol (if available); re-clicking the same one toggles it off.
    public void SelectTile(string tileValue)
    {
        if (!IsTileAvailable(NormalizeSymbol(tileValue))) return;

        if (tileValue == CurrentSelectedTile)
        {
            DeselectTile();
            return;
        }

        CurrentSelectedTile = tileValue;

        if (currentSelectionText != null)
        {
            currentSelectionText.text = $"Selected: {tileValue}";
        }

        OnTileSelected?.Invoke();
        Debug.Log($"Inventory updated. Ready to place: {tileValue}");
    }

    // Clears the armed symbol without firing OnTileSelected.
    public void DeselectTile()
    {
        if (CurrentSelectedTile == "") return;

        CurrentSelectedTile = "";
        if (currentSelectionText != null)
        {
            currentSelectionText.text = "Selected: none";
        }
    }

    // Enable/disable each Tile_* button per tutorial gate and puzzle restrictions.
    public void RefreshPaletteAvailability()
    {
        if (collapsiblePanel == null) return;

        Button[] buttons = collapsiblePanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.name.StartsWith("Tile_")) continue;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) continue;

            bool available = IsTileAvailable(NormalizeSymbol(label.text));
            button.interactable = available;
            label.color = available ? HexlinkTheme.ChipText : HexlinkTheme.TextGray;
        }
    }

    // Availability rules: tutorial gate first, then puzzle number list and
    // disabled operations; the blank "_" is always allowed.
    private bool IsTileAvailable(string symbol)
    {
        if (TutorialGate.Active && !TutorialGate.TileAllowed(symbol)) return false;

        StandardPuzzleData standard = PuzzleSelection.SelectedPuzzle as StandardPuzzleData;
        if (standard == null) return true;

        if (int.TryParse(symbol, out int number))
        {
            return standard.availableNumbers == null
                || standard.availableNumbers.Count == 0
                || standard.availableNumbers.Contains(number);
        }

        if (symbol == "_") return true;

        if (standard.disabledOperations != null)
        {
            foreach (string disabled in standard.disabledOperations)
            {
                if (NormalizeSymbol(disabled) == symbol) return false;
            }
        }

        return true;
    }

    // Map display glyphs back to TileDataFactory symbols.
    private static string NormalizeSymbol(string display)
    {
        switch (display)
        {
            case "\u00D7": return "*";
            case "\u00F7": return "/";
            default: return display;
        }
    }
}