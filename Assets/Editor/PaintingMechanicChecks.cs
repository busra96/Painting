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
    private static int stage, count, lastFrame;
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
                CaptureVolumeResponse();
                CheckScoring();
                surface.ClearPaint();
                DrawDemonstration();
                Capture("Impasto-Before");
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
                ui.DoneButton.onClick.Invoke();
                stage = 4;
            }
            else if (stage == 4)
            {
                var restarted = ImpastoPaintSurface.Active;
                if (restarted == null || restarted == surface || !restarted.IsReady) return;
                Check(restarted.DepositedVolume == 0 && restarted.MeasureVolume() == 0, "Retry clears pigment, thickness and blade reservoir");
                Check(Object.FindFirstObjectByType<PaintAmount>().UsedAmount == 0, "Retry resets fractional paint budget");
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

    private static void Capture(string name)
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
            Directory.CreateDirectory("Logs/PaintingPreview");
            File.WriteAllBytes("Logs/PaintingPreview/" + name + ".png", image.EncodeToPNG());
            Check(true, "Rendered visual preview: " + name);
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
