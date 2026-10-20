using System;
using System.Collections.Generic;
using TMPro;
using PaintingGame.Gallery;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time authoring of the gallery: catalog asset, target sprites, the overlay panel and the
// GALERİ / KAYDET buttons. Like the workshop builder it refuses to rebuild an existing panel,
// so later Inspector edits are never overwritten.
public static class GallerySceneAuthoring
{
    private const string ScenePath = "Assets/Painting/Scenes/Painting.unity";
    private const string CatalogPath = "Assets/Painting/Settings/PaintingCatalog.asset";
    private const string TargetsPath = "Assets/Painting/Art/Targets/";
    private const string Art = "Assets/Painting/Art/UI/";
    private static readonly Color Ink = new Color(0.29f, 0.17f, 0.09f);
    private static readonly Color Cream = new Color(0.97f, 0.92f, 0.82f);
    private static readonly Color Light = new Color(1, 0.94f, 0.77f);
    private static Sprite frame, plate;
    private static TMP_FontAsset font;

    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Painting/Create Gallery Panel")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        if (Object.FindFirstObjectByType<GalleryPanel>(FindObjectsInactive.Include) != null)
            throw new InvalidOperationException("Gallery is already authored. Edit 'Gallery Canvas' in the scene directly.");
        WorkshopVisualController view = Object.FindFirstObjectByType<WorkshopVisualController>();
        if (view == null) throw new InvalidOperationException("Author the Workshop Canvas first.");
        UIManager ui = view.GetComponent<UIManager>();
        Transform workshop = view.transform.Find("Workshop");

