using UnityEngine;

namespace PaintingGame.Atelier
{
    // Keeps the palette gauges in sync with the tube inventory. Event driven; no Update.
    public sealed class TubeRackView : MonoBehaviour
    {
        public PaintToolController controller;
        public TubeGaugeView[] gauges;

        private void Start()
        {
            controller.Inventory.Changed += Refresh;
            foreach (PaintColorDefinition color in controller.Inventory.Colors) Refresh(color);
        }

        private void OnDestroy() { if (controller != null && controller.Inventory != null) controller.Inventory.Changed -= Refresh; }

        private void Refresh(PaintColorDefinition color)
        {
            if (color == null) return;
            float fraction = controller.Inventory.Fraction(color);
            foreach (TubeGaugeView gauge in gauges)
                if (gauge != null && gauge.slot == color.paletteSlot) gauge.Show(fraction, controller.config.lowTubeWarning);
        }
    }
}
