using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InstructionToken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI label;

    private HexTileSelector selector;
    private HexCoord coord;
    private bool hovering;
    private Image chipImage;
    private Color baseColor;
    private float baseFontSize;

    private void Awake()
    {
        if (label != null) baseFontSize = label.fontSize;
    }

    public void Setup(HexTileSelector selector, HexCoord coord)
    {
        this.selector = selector;
        this.coord = coord;
    }

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
