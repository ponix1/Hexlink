using UnityEngine;
using TMPro;

public class TileInventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject collapsiblePanel;
    [SerializeField] private TextMeshProUGUI currentSelectionText;

    // This holds the data for what we want to place (e.g., "7", "+", "_")
    public string CurrentSelectedTile { get; private set; } = "";

    private bool isExpanded = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
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
}