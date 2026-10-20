using UnityEngine;

namespace PaintingGame.Atelier
{
    public sealed class PressTool : PaintTool
    {
        private readonly PressToolDefinition definition;
        private float seed;
        private float spacingCarry;
        private Vector3 lastContact;

        public PressTool(PressToolDefinition definition) => this.definition = definition;
        public override PaintToolDefinition Definition => definition;

        public override void Begin(Vector3 point)
        {
            Direction = Vector2.zero;
            seed = Random.value * 50;
            spacingCarry = 0;
            Press(point, definition.dragStrength);
        }

        public override void Drag(Vector3 from, Vector3 to, float deltaTime)
        {
            float distance = Surface.VisualDistance(from,to);
            if (distance < Surface.CellSize * .1f)
            {
                Press(to, definition.holdStrength * deltaTime);
                return;
            }
            TrackDirection(from, to);
            float spacing = Mathf.Max(Surface.CellSize,WorkingSize * .22f);
            float next = spacing - spacingCarry;
            for (;next <= distance;next += spacing)
                Press(Vector3.Lerp(from,to,next/distance),definition.dragStrength);
            spacingCarry = distance - (next-spacing);
        }

        public override void End(Vector3 point)
        {
            if(Surface.VisualDistance(lastContact,point)>Surface.CellSize*.25f)Press(point,definition.dragStrength);
        }

        private void Press(Vector3 point, float strength)
        {
            Surface.Press(point, WorkingSize, strength * Mathf.Lerp(.35f,1.35f,Context.Pressure), definition.textureScale, seed);
            lastContact=point;
        }
    }
}
