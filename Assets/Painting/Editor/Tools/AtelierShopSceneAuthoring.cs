using System;
using System.IO;
using System.Linq;
using PaintCore;
using PaintIn2D;
using PaintingGame.Atelier;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class AtelierShopSceneAuthoring
{
    public const string ScenePath = "Assets/Painting/Scenes/AtelierShop.unity";
    private static readonly Color Ink = new Color(0.21f, 0.12f, 0.075f);
    private static readonly Color Cream = new Color(0.95f, 0.88f, 0.75f);
    private static readonly Color Teal = new Color(0.11f, 0.31f, 0.30f);
    private static TMP_FontAsset font;
    private static Sprite wood, dab;
    private static AtelierShopController controller;

    [MenuItem("Tools/Painting/Create Atelier Shop Scene")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Atelier Shop already exists. Edit its Canvas in the scene; creation never overwrites manual edits.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");
        wood = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/UI/TabletopPlank.png");
        dab = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/UI/MattePaintDab.png");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 9f;
        camera.transform.position = new Vector3(0, 0, -20);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.23f, 0.13f, 0.08f);
        SpriteRenderer background = new GameObject("Painted Wooden Worktable", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/UI/TabletopBackground.png");
        background.sortingOrder = -50;
        SpriteRenderer paper = new GameObject("Blank Paintable Canvas", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        paper.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/Sprites/WhitePage.png");
        Vector3 spriteSize = paper.sprite.bounds.size;
        paper.transform.localScale = new Vector3(8.8f / spriteSize.x, (8.8f * 790f / 920f) / spriteSize.y, 1);
        paper.sortingOrder = 0;
        var model = paper.gameObject.AddComponent<CwPaintableSprite>();
        model.AutoSize = false; model.AutoTexture = false; model.AutoMask = false;
        var texture = paper.GetComponent<CwPaintableSpriteTexture>();
        if (texture == null) texture = paper.gameObject.AddComponent<CwPaintableSpriteTexture>();
        texture.Slot = new CwSlot(0, "_MainTex");
        texture.Width = 384; texture.Height = 406; texture.Color = Color.white;
        texture.UndoRedo = CwPaintableTexture.UndoRedoType.None;
        var surface = paper.gameObject.AddComponent<ImpastoPaintSurface>();
        var surfaceSettings = AssetDatabase.LoadAssetAtPath<SettingsContain>("Assets/Painting/Settings/AtelierSettings.asset");
        if (surfaceSettings == null)
        {
            surfaceSettings = ScriptableObject.CreateInstance<SettingsContain>();
            surfaceSettings.name = "Atelier paint settings";
            AssetDatabase.CreateAsset(surfaceSettings, "Assets/Painting/Settings/AtelierSettings.asset");
        }
        surface.settings = surfaceSettings;
        var serializedSurface = new SerializedObject(surface);
        serializedSurface.FindProperty("resolution").intValue = 384;
        serializedSurface.FindProperty("paintShader").objectReferenceValue = Shader.Find("Painting/Impasto Paper");
        serializedSurface.ApplyModifiedPropertiesWithoutUndo();
        var owner = new GameObject("Atelier Shop - Game Loop");
        controller = owner.AddComponent<AtelierShopController>();
        var painter = owner.AddComponent<AtelierCanvasPainter>();
        painter.surface = surface; painter.paintCamera = camera;
        controller.paper = paper; controller.painter = painter;

        Canvas canvas = new GameObject("Atelier Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.gameObject.layer = 5;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var layout = canvas.gameObject.AddComponent<AtelierCanvasLayout>();
        layout.paintCamera = camera; layout.canvas = canvas; layout.paper = paper; layout.backdrop = background;
        controller.layout = layout;

        Image header = Panel("Shop Header", canvas.transform, new Color(0.13f, 0.075f, 0.04f, 0.96f));
        Box(header.rectTransform, 540, 1805, 1080, 230);
        Label("Game Title", header.transform, "RENK ATÖLYEM", 54, Cream, 540, 165, 1000, 72);
        controller.economyLabel = Label("Economy", header.transform, "", 29, new Color(1f, 0.77f, 0.31f), 540, 91, 990, 55);
        Button("Target Mode", header.transform, "HEDEF GÖRSEL MODU", AtelierAction.Challenge, 0, 540, 30, 570, 44);
        Button("Workshop Tab", canvas.transform, "ATÖLYE", AtelierAction.Tab, 0, 185, 1640, 320, 82);
        Button("Store Tab", canvas.transform, "MAĞAZAM", AtelierAction.Tab, 1, 540, 1640, 320, 82);
        Button("Supplies Tab", canvas.transform, "MALZEMELER", AtelierAction.Tab, 2, 895, 1640, 320, 82);
        Image status = Panel("Status Board", canvas.transform, new Color(0.96f, 0.86f, 0.66f));
        Box(status.rectTransform, 540, 1540, 1020, 102);
        controller.statusLabel = Label("Message", status.transform, "", 29, Ink, 510, 51, 972, 94);

        RectTransform workshop = Rect("Workshop", canvas.transform);
        Stretch(workshop); controller.workshopPanel = workshop.gameObject;
        controller.titleLabel = Label("Canvas Type", workshop, "", 24, Cream, 600, 1465, 470, 42);
        Button("Wide Spatula", workshop, "GENİŞ SPATULA", AtelierAction.Tool, 8, 230, 1465, 360, 42);
        RectTransform region = Rect("Paint Area", workshop);
        Box(region, 540, 1040, 920, 790); layout.paperRegion = region;
        AddPaintInput(region, controller);
        Border(region);
        controller.paintLabel = Label("Paint And Value", workshop, "", 27, Cream, 540, 456, 1010, 48);
        Image tools = Panel("Tool Rack", workshop, Cream);
        Box(tools.rectTransform, 540, 320, 1020, 212);
        for (int i = 0; i < 8; i++)
            Button("Tool " + ((AtelierTool)i), tools.transform, "", AtelierAction.Tool, i,
                132 + (i % 4) * 252, i < 4 ? 160 : 55, 238, 88);

        Image palette = Panel("Pigment Palette", workshop, new Color(0.72f, 0.48f, 0.26f));
        Box(palette.rectTransform, 540, 565, 1020, 118);
        for (int i = 0; i < AtelierRules.Colors.Length; i++)
        {
            AtelierActionButton action = Button("Pigment " + i, palette.transform, "", AtelierAction.Color, i,
                67 + i * 126, 59, 94, 94);
            Image graphic = action.GetComponent<Image>();
            graphic.sprite = dab; graphic.color = AtelierRules.Colors[i];
            if (action.label != null) { action.label.color = i == 1 || i == 3 ? Ink : Cream; action.label.fontSize = 44; }
        }
        Label("Size Label", workshop, "BOYUT", 24, Cream, 105, 177, 160, 40);
        Slider size = Slider("Brush Size", workshop, 350, 177, 310);
        size.minValue = 0.10f; size.maxValue = 0.70f; size.value = 0.34f;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(size.onValueChanged, painter.SetRadius);
        Label("Pressure Label", workshop, "BASINÇ", 24, Cream, 629, 177, 160, 40);
        Slider pressure = Slider("Brush Pressure", workshop, 870, 177, 310);
        pressure.minValue = 0.1f; pressure.maxValue = 1f; pressure.value = 0.7f;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(pressure.onValueChanged, painter.SetPressure);
        Button("New Canvas", workshop, "", AtelierAction.NewCanvas, 0, 224, 70, 370, 92);
        Button("Undo", workshop, "GERİ AL", AtelierAction.Undo, 0, 530, 70, 210, 92);
        Button("Exhibit", workshop, "VİTRİNE KOY", AtelierAction.Exhibit, 0, 855, 70, 382, 92);
        Button("Clear", workshop, "TEMİZLE", AtelierAction.Clear, 0, 950, 1465, 155, 42);

        RectTransform shop = Rect("Shop", canvas.transform); Stretch(shop); controller.shopPanel = shop.gameObject;
        Image wall = Panel("Gallery Wall", shop, new Color(0.89f, 0.83f, 0.71f));
        Box(wall.rectTransform, 540, 745, 1080, 1460);
        controller.shopLabel = Label("Gallery Stats", shop, "", 33, Ink, 540, 1405, 1010, 75);
        controller.customerLabel = Label("Customer", shop, "", 29, Ink, 540, 1315, 1010, 72);
        RectTransform content = Scroll("Gallery Scroll", shop, 540, 670, 1010, 1160);
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(465, 410); grid.spacing = new Vector2(24, 24);
        grid.padding = new RectOffset(22, 22, 22, 22); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        controller.cards = new AtelierArtworkCard[12];
        for (int i = 0; i < 12; i++) controller.cards[i] = Card(content, i);

        RectTransform supplies = Rect("Supplies", canvas.transform); Stretch(supplies); controller.suppliesPanel = supplies.gameObject;
        Image supplyBoard = Panel("Supplies Board", supplies, new Color(0.89f, 0.83f, 0.71f));
        Box(supplyBoard.rectTransform, 540, 745, 1080, 1460);
        Label("Upgrades Title", supplies, "KENDİ ATÖLYENİ BÜYÜT", 42, Ink, 540, 1410, 1000, 90);
        Label("Upgrades Hint", supplies, "Yeni aletler farklı izler bırakır. Kaliteli tuval daha değerli satılır.", 28, Ink, 540, 1310, 970, 100);
        for (int i = 0; i < 3; i++)
            Button("Canvas Tier " + i, supplies, "", AtelierAction.Canvas, i, 540, 1170 - i * 150, 940, 126);
        Button("Pigment Upgrade", supplies, "", AtelierAction.Palette, 0, 285, 690, 450, 150);
        Button("Shop Expansion", supplies, "", AtelierAction.Expand, 0, 795, 690, 450, 150);
        Label("Tool Hints", supplies, "DÜZ FIRÇA: geniş, çizgili iz\nFAN FIRÇA: yumuşak, taranmış doku\nBASTIR: boyayı bulunduğu yerde düzleştir\nPALET BIÇAĞI: boyayı istediğin yöne taşı\nÇİÇEK / YILDIZ: sürükledikçe aralıklı desen bas", 32, Ink, 540, 420, 950, 310);
        Label("Purchase Hint", supplies, "Aletleri ATÖLYE'deki fiyatlı düğmelerinden alabilirsin.\nMüşteriler vitrindeki tabloları sırayla satın alır.", 28, Ink, 540, 155, 950, 135);

        controller.buttons = canvas.GetComponentsInChildren<AtelierActionButton>(true);
        shop.gameObject.SetActive(false); supplies.gameObject.SetActive(false);
        layout.RefreshLayout();
        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("ATELIER SHOP SCENE CREATED: " + ScenePath);
    }

    public static void BuildBatch()
    {
        try { Build(); Retire3DPrototype(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static void Retire3DPrototype()
    {
        string[] paths = {
            "Assets/Painting/Scenes/Tests/OilPaint3D.unity", "Assets/Painting/Scripts/Painting3D",
            "Assets/Painting/Materials/Painting3D", "Assets/Painting/Shaders/OilPaint3D.shader",
            "Assets/Painting/Editor/Tools/OilPaint3DSceneAuthoring.cs", "Assets/Painting/Editor/Checks/OilPaint3DChecks.cs" };
        foreach (string path in paths) if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path))
            if (!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not retire " + path);
        Debug.Log("Project-owned 3D oil-paint prototype retired; vendor assets are preserved.");
    }

    private static AtelierArtworkCard Card(Transform parent, int index)
    {
        Image background = Panel("Exhibition Slot " + (index + 1), parent, new Color(0.70f, 0.47f, 0.26f));
        var card = background.gameObject.AddComponent<AtelierArtworkCard>();
        RectTransform pictureArea = Rect("Picture Area", background.transform);
        Box(pictureArea, 232, 246, 405, 285);
        RectTransform imageRoot = Rect("Painting", pictureArea); Stretch(imageRoot);
        RawImage image = imageRoot.gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
        var aspect = imageRoot.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        card.picture = image; card.aspect = aspect;
        card.caption = Label("Caption", background.transform, "", 30, Cream, 232, 55, 430, 85);
        return card;
    }

    private static RectTransform Scroll(string name, Transform parent, float x, float y, float w, float h)
    {
        Image viewport = Panel(name, parent, new Color(0.83f, 0.75f, 0.61f));
        Box(viewport.rectTransform, x, y, w, h);
        viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        RectTransform content = Rect("Content", viewport.transform);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
        scroll.viewport = viewport.rectTransform; scroll.content = content;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        return content;
    }

    private static Slider Slider(string name, Transform parent, float x, float y, float width)
    {
        Image root = Panel(name, parent, new Color(0.37f, 0.25f, 0.16f));
        root.raycastTarget = true;
        Box(root.rectTransform, x, y, width, 38);
        var slider = root.gameObject.AddComponent<Slider>();
        Image fill = Panel("Fill", root.transform, new Color(0.92f, 0.58f, 0.19f)); Stretch(fill.rectTransform);
        Image handle = Panel("Handle", root.transform, Cream);
        handle.rectTransform.sizeDelta = new Vector2(34, 58);
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        return slider;
    }

    private static void Border(RectTransform region)
    {
        float width = region.sizeDelta.x, height = region.sizeDelta.y;
        Image left = Panel("Left Wooden Edge", region, new Color(0.44f, 0.25f, 0.11f));
        Box(left.rectTransform, -12, height * 0.5f, 24, height + 48);
        Image right = Panel("Right Wooden Edge", region, left.color); Box(right.rectTransform, width + 12, height * 0.5f, 24, height + 48);
        Image top = Panel("Top Wooden Edge", region, left.color); Box(top.rectTransform, width * 0.5f, height + 12, width + 48, 24);
        Image bottom = Panel("Bottom Wooden Edge", region, left.color); Box(bottom.rectTransform, width * 0.5f, -12, width + 48, 24);
    }

    public static void PolishLayoutBatch()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var owner = Object.FindFirstObjectByType<AtelierShopController>();
            controller = owner;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");
            if (owner.workshopPanel.transform.Find("Wide Spatula") == null)
            {
                Button("Wide Spatula", owner.workshopPanel.transform, "GENİŞ SPATULA", AtelierAction.Tool, 8, 230, 1465, 360, 42);
                Box(owner.titleLabel.rectTransform, 600, 1465, 470, 42);
                owner.buttons = owner.layout.canvas.GetComponentsInChildren<AtelierActionButton>(true);
            }
            SpriteRenderer paper = owner.paper;
            Vector3 size = paper.sprite.bounds.size;
            paper.transform.localScale = new Vector3(8.8f / size.x, (8.8f * 790f / 920f) / size.y, 1);
            RectTransform region = owner.layout.paperRegion;
            Box(region, 540, 1040, 920, 790);
            while (region.childCount > 0) Object.DestroyImmediate(region.GetChild(0).gameObject);
            Border(region);
            foreach (AtelierArtworkCard card in owner.cards)
            {
                if (card.transform.Find("Picture Area") != null) continue;
                RectTransform area = Rect("Picture Area", card.transform);
                Box(area, 232, 246, 405, 285);
                card.picture.transform.SetParent(area, false);
                Stretch(card.picture.rectTransform);
            }
            owner.layout.RefreshLayout();
            EditorSceneManager.SaveScene(scene);
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static void AddPaintInput(RectTransform region, AtelierShopController owner)
    {
        Image hitArea = region.GetComponent<Image>();
        if (hitArea == null) hitArea = region.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear; hitArea.raycastTarget = true;
        hitArea.canvasRenderer.cullTransparentMesh = false;
        AtelierPaintInput input = region.GetComponent<AtelierPaintInput>();
        if (input == null) input = region.gameObject.AddComponent<AtelierPaintInput>();
        input.owner = owner;
    }

    public static void RepairPaintInputBatch()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var owner = Object.FindFirstObjectByType<AtelierShopController>();
            AddPaintInput(owner.layout.paperRegion, owner);
            foreach (Slider slider in owner.layout.canvas.GetComponentsInChildren<Slider>(true))
            {
                Image background = slider.GetComponent<Image>();
                if (background != null) background.raycastTarget = true;
            }
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ATELIER PAINT INPUT REPAIRED");
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static AtelierActionButton Button(string name, Transform parent, string value, AtelierAction action,
        int index, float x, float y, float w, float h)
    {
        Image image = Panel(name, parent, Teal); image.raycastTarget = true;
        Box(image.rectTransform, x, y, w, h);
        Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        ColorBlock colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.disabledColor = new Color(0.68f, 0.68f, 0.68f, 0.8f); button.colors = colors;
        TMP_Text label = Label("Label", image.transform, value, 30, Cream, w * 0.5f, h * 0.5f, w - 14, h - 10);
        var binding = image.gameObject.AddComponent<AtelierActionButton>();
        binding.owner = controller; binding.action = action; binding.index = index; binding.label = label;
        return binding;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, Color color,
        float x, float y, float w, float h)
    {
        RectTransform rect = Rect(name, parent); Box(rect, x, y, w, h);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text;
        label.fontSize = size; label.color = color; label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true; label.fontSizeMin = Mathf.Min(20, size); label.fontSizeMax = size;
        return label;
    }

    private static Image Panel(string name, Transform parent, Color color)
    { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
    }

    private static void Box(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
