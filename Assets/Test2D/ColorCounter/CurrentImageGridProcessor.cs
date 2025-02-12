using System.Collections.Generic;
using UnityEngine;

public class CurrentImageGridProcessor : MonoBehaviour
{
     public SpriteRenderer spriteRenderer; // Renkleri alacağımız kağıt sprite'ı
    private Texture2D texture;

    [SerializeField]
    private List<GridCell> gridData = new List<GridCell>(); // **Artık Inspector'da görünebilir!**

    private int gridX = 43;  // X ekseninde kaç parçaya bölünecek
    private int gridY = 125; // Y ekseninde kaç parçaya bölünecek

    private float minX = -4.25f, maxX = 4.25f;
    private float minY = -5.2f, maxY = 7.4f;
    
    public List<GridCell> GetGridData() => gridData;

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

                // En yakın tanımlı renge yuvarla
                Color nearestColor = Color.white;

                // Listeye ekle (Artık Inspector'da yuvarlanmış renkleri görebilirsin!)
                gridData.Add(new GridCell { position = worldPosition, color = nearestColor });
            }
        }

        Debug.Log($"Toplam {gridData.Count} hücre işlendi!");
    }
}
