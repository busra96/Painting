using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Atelier/Tools/Tube Pour", fileName = "Tool_Pour")]
    public sealed class PourToolDefinition : PaintToolDefinition
    {
        [Min(0.1f)] public float unitsPerSecond = 7;
        [Tooltip("Minimum paint per canvas-width of movement.")]
        [Min(0)] public float unitsPerCanvasWidth = 15;

        public override PaintToolKind Kind => PaintToolKind.Pour;
        public override PaintTool CreateTool() => new PourTool(this);
    }
}
