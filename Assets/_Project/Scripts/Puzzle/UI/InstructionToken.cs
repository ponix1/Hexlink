using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InstructionToken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI label;

    private HexTileSelector selector;
    private HexCoord coord;
    private bool hovering;

    public void Setup(HexTileSelector selector, HexCoord coord)
    {
        this.selector = selector;
        this.coord = coord;
    }

    public void SetText(string text)
    {
        label.text = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (selector != null) selector.HighlightTile(coord);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (selector != null) selector.ClearTileHighlight();
    }

    private void OnDestroy()
    {
        // Re-rendered tokens can be destroyed mid-hover; OnPointerExit never fires and the
        // hover system would freeze on the stale uiHighlightActive flag.
        if (hovering && selector != null) selector.ClearTileHighlight();
    }
}
