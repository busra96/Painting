using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName="Painting/Atelier/Tools/Sketch Pencil",fileName="Tool_Pencil")]
    public sealed class PencilToolDefinition:PaintToolDefinition
    {
        public Color graphite=new Color(.13f,.12f,.11f);
        [Min(0)] public float graphitePerCanvasWidth=.06f;
        public override PaintToolKind Kind=>PaintToolKind.Pencil;
        public override bool UsesPaint=>false;
        public override PaintTool CreateTool()=>new PencilTool(this);
    }
}
