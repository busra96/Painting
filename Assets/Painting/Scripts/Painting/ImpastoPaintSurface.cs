using System;
using PaintIn2D;
using UnityEngine;
using Unity.Profiling;

// Pigment mass and paint volume live here. PaintIn2D remains the albedo/render-texture and scoring backend.
[DisallowMultipleComponent]
[DefaultExecutionOrder(30)]
public sealed partial class ImpastoPaintSurface : MonoBehaviour
{
    public static ImpastoPaintSurface Active { get; private set; }
    public SettingsContain settings;
    [SerializeField, Range(192, 768)] private int resolution = 512;
    [SerializeField] private Shader paintShader;
    [SerializeField] private Shader spatulaShader;
    public bool IsReady { get; private set; }
    public bool IsSpreading { get; private set; }
    public double DepositedVolume { get; private set; }
    public double PaperVolume { get; private set; }
    public double BladeVolume { get; private set; }
    public float BladeLoad => (float)BladeVolume;
    public int Width => width;
    public int Height => height;
    public Texture HeightTexture => heightTexture;
    public bool RevealBackdropCanvas { get; set; }
    public int SettlingTilesProcessedLastStep { get; private set; }
    public int RecoloredPixelsLastFlush { get; private set; }
    public int UploadCount { get; private set; }
    public int AlbedoSyncCount { get; private set; }

    private SpriteRenderer paper;
    private CwPaintableSpriteTexture paintable;
    private Material originalMaterial, paintMaterial, bladeMaterial, pigmentReadbackMaterial;
    private Texture2D colorTexture, heightTexture, reservoirTexture;
    private Sprite bladeSprite;
    private float[] thickness, blade, bladeSkin, contactTexture;
    private Vector3[] pigment, bladePigment, skinPigment;
    // Visible surface material is a subset of the total mass, not an extra supply of paint.
    private float[] surfaceThickness;
    private Vector3[] surfacePigment;
    private int[] surfaceOrder, bladeOrder;
    private int depositOrder;
    private Color lastDepositColor;
    private bool hasDepositColor;
    private float[] flowEdges, flowUpper, flowVolume;
    private float[] depositWeights;
    private int depositStampWidth, depositStampHeight;
    private const int DomeSize = 65;
    private float[] domeKernel;
    private Vector3[] flowPigment;
    private Color[] reservoirPixels;
    private Bounds localBounds;
    private int width, height, nextRow;
    private float dx, dy, cellArea, previousBladeY;
    private bool dirty, albedoNeedsSync, reservoirDirty;
    private const int TileSize = 32;
    private int tilesX, tilesY, settlingCursor;
    private byte[] settlingTicks;
    private bool[] changedTiles;
    private int activeSettlingTiles;
    private float settleAccumulator;
    private static readonly ProfilerMarker DepositMarker = new ProfilerMarker("Painting.Deposit");
    private static readonly ProfilerMarker SettleMarker = new ProfilerMarker("Painting.SettleTiles");
    private static readonly ProfilerMarker UploadMarker = new ProfilerMarker("Painting.Upload");

    public float UnitVolume => settings != null ? settings.PaintVolumePerUnit : 0.12f;
    public float BrushRadius => settings != null ? settings.DepositRadius : 0.24f;
    private float Film => settings != null ? settings.FilmThickness : 0.085f;
    private float Tail => settings != null ? settings.FeatherDistance : 0.4f;
    private float SideFlow => settings != null ? settings.LateralFlow : 0.012f;
    private float Ribbons => settings != null ? settings.ColorRibbons : 0.68f;
    private float Variation => settings != null ? settings.SurfaceVariation : 0.32f;
    private float Opacity => settings != null ? settings.PigmentOpacity : 65f;

    private void Awake() => Active = this;

