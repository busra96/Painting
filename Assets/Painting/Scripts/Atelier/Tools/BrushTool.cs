using UnityEngine;

namespace PaintingGame.Atelier
{
    public sealed class BrushTool : PaintTool
    {
        private readonly BrushToolDefinition definition;
        private readonly ImpastoPaintSurface.BrushLoad load = new ImpastoPaintSurface.BrushLoad();
        private ImpastoPaintSurface.BrushDab dab;
        private float carry, travel;
        private Vector3 last;
        private float ContactRadius => WorkingSize * Mathf.Lerp(.72f,1.12f,Context.Pressure);

        public BrushTool(BrushToolDefinition definition) => this.definition = definition;
        public override PaintToolDefinition Definition => definition;

        public override void Begin(Vector3 point)
        {
            load.Clear();
            Direction = Vector2.zero;
            carry = travel = 0;
            last = point;
            float radius = ContactRadius;
            dab = new ImpastoPaintSurface.BrushDab
            {
                radius = radius, lengthRatio = definition.lengthRatio, squareness = definition.squareness,
                bristles = definition.bristles, bristleContrast = definition.bristleContrast,
                smudge = definition.smudge, release = definition.release,
                seed = Random.value * 100, order = Surface.NewLayerOrder(), direction = Vector2.up
            };
            // A touch-down leaves a small first dab, so tapping paints too.
            dab.lengthRatio = 1;
            Lay(point, Vector2.up, radius * .45f, definition.unitsPerCanvasWidth * 0.008f);
            dab.lengthRatio = definition.lengthRatio;
        }

        public override void Drag(Vector3 from, Vector3 to, float deltaTime)
        {
            if (!Surface.Contains(to)) return;
            last = to;
            TrackDirection(from, to);
            float distance = Surface.VisualDistance(from, to);
            float baseRadius = ContactRadius;
            if (distance <= 0.00001f)
            {
                // Resting on one spot slowly loads more paint there.
                float units = definition.unitsPerSecondStill * deltaTime;
                if (units > 0) Lay(to, Direction == Vector2.zero ? Vector2.up : Direction, baseRadius * 0.95f, units);
                return;
            }
            float speed = distance / Mathf.Max(deltaTime, 0.0001f) / Surface.CanvasWidth;
            float radius = baseRadius * (1 - definition.speedThinning * Mathf.Clamp01(speed / 3f));
            float spacing = Mathf.Max(radius * definition.spacing, Surface.CellSize);
            float unitsPerWorld = definition.unitsPerCanvasWidth / Surface.CanvasWidth;
            Vector2 direction = Direction == Vector2.zero ? Surface.ToVisual(to - from).normalized : Direction;
            float walked = spacing - carry;
            while (walked <= distance)
            {
                Vector3 point = Vector3.Lerp(from, to, Mathf.Clamp01(walked / distance));
                travel += spacing;
                Lay(point, direction, radius, unitsPerWorld * spacing);
                walked += spacing;
            }
            carry = distance - (walked - spacing);
        }

        public override void End(Vector3 point)
        {
            Surface.ReleaseBrush(last, load, dab.order, dab.radius * .85f);
        }

        private void Lay(Vector3 point, Vector2 direction, float radius, float units)
        {
            if (!Surface.Contains(point)) return;
            dab.position = point;
            dab.direction = direction;
            dab.radius = radius;
            dab.travel = travel / Mathf.Max(radius, 0.0001f) * 0.25f;
            float granted = Context.TakePaint(units);
            Surface.Brush(dab, Context.Pigment, granted, load);
        }
    }
}
