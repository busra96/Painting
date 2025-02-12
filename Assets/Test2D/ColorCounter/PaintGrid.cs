using PaintIn2D;
using UnityEngine;

public class PaintGrid : MonoBehaviour
{
    public CwPaintDecal2D PaintDecal2D;
    public CurrentImageGridProcessor gridProcessor;
    public Transform brush; // Boya objesi (örneğin fırça)
    public float brushRadius = 0.5f; // Boya objesinin etkilediği yarıçap
    private Color color;
    private bool isActive = false;

    private Color[] predefinedColors = new Color[]
    {
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.white, Color.black, Color.cyan, Color.magenta
    };

    public void Activate()
    {
        isActive = true;
    }

    public void SetBlushRadius()
    {
        brushRadius = PaintDecal2D.Radius;
    }

    void Update()
    {
        if(!isActive)
            return;
        
        if (gridProcessor == null || brush == null)
        {
            Debug.LogError("GridProcessor veya Brush atanmadı!");
            return;
        }

        color = PaintDecal2D.Color;
        PaintCells(brush.position, brushRadius);
    }

    void PaintCells(Vector2 brushPosition, float radius)
    {
        for (int i = 0; i < gridProcessor.GetGridData().Count; i++)
        {
            GridCell cell = gridProcessor.GetGridData()[i];
            float distance = Vector2.Distance(brushPosition, cell.position);

            if (distance <= radius)
            {
                Color nearestColor = GetNearestColor(color); // Kırmızıya en yakın rengi al
                gridProcessor.GetGridData()[i] = new GridCell { position = cell.position, color = nearestColor };
            }
        }
    }

    Color GetNearestColor(Color inputColor)
    {
        Color closestColor = predefinedColors[0];
        float minDistance = float.MaxValue;

        foreach (Color predefined in predefinedColors)
        {
            float distance = ColorDistance(inputColor, predefined);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestColor = predefined;
            }
        }

        return closestColor;
    }

    float ColorDistance(Color a, Color b)
    {
        float rDiff = a.r - b.r;
        float gDiff = a.g - b.g;
        float bDiff = a.b - b.b;

        return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff); // Öklid Mesafesi
    }
   /* public CurrentImageGridProcessor gridProcessor;
    public Transform brush; // Boya objesi (örneğin fırça)
    public float brushRadius = 0.5f; // Boya objesinin etkilediği yarıçap

    void Update()
    {
        if (gridProcessor == null || brush == null)
        {
            Debug.LogError("GridProcessor veya Brush atanmadı!");
            return;
        }

        PaintCells(brush.position, brushRadius);
    }

    void PaintCells(Vector2 brushPosition, float radius)
    {
        for (int i = 0; i < gridProcessor.GetGridData().Count; i++)
        {
            GridCell cell = gridProcessor.GetGridData()[i];
            float distance = Vector2.Distance(brushPosition, cell.position);
            
            if (distance <= radius)
            {
                // Hücrenin rengini kırmızıya çevir
                gridProcessor.GetGridData()[i] = new GridCell { position = cell.position, color = Color.red };
            }
        }
    }*/
}
