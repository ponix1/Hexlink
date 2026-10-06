// Custom UI graphic: a pointy-top hexagon, optionally drawn as an outline ring.
using UnityEngine;
using UnityEngine.UI;

// Procedural hexagon replacement for UI Image: no sprite needed, and ring
// mode gives clean hex outlines of any thickness.
public class HexagonImage : Image
{
    [SerializeField] private float outlineThickness = 0f;

    // 0 = solid hexagon. Above 0 the hexagon is drawn as a ring of this
    // thickness, leaving the inside transparent.
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
        float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
        Vector2 center = rect.center;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        // Solid mode: centre vertex fanned out to the six corners.
        if (outlineThickness <= 0f)
        {
            Vector2[] outerPoints = HexPoints(center, outer);

            vh.AddVert(new UIVertex { position = center, color = color });
            foreach (Vector2 point in outerPoints)
            {
                vertex.position = point;
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= outerPoints.Length; i++)
            {
                vh.AddTriangle(0, i, (i % outerPoints.Length) + 1);
            }
            return;
        }

        // Ring mode: stitch outer + inner hexagons into quad strips. Inner
        // radius clamped to >= 25% of outer so a huge thickness can't invert
        // the ring.
        float inner = Mathf.Clamp(outer - outlineThickness, outer * 0.25f, outer);
        Vector2[] outerRing = HexPoints(center, outer);
        Vector2[] innerRing = HexPoints(center, inner);

        foreach (Vector2 point in outerRing)
        {
            vertex.position = point;
            vh.AddVert(vertex);
        }
        foreach (Vector2 point in innerRing)
        {
            vertex.position = point;
            vh.AddVert(vertex);
        }

        for (int i = 0; i < outerRing.Length; i++)
        {
            int j = (i + 1) % outerRing.Length;
            vh.AddTriangle(i, j, outerRing.Length + j);
            vh.AddTriangle(i, outerRing.Length + j, outerRing.Length + i);
        }
    }

    // Pointy-top hexagon, clockwise winding.
    private static Vector2[] HexPoints(Vector2 center, float radius)
    {
        return new Vector2[]
        {
            new Vector2(center.x, center.y + radius),
            new Vector2(center.x + radius * 0.866f, center.y + radius * 0.5f),
            new Vector2(center.x + radius * 0.866f, center.y - radius * 0.5f),
            new Vector2(center.x, center.y - radius),
            new Vector2(center.x - radius * 0.866f, center.y - radius * 0.5f),
            new Vector2(center.x - radius * 0.866f, center.y + radius * 0.5f)
        };
    }
}
