using UnityEngine;

namespace PaintingGame.Atelier
{
    public enum PaintToolKind { Brush, Knife, Press, Pour, Pencil }

    // Base for every hand tool. Each definition is a data asset that creates its own runtime strategy, so a new
    // brush or knife is added by creating an asset and putting it in the AtelierConfig list, without code changes.
    public abstract class PaintToolDefinition : ScriptableObject
    {
        [Tooltip("Stable save key.")]
        public string id;
        public string displayName;
        [TextArea(1, 3)] public string hint;
        public Sprite icon;
        [Header("Cursor shown on the canvas")]
        public Sprite cursor;
        [Tooltip("Optional overlay of the cursor that is tinted with the selected paint.")]
        public Sprite cursorTip;
        [Tooltip("Cursor size as a fraction of the tool's working size.")]
        public float cursorScale = 1;
        [Tooltip("Rotate the cursor to follow the stroke direction.")]
        public bool cursorFollowsStroke = true;
        [Header("Size")]
        [Tooltip("Working size as a fraction of the canvas width (brush/press radius, blade width, nozzle radius).")]
        [Range(0.005f, 0.6f)] public float size = 0.05f;

        public abstract PaintToolKind Kind { get; }
        public virtual bool UsesPaint => true;
        // Size in the surface's visual units (on-screen proportions; 1 canvas width = CanvasWidth).
        public float WorldSize(ImpastoPaintSurface surface) => size * (surface != null ? surface.CanvasWidth : 1);
        public abstract PaintTool CreateTool();
    }
}
