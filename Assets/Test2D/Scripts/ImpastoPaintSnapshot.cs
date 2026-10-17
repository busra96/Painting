using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class ImpastoPaintSurface
{
    internal struct SavedPixel
    {
        public int order;
        public float height, coatHeight;
        public Vector3 pigment, coatPigment;
    }

    internal struct SavedTile
    {
        public int index;
        // Null records a tile that was empty before this stroke; no pixel storage is needed.
        public SavedPixel[] pixels;
    }

    public sealed class PaintState
    {
        internal List<SavedTile> tiles;
        internal bool[] captured;
        internal byte[] ticks;
        internal int populatedTiles;
        internal bool valid;
        internal int width, height, order, activeTiles, cursor;
        internal float accumulator;
        internal double deposited, paper;
        internal Color lastColor;
        internal bool hasColor;
        public long EstimatedBytes => (long)populatedTiles * TileSize * TileSize * 36 +
            (long)captured.Length * 18;
    }

    private PaintState recordingState;
    private readonly Stack<SavedPixel[]> savedTilePool = new Stack<SavedPixel[]>(64);
    private readonly Stack<PaintState> paintStatePool = new Stack<PaintState>(16);

    // Start a copy-on-write journal. Pointer-down only saves small scalar/tile metadata;
    // pixel data is copied once, just before a tile is first changed by pouring or settling.
    public PaintState CapturePaintState()
    {
        if (!IsReady || IsSpreading) return null;
        PaintState state = paintStatePool.Count > 0 ? paintStatePool.Pop() : new PaintState
        {
            tiles = new List<SavedTile>(settlingTicks.Length), captured = new bool[settlingTicks.Length],
            ticks = new byte[settlingTicks.Length]
        };
        Array.Copy(settlingTicks, state.ticks, settlingTicks.Length);
        state.width = width; state.height = height; state.order = depositOrder;
        state.activeTiles = activeSettlingTiles; state.cursor = settlingCursor; state.accumulator = settleAccumulator;
        state.deposited = DepositedVolume; state.paper = PaperVolume;
        state.lastColor = lastDepositColor; state.hasColor = hasDepositColor; state.valid = true;
        recordingState = state;
        return state;
    }

    private void CaptureHistoryTiles(int x0, int y0, int x1, int y1)
    {
        PaintState state = recordingState;
        if (state == null) return;
        for (int ty = y0 / TileSize; ty <= y1 / TileSize; ty++)
        for (int tx = x0 / TileSize; tx <= x1 / TileSize; tx++)
        {
            int tile = ty * tilesX + tx;
            if (state.captured[tile]) continue;
            state.captured[tile] = true;
            int startX = tx * TileSize, startY = ty * TileSize;
            int endX = Mathf.Min(width, startX + TileSize), endY = Mathf.Min(height, startY + TileSize);
            bool hasPaint = false;
            for (int y = startY; y < endY && !hasPaint; y++)
            for (int x = startX; x < endX; x++)
                if (thickness[y * width + x] > 0) { hasPaint = true; break; }
            SavedPixel[] pixels = null;
            if (hasPaint)
            {
                pixels = savedTilePool.Count > 0 ? savedTilePool.Pop() : new SavedPixel[TileSize * TileSize];
                for (int y = startY; y < endY; y++)
                for (int x = startX; x < endX; x++)
                {
                    int i = y * width + x;
                    pixels[(y - startY) * TileSize + x - startX] = new SavedPixel
                    {
                        height = thickness[i], pigment = pigment[i], coatHeight = surfaceThickness[i],
                        coatPigment = surfacePigment[i], order = surfaceOrder[i]
                    };
                }
                state.populatedTiles++;
            }
            state.tiles.Add(new SavedTile { index = tile, pixels = pixels });
        }
    }

    public void ContinuePaintState(PaintState state)
    {
        recordingState = state != null && state.valid && state.width == width && state.height == height ? state : null;
    }

    public void ReleasePaintState(PaintState state)
    {
        if (state == null || !state.valid) return;
        if (recordingState == state) recordingState = null;
        foreach (SavedTile tile in state.tiles)
            if (tile.pixels != null && savedTilePool.Count < 64) savedTilePool.Push(tile.pixels);
        state.tiles.Clear();
        Array.Clear(state.captured, 0, state.captured.Length);
        state.populatedTiles = 0; state.valid = false;
        if (paintStatePool.Count < 16) paintStatePool.Push(state);
    }

    public bool RestorePaintState(PaintState state)
    {
        if (!IsReady || IsSpreading || state == null || !state.valid || state.width != width || state.height != height) return false;
        recordingState = null;
        foreach (SavedTile tile in state.tiles)
        {
            int startX = tile.index % tilesX * TileSize, startY = tile.index / tilesX * TileSize;
            int endX = Mathf.Min(width, startX + TileSize), endY = Mathf.Min(height, startY + TileSize);
            for (int y = startY; y < endY; y++)
            for (int x = startX; x < endX; x++)
            {
                SavedPixel pixel = tile.pixels != null ? tile.pixels[(y - startY) * TileSize + x - startX] : default;
                int i = y * width + x;
                thickness[i] = pixel.height; pigment[i] = pixel.pigment;
                surfaceThickness[i] = pixel.coatHeight; surfacePigment[i] = pixel.coatPigment;
                surfaceOrder[i] = pixel.order;
            }
            IncludeChanged(startX, startY, endX - 1, endY - 1);
        }
        Array.Copy(state.ticks, settlingTicks, settlingTicks.Length);
        activeSettlingTiles = state.activeTiles; settlingCursor = state.cursor; settleAccumulator = state.accumulator;
        depositOrder = state.order; lastDepositColor = state.lastColor; hasDepositColor = state.hasColor;
        DepositedVolume = state.deposited; PaperVolume = state.paper;
        dirty = true;
        Flush();
        SyncAlbedo();
        return true;
    }
}
