using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Atelier/Tools/Brush", fileName = "Tool_Brush")]
    public sealed class BrushToolDefinition : PaintToolDefinition
    {
        [Header("Shape")]
        [Tooltip("Footprint length along the stroke relative to its width. Flat brushes ≈ 0.35, round ≈ 1.")]
        [Range(0.15f, 1.5f)] public float lengthRatio = 0.4f;
        [Range(0, 1)] public float squareness = 0.7f;
        [Range(2, 60)] public float bristles = 18;
        [Range(0, 1)] public float bristleContrast = 0.6f;
        [Header("Paint")]
        [Tooltip("Tube paint used per canvas-width of stroke.")]
        [Min(0)] public float unitsPerCanvasWidth = 6;
        [Tooltip("Extra paint while the brush rests on one spot, per second.")]
        [Min(0)] public float unitsPerSecondStill = 0.6f;
        [Range(0, 1)] public float smudge = 0.45f;
        [Range(0, 1)] public float release = 0.35f;
        [Tooltip("Distance between dabs relative to the brush radius.")]
        [Range(0.08f, 0.6f)] public float spacing = 0.22f;
        [Tooltip("Fast strokes get thinner, like a lighter touch.")]
        [Range(0, 0.6f)] public float speedThinning = 0.25f;

        public override PaintToolKind Kind => PaintToolKind.Brush;
        public override PaintTool CreateTool() => new BrushTool(this);
    }
}
