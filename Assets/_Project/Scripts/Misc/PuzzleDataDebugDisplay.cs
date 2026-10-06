// Dev tool: OnGUI overlay that dumps the selected puzzle's fields via
// reflection, to verify what a PuzzleData asset actually contains at runtime.
using UnityEngine;
using System.Reflection;
using System.Text;
using System.Collections;

// Draws a box in the top-right corner listing the runtime type and all
// public fields of PuzzleSelection.SelectedPuzzle.
public class PuzzleDataDebugDisplay : MonoBehaviour
{
    private const float BoxWidth = 320f;
    private const float BoxHeight = 260f;
    private const float Margin = 10f;

    // IMGUI draw pass; runs every frame but is cheap enough for a debug box.
    void OnGUI()
    {
        PuzzleData data = PuzzleSelection.SelectedPuzzle;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;

        float boxX = Screen.width - BoxWidth - Margin;
        float boxY = Margin;

        GUI.Box(new Rect(boxX, boxY, BoxWidth, BoxHeight), "");

        if (data == null)
        {
            GUI.Label(new Rect(boxX + 10, boxY + 10, BoxWidth - 20, 20), "PuzzleSelection.SelectedPuzzle is NULL", style);
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Runtime type: {data.GetType().Name}");
        sb.AppendLine();

        // Walk up the inheritance chain so we catch fields declared on
        // PuzzleData itself as well as on whatever subclass this is.
        System.Type currentType = data.GetType();
        while (currentType != null && currentType != typeof(ScriptableObject))
        {
            FieldInfo[] fields = currentType.GetFields(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(data);
                string valueString = FormatValue(value);
                sb.AppendLine($"{field.Name}: {valueString}");
            }

            currentType = currentType.BaseType;
        }

        GUI.Label(new Rect(boxX + 10, boxY + 10, BoxWidth - 20, BoxHeight - 20), sb.ToString(), style);
    }

    // Human-friendly value formatting for the dump.
    private string FormatValue(object value)
    {
        if (value == null) return "null";

        // Lists print each element instead of just the type name.
        if (value is IList list && !(value is string))
        {
            StringBuilder listSb = new StringBuilder("[");
            for (int i = 0; i < list.Count; i++)
            {
                listSb.Append(list[i]);
                if (i < list.Count - 1) listSb.Append(", ");
            }
            listSb.Append("]");
            return listSb.ToString();
        }

        return value.ToString();
    }
}