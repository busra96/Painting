using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridDataComparer : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI yuzdelikText;
    public ImageGridProcessor imageGridProcessor;
    public CurrentImageGridProcessor currentImageGridProcessor;

    [ContextMenu("TEST")]
    public void Test()
    {
       float target =  CompareGridData(imageGridProcessor.GetGridData(), currentImageGridProcessor.GetGridData());
       int roundedTarget = Mathf.CeilToInt(target); // En yakın üst tam sayıya yuvarla
       yuzdelikText.text = roundedTarget + " / " + 100;
       slider.DOValue(roundedTarget, 0.25f);
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