    public void Initialize()
    {
        if (IsReady) return;
        paper = GetComponent<SpriteRenderer>();
        paintable = GetComponent<CwPaintableSpriteTexture>();
        if (paper == null || paper.sprite == null || paintable == null || paintable.Current == null) return;
        localBounds = paper.sprite.bounds;
        width = Application.isMobilePlatform ? Mathf.Min(resolution, 384) : resolution;
        height = Mathf.RoundToInt(width * localBounds.size.y / localBounds.size.x);
        dx = paper.bounds.size.x / width;
        dy = paper.bounds.size.y / height;
        cellArea = dx * dy;
        thickness = new float[width * height];
        pigment = new Vector3[thickness.Length];
        surfaceThickness = new float[thickness.Length];
        surfacePigment = new Vector3[thickness.Length];
        surfaceOrder = new int[thickness.Length];
        tilesX = (width + TileSize - 1) / TileSize;
        tilesY = (height + TileSize - 1) / TileSize;
        settlingTicks = new byte[tilesX * tilesY];
        changedTiles = new bool[settlingTicks.Length];
        blade = new float[width];
        bladeSkin = new float[width];
        bladeOrder = new int[width];
        flowEdges = new float[width];
        flowUpper = new float[width];
        flowVolume = new float[width];
        flowPigment = new Vector3[width];
        domeKernel = new float[DomeSize * DomeSize];
        for (int y = 0; y < DomeSize; y++)
        for (int x = 0; x < DomeSize; x++)
        {
            float nx = x * 2.4f / (DomeSize - 1) - 1.2f, ny = y * 2.4f / (DomeSize - 1) - 1.2f;
            float irregularity = 1 + 0.025f * Mathf.Sin(nx * 3 + ny * 4) + 0.015f * Mathf.Sin(nx * 7 - ny * 5);
            domeKernel[y * DomeSize + x] = Mathf.Pow(Mathf.Max(0, 1 - (nx * nx + ny * ny) * irregularity), 1.5f);
        }
        float stampRadiusX = BrushRadius / dx * 1.2f;
        float stampRadiusY = BrushRadius / dy * 1.2f;
        depositStampWidth = Mathf.CeilToInt(stampRadiusX * 2) + 2;
        depositStampHeight = Mathf.CeilToInt(stampRadiusY * 2) + 2;
        depositWeights = new float[depositStampWidth * depositStampHeight];
        contactTexture = new float[thickness.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float worldX = (x + 0.5f) * dx;
            float worldY = (y + 0.5f) * dy;
            float broadGrain = Mathf.PerlinNoise(worldX * 20.3f, worldY * 0.75f + 12.7f);
            float fineGrain = 0.5f + 0.5f * Mathf.Sin(worldX * 67.1f + worldY * 4.3f);
            contactTexture[y * width + x] = 0.85f * broadGrain + 0.15f * fineGrain;
        }
        bladePigment = new Vector3[width];
        skinPigment = new Vector3[width];
        reservoirPixels = new Color[width];
        colorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false) { name = "Impasto pigment", wrapMode = TextureWrapMode.Clamp };
        heightTexture = new Texture2D(width, height, TextureFormat.RFloat, false, true) { name = "Impasto thickness", wrapMode = TextureWrapMode.Clamp };
        reservoirTexture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false, true) { name = "Paint on blade", wrapMode = TextureWrapMode.Clamp };
        originalMaterial = paper.sharedMaterial;
        pigmentReadbackMaterial = new Material(Resources.Load<Shader>("Shaders/ImpastoReadback"));
        paintMaterial = new Material(paintShader != null ? paintShader : Shader.Find("Painting/Impasto Paper")) { name = "Wet paint and canvas" };
        paintMaterial.SetTexture("_HeightTex", heightTexture);
        paintMaterial.SetTexture("_PigmentTex", colorTexture);
        paintMaterial.SetTexture("_ReservoirTex", reservoirTexture);
        paintMaterial.SetFloat("_BackdropCanvas", RevealBackdropCanvas ? 1 : 0);
        paintMaterial.SetVector("_PaperSize", new Vector4(paper.bounds.size.x, paper.bounds.size.y, dx, dy));
        Rect spriteRect = paper.sprite.rect;
        Texture2D spriteTexture = paper.sprite.texture;
        paintMaterial.SetVector("_SpriteUVRect", new Vector4(spriteRect.x / spriteTexture.width,
            spriteRect.y / spriteTexture.height, spriteRect.width / spriteTexture.width, spriteRect.height / spriteTexture.height));
        paintMaterial.SetFloat("_Relief", settings != null ? settings.ReliefHeight : 0.075f);
        paintMaterial.SetFloat("_Gloss", settings != null ? settings.WetGloss : 0.32f);
        paintMaterial.SetFloat("_ShadowStrength", settings != null ? settings.ContactShadow : 0.32f);
        paintMaterial.SetFloat("_ImpastoStyle", settings != null ? settings.SculptedOilLighting : 0);
        paper.sharedMaterial = paintMaterial;
        IsReady = true;
        IncludeChanged(0, 0, width - 1, height - 1);
        dirty = true;
        reservoirDirty = true;
        Flush();
        SyncAlbedo();
    }

    public bool Contains(Vector3 world)
    {
        if (!IsReady) return false;
        Vector3 local = transform.InverseTransformPoint(world);
        return local.x >= localBounds.min.x && local.x <= localBounds.max.x && local.y >= localBounds.min.y && local.y <= localBounds.max.y;
    }

    private Vector2 GridPoint(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return new Vector2((local.x - localBounds.min.x) / localBounds.size.x * width - 0.5f,
            (local.y - localBounds.min.y) / localBounds.size.y * height - 0.5f);
    }

    // Every sample is normalized: depositing 3 units creates exactly 3 times the volume, including at paper edges.
    public void Deposit(Vector3 world, Color color, float units, Vector2 direction)
    {
        if (!IsReady || IsSpreading || units <= 0 || !Contains(world)) return;
        DepositInside(world, color, units, direction);
    }

    // radius <= 0 uses Settings.DepositRadius (the original pour). Tools pass their own nozzle size.
    internal void DepositInside(Vector3 world, Color color, float units, Vector2 direction, float radius = -1)
    {
        if (!IsReady || IsSpreading || units <= 0) return;
        using (DepositMarker.Auto())
        {
            Vector2 center = GridPoint(world);
            if (radius <= 0) radius = BrushRadius;
            // VisualScale is (1,1) unless the atelier sets it, so the original pour is unchanged.
            float rx = radius / (dx * VisualScale.x), ry = radius / (dy * VisualScale.y);
            int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - rx * 1.2f));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rx * 1.2f));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - ry * 1.2f));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + ry * 1.2f));
            int stampWidth = x1 - x0 + 1, stampHeight = y1 - y0 + 1;
            EnsureDepositCapacity(stampWidth, stampHeight);
            float weight = 0;
            for (int y = y0; y <= y1; y++)
            {
                int row = (y - y0) * depositStampWidth;
                for (int x = x0; x <= x1; x++)
                {
                    float value = Dome(x, y, center, rx, ry);
                    depositWeights[row + x - x0] = value;
                    weight += value;
                }
            }
            if (weight <= 0) return;
            float volume = units * UnitVolume;
            float scale = volume / (weight * cellArea);
            Vector3 density = Absorption(color);
            CaptureHistoryTiles(x0, y0, x1, y1);
            if (!hasDepositColor || color != lastDepositColor)
            {
                depositOrder++;
                lastDepositColor = color;
                hasDepositColor = true;
            }
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float amount = depositWeights[(y - y0) * depositStampWidth + x - x0] * scale;
                if (amount <= 0) continue;
                int index = y * width + x;
                Vector3 addedMass = density * amount;
                thickness[index] += amount;
                pigment[index] += addedMass;
                AddSurfaceLayer(index, amount, addedMass, depositOrder);
            }
            DepositedVolume += volume;
            PaperVolume += volume;
            ActivateSettling(x0, y0, x1, y1);
            IncludeChanged(x0, y0, x1, y1);
            dirty = true;
        }
    }

    private void EnsureDepositCapacity(int stampWidth, int stampHeight)
    {
        if (stampWidth <= depositStampWidth && stampHeight <= depositStampHeight) return;
        depositStampWidth = Mathf.Max(depositStampWidth, stampWidth);
        depositStampHeight = Mathf.Max(depositStampHeight, stampHeight);
        depositWeights = new float[depositStampWidth * depositStampHeight];
    }

    private float Dome(int x, int y, Vector2 center, float rx, float ry)
    {
        float nx = (x - center.x) / rx, ny = (y - center.y) / ry;
        float u = (nx + 1.2f) * ((DomeSize - 1) / 2.4f), v = (ny + 1.2f) * ((DomeSize - 1) / 2.4f);
        if (u < 0 || v < 0 || u >= DomeSize - 1 || v >= DomeSize - 1) return 0;
        int ix = (int)u, iy = (int)v, index = iy * DomeSize + ix;
        float fx = u - ix, fy = v - iy;
        float a = domeKernel[index] + (domeKernel[index + 1] - domeKernel[index]) * fx;
        float b = domeKernel[index + DomeSize] + (domeKernel[index + DomeSize + 1] - domeKernel[index + DomeSize]) * fx;
        return a + (b - a) * fy;
    }

    private void ActivateSettling(int x0, int y0, int x1, int y1)
    {
        for (int y = Mathf.Max(0, y0 - 3) / TileSize; y <= Mathf.Min(height - 1, y1 + 3) / TileSize; y++)
        for (int x = Mathf.Max(0, x0 - 3) / TileSize; x <= Mathf.Min(width - 1, x1 + 3) / TileSize; x++)
        {
            int tile = y * tilesX + x;
            if (settlingTicks[tile] == 0) activeSettlingTiles++;
            settlingTicks[tile] = 18;
        }
    }

    private void IncludeChanged(int x0, int y0, int x1, int y1)
    {
        for (int y = y0 / TileSize; y <= y1 / TileSize; y++)
        for (int x = x0 / TileSize; x <= x1 / TileSize; x++) changedTiles[y * tilesX + x] = true;
    }

    // A yield threshold keeps thick acrylic in rounded heaps instead of diffusing it like water.
    private void SettleTile(int tile)
    {
        int x0 = tile % tilesX * TileSize, y0 = tile / tilesX * TileSize;
        int x1 = Mathf.Min(width, x0 + TileSize), y1 = Mathf.Min(height, y0 + TileSize);
        CaptureHistoryTiles(x0, y0, Mathf.Min(width - 1, x1), Mathf.Min(height - 1, y1));
        bool moved = false;
        for (int pass = 0; pass < 3; pass++)
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            int index = y * width + x;
            if (x + 1 < width) moved |= LevelPair(index, index + 1);
            if (y + 1 < height) moved |= LevelPair(index, index + width);
        }
        if (moved)
        {
            IncludeChanged(x0, y0, Mathf.Min(width - 1, x1), Mathf.Min(height - 1, y1));
            dirty = true;
        }
        else settlingTicks[tile] = 1;
    }

    public void AdvanceSettling(float deltaTime, int tileBudget = int.MaxValue)
    {
        SettlingTilesProcessedLastStep = 0;
        if (!IsReady || IsSpreading || activeSettlingTiles == 0 || deltaTime <= 0) return;
        using (SettleMarker.Auto())
        {
            settleAccumulator += deltaTime;
            // Keep pointer input responsive even after a slow editor/GPU frame. Unfinished work resumes next frame.
            if (tileBudget != int.MaxValue) settleAccumulator = Mathf.Min(settleAccumulator, 0.05f);
            while (settleAccumulator + 0.000001f >= 1f / 60 && activeSettlingTiles > 0)
            {
                for (; settlingCursor < settlingTicks.Length; settlingCursor++)
                {
                    if (settlingTicks[settlingCursor] == 0) continue;
                    if (SettlingTilesProcessedLastStep >= tileBudget) return;
                    SettleTile(settlingCursor);
                    if (--settlingTicks[settlingCursor] == 0) activeSettlingTiles--;
                    SettlingTilesProcessedLastStep++;
                }
                settlingCursor = 0;
                settleAccumulator -= 1f / 60;
            }
            if (activeSettlingTiles == 0) settleAccumulator = 0;
        }
    }

    private bool LevelPair(int a, int b)
    {
        float difference = thickness[a] - thickness[b];
        if (Mathf.Abs(difference) < 0.18f || Mathf.Max(thickness[a], thickness[b]) < 0.9f) return false;
        int from = difference > 0 ? a : b, to = difference > 0 ? b : a;
        float amount = Mathf.Min(thickness[from] * 0.12f, (Mathf.Abs(difference) - 0.18f) * 0.1f);
        float fraction = amount / Mathf.Max(thickness[from], 0.000001f);
        Vector3 mass = pigment[from] * fraction;
        float visible = surfaceThickness[from] * fraction;
        Vector3 visibleMass = surfacePigment[from] * fraction;
        surfaceThickness[from] -= visible;
        surfacePigment[from] -= visibleMass;
        thickness[from] -= amount; thickness[to] += amount;
        pigment[from] -= mass; pigment[to] += mass;
        // An older neighboring color cannot move above a newer surface coat while heaps relax.
        if (surfaceOrder[from] >= surfaceOrder[to])
            AddSurfaceLayer(to, visible, visibleMass, surfaceOrder[from]);
        return true;
    }

    private void AddSurfaceLayer(int index, float amount, Vector3 mass, int order)
    {
        if (amount <= 0) return;
        if (surfaceOrder[index] != order)
        {
            surfaceThickness[index] = 0;
            surfacePigment[index] = Vector3.zero;
            surfaceOrder[index] = order;
        }
        surfaceThickness[index] += amount;
        surfacePigment[index] += mass;
    }

    public void BeginSpread(float worldY)
    {
        if (!IsReady) return;
        recordingState = null;
        Flush();
        Array.Clear(settlingTicks, 0, settlingTicks.Length);
        activeSettlingTiles = settlingCursor = 0; settleAccumulator = 0;
        IsSpreading = true;
        previousBladeY = worldY;
        nextRow = height - 1;
        paintMaterial.SetFloat("_BladeVisible", 1);
        UpdateBladeUniform(worldY);
    }

    public void SpreadTo(float worldY, float speed = 1)
    {
        if (!IsSpreading || worldY >= previousBladeY) return;
        Vector2 grid = GridPoint(new Vector3(paper.bounds.center.x, worldY, transform.position.z));
        int lastRow = Mathf.Max(0, Mathf.CeilToInt(grid.y));
        int firstRow = nextRow;
        // Process each crossed row exactly once. Fast/slow drags transfer the same material volume.
        while (nextRow >= lastRow && nextRow >= 0) SweepRow(nextRow--);
        previousBladeY = worldY;
        UpdateBladeUniform(worldY);
        dirty |= nextRow != firstRow;
    }

    private void SweepRow(int y)
    {
        reservoirDirty = true;
        IncludeChanged(0, y, width - 1, y);
        const float pickup = 0.96f;
        float ribbons = Ribbons, film = Film, variation = Variation;
        float poolRatio = ribbons > 0 ? (1 - ribbons) / ribbons : 0;
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            float collected = thickness[i] * pickup;
            Vector3 mass = pigment[i] * pickup;
            float visible = surfaceThickness[i] * pickup;
            Vector3 visibleMass = surfacePigment[i] * pickup;
            thickness[i] -= collected; pigment[i] -= mass;
            surfaceThickness[i] -= visible; surfacePigment[i] -= visibleMass;
            float volume = collected * cellArea;
            if (visible > 0)
            {
                // Meeting a different coat rolls the previous contact layer behind the new paint.
                // Its mass remains in the pool, but cannot shine through a well supplied fresh coat.
                if (bladeOrder[x] != surfaceOrder[i])
                {
                    MovePaint(bladeSkin, skinPigment, x, blade, bladePigment, x, bladeSkin[x]);
                    bladeOrder[x] = surfaceOrder[i];
                }
                bladeSkin[x] += visible * cellArea * ribbons;
                skinPigment[x] += visibleMass * (cellArea * ribbons);
            }
            blade[x] += volume - visible * cellArea * ribbons;
            bladePigment[x] += (mass - visibleMass * ribbons) * cellArea;
            PaperVolume -= volume; BladeVolume += volume;
        }
        FlowAlongBlade();
        float tailRows = Mathf.Max(2, Tail / dy);
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            float carried = blade[x] + bladeSkin[x];
            if (carried <= 0) continue;
            float grain = contactTexture[i];
            float load = carried / (dx * 0.12f);
            // Contact changes along the travel as well as across the blade, so ridges bend and terminate.
            float contact = 1 + (grain - 0.5f) * variation;
            float target = film * contact * Mathf.Lerp(0.9f, 1.2f, Mathf.Clamp01(load / 2)) * cellArea;
            float fullness = Mathf.Sqrt(Mathf.Clamp01(carried / (target * tailRows)));
            float wetting = Mathf.Lerp(0.45f, 1, Mathf.SmoothStep(0, 1,
                Mathf.Clamp01(fullness * 2.2f + (grain - 0.5f) * 0.9f)));
            float volume = Mathf.Min(carried, target * fullness * wetting);
            // The fresh contact coat thins over a finite tail instead of ending abruptly.
            float coatSupply = Mathf.Clamp01(bladeSkin[x] / (target * tailRows));
            float coatCoverage = coatSupply * coatSupply * (3 - 2 * coatSupply);
            float fromSkin = Mathf.Min(bladeSkin[x], volume * coatCoverage);
            float fromPool = Mathf.Min(blade[x], volume - fromSkin);
            // Only material in contact with the fresh face can leave the older reservoir.
            // Buried paint remains on the blade when that face runs dry, rather than coming
            // back as another copy of an earlier stroke. Ribbons=0 retains deliberate mixing.
            if (ribbons > 0)
                fromPool = Mathf.Min(fromPool, fromSkin * poolRatio);
            Vector3 freshMass = TakePaint(bladeSkin, skinPigment, x, fromSkin);
            Vector3 poolMass = TakePaint(blade, bladePigment, x, fromPool);
            volume = fromSkin + fromPool;
            thickness[i] += volume / cellArea;
            pigment[i] += (freshMass + poolMass) / cellArea;
            // Older mixed paint sits below the fresh coat. Combining both masses into a single
            // visible layer makes buried strokes appear as extra muddy marks inside the new color.
            if (fromSkin > 0)
                AddSurfaceLayer(i, fromSkin / cellArea, freshMass / cellArea, bladeOrder[x]);
            else if (fromPool > 0)
                AddSurfaceLayer(i, fromPool / cellArea, poolMass / cellArea, 0);
            PaperVolume += volume; BladeVolume -= volume;
        }
    }

    private void FlowAlongBlade()
    {
        // A no-flux implicit diffusion solve is positive and conservative without resolution-dependent substeps.
        float coefficient = SideFlow * dy / (dx * dx);
        if (coefficient <= 0) return;
        bool flowing = false;
        for (int x = 0; x < width - 1; x++)
        {
            float load = (blade[x] + bladeSkin[x] + blade[x + 1] + bladeSkin[x + 1]) / (2 * dx * 0.12f);
            float mobility = Mathf.SmoothStep(0, 1, Mathf.Clamp01(load / 0.8f));
            flowEdges[x] = coefficient * mobility;
            flowing |= flowEdges[x] > 0;
        }
        flowEdges[width - 1] = 0;
        if (!flowing) return;
        SolveBladeLayer(blade, bladePigment, 1);
        for (int x = 0; x < width - 1; x++)
        {
            // Different exposed coats displace one another instead of diffusing old pigment through a fresh cover.
            if (bladeSkin[x] <= 0 && bladeSkin[x + 1] > 0) bladeOrder[x] = bladeOrder[x + 1];
            if (bladeSkin[x + 1] <= 0 && bladeSkin[x] > 0) bladeOrder[x + 1] = bladeOrder[x];
            if (bladeOrder[x] != bladeOrder[x + 1]) flowEdges[x] = 0;
        }
        SolveBladeLayer(bladeSkin, skinPigment, 1.4f);
    }

    private void SolveBladeLayer(float[] volume, Vector3[] mass, float mobility)
    {
        // The same symmetric matrix transports volume and each absorption channel; no pigment is created or discarded.
        for (int x = 0; x < width; x++)
        {
            float left = x > 0 ? flowEdges[x - 1] * mobility : 0;
            float right = flowEdges[x] * mobility;
            float inverse = 1 / (1 + left + right - (x > 0 ? left * flowUpper[x - 1] : 0));
            flowUpper[x] = right * inverse;
            flowVolume[x] = (volume[x] + (x > 0 ? left * flowVolume[x - 1] : 0)) * inverse;
            flowPigment[x] = (mass[x] + (x > 0 ? left * flowPigment[x - 1] : Vector3.zero)) * inverse;
        }
        for (int x = width - 1; x >= 0; x--)
        {
            volume[x] = flowVolume[x] + (x < width - 1 ? flowUpper[x] * volume[x + 1] : 0);
            mass[x] = flowPigment[x] + (x < width - 1 ? flowUpper[x] * mass[x + 1] : Vector3.zero);
        }
    }

    private static void MovePaint(float[] source, Vector3[] sourceMass, int from,
        float[] destination, Vector3[] destinationMass, int to, float amount)
    {
        amount = Mathf.Min(source[from], amount);
        if (amount <= 0) return;
        Vector3 pigmentMass = TakePaint(source, sourceMass, from, amount);
        destination[to] += amount;
        destinationMass[to] += pigmentMass;
    }

    private static Vector3 TakePaint(float[] volume, Vector3[] mass, int index, float amount)
    {
        if (amount <= 0 || volume[index] <= 0) return Vector3.zero;
        Vector3 taken = mass[index] * Mathf.Min(1, amount / volume[index]);
        volume[index] = Mathf.Max(0, volume[index] - amount);
        mass[index] -= taken;
        return taken;
    }

    private void UpdateBladeUniform(float worldY)
    {
        float uvY = (worldY - paper.bounds.min.y) / paper.bounds.size.y;
        paintMaterial.SetFloat("_BladeY", uvY);
    }

    public void FinishSpread()
    {
        if (!IsReady) return;
        paintMaterial.SetFloat("_BladeVisible", 0);
        IsSpreading = false;
        Flush();
    }

    public void Flush()
    {
        if (!IsReady || !dirty) return;
        using (UploadMarker.Auto())
        {
            RecoloredPixelsLastFlush = 0;
            var pixels = colorTexture.GetPixelData<Color32>(0);
            var heights = heightTexture.GetPixelData<float>(0);
            float opacity = Opacity;
            // Recompute absorption only in changed tiles, including when deposits are far apart.
            for (int tile = 0; tile < changedTiles.Length; tile++)
            {
                if (!changedTiles[tile]) continue;
                changedTiles[tile] = false;
                int x0 = tile % tilesX * TileSize, y0 = tile / tilesX * TileSize;
                int x1 = Mathf.Min(width, x0 + TileSize), y1 = Mathf.Min(height, y0 + TileSize);
                RecoloredPixelsLastFlush += (x1 - x0) * (y1 - y0);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int i = y * width + x;
                    float h = thickness[i];
                    heights[i] = h;
                    if (h < 0.000001f) { pixels[i] = new Color32(0, 0, 0, 0); continue; }
                    // Encode premultiplied linear pigment in the sRGB texture. Hardware filtering
                    // then interpolates color and coverage together without borrowing white paper.
                    pixels[i] = VisiblePaint(i, opacity).gamma;
                }
            }
            // Write straight into Texture2D backing storage; avoid copying the full managed arrays each frame.
            colorTexture.Apply(false);
            heightTexture.Apply(false);
            UploadCount++;
            if (reservoirDirty)
            {
                for (int x = 0; x < width; x++)
                {
                    float carried = blade[x] + bladeSkin[x];
                    if (carried < 0.00000001f) { reservoirPixels[x] = Color.clear; continue; }
                    Vector3 density = (bladePigment[x] + skinPigment[x]) / carried;
                    if (bladeSkin[x] > 0.00000001f) density = Vector3.Lerp(density, skinPigment[x] / bladeSkin[x], 0.65f);
                    Color color = PigmentColor(density).linear;
                    color.a = Mathf.Min(4, carried / (dx * 0.12f));
                    reservoirPixels[x] = color;
                }
                reservoirTexture.SetPixels(reservoirPixels);
                reservoirTexture.Apply(false);
                reservoirDirty = false;
            }
            albedoNeedsSync = true;
            dirty = false;
        }
    }

    // The package texture is needed for result readback, not as an extra full-page blit on every pointer frame.
    public void SyncAlbedo()
    {
        Flush();
        if (!IsReady || !albedoNeedsSync) return;
        RenderTexture previous = RenderTexture.active;
        try { Graphics.Blit(colorTexture, paintable.Current, pigmentReadbackMaterial); }
        finally { RenderTexture.active = previous; }
        albedoNeedsSync = false;
        AlbedoSyncCount++;
    }

    public float SampleThickness(Vector3 world)
    {
        if (!Contains(world)) return 0;
        Vector2 p = GridPoint(world);
        return thickness[Mathf.Clamp(Mathf.RoundToInt(p.y), 0, height - 1) * width + Mathf.Clamp(Mathf.RoundToInt(p.x), 0, width - 1)];
    }

    public Color SamplePigment(Vector3 world)
    {
        if (!Contains(world)) return Color.white;
        Vector2 p = GridPoint(world);
        int index = Mathf.Clamp(Mathf.RoundToInt(p.y), 0, height - 1) * width + Mathf.Clamp(Mathf.RoundToInt(p.x), 0, width - 1);
        return VisibleColor(index, Opacity);
    }

    private Color VisibleColor(int index, float opacity)
    {
        Color paint = VisiblePaint(index, opacity);
        float paper = 1 - paint.a;
        return new Color(paint.r + paper, paint.g + paper, paint.b + paper, 1).gamma;
    }

    private Color VisiblePaint(int index, float opacity)
    {
        float h = thickness[index];
        if (h <= 0.000001f) return Color.clear;
        float top = Mathf.Min(h, surfaceThickness[index]);
        if (top > 0.000001f)
        {
            Color coat = PigmentLinear(surfacePigment[index] / surfaceThickness[index]);
            // Let fresh surface pigment cover older marks cleanly; thin edges still reveal the underlying coats.
            float coatOpacity = opacity * 1.8f;
            // At >99.5% coverage the buried color is visually negligible; skip its optical conversion.
            if (top * coatOpacity >= 5.3f) return coat;
            float underneath = h - top;
            Color baseColor = Color.clear;
            if (underneath > 0.000001f)
            {
                Vector3 mass = Vector3.Max(Vector3.zero, pigment[index] - surfacePigment[index]);
                float baseCoverage = 1 - Mathf.Exp(-underneath * opacity);
                baseColor = PigmentLinear(mass / underneath) * baseCoverage;
            }
            return Color.Lerp(baseColor, coat, 1 - Mathf.Exp(-top * coatOpacity));
        }
        return PigmentLinear(pigment[index] / h) * (1 - Mathf.Exp(-h * opacity));
    }

    public double MeasureVolume()
    {
        double total = 0;
        foreach (float h in thickness) total += h * cellArea;
        foreach (float v in blade) total += v;
        foreach (float v in bladeSkin) total += v;
        return total;
    }

    public Vector3 MeasurePigmentMass()
    {
        double r = 0, g = 0, b = 0;
        foreach (Vector3 m in pigment) { r += m.x * cellArea; g += m.y * cellArea; b += m.z * cellArea; }
        for (int x = 0; x < width; x++)
        {
            Vector3 m = bladePigment[x] + skinPigment[x];
            r += m.x; g += m.y; b += m.z;
        }
        return new Vector3((float)r, (float)g, (float)b);
    }

    public float CoveredArea(float minimumThickness = 0.015f)
    {
        int covered = 0;
        foreach (float h in thickness) if (h >= minimumThickness) covered++;
        return covered * cellArea;
    }

    public void ClearPaint()
    {
        recordingState = null;
        if (!IsReady) return;
        Array.Clear(thickness, 0, thickness.Length);
        Array.Clear(pigment, 0, pigment.Length);
        Array.Clear(surfaceThickness, 0, surfaceThickness.Length);
        Array.Clear(surfacePigment, 0, surfacePigment.Length);
        Array.Clear(surfaceOrder, 0, surfaceOrder.Length);
        Array.Clear(bladeOrder, 0, bladeOrder.Length);
        depositOrder = 0;
        hasDepositColor = false;
        Array.Clear(blade, 0, blade.Length);
        Array.Clear(bladeSkin, 0, bladeSkin.Length);
        Array.Clear(bladePigment, 0, bladePigment.Length);
        Array.Clear(skinPigment, 0, skinPigment.Length);
        DepositedVolume = PaperVolume = BladeVolume = 0;
        IsSpreading = false;
        Array.Clear(settlingTicks, 0, settlingTicks.Length);
        activeSettlingTiles = settlingCursor = 0; settleAccumulator = 0;
        IncludeChanged(0, 0, width - 1, height - 1);
        paintMaterial.SetFloat("_BladeVisible", 0);
        dirty = true;
        reservoirDirty = true;
        Flush();
    }

    private static Vector3 Absorption(Color color)
    {
        color = color.linear;
        return new Vector3(-Mathf.Log(Mathf.Max(0.0001f, color.r)), -Mathf.Log(Mathf.Max(0.0001f, color.g)), -Mathf.Log(Mathf.Max(0.0001f, color.b)));
    }

    private static Color PigmentLinear(Vector3 density) =>
        new Color(Mathf.Exp(-density.x), Mathf.Exp(-density.y), Mathf.Exp(-density.z), 1);

    private static Color PigmentColor(Vector3 density) => PigmentLinear(density).gamma;

    public void StyleSpatula(Spatula spatula)
    {
        if (spatula == null) return;
        foreach (var emitter in spatula.GetComponentsInChildren<PaintCore.CwHitNearby>(true)) emitter.enabled = false;
        Transform visual = spatula.transform.Find("Spatula_SpriteRenderer");
        if (visual == null) return;
        var renderer = visual.GetComponent<SpriteRenderer>();
        if (bladeSprite == null)
        {
            var white = Texture2D.whiteTexture;
            bladeSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);
            bladeMaterial = new Material(spatulaShader != null ? spatulaShader : Shader.Find("Painting/Steel Spatula"));
        }
        renderer.sprite = bladeSprite;
        renderer.sharedMaterial = bladeMaterial;
        renderer.color = Color.white;
        visual.localScale = new Vector3(paper.bounds.size.x + 0.18f, 0.7f, 1);
        visual.localPosition = new Vector3(0, 0.4f, -0.2f);
    }

    private void LateUpdate()
    {
        AdvanceSettling(Time.deltaTime, Application.isMobilePlatform ? 6 : 12);
        Flush();
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        if (paper != null && originalMaterial != null) paper.sharedMaterial = originalMaterial;
        DestroyResource(colorTexture); DestroyResource(heightTexture); DestroyResource(reservoirTexture);
        DestroyResource(paintMaterial); DestroyResource(bladeMaterial); DestroyResource(bladeSprite);
        DestroyResource(pigmentReadbackMaterial);
    }

    private static void DestroyResource(UnityEngine.Object resource)
    {
        if (resource == null) return;
        if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
    }
}
