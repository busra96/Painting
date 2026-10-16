using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorkshopVisualChecks
{
    private const string Key = "WorkshopVisualChecks.Running";
    private const string ScenePath = "Assets/Test2D/Scenes/Test 1.unity";
    private static double deadline;
    private static int checks;

    static WorkshopVisualChecks()
    {
        if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnStateChanged;
    }

    public static void RunBatch() => Begin();

    [MenuItem("Tools/Painting/Check Workshop Visual")]
    private static void RunInteractive()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Begin();
    }

    private static void Begin()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.playModeStateChanged -= OnStateChanged;
        EditorApplication.playModeStateChanged += OnStateChanged;
        EditorApplication.isPlaying = true;
    }

    private static void OnStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        checks = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Workshop visual check timed out.");
            WorkshopVisualController visual = Object.FindFirstObjectByType<WorkshopVisualController>();
            ImpastoPaintSurface surface = ImpastoPaintSurface.Active;
            if (visual == null || !visual.IsReady || surface == null || !surface.IsReady) return;
            EditorApplication.update -= Tick;

            Check(visual.BackgroundRenderer != null && visual.BackgroundRenderer.sprite != null,
                "Workshop background is loaded");
            Bounds backgroundBounds = visual.BackgroundRenderer.bounds;
            Vector3 backgroundMin = Camera.main.WorldToViewportPoint(backgroundBounds.min);
            Vector3 backgroundMax = Camera.main.WorldToViewportPoint(backgroundBounds.max);
            Check(backgroundMin.x <= 0.01f && backgroundMin.y <= 0.01f &&
                backgroundMax.x >= 0.99f && backgroundMax.y >= 0.99f,
                "Workshop background covers the portrait viewport");

            SpriteRenderer paper = GameObject.Find("Paintable Book  Paint").GetComponent<SpriteRenderer>();
            Vector3 paperMin = Camera.main.WorldToViewportPoint(paper.bounds.min);
            Vector3 paperMax = Camera.main.WorldToViewportPoint(paper.bounds.max);
            Rect expected = WorkshopVisualController.PaperViewport;
            Check(Mathf.Abs(paperMin.x - expected.xMin) < 0.001f && Mathf.Abs(paperMin.y - expected.yMin) < 0.001f &&
                Mathf.Abs(paperMax.x - expected.xMax) < 0.001f && Mathf.Abs(paperMax.y - expected.yMax) < 0.001f,
                "Paintable surface aligns with the framed canvas");
            SpriteRenderer guide = FindRendererIncludingInactive("Paintable Book  BlackLine");
            Check(guide != null && !guide.enabled,
                "Center canvas starts blank like the reference");

            ColorSelection selection = Object.FindFirstObjectByType<ColorSelection>();
            Button[] buttons = { selection.RedButton, selection.YellowButton, selection.GreenButton,
                selection.PinkButton, selection.BlueButton, selection.OrangeButton };
            foreach (Button button in buttons)
                Check(button != null && button.gameObject.activeInHierarchy &&
                    button.targetGraphic.raycastTarget && button.targetGraphic.color.a < 0.02f,
                    button.name + " has a live hit area above the reference paint");

            Check(visual.AmountSegmentCount == 7, "Amount display has seven live segments");
            for (int i = 1; i <= 7; i++)
                Check(GameObject.Find("Amount Segment " + i) != null, "Amount segment " + i + " is present");

            UIManager ui = Object.FindFirstObjectByType<UIManager>();
            TMP_Text doneLabel = ui.DoneButton.GetComponentInChildren<TMP_Text>(true);
            Check(doneLabel != null && doneLabel.text == "DONE", "Bottom button matches the reference label");
            Check(surface.RevealBackdropCanvas, "Blank paper reveals the reference canvas weave");
            Check(Vector2.Distance(paper.bounds.size, new Vector2(8.51f, 12.61f)) < 0.01f,
                "Reference layout preserves simulation dimensions");
            Canvas.ForceUpdateCanvases();
            foreach (Button button in buttons)
                CheckHitArea(button);
            CheckHitArea(ui.DoneButton);
            selection.YellowButton.onClick.Invoke();
            Check(selection.ColorType == ColorType.Yellow && selection.GetOriginalColor() == selection.YellowColor,
                "Palette button selects the pigment used by drawing");
            selection.RedButton.onClick.Invoke();

            CapturePreview("Workshop-Initial");
            surface.ClearPaint();
            Vector3 center = paper.bounds.center;
            surface.Deposit(center, selection.RedColor, 1f, Vector2.zero);
            surface.Flush();
            Check(surface.SampleThickness(center) > 0 && surface.SamplePigment(center).r > 0.8f,
                "Painting remains interactive on the resized canvas");

            CapturePreview("Workshop-Painted");
            CapturePreview("Workshop-Tall", 540, 1200);
            Finish(null);
        }
        catch (Exception exception)
        {
            Finish(exception.ToString());
        }
    }

    public static void CapturePreview(string name, int width = 540, int height = 960)
    {
        Camera camera = Camera.main;
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        RenderMode oldMode = canvas.renderMode;
        Camera oldCanvasCamera = canvas.worldCamera;
        float oldPlane = canvas.planeDistance;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        GameObject uiCameraObject = new GameObject("Workshop Preview UI Camera");
        Camera uiCamera = uiCameraObject.AddComponent<Camera>();
        uiCamera.CopyFrom(camera);
        uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 0.5f;
            camera.targetTexture = target;
            uiCamera.targetTexture = target;
            Object.FindFirstObjectByType<WorkshopVisualController>().RefreshLayout();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            uiCamera.Render();
            RenderTexture.active = target;
            snapshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            snapshot.Apply(false);
            Vector3 min = camera.WorldToViewportPoint(ImpastoPaintSurface.Active.GetComponent<SpriteRenderer>().bounds.min);
            Vector3 max = camera.WorldToViewportPoint(ImpastoPaintSurface.Active.GetComponent<SpriteRenderer>().bounds.max);
            Rect expected = WorkshopVisualController.PaperViewport;
            Check(Mathf.Abs(min.x - expected.xMin) < 0.001f && Mathf.Abs(min.y - expected.yMin) < 0.001f &&
                Mathf.Abs(max.x - expected.xMax) < 0.001f && Mathf.Abs(max.y - expected.yMax) < 0.001f,
                "Canvas remains aligned while rendering " + width + "x" + height);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/WorkshopPreview"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), snapshot.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCanvasCamera;
            canvas.planeDistance = oldPlane;
            Object.FindFirstObjectByType<WorkshopVisualController>().RefreshLayout();
            target.Release();
            Object.DestroyImmediate(uiCameraObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(snapshot);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        checks++;
        Debug.Log("WORKSHOP PASS: " + message);
    }

    private static void CheckHitArea(Button button)
    {
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
            button.name + " receives pointer events at its visible position");
    }

    private static SpriteRenderer FindRendererIncludingInactive(string objectName)
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
            if (renderer.name == objectName) return renderer;
        return null;
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
        if (error == null) Debug.Log("WORKSHOP VISUAL CHECKS PASSED: " + checks);
        else Debug.LogError(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
