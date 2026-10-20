using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Atelier/Tools/Press Pad", fileName = "Tool_Press")]
    public sealed class PressToolDefinition : PaintToolDefinition
    {
        [Tooltip("Squeeze per second while held still.")]
        [Range(0.1f, 6)] public float holdStrength = 2.2f;
        [Tooltip("Squeeze per press step while dragging.")]
        [Range(0, 1)] public float dragStrength = 0.35f;
        [Tooltip("Imprint texture frequency (cells).")]
        [Range(0.02f, 0.6f)] public float textureScale = 0.18f;

        public override PaintToolKind Kind => PaintToolKind.Press;
        public override bool UsesPaint => false;
        public override PaintTool CreateTool() => new PressTool(this);
    }
}
