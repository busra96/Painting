using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Atelier
{
    // Builds the tool tray from AtelierConfig.tools and mirrors the controller's selection.
    public sealed class ToolTrayView : MonoBehaviour
    {
        public PaintToolController controller;
        public ToolButtonView template;
        public RectTransform container;

        private readonly List<ToolButtonView> buttons = new List<ToolButtonView>();

        private void Start()
        {
            template.gameObject.SetActive(false);
            foreach (PaintToolDefinition tool in controller.config.tools)
            {
                if (tool == null) continue;
                ToolButtonView button = Instantiate(template, container);
                button.gameObject.SetActive(true);
                PaintToolDefinition captured = tool;
                button.Setup(tool, () => controller.SelectTool(captured));
                buttons.Add(button);
            }
            controller.ToolChanged += Highlight;
            Highlight(controller.CurrentTool);
        }

        private void OnDestroy() { if (controller != null) controller.ToolChanged -= Highlight; }

        private void Highlight(PaintToolDefinition tool)
        {
            foreach (ToolButtonView button in buttons) button.SetSelected(button.Tool == tool);
        }
    }
}
