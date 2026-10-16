using System.Collections.Generic;
using UnityEngine;

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
    private long savedBytes;
    public bool CanUndo => entries.Count > 0 && surface != null && !surface.IsSpreading;
    public int StrokeCount => entries.Count;
    public long SavedBytes => savedBytes;

    public void RecordStroke()
    {
        if (surface == null || amount == null || spawner == null || !surface.IsReady || surface.IsSpreading) return;
        var state = surface.CapturePaintState();
        if (state == null) return;
        if (state.EstimatedBytes > memoryLimitMB * 1024L * 1024) { Forget(); return; }
        while (entries.Count > 0 && (entries.Count >= maxStrokes || savedBytes + state.EstimatedBytes > memoryLimitMB * 1024L * 1024))
        {
            savedBytes -= entries[0].state.EstimatedBytes;
            entries.RemoveAt(0);
        }
        entries.Add(new Entry { state = state, used = amount.UsedAmount, count = spawner.PaintCount, units = spawner.DepositedUnits });
        savedBytes += state.EstimatedBytes;
    }

    public void UndoStroke()
    {
        if (!CanUndo || amount == null || spawner == null) return;
        spawner.EndStroke();
        Entry entry = entries[entries.Count - 1];
        if (!surface.RestorePaintState(entry.state)) return;
        amount.RestoreUsedAmount(entry.used);
        spawner.RestoreDrawingState(entry.count, entry.units);
        savedBytes -= entry.state.EstimatedBytes;
        entries.RemoveAt(entries.Count - 1);
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

    public void Forget() { entries.Clear(); savedBytes = 0; }
}
