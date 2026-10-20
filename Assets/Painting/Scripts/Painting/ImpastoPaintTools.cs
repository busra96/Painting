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

    // ---------------------------------------------------------------- palette knife / spatula

    // Paint riding on the blade. Indexed across the blade, like the full-width spreader's reservoir.
    public sealed class KnifeLoad
    {
        internal float[] pool = new float[0], skin = new float[0];
        internal Vector3[] poolMass = new Vector3[0], skinMass = new Vector3[0];
        internal int[] order = new int[0];
        internal int[] lastCell = new int[0];
        internal int count;
        internal Vector2 axis;          // visual units
        internal bool hasAxis;
        internal Vector2 lastCenter;    // grid units
        internal Vector2 travel;

        public float Volume
        {
            get { float v = 0; for (int k = 0; k < count; k++) v += pool[k] + skin[k]; return v; }
        }

        internal void Prepare(int samples)
        {
            if (pool.Length < samples)
            {
                pool = new float[samples]; skin = new float[samples];
                poolMass = new Vector3[samples]; skinMass = new Vector3[samples];
                order = new int[samples];
                lastCell = new int[samples];
            }
            System.Array.Clear(pool, 0, samples); System.Array.Clear(skin, 0, samples);
            System.Array.Clear(poolMass, 0, samples); System.Array.Clear(skinMass, 0, samples);
            System.Array.Clear(order, 0, samples);
            count = samples;
            for(int k=0;k<samples;k++)lastCell[k]=-1;
            hasAxis = false;
        }
    }

    public KnifeLoad BeginKnife(KnifeLoad load, float bladeWidth)
    {
        if (load == null) load = new KnifeLoad();
        int samples = Mathf.Clamp(Mathf.CeilToInt(bladeWidth / Mathf.Max(CellSize, 0.0001f)), 2, 256);
        load.Prepare(samples);
        return load;
    }

    // Drags the blade from 'from' to 'to'. The edge stays perpendicular to the travel (as seen on screen);
    // it scrapes paint above the film into its load and lays a film behind, so paint can be pulled any distance.
    public void KnifeSweep(Vector3 from, Vector3 to, float bladeWidth, float filmScale, float pickup, KnifeLoad load)
    {
        if (!IsReady || IsSpreading || load == null || load.count == 0) return;
        Vector2 a = GridPoint(from), b = GridPoint(to);
        float vdx = VDx, vdy = VDy;
        Vector2 deltaVisual = new Vector2((b.x - a.x) * vdx, (b.y - a.y) * vdy);
        float distance = deltaVisual.magnitude;
        if (distance < CellSize * 0.25f) return;
        using (KnifeMarker.Auto())
        {
            Vector2 travel = deltaVisual / distance;
            load.travel = travel;
            bool firstContact=!load.hasAxis;
            Vector2 axis = new Vector2(-travel.y, travel.x);
            // Keep the blade's left/right consistent so load indices map to the same physical side.
            if (load.hasAxis)
            {
                if (Vector2.Dot(axis, load.axis) < 0) axis = -axis;
                axis = Vector2.Lerp(load.axis, axis, 0.5f).normalized;
            }
            load.axis = axis; load.hasAxis = true;
            // One step ≈ 0.9 of a cell along the travel direction.
            float cellAlongTravel = 1 / Mathf.Sqrt(travel.x * travel.x / (vdx * vdx) + travel.y * travel.y / (vdy * vdy));
            float step = cellAlongTravel * 0.9f;
            int steps = Mathf.Clamp(Mathf.CeilToInt(distance / step), 1, 768);
            float spacing = bladeWidth / load.count, half = bladeWidth * 0.5f;
            float film = Film * Mathf.Max(0.05f, filmScale), ribbons = Ribbons, variation = Variation;
            float poolRatio = ribbons > 0 ? (1 - ribbons) / ribbons : 0;
            float tailSteps = Mathf.Max(2, Tail * VisualScale.y / step);
            pickup = Mathf.Clamp(pickup, 0.1f, 0.98f);
            float ex = Mathf.Abs(axis.x) * half / vdx + 1, ey = Mathf.Abs(axis.y) * half / vdy + 1;
            for (int s = firstContact?0:1; s <= steps; s++)
            {
                Vector2 c = Vector2.Lerp(a, b, s / (float)steps);
                int bx0 = Mathf.Max(0, Mathf.FloorToInt(c.x - ex)), bx1 = Mathf.Min(width - 1, Mathf.CeilToInt(c.x + ex));
                int by0 = Mathf.Max(0, Mathf.FloorToInt(c.y - ey)), by1 = Mathf.Min(height - 1, Mathf.CeilToInt(c.y + ey));
                if (bx0 > bx1 || by0 > by1) continue;
                CaptureHistoryTiles(bx0, by0, bx1, by1);
                int previousCell = -1;
                bool touched=false;
                for (int k = 0; k < load.count; k++)
                {
                    float offset = (k + 0.5f) * spacing - half;
                    int x = Mathf.RoundToInt(c.x + axis.x * offset / vdx), y = Mathf.RoundToInt(c.y + axis.y * offset / vdy);
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    int i = y * width + x;
                    // Neighbouring samples can land on the same stretched cell; work it once.
                    if (i == previousCell) continue;
                    previousCell = i;
                    if(load.lastCell[k]==i)continue;
                    load.lastCell[k]=i;
                    touched=true;
                    KnifeCell(i, k, load, film, ribbons, variation, poolRatio, tailSteps, pickup);
                }
                if(touched)SmoothKnifeLoad(load);
                ActivateSettling(bx0, by0, bx1, by1);
                IncludeChanged(bx0, by0, bx1, by1);
            }
            load.lastCenter = b;
            dirty = true;
        }
    }

    private void KnifeCell(int i, int k, KnifeLoad load, float film, float ribbons, float variation,
        float poolRatio, float tailSteps, float pickup)
    {
        // Scrape: same layered pickup as the full-width spreader (fresh coat → skin, the rest → pool).
        float collected = Mathf.Max(0,thickness[i]-film*.45f) * pickup;
        if (collected > 0)
        {
            float fraction=collected/thickness[i];
            Vector3 mass = pigment[i] * fraction;
            float visible = surfaceThickness[i] * fraction;
            Vector3 visibleMass = surfacePigment[i] * fraction;
            thickness[i] -= collected; pigment[i] -= mass;
            surfaceThickness[i] -= visible; surfacePigment[i] -= visibleMass;
            if (visible > 0)
            {
                if (load.order[k] != surfaceOrder[i])
                {
                    MovePaint(load.skin, load.skinMass, k, load.pool, load.poolMass, k, load.skin[k]);
                    load.order[k] = surfaceOrder[i];
                }
                load.skin[k] += visible * cellArea * ribbons;
                load.skinMass[k] += visibleMass * (cellArea * ribbons);
            }
            load.pool[k] += collected * cellArea - visible * cellArea * ribbons;
            load.poolMass[k] += (mass - visibleMass * ribbons) * cellArea;
            PaperVolume -= collected * cellArea;
        }
        // Lay the film behind the blade.
        float carried = load.pool[k] + load.skin[k];
        if (carried <= 0) return;
        float grain = contactTexture[i];
        float loadRatio = carried / (dx * 0.12f);
        float target = film * (1 + (grain - 0.5f) * variation) * Mathf.Lerp(0.9f, 1.2f, Mathf.Clamp01(loadRatio / 2)) * cellArea;
        float fullness = Mathf.Sqrt(Mathf.Clamp01(carried / (target * tailSteps)));
        float wetting = Mathf.Lerp(0.45f, 1, Mathf.SmoothStep(0, 1, Mathf.Clamp01(fullness * 2.2f + (grain - 0.5f) * 0.9f)));
        float volume = Mathf.Min(carried, Mathf.Max(0,target-thickness[i]*cellArea) * fullness * wetting);
        float coatSupply = Mathf.Clamp01(load.skin[k] / (target * tailSteps));
        float coverage = coatSupply * coatSupply * (3 - 2 * coatSupply);
        float fromSkin = Mathf.Min(load.skin[k], volume * coverage);
        float fromPool = Mathf.Min(load.pool[k], volume - fromSkin);
        // As in the full-width spreader, buried paint only leaves alongside a fresh coat; the rest stays on the
        // blade and is laid as a ridge when the knife is lifted (EndKnife).
        if (ribbons > 0) fromPool = Mathf.Min(fromPool, fromSkin * poolRatio);
        Vector3 fresh = TakePaint(load.skin, load.skinMass, k, fromSkin);
        Vector3 old = TakePaint(load.pool, load.poolMass, k, fromPool);
        float laid = fromSkin + fromPool;
        if (laid <= 0) return;
        thickness[i] += laid / cellArea;
        pigment[i] += (fresh + old) / cellArea;
        if (fromSkin > 0) AddSurfaceLayer(i, fromSkin / cellArea, fresh / cellArea, load.order[k]);
        else AddSurfaceLayer(i, fromPool / cellArea, old / cellArea, 0);
        PaperVolume += laid;
    }

    // Heavy loads squeeze sideways along the edge (conservative exchange between neighbours).
    private void SmoothKnifeLoad(KnifeLoad load)
    {
        float flow = Mathf.Clamp01(SideFlow * 25);
        if (flow <= 0) return;
        for (int k = 0; k < load.count - 1; k++)
        {
            float a = load.pool[k], b = load.pool[k + 1];
            float mobility = Mathf.SmoothStep(0, 1, Mathf.Clamp01((a + b) / (2 * dx * 0.12f) / 0.8f));
            float move = (a - b) * 0.25f * flow * mobility;
            if (move > 0) MovePaint(load.pool, load.poolMass, k, load.pool, load.poolMass, k + 1, move);
            else if (move < 0) MovePaint(load.pool, load.poolMass, k + 1, load.pool, load.poolMass, k, -move);
        }
    }

    // Lifting the knife leaves its remaining paint as a ridge along the blade's last position.
    public void EndKnife(KnifeLoad load, float bladeWidth)
    {
        if (!IsReady || load == null || load.count == 0 || !load.hasAxis) return;
        float spacing = bladeWidth / load.count, half = bladeWidth * 0.5f;
        float vdx = VDx, vdy = VDy;
        Vector2 c = load.lastCenter;
        float depth=Mathf.Clamp(load.Volume/Mathf.Max(.001f,bladeWidth*.3f),CellSize*2,bladeWidth*.3f);
        int steps=Mathf.Clamp(Mathf.CeilToInt(depth/CellSize),2,16);
        float weightSum=0;
        for(int s=0;s<steps;s++){float t=s/(float)(steps-1);weightSum+=(1-t)*(1-t)+.15f;}
        for (int k = 0; k < load.count; k++)
        {
            float volume = load.pool[k] + load.skin[k];
            if (volume <= 0) continue;
            float offset = (k + 0.5f) * spacing - half;
            Vector3 mass = load.poolMass[k] + load.skinMass[k];
            for(int s=0;s<steps;s++)
            {
                float t=s/(float)(steps-1),weight=((1-t)*(1-t)+.15f)/weightSum;
                Vector2 p=c+new Vector2((load.axis.x*offset-load.travel.x*depth*t)/vdx,(load.axis.y*offset-load.travel.y*depth*t)/vdy);
                int x=Mathf.Clamp(Mathf.RoundToInt(p.x),0,width-1),y=Mathf.Clamp(Mathf.RoundToInt(p.y),0,height-1),i=y*width+x;
                CaptureHistoryTiles(x,y,x,y);
                thickness[i]+=volume*weight/cellArea; pigment[i]+=mass*(weight/cellArea);
                AddSurfaceLayer(i,load.skin[k]*weight/cellArea,load.skinMass[k]*(weight/cellArea),load.order[k]);
                int bx0=Mathf.Max(0,x-2),by0=Mathf.Max(0,y-2),bx1=Mathf.Min(width-1,x+2),by1=Mathf.Min(height-1,y+2);
                ActivateSettling(bx0,by0,bx1,by1);IncludeChanged(bx0,by0,bx1,by1);
            }
            PaperVolume += volume;
        }
        load.Prepare(load.count);
        dirty = true;
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
