using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PaintTubeChecks
{
    private const string Key = "PaintTubeChecks.Running";
    private static double deadline;
    private static int checks;

    static PaintTubeChecks()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnState;
    }

    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Test2D/Scenes/Test 1.unity");
        EditorApplication.playModeStateChanged -= OnState;
        EditorApplication.playModeStateChanged += OnState;
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Paint tube check timed out.");
            WorkshopVisualController visual = Object.FindFirstObjectByType<WorkshopVisualController>();
            if (visual == null || !visual.IsReady || ImpastoPaintSurface.Active == null || !ImpastoPaintSurface.Active.IsReady) return;
            EditorApplication.update -= Tick;
            PaintTubeAmountView view = visual.amountTube;
            PaintAmount amount = view.amount;
            RectTransform rect = view.tube.rectTransform;
            Vector2 authoredPosition = rect.anchoredPosition, authoredSize = rect.sizeDelta;
            int children = amount.GetComponentsInChildren<Transform>(true).Length;
            view.Advance(1f / 60);
            Check(view.remainingLabel.text == "100%" && view.tube.DisplayedRemaining == 1, "Fresh scene starts with a full tube");
            WorkshopVisualChecks.CapturePreview("Tube-Full", 941, 1672);
            Bounds fullShell = ShellBounds(view.tube);

            float granted = amount.Spend(amount.MaxAmount * 0.5f);
            view.Advance(1f / 60);
            Check(amount.UsedAmount == granted, "The paint tube follows the granted budget exactly");
            WorkshopVisualChecks.CapturePreview("Tube-Half-Transition", 941, 1672);
            Settle(view);
            Check(Mathf.Abs(view.tube.DisplayedRemaining - 0.5f) < 0.001f && view.remainingLabel.text == "50%", "Half the budget displays a half-full paint tube");
            Check(SameBounds(fullShell, ShellBounds(view.tube)), "Tube artwork keeps its original shape as paint is spent");
            WorkshopVisualChecks.CapturePreview("Tube-Half", 941, 1672);

            view.selection.SelectColor(ColorType.Blue);
            view.Advance(1f / 60);
            Check(view.tube.Pigment == view.selection.BlueColor,
                "Changing pigment recolors the paint without changing the tube artwork");
            amount.ShowSpreadProgress(0.2f);
            view.Advance(1f / 60);
            Check(Mathf.Abs(view.tube.DisplayedRemaining - 0.5f) < 0.001f && view.remainingLabel.text == "50%",
                "Spatula progress does not refill or drain the tube");

            // Warm the amount graphic and TMP buffers before measuring a continuous stroke.
            for (int i = 0; i < 20; i++)
            {
                amount.Spend(0.001f); view.Advance(1f / 60);
                view.tube.Rebuild(CanvasUpdate.PreRender);
            }
            long animatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                amount.Spend(0.001f); view.Advance(1f / 60);
                view.tube.Rebuild(CanvasUpdate.PreRender);
            }
            long animatedAllocated = GC.GetAllocatedBytesForCurrentThread() - animatedBefore;
            Check(animatedAllocated == 0, "Continuous fill updates and mesh rebuild allocate zero managed bytes");

            amount.Spend(amount.MaxAmount);
            view.Advance(1f / 60);
            Check(amount.RemainingAmount == 0, "The last paint spend exhausts the available budget");
            Settle(view);
            Check(view.tube.DisplayedRemaining == 0 && view.remainingLabel.text == "0%", "Empty budget clears the paint inside the tube");
            Check(SameBounds(fullShell, ShellBounds(view.tube)), "Emptying the tube leaves its artwork unchanged");
            Check(amount.Spend(1) == 0, "Empty tube grants no paint");
            view.Advance(1f / 60);
            Check(rect.anchoredPosition == authoredPosition && rect.sizeDelta == authoredSize &&
                amount.GetComponentsInChildren<Transform>(true).Length == children,
                "Fill animation preserves authored transforms and creates no UI objects");
            WorkshopVisualChecks.CapturePreview("Tube-Empty", 941, 1672);

            // Settled tubes should be equally inexpensive.
            view.Advance(1f / 60);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) view.Advance(1f / 60);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(allocated == 0, "Settled tube update allocates zero managed bytes");
            Debug.Log("PAINT TUBE CHECKS PASSED: " + checks);
            Finish(null);
        }
        catch (Exception exception) { Finish(exception.ToString()); }
    }

    private static void Settle(PaintTubeAmountView view)
    {
        for (int i = 0; i < 120; i++) view.Advance(1f / 60);
    }

    private static Bounds ShellBounds(PaintTubeGraphic graphic)
    {
        Canvas.ForceUpdateCanvases();
        Mesh mesh = graphic.canvasRenderer.GetMesh();
        Vector3[] vertices = mesh.vertices;
        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        // The first 40 quads are the tube image. Later vertices contain the variable pigment.
        for (int i = 1; i < 160; i++) bounds.Encapsulate(vertices[i]);
        return bounds;
    }

    private static bool SameBounds(Bounds a, Bounds b) =>
        (a.min - b.min).sqrMagnitude < 0.000001f && (a.max - b.max).sqrMagnitude < 0.000001f;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        checks++;
        Debug.Log("TUBE PASS: " + message);
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnState;
        SessionState.SetBool(Key, false);
        if (error != null) Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
