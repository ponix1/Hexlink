// Builds the Level_Select map entirely in code: a horizontally scrolling
// dotted path of numbered level nodes with completion/lock state.
// Auto-spawns on scene load; no prefabs involved.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class LevelMapFactory
{
    // Node square size in canvas pixels.
    private const float NodeSize = 130f;
    // Padding beyond the first/last node inside the scrollable content.
    private const float EdgePadding = 220f;

    // Fixed, theme-independent visuals for elements that live directly on the
    // starry sky (the sky never changes theme, so these stay readable in all
    // three designs). Values are nudged off palette colours so ThemeSwitcher
    // never remaps them.
    private static readonly Color GlassFill = new Color(0.10f, 0.12f, 0.16f, 0.56f);
    private static readonly Color NodeNumber = new Color(0.99f, 0.99f, 1f);
    private static readonly Color NodeLockText = new Color(0.72f, 0.75f, 0.78f);
    private static readonly Color DotUnlocked = new Color(0.43f, 0.46f, 0.52f);
    private static readonly Color DotLocked = new Color(0.43f, 0.46f, 0.52f, 0.40f);

    // Connector dot visuals.
    private const float DotSize = 6f;
    private const float DotSpacing = 20f;
    // Gap kept at each connector end so dots don't slide under the nodes.
    private const float ConnectorTrim = 80f;

    // Rebuild the map every time Level_Select loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        SceneManager.sceneLoaded += (scene, mode) => TrySpawn();
        TrySpawn();
    }

    // Only in Level_Select; destroy any previous map so state is rebuilt fresh.
    private static void TrySpawn()
    {
        if (SceneManager.GetActiveScene().name != "Level_Select") return;

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find("LevelMap");
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        Build(canvas);
    }

    // Create the full-screen map root; show a hint label when no levels exist.
    public static void Build(Canvas canvas)
    {
        EnsureSkyboxRotation();

        GameObject map = new GameObject("LevelMap", typeof(RectTransform));
        map.transform.SetParent(canvas.transform, false);
        Stretch(map.GetComponent<RectTransform>());

        List<PuzzleData> levels = LevelProgress.GetLevels();

        if (levels.Count == 0)
        {
            GameObject empty = CreateTMP("EmptyLabel", map.transform,
                "No levels yet - add them under Assets/_Project/Assets/Levels", 16, FontStyles.Normal);
            empty.GetComponent<TextMeshProUGUI>().color = HexlinkTheme.TextLight;
            RectTransform emptyRT = empty.GetComponent<RectTransform>();
            emptyRT.anchorMin = new Vector2(0f, 0f);
            emptyRT.anchorMax = new Vector2(1f, 1f);
            emptyRT.offsetMin = Vector2.zero;
            emptyRT.offsetMax = Vector2.zero;
            return;
        }

        BuildPath(map.transform, levels);
    }

    // Give the main camera its slowly rotating skybox, if missing.
    private static void EnsureSkyboxRotation()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        if (camera.GetComponent<SkyboxRotator>() == null)
        {
            camera.gameObject.AddComponent<SkyboxRotator>();
        }
    }

    // Use each level's authored map position, else the zig-zag default.
    private static Vector2[] GetPositions(List<PuzzleData> levels)
    {
        Vector2[] positions = new Vector2[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            positions[i] = levels[i].hasMapPosition
                ? levels[i].mapPosition
                : LevelNode.DefaultPosition(i, levels.Count);
        }
        return positions;
    }

    // Assemble the ScrollRect with content sized to the path, then connectors and nodes.
    private static void BuildPath(Transform parent, List<PuzzleData> levels)
    {
        GameObject scroll = new GameObject("PathScroll", typeof(RectTransform));
        scroll.transform.SetParent(parent, false);
        ScrollRect scrollRect = scroll.AddComponent<ScrollRect>();
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        Stretch(scrollRT);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scroll.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        Stretch(viewportRT);

        Vector2[] positions = GetPositions(levels);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (Vector2 position in positions)
        {
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
        }

        // Content width spans the path's X range plus edge padding on both sides.
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0.5f);
        contentRT.anchorMax = new Vector2(0f, 0.5f);
        contentRT.pivot = new Vector2(0f, 0.5f);
        contentRT.sizeDelta = new Vector2(maxX - minX + EdgePadding * 2f, 0f);
        contentRT.anchoredPosition = Vector2.zero;

        // Invisible image that still catches raycasts, so drags scroll anywhere.
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(content.transform, false);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0f);
        backgroundImage.raycastTarget = true;
        Stretch(background.GetComponent<RectTransform>());

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 40f;
        // Scrolling shifts anchors; drop the tooltip immediately.
        scrollRect.onValueChanged.AddListener(_ => LevelTooltip.Hide());

        // The level progress considers "current"; its node gets highlighted.
        int current = LevelProgress.CurrentIndex();

        // Shift X so the leftmost node lands at EdgePadding inside the content.
        Vector2[] local = new Vector2[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            local[i] = new Vector2(positions[i].x - minX + EdgePadding, positions[i].y);
        }

        for (int i = 0; i < levels.Count - 1; i++)
        {
            BuildDottedConnector(content.transform,
                local[i], local[i + 1], LevelProgress.IsUnlocked(i + 1));
        }

        for (int i = 0; i < levels.Count; i++)
        {
            BuildNode(content.transform, levels[i], i,
                LevelProgress.IsUnlocked(i), current == i, local[i]);
        }
    }

    // Lay dots along the segment between two nodes; dimmed if it leads to a locked node.
    private static void BuildDottedConnector(Transform parent, Vector2 from, Vector2 to, bool leadsToUnlocked)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length <= ConnectorTrim * 2f) return;

        Vector2 direction = delta / length;
        Vector2 start = from + direction * ConnectorTrim;
        Vector2 end = to - direction * ConnectorTrim;
        Color dotColor = leadsToUnlocked ? DotUnlocked : DotLocked;

        for (float distance = DotSpacing * 0.5f; distance < length - ConnectorTrim * 2f; distance += DotSpacing)
        {
            Vector2 position = start + direction * distance;
            GameObject dot = new GameObject("Dot", typeof(RectTransform));
            dot.transform.SetParent(parent, false);
            ChamferedImage chamfer = dot.AddComponent<ChamferedImage>();
            chamfer.Chamfer = DotSize * 0.5f;
            chamfer.color = dotColor;
            chamfer.raycastTarget = false;
            RectTransform rt = dot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(DotSize, DotSize);
        }
    }

    // Create one node: glass face, ring, number, check and "locked" labels,
    // then wire click/state handling into LevelNode.
    private static void BuildNode(Transform parent, PuzzleData data, int index, bool unlocked, bool isCurrent, Vector2 position)
    {
        GameObject node = new GameObject($"LevelNode_{index + 1}", typeof(RectTransform), typeof(CanvasGroup));
        node.transform.SetParent(parent, false);
        RectTransform nodeRT = node.GetComponent<RectTransform>();
        nodeRT.anchorMin = new Vector2(0f, 0.5f);
        nodeRT.anchorMax = new Vector2(0f, 0.5f);
        nodeRT.pivot = new Vector2(0.5f, 0.5f);
        nodeRT.anchoredPosition = position;
        nodeRT.sizeDelta = new Vector2(NodeSize, NodeSize);

        GameObject faceGO = new GameObject("Face", typeof(RectTransform));
        faceGO.transform.SetParent(node.transform, false);
        ChamferedImage face = faceGO.AddComponent<ChamferedImage>();
        face.Chamfer = 32f;
        face.color = GlassFill;
        face.raycastTarget = true;
        RectTransform faceRT = faceGO.GetComponent<RectTransform>();
        Stretch(faceRT);

        GameObject ringGO = new GameObject("Ring", typeof(RectTransform));
        ringGO.transform.SetParent(node.transform, false);
        ChamferedImage ring = ringGO.AddComponent<ChamferedImage>();
        ring.Chamfer = 38f;
        ring.OutlineThickness = 6f;
        ring.color = HexlinkTheme.Divider;
        ring.raycastTarget = false;
        RectTransform ringRT = ringGO.GetComponent<RectTransform>();
        Stretch(ringRT);

        GameObject number = CreateTMP("Number", node.transform, (index + 1).ToString(), 40, FontStyles.Bold);
        Stretch(number.GetComponent<RectTransform>());
        TextMeshProUGUI numberTMP = number.GetComponent<TextMeshProUGUI>();
        numberTMP.color = NodeNumber;
        numberTMP.raycastTarget = false;

        GameObject check = CreateTMP("Check", node.transform, "\u2713", 34, FontStyles.Bold);
        RectTransform checkRT = check.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.5f, 1f);
        checkRT.anchorMax = new Vector2(0.5f, 1f);
        checkRT.pivot = new Vector2(0.5f, 1f);
        checkRT.anchoredPosition = new Vector2(34f, -6f);
        checkRT.sizeDelta = new Vector2(50f, 40f);
        check.GetComponent<TextMeshProUGUI>().color = new Color(0.30f, 0.78f, 0.47f);
        check.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        GameObject lockGO = CreateTMP("LockText", node.transform, "locked", 12, FontStyles.Bold);
        RectTransform lockRT = lockGO.GetComponent<RectTransform>();
        lockRT.anchorMin = new Vector2(0.5f, 0f);
        lockRT.anchorMax = new Vector2(0.5f, 0f);
        lockRT.pivot = new Vector2(0.5f, 0f);
        lockRT.anchoredPosition = new Vector2(0f, 2f);
        lockRT.sizeDelta = new Vector2(80f, 20f);
        lockGO.GetComponent<TextMeshProUGUI>().color = NodeLockText;
        lockGO.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        Button button = node.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Button.Transition.ColorTint;
        HexlinkTheme.ApplyHoverTint(button);

        // LevelNode applies state visuals (ring colour, alpha, check/lock).
        LevelNode levelNode = node.AddComponent<LevelNode>();
        levelNode.Setup(data, index, unlocked, isCurrent, ring,
            numberTMP,
            check.GetComponent<TextMeshProUGUI>(),
            lockGO.GetComponent<TextMeshProUGUI>());
        button.onClick.AddListener(levelNode.OnNodeClicked);
    }

    // TMP label helper; font size follows GameOptions.UiScale.
    private static GameObject CreateTMP(string name, Transform parent, string text, int fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = Mathf.Max(8f, Mathf.Round(fontSize * GameOptions.UiScale));
        tmp.fontStyle = style;
        tmp.color = HexlinkTheme.TextLight;
        tmp.alignment = TextAlignmentOptions.Center;
        go.AddComponent<LayoutElement>().preferredHeight = tmp.fontSize * 1.4f;
        return go;
    }

    // Stretch a rect to fill its parent.
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
