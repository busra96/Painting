using System;
using System.IO;
using PaintCore;
using PaintingGame.Atelier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class AtelierShopChecks
{
    private const string Key = "Painting.AtelierShopChecks";
    private const string CountKey = Key + ".Count";
    private const string FolderKey = Key + ".Folder";
    private static double deadline;
    private static AtelierShopController shop;
    private static string testFolder;

    static AtelierShopChecks()
    { if (SessionState.GetBool(Key, false)) EditorApplication.playModeStateChanged += OnState; }

    public static void RunBatch()
    {
        try
        {
            SessionState.SetInt(CountKey, 0);
            testFolder = Path.GetFullPath("Logs/AtelierChecks/" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(FolderKey, testFolder);
            Directory.CreateDirectory(testFolder);
            ValidateEconomy();
            var scene = EditorSceneManager.OpenScene(AtelierShopSceneAuthoring.ScenePath);
            Check(scene.IsValid() && scene.isLoaded, "Separate atelier shop scene opens");
            int missing = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            Check(missing == 0, "Shop scene has no missing scripts");
            Check(!AssetDatabase.IsValidFolder("Assets/Painting/Scripts/Painting3D") &&
                  AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Painting/Scenes/Tests/OilPaint3D.unity") == null,
                "Project-owned 3D model painting prototype was removed");
            shop = Object.FindFirstObjectByType<AtelierShopController>();
            Check(shop != null && shop.painter != null, "Shop and painter are authored in the scene");
            Check(shop.cards.Length == 12 && shop.buttons.Length >= 25, "Editable Canvas contains shop slots and game controls");
            var serialized = new SerializedObject(shop);
            serialized.FindProperty("storageFolderOverride").stringValue = Path.Combine(testFolder, "Runtime");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetBool(Key, true);
            EditorApplication.playModeStateChanged -= OnState;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.isPlaying = true;
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void ValidateEconomy()
    {
        var store = new AtelierShopStore(Path.Combine(testFolder, "Economy"));
        Check(store.Data.coins == 90 && store.Data.shopLevel == 1, "A new shop starts with seed money and four wall slots");
        Check(store.ToolUnlocked(AtelierTool.Press) && !store.ToolUnlocked(AtelierTool.Knife), "Starter tools and locked upgrades differ");
        Check(!store.Expand() && store.Data.coins == 90, "An unaffordable purchase cannot spend money");
        Check(!store.BuyPalette() && store.Data.coins == 90,
            "Unlock purchases cannot strand an empty shop without money for its next canvas");
        var sample = new Texture2D(8, 8, TextureFormat.RGB24, false);
        byte[] png = sample.EncodeToPNG(); Object.DestroyImmediate(sample);
        Check(!store.ListArtwork(png, 0.4f, 1, "Empty"), "Listing requires a purchased canvas");
        Check(store.BeginCanvas() && store.Data.coins == 82, "Buying a blank canvas charges its actual cost");
        Check(!store.BeginCanvas() && store.Data.coins == 82, "An existing draft cannot be silently overwritten or charged twice");
        Check(!store.ListArtwork(png, 0f, 1, "Blank"), "An unpainted canvas cannot be sold");
        File.WriteAllBytes(store.DraftPath, new byte[] { 1 });
        File.WriteAllBytes(store.DraftPath + ".bak", new byte[] { 1 });
        Check(store.ListArtwork(png, 0.4f, 3, "First painting") && store.Data.activeCanvasTier == -1,
            "Exhibiting stores the art and releases the workbench");
        Check(!File.Exists(store.DraftPath) && !File.Exists(store.DraftPath + ".bak"),
            "A completed artwork cannot reappear on a new canvas through a stale draft backup");
        int price = store.Data.artworks[0].price, before = store.Data.coins;
        for (int i = 0; i < 20; i++) store.TickSales(1f);
        Check(store.Data.soldCount == 1 && store.Data.coins == before + price && store.Data.artworks.Count == 0,
            "A customer buys exactly one listed painting and pays its displayed price");
        store.TickSales(1f);
        Check(store.Data.coins == before + price, "An empty wall cannot pay the same sale twice");
        Check(store.BuyTool(AtelierTool.FlatBrush), "Sales fund a new brush");
        int coinsAfterTool = store.Data.coins;
        Check(!store.BuyTool(AtelierTool.FlatBrush) && store.Data.coins == coinsAfterTool, "Owned tools cannot be purchased twice");
        for (int painting = 0; painting < 5; painting++)
        {
            Check(store.BeginCanvas() && store.ListArtwork(png, 0.55f, 7, "Practice " + painting), "Create and list another paid canvas");
            for (int i = 0; i < 20; i++) store.TickSales(1f);
        }
        Check(store.BuyPalette() && store.Data.expandedPalette, "Special pigments can be unlocked with earned money");
        Check(store.SelectCanvas(1) && store.Data.selectedCanvasTier == 1, "Better canvas stock can be unlocked and selected");
        Check(AtelierRules.Quote(0.4f, 3, 1) > AtelierRules.Quote(0.4f, 3, 0), "Higher-quality canvases increase the same artwork's value");
        store.SelectCanvas(0);
        for (int i = 0; i < 4; i++) Check(store.BeginCanvas() && store.ListArtwork(png, 0.1f, 1, "Wall " + i), "Fill a gallery slot");
        store.BeginCanvas(); int fullWallet = store.Data.coins;
        Check(!store.ListArtwork(png, 0.1f, 1, "Overflow") && store.Data.activeCanvasTier == 0 && store.Data.coins == fullWallet,
            "A full gallery preserves the unsold draft and wallet");
        Check(store.Expand() && AtelierRules.Capacity(store.Data.shopLevel) == 6, "Shop expansion opens two actual exhibition slots");
        Check(store.ListArtwork(png, 0.1f, 1, "New wall"), "The new capacity accepts the waiting artwork");
        var reloaded = new AtelierShopStore(store.Folder);
        Check(reloaded.Data.coins == store.Data.coins && reloaded.Data.shopLevel == 2 && reloaded.Data.artworks.Count == 5 &&
              reloaded.ToolUnlocked(AtelierTool.FlatBrush) && reloaded.Data.expandedPalette,
            "Reload preserves wallet, inventory, unlocks and displayed paintings");
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        testFolder = SessionState.GetString(FolderKey, "");
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Atelier scene initialization timed out.");
            shop = Object.FindFirstObjectByType<AtelierShopController>();
            if (shop == null || !shop.Ready) return;
            EditorApplication.update -= Tick;
            Check(shop.Store.Folder == Path.Combine(testFolder, "Runtime"), "Runtime checks use an isolated save folder");
            var surface = shop.painter.surface;
            Check(surface.IsReady && surface.Width == 384, "Existing impasto backend initializes at a bounded shop resolution");
            ValidatePointerInput();
            Check(shop.Store.Data.activeCanvasTier == 0, "Runtime workbench receives a blank purchased canvas");
            Vector3 center = surface.transform.position;
            surface.ClearPaint();
            foreach (ImpastoPaintSurface.ArtistBrush brush in Enum.GetValues(typeof(ImpastoPaintSurface.ArtistBrush)))
            {
                double volume = surface.MeasureVolume();
                Check(surface.ArtistDab(center, Color.red, 2f, 0.45f, 0, brush), brush + " brush deposits paint");
                Check(Math.Abs(surface.MeasureVolume() - volume - 2f * surface.UnitVolume) < 0.00001,
                    brush + " brush conserves the requested paint quantity");
            }
            double pressedVolume = surface.MeasureVolume(); Vector3 mass = surface.MeasurePigmentMass();
            for (int i = 0; i < 8; i++) surface.ArtistPress(center, 0.8f, 0.8f, Vector2.right);
            Check(Math.Abs(surface.MeasureVolume() - pressedVolume) < 0.00001 &&
                Vector3.Distance(mass, surface.MeasurePigmentMass()) < 0.0001f,
                "Local pressing and palette knife conserve paint volume and pigment mass");
            surface.ClearPaint();
            Check(!surface.ArtistPress(center, 0.7f, 1f, Vector2.right) && surface.MeasureVolume() == 0,
                "Pressing on blank canvas cannot generate free paint");
            Vector3 edge = shop.paper.bounds.max - new Vector3(0.001f, 0.001f, 0);
            surface.ArtistDab(edge, Color.blue, 3f, 0.6f, 0, ImpastoPaintSurface.ArtistBrush.Flat);
            Check(Math.Abs(surface.MeasureVolume() - 3f * surface.UnitVolume) < 0.00001,
                "Clipped artistic brushes preserve paint quantity at canvas corners");
            surface.ClearPaint();
            surface.ArtistDab(center, Color.red, 1f, 0.34f, 0, ImpastoPaintSurface.ArtistBrush.Flat);
            surface.Flush();
            Check(surface.RecoloredPixelsLastFlush < surface.Width * surface.Height / 3,
                "Artistic brushes update local tiles instead of recoloring the entire canvas");
            // Warm the largest stamp buffer before measuring the steady-state CPU drawing path.
            surface.ArtistDab(center, Color.red, 0.1f, 0.7f, 0, ImpastoPaintSurface.ArtistBrush.Round);
            long allocation = GC.GetAllocatedBytesForCurrentThread();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            // Stopwatch itself is created before the allocation baseline.
            allocation = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 60; i++)
                surface.ArtistDab(center, Color.red, 0.1f, 0.5f, 0, ImpastoPaintSurface.ArtistBrush.Flat);
            timer.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocation;
            Check(allocated == 0, "Steady-state artistic brush stamping allocates no managed memory");
            Debug.Log("ATELIER PERF brush_mean_ms=" + timer.Elapsed.TotalMilliseconds / 60 + " managed_bytes=" + allocated);
            surface.ClearPaint();
            var painter = shop.painter;
            painter.SetTool(AtelierTool.RoundBrush);
            painter.BeginStroke(center + new Vector3(-2, 1, 0));
            for (int i = 0; i < 30; i++) painter.ContinueStroke(center + new Vector3(-2 + i * 0.13f, 1 + Mathf.Sin(i * 0.2f) * 0.5f, 0), 0.016f);
            painter.EndStroke();
            double strokedVolume = surface.MeasureVolume();
            Check(strokedVolume > 0 && painter.CanUndo && painter.UsedPaint > 0, "A free-form stroke paints and records one undo operation");
            painter.Undo();
            Check(surface.MeasureVolume() < 0.000001 && painter.UsedPaint == 0, "Undo removes the stroke and refunds its paint allowance");
            painter.SetTool(AtelierTool.Flower); painter.BeginStroke(center);
            float stampPaint = painter.UsedPaint;
            for (int i = 0; i < 30; i++) painter.ContinueStroke(center, 0.016f);
            painter.EndStroke();
            Check(painter.UsedPaint == stampPaint, "Holding a pattern stamp still cannot repeatedly consume paint");
            painter.ClearCanvas();
            surface.ArtistDab(center + Vector3.up, Color.red, 8f, 0.45f, 0, ImpastoPaintSurface.ArtistBrush.Round);
            double wideVolume = surface.MeasureVolume();
            Vector3 wideMass = surface.MeasurePigmentMass();
            var beforeWide = surface.CapturePaintState();
            Check(surface.ArtistWideBlade(center.y + 2f, center.y - 2f, 0.8f),
                "A broad spatula can still spread paint downward in the new shop mode");
            Check(Math.Abs(surface.MeasureVolume() - wideVolume) < 0.00001 &&
                  Vector3.Distance(surface.MeasurePigmentMass(), wideMass) < 0.0001f,
                "The broad spatula leaves a paint film while conserving material");
            float fastArea = surface.CoveredArea();
            surface.RestorePaintState(beforeWide); surface.ReleasePaintState(beforeWide);
            for (int i = 0; i < 40; i++) surface.ArtistWideBlade(center.y + 2f - i * 0.1f, center.y + 2f - (i + 1) * 0.1f, 0.8f);
            Check(Mathf.Abs(surface.CoveredArea() - fastArea) < 0.001f,
                "Fast and slow broad-spatula drags cover the same area");
            painter.ClearCanvas();
            DrawPreview(painter, center);
            shop.SaveDraft(); double savedVolume = surface.MeasureVolume();
            float savedPaint = painter.UsedPaint; int savedTools = painter.UsedTools;
            painter.ClearCanvas();
            Check(surface.ReadArtistDraft(shop.Store.DraftPath) && Math.Abs(surface.MeasureVolume() - savedVolume) < 0.00001,
                "Compressed draft reload restores physical paint instead of a flattened screenshot");
            painter.RestoreBudget(savedPaint, savedTools);
            shop.SetTab(0);
            Capture("Atelier-Workshop");
            Check(shop.Exhibit() && shop.Store.Data.artworks.Count == 1 && shop.Store.Data.activeCanvasTier == -1,
                "Runtime painting is captured and appears in the gallery");
            string artworkPath = shop.Store.ImagePath(shop.Store.Data.artworks[0].id);
            Check(File.Exists(artworkPath) && new FileInfo(artworkPath).Length > 10000,
                "The listed artwork has its own saved image");
            var storedImage = new Texture2D(2, 2, TextureFormat.RGB24, false);
            storedImage.LoadImage(File.ReadAllBytes(artworkPath));
            Check(Mathf.Abs(storedImage.width / (float)storedImage.height - shop.paper.bounds.size.x / shop.paper.bounds.size.y) < 0.01f,
                "Exhibited artwork preserves the whole canvas aspect independently of the screen");
            Object.DestroyImmediate(storedImage);
            File.Copy(artworkPath, "Logs/AtelierPreview/Atelier-Artwork.png", true);
            Capture("Atelier-Shop");
            int beforeSale = shop.Store.Data.coins, value = shop.Store.Data.artworks[0].price;
            for (int i = 0; i < 20; i++) shop.Store.TickSales(1f);
            Check(shop.Store.Data.coins == beforeSale + value && shop.Store.Data.soldCount == 1,
                "The complete paint-to-sale loop pays the player");
            shop.SetTab(2); Capture("Atelier-Supplies");
            Check(EditorBuildSettings.scenes.Length >= 2 && EditorBuildSettings.scenes[0].path.EndsWith("/Painting.unity"),
                "Target-matching remains the original build entry and the shop is a separate scene");
            Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }

    private static void ValidatePointerInput()
    {
        var surface = shop.painter.surface;
        var input = shop.layout.paperRegion.GetComponent<AtelierPaintInput>();
        Check(input != null && input.owner == shop, "Visible paint area has its authored pointer handler");
        shop.layout.RefreshLayout(); Canvas.ForceUpdateCanvases();
        Check(shop.Store.Data.activeCanvasTier == -1 && !shop.CanPaint,
            "A first-time shop starts before its visible blank canvas is purchased");
        Vector2 start = shop.painter.paintCamera.WorldToScreenPoint(shop.paper.bounds.center);
        PointerEventData mouse = PointerAt(start, -1);
        GameObject hit = mouse.pointerCurrentRaycast.gameObject;
        Check(hit == input.gameObject, "The real UI raycast reaches the transparent paint area");
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.pointerDownHandler);
        Check(shop.CanPaint && shop.Store.Data.coins == 82 && surface.MeasureVolume() > 0,
            "The first mouse-down buys one canvas and immediately deposits visible paint");
        double initialVolume = surface.MeasureVolume();
        mouse.position = shop.painter.paintCamera.WorldToScreenPoint(shop.paper.bounds.center + Vector3.right);
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.beginDragHandler);
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.dragHandler);
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hit, mouse, ExecuteEvents.endDragHandler);
        Check(surface.MeasureVolume() > initialVolume && shop.Store.Data.coins == 82 && shop.painter.CanUndo,
            "A short mouse drag paints through the EventSystem without buying another canvas");
        shop.painter.Undo();
        Check(surface.MeasureVolume() < 0.000001 && shop.painter.UsedPaint == 0,
            "One undo restores the entire pointer-driven mouse stroke");

        PointerEventData touch = PointerAt(start, 21);
        ExecuteEvents.ExecuteHierarchy(input.gameObject, touch, ExecuteEvents.pointerDownHandler);
        touch.position = shop.painter.paintCamera.WorldToScreenPoint(shop.paper.bounds.center + Vector3.up);
        ExecuteEvents.ExecuteHierarchy(input.gameObject, touch, ExecuteEvents.dragHandler);
        ExecuteEvents.ExecuteHierarchy(input.gameObject, touch, ExecuteEvents.pointerUpHandler);
        Check(surface.MeasureVolume() > 0 && shop.painter.CanUndo,
            "Touch-pointer down, drag and up paint through the same UI route");
        shop.painter.Undo();

        foreach (Slider slider in shop.layout.canvas.GetComponentsInChildren<Slider>(true))
        {
            RectTransform rect = (RectTransform)slider.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * 0.25f, 0)));
            PointerEventData data = PointerAt(point, -1);
            Check(data.pointerCurrentRaycast.gameObject != null &&
                  data.pointerCurrentRaycast.gameObject.GetComponentInParent<Slider>() == slider,
                slider.name + " receives UI hits instead of passing the pointer to paint");
            float before = slider.value;
            ExecuteEvents.ExecuteHierarchy(data.pointerCurrentRaycast.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(data.pointerCurrentRaycast.gameObject, data, ExecuteEvents.pointerUpHandler);
            Check(Mathf.Abs(slider.value - before) > 0.01f, slider.name + " responds to pointer input");
            slider.value = before;
        }
        Check(surface.MeasureVolume() < 0.000001, "Adjusting painting controls cannot leave accidental paint on the canvas");
    }

    private static PointerEventData PointerAt(Vector2 position, int id)
    {
        var data = new PointerEventData(EventSystem.current)
        { pointerId = id, button = PointerEventData.InputButton.Left, position = position };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        if (results.Count > 0) data.pointerCurrentRaycast = data.pointerPressRaycast = results[0];
        return data;
    }

    private static void DrawPreview(AtelierCanvasPainter painter, Vector3 center)
    {
        Color[] colors = { AtelierRules.Colors[0], AtelierRules.Colors[2], AtelierRules.Colors[1] };
        for (int band = 0; band < 3; band++)
        {
            painter.SetTool(AtelierTool.FlatBrush); painter.SetColor(colors[band]); painter.SetRadius(0.58f);
            Vector3 start = center + new Vector3(-2.8f, 2f - band * 1.4f, 0);
            painter.BeginStroke(start);
            for (int i = 1; i <= 40; i++)
                painter.ContinueStroke(start + new Vector3(i * 0.14f, Mathf.Sin(i * 0.17f + band) * 0.4f, 0), 0.016f);
            painter.EndStroke();
        }
        painter.SetTool(AtelierTool.Flower); painter.SetColor(AtelierRules.Colors[3]); painter.SetRadius(0.38f);
        for (int i = 0; i < 4; i++) { painter.BeginStroke(center + new Vector3(-2.1f + i * 1.4f, -2.7f, 0)); painter.EndStroke(); }
        painter.SetTool(AtelierTool.RoundBrush); painter.surface.Flush();
    }

    private static void Capture(string name)
    {
        Canvas canvas = shop.layout.canvas;
        Camera camera = shop.painter.paintCamera;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
        RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
        bool oldScaler = scaler.enabled; float oldScale = canvas.scaleFactor;
        RenderTexture target = RenderTexture.GetTemporary(720, 1280, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(720, 1280, TextureFormat.RGB24, false);
        var uiObject = new GameObject("Check UI Camera", typeof(Camera));
        Camera uiCamera = uiObject.GetComponent<Camera>();
        uiCamera.enabled = false; uiCamera.orthographic = true; uiCamera.orthographicSize = 5;
        uiCamera.transform.position = new Vector3(0, 0, -10);
        uiCamera.cullingMask = 1 << 5; uiCamera.clearFlags = CameraClearFlags.Depth;
        uiCamera.targetTexture = target;
        int oldMask = camera.cullingMask;
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 5;
            scaler.enabled = false; canvas.scaleFactor = 720f / 1080f;
            Canvas.ForceUpdateCanvases(); shop.layout.RefreshLayout();
            camera.cullingMask &= ~(1 << 5);
            camera.Render(); uiCamera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); image.Apply(false);
            Directory.CreateDirectory("Logs/AtelierPreview");
            File.WriteAllBytes("Logs/AtelierPreview/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
            scaler.enabled = oldScaler; canvas.scaleFactor = oldScale;
            camera.cullingMask = oldMask; Object.DestroyImmediate(uiObject);
            RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image); shop.layout.RefreshLayout();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("ATELIER CHECK FAILED: " + message);
        SessionState.SetInt(CountKey, SessionState.GetInt(CountKey, 0) + 1); Debug.Log("ATELIER PASS: " + message);
    }

    private static void Finish(Exception exception)
    {
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= OnState;
        SessionState.EraseBool(Key);
        if (exception != null) Debug.LogException(exception);
        else Debug.Log("ATELIER SHOP CHECKS PASSED: " + SessionState.GetInt(CountKey, 0));
        EditorApplication.Exit(exception == null ? 0 : 1);
    }
}
