using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Atelier/Tools/Palette Knife", fileName = "Tool_Knife")]
    public sealed class KnifeToolDefinition : PaintToolDefinition
    {
        [Tooltip("Thickness of the film left behind, relative to Settings.FilmThickness.")]
        [Range(0.1f, 3f)] public float filmScale = 1;
        [Tooltip("Share of the wet paint the edge scrapes up each pass.")]
        [Range(0.1f, 0.98f)] public float pickup = 0.85f;

        public override PaintToolKind Kind => PaintToolKind.Knife;
        public override bool UsesPaint => false;
        public override PaintTool CreateTool() => new KnifeTool(this);
    }
}
