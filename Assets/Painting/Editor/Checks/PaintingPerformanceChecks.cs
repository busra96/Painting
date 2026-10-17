using System;
using System.Diagnostics;
using PaintIn2D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PaintingPerformanceChecks
{
    private const string Key = "PaintingPerformanceChecks.Running";
    private static double deadline;

    static PaintingPerformanceChecks()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnState;
    }

    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Painting/Scenes/Painting.unity");
        EditorApplication.playModeStateChanged -= OnState;
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Painting/Profile Drawing Performance")]
    private static void RunInteractive()
    {
        if (EditorApplication.isCompiling) return;
        if (EditorSceneManager.GetActiveScene().path != "Assets/Painting/Scenes/Painting.unity")
        {
            Debug.LogWarning("Open the main Test 1 scene before profiling painting.");
            return;
        }
        SessionState.SetBool(Key, true);
        if (EditorApplication.isPlaying)
        {
            deadline = EditorApplication.timeSinceStartup + 150;
            EditorApplication.update += Tick;
        }
        else
        {
            EditorApplication.playModeStateChanged -= OnState;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.isPlaying = true;
        }
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        deadline = EditorApplication.timeSinceStartup + 150;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > deadline) { Finish("Performance check timed out"); return; }
        var surface = ImpastoPaintSurface.Active;
        if (surface == null || !surface.IsReady) return;
        EditorApplication.update -= Tick;
        try
        {
            DrawLongStroke(surface);
            DrawSeparatedStrokes(surface);
            DrawFilledCanvasStrokes(surface);
            RenderPaint(surface);
            Finish(null);
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private static void DrawLongStroke(ImpastoPaintSurface surface)
    {
        surface.ClearPaint();
        var spawner = Object.FindFirstObjectByType<Test_RaycastSpawn_CameraPerspective>();
        const int frames = 120;
        var pour = new double[frames]; var settle = new double[frames]; var upload = new double[frames];
        var timer = new Stopwatch();
        Vector3 previous = new Vector3(0, 6, 0);
        spawner.BeginStroke(previous);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < frames; i++)
        {
            float t = i / (frames - 1f);
            var point = new Vector3(Mathf.Sin(t * Mathf.PI * 4) * 3.3f, 6 - 10 * t, 0);
            timer.Restart();
            spawner.DrawBetween(previous, point);
            pour[i] = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            surface.AdvanceSettling(1f / 60, 24);
            settle[i] = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            surface.Flush();
            upload[i] = timer.Elapsed.TotalMilliseconds;
            previous = point;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        spawner.EndStroke();
        Report("LONG STROKE deposit", pour);
        Report("LONG STROKE settle", settle);
        Report("LONG STROKE texture", upload);
        Debug.Log("PAINT PERF managed_bytes=" + allocated + " volume_error=" + Math.Abs(surface.MeasureVolume() - surface.DepositedVolume));
        if (allocated != 0) throw new Exception("Steady drawing allocated managed memory: " + allocated);
        if (Math.Abs(surface.MeasureVolume() - surface.DepositedVolume) > 0.0002)
            throw new Exception("Long-stroke profiling changed the deposited paint volume");
    }

    private static void DrawSeparatedStrokes(ImpastoPaintSurface surface)
    {
        surface.ClearPaint();
        var times = new double[60];
        var timer = new Stopwatch();
        for (int i = 0; i < times.Length; i++)
        {
            Vector3 point = i % 2 == 0 ? new Vector3(-3.3f, 6, 0) : new Vector3(3.3f, -4, 0);
            timer.Restart();
            surface.Deposit(point, Color.blue, 0.1f, Vector2.zero);
            surface.AdvanceSettling(1f / 60, 24);
            surface.Flush();
            times[i] = timer.Elapsed.TotalMilliseconds;
        }
        Report("SEPARATED DROPS combined", times);
    }

    private static void DrawFilledCanvasStrokes(ImpastoPaintSurface surface)
    {
        var spawner = Object.FindFirstObjectByType<Test_RaycastSpawn_CameraPerspective>();
        PaintHistory history = spawner.history;
        if (history == null) throw new Exception("Filled-canvas profiling requires the real undo history");
        history.ClearPainting();
        for (int y = 0; y <= 25; y++)
        for (int x = 0; x <= 16; x++)
            surface.Deposit(new Vector3(-3.2f + x * 0.4f, 5.8f - y * 0.4f, 0), Color.blue, 0.12f, Vector2.zero);
        surface.AdvanceSettling(1);
        surface.Flush();
        var point = new Vector3(-1, 3, 0);
        for (int i = 0; i < 12; i++) { spawner.BeginStroke(point); spawner.EndStroke(); }
        var samples = new double[24];
        var timer = new Stopwatch();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < samples.Length; i++)
        {
            timer.Restart();
            spawner.BeginStroke(point); spawner.EndStroke();
            samples[i] = timer.Elapsed.TotalMilliseconds;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Report("FILLED CANVAS stroke start and deposit", samples);
        Debug.Log("PAINT PERF FILLED CANVAS managed_bytes=" + allocated + " history_bytes=" + history.SavedBytes);
        if (allocated != 0) throw new Exception("Repeated strokes on an already painted canvas allocated managed memory: " + allocated);
        if (history.SavedBytes > (long)surface.Width * surface.Height * 8)
            throw new Exception("Small strokes copied too much of the existing painting into undo history");
        history.ClearPainting();
    }

    private static void RenderPaint(ImpastoPaintSurface surface)
    {
        surface.ClearPaint();
        for (int x = 0; x < 12; x++) surface.Deposit(new Vector3(-3.1f + x * 0.55f, 5.5f, 0), Color.red, 4, Vector2.zero);
        surface.BeginSpread(7.5f); surface.SpreadTo(-5.5f); surface.FinishSpread();
        var camera = Camera.main;
        var original = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var target = new RenderTexture(720, 1280, 24);
        var readback = new Texture2D(1, 1, TextureFormat.RGB24, false);
        var times = new double[12];
        var timer = new Stopwatch();
        try
        {
            camera.targetTexture = target;
            for (int i = -3; i < times.Length; i++)
            {
                timer.Restart();
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                if (i >= 0) times[i] = timer.Elapsed.TotalMilliseconds;
            }
            Report("CAMERA render and GPU sync 720x1280", times);
        }
        finally
        {
            camera.targetTexture = original; RenderTexture.active = oldActive;
            target.Release(); Object.Destroy(target); Object.Destroy(readback);
        }
    }

    private static void Report(string label, double[] samples)
    {
        double sum = 0;
        foreach (double sample in samples) sum += sample;
        Array.Sort(samples);
        Debug.Log("PAINT PERF " + label + " mean_ms=" + sum / samples.Length + " p95_ms=" +
            samples[Mathf.Min(samples.Length - 1, Mathf.FloorToInt(samples.Length * 0.95f))] + " max_ms=" + samples[samples.Length - 1]);
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
        if (error == null) Debug.Log("PAINT PERFORMANCE CHECKS COMPLETED"); else Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
