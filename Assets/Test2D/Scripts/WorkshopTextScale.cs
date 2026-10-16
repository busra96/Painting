using TMPro;
using UnityEngine;

// Keeps authored type proportions when the camera's painting projection changes Canvas units.
[ExecuteAlways]
public sealed class WorkshopTextScale : MonoBehaviour
{
    public RectTransform referenceRoot;
    public TMP_Text label;
    public float fontSizeAt941 = 32;
    private void OnEnable() { Canvas.preWillRenderCanvases += Refresh; Refresh(); }
    private void OnDisable() => Canvas.preWillRenderCanvases -= Refresh;
    public void Refresh()
    {
        if (referenceRoot == null || label == null) return;
        float scale = referenceRoot.rect.width / 941f;
        float size = fontSizeAt941 * scale;
        if (label.enableAutoSizing)
        {
            if (!Mathf.Approximately(label.fontSizeMax, size)) label.fontSizeMax = size;
            float min = Mathf.Min(18, fontSizeAt941) * scale;
            if (!Mathf.Approximately(label.fontSizeMin, min)) label.fontSizeMin = min;
        }
        else if (!Mathf.Approximately(label.fontSize, size)) label.fontSize = size;
    }
}
