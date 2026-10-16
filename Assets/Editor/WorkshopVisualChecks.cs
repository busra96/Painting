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
        WorkshopVisualController authored = Object.FindFirstObjectByType<WorkshopVisualController>();
        if (authored == null || authored.transform.Find("Workshop") == null || authored.backgroundImage == null)
        {
            Finish("The workshop must exist as editable Canvas objects before entering Play mode.");
            return;
        }
        if (authored.selection.RedButton == null || authored.amountTube == null || authored.amountTube.tube == null)
        {
            Finish("Serialized workshop control references are incomplete.");
            return;
        }
        Debug.Log("WORKSHOP AUTHORED CHECK: Layout, artwork and control references exist in Edit mode.");
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

            Check(visual.backgroundImage != null && visual.backgroundImage.sprite != null &&
                visual.backgroundImage.transform.IsChildOf(visual.canvas.transform),
                "Background is an editable Canvas Image");
            Check(GameObject.Find("Palette Panel") != null && GameObject.Find("Panel") == null &&
                GameObject.Find("DoneAndEraserPanel") == null,
                "Legacy UI panels are removed from the scene");

            SpriteRenderer paper = GameObject.Find("Paintable Book  Paint").GetComponent<SpriteRenderer>();
            Vector3 paperMin = Camera.main.WorldToViewportPoint(paper.bounds.min);
            Vector3 paperMax = Camera.main.WorldToViewportPoint(paper.bounds.max);
            Rect expected = visual.CurrentPaperViewport;
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
                    button.targetGraphic.raycastTarget && button.targetGraphic.color.a > 0.9f &&
                    (button.targetGraphic as Image)?.sprite != null,
                    button.name + " has a separately editable paint sprite and hit area");

            Check(visual.amountTube.transform.IsChildOf(visual.canvas.transform), "Paint tube is an editable Canvas child");
            Check(GameObject.Find("Amount Segments") == null, "Old amount segments are removed");

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

            RectTransform statusRect = ui.StatusLabel.rectTransform;
            Vector2 authoredPosition = statusRect.anchoredPosition;
            Vector2 editedPosition = authoredPosition + new Vector2(11, 7);
            statusRect.anchoredPosition = editedPosition;
            Image redImage = selection.RedButton.targetGraphic as Image;
            Color authoredTint = redImage.color;
            Color editedTint = new Color(0.76f, 0.12f, 0.09f);
            redImage.color = editedTint;
            visual.SendMessage("LateUpdate");
            selection.SelectColor(ColorType.Red);
            Check(statusRect.anchoredPosition == editedPosition && redImage.color == editedTint,
                "Inspector position and sprite tint edits survive runtime visual updates");
            statusRect.anchoredPosition = authoredPosition;
            redImage.color = authoredTint;
            RectTransform frameRect = visual.paintRegion.parent as RectTransform;
            Check(frameRect != null && frameRect.name == "Canvas Frame",
                "Moving the editable canvas frame also moves its paint region");
            Vector2 framePosition = frameRect.anchoredPosition;
            Vector2 previousCenter = visual.CurrentPaperViewport.center;
            frameRect.anchoredPosition += new Vector2(12, 8);
            visual.RefreshLayout();
            Check(Vector2.Distance(previousCenter, visual.CurrentPaperViewport.center) > 0.001f,
                "Camera follows an edited canvas position without rebuilding UI");
            frameRect.anchoredPosition = framePosition;
            visual.RefreshLayout();

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
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            Object.FindFirstObjectByType<WorkshopVisualController>().RefreshLayout();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            snapshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            snapshot.Apply(false);
            Vector3 min = camera.WorldToViewportPoint(ImpastoPaintSurface.Active.GetComponent<SpriteRenderer>().bounds.min);
            Vector3 max = camera.WorldToViewportPoint(ImpastoPaintSurface.Active.GetComponent<SpriteRenderer>().bounds.max);
            Rect expected = Object.FindFirstObjectByType<WorkshopVisualController>().CurrentPaperViewport;
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
            Object.FindFirstObjectByType<WorkshopVisualController>().RefreshLayout();
            target.Release();
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
            position = RectTransformUtility.WorldToScreenPoint(button.GetComponentInParent<Canvas>().worldCamera,
                button.transform.position)
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
