using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// Hand tools for the atelier: bristle brush, palette knife (any direction), press pad and tube pour.
// Every operation is local (bounding box of the tool), conserves volume and pigment unless paint is
// explicitly added from a tube, records undo tiles before writing, and allocates nothing per call.
public sealed partial class ImpastoPaintSurface
{
    private static readonly ProfilerMarker BrushMarker = new ProfilerMarker("Painting.Brush");
    private static readonly ProfilerMarker KnifeMarker = new ProfilerMarker("Painting.Knife");
    private static readonly ProfilerMarker PressMarker = new ProfilerMarker("Painting.Press");
    private readonly float[] brushStreakSamples = new float[129];

    public Vector2 PaperSize => paper != null ? (Vector2)paper.bounds.size : Vector2.one;

    // On-screen proportions of world axes, normalised so x = 1. The workshop camera may stretch the paper
    // non-uniformly to fit the frame; tools work in these visual units so a round brush looks round.
    public Vector2 VisualScale { get; set; } = Vector2.one;
    public float CanvasWidth => PaperSize.x * VisualScale.x;
    public float CellSize => Mathf.Min(dx * VisualScale.x, dy * VisualScale.y);
    public Vector2 ToVisual(Vector2 worldDelta) => Vector2.Scale(worldDelta, VisualScale);
    public Vector2 FromVisual(Vector2 visualDelta) => new Vector2(visualDelta.x / VisualScale.x, visualDelta.y / VisualScale.y);
    public float VisualDistance(Vector3 a, Vector3 b) => ToVisual(b - a).magnitude;
    private float VDx => dx * VisualScale.x;
    private float VDy => dy * VisualScale.y;

    // A new visible coat. Each tool stroke paints its own coat so it can cover older paint.
    public int NewLayerOrder() => ++depositOrder;

    // ---------------------------------------------------------------- bristle brush

    public struct BrushDab
    {
        public Vector3 position;
        public Vector2 direction;      // normalized travel direction, visual units
        public float radius;           // visual units, across the brush
        public float lengthRatio;      // footprint length along travel / radius (flat brush ≈ 0.35, round = 1)
        public float squareness;       // 0 = ellipse, 1 = flat rectangular ferrule
        public float bristles;         // streak frequency across the brush
        public float bristleContrast;  // 0..1, how strongly bristle ridges show
        public float smudge;           // 0..1, fraction of wet paint picked up and dragged
        public float release;          // 0..1, fraction of the brush's dirty load laid back down per dab
        public float travel;           // stroke progress, for slowly changing streaks
        public float seed;
        public int order;
    }

    // Loose paint carried on the bristles (already on the canvas once). Volume in world³, mass is pigment·volume.
    public sealed class BrushLoad
    {
        public float volume;
        public Vector3 mass;
        public void Clear() { volume = 0; mass = Vector3.zero; }
    }

