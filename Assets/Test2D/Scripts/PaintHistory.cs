using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(40)]
public sealed class PaintHistory : MonoBehaviour
{
    public ImpastoPaintSurface surface;
    public PaintAmount amount;
    public Test_RaycastSpawn_CameraPerspective spawner;
    [Range(1, 16)] public int maxStrokes = 8;
    [Range(16, 64)] public int memoryLimitMB = 32;
    private struct Entry
    {
        public ImpastoPaintSurface.PaintState state;
        public float used, units;
        public int count;
    }
    private readonly List<Entry> entries = new List<Entry>(16);
    public bool CanUndo => entries.Count > 0 && surface != null && !surface.IsSpreading;
    public int StrokeCount => entries.Count;
    public long SavedBytes
    {
        get
        {
            long bytes = 0;
            foreach (Entry entry in entries) bytes += entry.state.EstimatedBytes;
            return bytes;
        }
    }

    // Tile journals grow as wet paint changes, including settling after pointer release.
    private void LateUpdate() => TrimHistory();
    private void OnDestroy() => Forget();

    public void RecordStroke()
    {
        if (surface == null || amount == null || spawner == null || !surface.IsReady || surface.IsSpreading) return;
        surface.ContinuePaintState(null);
        while (entries.Count >= Mathf.Max(1, maxStrokes)) DropOldest();
        var state = surface.CapturePaintState();
        if (state == null) return;
        entries.Add(new Entry { state = state, used = amount.UsedAmount, count = spawner.PaintCount, units = spawner.DepositedUnits });
        TrimHistory();
    }

    public void UndoStroke()
    {
        if (!CanUndo || amount == null || spawner == null) return;
        spawner.EndStroke();
        Entry entry = entries[entries.Count - 1];
        if (!surface.RestorePaintState(entry.state)) return;
        amount.RestoreUsedAmount(entry.used);
        spawner.RestoreDrawingState(entry.count, entry.units);
        entries.RemoveAt(entries.Count - 1);
        surface.ReleasePaintState(entry.state);
        surface.ContinuePaintState(entries.Count > 0 ? entries[entries.Count - 1].state : null);
    }

    public void ClearPainting()
    {
        if (surface == null || !surface.IsReady || surface.IsSpreading || amount == null || spawner == null) return;
        spawner.EndStroke();
        surface.ClearPaint();
        surface.SyncAlbedo();
        amount.RestoreUsedAmount(0);
        spawner.RestoreDrawingState(0, 0);
        Forget();
    }

    private void TrimHistory()
    {
        long limit = Mathf.Max(1, memoryLimitMB) * 1024L * 1024;
        while (entries.Count > 0 && (entries.Count > Mathf.Max(1, maxStrokes) || SavedBytes > limit)) DropOldest();
    }

    private void DropOldest()
    {
        if (surface != null) surface.ReleasePaintState(entries[0].state);
        entries.RemoveAt(0);
    }

    public void Forget()
    {
        if (surface != null)
            foreach (Entry entry in entries) surface.ReleasePaintState(entry.state);
        entries.Clear();
    }
}
