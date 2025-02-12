using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridCell
{
    public Vector2 position;
    public Color color;
}

public class ImageGridProcessor : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // Renkleri alacağımız kağıt sprite'ı
    private Texture2D texture;

    [SerializeField]
    private List<GridCell> gridData = new List<GridCell>(); // **Artık Inspector'da görünebilir!**

    private int gridX = 43;  // X ekseninde kaç parçaya bölünecek
    private int gridY = 125; // Y ekseninde kaç parçaya bölünecek

    private float minX = -4.25f, maxX = 4.25f;
    private float minY = -5.2f, maxY = 7.4f;

    private Color[] predefinedColors = new Color[]
    {
        Color.red,     // Kırmızı
        Color.green,   // Yeşil
        Color.blue,    // Mavi
        Color.yellow,  // Sarı
        Color.white,   // Beyaz
        Color.black,   // Siyah
        Color.cyan,    // Açık Mavi
        Color.magenta  // Mor
    };

    void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer atanmamış!");
            return;
        }

        // Texture2D'yi al
        texture = spriteRenderer.sprite.texture;

        if (!texture.isReadable)
        {
            Debug.LogError("Texture2D is not readable! Lütfen texture'ı 'Read/Write Enabled' yap.");
            return;
        }

        ProcessGrid();
    }

    void ProcessGrid()
    {
        gridData.Clear(); // Önceki verileri temizle

        float cellWidth = (maxX - minX) / gridX;   // X ekseninde hücre genişliği
        float cellHeight = (maxY - minY) / gridY;  // Y ekseninde hücre yüksekliği

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                // Hücrenin dünya koordinatlarındaki merkezi
                float worldX = minX + (x + 0.5f) * cellWidth;
                float worldY = minY + (y + 0.5f) * cellHeight;
                Vector2 worldPosition = new Vector2(worldX, worldY);

                // Texture2D içindeki renk değerini al
                Color cellColor = GetTextureColor(worldX, worldY);

                // En yakın tanımlı renge yuvarla
                Color nearestColor = GetNearestColor(cellColor);

                // Listeye ekle (Artık Inspector'da yuvarlanmış renkleri görebilirsin!)
                gridData.Add(new GridCell { position = worldPosition, color = nearestColor });
            }
        }

        Debug.Log($"Toplam {gridData.Count} hücre işlendi!");
    }

    Color GetTextureColor(float worldX, float worldY)
    {
        // Sprite'ın dünya koordinatlarındaki sınırlarını al
        Bounds bounds = spriteRenderer.bounds;

        // Eğer dünya pozisyonu sprite'ın dışında kalıyorsa varsayılan renk dön
        if (!bounds.Contains(new Vector3(worldX, worldY, 0)))
            return Color.clear;

        // Dünya koordinatlarını sprite’ın local koordinatlarına çevir
        Vector2 localPoint = spriteRenderer.transform.InverseTransformPoint(new Vector2(worldX, worldY));

        // Texture boyutlarını al
        int texWidth = texture.width;
        int texHeight = texture.height;

        // LocalPoint'i (0,1) aralığına çevirerek UV koordinatlarına dönüştür
        float uvX = (localPoint.x - bounds.min.x) / bounds.size.x;
        float uvY = (localPoint.y - bounds.min.y) / bounds.size.y;

        // UV koordinatlarını piksel koordinatlarına dönüştür
        int pixelX = Mathf.FloorToInt(uvX * texWidth);
        int pixelY = Mathf.FloorToInt(uvY * texHeight);

        // Sınırları aşmamak için kontrol et
        pixelX = Mathf.Clamp(pixelX, 0, texWidth - 1);
        pixelY = Mathf.Clamp(pixelY, 0, texHeight - 1);

        return texture.GetPixel(pixelX, pixelY); // Pikselin rengini al
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

    public List<GridCell> GetGridData()
    {
        return gridData;
    }
}