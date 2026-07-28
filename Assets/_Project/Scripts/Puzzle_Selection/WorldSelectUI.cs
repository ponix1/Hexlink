using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class WorldSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public List<WorldData> worlds;
    public WorldCardUI worldCard;   // the ONE card object, dragged in directly
    public PuzzleGridUI puzzleGrid;  

    private int currentIndex = 0;
    private bool isHovering = false;

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
        if (!isHovering) return;

        if (Input.GetKeyDown(KeyCode.D)) ChangeWorld(1);
        if (Input.GetKeyDown(KeyCode.A)) ChangeWorld(-1);
    }

    void ChangeWorld(int direction)
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
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovering = true;
    public void OnPointerExit(PointerEventData eventData) => isHovering = false;
}