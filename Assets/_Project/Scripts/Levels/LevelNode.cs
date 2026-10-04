using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Fixed, theme-independent node visuals (nudged off palette values by one
    // quantization step so ThemeSwitcher never remaps them).
    private static readonly Color CompleteGreen = new Color(0.30f, 0.78f, 0.47f);
    private static readonly Color RingIdle = new Color(0.43f, 0.46f, 0.52f);
    private static readonly Color RingCurrent = new Color(0.59f, 0.75f, 0.79f);

    // Layout space is a 1920x1080 canvas centred on (0,0). Used when a level
    // has no authored map position yet.
    public static Vector2 DefaultPosition(int index, int count)
    {
        float spacing = 300f;
        float x = (index - (count - 1) * 0.5f) * spacing;
        float y = 160f * (index % 2 == 0 ? 1f : -1f);
        return new Vector2(x, y);
    }

    private PuzzleData data;
    private int index;
    private bool unlocked;
    private bool isCurrent;
    private ChamferedImage frame;
    private TextMeshProUGUI numberLabel;
    private TextMeshProUGUI checkLabel;
    private TextMeshProUGUI lockLabel;
    private RectTransform ownRect;
    private bool pulse;

    public void Setup(PuzzleData puzzleData, int levelIndex, bool nodeUnlocked, bool currentNode,
        ChamferedImage nodeFrame,
        TextMeshProUGUI number, TextMeshProUGUI check, TextMeshProUGUI lockText)
    {
        data = puzzleData;
        index = levelIndex;
        unlocked = nodeUnlocked;
        isCurrent = currentNode;
        frame = nodeFrame;
        numberLabel = number;
        checkLabel = check;
        lockLabel = lockText;
        ownRect = (RectTransform)transform;

        bool complete = PuzzleProgress.IsComplete(data.puzzleID);
        pulse = isCurrent && unlocked && !complete && !GameOptions.ReducedMotion;

        numberLabel.text = (index + 1).ToString();
        checkLabel.gameObject.SetActive(complete);
        lockLabel.gameObject.SetActive(!unlocked);

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = unlocked ? 1f : 0.45f;

        frame.color = complete ? CompleteGreen
            : unlocked && isCurrent ? RingCurrent
            : RingIdle;

        GetComponent<Button>().interactable = unlocked;
    }

    public void OnNodeClicked()
    {
        if (!unlocked || data == null) return;

        PuzzleSelection.SelectedPuzzle = data;
        PuzzleSelection.ReturnScene = "Level_Select";
        SolutionPickerPopup.Show(data,
            selected =>
            {
                SolutionStore.PendingIndex = selected;
                SceneManager.LoadScene("Puzzle_Play");
            },
            () =>
            {
                SolutionStore.PendingIndex = SolutionStore.PendingNew;
                SceneManager.LoadScene("Puzzle_Play");
            });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (data != null) LevelTooltip.Show(data, ownRect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LevelTooltip.Hide();
    }

    private void OnDisable()
    {
        LevelTooltip.Hide();
        if (ownRect != null) ownRect.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!pulse)
        {
            if (ownRect.localScale != Vector3.one) ownRect.localScale = Vector3.one;
            return;
        }

        float scale = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.025f;
        ownRect.localScale = Vector3.one * scale;
    }
}
