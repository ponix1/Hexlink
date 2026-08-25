using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InstructionToken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI label;

    private HexTileSelector selector;
    private HexCoord coord;

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
        if (selector != null) selector.HighlightTile(coord);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selector != null) selector.ClearTileHighlight();
    }
}
