using UnityEngine;

namespace PaintingGame.Gallery
{
// Renders the camera once into an off-screen texture and crops the paper, so the saved image shows the
// lit impasto relief and gloss exactly as on screen. Screen Space Overlay canvases (the gallery) are not
// part of a camera render and never appear in the picture.
public static class PaintingCapture
{
    public static Texture2D CapturePaper(Camera camera, SpriteRenderer paper, int maxWidth = 1024)
    {
        if (camera == null || paper == null) return null;
        if (ImpastoPaintSurface.Active != null) ImpastoPaintSurface.Active.Flush();
        int screenWidth = Mathf.Max(1, camera.pixelWidth), screenHeight = Mathf.Max(1, camera.pixelHeight);
        Bounds bounds = paper.bounds;
        Vector3 min = camera.WorldToViewportPoint(bounds.min), max = camera.WorldToViewportPoint(bounds.max);
        float minX = Mathf.Clamp01(Mathf.Min(min.x, max.x)), minY = Mathf.Clamp01(Mathf.Min(min.y, max.y));
        float maxX = Mathf.Clamp01(Mathf.Max(min.x, max.x)), maxY = Mathf.Clamp01(Mathf.Max(min.y, max.y));
        int paperPixelWidth = Mathf.Max(1, Mathf.RoundToInt((maxX - minX) * screenWidth));
        float scale = Mathf.Min(1f, Mathf.Max(64, maxWidth) / (float)paperPixelWidth);
        int renderWidth = Mathf.Max(1, Mathf.RoundToInt(screenWidth * scale));
        int renderHeight = Mathf.Max(1, Mathf.RoundToInt(screenHeight * scale));
        int x0 = Mathf.Clamp(Mathf.RoundToInt(minX * renderWidth), 0, renderWidth - 1);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(minY * renderHeight), 0, renderHeight - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(maxX * renderWidth), x0 + 1, renderWidth);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(maxY * renderHeight), y0 + 1, renderHeight);

        RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
        RenderTexture frame = RenderTexture.GetTemporary(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
        Texture2D crop = new Texture2D(x1 - x0, y1 - y0, TextureFormat.RGB24, false) { name = "Saved painting" };
        try
        {
            camera.targetTexture = frame;
            camera.Render();
            RenderTexture.active = frame;
            crop.ReadPixels(new Rect(x0, y0, crop.width, crop.height), 0, 0);
            crop.Apply(false);
        }
        catch
        {
            Object.Destroy(crop);
            throw;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(frame);
        }
        return crop;
    }

    // GPU bilinear resize; caller owns the result.
    public static Texture2D Resize(Texture2D source, int width, int height)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var result = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            FilterMode filter = source.filterMode;
            source.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, target);
            source.filterMode = filter;
            RenderTexture.active = target;
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(false);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
        return result;
    }
}
}
