using UnityEngine;

namespace PaintingGame.Atelier
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    public sealed class GallerySafeArea : MonoBehaviour
    {
        public RectTransform composition;
        private Rect last;
        private Vector2 screen;
        private Vector2 parentSize;
        private void OnEnable() => Refresh();
        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (last != Screen.safeArea || screen.x != Screen.width || screen.y != Screen.height || parent != null && parentSize != parent.rect.size) Refresh();
        }
        public void Refresh()
        {
            last = Screen.safeArea; screen = new Vector2(Screen.width, Screen.height);
            if (screen.x <= 0 || screen.y <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = last.min / screen; rect.anchorMax = last.max / screen;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var parent = transform.parent as RectTransform;
            parentSize = parent != null ? parent.rect.size : screen;
            if (composition == null) return;
            composition.anchorMin = composition.anchorMax = composition.pivot = Vector2.one * .5f;
            composition.anchoredPosition = Vector2.zero;
            composition.sizeDelta = new Vector2(1080,1920);
            float scale = Mathf.Min(rect.rect.width / 1080f, rect.rect.height / 1920f);
            composition.localScale = Vector3.one * Mathf.Max(.0001f,scale);
        }
    }
}
