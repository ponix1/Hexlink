using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class InfoTabBuilder
{
    [MenuItem("Hexlink/Build Info Tab")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        // --- InfoTab (black border layer) ---
        GameObject infoTab = new GameObject("InfoTab", typeof(RectTransform), typeof(Image));
        infoTab.transform.SetParent(canvas.transform, false);
        RectTransform infoTabRT = infoTab.GetComponent<RectTransform>();
        infoTabRT.anchorMin = new Vector2(0f, 0f);
        infoTabRT.anchorMax = new Vector2(1f, 0f);
        infoTabRT.pivot = new Vector2(0.5f, 0f);
        infoTabRT.sizeDelta = new Vector2(0f, 220f);
        infoTabRT.anchoredPosition = Vector2.zero;
        infoTab.GetComponent<Image>().color = Color.black;

        // --- Content (white inset layer) ---
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
        content.transform.SetParent(infoTab.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(4f, 4f);
        contentRT.offsetMax = new Vector2(-4f, -4f);
        content.GetComponent<Image>().color = Color.white;

        // --- MainRow ---
        GameObject mainRow = new GameObject("MainRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        mainRow.transform.SetParent(content.transform, false);
        RectTransform mainRowRT = mainRow.GetComponent<RectTransform>();
        mainRowRT.anchorMin = Vector2.zero;
        mainRowRT.anchorMax = Vector2.one;
        mainRowRT.offsetMin = Vector2.zero;
        mainRowRT.offsetMax = Vector2.zero;
        HorizontalLayoutGroup mainRowHLG = mainRow.GetComponent<HorizontalLayoutGroup>();
        mainRowHLG.childAlignment = TextAnchor.UpperLeft;
        mainRowHLG.childForceExpandWidth = true;
        mainRowHLG.childForceExpandHeight = true;
        mainRowHLG.childControlWidth = true;
        mainRowHLG.childControlHeight = true;
        mainRowHLG.padding = new RectOffset(10, 10, 10, 10);
        mainRowHLG.spacing = 10f;

        // --- LeftPanel ---
        GameObject leftPanel = CreateVerticalGroup("LeftPanel", mainRow.transform, TextAnchor.UpperLeft, 4f);
        LayoutElement leftLE = leftPanel.AddComponent<LayoutElement>();
        leftLE.preferredWidth = 220f;
        leftLE.flexibleWidth = 0f;

        CreateTMP("TargetText", leftPanel.transform, "Target: 345", 28, FontStyles.Bold);
        CreateTMP("RestrictionsText", leftPanel.transform, "Restrictions: None", 16, FontStyles.Normal);

        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(leftPanel.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

        CreateTMP("DifficultyText", leftPanel.transform, "Difficulty: 5", 16, FontStyles.Normal);

        // --- Divider ---
        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        divider.transform.SetParent(mainRow.transform, false);
        divider.GetComponent<Image>().color = Color.black;
        LayoutElement dividerLE = divider.GetComponent<LayoutElement>();
        dividerLE.preferredWidth = 3f;
        dividerLE.flexibleHeight = 1f;

        // --- RightPanel: ScrollRect holding dynamically-instantiated cycle rows ---
        GameObject rightPanel = new GameObject("RightPanel", typeof(RectTransform), typeof(LayoutElement), typeof(ScrollRect), typeof(Image));
        rightPanel.transform.SetParent(mainRow.transform, false);
        rightPanel.GetComponent<LayoutElement>().flexibleWidth = 1f;
        rightPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f); // transparent, needed for raycast area

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(rightPanel.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // near-invisible, required for Mask
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject scrollContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        scrollContent.transform.SetParent(viewport.transform, false);
        RectTransform scrollContentRT = scrollContent.GetComponent<RectTransform>();
        scrollContentRT.anchorMin = new Vector2(0f, 1f);
        scrollContentRT.anchorMax = new Vector2(1f, 1f);
        scrollContentRT.pivot = new Vector2(0.5f, 1f);
        scrollContentRT.offsetMin = new Vector2(0f, 0f);
        scrollContentRT.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup scrollVLG = scrollContent.GetComponent<VerticalLayoutGroup>();
        scrollVLG.childAlignment = TextAnchor.UpperLeft;
        scrollVLG.childForceExpandWidth = true;
        scrollVLG.childForceExpandHeight = false;
        scrollVLG.childControlWidth = true;
        scrollVLG.childControlHeight = true;
        scrollVLG.spacing = 2f;

        ContentSizeFitter csf = scrollContent.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = rightPanel.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRT;
        scrollRect.content = scrollContentRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // --- CycleRowTemplate: one row = [number label] + [instruction token strip] + [divider line] ---
        // This lives under Content, disabled, and is Instantiate()'d at runtime — one copy per cycle.
        // A controller script duplicates it, sets the number, and appends instruction tokens
        // (hex icon / arrow / hex icon / comma, etc.) into InstructionContainer as the player builds code.
        GameObject rowTemplate = new GameObject("CycleRowTemplate", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        rowTemplate.transform.SetParent(scrollContent.transform, false);
        VerticalLayoutGroup rowVLG = rowTemplate.GetComponent<VerticalLayoutGroup>();
        rowVLG.childAlignment = TextAnchor.UpperLeft;
        rowVLG.childForceExpandWidth = true;
        rowVLG.childForceExpandHeight = false;
        rowVLG.childControlWidth = true;
        rowVLG.childControlHeight = true;
        rowVLG.spacing = 2f;
        rowTemplate.GetComponent<LayoutElement>().preferredHeight = 40f;

        // Number label ("1", "2", ...) sits above the instruction strip, small, like the reference image.
        GameObject numberLabel = CreateTMP("NumberText", rowTemplate.transform, "1", 14, FontStyles.Normal);
        numberLabel.GetComponent<LayoutElement>().preferredHeight = 18f;

        // InstructionContainer: empty horizontal strip. Runtime code appends instruction token
        // prefabs here left-to-right (hex icon, arrow icon, hex icon, comma...) as the player commits instructions.
        GameObject instructionContainer = new GameObject("InstructionContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        instructionContainer.transform.SetParent(rowTemplate.transform, false);
        HorizontalLayoutGroup instrHLG = instructionContainer.GetComponent<HorizontalLayoutGroup>();
        instrHLG.childAlignment = TextAnchor.MiddleLeft;
        instrHLG.childForceExpandWidth = false;
        instrHLG.childForceExpandHeight = false;
        instrHLG.childControlWidth = false;
        instrHLG.childControlHeight = false;
        instrHLG.spacing = 4f;
        instructionContainer.GetComponent<LayoutElement>().preferredHeight = 20f;

        // Bottom divider line under each row, matching the reference image's horizontal rules.
        GameObject rowLine = new GameObject("LineImage", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowLine.transform.SetParent(rowTemplate.transform, false);
        rowLine.GetComponent<Image>().color = Color.black;
        rowLine.GetComponent<LayoutElement>().preferredHeight = 2f;

        // Template is inactive — it's a stamp, not a visible row. Runtime code enables its Instantiate()'d copies.
        rowTemplate.SetActive(false);

        Selection.activeGameObject = infoTab;
        Debug.Log("InfoTab built successfully. CycleRowTemplate is under RightPanel > Viewport > Content, disabled — duplicate it at runtime per cycle.");
    }

    private static GameObject CreateVerticalGroup(string name, Transform parent, TextAnchor alignment, float spacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = alignment;
        vlg.spacing = spacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        return go;
    }

    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.black;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.4f;
        return go;
    }
}