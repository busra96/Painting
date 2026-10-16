using UnityEngine;
using UnityEngine.UI;

// All artwork and controls are serialized Canvas children. This component never builds UI.
[DefaultExecutionOrder(-500)]
[ExecuteAlways]
public sealed class WorkshopVisualController : MonoBehaviour
{
    public Camera mainCamera;
    public Canvas canvas;
    public SpriteRenderer paper;
    public RectTransform paintRegion;
    public Image backgroundImage;
    public PaintAmount paintAmount;
    public ColorSelection selection;
    public Image[] amountSegments;
    public RectTransform selectedMark;
    public RectTransform[] paletteButtons;
    public Color fullSegmentColor = new Color(0.82f, 0.16f, 0.10f);
    public Color emptySegmentColor = new Color(0.43f, 0.33f, 0.23f);

    public bool IsReady { get; private set; }
    public int AmountSegmentCount => amountSegments != null ? amountSegments.Length : 0;
    public Rect CurrentPaperViewport { get; private set; }
    private float previousRemaining = -1;
    private ColorType previousColor = (ColorType)(-1);
    private Vector2 markerOffset;
    private readonly Vector3[] regionCorners = new Vector3[4];
    private static readonly ColorType[] PaletteTypes = { ColorType.Red, ColorType.Yellow, ColorType.Green,
        ColorType.Pink, ColorType.Blue, ColorType.Orange };

    private void Awake()
    {
        if (paper != null && paper.TryGetComponent(out ImpastoPaintSurface surface))
            surface.RevealBackdropCanvas = true;
        // Marker placement is authored in the editor and retained as a relative offset.
        if (selectedMark != null && paletteButtons != null && paletteButtons.Length > 0)
            markerOffset = selectedMark.anchoredPosition - paletteButtons[0].anchoredPosition;
    }

    private void Start()
    {
        if (!Application.isPlaying) { RefreshLayout(); return; }
        if (mainCamera == null || canvas == null || paper == null || paintRegion == null ||
            selection == null || amountSegments == null || paletteButtons == null) return;
        RefreshLayout();
        IsReady = true;
        UpdateVisuals();
    }

    public void RefreshLayout()
    {
        if (mainCamera == null || canvas == null || paper == null || paintRegion == null) return;
        Canvas.ForceUpdateCanvases();
        paintRegion.GetWorldCorners(regionCorners);
        Vector3 min = mainCamera.WorldToViewportPoint(regionCorners[0]);
        Vector3 max = mainCamera.WorldToViewportPoint(regionCorners[2]);
        Rect viewport = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        if (viewport.width < 0.01f || viewport.height < 0.01f) return;
        CurrentPaperViewport = viewport;
        // Fit the unchanged simulation into the editor-authored region; never resize its mass grid.
        float worldHeight = paper.bounds.size.y / viewport.height;
        float worldWidth = paper.bounds.size.x / viewport.width;
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = worldHeight * 0.5f;
        mainCamera.aspect = worldWidth / worldHeight;
        Vector3 center = paper.bounds.center;
        mainCamera.transform.position = new Vector3(center.x - (viewport.center.x - 0.5f) * worldWidth,
            center.y - (viewport.center.y - 0.5f) * worldHeight, mainCamera.transform.position.z);
        Canvas.ForceUpdateCanvases();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) { RefreshLayout(); return; }
        if (IsReady) UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        float remaining = paintAmount != null ? paintAmount.RemainingFraction : 1;
        if (!Mathf.Approximately(previousRemaining, remaining))
        {
            for (int i = 0; i < amountSegments.Length; i++)
                if (amountSegments[i] != null)
                    amountSegments[i].color = Color.Lerp(emptySegmentColor, fullSegmentColor,
                        Mathf.Clamp01(remaining * amountSegments.Length - i));
            previousRemaining = remaining;
        }
        if (selectedMark == null) return;
        if (previousColor != selection.ColorType)
        {
            for (int i = 0; i < PaletteTypes.Length && i < paletteButtons.Length; i++)
                if (PaletteTypes[i] == selection.ColorType && paletteButtons[i] != null)
                {
                    selectedMark.anchoredPosition = paletteButtons[i].anchoredPosition + markerOffset;
                    break;
                }
            previousColor = selection.ColorType;
        }
        bool paletteVisible = selection.RedButton != null && selection.RedButton.gameObject.activeSelf;
        if (selectedMark.gameObject.activeSelf != paletteVisible) selectedMark.gameObject.SetActive(paletteVisible);
    }
}
