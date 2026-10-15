using System;
using PaintIn2D;
using UnityEngine;

// Pigment mass and paint volume live here. PaintIn2D remains the albedo/render-texture and scoring backend.
[DisallowMultipleComponent]
[DefaultExecutionOrder(30)]
public sealed class ImpastoPaintSurface : MonoBehaviour
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

    private SpriteRenderer paper;
    private CwPaintableSpriteTexture paintable;
    private Material originalMaterial, paintMaterial, bladeMaterial;
    private Texture2D colorTexture, heightTexture, reservoirTexture;
    private Sprite bladeSprite;
    private float[] thickness, blade, bladeProfile;
    private Vector3[] pigment, bladePigment;
    private Color32[] pixels;
    private Color[] reservoirPixels;
    private Bounds localBounds;
    private int width, height, nextRow;
    private float dx, dy, cellArea, previousBladeY;
    private bool dirty;
    private RectInt disturbed, changed, settlingRegion;
    private float settleRemaining, settleAccumulator;
    private int settlePasses = 3;

    public float UnitVolume => settings != null ? settings.PaintVolumePerUnit : 0.12f;
    public float BrushRadius => settings != null ? settings.DepositRadius : 0.24f;
    private float Film => settings != null ? settings.FilmThickness : 0.085f;
    private float Tail => settings != null ? settings.FeatherDistance : 0.4f;

    private void Awake() => Active = this;

    public void Initialize()
    {
        if (IsReady) return;
        paper = GetComponent<SpriteRenderer>();
        paintable = GetComponent<CwPaintableSpriteTexture>();
        if (paper == null || paper.sprite == null || paintable == null || paintable.Current == null) return;
        localBounds = paper.sprite.bounds;
        width = resolution;
        height = Mathf.RoundToInt(width * localBounds.size.y / localBounds.size.x);
        dx = paper.bounds.size.x / width;
        dy = paper.bounds.size.y / height;
        cellArea = dx * dy;
        thickness = new float[width * height];
        pigment = new Vector3[thickness.Length];
        pixels = new Color32[thickness.Length];
        blade = new float[width];
        bladeProfile = new float[width];
        for (int x = 0; x < width; x++)
        {
            float worldX = (x + 0.5f) * dx;
            bladeProfile[x] = 1 + 0.65f * (Mathf.PerlinNoise(worldX * 14.3f, 12.7f) - 0.5f)
                + 0.28f * (Mathf.PerlinNoise(worldX * 39.1f, 4.3f) - 0.5f);
        }
        bladePigment = new Vector3[width];
        reservoirPixels = new Color[width];
        colorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false) { name = "Impasto pigment", wrapMode = TextureWrapMode.Clamp };
        heightTexture = new Texture2D(width, height, TextureFormat.RFloat, false, true) { name = "Impasto thickness", wrapMode = TextureWrapMode.Clamp };
        reservoirTexture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false, true) { name = "Paint on blade", wrapMode = TextureWrapMode.Clamp };
        originalMaterial = paper.sharedMaterial;
        paintMaterial = new Material(paintShader != null ? paintShader : Shader.Find("Painting/Impasto Paper")) { name = "Wet paint and canvas" };
        paintMaterial.SetTexture("_HeightTex", heightTexture);
        paintMaterial.SetTexture("_ReservoirTex", reservoirTexture);
        paintMaterial.SetVector("_PaperSize", new Vector4(paper.bounds.size.x, paper.bounds.size.y, dx, dy));
        paintMaterial.SetFloat("_Relief", settings != null ? settings.ReliefHeight : 0.075f);
        paintMaterial.SetFloat("_Gloss", settings != null ? settings.WetGloss : 0.32f);
        paintMaterial.SetFloat("_ShadowStrength", settings != null ? settings.ContactShadow : 0.32f);
        paper.sharedMaterial = paintMaterial;
        IsReady = true;
        changed = new RectInt(0, 0, width, height);
        dirty = true;
        Flush();
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
        Vector2 center = GridPoint(world);
        float radius = BrushRadius;
        float rx = radius / dx, ry = radius / dy;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - rx * 1.2f));
        int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rx * 1.2f));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - ry * 1.2f));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + ry * 1.2f));
        float weight = 0;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++) weight += Dome(x, y, center, rx, ry);
        if (weight <= 0) return;
        float volume = units * UnitVolume;
        float scale = volume / (weight * cellArea);
        Vector3 density = Absorption(color);
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float amount = Dome(x, y, center, rx, ry) * scale;
            int index = y * width + x;
            thickness[index] += amount;
            pigment[index] += density * amount;
        }
        DepositedVolume += volume;
        PaperVolume += volume;
        IncludeDisturbed(x0, y0, x1, y1);
        if (settleRemaining <= 0) settlingRegion = default;
        settlingRegion = Union(settlingRegion, disturbed);
        settleRemaining = 0.3f;
        IncludeChanged(x0, y0, x1, y1);
        dirty = true;
    }

    private float Dome(int x, int y, Vector2 center, float rx, float ry)
    {
        float nx = (x - center.x) / rx, ny = (y - center.y) / ry;
        float irregularity = 1f + 0.025f * Mathf.Sin(nx * 3f + ny * 4f) + 0.015f * Mathf.Sin(nx * 7f - ny * 5f);
        return Mathf.Pow(Mathf.Max(0, 1 - (nx * nx + ny * ny) * irregularity), 1.5f);
    }

    private void IncludeDisturbed(int x0, int y0, int x1, int y1)
    {
        if (disturbed.width == 0) disturbed = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        else disturbed = new RectInt(Mathf.Min(x0, disturbed.xMin), Mathf.Min(y0, disturbed.yMin),
            Mathf.Max(x1 + 1, disturbed.xMax) - Mathf.Min(x0, disturbed.xMin),
            Mathf.Max(y1 + 1, disturbed.yMax) - Mathf.Min(y0, disturbed.yMin));
    }

    private void IncludeChanged(int x0, int y0, int x1, int y1)
    {
        if (changed.width == 0) changed = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        else changed = new RectInt(Mathf.Min(x0, changed.xMin), Mathf.Min(y0, changed.yMin),
            Mathf.Max(x1 + 1, changed.xMax) - Mathf.Min(x0, changed.xMin),
            Mathf.Max(y1 + 1, changed.yMax) - Mathf.Min(y0, changed.yMin));
    }

    private static RectInt Union(RectInt a, RectInt b)
    {
        if (a.width == 0) return b;
        if (b.width == 0) return a;
        int x = Mathf.Min(a.xMin, b.xMin), y = Mathf.Min(a.yMin, b.yMin);
        return new RectInt(x, y, Mathf.Max(a.xMax, b.xMax) - x, Mathf.Max(a.yMax, b.yMax) - y);
    }

    // A yield threshold keeps thick acrylic in rounded heaps instead of diffusing it like water.
    private void SettleHeaps()
    {
        if (disturbed.width == 0) return;
        int x0 = Mathf.Max(0, disturbed.xMin - 3), x1 = Mathf.Min(width - 1, disturbed.xMax + 3);
        int y0 = Mathf.Max(0, disturbed.yMin - 3), y1 = Mathf.Min(height - 1, disturbed.yMax + 3);
        IncludeChanged(x0, y0, x1, y1);
        for (int pass = 0; pass < settlePasses; pass++)
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            int index = y * width + x;
            LevelPair(index, index + 1);
            LevelPair(index, index + width);
        }
        settlingRegion = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        settlePasses = 3;
        disturbed = default;
    }

    private void LevelPair(int a, int b)
    {
        float difference = thickness[a] - thickness[b];
        if (Mathf.Abs(difference) < 0.18f || Mathf.Max(thickness[a], thickness[b]) < 0.9f) return;
        int from = difference > 0 ? a : b, to = difference > 0 ? b : a;
        float amount = Mathf.Min(thickness[from] * 0.12f, (Mathf.Abs(difference) - 0.18f) * 0.1f);
        Vector3 mass = pigment[from] * (amount / Mathf.Max(thickness[from], 0.000001f));
        thickness[from] -= amount; thickness[to] += amount;
        pigment[from] -= mass; pigment[to] += mass;
    }

    public void BeginSpread(float worldY)
    {
        if (!IsReady) return;
        Flush();
        settleRemaining = settleAccumulator = 0;
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
        // Process each crossed row exactly once. Fast/slow drags transfer the same material volume.
        while (nextRow >= lastRow && nextRow >= 0) SweepRow(nextRow--);
        previousBladeY = worldY;
        UpdateBladeUniform(worldY);
        dirty = true;
    }

    private void SweepRow(int y)
    {
        IncludeChanged(0, y, width - 1, y);
        const float pickup = 0.96f;
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            float collected = thickness[i] * pickup;
            Vector3 mass = pigment[i] * pickup;
            thickness[i] -= collected; pigment[i] -= mass;
            float volume = collected * cellArea;
            blade[x] += volume; bladePigment[x] += mass * cellArea;
            PaperVolume -= volume; BladeVolume += volume;
        }
        // Conservative lateral kneading softens column seams and produces local pigment marbling.
        for (int x = 0; x < width - 1; x++)
        {
            int from = blade[x] > blade[x + 1] ? x : x + 1, to = from == x ? x + 1 : x;
            float amount = Mathf.Abs(blade[x] - blade[x + 1]) * 0.055f;
            Vector3 mass = bladePigment[from] * (amount / Mathf.Max(blade[from], 0.00000001f));
            blade[from] -= amount; blade[to] += amount;
            bladePigment[from] -= mass; bladePigment[to] += mass;
        }
        float tailRows = Mathf.Max(2, Tail / dy);
        float worldY = (y + 0.5f) * dy;
        for (int x = 0; x < width; x++)
        {
            if (blade[x] <= 0.00000001f) continue;
            float worldX = (x + 0.5f) * dx;
            // Uneven micro-grooves avoid a repeating comb pattern along the wet edge.
            float comb = bladeProfile[x] + 0.025f * Mathf.Sin(worldX * 29.5f + worldY * 1.9f);
            float target = Film * comb * cellArea;
            // A square-root taper runs out in a finite distance, leaving thin streaks instead of an endless airbrush trail.
            float fullness = Mathf.Sqrt(Mathf.Clamp01(blade[x] / (target * tailRows)));
            float volume = Mathf.Min(blade[x], target * fullness);
            Vector3 mass = bladePigment[x] * (volume / blade[x]);
            blade[x] -= volume; bladePigment[x] -= mass;
            int i = y * width + x;
            thickness[i] += volume / cellArea;
            pigment[i] += mass / cellArea;
            PaperVolume += volume; BladeVolume -= volume;
        }
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
        if (!IsSpreading) SettleHeaps();
        // Recompute expensive pigment absorption only where the nozzle or blade changed the surface.
        for (int y = changed.yMin; y < changed.yMax; y++)
        for (int x = changed.xMin; x < changed.xMax; x++)
        {
            int i = y * width + x;
            float h = thickness[i];
            if (h < 0.000001f) { pixels[i] = new Color32(255, 255, 255, 255); continue; }
            Vector3 density = pigment[i] / h;
            Color color = PigmentColor(density);
            float coverage = 1 - Mathf.Exp(-h * 65f);
            pixels[i] = Color.Lerp(Color.white, color, coverage);
        }
        colorTexture.SetPixels32(pixels);
        colorTexture.Apply(false);
        heightTexture.SetPixelData(thickness, 0);
        heightTexture.Apply(false);
        for (int x = 0; x < width; x++)
        {
            if (blade[x] < 0.00000001f) { reservoirPixels[x] = Color.clear; continue; }
            Color color = PigmentColor(bladePigment[x] / blade[x]).linear;
            color.a = Mathf.Min(4, blade[x] / (dx * 0.12f));
            reservoirPixels[x] = color;
        }
        reservoirTexture.SetPixels(reservoirPixels);
        reservoirTexture.Apply(false);
        // Keep a clean albedo in the package's paintable texture. Shading never contaminates the score.
        RenderTexture previous = RenderTexture.active;
        try { Graphics.Blit(colorTexture, paintable.Current); }
        finally { RenderTexture.active = previous; }
        dirty = false;
        changed = default;
    }

    public float SampleThickness(Vector3 world)
    {
        if (!Contains(world)) return 0;
        Vector2 p = GridPoint(world);
        return thickness[Mathf.Clamp(Mathf.RoundToInt(p.y), 0, height - 1) * width + Mathf.Clamp(Mathf.RoundToInt(p.x), 0, width - 1)];
    }

    public double MeasureVolume()
    {
        double total = 0;
        foreach (float h in thickness) total += h * cellArea;
        foreach (float v in blade) total += v;
        return total;
    }

    public float CoveredArea(float minimumThickness = 0.015f)
    {
        int covered = 0;
        foreach (float h in thickness) if (h >= minimumThickness) covered++;
        return covered * cellArea;
    }

    public void ClearPaint()
    {
        if (!IsReady) return;
        Array.Clear(thickness, 0, thickness.Length);
        Array.Clear(pigment, 0, pigment.Length);
        Array.Clear(blade, 0, blade.Length);
        Array.Clear(bladePigment, 0, bladePigment.Length);
        DepositedVolume = PaperVolume = BladeVolume = 0;
        IsSpreading = false;
        disturbed = default;
        settlingRegion = default;
        settleRemaining = settleAccumulator = 0;
        changed = new RectInt(0, 0, width, height);
        paintMaterial.SetFloat("_BladeVisible", 0);
        dirty = true;
        Flush();
    }

    private static Vector3 Absorption(Color color)
    {
        color = color.linear;
        return new Vector3(-Mathf.Log(Mathf.Max(0.0001f, color.r)), -Mathf.Log(Mathf.Max(0.0001f, color.g)), -Mathf.Log(Mathf.Max(0.0001f, color.b)));
    }

    private static Color PigmentColor(Vector3 density) =>
        new Color(Mathf.Exp(-density.x), Mathf.Exp(-density.y), Mathf.Exp(-density.z), 1).gamma;

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
        if (IsReady && !IsSpreading && settleRemaining > 0)
        {
            float step = Mathf.Min(Time.deltaTime, settleRemaining);
            settleRemaining -= step;
            settleAccumulator += step;
            int ticks = Mathf.Min(4, Mathf.FloorToInt(settleAccumulator * 60));
            if (ticks > 0)
            {
                settleAccumulator -= ticks / 60f;
                disturbed = Union(disturbed, settlingRegion);
                settlePasses = ticks * 3;
                dirty = true;
            }
        }
        Flush();
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        if (paper != null && originalMaterial != null) paper.sharedMaterial = originalMaterial;
        DestroyResource(colorTexture); DestroyResource(heightTexture); DestroyResource(reservoirTexture);
        DestroyResource(paintMaterial); DestroyResource(bladeMaterial); DestroyResource(bladeSprite);
    }

    private static void DestroyResource(UnityEngine.Object resource)
    {
        if (resource == null) return;
        if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource);
    }
}
