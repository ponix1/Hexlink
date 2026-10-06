using UnityEngine;
using UnityEngine.UI;

// A simple filled triangle, pointing along local +Y. Rotate the transform to
// aim it. Used by the tutorial popups as a pointer arrow.
public class ArrowImage : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        // Tip at top centre; base pulled in 15% per side for a slimmer triangle.
        Vector2 tip = new Vector2(0f, rect.yMax);
        Vector2 baseLeft = new Vector2(rect.xMin + rect.width * 0.15f, rect.yMin);
        Vector2 baseRight = new Vector2(rect.xMax - rect.width * 0.15f, rect.yMin);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = tip;
        vh.AddVert(vertex);
        vertex.position = baseRight;
        vh.AddVert(vertex);
        vertex.position = baseLeft;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
    }
}
