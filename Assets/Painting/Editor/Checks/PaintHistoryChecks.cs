using System;
using System.Diagnostics;
using UnityEditor;
using PaintingGame.Gallery;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PaintHistoryChecks
{
    private const string Key = "PaintHistoryChecks.Running";
    private static double deadline;
    private static int checks;
    static PaintHistoryChecks() { if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnState; }
    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Painting/Scenes/Painting.unity");
        EditorApplication.playModeStateChanged -= OnState; EditorApplication.playModeStateChanged += OnState;
        PaintingSession.Select(""); // Checks always use the scene's authored target.
        EditorApplication.isPlaying = true;
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("History check timed out.");
            UIManager ui = Object.FindFirstObjectByType<UIManager>();
            ImpastoPaintSurface surface = ImpastoPaintSurface.Active;
            if (ui == null || surface == null || !surface.IsReady || ui.PaintHistory == null) return;
            EditorApplication.update -= Tick;
            PaintHistory history = ui.PaintHistory;
            var draw = ui.RaycastAndSpawn;
            PaintAmount amount = history.amount;
            Vector3 point = new Vector3(-1, 4, 0);
            history.ClearPainting();
            ui.UndoButton.onClick.Invoke();
            Check(!history.CanUndo && amount.UsedAmount == 0, "Undo on a blank canvas is harmless");
            ui.ColorSelection.SelectColor(ColorType.Red);
            draw.BeginStroke(point); draw.PourAt(point, 1.4f); draw.EndStroke();
            surface.AdvanceSettling(0.1f, 24); surface.Flush();
            float used = amount.UsedAmount, height = surface.SampleThickness(point);
            Color color = surface.SamplePigment(point);
            double volume = surface.MeasureVolume();
            int count = draw.PaintCount;
            Check(history.StrokeCount == 1 && used > 0, "A continuous stroke creates one history entry");
            ui.ColorSelection.SelectColor(ColorType.Blue);
            draw.BeginStroke(point); draw.PourAt(point, 1.4f); draw.EndStroke();
            surface.AdvanceSettling(0.1f, 24); surface.Flush();
            Check(surface.SamplePigment(point).b > 0.7f && history.StrokeCount == 2, "New blue stroke covers red and records separately");
            long savedBlueVersion = ui.ArtworkVersion;
            int savedBlueCount = draw.PaintCount;
            ui.UndoButton.onClick.Invoke();
            Check(ui.ArtworkVersion > savedBlueVersion, "Undo advances the artwork revision used by gallery saving");
            Check(Math.Abs(surface.MeasureVolume() - volume) < 0.00001 &&
                Mathf.Abs(surface.SampleThickness(point) - height) < 0.00001f,
                "Undo restores the actual wet paint thickness and volume");
            Check(Vector4.Distance(surface.SamplePigment(point), color) < 0.00001f,
                "Undo restores the visible coat and its pigment order");
            Check(amount.UsedAmount == used && draw.PaintCount == count && Mathf.Abs(draw.DepositedUnits - used) < 0.0001f,
                "Undo refunds exactly the stroke's paint and restores drawing counters");
            ui.ColorSelection.SelectColor(ColorType.Green);
            draw.BeginStroke(point); draw.PourAt(point, 1.4f); draw.EndStroke();
            Check(draw.PaintCount == savedBlueCount && ui.ArtworkVersion > savedBlueVersion,
                "Replacing an undone stroke changes the save revision even when paint count is identical");
            ui.UndoButton.onClick.Invoke();
            WorkshopVisualChecks.CapturePreview("Tabletop-Undo", 941, 1672);
            ui.UndoButton.onClick.Invoke();
            Check(surface.MeasureVolume() == 0 && amount.UsedAmount == 0 && draw.PaintCount == 0 && !history.CanUndo,
                "Undoing the first stroke returns to a blank full-budget canvas");

            draw.BeginStroke(point); draw.PourAt(point, amount.MaxAmount); draw.EndStroke();
            Check(amount.isFull && draw.isPaintAmountDone, "Exhausting the tube stops drawing");
            ui.UndoButton.onClick.Invoke();
            Check(!amount.isFull && !draw.isPaintAmountDone && amount.RemainingFraction == 1,
                "Undo after exhaustion reopens drawing and refills the tube");
            draw.BeginStroke(point); draw.PourAt(point, 1); draw.EndStroke();
            Check(surface.MeasureVolume() > 0 && amount.UsedAmount > 0, "Drawing works after an exhaustion undo");
            ui.ClearButton.onClick.Invoke();
            Object.FindFirstObjectByType<WorkshopVisualController>().amountTube.Advance(2);
            Check(surface.MeasureVolume() == 0 && amount.RemainingFraction == 1 && draw.PaintCount == 0 && !history.CanUndo,
                "Trash clears paint, history, counters and consumed supply");
            Check(Object.FindFirstObjectByType<WorkshopVisualController>().amountTube.tube.DisplayedRemaining == 1,
                "Clear visibly restores the full tube");

            Vector3 left = new Vector3(-3.1f, 5.8f, 0), right = new Vector3(3.1f, -4, 0);
            draw.BeginStroke(left); draw.PourAt(left, 3); draw.EndStroke();
            surface.AdvanceSettling(0.05f, 6);
            double beforeRemoteStroke = surface.MeasureVolume();
            Vector3 beforeRemoteMass = surface.MeasurePigmentMass();
            float beforeRemoteHeight = surface.SampleThickness(left);
            draw.BeginStroke(right); draw.PourAt(right, 3); draw.EndStroke();
            surface.AdvanceSettling(0.2f, 6);
            history.UndoStroke();
            Check(Math.Abs(surface.MeasureVolume() - beforeRemoteStroke) < 0.00001 &&
                Vector3.Distance(surface.MeasurePigmentMass(), beforeRemoteMass) < 0.00001f &&
                Mathf.Abs(surface.SampleThickness(left) - beforeRemoteHeight) < 0.00001f && surface.SampleThickness(right) == 0,
                "Tile undo restores remote wet changes and keeps the untouched painting exact");
            surface.AdvanceSettling(0.3f);
            history.UndoStroke();
            Check(surface.MeasureVolume() == 0 && amount.UsedAmount == 0,
                "Settling after undo is captured before undoing the preceding stroke");
            history.ClearPainting();

            history.maxStrokes = 2;
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 3; i++) { draw.BeginStroke(point + Vector3.right * i); draw.EndStroke(); }
            timer.Stop();
            Check(history.StrokeCount == 2 && history.SavedBytes <= history.memoryLimitMB * 1024L * 1024,
                "History retains only the configured stroke and memory budget");
            Debug.Log("HISTORY capture and deposit mean_ms=" + timer.Elapsed.TotalMilliseconds / 3);
            ui.UndoButton.onClick.Invoke(); ui.UndoButton.onClick.Invoke();
            Check(!history.CanUndo && surface.MeasureVolume() > 0, "Evicting old history preserves the oldest painting on canvas");
            ui.DoneButton.onClick.Invoke();
            Check(surface.IsSpreading && !ui.UndoButton.interactable && !ui.ClearButton.interactable && !history.CanUndo,
                "Done locks painting edits and releases history before spreading");
            Debug.Log("PAINT HISTORY CHECKS PASSED: " + checks);
            Finish(null);
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }
    private static void Check(bool condition, string text)
    {
        if (!condition) throw new Exception("FAIL: " + text);
        checks++; Debug.Log("HISTORY PASS: " + text);
    }
    private static void Finish(string error)
    {
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnState;
        SessionState.SetBool(Key, false);
        if (error != null) Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
