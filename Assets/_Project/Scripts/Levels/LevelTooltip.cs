using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LevelTooltip
{
    private static readonly (string display, string normalized)[] AllOperations =
    {
        ("+", "+"),
        ("-", "-"),
        ("\u00D7", "*"),
        ("\u00F7", "/"),
        ("^", "^"),
        ("!", "!"),
        ("\u221A", "\u221A"),
    };

    private static GameObject root;
    private static RectTransform rootRT;
    private static TextMeshProUGUI titleText;
    private static TextMeshProUGUI targetText;
    private static TextMeshProUGUI numbersText;
    private static TextMeshProUGUI operationsText;

    public static void Show(PuzzleData data, RectTransform anchor)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || anchor == null) return;

        if (root == null) Build(canvas.transform);
        if (root == null) return;

        root.transform.SetAsLastSibling();
        Fill(data);
        PositionOver(anchor);
        root.SetActive(true);
    }

    public static void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private static void Fill(PuzzleData data)
    {
        titleText.text = data.puzzleTitle;
        targetText.text = "Target: " + data.GetDisplayTarget();

        StandardPuzzleData standard = data as StandardPuzzleData;
        if (standard != null && standard.availableNumbers != null && standard.availableNumbers.Count > 0)
        {
            numbersText.text = "Numbers: " + string.Join(", ", standard.availableNumbers);
            operationsText.text = "Operations: " + BuildOperationsText(standard);
        }
        else
        {
            numbersText.text = "Numbers: All";
            operationsText.text = "Operations: All";
        }
    }

    private static string BuildOperationsText(StandardPuzzleData standard)
    {
        HashSet<string> disabled = new HashSet<string>();
        if (standard.disabledOperations != null)
        {
            foreach (string operation in standard.disabledOperations)
            {
                disabled.Add(NormalizeSymbol(operation));
            }
        }

        List<string> available = new List<string>();
        foreach ((string display, string normalized) operation in AllOperations)
        {
            if (!disabled.Contains(operation.normalized)) available.Add(operation.display);
        }

        return available.Count == AllOperations.Length ? "All" : string.Join(" ", available);
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

    private static void PositionOver(RectTransform anchor)
    {
        RectTransform canvasRT = (RectTransform)root.transform.parent;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, null, out Vector2 local)) return;

        Vector2 size = rootRT.sizeDelta;
        Vector2 half = canvasRT.rect.size * 0.5f - size * 0.5f - new Vector2(10f, 10f);

        float x = Mathf.Clamp(local.x, -half.x, half.x);
        float above = local.y + 140f;
        float y = above <= half.y
            ? Mathf.Max(above, -half.y)
            : Mathf.Clamp(local.y - 140f, -half.y, half.y);

        rootRT.anchoredPosition = new Vector2(x, y);
    }

    private static void Build(Transform canvasTransform)
    {
        root = new GameObject("LevelTooltip", typeof(RectTransform));
        root.transform.SetParent(canvasTransform, false);
        rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.sizeDelta = new Vector2(300f, 130f);
        root.SetActive(false);

        titleText = CreateLine("Title", 20, FontStyles.Bold);
        targetText = CreateLine("Target", 15, FontStyles.Normal);
        numbersText = CreateLine("Numbers", 15, FontStyles.Normal);
        operationsText = CreateLine("Operations", 15, FontStyles.Normal);

        RectTransform titleRT = titleText.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -8f);
        titleRT.sizeDelta = new Vector2(290f, 30f);

        GameObject separator = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        separator.transform.SetParent(root.transform, false);
        Image line = separator.GetComponent<Image>();
        line.color = new Color(1f, 1f, 1f, 0.35f);
        line.raycastTarget = false;
        RectTransform separatorRT = separator.GetComponent<RectTransform>();
        separatorRT.anchorMin = new Vector2(0.5f, 1f);
        separatorRT.anchorMax = new Vector2(0.5f, 1f);
        separatorRT.pivot = new Vector2(0.5f, 1f);
        separatorRT.anchoredPosition = new Vector2(0f, -42f);
        separatorRT.sizeDelta = new Vector2(170f, 2f);

        AnchorBelowSeparator(targetText, 54f);
        AnchorBelowSeparator(numbersText, 80f);
        AnchorBelowSeparator(operationsText, 106f);
    }

    private static void AnchorBelowSeparator(TextMeshProUGUI tmp, float yOffset)
    {
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yOffset);
        rt.sizeDelta = new Vector2(290f, 26f);
    }

    private static TextMeshProUGUI CreateLine(string name, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(fontSize * GameOptions.UiScale));
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
