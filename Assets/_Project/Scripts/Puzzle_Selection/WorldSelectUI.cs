using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class WorldSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public List<WorldData> worlds;
    public WorldCardUI worldCard;   // the ONE card object, dragged in directly
    public PuzzleGridUI puzzleGrid;

    private int currentIndex = 0;
    private bool isHovering = false;
    private Button prevButton;
    private Button nextButton;

    void Start()
    {
        GameContentRegistry registry = GameContentRegistry.Load();
        if (registry != null && registry.worlds != null && registry.worlds.Count > 0)
        {
            worlds = registry.worlds;
        }

        if (worlds == null || worlds.Count == 0)
        {
            Debug.LogError("WorldSelectUI: no worlds found - run Hexlink/Sync World Content.");
            return;
        }

        BuildNavigation();
        UpdateDisplay();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D)) ChangeWorld(1);
        if (Input.GetKeyDown(KeyCode.A)) ChangeWorld(-1);
    }

    public void ChangeWorld(int direction)
    {
        int newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= worlds.Count) return;

        currentIndex = newIndex;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        worldCard.Setup(worlds[currentIndex]);
        puzzleGrid.Populate(worlds[currentIndex].puzzles);

        if (prevButton != null) prevButton.gameObject.SetActive(currentIndex > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(currentIndex < worlds.Count - 1);
    }

    private void BuildNavigation()
    {
        prevButton = CreateNavButton("<", new Vector2(15.5f, -15.5f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        prevButton.onClick.AddListener(() => ChangeWorld(-1));

        nextButton = CreateNavButton(">", new Vector2(-15.5f, -15.5f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        nextButton.onClick.AddListener(() => ChangeWorld(1));
    }

    private Button CreateNavButton(string label, Vector2 position, Vector2 anchor, Vector2 pivot)
    {
        GameObject button = new GameObject("NavButton_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(transform, false);
        Image image = button.GetComponent<Image>();
        image.color = HexlinkTheme.Ghost;
        Button buttonComponent = button.GetComponent<Button>();
        buttonComponent.targetGraphic = image;
        HexlinkTheme.ApplyHoverTint(buttonComponent);

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(30f, 43f);

        GameObject text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        ThemeSwitcher.ApplyToSubtree(button.transform);

        return buttonComponent;
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    public void OnPointerExit(PointerEventData eventData) => isHovering = false;
}
