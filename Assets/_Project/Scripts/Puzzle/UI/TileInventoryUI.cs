using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TileInventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject collapsiblePanel;
    [SerializeField] private TextMeshProUGUI currentSelectionText;

    // This holds the data for what we want to place (e.g., "7", "+", "_")
    public string CurrentSelectedTile { get; private set; } = "";

    public event System.Action OnTileSelected;

    private bool isExpanded = true;

    private void Start()
    {
        RefreshPaletteAvailability();
    }

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

    public void DeselectTile()
    {
        if (CurrentSelectedTile == "") return;

        CurrentSelectedTile = "";
        if (currentSelectionText != null)
        {
            currentSelectionText.text = "Selected: none";
        }
    }

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
            label.color = available ? Color.black : new Color(0.55f, 0.55f, 0.55f);
        }
    }

    private bool IsTileAvailable(string symbol)
    {
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