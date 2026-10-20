using System.Collections.Generic;

namespace PaintingGame.Atelier
{
    // Undo for the atelier: the surface's copy-on-write tile journal plus the tube levels before each stroke,
    // so undoing a stroke also gives its paint back to the tubes.
    public sealed class StrokeHistory
    {
        private struct Entry
        {
            public ImpastoPaintSurface.PaintState state;
            public float[] tubes;
        }

        private readonly ImpastoPaintSurface surface;
        private readonly TubeInventory inventory;
        private readonly List<Entry> entries = new List<Entry>(16);
        private readonly Stack<float[]> tubeArrays = new Stack<float[]>();
        private readonly int maxStrokes;
        private readonly long memoryLimit;

        public StrokeHistory(ImpastoPaintSurface surface, TubeInventory inventory, int maxStrokes = 12, int memoryLimitMB = 40)
        {
            this.surface = surface; this.inventory = inventory;
            this.maxStrokes = maxStrokes < 1 ? 1 : maxStrokes;
            memoryLimit = memoryLimitMB * 1024L * 1024;
        }

        public bool CanUndo => entries.Count > 0 && surface != null && surface.IsReady && !surface.IsSpreading;
        public int Count => entries.Count;

        public void Record()
        {
            if (surface == null || !surface.IsReady || surface.IsSpreading) return;
            surface.ContinuePaintState(null);
            while (entries.Count >= maxStrokes) DropOldest();
            ImpastoPaintSurface.PaintState state = surface.CapturePaintState();
            if (state == null) return;
            float[] tubes = inventory.Snapshot(tubeArrays.Count > 0 ? tubeArrays.Pop() : null);
            entries.Add(new Entry { state = state, tubes = tubes });
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            Entry entry = entries[entries.Count - 1];
            if (!surface.RestorePaintState(entry.state)) return false;
            inventory.Restore(entry.tubes);
            entries.RemoveAt(entries.Count - 1);
            Release(entry);
            surface.ContinuePaintState(entries.Count > 0 ? entries[entries.Count - 1].state : null);
            return true;
        }

        // The journal grows while wet paint settles after a stroke; call after strokes to keep memory bounded.
        public void Trim()
        {
            while (entries.Count > 0 && TotalBytes() > memoryLimit) DropOldest();
        }

        public void Clear()
        {
            foreach (Entry entry in entries) Release(entry);
            entries.Clear();
        }

        private long TotalBytes()
        {
            long bytes = 0;
            foreach (Entry entry in entries) bytes += entry.state.EstimatedBytes;
            return bytes;
        }

        private void DropOldest()
        {
            Release(entries[0]);
            entries.RemoveAt(0);
        }

        private void Release(Entry entry)
        {
            surface.ReleasePaintState(entry.state);
            if (entry.tubes != null) tubeArrays.Push(entry.tubes);
        }
    }
}
