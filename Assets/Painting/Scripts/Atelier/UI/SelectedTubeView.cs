using TMPro;
using UnityEngine;

namespace PaintingGame.Atelier
{
    // The large metal tube at the top: selected colour, remaining paint and its name.
    public sealed class SelectedTubeView : MonoBehaviour
    {
        public PaintToolController controller;
        public PaintTubeGraphic tube;
        public TMP_Text percentLabel;
        public TMP_Text nameLabel;

        private PaintColorDefinition shown;
        private int shownPercent = -1;

        private void Start()
        {
            controller.ColorChanged += OnColorChanged;
            controller.Inventory.Changed += OnTubeChanged;
            OnColorChanged(controller.CurrentColor);
        }

        private void OnDestroy()
        {
            if (controller == null) return;
            controller.ColorChanged -= OnColorChanged;
            if (controller.Inventory != null) controller.Inventory.Changed -= OnTubeChanged;
        }

        private void OnColorChanged(PaintColorDefinition color)
        {
            shown = color;
            if (nameLabel != null && color != null) nameLabel.text = color.displayName;
            shownPercent = -1;
            Refresh();
        }

        private void OnTubeChanged(PaintColorDefinition color) { if (color == shown) Refresh(); }


        private void Refresh()
        {
            if (shown == null) return;
            float fraction = controller.Inventory.Fraction(shown);
            if (tube != null) tube.SetState(fraction, shown.pigment);
            int percent = Mathf.CeilToInt(fraction * 100);
            if (percentLabel != null && percent != shownPercent)
            {
                shownPercent = percent;
                percentLabel.SetText("{0}%", percent);
            }
        }

    }
}
