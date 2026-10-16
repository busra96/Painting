using UnityEngine;
using UnityEngine.UI;

// Small deformable UI mesh: no generated textures, particles or per-frame allocations.
[AddComponentMenu("UI/Paint Tube Graphic")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PaintTubeGraphic : MaskableGraphic
{
    public Color metalColor = new Color(0.83f, 0.81f, 0.72f);
    public Color paperColor = new Color(0.98f, 0.93f, 0.78f);
    public Color outlineColor = new Color(0.30f, 0.24f, 0.18f);
    [Range(0, 1)] public float previewRemaining = 1;
    public Color previewPigment = new Color(0.82f, 0.11f, 0.13f);
    private float remaining = 1, pressure;
    private Color pigment;
    public float DisplayedRemaining => Application.isPlaying ? remaining : previewRemaining;
    public float SqueezePressure => pressure;
    public Color Pigment => Application.isPlaying ? pigment : previewPigment;

    public void SetState(float fill, float squeeze, Color paint)
    {
        fill = Mathf.Clamp01(fill);
        squeeze = Mathf.Clamp01(squeeze);
        if (Mathf.Abs(fill - remaining) < 0.00005f && Mathf.Abs(squeeze - pressure) < 0.0001f && paint == pigment) return;
        remaining = fill; pressure = squeeze; pigment = paint;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float fill = DisplayedRemaining;
        Color paint = Pigment;
        float squeeze = Application.isPlaying ? pressure : 0;
        const int steps = 28;
        // The spent part stays as flattened foil, rather than disappearing like a bar.
        for (int i = 0; i < steps; i++)
        {
            float x0 = Mathf.Lerp(0.035f, 0.77f, i / (float)steps);
            float x1 = Mathf.Lerp(0.035f, 0.77f, (i + 1f) / steps);
            float h0 = HalfHeight(x0, fill, squeeze), h1 = HalfHeight(x1, fill, squeeze);
            Band(vh, x0, x1, -h0 - 0.025f, h0 + 0.025f, -h1 - 0.025f, h1 + 0.025f, outlineColor);
            Band(vh, x0, x1, -h0, -h0 * 0.53f, -h1, -h1 * 0.53f, Color.Lerp(metalColor, outlineColor, 0.18f));
            Band(vh, x0, x1, -h0 * 0.53f, h0 * 0.56f, -h1 * 0.53f, h1 * 0.56f, paperColor);
            Band(vh, x0, x1, h0 * 0.56f, h0, h1 * 0.56f, h1, metalColor);
            if (x0 > 0.19f && x1 < 0.66f)
            {
                Band(vh, x0, x1, -h0 * 0.64f, h0 * 0.64f, -h1 * 0.64f, h1 * 0.64f, Color.Lerp(paint, paperColor, 0.14f));
                Band(vh, x0, x1, h0 * 0.40f, h0 * 0.53f, h1 * 0.40f, h1 * 0.53f, Color.Lerp(paint, paperColor, 0.48f));
                if (i % 4 == 0)
                    Band(vh, x0, x1, -h0 * 0.27f, -h0 * 0.16f, -h1 * 0.25f, -h1 * 0.14f, paperColor);
            }
        }
        // Rolled seam and creases travel toward the fixed shoulder as the tube empties.
        float fold = Mathf.Lerp(0.04f, 0.715f, 1 - fill);
        float foldH = HalfHeight(fold, fill, squeeze) + 0.035f;
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
        if (squeeze > 0.001f)
        {
            float tip = 0.87f + squeeze * 0.115f;
            float thickness = 0.025f + squeeze * 0.025f;
            Band(vh, 0.863f, tip, -thickness, thickness, -thickness * 1.25f - 0.025f, thickness * 0.8f - 0.025f, paint);
            Band(vh, 0.868f, tip - 0.003f, thickness * 0.12f, thickness * 0.48f,
                -0.02f, thickness * 0.35f - 0.025f, Color.Lerp(paint, paperColor, 0.45f));
        }
    }

    private static float HalfHeight(float x, float fill, float squeeze)
    {
        float front = Mathf.Lerp(-0.035f, 0.715f, 1 - fill);
        float inflated = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(front - 0.02f, front + 0.065f, x));
        float shoulder = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.67f, 0.77f, x)) * 0.60f;
        float fullness = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 0.09f, fill));
        float dent = squeeze * Mathf.Exp(-Mathf.Pow((x - (front + 0.08f)) * 13, 2)) * 0.09f;
        return 0.065f + inflated * fullness * (0.29f * shoulder - dent) + Mathf.Sin(x * 83) * 0.006f;
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
