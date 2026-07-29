using UnityEngine;
using System.Reflection;
using System.Text;
using System.Collections;

public class PuzzleDataDebugDisplay : MonoBehaviour
{
    void OnGUI()
    {
        PuzzleData data = PuzzleSelection.SelectedPuzzle;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(10, 10, 500, 400), "");

        if (data == null)
        {
            GUI.Label(new Rect(20, 20, 480, 30), "PuzzleSelection.SelectedPuzzle is NULL", style);
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

        GUI.Label(new Rect(20, 20, 480, 380), sb.ToString(), style);
    }

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