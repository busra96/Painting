using UnityEngine;

namespace PaintingGame.Atelier
{
    public sealed class KnifeTool : PaintTool
    {
        private readonly KnifeToolDefinition definition;
        private ImpastoPaintSurface.KnifeLoad load;
        private float bladeWidth;
        private Vector3 anchor;
        public Color CarriedColor => load != null ? load.PaintColor : Color.clear;
        public float CarriedAmount => load != null ? Mathf.Clamp01(load.ContactVolume / Mathf.Max(.001f,Surface.UnitVolume * .1f)) : 0;

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
            float pressure = Context.Pressure;
            Surface.KnifeSweep(anchor, to, bladeWidth, definition.filmScale * Mathf.Lerp(1.4f,0.55f,pressure),
                definition.pickup * Mathf.Lerp(0.55f,1,pressure), load);
            Direction = load.Direction;
            anchor = to;
        }

        public override void End(Vector3 point)
        {
            Drag(anchor, point, 0);
            Surface.EndKnife(load, bladeWidth);
        }
    }
}
