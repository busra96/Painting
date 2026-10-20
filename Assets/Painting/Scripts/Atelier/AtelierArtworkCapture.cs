using UnityEngine;

namespace PaintingGame.Atelier
{
    // Fit a camera directly to the paper for one render, independent of Game View aspect or UI layout.
    public static class AtelierArtworkCapture
    {
        public static Texture2D Capture(Camera camera, SpriteRenderer paper, int width = 448)
        {
            Bounds bounds = paper.bounds;
            int height = Mathf.Max(1, Mathf.RoundToInt(width * bounds.size.y / bounds.size.x));
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;
            Vector3 position = camera.transform.position;
            float size = camera.orthographicSize, aspect = camera.aspect;
            bool orthographic = camera.orthographic;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.orthographic = true;
                camera.orthographicSize = bounds.size.y * 0.5f;
                camera.aspect = bounds.size.x / bounds.size.y;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, position.z);
                camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(false);
                return image;
            }
            catch { Object.Destroy(image); throw; }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                camera.orthographic = orthographic; camera.orthographicSize = size;
                camera.aspect = aspect; camera.transform.position = position;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
