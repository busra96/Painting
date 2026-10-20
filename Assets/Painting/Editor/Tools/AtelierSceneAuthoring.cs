using System;
using System.Collections.Generic;
using System.Linq;
using PaintingGame.Atelier;
using PaintingGame.Gallery;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One-time authoring of the painting atelier.
// 1. Imports the atelier sprites and creates the data assets (colours, tools, AtelierConfig).
// 2. Copies Painting.unity to Atelier.unity (the original scene and its checks stay untouched).
// 3. Replaces the pour→spatula flow with the tool system and lays out the mobile UI.
// Like the other builders it refuses to overwrite an existing Atelier scene, so Inspector edits are kept.
public static class AtelierSceneAuthoring
{
    private const string SourceScene = "Assets/Painting/Scenes/Painting.unity";
    private const string ScenePath = "Assets/Painting/Scenes/Atelier.unity";
    private const string DataPath = "Assets/Painting/Settings/Atelier/";
    private const string Art = "Assets/Painting/Art/Atelier/";
    private const string UiArt = "Assets/Painting/Art/UI/";
    private static readonly Color Ink = new Color(0.29f, 0.17f, 0.09f);
    private static readonly Color Cream = new Color(0.97f, 0.92f, 0.82f);
    private static TMP_FontAsset font;
    private static RectTransform referenceRoot;

    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Painting/Create Atelier Scene")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Atelier.unity already exists. Edit it directly; the builder will not overwrite it.");

        ImportArt();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");

        EditorSceneManager.OpenScene(SourceScene);
        ColorSelection sourceSelection = Object.FindFirstObjectByType<UIManager>().ColorSelection;
        AtelierConfig config = CreateData(sourceSelection);

        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath)) throw new InvalidOperationException("Could not copy " + SourceScene);
        var scene = EditorSceneManager.OpenScene(ScenePath);

        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        WorkshopVisualController view = Object.FindFirstObjectByType<WorkshopVisualController>();
        if (ui == null || view == null) throw new InvalidOperationException("Painting.unity must contain the authored workshop and gallery first.");
        GalleryPanel gallery = Object.FindFirstObjectByType<GalleryPanel>(FindObjectsInactive.Include);
        if (gallery == null) throw new InvalidOperationException("Run Tools → Painting → Create Gallery Panel on Painting.unity first.");
        Transform root = view.transform.Find("Workshop");
        referenceRoot = view.transform as RectTransform;
        ImpastoPaintSurface surface = view.paper.GetComponent<ImpastoPaintSurface>();
        ColorSelection selection = ui.ColorSelection;
        Button undo = ui.UndoButton, clear = ui.ClearButton, done = ui.DoneButton;
        GridDataComparer comparer = ui.Spatula.GetComponentInChildren<DragYPos_Spatula>(true).gridDataComparer;

        RetireLegacyFlow(ui, view, selection, root, gallery);
        Layout(root, view);
        SelectedTubeView tubeView = BuildSelectedTube(view);
        TubeGaugeView[] gauges = BuildGauges(view);
        RectTransform tray = BuildToolTray(root);
        ToolCursorView cursor = BuildCursor(view.paper);
        ModalView modal = BuildModal(gallery.transform);

        // Composition root.
        var atelier = new GameObject("Atelier");
        PaintToolController controller = atelier.AddComponent<PaintToolController>();
        controller.config = config; controller.surface = surface; controller.mainCamera = view.mainCamera;
        controller.palette = selection; controller.cursor = cursor;
        tubeView.controller = controller;
        TubeRackView rack = view.paintAmount.gameObject.AddComponent<TubeRackView>();
        rack.controller = controller; rack.gauges = gauges;
        ToolTrayView trayView = tray.gameObject.AddComponent<ToolTrayView>();
        trayView.controller = controller;
        trayView.container = tray.Find("Slots") as RectTransform;
        trayView.template = trayView.container.Find("Tool Template").GetComponent<ToolButtonView>();

        AtelierHUD hud = atelier.AddComponent<AtelierHUD>();
        hud.controller = controller; hud.surface = surface; hud.comparer = comparer; hud.gallery = gallery; hud.modal = modal;
        hud.undoButton = undo; hud.clearButton = clear; hud.finishButton = done;
        hud.finishLabel = done.GetComponentInChildren<TMP_Text>(true);
        hud.finishLabel.text = "BİTİR";
        hud.statusLabel = root.Find("Status Text").GetComponent<TMP_Text>();
        hud.artworkCounter = Label("Artwork Counter", root, "ESER 0/15", 30, new Color(1, 0.93f, 0.76f), 30);
        Place(hud.artworkCounter.rectTransform, 560, 44, 300, 64);

        view.RefreshLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("ATELIER SAVED: " + ScenePath + " with " + config.tools.Count + " tools and " + config.colors.Count + " paint tubes.");
    }

    // ------------------------------------------------------------------ data

    public static AtelierConfig CreateData(ColorSelection selection)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Painting/Settings/Atelier"))
            AssetDatabase.CreateFolder("Assets/Painting/Settings", "Atelier");
        var config = LoadOrCreate<AtelierConfig>("AtelierConfig");
        config.colors.Clear();
        config.colors.Add(PaintColor("red", "Kırmızı", ColorType.Red, selection.RedColor));
        config.colors.Add(PaintColor("yellow", "Sarı", ColorType.Yellow, selection.YellowColor));
        config.colors.Add(PaintColor("green", "Yeşil", ColorType.Green, selection.GreenColor));
        config.colors.Add(PaintColor("pink", "Pembe", ColorType.Pink, selection.PinkColor));
        config.colors.Add(PaintColor("blue", "Mavi", ColorType.Blue, selection.BlueColor));
        config.colors.Add(PaintColor("orange", "Turuncu", ColorType.Orange, selection.OrangeColor));

        var wide = LoadOrCreate<BrushToolDefinition>("Tool_BrushWide");
        Common(wide, "brush-wide", "Geniş Fırça", "Geniş alanları boyar. Boyasız fırça tuvaldeki boyayı karıştırır.",
            "Icon_BrushWide", "Cursor_BrushWide", "Cursor_BrushWideTip", 3.2f, true, 0.075f);
        wide.lengthRatio = 0.35f; wide.squareness = 0.8f; wide.bristles = 22; wide.bristleContrast = 0.6f;
        wide.unitsPerCanvasWidth = 7; wide.unitsPerSecondStill = 0.5f; wide.smudge = 0.45f; wide.release = 0.35f;
        wide.spacing = 0.2f; wide.speedThinning = 0.2f;

        var thin = LoadOrCreate<BrushToolDefinition>("Tool_BrushThin");
        Common(thin, "brush-thin", "İnce Fırça", "Çizgi ve ayrıntı için.",
            "Icon_BrushThin", "Cursor_BrushThin", "Cursor_BrushThinTip", 7.1f, true, 0.022f);
        thin.lengthRatio = 1; thin.squareness = 0; thin.bristles = 6; thin.bristleContrast = 0.35f;
        thin.unitsPerCanvasWidth = 2.2f; thin.unitsPerSecondStill = 0.25f; thin.smudge = 0.3f; thin.release = 0.4f;
        thin.spacing = 0.25f; thin.speedThinning = 0.35f;

        var knife = LoadOrCreate<KnifeToolDefinition>("Tool_Knife");
        Common(knife, "knife", "Spatula", "Boyayı istediğin yöne, istediğin kadar çek.",
            "Icon_Knife", "Cursor_Knife", "Cursor_KnifeTip", 0.53f, true, 0.3f);
        knife.filmScale = 1; knife.pickup = 0.85f;

        var press = LoadOrCreate<PressToolDefinition>("Tool_Press");
        Common(press, "press", "Baskı", "Basılı tut: boyayı ezip dağıtır, sürükle: dokulu iz bırakır.",
            "Icon_Press", "Cursor_Press", null, 1.085f, false, 0.09f);
        press.holdStrength = 2.2f; press.dragStrength = 0.35f; press.textureScale = 0.18f;

        var pour = LoadOrCreate<PourToolDefinition>("Tool_Pour");
        Common(pour, "pour", "Tüp", "Tüpten kalın boya sık, beklersen boya kabarır.",
            "Icon_Pour", "Cursor_Pour", "Cursor_PourTip", 10f, false, 0.06f);
        pour.unitsPerSecond = 7; pour.unitsPerCanvasWidth = 15;

        config.tools.Clear();
        config.tools.AddRange(new PaintToolDefinition[] { wide, thin, knife, press, pour });
        config.defaultTool = wide;
        foreach (Object asset in config.tools.Cast<Object>().Concat(config.colors).Append(config)) EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static PaintColorDefinition PaintColor(string id, string name, ColorType slot, Color pigment)
    {
        var color = LoadOrCreate<PaintColorDefinition>("Color_" + id);
        color.id = id; color.displayName = name; color.paletteSlot = slot; color.pigment = pigment;
        color.tubeCapacity = 80; color.startingFill = 1; color.price = 25;
        return color;
    }

    private static void Common(PaintToolDefinition tool, string id, string name, string hint, string icon, string cursor,
        string tip, float cursorScale, bool follows, float size)
    {
        tool.id = id; tool.displayName = name; tool.hint = hint; tool.size = size;
        tool.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Art + icon + ".png");
        tool.cursor = AssetDatabase.LoadAssetAtPath<Sprite>(Art + cursor + ".png");
        tool.cursorTip = tip != null ? AssetDatabase.LoadAssetAtPath<Sprite>(Art + tip + ".png") : null;
        tool.cursorScale = cursorScale; tool.cursorFollowsStroke = follows;
    }

    private static T LoadOrCreate<T>(string name) where T : ScriptableObject
    {
        string path = DataPath + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // ------------------------------------------------------------------ art import

    private static readonly Dictionary<string, Vector2> CursorPivots = new Dictionary<string, Vector2>
    {
        // Working point of each cursor sprite (bristle centre, blade edge, pad centre, nozzle).
        { "Cursor_BrushWide", new Vector2(0.5f, 0.815f) }, { "Cursor_BrushWideTip", new Vector2(0.5f, 0.815f) },
        { "Cursor_BrushThin", new Vector2(0.5f, 0.83f) }, { "Cursor_BrushThinTip", new Vector2(0.5f, 0.83f) },
        { "Cursor_Knife", new Vector2(0.5f, 0.63f) }, { "Cursor_KnifeTip", new Vector2(0.5f, 0.63f) },
        { "Cursor_Press", new Vector2(0.5f, 0.5f) },
        { "Cursor_Pour", new Vector2(0.5f, 0.94f) }, { "Cursor_PourTip", new Vector2(0.5f, 0.94f) },
    };

    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "UI_ToolSlot", new Vector4(64, 64, 64, 64) }, { "UI_SelectedRing", new Vector4(64, 64, 64, 64) },
    };

    private static void ImportArt()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art.TrimEnd('/') }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = name.StartsWith("Cursor_"); // world sprites are minified on small screens
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = name.StartsWith("Cursor_") || name.StartsWith("Icon_") ? 512 : 256;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (CursorPivots.TryGetValue(name, out Vector2 pivot))
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
            }
            if (Borders.TryGetValue(name, out Vector4 border)) settings.spriteBorder = border;
            settings.spritePixelsPerUnit = 512;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------------ scene edits

    private static void RetireLegacyFlow(UIManager ui, WorkshopVisualController view, ColorSelection selection,
        Transform root, GalleryPanel gallery)
    {
        Test_RaycastSpawn_CameraPerspective spawner = ui.RaycastAndSpawn;
        if (spawner != null)
        {
            if (spawner.PaintDecalParent != null) spawner.PaintDecalParent.SetActive(false);
            spawner.gameObject.SetActive(false);
        }
        if (ui.ShadowDecalObj != null) ui.ShadowDecalObj.SetActive(false);
        if (ui.Spatula != null) ui.Spatula.SetActive(false);
        selection.ColorPaintDecalClassList?.Clear();
        selection.ShadowObj = null;
        // The tube inventory replaces the per-painting budget; keep the components so references stay valid.
        view.paintAmount.enabled = false;
        if (view.amountTube != null) view.amountTube.enabled = false;
        Object.DestroyImmediate(view.GetComponent<PaintingSaver>());
        Object.DestroyImmediate(view.GetComponent<PaintHistory>());
        Transform save = root.Find("Save Button");
        if (save != null) Object.DestroyImmediate(save.gameObject);
        gallery.ui = null;
        Object.DestroyImmediate(ui);
    }

    // Portrait layout on the 941×1672 reference used by the workshop tools.
    private static void Layout(Transform root, WorkshopVisualController view)
    {
        Move(root, "Target Card", 132, 172, 226, 278);
        Move(root, "Amount Panel", 600, 172, 600, 172);
        // The canvas frame keeps its aspect so the authored paint-area mapping is unchanged.
        Move(root, "Canvas Frame", 476, 722, 727, 810);
        Move(root, "Palette Panel", 470, 1214, 883, 160);
        Move(root, "Done Button", 470, 1566, 340, 108);
        Move(root, "Undo Button", 116, 1566, 128, 108);
        Move(root, "Clear Button", 826, 1566, 124, 118);
        Move(root, "Status Text", 470, 1640, 760, 40);
        Move(root, "Gallery Button", 830, 44, 190, 70);
    }

    private static SelectedTubeView BuildSelectedTube(WorkshopVisualController view)
    {
        Transform panel = view.paintAmount.transform;
        var tube = panel.gameObject.AddComponent<SelectedTubeView>();
        tube.tube = view.amountTube.tube;
        tube.percentLabel = view.amountTube.remainingLabel;
        Transform title = panel.Find("Title Tape/Title");
        tube.nameLabel = title != null ? title.GetComponent<TMP_Text>() : null;
        return tube;
    }

    private static TubeGaugeView[] BuildGauges(WorkshopVisualController view)
    {
        ColorType[] order = { ColorType.Red, ColorType.Yellow, ColorType.Green, ColorType.Pink, ColorType.Blue, ColorType.Orange };
        Sprite gaugeSprite = Sprite("UI_Gauge"), badge = Sprite("UI_EmptyBadge");
        var gauges = new List<TubeGaugeView>();
        for (int i = 0; i < view.paletteButtons.Length && i < order.Length; i++)
        {
            RectTransform dab = view.paletteButtons[i];
            RectTransform holder = Rect("Tube Gauge", dab);
            Anchor(holder, 0.74f, 0.16f, 0.9f, 0.84f);
            Image low = Img("Low Glow", holder, gaugeSprite, new Color(1f, 0.45f, 0.15f, 1));
            Anchor(low.rectTransform, -0.35f, -0.08f, 1.35f, 1.08f);
            low.enabled = false;
            Image back = Img("Back", holder, gaugeSprite, new Color(0.18f, 0.1f, 0.05f, 0.55f));
            Stretch(back.rectTransform);
            Image fill = Img("Fill", holder, gaugeSprite, new Color(1f, 0.96f, 0.86f, 0.95f));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Vertical; fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            Image empty = Img("Empty", dab, badge, Color.white);
            Anchor(empty.rectTransform, 0.3f, 0.3f, 0.7f, 0.7f);
            empty.preserveAspect = true;
            empty.gameObject.SetActive(false);
            var gauge = holder.gameObject.AddComponent<TubeGaugeView>();
            gauge.slot = order[i]; gauge.fill = fill; gauge.emptyMark = empty.gameObject; gauge.lowWarning = low;
            gauges.Add(gauge);
        }
        return gauges.ToArray();
    }

    private static RectTransform BuildToolTray(Transform root)
    {
        Image tray = Img("Tool Tray", root, AssetDatabase.LoadAssetAtPath<Sprite>(UiArt + "TabletopPlankTrimmed.asset"), Color.white);
        Place(tray.rectTransform, 470, 1384, 883, 168);
        RectTransform slots = Rect("Slots", tray.transform);
        Anchor(slots, 0.04f, 0.1f, 0.96f, 0.9f);
        var layout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        layout.spacing = 0; layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform template = Rect("Tool Template", slots);
        Image hit = template.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0); hit.raycastTarget = true;
        RectTransform lift = Rect("Lift", template); Stretch(lift);
        Image glow = Img("Glow", lift, Sprite("UI_SelectedRing"), Color.white);
        Anchor(glow.rectTransform, 0.04f, 0.0f, 0.96f, 1.0f); glow.preserveAspect = true; glow.enabled = false;
        Image slot = Img("Slot", lift, Sprite("UI_ToolSlot"), Color.white);
        Anchor(slot.rectTransform, 0.1f, 0.06f, 0.9f, 0.94f); slot.preserveAspect = true;
        Image icon = Img("Icon", lift, null, Color.white);
        Anchor(icon.rectTransform, 0.2f, 0.26f, 0.8f, 0.9f); icon.preserveAspect = true;
        TMP_Text label = Label("Label", lift, "Fırça", 22, Ink, 22);
        Anchor(label.rectTransform, 0.05f, 0.02f, 0.95f, 0.3f);
        Button button = template.gameObject.AddComponent<Button>();
        button.targetGraphic = slot;
        var view = template.gameObject.AddComponent<ToolButtonView>();
        view.button = button; view.icon = icon; view.label = label; view.selectedGlow = glow; view.lift = lift;
        return tray.rectTransform;
    }

    private static ToolCursorView BuildCursor(SpriteRenderer paper)
    {
        var space = new GameObject("Tool Cursor Space");
        space.transform.position = new Vector3(0, 0, paper.transform.position.z - 0.4f);
        var cursorObject = new GameObject("Tool Cursor");
        cursorObject.transform.SetParent(space.transform, false);
        SpriteRenderer body = cursorObject.AddComponent<SpriteRenderer>();
        body.sortingLayerID = paper.sortingLayerID; body.sortingOrder = paper.sortingOrder + 50;
        var tipObject = new GameObject("Tip");
        tipObject.transform.SetParent(cursorObject.transform, false);
        tipObject.transform.localPosition = new Vector3(0, 0, -0.01f);
        SpriteRenderer tip = tipObject.AddComponent<SpriteRenderer>();
        tip.sortingLayerID = paper.sortingLayerID; tip.sortingOrder = paper.sortingOrder + 51;
        var cursor = cursorObject.AddComponent<ToolCursorView>();
        cursor.body = body; cursor.tip = tip;
        return cursor;
    }

    private static ModalView BuildModal(Transform overlayCanvas)
    {
        Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>(UiArt + "PaintedFrame.png");
        Sprite plate = AssetDatabase.LoadAssetAtPath<Sprite>(UiArt + "DonePlateTrimmed.asset");
        RectTransform modalObject = Rect("Modal", overlayCanvas); Stretch(modalObject);
        modalObject.SetAsLastSibling();
        RectTransform root = Rect("Modal Root", modalObject); Stretch(root);
        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.05f, 0.03f, 0.6f); dim.raycastTarget = true;
        Image card = Img("Card", root, frame, Cream);
        card.type = Image.Type.Sliced; card.pixelsPerUnitMultiplier = 8; card.raycastTarget = true;
        Center(card.rectTransform, 0, 40, 780, 720);
        TMP_Text title = Label("Title", card.transform, "Tablo bitti!", 58, Ink, 0);
        Center(title.rectTransform, 0, 270, 680, 90);
        var stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            stars[i] = Img("Star " + (i + 1), card.transform, Sprite("UI_Star"), Color.white);
            Center(stars[i].rectTransform, -150 + i * 150, 150 + (i == 1 ? 18 : 0), i == 1 ? 140 : 116, i == 1 ? 140 : 116);
        }
        TMP_Text body = Label("Body", card.transform, "", 34, Ink, 0);
        Center(body.rectTransform, 0, -40, 660, 220);
        Button primary = PlateButton("Primary", card.transform, plate, out TMP_Text primaryLabel);
        Center(primary.GetComponent<RectTransform>(), -170, -255, 300, 100);
        Button secondary = PlateButton("Secondary", card.transform, plate, out TMP_Text secondaryLabel);
        Center(secondary.GetComponent<RectTransform>(), 170, -255, 300, 100);
        var modal = modalObject.gameObject.AddComponent<ModalView>();
        modal.root = root.gameObject; modal.card = card.rectTransform; modal.title = title; modal.body = body; modal.stars = stars;
        modal.primary = primary; modal.secondary = secondary; modal.primaryLabel = primaryLabel; modal.secondaryLabel = secondaryLabel;
        root.gameObject.SetActive(false);
        return modal;
    }

    private static void AddToBuildSettings()
    {
        // Appended: Painting.unity stays the first build scene (AtelierShopChecks relies on it).
        if (EditorBuildSettings.scenes.Any(s => s.path == ScenePath)) return;
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
    }

    // ------------------------------------------------------------------ helpers

    private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

    private static void Move(Transform root, string path, float x, float y, float w, float h)
    {
        if (root.Find(path) is RectTransform rect) Place(rect, x, y, w, h);
        else Debug.LogWarning("Atelier layout: '" + path + "' not found; left as is.");
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
        return image;
    }

    // scaledSize > 0 keeps type proportional on the stretched workshop canvas.
    private static TMP_Text Label(string name, Transform parent, string text, float size, Color color, float scaledSize)
    {
        TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = size;
        if (scaledSize > 0 && referenceRoot != null)
        {
            var scale = label.gameObject.AddComponent<WorkshopTextScale>();
            scale.referenceRoot = referenceRoot; scale.label = label; scale.fontSizeAt941 = scaledSize; scale.Refresh();
        }
        return label;
    }

    private static Button PlateButton(string name, Transform parent, Sprite plate, out TMP_Text label)
    {
        Image image = Img(name, parent, plate, Color.white);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = Label("Label", image.transform, "TAMAM", 36, new Color(1, 0.94f, 0.77f), 0);
        Anchor(label.rectTransform, 0.08f, 0.1f, 0.92f, 0.9f);
        return button;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        Vector2 center = new Vector2(x / 941, 1 - y / 1672), half = new Vector2(width / 941, height / 1672) * 0.5f;
        rect.anchorMin = center - half; rect.anchorMax = center + half; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect) => Anchor(rect, 0, 0, 1, 1);

    private static void Center(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
}
