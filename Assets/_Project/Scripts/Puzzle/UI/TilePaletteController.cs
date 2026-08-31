using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TilePaletteController : MonoBehaviour
{
    [SerializeField] private Button collapseButton;
    [SerializeField] private TextMeshProUGUI collapseLabel;

    private const float CollapsedHeight = 40f;
    private const float CollapseSpeed = 5f;

    private RectTransform panelRect;
    private bool collapsed;
    private float expandedHeight;
    private float targetHeight;

    private void Start()
    {
        panelRect = (RectTransform)transform;
        expandedHeight = panelRect.sizeDelta.y;
        targetHeight = expandedHeight;

        if (collapseButton != null)
        {
            collapseButton.onClick.AddListener(ToggleCollapsed);
        }
    }

    private void Update()
    {
        if (panelRect != null && Mathf.Abs(panelRect.sizeDelta.y - targetHeight) > 0.1f)
        {
            Vector2 size = panelRect.sizeDelta;
            size.y = Mathf.Lerp(size.y, targetHeight, CollapseSpeed * Time.deltaTime);
            panelRect.sizeDelta = size;
        }
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        targetHeight = collapsed ? CollapsedHeight : expandedHeight;
        if (collapseLabel != null)
        {
            collapseLabel.text = collapsed ? "Show" : "Hide";
        }
    }
}
