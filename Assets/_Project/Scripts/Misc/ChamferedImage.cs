using UnityEngine;
using UnityEngine.UI;

public class ChamferedImage : Image
{
    [SerializeField] private float chamfer = 18f;

    public float Chamfer
    {
        get { return chamfer; }
        set
        {
            chamfer = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float c = Mathf.Clamp(chamfer, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);

        Vector2[] points =
        {
            new Vector2(rect.xMin + c, rect.yMax),
            new Vector2(rect.xMax - c, rect.yMax),
            new Vector2(rect.xMax, rect.yMax - c),
            new Vector2(rect.xMax, rect.yMin + c),
            new Vector2(rect.xMax - c, rect.yMin),
            new Vector2(rect.xMin + c, rect.yMin),
            new Vector2(rect.xMin, rect.yMin + c),
            new Vector2(rect.xMin, rect.yMax - c)
        };

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
}
