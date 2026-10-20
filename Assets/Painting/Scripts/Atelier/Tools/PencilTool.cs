using UnityEngine;

namespace PaintingGame.Atelier
{
    // A thin graphite sketch underneath oil paint. It uses no tube paint and stays below the
    // palette knife's working film height, so drawing a guide does not create a wet oil ridge.
    public sealed class PencilTool:PaintTool
    {
        private readonly PencilToolDefinition definition;
        private readonly ImpastoPaintSurface.BrushLoad load=new ImpastoPaintSurface.BrushLoad();
        private ImpastoPaintSurface.BrushDab dab;
        private Vector3 last;
        private float carry;
        public PencilTool(PencilToolDefinition definition)=>this.definition=definition;
        public override PaintToolDefinition Definition=>definition;
        public override void Begin(Vector3 point)
        {
            Direction=Vector2.zero;last=point;carry=0;load.Clear();
            dab=new ImpastoPaintSurface.BrushDab{radius=Mathf.Max(Surface.CellSize*.65f,WorkingSize*.5f),
                direction=Vector2.up,lengthRatio=1,squareness=0,bristles=6,bristleContrast=.15f,
                order=Surface.NewLayerOrder(),seed=13};
            Draw(point,Surface.CellSize);
        }
        public override void Drag(Vector3 from,Vector3 to,float deltaTime)
        {
            float distance=Surface.VisualDistance(from,to);
            if(distance<.000001f)return;
            TrackDirection(from,to);dab.direction=Direction;
            float spacing=Mathf.Max(Surface.CellSize*.7f,dab.radius*.4f),walked=spacing-carry;
            for(;walked<=distance;walked+=spacing)Draw(Vector3.Lerp(from,to,walked/distance),spacing);
            carry=distance-(walked-spacing);last=to;
        }
        private void Draw(Vector3 point,float distance)
        {
            dab.position=point;
            float units=definition.graphitePerCanvasWidth*distance/Surface.CanvasWidth*Mathf.Lerp(.6f,1.3f,Context.Pressure);
            Surface.Brush(dab,definition.graphite,units,load);
        }
        public override void End(Vector3 point)=>Surface.ReleaseBrush(last,load,dab.order,dab.radius);
    }
}
