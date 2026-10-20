using UnityEngine;

namespace PaintingGame.Atelier
{
    public sealed class KnifeTool : PaintTool
    {
        private readonly KnifeToolDefinition definition;
        private ImpastoPaintSurface.KnifeLoad load;
        private float bladeWidth;
        private Vector3 anchor;

        public KnifeTool(KnifeToolDefinition definition) => this.definition = definition;
        public override PaintToolDefinition Definition => definition;

        public override void Begin(Vector3 point)
        {
            Direction = Vector2.zero;
            bladeWidth = WorkingSize;
            load = Surface.BeginKnife(load, bladeWidth);
            anchor = point;
        }

        public override void Drag(Vector3 from, Vector3 to, float deltaTime)
        {
            // Accumulate slow movement so a careful, slow pull still scrapes every cell it crosses.
            if (Surface.VisualDistance(anchor, to) < Surface.CellSize * 0.6f) return;
            TrackDirection(anchor, to);
            float pressure = Context.Pressure;
            Surface.KnifeSweep(anchor, to, bladeWidth, definition.filmScale * Mathf.Lerp(1.4f,0.55f,pressure),
                definition.pickup * Mathf.Lerp(0.55f,1,pressure), load);
            anchor = to;
        }

        public override void End(Vector3 point) => Surface.EndKnife(load, bladeWidth);
    }
}
