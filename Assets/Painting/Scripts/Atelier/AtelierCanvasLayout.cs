using UnityEngine;

namespace PaintingGame.Atelier
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-450)]
    public sealed class AtelierCanvasLayout : MonoBehaviour
    {
        public Camera paintCamera;
        public Canvas canvas;
        public RectTransform paperRegion;
        public SpriteRenderer paper, backdrop;
        private readonly Vector3[] corners = new Vector3[4];
        private int previousWidth, previousHeight;

        private void Start() => RefreshLayout();
        private void LateUpdate()
        {
            if (!Application.isPlaying || previousWidth != Screen.width || previousHeight != Screen.height)
                RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (paintCamera == null || canvas == null || paperRegion == null || paper == null) return;
            Canvas.ForceUpdateCanvases();
            paperRegion.GetWorldCorners(corners);
            // Overlay Canvas coordinates are screen pixels. Keep the simulation transform fixed.
            float sw = Mathf.Max(1, canvas.pixelRect.width), sh = Mathf.Max(1, canvas.pixelRect.height);
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            Vector2 size = new Vector2((max.x - min.x) / sw, (max.y - min.y) / sh);
            if (size.x <= 0.01f || size.y <= 0.01f) return;
            Vector2 center = (min + max) * 0.5f / new Vector2(sw, sh);
            float aspect = sw / sh;
            float worldHeight = Mathf.Max(paper.bounds.size.y / size.y, paper.bounds.size.x / (size.x * aspect));
            float worldWidth = worldHeight * aspect;
            paintCamera.orthographic = true;
            paintCamera.orthographicSize = worldHeight * 0.5f;
            paintCamera.aspect = aspect;
            Vector3 paintCenter = paper.bounds.center;
            paintCamera.transform.position = new Vector3(paintCenter.x - (center.x - 0.5f) * worldWidth,
                paintCenter.y - (center.y - 0.5f) * worldHeight, -20f);
            if (backdrop != null)
            {
                backdrop.transform.position = new Vector3(paintCamera.transform.position.x, paintCamera.transform.position.y, 2);
                Vector2 artSize = backdrop.sprite.bounds.size;
                float scale = Mathf.Max(worldWidth / artSize.x, worldHeight / artSize.y) * 1.04f;
                backdrop.transform.localScale = Vector3.one * scale;
            }
            previousWidth = Screen.width; previousHeight = Screen.height;
        }
    }
}
