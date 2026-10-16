using System;
using UnityEngine;

public sealed partial class ImpastoPaintSurface
{
    internal struct SavedPixel
    {
        public int index, order;
        public float height, coatHeight;
        public Vector3 pigment, coatPigment;
    }

    public sealed class PaintState
    {
        internal SavedPixel[] pixels;
        internal byte[] ticks;
        internal int width, height, order, activeTiles, cursor;
        internal float accumulator;
        internal double deposited, paper;
        internal Color lastColor;
        internal bool hasColor;
        public long EstimatedBytes => pixels.LongLength * 40 + ticks.LongLength;
    }

    // Sparse snapshots preserve the wet coat and its order, not just a screenshot of the color.
    // Captured once per stroke. Empty pixels do not consume history memory.
    public PaintState CapturePaintState()
    {
        if (!IsReady || IsSpreading) return null;
        int count = 0;
        for (int i = 0; i < thickness.Length; i++) if (thickness[i] > 0) count++;
        var state = new PaintState
        {
            pixels = new SavedPixel[count], ticks = (byte[])settlingTicks.Clone(),
            width = width, height = height, order = depositOrder,
            activeTiles = activeSettlingTiles, cursor = settlingCursor, accumulator = settleAccumulator,
            deposited = DepositedVolume, paper = PaperVolume,
            lastColor = lastDepositColor, hasColor = hasDepositColor
        };
        int write = 0;
        for (int i = 0; i < thickness.Length; i++)
        {
            if (thickness[i] <= 0) continue;
            state.pixels[write++] = new SavedPixel { index = i, height = thickness[i], pigment = pigment[i],
                coatHeight = surfaceThickness[i], coatPigment = surfacePigment[i], order = surfaceOrder[i] };
        }
        return state;
    }

    public bool RestorePaintState(PaintState state)
    {
        if (!IsReady || IsSpreading || state == null || state.width != width || state.height != height) return false;
        ClearPaint();
        for (int i = 0; i < state.pixels.Length; i++)
        {
            SavedPixel pixel = state.pixels[i];
            thickness[pixel.index] = pixel.height; pigment[pixel.index] = pixel.pigment;
            surfaceThickness[pixel.index] = pixel.coatHeight; surfacePigment[pixel.index] = pixel.coatPigment;
            surfaceOrder[pixel.index] = pixel.order;
        }
        Array.Copy(state.ticks, settlingTicks, settlingTicks.Length);
        activeSettlingTiles = state.activeTiles; settlingCursor = state.cursor; settleAccumulator = state.accumulator;
        depositOrder = state.order; lastDepositColor = state.lastColor; hasDepositColor = state.hasColor;
        DepositedVolume = state.deposited; PaperVolume = state.paper;
        IncludeChanged(0, 0, width - 1, height - 1);
        dirty = true;
        Flush();
        SyncAlbedo();
        return true;
    }
}
