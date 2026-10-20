using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Atelier
{
    // Root data asset of the painting atelier: which tools and colours exist and the game rules around them.
    [CreateAssetMenu(menuName = "Painting/Atelier/Atelier Config", fileName = "AtelierConfig")]
    public sealed class AtelierConfig : ScriptableObject
    {
        public List<PaintToolDefinition> tools = new List<PaintToolDefinition>();
        public List<PaintColorDefinition> colors = new List<PaintColorDefinition>();
        [Tooltip("Tool selected when the atelier opens.")]
        public PaintToolDefinition defaultTool;
        [Header("Rules")]
        [Tooltip("Finished artworks needed to open an exhibition.")]
        [Min(1)] public int artworksForExhibition = 15;
        [Tooltip("Match score (0-100) for 1, 2 and 3 stars.")]
        public Vector3 starThresholds = new Vector3(35, 60, 82);
        [Range(0, 1)] public float lowTubeWarning = 0.2f;
        [Header("Touch")]
        [Tooltip("On touch screens the tool works this far above the finger (fraction of canvas width) so the finger does not hide it.")]
        [Range(0, 0.15f)] public float touchLift = 0.05f;
        [Header("Development")]
        [Tooltip("Shows a refill button on empty tubes until the market exists.")]
        public bool allowTestRefill = true;

        public PaintColorDefinition ColorFor(ColorType slot)
        {
            foreach (PaintColorDefinition color in colors)
                if (color != null && color.paletteSlot == slot) return color;
            return colors.Count > 0 ? colors[0] : null;
        }

        public int StarsFor(float score) => score >= starThresholds.z ? 3 : score >= starThresholds.y ? 2 : score >= starThresholds.x ? 1 : 0;
    }
}
