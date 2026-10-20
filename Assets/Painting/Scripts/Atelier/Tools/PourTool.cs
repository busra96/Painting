using UnityEngine;

namespace PaintingGame.Atelier
{
    // The original mechanic as a tool: squeeze the tube; holding still builds a thicker heap.
    public sealed class PourTool : PaintTool
    {
        private readonly PourToolDefinition definition;

        public PourTool(PourToolDefinition definition) => this.definition = definition;
        public override PaintToolDefinition Definition => definition;

        public override void Begin(Vector3 point)
        {
            Direction = Vector2.zero;
            Pour(point, point, definition.unitsPerSecond * 0.08f);
        }

        public override void Drag(Vector3 from, Vector3 to, float deltaTime)
        {
            TrackDirection(from, to);
            float distance = Surface.VisualDistance(from, to);
            float units = Mathf.Max(distance * definition.unitsPerCanvasWidth / Surface.CanvasWidth, definition.unitsPerSecond * deltaTime);
            Pour(from, to, units);
        }

        public override void End(Vector3 point) { }

        private void Pour(Vector3 from, Vector3 to, float units)
        {
            float radius = WorkingSize;
            int steps = Mathf.Clamp(Mathf.CeilToInt(Surface.VisualDistance(from, to) / (radius * 0.4f)), 1, 64);
            Vector2 direction = Surface.ToVisual(to - from);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / (float)steps);
                if (!Surface.Contains(point)) continue;
                float granted = Context.TakePaint(units / steps);
                if (granted <= 0) return;
                Surface.DepositInside(point, Context.Pigment, granted, direction, radius);
            }
        }
    }
}