        frame = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "PaintedFrame.png");
        plate = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "DonePlateTrimmed.asset");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");
        PaintingCatalog catalog = CreateCatalog();

        // Selected target → Target Artwork image and the scoring reference.
        var applier = view.gameObject.AddComponent<PaintingTargetApplier>();
        applier.catalog = catalog;
        Transform card = workshop.Find("Target Card");
        applier.targetCard = card != null ? card.gameObject : null;
        applier.targetArtwork = card != null ? card.Find("Target Artwork")?.GetComponent<Image>() : null;
        applier.reference = Object.FindFirstObjectByType<ImageGridProcessor>(FindObjectsInactive.Include);

        // Workshop buttons share the free strip above the Amount panel.
        Button galleryButton = PlateButton("Gallery Button", workshop, "GALERİ", 818, 44, 210, 74, view, out _);
        Button saveButton = PlateButton("Save Button", workshop, "KAYDET", 592, 44, 210, 74, view, out TMP_Text saveLabel);
        var saver = view.gameObject.AddComponent<PaintingSaver>();
        saver.ui = ui; saver.mainCamera = view.mainCamera; saver.paper = view.paper;
        saver.saveButton = saveButton; saver.saveLabel = saveLabel;

        GalleryPanel panel = BuildPanel(catalog, ui);
        panel.openButton = galleryButton;
        if (view.paper != null && view.paper.sprite != null)
            panel.freePaintingAspect = view.paper.sprite.rect.width / view.paper.sprite.rect.height;

        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        EditorSceneManager.SaveScene(view.gameObject.scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("GALLERY SAVED: Gallery Canvas, GALERİ/KAYDET buttons and " + CatalogPath);
    }

    private static PaintingCatalog CreateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PaintingCatalog>(CatalogPath);
        if (catalog != null) return catalog;
        catalog = ScriptableObject.CreateInstance<PaintingCatalog>();
        var entries = new List<(string id, string title, string path)>
        {
            ("renkli-kule", "Renkli Kule", "Assets/Painting/Art/Sprites/Last.png"),
            ("gun-batimi", "Gün Batımı", TargetsPath + "Sunset.png"),
            ("deniz-kiyisi", "Deniz Kıyısı", TargetsPath + "Seaside.png"),
            ("cicek-tarlasi", "Çiçek Tarlası", TargetsPath + "Flowers.png"),
        };
        foreach (var (id, title, path) in entries)
        {
            Sprite sprite = ImportTarget(path);
            if (sprite == null) { Debug.LogWarning("Target sprite missing: " + path); continue; }
            catalog.paintings.Add(new PaintingCatalog.Entry { id = id, title = title, artwork = sprite });
        }
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        return catalog;
    }

    // Targets are sampled on the CPU for scoring, so they must be readable and uncompressed.
    private static Sprite ImportTarget(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GalleryPanel BuildPanel(PaintingCatalog catalog, UIManager ui)
    {
        var canvasObject = new GameObject("Gallery Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(941, 1672);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0;
        GalleryPanel panel = canvasObject.AddComponent<GalleryPanel>();
        panel.catalog = catalog; panel.ui = ui;

        RectTransform root = Rect("Gallery Root", canvasObject.transform); Stretch(root, 0, 0, 0, 0);
        panel.root = root.gameObject;
        Image dim = AddImage(root.gameObject, null, new Color(0.08f, 0.05f, 0.03f, 0.72f));
        dim.raycastTarget = true; // Blocks painting and workshop buttons behind the panel.

        Image board = Board("Board", root, Cream); Stretch(board.rectTransform, 28, 28, 70, 50);
        panel.titleLabel = Label("Title", board.transform, "GALERİM", 56, Ink);
        TopBand(panel.titleLabel.rectTransform, 30, 80, 40, 220);

        panel.closeButton = PlateButtonLocal("Close Button", board.transform, "KAPAT", out _);
        Corner(panel.closeButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-110, -66), new Vector2(180, 70));

        panel.newTabButton = PlateButtonLocal("New Painting Tab", board.transform, "YENİ TABLO", out _);
        Corner(panel.newTabButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(-200, -160), new Vector2(370, 84));
        panel.savedTabButton = PlateButtonLocal("Saved Tab", board.transform, "GALERİM", out _);
        Corner(panel.savedTabButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(200, -160), new Vector2(370, 84));

        // Scroll view with a 3-column grid.
        RectTransform scrollRect = Rect("Scroll View", board.transform); Stretch(scrollRect, 30, 30, 220, 34);
        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 30;
        RectTransform viewport = Rect("Viewport", scrollRect); Stretch(viewport, 0, 0, 0, 0);
        Image viewportHit = AddImage(viewport.gameObject, null, new Color(1, 1, 1, 0));
        viewportHit.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(262, 420); grid.spacing = new Vector2(16, 20);
        grid.padding = new RectOffset(4, 4, 8, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        panel.scroll = scroll; panel.content = content;
        panel.itemTemplate = BuildCard(content);
        panel.emptyLabel = Label("Empty Label", scrollRect, "Henüz kaydedilmiş tablo yok.", 36, Ink);
        Stretch(panel.emptyLabel.rectTransform, 40, 40, 40, 40);
        panel.emptyLabel.gameObject.SetActive(false);

        BuildDetail(panel, board.transform);
        root.gameObject.SetActive(false);
        return panel;
    }

    private static GalleryItemView BuildCard(Transform content)
    {
        Image cardImage = Board("Card Template", content, Color.white);
        Button button = cardImage.gameObject.AddComponent<Button>();
        button.targetGraphic = cardImage; cardImage.raycastTarget = true;
        SetButtonColors(button);
        var view = cardImage.gameObject.AddComponent<GalleryItemView>();
        view.button = button;

        RectTransform pictureArea = Rect("Picture Area", cardImage.transform); Stretch(pictureArea, 22, 22, 22, 112);
        RectTransform pictureRect = Rect("Picture", pictureArea);
        view.picture = pictureRect.gameObject.AddComponent<RawImage>();
        view.picture.raycastTarget = false;
        view.pictureFitter = pictureRect.gameObject.AddComponent<AspectRatioFitter>();
        view.pictureFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        view.pictureFitter.aspectRatio = 851f / 1261f;

        view.caption = Label("Caption", cardImage.transform, "Tablo", 30, Ink);
        BottomBand(view.caption.rectTransform, 56, 44, 12);
        view.detail = Label("Detail", cardImage.transform, "", 22, new Color(0.45f, 0.32f, 0.2f));
        BottomBand(view.detail.rectTransform, 22, 32, 12);

        Image mark = AddImage(Rect("Current Mark", cardImage.transform).gameObject, frame, new Color(0.96f, 0.36f, 0.08f));
        mark.type = Image.Type.Sliced; mark.pixelsPerUnitMultiplier = 8; mark.fillCenter = false;
        Stretch(mark.rectTransform, -6, -6, -6, -6);
        view.currentMark = mark.gameObject;
        mark.gameObject.SetActive(false);
        return view;
    }

    private static void BuildDetail(GalleryPanel panel, Transform board)
    {
        Image detail = Board("Detail", board, Cream); Stretch(detail.rectTransform, 0, 0, 0, 0);
        detail.raycastTarget = true;
        panel.detailRoot = detail.gameObject;
        panel.detailTitle = Label("Detail Title", detail.transform, "Tablo", 52, Ink);
        TopBand(panel.detailTitle.rectTransform, 30, 76, 40, 40);

        RectTransform area = Rect("Picture Area", detail.transform); Stretch(area, 60, 60, 120, 330);
        RectTransform picture = Rect("Picture", area);
        panel.detailPicture = picture.gameObject.AddComponent<RawImage>();
        panel.detailPicture.raycastTarget = false;
        panel.detailFitter = picture.gameObject.AddComponent<AspectRatioFitter>();
        panel.detailFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        panel.detailFitter.aspectRatio = 851f / 1261f;

        panel.detailInfo = Label("Detail Info", detail.transform, "", 32, Ink);
        BottomBand(panel.detailInfo.rectTransform, 180, 110, 60);

        panel.primaryButton = PlateButtonLocal("Primary Button", detail.transform, "BAŞLA", out panel.primaryLabel);
        Corner(panel.primaryButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(-190, 90), new Vector2(330, 104));
        panel.backButton = PlateButtonLocal("Back Button", detail.transform, "GERİ", out _);
        Corner(panel.backButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(190, 90), new Vector2(330, 104));
        detail.gameObject.SetActive(false);
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static Button PlateButton(string name, Transform workshop, string text, float x, float y, float width,
        float height, WorkshopVisualController view, out TMP_Text label)
    {
        Button button = PlateButtonLocal(name, workshop, text, out label);
        Place(button.GetComponent<RectTransform>(), x, y, width, height);
        // Workshop canvas units change with the painting projection; keep type proportional like other labels.
        var scale = label.gameObject.AddComponent<WorkshopTextScale>();
        scale.referenceRoot = view.transform as RectTransform; scale.label = label; scale.fontSizeAt941 = 34; scale.Refresh();
        return button;
    }

    private static Button PlateButtonLocal(string name, Transform parent, string text, out TMP_Text label)
    {
        Image image = AddImage(Rect(name, parent).gameObject, plate, Color.white);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        SetButtonColors(button);
        label = Label("Label", image.transform, text, 36, Light);
        label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 36;
        Stretch(label.rectTransform, 14, 14, 6, 6);
        return button;
    }

    private static Image Board(string name, Transform parent, Color tint)
    {
        Image image = AddImage(Rect(name, parent).gameObject, frame, tint);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 8;
        return image;
    }

    private static Image AddImage(GameObject target, Sprite sprite, Color color)
    {
        Image image = target.AddComponent<Image>();
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void SetButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.90f);
        colors.pressedColor = new Color(0.84f, 0.76f, 0.65f);
        colors.disabledColor = new Color(0.7f, 0.66f, 0.6f, 0.75f);
        button.colors = colors;
    }

    // Workshop coordinates are authored against the 941×1672 reference, like the other workshop tools.
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        Vector2 center = new Vector2(x / 941, 1 - y / 1672), half = new Vector2(width / 941, height / 1672) * 0.5f;
        rect.anchorMin = center - half; rect.anchorMax = center + half; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
    }

    private static void TopBand(RectTransform rect, float top, float height, float left, float right)
    {
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
    }

    private static void BottomBand(RectTransform rect, float bottom, float height, float sides)
    {
        rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(0.5f, 0);
        rect.offsetMin = new Vector2(sides, bottom); rect.offsetMax = new Vector2(-sides, bottom + height);
    }

    private static void Corner(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
