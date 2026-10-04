using UnityEngine;
using UnityEngine.UI;

public class ChamferedImage : Image
{
    [SerializeField] private float chamfer = 18f;
    [SerializeField] private float outlineThickness = 0f;

    public float Chamfer
    {
        get { return chamfer; }
        set
        {
            chamfer = value;
            SetVerticesDirty();
        }
    }

    // 0 = solid filled shape (default). Above 0 the shape is drawn as a ring
    // of this thickness, leaving the inside transparent.
    public float OutlineThickness
    {
        get { return outlineThickness; }
        set
        {
            outlineThickness = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float c = Mathf.Clamp(chamfer, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);

        if (outlineThickness > 0f)
        {
            PopulateOutline(vh, rect, c);
            return;
        }

        Vector2[] points = Octagon(rect.xMin, rect.xMax, rect.yMin, rect.yMax, c);

        UIVertex centerVertex = UIVertex.simpleVert;
        centerVertex.color = color;
        centerVertex.position = rect.center;
        vh.AddVert(centerVertex);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        foreach (Vector2 point in points)
        {
            vertex.position = point;
            vh.AddVert(vertex);
        }

        for (int i = 1; i <= points.Length; i++)
        {
            vh.AddTriangle(0, i, (i % points.Length) + 1);
        }
    }

    private void PopulateOutline(VertexHelper vh, Rect rect, float c)
    {
        float t = Mathf.Clamp(outlineThickness, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
        float ci = Mathf.Max(0f, c - t);

        Vector2[] outer = Octagon(rect.xMin, rect.xMax, rect.yMin, rect.yMax, c);
        Vector2[] inner = Octagon(rect.xMin + t, rect.xMax - t, rect.yMin + t, rect.yMax - t, ci);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        foreach (Vector2 point in outer)
        {
            vertex.position = point;
            vh.AddVert(vertex);
        }
        foreach (Vector2 point in inner)
        {
            vertex.position = point;
            vh.AddVert(vertex);
        }

        for (int i = 0; i < outer.Length; i++)
        {
            int j = (i + 1) % outer.Length;
            vh.AddTriangle(i, j, outer.Length + j);
            vh.AddTriangle(i, outer.Length + j, outer.Length + i);
        }
    }

    private static Vector2[] Octagon(float xMin, float xMax, float yMin, float yMax, float c)
    {
        return new Vector2[]
        {
            new Vector2(xMin + c, yMax),
            new Vector2(xMax - c, yMax),
            new Vector2(xMax, yMax - c),
            new Vector2(xMax, yMin + c),
            new Vector2(xMax - c, yMin),
            new Vector2(xMin + c, yMin),
            new Vector2(xMin, yMin + c),
            new Vector2(xMin, yMax - c)
        };
    }
}
