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
    public SpriteRenderer spriteRenderer;
    [SerializeField] private List<GridCell> gridData = new List<GridCell>();
    public const int Columns = 43;
    public const int Rows = 125;

    private void Awake() => RebuildGrid();
    public List<GridCell> GetGridData() => gridData;

    public bool RebuildGrid()
    {
        gridData.Clear();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return false;
        Texture2D texture = spriteRenderer.sprite.texture;
        if (!texture.isReadable)
        {
            Debug.LogError("Reference texture must have Read/Write enabled.", this);
            return false;
        }
        Bounds bounds = spriteRenderer.sprite.bounds;
        for (int x = 0; x < Columns; x++)
        for (int y = 0; y < Rows; y++)
        {
            Vector2 uv = new Vector2((x + 0.5f) / Columns, (y + 0.5f) / Rows);
            Vector3 local = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, uv.x),
                Mathf.Lerp(bounds.min.y, bounds.max.y, uv.y), 0);
            gridData.Add(new GridCell
            {
                position = spriteRenderer.transform.TransformPoint(local),
                color = Helper.SampleSprite(spriteRenderer, texture, uv)
            });
        }
        return true;
    }
}
