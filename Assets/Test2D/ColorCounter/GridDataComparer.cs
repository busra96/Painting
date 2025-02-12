using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridDataComparer : MonoBehaviour
{
    public TextMeshProUGUI yuzdelikText;
    public ImageGridProcessor imageGridProcessor;
    public CurrentImageGridProcessor currentImageGridProcessor;

    [ContextMenu("TEST")]
    public void Test()
    {
       float target =  CompareGridData(imageGridProcessor.GetGridData(), currentImageGridProcessor.GetGridData());
       yuzdelikText.text = target + " / " + 100;
       Debug.Log( " Target " + target );
    }
    
    
    public float CompareGridData(List<GridCell> originalGrid, List<GridCell> currentGrid)
    {
        if (originalGrid == null || currentGrid == null || originalGrid.Count != currentGrid.Count)
        {
            Debug.LogError("Grid verileri karşılaştırılamaz: Boyutlar eşleşmiyor veya veri eksik!");
            return 0f;
        }

        int matchingCells = 0;
        for (int i = 0; i < originalGrid.Count; i++)
        {
            if (originalGrid[i].color == currentGrid[i].color)
            {
                matchingCells++;
            }
        }

        float accuracy = (float)matchingCells / originalGrid.Count;
        return Mathf.Clamp01(accuracy) * 100f; // Yüzde olarak döndür
    }
}