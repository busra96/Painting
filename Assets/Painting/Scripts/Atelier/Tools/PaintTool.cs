using UnityEngine;

namespace PaintingGame.Atelier
{
    // Runtime strategy for one tool. The controller feeds it pointer positions in world space.
    public abstract class PaintTool
    {
        protected PaintToolContext Context { get; private set; }
        protected ImpastoPaintSurface Surface => Context.Surface;
        public abstract PaintToolDefinition Definition { get; }
        protected float WorkingSize => Definition.WorldSize(Surface) * Context.SizeMultiplier;

        // Direction the tool is currently moving (for the cursor). Zero when it has not moved yet.
        public Vector2 Direction { get; protected set; }

        public void Bind(PaintToolContext context) => Context = context;
        public abstract void Begin(Vector3 point);
        // Called every frame while the pointer is down; from == to when the finger rests.
        public abstract void Drag(Vector3 from, Vector3 to, float deltaTime);
        public abstract void End(Vector3 point);

        protected void TrackDirection(Vector3 from, Vector3 to)
        {
            Vector2 delta = Context.Surface.ToVisual(to - from);
            if (delta.sqrMagnitude < 1e-8f) return;
            Direction = Direction == Vector2.zero ? delta.normalized : Vector2.Lerp(Direction, delta.normalized, 0.35f).normalized;
        }
    }
}
