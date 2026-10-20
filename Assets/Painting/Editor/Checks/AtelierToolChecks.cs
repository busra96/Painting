using System;
using System.Collections.Generic;
using PaintingGame.Atelier;
using PaintingGame.Gallery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Tools → Painting → Run Atelier Tool Checks. Opens Atelier.unity in Play Mode and verifies that every hand tool
// conserves paint volume and pigment, that brushes add exactly the tube paint they take, that undo refunds tubes,
// and that the tool loops allocate no managed memory. Batch entry: AtelierToolChecks.RunBatch.
[InitializeOnLoad]
public static class AtelierToolChecks
{
    private const string Key = "AtelierToolChecks.Running";
    private const string ScenePath = "Assets/Painting/Scenes/Atelier.unity";
    private static double deadline;
    private static int passed, failed;
    private static readonly List<string> report = new List<string>();

    static AtelierToolChecks()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += StateChanged;
    }

    public static void RunBatch() => Begin();

    [MenuItem("Tools/Painting/Run Atelier Tool Checks")]
    private static void RunInteractive()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Begin();
    }

    private static void Begin()
    {
        SessionState.SetBool(Key, true);
        PaintingSession.Select("");
        EditorSceneManager.OpenScene(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ? ScenePath : GalleryGameAuthoring.ScenePath);
        var gallery=Object.FindFirstObjectByType<GalleryGameController>();
        if(gallery!=null)gallery.storageFolderOverride=System.IO.Path.GetFullPath("Logs/AtelierToolChecks/"+Guid.NewGuid().ToString("N"));
        EditorApplication.playModeStateChanged -= StateChanged;
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        passed = failed = 0;
        report.Clear();
        deadline = EditorApplication.timeSinceStartup + 60;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        var surface = ImpastoPaintSurface.Active;
        var controller = Object.FindFirstObjectByType<PaintToolController>();
        bool ready = surface != null && surface.IsReady && controller != null && controller.Inventory != null;
        if (!ready && EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Tick;
        try
        {
            if (!ready) Check(false, "Atelier surface and controller become ready");
            else Run(surface, controller);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            failed++;
        }
        Finish();
    }

    private static void Run(ImpastoPaintSurface surface, PaintToolController controller)
    {
        AtelierConfig config = controller.config;
        controller.InputLocked = true;
        target = surface;
        float w = surface.CanvasWidth;
        red = config.colors[0].pigment;
        blue = config.colors[Mathf.Min(4, config.colors.Count - 1)].pigment;

        // Knife: any direction, volume and pigment are conserved.
        Seed();
        double v0 = surface.MeasureVolume(); Vector3 m0 = surface.MeasurePigmentMass();
        var knife = surface.BeginKnife(null, 0.3f * w);
        Vector3 from = Offset(-0.1f, 0.3f);
        for (int i = 1; i <= 30; i++)
        {
            Vector3 to = Offset(-0.1f + i * 0.012f, 0.3f - i * 0.02f);
            surface.KnifeSweep(from, to, 0.3f * w, 1, 0.85f, knife);
            from = to;
        }
        surface.EndKnife(knife, 0.3f * w);
        Conserved(surface, v0, m0, "Palette knife (diagonal pull and unchanged lift)");

        // Press pad: holding and dragging.
        Seed();
        v0 = surface.MeasureVolume(); m0 = surface.MeasurePigmentMass();
        for (int i = 0; i < 40; i++) surface.Press(Offset(0, 0.1f), 0.09f * w, 0.6f, 0.18f, 3);
        for (int i = 0; i < 20; i++) surface.Press(Offset(-0.2f + i * 0.02f, -0.1f), 0.09f * w, 0.35f, 0.18f, 3);
        Conserved(surface, v0, m0, "Press pad (hold + drag)");

        // Dry brush only moves paint.
        Seed();
        v0 = surface.MeasureVolume(); m0 = surface.MeasurePigmentMass();
        var load = new ImpastoPaintSurface.BrushLoad();
        var dab = new ImpastoPaintSurface.BrushDab
        {
            radius = 0.07f * w, lengthRatio = 0.35f, squareness = 0.8f, bristles = 22, bristleContrast = 0.6f,
            smudge = 0.45f, release = 0.35f, direction = Vector2.up, order = surface.NewLayerOrder()
        };
        for (int i = 0; i < 60; i++)
        {
            dab.position = Offset(0, -0.25f + i * 0.008f); dab.travel = i * 0.1f;
            surface.Brush(dab, red, 0, load);
        }
        surface.ReleaseBrush(dab.position, load, dab.order);
        Conserved(surface, v0, m0, "Dry blending brush");

        // A loaded brush adds exactly the paint it takes from the tube.
        Seed();
        v0 = surface.MeasureVolume();
        float units = 0;
        for (int i = 0; i < 60; i++)
        {
            dab.position = Offset(0.1f, -0.25f + i * 0.008f);
            surface.Brush(dab, blue, 0.05f, load);
            units += 0.05f;
        }
        surface.ReleaseBrush(dab.position, load, dab.order);
        double added = surface.MeasureVolume() - v0, expected = units * surface.UnitVolume;
        Check(Math.Abs(added - expected) <= expected * 0.002 + 1e-6, "Loaded brush adds exactly its tube paint (" + added.ToString("F5") + " / " + expected.ToString("F5") + ")");

        // Tube economy and undo refund.
        PaintColorDefinition color = config.colors[0];
        controller.Inventory.Refill(color);
        float before = controller.Inventory.Level(color);
        float granted = controller.Inventory.Spend(color, 5);
        Check(Mathf.Approximately(controller.Inventory.Level(color), before - granted) && granted > 0, "Spending paint empties the tube by the same amount");
        controller.Inventory.Refill(color);
        float[] snapshot = controller.Inventory.Snapshot();
        controller.Inventory.Spend(color, 9);
        controller.Inventory.Restore(snapshot);
        Check(Mathf.Approximately(controller.Inventory.Level(color), color.tubeCapacity), "Undo snapshot restores tube levels");
        float empty = controller.Inventory.Spend(color, color.tubeCapacity * 2);
        Check(Mathf.Approximately(empty, color.tubeCapacity) && controller.Inventory.IsEmpty(color), "An empty tube grants nothing more");
        controller.Inventory.Refill(color);
        controller.Inventory.FlushNotifications();

        // Steady-state tool loops allocate nothing.
        Seed();
        knife = surface.BeginKnife(knife, 0.3f * w);
        for (int i = 0; i < 5; i++) { surface.Brush(dab, red, 0.05f, load); surface.Press(Offset(0, 0), 0.09f * w, 0.3f, 0.18f, 1); }
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++)
        {
            dab.position = Offset(0, -0.2f + i * 0.006f);
            surface.Brush(dab, red, 0.05f, load);
            surface.Press(Offset(0.1f, 0), 0.09f * w, 0.3f, 0.18f, 1);
            surface.KnifeSweep(Offset(-0.2f, 0.2f - i * 0.004f), Offset(-0.2f, 0.196f - i * 0.004f), 0.3f * w, 1, 0.85f, knife);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Check(allocated == 0, "Brush, press and knife loops allocate no managed memory (" + allocated + " B)");
        surface.EndKnife(knife, 0.3f * w);
        surface.ClearPaint();
        controller.InputLocked = false;
    }

    private static ImpastoPaintSurface target;
    private static Color red, blue;

    // A point relative to the canvas centre, in canvas widths (visual units).
    private static Vector3 Offset(float x, float y)
    {
        float w = target.CanvasWidth;
        return target.transform.position + new Vector3(x * w, y * w / target.VisualScale.y, 0);
    }

    private static void Seed()
    {
        target.ClearPaint();
        for (int i = -6; i <= 6; i++) target.Deposit(Offset(i * 0.05f, 0.1f), red, 1.2f, Vector2.zero);
        for (int i = -6; i <= 6; i++) target.Deposit(Offset(i * 0.05f, -0.1f), blue, 1.2f, Vector2.zero);
        target.Flush();
    }

    private static void Conserved(ImpastoPaintSurface surface, double v0, Vector3 m0, string name)
    {
        double v1 = surface.MeasureVolume(); Vector3 m1 = surface.MeasurePigmentMass();
        bool volume = Math.Abs(v1 - v0) <= v0 * 0.002 + 1e-6;
        bool pigment = (m1 - m0).magnitude <= m0.magnitude * 0.004f + 1e-5f;
        Check(volume && pigment, name + " conserves volume (" + v0.ToString("F5") + " → " + v1.ToString("F5") + ") and pigment");
    }

    private static void Check(bool ok, string message)
    {
        if (ok) passed++; else failed++;
        report.Add((ok ? "PASS " : "FAIL ") + message);
    }

    private static void Finish()
    {
        SessionState.SetBool(Key, false);
        string summary = "ATELIER TOOL CHECKS " + passed + " passed, " + failed + " failed\n" + string.Join("\n", report);
        if(!Application.isBatchMode)System.IO.File.WriteAllText("Logs/AtelierInteractiveChecks.txt",summary);
        if (failed == 0) Debug.Log(summary); else Debug.LogError(summary);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
    }
}
