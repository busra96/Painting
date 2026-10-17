using System.Collections;
using System.Collections.Generic;
using PaintIn2D;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class CurrentImageGridProcessor : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    [SerializeField] private List<GridCell> gridData = new List<GridCell>();
    public List<GridCell> GetGridData() => gridData;
    public bool IsReady { get; private set; }

    private void Awake() => RebuildGrid();

    private IEnumerator Start()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) yield break;
        var model = spriteRenderer.GetComponent<CwPaintableSprite>();
        if (model != null && !model.IsActivated) model.Activate();
        // The initial Game View/graphics setup can recreate render targets on the first frame.
        yield return null;
        var paintable = spriteRenderer.GetComponent<CwPaintableSpriteTexture>();
        if (paintable == null || paintable.Current == null) yield break;
        // Initialize an opaque paper after PaintIn2D activation. RGB-only paint stamps must retain its alpha.
        RenderTexture previous = RenderTexture.active;
        try { Graphics.Blit(spriteRenderer.sprite.texture, paintable.Current); }
        finally { RenderTexture.active = previous; }
        var surface = spriteRenderer.GetComponent<ImpastoPaintSurface>();
        if (surface != null) surface.Initialize();
        IsReady = true;
    }

    public bool RebuildGrid()
    {
        gridData.Clear();
        if (spriteRenderer == null || spriteRenderer.sprite == null) return false;
        Bounds bounds = spriteRenderer.sprite.bounds;
        for (int x = 0; x < ImageGridProcessor.Columns; x++)
        for (int y = 0; y < ImageGridProcessor.Rows; y++)
        {
            Vector3 local = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (x + 0.5f) / ImageGridProcessor.Columns),
                Mathf.Lerp(bounds.min.y, bounds.max.y, (y + 0.5f) / ImageGridProcessor.Rows), 0);
            gridData.Add(new GridCell { position = spriteRenderer.transform.TransformPoint(local), color = Color.white });
        }
        return true;
    }

    // A single GPU readback at completion replaces thousands of estimates per brush per frame.
    public bool CapturePaintedImage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return false;
        var surface = spriteRenderer.GetComponent<ImpastoPaintSurface>();
        if (surface != null) surface.SyncAlbedo();
        CwPaintableSpriteTexture paintable = spriteRenderer.GetComponent<CwPaintableSpriteTexture>();
        if (paintable == null || paintable.Current == null) return false;
        if (gridData.Count == 0 && !RebuildGrid()) return false;
        paintable.ExecuteCommands(true, true);
        Texture2D snapshot = paintable.GetReadableCopy();
        if (snapshot == null) return false;
        try
        {
            for (int x = 0; x < ImageGridProcessor.Columns; x++)
            for (int y = 0; y < ImageGridProcessor.Rows; y++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / ImageGridProcessor.Columns,
                    (y + 0.5f) / ImageGridProcessor.Rows);
                gridData[x * ImageGridProcessor.Rows + y].color = Helper.SampleSprite(spriteRenderer, snapshot, uv);
            }
        }
        finally
        {
            if (Application.isPlaying) Destroy(snapshot);
            else DestroyImmediate(snapshot);
        }
        return true;
    }
}
