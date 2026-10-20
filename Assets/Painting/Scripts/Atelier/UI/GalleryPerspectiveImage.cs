using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    // Four authored corners place the real canvas inside the perspective of a painted wall frame.
    public sealed class GalleryPerspectiveImage : RawImage
    {
        public Vector2 bottomLeft, bottomRight, topRight, topLeft;
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Add(vertices,bottomLeft,Vector2.zero); Add(vertices,topLeft,Vector2.up);
            Add(vertices,topRight,Vector2.one); Add(vertices,bottomRight,Vector2.right);
            vertices.AddTriangle(0,1,2);vertices.AddTriangle(2,3,0);
        }
        private void Add(VertexHelper vertices,Vector2 corner,Vector2 uv)
        {
            Rect r=rectTransform.rect;
            vertices.AddVert(new Vector3(r.xMin+corner.x*r.width,r.yMin+corner.y*r.height),color,uv);
        }
    }
}