    // Lays clean tube paint (units may be 0 for a dry blending brush) and drags wet paint along with it.
    public void Brush(in BrushDab dab, Color color, float units, BrushLoad load)
    {
        if (!IsReady || IsSpreading || load == null || !Contains(dab.position)) return;
        using (BrushMarker.Auto())
        {
            Vector2 center = GridPoint(dab.position);
            Vector2 along = dab.direction.sqrMagnitude > 0.0001f ? dab.direction.normalized : Vector2.up;
            Vector2 across = new Vector2(-along.y, along.x);
            float vdx = VDx, vdy = VDy;
            float halfWidth = Mathf.Max(dab.radius, CellSize);
            float halfLength = Mathf.Max(halfWidth * dab.lengthRatio, CellSize);
            float reach = Mathf.Max(halfWidth, halfLength) * 1.05f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - reach / vdx)), x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + reach / vdx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - reach / vdy)), y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + reach / vdy));
            int stampWidth = x1 - x0 + 1, stampHeight = y1 - y0 + 1;
            if (stampWidth <= 0 || stampHeight <= 0) return;
            EnsureDepositCapacity(stampWidth, stampHeight);
            float square = Mathf.Clamp01(dab.squareness);
            // Noise varies across the bristles, not across every cell of the footprint. Sample it once.
            for (int s=0;s<brushStreakSamples.Length;s++)
                brushStreakSamples[s]=Mathf.PerlinNoise((s/64f-1)*dab.bristles+dab.seed,dab.travel*1.3f+dab.seed*.37f);
            float weightSum = 0;
            for (int y = y0; y <= y1; y++)
            {
                int row = (y - y0) * depositStampWidth;
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 d = new Vector2((x - center.x) * vdx, (y - center.y) * vdy);
                    float u = Vector2.Dot(d, along) / halfLength, v = Vector2.Dot(d, across) / halfWidth;
                    float u2=u*u,v2=v*v;
                    float round=u2+v2, box=u2*u2+v2*v2;
                    float shape=square<.5f?Mathf.Lerp(round,box,square*2):Mathf.Lerp(box,u2*u2*u2+v2*v2*v2,(square-.5f)*2);
                    float w = 0;
                    if (shape < 1)
                    {
                        float edge = Mathf.Clamp01((1 - shape) * 3);
                        // Streaks stay fixed across the brush and drift slowly with travel, so ridges read as bristle marks.
                        float sample=Mathf.Clamp((v+1)*64,0,128);int s=Mathf.Min(127,(int)sample);
                        float streak=Mathf.Lerp(brushStreakSamples[s],brushStreakSamples[s+1],sample-s);
                        w = edge * Mathf.Lerp(1, 0.25f + streak * 1.5f, dab.bristleContrast);
                    }
                    depositWeights[row + x - x0] = w;
                    weightSum += w;
                }
            }
            if (weightSum <= 0) return;
            CaptureHistoryTiles(x0, y0, x1, y1);

            // 1) Pick up wet paint under the bristles.
            float smudge = Mathf.Clamp01(dab.smudge) * 0.45f;
            if (smudge > 0)
            {
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float w = depositWeights[(y - y0) * depositStampWidth + x - x0];
                    if (w <= 0) continue;
                    int i = y * width + x;
                    if (thickness[i] <= 0.000001f) continue;
                    TakeFromCell(i, Mathf.Min(0.9f, smudge * Mathf.Min(1, w)), out float taken, out Vector3 takenMass);
                    load.volume += taken * cellArea;
                    load.mass += takenMass * cellArea;
                    PaperVolume -= taken * cellArea;
                }
            }

            // 2) Lay down clean tube paint mixed with part of the dirty load (wet-in-wet mixing).
            float clean = Mathf.Max(0, units) * UnitVolume;
            float dirtyVolume = load.volume * Mathf.Clamp01(dab.release);
            float total = clean + dirtyVolume;
            if (total > 0.0000001f)
            {
                Vector3 dirtyMass = load.volume > 0 ? load.mass * (dirtyVolume / load.volume) : Vector3.zero;
                load.volume -= dirtyVolume; load.mass -= dirtyMass;
                if (load.volume < 0.0000001f) load.Clear();
                Vector3 density = (Absorption(color) * clean + dirtyMass) / total;
                float scale = total / (weightSum * cellArea);
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float amount = depositWeights[(y - y0) * depositStampWidth + x - x0] * scale;
                    if (amount <= 0) continue;
                    int i = y * width + x;
                    Vector3 added = density * amount;
                    thickness[i] += amount; pigment[i] += added;
                    AddSurfaceLayer(i, amount, added, dab.order);
                }
                DepositedVolume += clean;
                PaperVolume += total;
            }
            ActivateSettling(x0, y0, x1, y1);
            IncludeChanged(x0, y0, x1, y1);
            dirty = true;
        }
    }

    // Paint left on the brush when it is lifted is laid down where the stroke ended (keeps volume exact).
    public void ReleaseBrush(Vector3 world, BrushLoad load, int order, float radius = 0)
    {
        if (load == null || load.volume <= 0) return;
        if (IsReady && Contains(world)) DropLoad(world, load.volume, load.mass, order, Mathf.Max(CellSize * 3,radius));
        load.Clear();
    }

    // ---------------------------------------------------------------- press pad

    private struct PressCell
    {
        public int x, y;
        public float t;        // normalised distance from the centre (0..1)
        public float distance; // visual distance
        public Vector2 dir;    // visual unit direction outward
    }

    private readonly List<PressCell> pressCells = new List<PressCell>(4096);
    private Vector2Int pressKey = new Vector2Int(-1, -1);

    // Presses a flat pad into the paint: wet paint is squeezed outward toward the rim and the pad's
    // texture is imprinted. Calling it every frame while held keeps spreading; moving it smears a trail.
    public void Press(Vector3 world, float radius, float strength, float textureScale, float seed)
    {
        if (!IsReady || IsSpreading || !Contains(world) || strength <= 0) return;
        using (PressMarker.Auto())
        {
            float vdx = VDx, vdy = VDy, cell = CellSize;
            float rx = Mathf.Clamp(radius / vdx, 2, 160), ry = Mathf.Clamp(radius / vdy, 2, 160);
            BuildPressCells(rx, ry, vdx, vdy);
            Vector2 center = GridPoint(world);
            int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
            int reachX = Mathf.CeilToInt(rx * 1.35f) + 2, reachY = Mathf.CeilToInt(ry * 1.35f) + 2;
            int x0 = Mathf.Max(0, cx - reachX), x1 = Mathf.Min(width - 1, cx + reachX);
            int y0 = Mathf.Max(0, cy - reachY), y1 = Mathf.Min(height - 1, cy + reachY);
            CaptureHistoryTiles(x0, y0, x1, y1);
            strength = 1-Mathf.Exp(-Mathf.Max(0,strength));
            float sum=0;int contacted=0;
            for(int n=0;n<pressCells.Count;n++)
            {
                PressCell p=pressCells[n];int x=cx+p.x,y=cy+p.y;
                if(x<0||y<0||x>=width||y>=height)continue;
                sum+=thickness[y*width+x];contacted++;
            }
            float plane=Mathf.Max(Film*.12f,sum/Mathf.Max(1,contacted)*.78f);
            // Outer cells first: paint pushed outward is never moved twice in the same press.
            for (int n = 0; n < pressCells.Count; n++)
            {
                PressCell p = pressCells[n];
                int x = cx + p.x, y = cy + p.y;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                int i = y * width + x;
                if (thickness[i] <= 0.000001f) continue;
                float target=plane*(1+(contactTexture[i]-.5f)*textureScale*.65f);
                float excess=Mathf.Max(0,thickness[i]-target);
                float f=Mathf.Min(excess/thickness[i],strength*(1-p.t*p.t*.65f)*.8f);
                if (f <= 0.0001f) continue;
                Vector2 dir = p.distance > 0.01f * cell ? p.dir : new Vector2(Mathf.Cos(seed + n), Mathf.Sin(seed + n));
                float push = Mathf.Min(radius*.12f,cell*(1+strength*3));
                float tx = x + dir.x * push / vdx, ty = y + dir.y * push / vdy;
                int order = surfaceOrder[i];
                TakeFromCell(i, f, out float taken, out Vector3 mass, out float coat, out Vector3 coatMass);
                SplatBilinear(tx, ty, taken, mass, coat, coatMass, order);
            }
            // Relax the radial transport locally. Conservative neighbour exchange removes grid spokes
            // without blurring pigment across the rest of the canvas or opening a centre hole.
            float smooth=.45f*Mathf.Sqrt(strength);
            for(int pass=0;pass<2;pass++)
            for(int n=0;n<pressCells.Count;n++)
            {
                PressCell p=pressCells[n];int x=cx+p.x,y=cy+p.y;
                if(x<0||y<0||x>=width||y>=height)continue;
                int i=y*width+x;float contact=smooth*(1-p.t*p.t*.7f);
                if(x+1<width)RelaxPressedPair(i,i+1,contact);
                if(y+1<height)RelaxPressedPair(i,i+width,contact);
            }
            ActivateSettling(x0, y0, x1, y1);
            IncludeChanged(x0, y0, x1, y1);
            dirty = true;
        }
    }

    private void RelaxPressedPair(int a,int b,float strength)
    {
        float flux=(thickness[a]-thickness[b])*strength;
        if(Mathf.Abs(flux)<.0000001f)return;
        int from=flux>0?a:b,to=flux>0?b:a;
        float amount=Mathf.Min(Mathf.Abs(flux),thickness[from]*.2f);
        if(amount>0)MoveArtistMass(from,to,amount);
    }

    private void BuildPressCells(float rx, float ry, float vdx, float vdy)
    {
        var key = new Vector2Int(Mathf.RoundToInt(rx * 4), Mathf.RoundToInt(ry * 4));
        if (key == pressKey) return;
        pressKey = key;
        pressCells.Clear();
        int ix = Mathf.CeilToInt(rx), iy = Mathf.CeilToInt(ry);
        for (int y = -iy; y <= iy; y++)
        for (int x = -ix; x <= ix; x++)
        {
            float t = Mathf.Sqrt(x * x / (rx * rx) + y * y / (ry * ry));
            if (t > 1) continue;
            Vector2 visual = new Vector2(x * vdx, y * vdy);
            float distance = visual.magnitude;
            pressCells.Add(new PressCell { x = x, y = y, t = t, distance = distance,
                dir = distance > 0 ? visual / distance : Vector2.zero });
        }
        pressCells.Sort((p, q) => q.t.CompareTo(p.t));
    }

    // ---------------------------------------------------------------- shared helpers

    private void TakeFromCell(int i, float fraction, out float taken, out Vector3 takenMass)
    {
        TakeFromCell(i, fraction, out taken, out takenMass, out _, out _);
    }

    private void TakeFromCell(int i, float fraction, out float taken, out Vector3 takenMass,
        out float coat, out Vector3 coatMass)
    {
        taken = thickness[i] * fraction;
        takenMass = pigment[i] * fraction;
        coat = surfaceThickness[i] * fraction;
        coatMass = surfacePigment[i] * fraction;
        thickness[i] -= taken; pigment[i] -= takenMass;
        surfaceThickness[i] -= coat; surfacePigment[i] -= coatMass;
        if (thickness[i] < 0.0000001f)
        {
            thickness[i] = 0; pigment[i] = Vector3.zero;
            surfaceThickness[i] = 0; surfacePigment[i] = Vector3.zero;
        }
    }

    // Distributes moved paint over the four nearest cells; paint that would leave the paper stays in place.
    private void SplatBilinear(float gx, float gy, float amount, Vector3 mass, float coat, Vector3 coatMass, int order)
    {
        gx = Mathf.Clamp(gx, 0, width - 1); gy = Mathf.Clamp(gy, 0, height - 1);
        int x = Mathf.Min((int)gx, width - 2), y = Mathf.Min((int)gy, height - 2);
        float fx = gx - x, fy = gy - y;
        AddToCell(y * width + x, (1 - fx) * (1 - fy), amount, mass, coat, coatMass, order);
        AddToCell(y * width + x + 1, fx * (1 - fy), amount, mass, coat, coatMass, order);
        AddToCell((y + 1) * width + x, (1 - fx) * fy, amount, mass, coat, coatMass, order);
        AddToCell((y + 1) * width + x + 1, fx * fy, amount, mass, coat, coatMass, order);
    }

    private void AddToCell(int i, float w, float amount, Vector3 mass, float coat, Vector3 coatMass, int order)
    {
        if (w <= 0) return;
        thickness[i] += amount * w; pigment[i] += mass * w;
        // Squeezed paint lands on top, so it becomes the visible coat where it arrives.
        AddSurfaceLayer(i, coat * w, coatMass * w, order);
    }

    // Puts a carried volume back as a small dome (used when a brush is lifted).
    private void DropLoad(Vector3 world, float volume, Vector3 mass, int order, float radius)
    {
        Vector2 center = GridPoint(world);
        float rx = radius / VDx, ry = radius / VDy;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - rx)), x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - ry)), y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + ry));
        float sum = 0;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++) sum += Dome(x, y, center, rx, ry);
        if (sum <= 0) return;
        CaptureHistoryTiles(x0, y0, x1, y1);
        float scale = volume / (sum * cellArea);
        Vector3 density = volume > 0 ? mass / volume : Vector3.zero;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float amount = Dome(x, y, center, rx, ry) * scale;
            if (amount <= 0) continue;
            int i = y * width + x;
            thickness[i] += amount; pigment[i] += density * amount;
            AddSurfaceLayer(i, amount, density * amount, order);
        }
        PaperVolume += volume;
        ActivateSettling(x0, y0, x1, y1);
        IncludeChanged(x0, y0, x1, y1);
        dirty = true;
    }
}
