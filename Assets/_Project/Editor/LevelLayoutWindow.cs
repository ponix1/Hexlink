using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelLayoutWindow : EditorWindow
{
    private const string Version = "v5";

    private const float LayoutWidth = 1920f;
    private const float LayoutHeight = 1080f;
    private const float PanLimitX = 4800f;
    private const float PanLimitY = 300f;
    private const float NodeRadius = 65f;
    private const float HitRadius = 130f;
    private const float SnapStep = 20f;

    private static readonly Color SkyColor = new Color(0.055f, 0.065f, 0.10f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color GlassFill = new Color(0.10f, 0.12f, 0.16f, 0.85f);
    private static readonly Color DotColor = new Color(0.42f, 0.45f, 0.51f);
    private static readonly Color CompleteGreen = new Color(0.30f, 0.78f, 0.47f);
    private static readonly Color RingIdle = new Color(0.43f, 0.46f, 0.52f);
    private static readonly Color RingCurrent = new Color(0.59f, 0.75f, 0.79f);

    private List<PuzzleData> levels = new List<PuzzleData>();
    private Texture2D circleTexture;
    private Vector2[] stars;
    private bool snap = true;
    private int draggedIndex = -1;
    private int hoverIndex = -1;
    private Vector2 dragOffset;
    private string loadError;
    private string eventFeedback;
    private int canvasControlID;
    private Vector2 viewOffset;
    private bool panning;
    private Vector2 panStartMouse;
    private Vector2 panStartOffset;

    [MenuItem("Hexlink/Edit Level Layout")]
    public static void Open()
    {
        LevelLayoutWindow window = GetWindow<LevelLayoutWindow>("Level Layout");
        window.minSize = new Vector2(900f, 520f);
    }

    private void OnEnable()
    {
        wantsMouseMove = true;
        viewOffset = new Vector2(
            Mathf.Clamp(EditorPrefs.GetFloat("Hexlink.LayoutViewX", 0f), 0f, PanLimitX),
            Mathf.Clamp(EditorPrefs.GetFloat("Hexlink.LayoutViewY", 0f), -PanLimitY, 0f));
        LoadContent();
        if (circleTexture == null) circleTexture = MakeCircleTexture();
        if (stars == null)
        {
            System.Random random = new System.Random(12345);
            stars = new Vector2[220];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = new Vector2(
                    (float)(random.NextDouble() * 2.0 - 1.0) * PanLimitX,
                    (float)(random.NextDouble() * 2.0 - 1.0) * LayoutHeight * 0.5f);
            }
        }
    }

    private void OnDisable()
    {
        SaveView();
        if (draggedIndex >= 0)
        {
            draggedIndex = -1;
            SaveLayout();
        }
    }

    private void SaveView()
    {
        EditorPrefs.SetFloat("Hexlink.LayoutViewX", viewOffset.x);
        EditorPrefs.SetFloat("Hexlink.LayoutViewY", viewOffset.y);
    }

    private void LoadContent()
    {
        loadError = null;
        eventFeedback = null;
        draggedIndex = -1;
        hoverIndex = -1;

        GameContentRegistry registry = GameContentRegistry.Load();
        levels = registry != null && registry.levels != null
            ? new List<PuzzleData>(registry.levels)
            : new List<PuzzleData>();

        if (levels.Count == 0)
        {
            loadError = "No levels found.\n" +
                "Create Assets/_Project/Assets/Levels/Level_N.asset files and run Hexlink/Sync World Content.";
        }
        else
        {
            eventFeedback = $"{levels.Count} levels loaded";
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        Rect canvas = GUILayoutUtility.GetRect(10f, 10000f, 100f, 100000f);
        canvasControlID = GUIUtility.GetControlID(FocusType.Passive);

        if (Event.current.type == EventType.Repaint)
        {
            DrawSky(canvas);
            DrawBorder(canvas);
            if (loadError != null)
            {
                DrawError(canvas, loadError);
            }
            else
            {
                DrawGrid(canvas);
                DrawConnectors(canvas);
                DrawNodes(canvas);
            }
        }

        HandleEvents(canvas);
        DrawStatusBar();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
            {
                LoadContent();
                Repaint();
            }
            snap = GUILayout.Toggle(snap, "Snap", EditorStyles.toolbarButton);
            if (GUILayout.Button("Reset to zig-zag", EditorStyles.toolbarButton))
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    levels[i].hasMapPosition = true;
                    levels[i].mapPosition = LevelNode.DefaultPosition(i, levels.Count);
                    EditorUtility.SetDirty(levels[i]);
                }
                SaveLayout();
                eventFeedback = "reset to zig-zag (saved)";
                Repaint();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{levels.Count} levels \u00B7 {Version}", EditorStyles.miniLabel);
        }
    }

    private void DrawStatusBar()
    {
        string status;
        if (loadError != null)
        {
            status = "Nothing to edit";
        }
        else if (eventFeedback != null)
        {
            status = eventFeedback;
        }
        else if (hoverIndex >= 0)
        {
            status = Describe(hoverIndex);
        }
        else
        {
            status = "left-drag node \u00B7 right-drag pan \u00B7 double-click ping";
        }

        Rect bar = GUILayoutUtility.GetRect(10f, 20f, GUILayout.ExpandWidth(true));
        EditorGUI.LabelField(bar, status, EditorStyles.miniLabel);
    }

    private string Describe(int index)
    {
        PuzzleData level = levels[index];
        Vector2 position = GetPosition(index);
        return $"{level.puzzleTitle} \u00B7 x {position.x:F0}, y {position.y:F0}";
    }

    private void HandleEvents(Rect canvas)
    {
        if (levels.Count == 0) return;

        Event e = Event.current;

        if (e.type == EventType.MouseMove)
        {
            if (canvas.Contains(e.mousePosition))
            {
                int hit = HitTest(canvas, e.mousePosition);
                if (hit != hoverIndex)
                {
                    hoverIndex = hit;
                    if (draggedIndex < 0) eventFeedback = null;
                    Repaint();
                }
            }
            else if (hoverIndex != -1)
            {
                hoverIndex = -1;
                Repaint();
            }
            return;
        }

        if (e.type == EventType.MouseDown && (e.button == 1 || e.button == 2) && canvas.Contains(e.mousePosition))
        {
            panning = true;
            panStartMouse = e.mousePosition;
            panStartOffset = viewOffset;
            eventFeedback = "panning view";
            e.Use();
            Repaint();
            return;
        }
        else if (e.type == EventType.MouseDrag && panning)
        {
            float scale = GetScale(canvas);
            viewOffset = panStartOffset + new Vector2(
                -(e.mousePosition.x - panStartMouse.x) / scale,
                (e.mousePosition.y - panStartMouse.y) / scale);
            viewOffset.x = Mathf.Clamp(viewOffset.x, 0f, PanLimitX);
            viewOffset.y = Mathf.Clamp(viewOffset.y, -PanLimitY, 0f);
            e.Use();
            Repaint();
            return;
        }
        else if (e.type == EventType.MouseUp && panning)
        {
            panning = false;
            eventFeedback = null;
            SaveView();
            e.Use();
            Repaint();
            return;
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (!canvas.Contains(e.mousePosition)) return;

            int hit = HitTest(canvas, e.mousePosition);
            if (hit < 0)
            {
                eventFeedback = $"missed @ {e.mousePosition.x:F0},{e.mousePosition.y:F0} - click closer to a node";
                Repaint();
                return;
            }

            if (e.clickCount >= 2)
            {
                Selection.activeObject = levels[hit];
                EditorGUIUtility.PingObject(levels[hit]);
                eventFeedback = $"pinged {levels[hit].puzzleTitle}";
            }
            else
            {
                draggedIndex = hit;
                dragOffset = GetPosition(hit) - ToLayout(canvas, e.mousePosition);
                GUIUtility.hotControl = canvasControlID;
                eventFeedback = $"grabbed {levels[hit].puzzleTitle}";
            }
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && draggedIndex >= 0 && GUIUtility.hotControl == canvasControlID)
        {
            Vector2 position = ToLayout(canvas, e.mousePosition) + dragOffset;
            position.x = Mathf.Clamp(position.x, -PanLimitX + NodeRadius, PanLimitX - NodeRadius);
            position.y = Mathf.Clamp(position.y, -LayoutHeight * 0.5f + NodeRadius, LayoutHeight * 0.5f - NodeRadius);
            if (snap)
            {
                position.x = Mathf.Round(position.x / SnapStep) * SnapStep;
                position.y = Mathf.Round(position.y / SnapStep) * SnapStep;
            }
            levels[draggedIndex].hasMapPosition = true;
            levels[draggedIndex].mapPosition = position;
            EditorUtility.SetDirty(levels[draggedIndex]);
            eventFeedback = $"dragging {Describe(draggedIndex)}";
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp && draggedIndex >= 0 && GUIUtility.hotControl == canvasControlID)
        {
            GUIUtility.hotControl = 0;
            eventFeedback = $"dropped {Describe(draggedIndex)} (saved)";
            draggedIndex = -1;
            SaveLayout();
            e.Use();
            Repaint();
        }
    }

    private void DrawSky(Rect canvas)
    {
        EditorGUI.DrawRect(canvas, SkyColor);
        foreach (Vector2 star in stars)
        {
            Vector2 point = ToCanvas(canvas, star);
            if (!canvas.Contains(point)) continue;
            EditorGUI.DrawRect(new Rect(point.x, point.y, 1.5f, 1.5f), new Color(1f, 1f, 1f, 0.5f));
        }
    }

    private void DrawError(Rect canvas, string message)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = 14
        };
        style.normal.textColor = new Color(1f, 0.4f, 0.4f);
        Rect label = new Rect(canvas.x, canvas.center.y - 40f, canvas.width, 80f);
        GUI.Label(label, message, style);
    }

    private void DrawBorder(Rect canvas)
    {
        float scale = GetScale(canvas);
        Vector2 topLeft = ToCanvas(canvas, new Vector2(-LayoutWidth * 0.5f, LayoutHeight * 0.5f));
        Rect border = new Rect(topLeft.x, topLeft.y, LayoutWidth * scale, LayoutHeight * scale);
        EditorGUI.DrawRect(new Rect(border.x, border.y, border.width, 2f), BorderColor);
        EditorGUI.DrawRect(new Rect(border.x, border.yMax - 2f, border.width, 2f), BorderColor);
        EditorGUI.DrawRect(new Rect(border.x, border.y, 2f, border.height), BorderColor);
        EditorGUI.DrawRect(new Rect(border.xMax - 2f, border.y, 2f, border.height), BorderColor);
    }

    private void DrawGrid(Rect canvas)
    {
        Vector2 bottomLeft = ToLayout(canvas, new Vector2(canvas.xMin, canvas.yMax));
        Vector2 topRight = ToLayout(canvas, new Vector2(canvas.xMax, canvas.yMin));

        for (float x = Mathf.Ceil(bottomLeft.x / 240f) * 240f; x <= topRight.x; x += 240f)
        {
            Vector2 top = ToCanvas(canvas, new Vector2(x, LayoutHeight));
            Vector2 bottom = ToCanvas(canvas, new Vector2(x, -LayoutHeight));
            DrawLine(top, bottom, GridColor);
        }
        for (float y = Mathf.Ceil(bottomLeft.y / 240f) * 240f; y <= topRight.y; y += 240f)
        {
            float xMin = Mathf.Max(-PanLimitX, bottomLeft.x);
            float xMax = Mathf.Min(PanLimitX, topRight.x);
            Vector2 left = ToCanvas(canvas, new Vector2(xMin, y));
            Vector2 right = ToCanvas(canvas, new Vector2(xMax, y));
            DrawLine(left, right, GridColor);
        }
    }

    private void DrawConnectors(Rect canvas)
    {
        for (int i = 0; i < levels.Count - 1; i++)
        {
            Vector2 from = GetPosition(i);
            Vector2 to = GetPosition(i + 1);
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length <= 160f) continue;

            Vector2 direction = delta / length;
            Vector2 start = from + direction * 80f;
            for (float d = 10f; d < length - 160f; d += 20f)
            {
                Vector2 point = ToCanvas(canvas, start + direction * d);
                Rect dot = new Rect(point.x - 2f, point.y - 2f, 4f, 4f);
                Color previous = GUI.color;
                GUI.color = DotColor;
                GUI.DrawTexture(dot, circleTexture);
                GUI.color = previous;
            }
        }
    }

    private void DrawNodes(Rect canvas)
    {
        float scale = GetScale(canvas);
        int current = GetCurrentUserIndex();

        for (int i = 0; i < levels.Count; i++)
        {
            bool complete = PuzzleProgress.IsComplete(levels[i].puzzleID);
            bool unlocked = i == 0 || PuzzleProgress.IsComplete(levels[i - 1].puzzleID);

            Color ring = complete ? CompleteGreen
                : unlocked && i == current ? RingCurrent
                : RingIdle;
            if (!unlocked) ring *= 0.5f;
            if (i == hoverIndex || i == draggedIndex) ring = Color.white;

            Vector2 center = ToCanvas(canvas, GetPosition(i));
            float radius = NodeRadius * scale;

            DrawCircle(new Vector2(center.x - radius, center.y - radius), radius * 2f, ring);
            DrawCircle(new Vector2(center.x - radius + 4f, center.y - radius + 4f), (radius - 4f) * 2f, GlassFill);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(24f * scale))
            };
            style.normal.textColor = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            Rect label = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            GUI.Label(label, (i + 1).ToString(), style);
        }
    }

    private Vector2 GetPosition(int index)
    {
        return levels[index].hasMapPosition
            ? levels[index].mapPosition
            : LevelNode.DefaultPosition(index, levels.Count);
    }

    private int HitTest(Rect canvas, Vector2 mouse)
    {
        Vector2 layoutMouse = ToLayout(canvas, mouse);
        int best = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < levels.Count; i++)
        {
            float distance = Vector2.Distance(layoutMouse, GetPosition(i));
            if (distance < HitRadius && distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }
        return best;
    }

    private int GetCurrentUserIndex()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (!PuzzleProgress.IsComplete(levels[i].puzzleID)) return i;
        }
        return -1;
    }

    private float GetScale(Rect canvas)
    {
        return Mathf.Min(canvas.width / (LayoutWidth + 80f), canvas.height / (LayoutHeight + 80f), 1f);
    }

    private Vector2 ToCanvas(Rect canvas, Vector2 layoutPosition)
    {
        float scale = GetScale(canvas);
        return new Vector2(
            canvas.center.x + (layoutPosition.x - viewOffset.x) * scale,
            canvas.center.y - (layoutPosition.y - viewOffset.y) * scale);
    }

    private Vector2 ToLayout(Rect canvas, Vector2 canvasPosition)
    {
        float scale = GetScale(canvas);
        return new Vector2(
            (canvasPosition.x - canvas.center.x) / scale + viewOffset.x,
            viewOffset.y - (canvasPosition.y - canvas.center.y) / scale);
    }

    private void DrawCircle(Vector2 topLeft, float size, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(topLeft.x, topLeft.y, size, size), circleTexture);
        GUI.color = previous;
    }

    private void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        bool steep = Mathf.Abs(to.y - from.y) > Mathf.Abs(to.x - from.x);
        int steps = Mathf.CeilToInt(steep ? Mathf.Abs(to.y - from.y) : Mathf.Abs(to.x - from.x));
        if (steps <= 0) return;

        for (int i = 0; i <= steps; i += 4)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            if (steep) EditorGUI.DrawRect(new Rect(point.x, point.y, 1f, 2f), color);
            else EditorGUI.DrawRect(new Rect(point.x, point.y, 2f, 1f), color);
        }
    }

    private void SaveLayout()
    {
        foreach (PuzzleData level in levels)
        {
            EditorUtility.SetDirty(level);
        }
        AssetDatabase.SaveAssets();
    }

    private static Texture2D MakeCircleTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
