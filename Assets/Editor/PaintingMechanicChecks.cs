using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PaintIn2D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PaintingMechanicChecks
{
    private const string Key = "PaintingMechanicChecks.Running";
    private const string ScenePath = "Assets/Test2D/Scenes/Test 1.unity";
    private static int stage, count, lastFrame, animationFrame;
    private static double deadline;
    private static bool compared, during;
    private static UIManager ui;
    private static ImpastoPaintSurface surface;
    private static Test_RaycastSpawn_CameraPerspective spawner;
    private static DragYPos_Spatula drag;
    private static GridDataComparer comparer;

    static PaintingMechanicChecks()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += StateChanged;
    }

    public static void RunBatch() => Begin();

    [MenuItem("Tools/Painting/Run Mechanic Checks")]
    private static void RunInteractive()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Begin();
    }

    private static void Begin()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.playModeStateChanged -= StateChanged;
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        stage = count = 0;
        compared = during = false;
        animationFrame = 1;
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Impasto test timed out.");
            if (stage == 0)
            {
                surface = ImpastoPaintSurface.Active;
                ui = Object.FindFirstObjectByType<UIManager>();
                spawner = Object.FindFirstObjectByType<Test_RaycastSpawn_CameraPerspective>();
                if (surface == null || !surface.IsReady || ui == null || spawner == null || !spawner.gridProcessor.IsReady) return;
                drag = ui.Spatula.GetComponentInChildren<DragYPos_Spatula>(true);
                comparer = drag.gridDataComparer;
                comparer.OnCompared += _ => compared = true;
                stage = 1;
                return;
            }
            if (stage == 1)
            {
                Check(Shader.Find("Painting/Impasto Paper").isSupported, "Impasto lighting shader supported");
                Check(!ShaderUtil.ShaderHasError(Shader.Find("Painting/Impasto Paper")), "Impasto shader compiles");
                Check(!ShaderUtil.ShaderHasError(Shader.Find("Painting/Steel Spatula")), "Metal blade shader compiles");
                CheckVolume();
                CheckAmountAndStepSize();
                CheckNaturalTransport();
                CheckCoatOrderAndSupply();
                CheckDrawingWork();
                CheckLoadedBlade();
                CaptureVolumeResponse();
                CheckScoring();
                surface.ClearPaint();
                DrawDemonstration();
                Capture("Impasto-Before");
                Capture("Animation/Frame-00", false);
                ui.DoneButton.onClick.Invoke();
                Check(surface.IsSpreading, "Done starts volume-based paint spreading");
                Physics.SyncTransforms();
                var handle = drag.GetComponent<Collider>();
                Vector3 screen = Camera.main.WorldToScreenPoint(handle.bounds.center);
                Check(Physics.Raycast(Camera.main.ScreenPointToRay(screen), out RaycastHit hit, Mathf.Infinity, drag.DragLayer) &&
                    hit.collider.CompareTag("SpatulaDrag"), "Visible steel blade has a working pointer hit area");
                float beforeRelease = drag.Spatula.transform.position.y;
                Set(drag, "isDragging", false);
                Set(drag, "targetY", beforeRelease - 0.5f);
                typeof(DragYPos_Spatula).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(drag, new object[] { 1f / 60f });
                Check(drag.Spatula.transform.position.y < beforeRelease, "Released spatula smoothly catches up with the drag target");
                lastFrame = Time.frameCount;
                stage = 2;
                return;
            }
            if (stage == 2)
            {
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (animationFrame <= 16 && drag.Progress >= animationFrame / 16f)
                    Capture("Animation/Frame-" + (animationFrame++).ToString("D2"), false);
                if (!during && drag.Progress > 0.46f)
                {
                    surface.Flush();
                    Capture("Impasto-During");
                    during = true;
                }
                if (drag.IsComplete) { stage = 3; return; }
                Set(drag, "isDragging", true);
                Set(drag, "targetY", -6f);
                typeof(DragYPos_Spatula).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(drag, new object[] { 1f / 60f });
            }
            else if (stage == 3 && ui.DoneButton.gameObject.activeSelf && !comparer.IsEvaluating)
            {
                Check(compared, "Actual PaintIn2D albedo is used for the result");
                Check(!ui.Spatula.activeSelf, "Spatula completes once");
                Check(surface.PaperVolume > 0.01, "Paint remains on the paper");
                Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002, "Full game preserves deposited paint volume");
                Check(comparer.LastScore >= 0 && comparer.LastScore <= 100, "Final similarity is bounded");
                Capture("Impasto-After");
                Capture("Animation/Frame-17", false);
                WorkshopVisualChecks.CapturePreview("Workshop-Result");
                ui.DoneButton.onClick.Invoke();
                stage = 4;
            }
            else if (stage == 4)
            {
                var restarted = ImpastoPaintSurface.Active;
                if (restarted == null || restarted == surface || !restarted.IsReady) return;
                Check(restarted.DepositedVolume == 0 && restarted.MeasureVolume() == 0, "Retry clears pigment, thickness and blade reservoir");
                Check(Object.FindFirstObjectByType<PaintAmount>().UsedAmount == 0, "Retry resets fractional paint budget");
                var workshop = Object.FindFirstObjectByType<WorkshopVisualController>();
                Check(workshop != null && workshop.IsReady, "Retry restores the workshop controls");
                Check(workshop.amountTube != null && workshop.amountTube.tube.DisplayedRemaining == 1,
                    "Retry restores a full idle paint tube");
                Check(Object.FindObjectsOfType<WorkshopVisualController>().Length == 1,
                    "Retry creates exactly one workshop presentation");
                WorkshopVisualChecks.CapturePreview("Workshop-Retry");
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error.ToString()); }
    }

    private static void CheckVolume()
    {
        surface.ClearPaint();
        surface.Deposit(new Vector3(-1, 5, 0), Color.red, 1, Vector2.zero);
        double one = surface.MeasureVolume();
        Check(Math.Abs(one - surface.UnitVolume) < 0.00001, "One unit deposits its actual volume");
        float h1 = surface.SampleThickness(new Vector3(-1, 5, 0));
        surface.Deposit(new Vector3(-1, 5, 0), Color.red, 2, Vector2.zero);
        Check(Math.Abs(surface.MeasureVolume() / one - 3) < 0.001, "Three units create three times the paint");
        Check(surface.SampleThickness(new Vector3(-1, 5, 0)) > h1 * 2.5f, "Holding builds a raised paint mound");
        surface.Deposit(new Vector3(4.22f, 2, 0), Color.blue, 1.5f, Vector2.zero);
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.00001, "Depositing near paper edges preserves volume");
        surface.AdvanceSettling(0.3f);
        surface.Flush();
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.00002, "Viscous leveling conserves pigment and volume");
        surface.BeginSpread(7.5f);
        surface.SpreadTo(2);
        Check(surface.BladeVolume > 0, "Spatula picks up a real paint reservoir");
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0001, "Pickup and film transfer conserve paint");
        double volume = surface.MeasureVolume();
        surface.SpreadTo(2);
        Check(Math.Abs(surface.MeasureVolume() - volume) < 0.00001, "A stationary spatula cannot consume paint");
    }

    private static float MakeTrail(float units, int steps)
    {
        surface.ClearPaint();
        surface.Deposit(new Vector3(0, 5.2f, 0), new Color(0.08f, 0.35f, 0.95f), units, Vector2.zero);
        surface.BeginSpread(7.5f);
        for (int i = 1; i <= steps; i++) surface.SpreadTo(Mathf.Lerp(7.5f, -5.5f, i / (float)steps));
        surface.FinishSpread();
        return surface.CoveredArea();
    }

    private static void CheckAmountAndStepSize()
    {
        float small = MakeTrail(0.7f, 1);
        float large = MakeTrail(2.1f, 1);
        Debug.Log("VOLUME RESPONSE: small=" + small + " large=" + large);
        Check(large > small * 1.65f, "More paint spreads over a proportionately larger area");
        var samples = new float[60];
        for (int i = 0; i < samples.Length; i++) samples[i] = surface.SampleThickness(new Vector3(0, 5 - i * 0.17f, 0));
        float stepped = MakeTrail(2.1f, 130);
        Check(Mathf.Abs(large - stepped) < 0.0001f, "Fast and slow drags cover the same area");
        float difference = 0;
        for (int i = 0; i < samples.Length; i++)
            difference = Mathf.Max(difference, Mathf.Abs(samples[i] - surface.SampleThickness(new Vector3(0, 5 - i * 0.17f, 0))));
        Check(difference < 0.0001f, "Film thickness is independent of frame/step count");
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002, "Remaining blade paint is retained at the page end");
    }

    private static void CheckScoring()
    {
        var target = new List<GridCell> { new GridCell { color = Color.red }, new GridCell { color = Color.white } };
        var blank = new List<GridCell> { new GridCell { color = Color.white }, new GridCell { color = Color.white } };
        Check(comparer.CompareGridData(target, target) == 100, "An identical pigment image scores 100");
        Check(comparer.CompareGridData(target, blank) == 0, "Blank paper cannot inflate the score");
        Check(spawner.gridProcessor.CapturePaintedImage(), "Color readback works with the custom relief material");
        surface.ClearPaint();
        spawner.gridProcessor.CapturePaintedImage();
        var cells = spawner.gridProcessor.GetGridData();
        int center = cells.Count / 2;
        Color blankColor = cells[center].color;
        Check(Vector3.Distance(new Vector3(blankColor.r, blankColor.g, blankColor.b), Vector3.one) < 0.01f,
            "Canvas warmth and contact shadows do not color the scored blank paper");
        Color pigment = new Color(0.21f, 0.47f, 0.73f);
        surface.Deposit(cells[center].position, pigment, 2, Vector2.zero);
        spawner.gridProcessor.CapturePaintedImage();
        Color result = cells[center].color;
        Check(Vector3.Distance(new Vector3(result.r, result.g, result.b), new Vector3(pigment.r, pigment.g, pigment.b)) < 0.025f,
            "Rendered albedo readback preserves the actual deposited pigment");
    }

    private static void CheckNaturalTransport()
    {
        surface.ClearPaint();
        Vector3 point = new Vector3(0, 5.2f, 0);
        surface.Deposit(point, new Color(0.08f, 0.29f, 0.77f), 3, Vector2.zero);
        float originalHeight = surface.SampleThickness(point);
        Vector3 pigmentMass = surface.MeasurePigmentMass();
        surface.Flush(); surface.Flush();
        Check(Mathf.Abs(surface.SampleThickness(point) - originalHeight) < 0.000001f,
            "Rendering and readback cannot advance paint physics");
        surface.AdvanceSettling(0.3f);
        Check(surface.SampleThickness(point) < originalHeight, "A fresh mound gently relaxes after pouring");
        var settled = new float[49];
        for (int i = 0; i < settled.Length; i++) settled[i] = surface.SampleThickness(point + new Vector3((i % 7 - 3) * 0.09f, (i / 7 - 3) * 0.09f));
        Check(Vector3.Distance(pigmentMass, surface.MeasurePigmentMass()) < 0.0001f, "Settling preserves all pigment channels");
        surface.ClearPaint();
        surface.Deposit(point, new Color(0.08f, 0.29f, 0.77f), 3, Vector2.zero);
        for (int tick = 0; tick < 18; tick++) surface.AdvanceSettling(1f / 60f);
        float difference = 0;
        for (int i = 0; i < settled.Length; i++) difference = Mathf.Max(difference, Mathf.Abs(settled[i] -
            surface.SampleThickness(point + new Vector3((i % 7 - 3) * 0.09f, (i / 7 - 3) * 0.09f))));
        Check(difference < 0.00001f, "Mound relaxation is independent of frame duration");

        float originalFlow = surface.settings.LateralFlow;
        float originalRibbons = surface.settings.ColorRibbons;
        try
        {
            float wide = TransportWidth(originalFlow);
            float narrow = TransportWidth(0);
            Debug.Log("LATERAL RESPONSE: flowing=" + wide + " locked=" + narrow);
            Check(wide > narrow * 1.08f, "Carried paint fans sideways under the loaded blade");
            surface.settings.LateralFlow = originalFlow;
            surface.settings.ColorRibbons = 0;
            Color mixed = ColorCross();
            surface.settings.ColorRibbons = originalRibbons;
            Color ribbon = ColorCross();
            Vector3 towardPink = new Vector3(0.89f - 0.08f, 0.17f - 0.29f, 0.45f - 0.77f);
            float fresh = Vector3.Dot(new Vector3(ribbon.r - mixed.r, ribbon.g - mixed.g, ribbon.b - mixed.b), towardPink);
            Debug.Log("RIBBON RESPONSE: fresh=" + fresh + " mixed=" + mixed + " ribbon=" + ribbon);
            Check(fresh > 0.015f, "Fresh pigment forms a visible ribbon before mixing into the carried pool");
            surface.SpreadTo(-5.5f);
            surface.FinishSpread();
            Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002,
                "Sideways flow and two carried layers conserve volume");
        }
        finally
        {
            surface.settings.LateralFlow = originalFlow;
            surface.settings.ColorRibbons = originalRibbons;
        }
    }

    private static float TransportWidth(float flow)
    {
        surface.settings.LateralFlow = flow;
        surface.ClearPaint();
        surface.Deposit(new Vector3(0, 5.2f, 0), Color.blue, 1.8f, Vector2.zero);
        Vector3 mass = surface.MeasurePigmentMass();
        surface.BeginSpread(7.5f);
        surface.SpreadTo(4.4f);
        Check(Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0001f, "Lateral transport preserves pigment mass");
        float first = float.MaxValue, last = float.MinValue;
        for (int i = 0; i < 300; i++)
        {
            float x = Mathf.Lerp(-2, 2, i / 299f);
            if (surface.SampleThickness(new Vector3(x, 4.6f, 0)) < 0.008f) continue;
            first = Mathf.Min(first, x); last = Mathf.Max(last, x);
        }
        return first <= last ? last - first : 0;
    }

    private static Color ColorCross()
    {
        surface.ClearPaint();
        surface.Deposit(new Vector3(0, 5.2f, 0), new Color(0.08f, 0.29f, 0.77f), 1.8f, Vector2.zero);
        surface.Deposit(new Vector3(0, 4.4f, 0), new Color(0.89f, 0.17f, 0.45f), 1.8f, Vector2.zero);
        surface.BeginSpread(7.5f);
        surface.SpreadTo(4.35f);
        spawner.gridProcessor.CapturePaintedImage();
        float nearest = float.MaxValue;
        Color color = Color.clear;
        foreach (var cell in spawner.gridProcessor.GetGridData())
        {
            float distance = (cell.position - new Vector2(0, 4.4f)).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance; color = cell.color;
        }
        return color;
    }

    private static void CheckLoadedBlade()
    {
        surface.ClearPaint();
        for (int i = 0; i < 16; i++)
            surface.Deposit(new Vector3(-3.2f + i * 0.4f, 5.2f, 0),
                Color.Lerp(new Color(0.08f, 0.29f, 0.77f), new Color(0.89f, 0.17f, 0.45f), i / 15f), 3, Vector2.zero);
        Vector3 mass = surface.MeasurePigmentMass();
        surface.BeginSpread(7.5f);
        var durations = new double[80];
        double simulationTime = 0, textureTime = 0;
        var timer = new System.Diagnostics.Stopwatch();
        for (int i = 0; i < durations.Length; i++)
        {
            timer.Restart();
            surface.SpreadTo(Mathf.Lerp(7.5f, -5.5f, (i + 1f) / durations.Length));
            double simulation = timer.Elapsed.TotalMilliseconds;
            surface.Flush();
            timer.Stop();
            durations[i] = timer.Elapsed.TotalMilliseconds;
            simulationTime += simulation;
            textureTime += durations[i] - simulation;
        }
        double total = 0;
        foreach (double time in durations) total += time;
        Array.Sort(durations);
        Debug.Log("LOADED BLADE CPU (simulation + texture submission): mean_ms=" + total / durations.Length +
            " p95_ms=" + durations[(int)(durations.Length * 0.95f)] + " simulation_ms=" + simulationTime / durations.Length +
            " texture_ms=" + textureTime / durations.Length);
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002,
            "A densely loaded full-width blade conserves volume");
        Check(Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0003f,
            "A densely loaded full-width blade conserves pigment mass");
    }

    private static void CheckCoatOrderAndSupply()
    {
        Color blue = new Color(0.08f, 0.29f, 0.77f);
        Color yellow = new Color(1, 0.76f, 0.04f);
        Vector3 overlap = new Vector3(0, 4.3f, 0);
        surface.ClearPaint();
        surface.Deposit(overlap, blue, 4, Vector2.zero);
        surface.Deposit(overlap, yellow, 0.8f, Vector2.zero);
        Check(ColorDistance(surface.SamplePigment(overlap), yellow) < 0.04f,
            "A sufficiently thick new coat hides the previous color before spreading");
        Vector3 mass = surface.MeasurePigmentMass();
        surface.AdvanceSettling(0.3f);
        Check(ColorDistance(surface.SamplePigment(overlap), yellow) < 0.08f,
            "Settling cannot pull an older color above a newer opaque coat");
        surface.BeginSpread(7.5f); surface.SpreadTo(-5.5f); surface.FinishSpread();
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002 &&
            Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0003f,
            "Buried pigment stays in the material budget when coats cover one another");

        float smallReach = LowerCoat(0.12f, "Layer-Small");
        Color thin = surface.SamplePigment(new Vector3(0, 3.3f, 0));
        float largeReach = LowerCoat(2.4f, "Layer-Large");
        Color thick = surface.SamplePigment(new Vector3(0, 3.3f, 0));
        Debug.Log("COAT RESPONSE: small_reach=" + smallReach + " large_reach=" + largeReach +
            " thin=" + thin + " thick=" + thick);
        Check(largeReach > smallReach + 0.6f, "More of the lower color creates a longer opaque covering trail");
        Check(ColorDistance(thick, yellow) < ColorDistance(thin, yellow) * 0.55f,
            "Lower paint quantity determines whether the old carried color is visible");
        Check(ColorDistance(thick, yellow) < 0.10f,
            "An ample lower coat stays opaque even over a large older paint reservoir");
        Check(spawner.gridProcessor.CapturePaintedImage(), "Layered pigment remains readable by the result system");
        surface.ClearPaint();
        foreach (float x in new[] { -2f, 2f })
            surface.Deposit(new Vector3(x, 5.5f, 0), blue, 6, Vector2.zero);
        surface.Deposit(new Vector3(-2, 4.3f, 0), yellow, 0.12f, Vector2.zero);
        surface.Deposit(new Vector3(2, 4.3f, 0), yellow, 2.4f, Vector2.zero);
        surface.BeginSpread(7.5f); surface.SpreadTo(-5.5f); surface.FinishSpread();
        Capture("Amount-Layers");
    }

    private static float LowerCoat(float units, string preview)
    {
        surface.ClearPaint();
        surface.Deposit(new Vector3(0, 5.5f, 0), new Color(0.08f, 0.29f, 0.77f), 6, Vector2.zero);
        surface.Deposit(new Vector3(0, 4.3f, 0), new Color(1, 0.76f, 0.04f), units, Vector2.zero);
        Vector3 mass = surface.MeasurePigmentMass();
        surface.BeginSpread(7.5f); surface.SpreadTo(-5.5f); surface.FinishSpread();
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0002 &&
            Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0003f,
            "Crossing different paint amounts preserves all material");
        float reach = 0;
        for (int i = 0; i < 150; i++)
        {
            float y = 4.1f - i * 0.04f;
            if (ColorDistance(surface.SamplePigment(new Vector3(0, y, 0)), new Color(1, 0.76f, 0.04f)) < 0.15f)
                reach = Mathf.Max(reach, 4.1f - y);
        }
        Capture(preview);
        return reach;
    }

    private static float ColorDistance(Color a, Color b) =>
        Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b));

    private static void CheckDrawingWork()
    {
        surface.ClearPaint();
        int syncs = surface.AlbedoSyncCount;
        surface.Deposit(new Vector3(-3.3f, 6, 0), Color.blue, 1, Vector2.zero);
        surface.Deposit(new Vector3(3.3f, -4, 0), Color.red, 1, Vector2.zero);
        Vector3 mass = surface.MeasurePigmentMass();
        surface.Flush();
        Check(surface.RecoloredPixelsLastFlush < surface.Width * surface.Height / 10,
            "Separated deposits update small tiles instead of the empty space between them");
        Check(surface.AlbedoSyncCount == syncs, "Drawing does not blit the full page into the scoring texture");
        surface.AdvanceSettling(0.5f, 4);
        Check(surface.SettlingTilesProcessedLastStep <= 4, "A slow frame cannot exceed the requested settling work budget");
        for (int i = 0; i < 60; i++) surface.AdvanceSettling(1f / 60, 24);
        surface.Flush();
        Check(Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) < 0.0001,
            "Deferred local settling preserves paint volume");
        Check(Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0001f,
            "Deferred local settling preserves pigment channels");
        int uploads = surface.UploadCount;
        surface.AdvanceSettling(1f / 60, 24);
        surface.Flush(); surface.Flush();
        Check(surface.UploadCount == uploads, "Resting paint performs no texture uploads");
        Check(spawner.gridProcessor.CapturePaintedImage() && surface.AlbedoSyncCount == syncs + 1,
            "Scoring synchronizes the latest pigment image on demand");
    }

    private static void CaptureVolumeResponse()
    {
        surface.ClearPaint();
        Color blue = new Color(0.08f, 0.29f, 0.77f);
        surface.Deposit(new Vector3(-2.4f, 5.25f, 0), blue, 0.65f, Vector2.zero);
        surface.Deposit(new Vector3(0, 5.25f, 0), blue, 1.3f, Vector2.zero);
        surface.Deposit(new Vector3(2.4f, 5.25f, 0), blue, 2.6f, Vector2.zero);
        surface.BeginSpread(7.5f);
        surface.SpreadTo(-5.5f);
        surface.FinishSpread();
        Capture("Impasto-Volume");
    }

    private static void DrawDemonstration()
    {
        var colors = ui.ColorSelection;
        // Three different amounts make their different mound heights and travel lengths easy to inspect.
        colors.SelectColor(ColorType.Blue);
        spawner.PourAt(new Vector3(-2.4f, 5.25f, 0), 0.65f);
        spawner.PourAt(new Vector3(-1.1f, 5.25f, 0), 1.3f);
        spawner.PourAt(new Vector3(0.3f, 5.25f, 0), 2.6f);
        colors.SelectColor(ColorType.Pink);
        spawner.BeginStroke(new Vector3(-3, 3.6f, 0));
        spawner.DrawBetween(new Vector3(-3, 3.6f, 0), new Vector3(2.8f, 3.3f, 0));
        spawner.EndStroke();
        colors.SelectColor(ColorType.Orange);
        spawner.BeginStroke(new Vector3(-2.8f, 0.8f, 0));
        spawner.DrawBetween(new Vector3(-2.8f, 0.8f, 0), new Vector3(3, 0.6f, 0));
        spawner.EndStroke();
        colors.SelectColor(ColorType.Yellow);
        spawner.PourAt(new Vector3(-1.8f, -0.6f, 0), 0.8f);
        spawner.PourAt(new Vector3(0, -0.6f, 0), 1.2f);
        colors.SelectColor(ColorType.Green);
        spawner.BeginStroke(new Vector3(-2.8f, -2.5f, 0));
        spawner.DrawBetween(new Vector3(-2.8f, -2.5f, 0), new Vector3(2.8f, -2.7f, 0));
        spawner.EndStroke();
        surface.Flush();
        Check(spawner.DepositedUnits > 0 && Object.FindFirstObjectByType<PaintAmount>().UsedAmount > 0,
            "Continuous pouring spends fractional supply");
    }

    private static void Capture(string name, bool verify = true)
    {
        surface.Flush();
        var camera = Camera.main;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        bool canvasEnabled = canvas != null && canvas.enabled;
        if (canvas != null) canvas.enabled = false;
        var position = camera.transform.position;
        bool ortho = camera.orthographic;
        float size = camera.orthographicSize, aspect = camera.aspect;
        var target = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(800, 1200, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var image = new Texture2D(800, 1200, TextureFormat.RGB24, false);
        try
        {
            camera.orthographic = true;
            camera.orthographicSize = 6.95f;
            camera.aspect = 800f / 1200;
            camera.transform.position = new Vector3(0, 1.13f, -10);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 800, 1200), 0, 0);
            image.Apply();
            string path = "Logs/PaintingPreview/" + name + ".png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
            if (verify) Check(true, "Rendered visual preview: " + name);
        }
        finally
        {
            camera.targetTexture = target;
            camera.orthographic = ortho;
            camera.orthographicSize = size;
            camera.aspect = aspect;
            camera.transform.position = position;
            RenderTexture.active = active;
            if (canvas != null) canvas.enabled = canvasEnabled;
            rt.Release();
            Object.Destroy(rt); Object.Destroy(image);
        }
    }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        count++;
        Debug.Log("IMPASTO PASS: " + label);
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= StateChanged;
        SessionState.SetBool(Key, false);
        if (error == null) Debug.Log("IMPASTO CHECKS PASSED: " + count);
        else Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
