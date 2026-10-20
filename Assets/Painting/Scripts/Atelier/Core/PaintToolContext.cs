using UnityEngine;

namespace PaintingGame.Atelier
{
    // What a tool needs while it works. Owned by PaintToolController; tools never search the scene.
    public sealed class PaintToolContext
    {
        public ImpastoPaintSurface Surface { get; }
        public TubeInventory Inventory { get; }
        public PaintColorDefinition Color { get; internal set; }
        public bool OutOfPaintThisStroke { get; private set; }
        public float SizeMultiplier { get; internal set; } = 1;
        public float Pressure { get; internal set; } = 0.75f;

        public PaintToolContext(ImpastoPaintSurface surface, TubeInventory inventory)
        {
            Surface = surface;
            Inventory = inventory;
        }

        public Color Pigment => Color != null ? Color.pigment : UnityEngine.Color.white;

        // Takes paint from the selected tube. Returns the granted amount (0 when the tube is empty).
        public float TakePaint(float units)
        {
            if (units <= 0 || Color == null || Inventory == null) return 0;
            float granted = Inventory.Spend(Color, units);
            if (granted < units * 0.999f) OutOfPaintThisStroke = true;
            return granted;
        }

        internal void BeginStroke() => OutOfPaintThisStroke = false;
    }
}
