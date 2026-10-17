using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

// Static tube artwork with a budget-driven pigment layer; no per-frame allocations.
[AddComponentMenu("UI/Paint Tube Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PaintTubeGraphic : MaskableGraphic
{
    public Color metalColor = new Color(0.83f, 0.81f, 0.72f);
    public Color paperColor = new Color(0.98f, 0.93f, 0.78f);
    public Color outlineColor = new Color(0.30f, 0.24f, 0.18f);
    [Range(0, 1)] public float previewRemaining = 1;
    public Color previewPigment = new Color(0.82f, 0.11f, 0.13f);
    public Sprite artwork;
    public override Texture mainTexture => artwork != null ? artwork.texture : base.mainTexture;
    private float remaining = 1;
    private Color pigment;
    public float DisplayedRemaining => Application.isPlaying ? remaining : previewRemaining;
    public Color Pigment => Application.isPlaying ? pigment : previewPigment;

    public void SetState(float fill, Color paint)
    {
        fill = Mathf.Clamp01(fill);
        if (Mathf.Abs(fill - remaining) < 0.00005f && paint == pigment) return;
        remaining = fill; pigment = paint;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float fill = DisplayedRemaining;
        Color paint = Pigment;
        if (artwork != null)
        {
            DrawPaintedTube(vh, fill, paint);
            return;
        }
        const int steps = 28;
        // Keep the foil and nozzle fixed while the pigment area follows the remaining amount.
        for (int i = 0; i < steps; i++)
        {
            float x0 = Mathf.Lerp(0.035f, 0.77f, i / (float)steps);
            float x1 = Mathf.Lerp(0.035f, 0.77f, (i + 1f) / steps);
            float h0 = HalfHeight(x0), h1 = HalfHeight(x1);
            Band(vh, x0, x1, -h0 - 0.025f, h0 + 0.025f, -h1 - 0.025f, h1 + 0.025f, outlineColor);
            Band(vh, x0, x1, -h0, -h0 * 0.53f, -h1, -h1 * 0.53f, Color.Lerp(metalColor, outlineColor, 0.18f));
            Band(vh, x0, x1, -h0 * 0.53f, h0 * 0.56f, -h1 * 0.53f, h1 * 0.56f, paperColor);
            Band(vh, x0, x1, h0 * 0.56f, h0, h1 * 0.56f, h1, metalColor);
            if (x0 > 0.19f && x1 < 0.66f)
            {
                float paintRight = Mathf.Lerp(0.19f, 0.66f, fill);
                float p0 = Mathf.Max(x0, 0.19f), p1 = Mathf.Min(x1, paintRight);
                if (p1 > p0)
                {
                    float t0 = Mathf.InverseLerp(x0, x1, p0), t1 = Mathf.InverseLerp(x0, x1, p1);
                    float ph0 = Mathf.Lerp(h0, h1, t0), ph1 = Mathf.Lerp(h0, h1, t1);
                    Band(vh, p0, p1, -ph0 * 0.64f, ph0 * 0.64f, -ph1 * 0.64f, ph1 * 0.64f, Color.Lerp(paint, paperColor, 0.14f));
                    Band(vh, p0, p1, ph0 * 0.40f, ph0 * 0.53f, ph1 * 0.40f, ph1 * 0.53f, Color.Lerp(paint, paperColor, 0.48f));
                }
                if (i % 4 == 0 && x0 < paintRight)
                    Band(vh, x0, x1, -h0 * 0.27f, -h0 * 0.16f, -h1 * 0.25f, -h1 * 0.14f, paperColor);
            }
        }
        // The folded end is fixed; only paint inside the tube changes with the budget.
        const float fold = 0.04f;
        float foldH = HalfHeight(fold) + 0.035f;
        Band(vh, fold - 0.014f, fold + 0.016f, -foldH, foldH, -foldH * 0.94f, foldH * 0.94f, outlineColor);
        Band(vh, fold - 0.008f, fold + 0.004f, -foldH * 0.83f, foldH * 0.83f, -foldH * 0.82f, foldH * 0.82f, metalColor);
        for (int i = 0; i < 7; i++)
        {
            float x = 0.055f + i * 0.088f;
            if (x > fold - 0.025f) break;
            Band(vh, x, x + 0.012f, -0.045f, 0.045f, -0.018f, 0.065f, Color.Lerp(metalColor, outlineColor, 0.35f));
        }
        // Shoulder, open threaded neck, and dark opening remain in the same place.
        Band(vh, 0.766f, 0.805f, -0.16f, 0.16f, -0.115f, 0.115f, outlineColor);
        Band(vh, 0.766f, 0.802f, -0.125f, 0.125f, -0.08f, 0.08f, metalColor);
        Band(vh, 0.80f, 0.87f, -0.105f, 0.105f, -0.105f, 0.105f, outlineColor);
        Band(vh, 0.80f, 0.862f, -0.075f, 0.075f, -0.075f, 0.075f, metalColor);
        for (int i = 0; i < 3; i++)
        {
            float x = 0.808f + i * 0.019f;
            Band(vh, x, x + 0.005f, -0.095f, 0.095f, -0.095f, 0.095f, outlineColor);
        }
    }

    private void DrawPaintedTube(VertexHelper vh, float fill, Color paint)
    {
        Vector4 uv = DataUtility.GetOuterUV(artwork);
        for (int i = 0; i < 40; i++)
        {
            float x0 = i / 40f, x1 = (i + 1) / 40f;
            TexturedBand(vh, uv, x0, x1, 0, 1, 0, 1, Color.white);
        }
        // Pigment occupies the metal interior; its torn brush edge follows the actual supply.
        float right = Mathf.Lerp(0.15f, 0.80f, fill);
        for (int i = 0; i < 32; i++)
        {
            float x0 = 0.15f + i * 0.65f / 32;
            if (x0 >= right) break;
            float x1 = Mathf.Min(right, x0 + 0.65f / 32);
            float bottom0 = 0.31f + Mathf.Sin(x0 * 127) * 0.012f;
            float bottom1 = 0.31f + Mathf.Sin(x1 * 127) * 0.012f;
            float top0 = 0.69f + Mathf.Sin(x0 * 93) * 0.010f;
            float top1 = 0.69f + Mathf.Sin(x1 * 93) * 0.010f;
            TexturedBand(vh, uv, x0, x1, bottom0, top0, bottom1, top1, paint);
            if (x0 > 0.23f && x1 < Mathf.Min(right, 0.57f))
                TexturedBand(vh, uv, x0, x1, 0.60f, 0.615f, 0.601f, 0.619f,
                    Color.Lerp(paint, Color.white, 0.53f));
        }
    }

    private void TexturedBand(VertexHelper vh, Vector4 uv, float x0, float x1, float b0, float t0,
        float b1, float t1, Color tint)
    {
        int start = vh.currentVertCount;
        AddTexturedVertex(vh, uv, x0, b0, tint);
        AddTexturedVertex(vh, uv, x0, t0, tint);
        AddTexturedVertex(vh, uv, x1, t1, tint);
        AddTexturedVertex(vh, uv, x1, b1, tint);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    private void AddTexturedVertex(VertexHelper vh, Vector4 uv, float x, float y, Color tint)
    {
        Rect r = rectTransform.rect;
        Vector3 position = new Vector3(r.xMin + x * r.width, r.yMin + y * r.height);
        Vector2 tex = new Vector2(Mathf.Lerp(uv.x, uv.z, x), Mathf.Lerp(uv.y, uv.w, y));
        vh.AddVert(position, tint * color, tex);
    }

    private static float HalfHeight(float x)
    {
        float shoulder = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.67f, 0.77f, x)) * 0.60f;
        return 0.065f + 0.29f * shoulder + Mathf.Sin(x * 83) * 0.006f;
    }

    private void Band(VertexHelper vh, float x0, float x1, float bottom0, float top0, float bottom1, float top1, Color tint)
    {
        Rect r = rectTransform.rect;
        Color c = tint * color;
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin + x0 * r.width, r.center.y + bottom0 * r.height), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x0 * r.width, r.center.y + top0 * r.height), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x1 * r.width, r.center.y + top1 * r.height), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + x1 * r.width, r.center.y + bottom1 * r.height), c, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
