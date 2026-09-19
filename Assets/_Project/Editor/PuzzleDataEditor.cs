using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(PuzzleData), true)]
public class PuzzleDataEditor : Editor
{
    private const string RadiusKey = "PuzzleDataEditor.Radius";
    private const float CellWidth = 30f;
    private const float CellHeight = 26f;
    private const float GapX = 2f;
    private const float GapY = 2f;

    private static readonly Color FilledColor = new Color(0.36f, 0.72f, 0.85f);
    private static readonly Color EmptyColor = new Color(0.86f, 0.86f, 0.86f);

    private HashSet<long> filled = new HashSet<long>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name != "layoutCells")
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        DrawHexPainter();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHexPainter()
    {
        SerializedProperty cells = serializedObject.FindProperty("layoutCells");
        CollectFilled(cells);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Board Layout", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click a hex to add or remove it from the board.", MessageType.None);

        int radius = SessionState.GetInt(RadiusKey, 3);
        radius = EditorGUILayout.IntSlider("Paint radius", radius, 1, 6);
        SessionState.SetInt(RadiusKey, radius);

        // Rows top to bottom (r = +radius .. -radius), each row offset by half a hex
        // like the in-game pointy-top layout (world x is proportional to q + r/2).
        for (int r = radius; r >= -radius; r--)
        {
            int qMin = Mathf.Max(-radius, -r - radius);
            int qMax = Mathf.Min(radius, radius - r);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space((qMin + r * 0.5f + radius) * (CellWidth + GapX));

            for (int q = qMin; q <= qMax; q++)
            {
                bool isFilled = filled.Contains(Key(q, r));
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = isFilled ? FilledColor : EmptyColor;
                bool clicked = GUILayout.Button(isFilled ? "\u25CF" : "", GUILayout.Width(CellWidth), GUILayout.Height(CellHeight));
                GUI.backgroundColor = previous;

                if (clicked)
                {
                    ToggleCell(cells, q, r);
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(GapY);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Cells", cells.arraySize.ToString());
    }

    private void CollectFilled(SerializedProperty cells)
    {
        filled.Clear();
        for (int i = 0; i < cells.arraySize; i++)
        {
            SerializedProperty cell = cells.GetArrayElementAtIndex(i);
            filled.Add(Key(
                cell.FindPropertyRelative("coordinate.q").intValue,
                cell.FindPropertyRelative("coordinate.r").intValue));
        }
    }

    private void ToggleCell(SerializedProperty cells, int q, int r)
    {
        for (int i = 0; i < cells.arraySize; i++)
        {
            SerializedProperty cell = cells.GetArrayElementAtIndex(i);
            if (cell.FindPropertyRelative("coordinate.q").intValue == q
                && cell.FindPropertyRelative("coordinate.r").intValue == r)
            {
                cells.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                CollectFilled(cells);
                return;
            }
        }

        cells.InsertArrayElementAtIndex(cells.arraySize);
        SerializedProperty added = cells.GetArrayElementAtIndex(cells.arraySize - 1);
        added.FindPropertyRelative("coordinate.q").intValue = q;
        added.FindPropertyRelative("coordinate.r").intValue = r;
        serializedObject.ApplyModifiedProperties();
        CollectFilled(cells);
    }

    private static long Key(int q, int r)
    {
        return ((long)q << 32) | (uint)r;
    }
}
