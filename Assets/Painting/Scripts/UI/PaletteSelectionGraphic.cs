using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class PaletteSelectionGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? 1 : -1, sy = (i & 2) == 0 ? 1 : -1;
            Vector2 p = new Vector2(sx > 0 ? r.xMin : r.xMax, sy > 0 ? r.yMin : r.yMax);
            float scale = r.width / 128;
            Stroke(vh, p + new Vector2(sx * 3, sy * 2) * scale, p + new Vector2(sx * 31, sy * 3) * scale, 4.5f * scale);
            Stroke(vh, p + new Vector2(sx * 2, sy * 2) * scale, p + new Vector2(sx * 3, sy * 27) * scale, 4 * scale);
        }
    }

    private void Stroke(VertexHelper vh, Vector2 from, Vector2 to, float width)
    {
        Vector2 normal = new Vector2(-(to - from).y, (to - from).x).normalized * width * 0.5f;
        int start = vh.currentVertCount;
        vh.AddVert(from - normal, color, Vector2.zero); vh.AddVert(from + normal, color, Vector2.zero);
        vh.AddVert(to + normal * 0.7f, color, Vector2.zero); vh.AddVert(to - normal * 0.7f, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
    }
}
