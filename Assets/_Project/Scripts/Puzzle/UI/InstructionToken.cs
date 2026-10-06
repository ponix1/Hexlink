// InstructionToken - a uGUI chip inside an InfoTab cell showing one hex tile's
// value; hovering brightens the chip and highlights the 3D hex via HexTileSelector.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Carries the HexCoord it mirrors so hover can address the right board hex.
public class InstructionToken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI label;

    // Wired by GridController at stamp time (Setup).
    private HexTileSelector selector;
    // Board hex this token represents.
    private HexCoord coord;
    // Hover state: brightened chip + active 3D highlight.
    private bool hovering;
    private Image chipImage;
    private Color baseColor;
    private float baseFontSize;

    private void Awake()
    {
        if (label != null) baseFontSize = label.fontSize;
    }

    // Bind the token to a board hex (called right after Instantiate).
    public void Setup(HexTileSelector selector, HexCoord coord)
    {
        this.selector = selector;
        this.coord = coord;
    }

    // Show the tile's display value on the chip.
    public void SetText(string text)
    {
        label.text = text;

        // Narrow glyphs like "!" have almost no ink at normal size and read as a
        // hairline — pump their font size so they fill the chip like a digit does.
        if (baseFontSize > 0f)
        {
            bool narrow = text.Length == 1 && (text == "!" || text == "." || text == ":");
            label.fontSize = narrow ? baseFontSize * 1.3f : baseFontSize;
        }
    }

    // Hover: brighten chip and highlight the 3D hex.
    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (chipImage == null) chipImage = GetComponent<Image>();
        if (chipImage != null)
        {
            baseColor = chipImage.color;
            chipImage.color = baseColor * new Color(1.25f, 1.25f, 1.25f);
        }
        if (selector != null) selector.HighlightTile(coord);
    }

    // Un-hover: restore chip color and clear the 3D highlight.
    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (chipImage != null)
        {
            chipImage.color = baseColor;
        }
        if (selector != null) selector.ClearTileHighlight();
    }

    private void OnDestroy()
    {
        // Re-rendered tokens can be destroyed mid-hover; OnPointerExit never fires and the
        // hover system would freeze on the stale uiHighlightActive flag.
        if (hovering && selector != null) selector.ClearTileHighlight();
    }
}
