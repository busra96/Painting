using UnityEngine;

// The shop's free-form tools share the same pigment/volume field as the target-matching game.
public sealed partial class ImpastoPaintSurface
{
    public enum ArtistBrush { Round, Flat, Fan, Flower, Star }

    public bool ArtistDab(Vector3 world, Color color, float units, float radius, float angle, ArtistBrush brush)
    {
        if (!IsReady || IsSpreading || units <= 0 || !Contains(world)) return false;
        Vector2 center = GridPoint(world);
        float reach = radius * 1.5f;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - reach / dx));
        int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + reach / dx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - reach / dy));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + reach / dy));
        EnsureDepositCapacity(x1 - x0 + 1, y1 - y0 + 1);
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle), sum = 0f;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float px = (x - center.x) * dx / radius, py = (y - center.y) * dy / radius;
            float u = px * cos + py * sin, v = -px * sin + py * cos;
            float value = ArtistWeight(u, v, brush);
            depositWeights[(y - y0) * depositStampWidth + x - x0] = value;
            sum += value;
        }
        if (sum <= 0) return false;
        CaptureHistoryTiles(x0, y0, x1, y1);
        // Every new gesture may lay an opaque coat on an older pigment of the same color.
        if (!hasDepositColor || color != lastDepositColor)
        {
            depositOrder++;
            lastDepositColor = color;
            hasDepositColor = true;
        }
        Vector3 density = Absorption(color);
        float scale = units * UnitVolume / (sum * cellArea);
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float added = depositWeights[(y - y0) * depositStampWidth + x - x0] * scale;
            if (added <= 0) continue;
            int i = y * width + x;
            Vector3 mass = density * added;
            thickness[i] += added;
            pigment[i] += mass;
            AddSurfaceLayer(i, added, mass, depositOrder);
        }
        DepositedVolume += units * UnitVolume;
        PaperVolume += units * UnitVolume;
        ActivateSettling(x0, y0, x1, y1);
        IncludeChanged(x0, y0, x1, y1);
        dirty = true;
        return true;
    }

    private static float ArtistWeight(float x, float y, ArtistBrush brush)
    {
        float distance = Mathf.Sqrt(x * x + y * y);
        float polar = Mathf.Atan2(y, x);
        switch (brush)
        {
            case ArtistBrush.Flat:
                return Mathf.Clamp01((1f - Mathf.Abs(x)) * 9f) *
                       Mathf.Clamp01((0.34f - Mathf.Abs(y)) * 18f) *
                       (0.68f + 0.32f * Mathf.Pow(Mathf.Sin(x * 46f + y * 2f), 2));
            case ArtistBrush.Fan:
                float fan = Mathf.Sqrt(x * x * 0.7f + y * y * 3f);
                return Mathf.Pow(Mathf.Max(0f, 1f - fan), 0.6f) *
                       (0.3f + 0.7f * Mathf.Pow(Mathf.Sin(polar * 19f + distance * 5f), 2));
            case ArtistBrush.Flower:
                float flowerEdge = 0.60f + 0.27f * Mathf.Cos(polar * 7f);
                return Mathf.Clamp01((flowerEdge - distance) * 24f);
            case ArtistBrush.Star:
                float sector = Mathf.Repeat(polar + Mathf.PI / 2f, Mathf.PI * 0.4f);
                float edge = Mathf.Lerp(0.40f, 0.98f, Mathf.Abs(sector / (Mathf.PI * 0.2f) - 1f));
                return Mathf.Clamp01((edge - distance) * 30f);
            default:
                return Mathf.Pow(Mathf.Max(0f, 1f - distance * distance), 0.65f) *
                       (0.87f + 0.13f * Mathf.Sin(x * 51f + y * 3f));
        }
    }

    // Pair exchanges keep total height and all three pigment masses unchanged, including at edges.
    // Only a local tile neighborhood is touched; no whole-canvas copy or GPU readback is required.
    public bool ArtistPress(Vector3 world, float radius, float pressure, Vector2 drag)
    {
        if (!IsReady || IsSpreading || !Contains(world)) return false;
        Vector2 center = GridPoint(world);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(center.x - radius / dx), 0, width - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(center.x + radius / dx), 0, width - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(center.y - radius / dy), 0, height - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(center.y + radius / dy), 0, height - 1);
        CaptureHistoryTiles(Mathf.Max(0, x0 - 1), Mathf.Max(0, y0 - 1),
            Mathf.Min(width - 1, x1 + 1), Mathf.Min(height - 1, y1 + 1));
        bool moved = false;
        float strength = Mathf.Clamp01(pressure) * 0.22f;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float nx = (x - center.x) * dx / radius, ny = (y - center.y) * dy / radius;
            float falloff = Mathf.Max(0, 1f - nx * nx - ny * ny);
            if (falloff <= 0) continue;
            int i = y * width + x;
            if (x + 1 < width) moved |= ArtistTransfer(i, i + 1, strength * falloff, drag.x);
            if (y + 1 < height) moved |= ArtistTransfer(i, i + width, strength * falloff, drag.y);
        }
        if (!moved) return false;
        IncludeChanged(Mathf.Max(0, x0 - 1), Mathf.Max(0, y0 - 1),
            Mathf.Min(width - 1, x1 + 1), Mathf.Min(height - 1, y1 + 1));
        dirty = true;
        return true;
    }

    private bool ArtistTransfer(int a, int b, float strength, float direction)
    {
        float flux = (thickness[a] - thickness[b]) * strength +
                     direction * (thickness[a] + thickness[b]) * strength * 0.65f;
        if (Mathf.Abs(flux) < 0.0000001f) return false;
        int from = flux > 0 ? a : b, to = flux > 0 ? b : a;
        float amount = Mathf.Min(Mathf.Abs(flux), thickness[from] * 0.25f);
        if (amount <= 0) return false;
        MoveArtistMass(from, to, amount);
        return true;
    }

    private void MoveArtistMass(int from, int to, float amount)
    {
        float fraction = amount / thickness[from];
        Vector3 mass = pigment[from] * fraction;
        float coat = surfaceThickness[from] * fraction;
        Vector3 coatMass = surfacePigment[from] * fraction;
        thickness[from] -= amount; thickness[to] += amount;
        pigment[from] -= mass; pigment[to] += mass;
        surfaceThickness[from] -= coat;
        surfacePigment[from] -= coatMass;
        if (surfaceOrder[from] >= surfaceOrder[to]) AddSurfaceLayer(to, coat, coatMass, surfaceOrder[from]);
    }

    public bool ArtistWideBlade(float fromY, float toY, float pressure)
    {
        if (!IsReady || IsSpreading || toY >= fromY) return false;
        int first = Mathf.Clamp(Mathf.FloorToInt(GridPoint(new Vector3(0, fromY, transform.position.z)).y), 1, height - 1);
        int last = Mathf.Max(1, Mathf.CeilToInt(GridPoint(new Vector3(0, toY, transform.position.z)).y));
        if (last > first) return false; // Sub-pixel motion must not repeatedly process the same row.
        CaptureHistoryTiles(0, last - 1, width - 1, first);
        bool moved = false;
        for (int y = first; y >= last; y--)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            // The blade carries excess pigment downward and leaves a visible paint film behind.
            float carry = Mathf.Max(0f, thickness[i] - Film) * Mathf.Lerp(0.75f, 0.98f, pressure);
            if (carry <= 0.0000001f) continue;
            MoveArtistMass(i, i - width, carry); moved = true;
        }
        if (moved) { IncludeChanged(0, last - 1, width - 1, first); dirty = true; }
        return moved;
    }
}
