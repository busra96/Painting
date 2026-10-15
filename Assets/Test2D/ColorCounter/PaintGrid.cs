using PaintIn2D;
using UnityEngine;

// Kept on existing prefabs for compatibility. Final scoring uses the actual painted texture.
public class PaintGrid : MonoBehaviour
{
    public CwPaintDecal2D PaintDecal2D;
    public CurrentImageGridProcessor gridProcessor;
    public Transform brush;
    public float brushRadius = 0.5f;

    public void Activate() => enabled = false;
    public void SetBlushRadius()
    {
        if (PaintDecal2D != null) brushRadius = PaintDecal2D.Radius;
    }
}
