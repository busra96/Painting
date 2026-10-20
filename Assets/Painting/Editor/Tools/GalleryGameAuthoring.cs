using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PaintingGame.Atelier;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Creates editable scene objects and assets once. Runtime never constructs a screen hierarchy.
public static class GalleryGameAuthoring
{
    public const string ScenePath = "Assets/Painting/Scenes/GalleryAtelier.unity";
    private const string Data = "Assets/Painting/Settings/Gallery/";
    private const string Art = "Assets/Painting/Art/Gallery/";
    private static GalleryGameController game;
    private static TMP_FontAsset font;
    private static Sprite plate;
    private static readonly Color Ink = new Color(0.18f, 0.25f, 0.23f);
    private static readonly Color Cream = new Color(0.97f, 0.94f, 0.87f);
    private static readonly Color Green = new Color(0.17f, 0.32f, 0.28f);
    private static readonly Color Gold = new Color(0.78f, 0.58f, 0.29f);
    private static readonly List<GalleryButton> buttons = new List<GalleryButton>();
    public static void BuildBatch()
    { try { Build(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }
    public static void SaveRulesBatch()
    { var rules=AssetDatabase.LoadAssetAtPath<GalleryRules>(Data+"GalleryRules.asset");EditorUtility.SetDirty(rules);AssetDatabase.SaveAssets();EditorApplication.Exit(0); }

    [MenuItem("Tools/Painting/Create Gallery Atelier")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new InvalidOperationException("Gallery Atelier already exists. Edit its scene Canvas; manual edits are preserved.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Data); AssetDatabase.Refresh();
        Import(Art + "GalleryInterior.png", false);
        Import(Art + "Customers.png", true);
        EditorSceneManager.OpenScene("Assets/Painting/Scenes/Painting.unity");
        var ui = Object.FindFirstObjectByType<UIManager>();
        var config = AtelierSceneAuthoring.CreateData(ui.ColorSelection);
        config.allowTestRefill = false;
        config.touchLift = 0; // The touch contact always matches the place the player actually pressed.
        EditorUtility.SetDirty(config);
        var rules = Asset<GalleryRules>(Data + "GalleryRules.asset"); rules.painting = OilPalette(config);
        rules.customers = CreateCustomers(); rules.exhibitionSeconds = 100; rules.visitorInterval = 8; EditorUtility.SetDirty(rules);

        AssetDatabase.CopyAsset(AtelierShopSceneAuthoring.ScenePath, ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var old = Object.FindFirstObjectByType<AtelierShopController>();
        var paper = old.paper; var camera = old.painter.paintCamera;
        var backdrop = old.layout.backdrop;
        Object.DestroyImmediate(old.layout.canvas.gameObject);
        Object.DestroyImmediate(old.gameObject);
        game = new GameObject("Gallery Game • Composition Root").AddComponent<GalleryGameController>();
        game.rules = rules; game.paper = paper;
        var tools = game.gameObject.AddComponent<PaintToolController>();
        tools.config = config; tools.surface = paper.GetComponent<ImpastoPaintSurface>(); tools.mainCamera = camera;
        tools.usePointerEvents = true; tools.externalInventoryPersistence = true;
        game.tools = tools;
        AddCursor(tools);
        // Crop the existing linen texture to the physical canvas aspect, keeping simulation cells square.
        var tex = paper.sprite.texture;
        float aspect = 920f / 790f;
        float h = Mathf.Min(tex.height, tex.width / aspect), w = h * aspect;
        var linen = Sprite.Create(tex, new Rect((tex.width - w) / 2, (tex.height - h) / 2, w, h), Vector2.one * 0.5f, 100);
        AssetDatabase.CreateAsset(linen, Art + "GalleryCanvas.asset");
        paper.sprite = linen;
        paper.transform.localScale = new Vector3(8.8f / linen.bounds.size.x, (8.8f / aspect) / linen.bounds.size.y, 1);
        // A smaller local simulation and local dirty tiles bound mobile painting work.
        var surface = new SerializedObject(tools.surface); surface.FindProperty("resolution").intValue = 320; surface.ApplyModifiedPropertiesWithoutUndo();
        camera.backgroundColor = Ink;
        var canvas = new GameObject("Gallery Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.gameObject.layer = 5; canvas.transform.localScale = Vector3.one;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
        var safe = Rect("Safe Area", canvas.transform); Stretch(safe); var safeLayout = safe.gameObject.AddComponent<GallerySafeArea>();
        var root = Rect("Portrait Composition 1080x1920", safe);
        safeLayout.composition = root; safeLayout.Refresh();
        var layout = canvas.gameObject.AddComponent<AtelierCanvasLayout>(); layout.canvas = canvas;
        layout.paintCamera = camera; layout.paper = paper; layout.backdrop = backdrop; game.layout = layout;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");
        plate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/Atelier/UI_ToolSlot.png");
        buttons.Clear();
        game.pages = new GameObject[5];
        for (int i = 0; i < 5; i++) { var p = Rect(new[] { "Gallery Room", "Painting Workshop", "Artwork Storage", "Computer • Shop", "Computer • Advertising" }[i], root); Stretch(p); game.pages[i] = p.gameObject; }
        BuildGallery(game.pages[0].transform);
        RefreshCustomers();
        BuildWorkshop(game.pages[1].transform, layout);
        BuildCollection(game.pages[2].transform);
        BuildComputer(game.pages[3].transform, false);
        BuildComputer(game.pages[4].transform, true);
        var header = Panel("Gallery Header", root, Cream, 540, 1830, 1020, 150);
        Text("Brand", header.transform, "ATELIER  /  GALERİM", 46, Ink, 500, 103, 940, 63);
        game.wallet = Text("Wallet & Canvas Stock", header.transform, "", 32, Ink, 500, 44, 940, 49);
        var note = Panel("Status Paper", root, Cream, 540, 264, 1020, 126);
        game.status = Text("Status", note.transform, "", 31, Ink, 510, 63, 960, 108);
        var nav = Panel("Bottom Navigation", root, Green, 540, 100, 1080, 166);
        string[] titles = { "GALERİ", "BOYA", "ESERLER", "BİLGİSAYAR" };
        for (int i = 0; i < 4; i++) Button(titles[i], nav.transform, GalleryAction.Page, i, 140 + i * 267, 83, 246, 130);
        game.buttons = buttons.ToArray();
        for (int i = 0; i < game.pages.Length; i++) game.pages[i].SetActive(i == 0);
        paper.enabled = false; layout.RefreshLayout();
        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath)) EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("GALLERY ATELIER CREATED: " + ScenePath);
    }

    // Targeted upgrade of this newly authored scene. Does not rebuild the gallery or overwrite other scenes.
    public static void PolishBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath); game = Object.FindFirstObjectByType<GalleryGameController>();
            game.rules.painting=OilPalette(game.rules.painting);game.tools.config=game.rules.painting;
            game.layout.canvas.gameObject.layer=5;game.layout.canvas.transform.localScale=Vector3.one;
            var safe=game.layout.canvas.GetComponentInChildren<GallerySafeArea>(true);
            safe.composition=(RectTransform)safe.transform.GetChild(0);
            var fit=safe.composition.GetComponent<AspectRatioFitter>();if(fit!=null)Object.DestroyImmediate(fit);
            safe.Refresh();
            RefreshCustomers();
            foreach(var view in game.wallArt)if(view!=null)Object.DestroyImmediate(view.gameObject);
            game.wallArt=WallArt(game.pages[0].transform.Find("Hand Painted Gallery Interior"));
            BuildExtensions(game.pages[0].transform.Find("Hand Painted Gallery Interior"));
            var sign=game.pages[0].transform.Find("Exhibition Sign") as RectTransform;Box(sign,240,1670,360,128);
            Box(game.exhibition.rectTransform,180,64,338,116);game.exhibition.fontSize=32;game.exhibition.fontSizeMax=32;
            for(int i=3;i<5;i++){var label=game.pages[i].transform.Find("Computer Title").GetComponent<TMP_Text>();label.color=Ink;Box(label.rectTransform,540,1705,900,70);}
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Painting/Art/Fonts/Kalam SDF.asset");
            game.rules.exhibitionSeconds = 100; game.rules.visitorInterval = 8; game.rules.minimumCoverage = .035f; EditorUtility.SetDirty(game.rules);
            Import(Art + "ArtistTools.png",true);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "ArtistTools.png");
            for (int i=0;i<game.rules.painting.tools.Count;i++)
            {
                string path=Art+"ArtistTool_"+i+".asset";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null){sprite=Sprite.Create(texture,new Rect(i*texture.width/5f,0,texture.width/5f,texture.height),new Vector2(.5f,.85f),100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(sprite,path);}
                var tool=game.rules.painting.tools[i];tool.icon=sprite;tool.cursor=sprite;tool.cursorTip=null;
                tool.cursorScale=i==1?2.5f:i==2?.65f:1.3f;EditorUtility.SetDirty(tool);
            }
            var workshop=game.pages[1].transform;
            for(int i=workshop.childCount-1;i>=0;i--)Object.DestroyImmediate(workshop.GetChild(i).gameObject);
            buttons.Clear();buttons.AddRange(game.layout.canvas.GetComponentsInChildren<GalleryButton>(true));
            BuildWorkshop(workshop,game.layout);
            game.buttons=buttons.Where(b=>b!=null).ToArray();
            if(game.tools.cursor==null)AddCursor(game.tools);
            for(int i=0;i<game.pages.Length;i++)game.pages[i].SetActive(i==0);
            game.paper.enabled=false;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(game.gameObject.scene);
            Debug.Log("GALLERY POLISH SAVED");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    private static void AddCursor(PaintToolController tools)
    {
        var root=new GameObject("Tool Cursor Coordinate Space");
        var cursor=new GameObject("Animated Hand Tool").AddComponent<ToolCursorView>();cursor.transform.SetParent(root.transform,false);cursor.transform.localPosition=new Vector3(0,0,-1);
        var body=new GameObject("Hand Painted Tool",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();body.transform.SetParent(cursor.transform,false);body.sortingOrder=30;
        cursor.body=body;tools.cursor=cursor;
    }
    private static void BuildGallery(Transform parent)
    {
        Panel("Gallery Matte",parent,Ink,540,1040,1080,1500);
        var image = Panel("Hand Painted Gallery Interior", parent, Color.white, 540, 1040, 1000, 1500);
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "GalleryInterior.png");
        game.wallArt = WallArt(image.transform);
        game.customers = new GalleryCustomerView[6];
        for (int i = 0; i < 6; i++)
        {
            var r = Rect("Customer Pool " + (i + 1), image.transform); Box(r, 500, 350, 220, 330);
            var view = r.gameObject.AddComponent<GalleryCustomerView>();
            var body = Panel("Animated Sprite", r, Color.white, 110, 165, 220, 330);
            body.sprite = game.rules.customers[i].portrait; body.preserveAspect = true;
            view.body = body; view.animator = body.gameObject.AddComponent<Animator>(); view.animator.runtimeAnimatorController = game.rules.customers[i].animationController;
            view.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            CustomerPieces(view,game.rules.customers[i]);
            var bubble = Panel("Customer Thought", r, Cream, 110, 355, 250, 95);
            view.bubbleRoot = bubble.rectTransform;
            view.bubble = Text("Thought", bubble.transform, "", 29, Ink, 125, 47, 234, 87);
            game.customers[i] = view; r.gameObject.SetActive(false);
        }
        var exhibition = Panel("Exhibition Sign", parent, Green, 240, 1670, 360, 128);
        game.exhibition = Text("Exhibition Progress", exhibition.transform, "", 32, Cream, 180, 64, 338, 116);
        Button("SERGİ AÇ", parent, GalleryAction.Open, 0, 540, 410, 610, 120);
        // Clickable places inside the room, plus labelled navigation, keep the mobile affordance clear.
        Button("ATÖLYE", parent, GalleryAction.Page, 1, 200, 665, 240, 100);
        Button("BİLGİSAYAR", parent, GalleryAction.Page, 3, 845, 650, 280, 100);
        game.decorations = new GameObject[4]; game.extensions = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var deco = Panel("Decoration Upgrade " + (i + 1), image.transform, Gold, 180 + i * 195, 980 - Mathf.Abs(i - 1) * 25, 100, 9);
            game.decorations[i] = deco.gameObject;
        }
        BuildExtensions(image.transform);
    }
    private static void BuildWorkshop(Transform parent, AtelierCanvasLayout layout)
    {
        Panel("Workshop Paper Background", parent, new Color(0.79f,0.73f,0.61f), 540, 1050, 1080, 1460);
        var tube = Panel("Selected Paint Tube", parent, new Color(0.76f,0.73f,0.66f,0), 540, 1625, 840, 130);
        var inner = Panel("Paint Interior", tube.transform, Color.white, 330, 46, 608, 58);
        inner.type = Image.Type.Filled; inner.fillMethod = Image.FillMethod.Horizontal; inner.fillOrigin = 0;
        game.tubeFill = inner;
        inner.gameObject.SetActive(false);
        var tubeArt=Rect("Static Metal Tube",parent);Box(tubeArt,540,1640,840,90);
        game.tubeGraphic=tubeArt.gameObject.AddComponent<PaintTubeGraphic>();game.tubeGraphic.raycastTarget=false;
        game.tubeGraphic.artwork=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/UI/MetalTube.png");
        var cap = Panel("Tube Cap", parent, Ink, 797, 1625, 62, 82);
        cap.gameObject.SetActive(false);
        game.selectedTube = Text("Selected Tube Name", parent, "", 36, Ink, 540, 1720, 950, 50);
        var shadow = Panel("Canvas Shadow", parent, new Color(0.18f,0.14f,0.1f,0.34f), 551, 1032, 960, 830);
        var frame = Panel("Canvas Walnut Frame", parent, new Color(0.35f,0.23f,0.14f), 540, 1045, 952, 822);
        var region = Rect("Paint Gesture Area", parent); Box(region, 540, 1175, 920, 790); layout.paperRegion = region;
        var hit = region.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true; hit.canvasRenderer.cullTransparentMesh = false;
        region.gameObject.AddComponent<GalleryPaintInput>().game = game;
        // The physical paper renders behind this Canvas, so the board is cut into four panels around it.
        Object.DestroyImmediate(shadow.gameObject); Object.DestroyImmediate(frame.gameObject);
        Object.DestroyImmediate(parent.Find("Workshop Paper Background").gameObject);
        Panel("Top Desk", parent, new Color(0.83f,0.78f,0.67f), 540, 1725, 1080, 310).transform.SetAsFirstSibling();
        Panel("Bottom Desk", parent, new Color(0.83f,0.78f,0.67f), 540, 550, 1080, 460).transform.SetAsFirstSibling();
        Panel("Left Frame", region, Green, -13, 395, 26, 842);
        Panel("Right Frame", region, Green, 933, 395, 26, 842);
        Panel("Top Frame", region, Green, 460, 803, 946, 26);
        Panel("Bottom Frame", region, Green, 460, -13, 946, 26);
        for (int i = 0; i < game.rules.painting.tools.Count; i++)
        {
            var def = game.rules.painting.tools[i];
            var b = Button(def.displayName, parent, GalleryAction.Tool, i, 120 + i * 210, 550, 190, 154);
            var icon = Panel("Tool Illustration", b.transform, Color.white, 95, 104, 96, 96);
            icon.sprite = def.icon; icon.preserveAspect = true;
            Box(b.label.rectTransform, 95, 36, 180, 60); b.label.fontSize = 28;
        }
        for (int i = 0; i < game.rules.painting.colors.Count; i++)
        {
            var def = game.rules.painting.colors[i];
            var b = Button("", parent, GalleryAction.Color, i, 110 + i * 172, 700, 153, 133);
            b.GetComponent<Image>().color = Cream;
            var paint = Panel("Pigment", b.transform, def.pigment, 76, 103, 120, 24);
            var level = Panel("Remaining Paint", b.transform, def.pigment, 76, 21, 120, 9);
            level.type = Image.Type.Filled; level.fillMethod = Image.FillMethod.Horizontal; b.fill = level;
            Box(b.label.rectTransform, 76, 64, 142, 70); b.label.color = Ink; b.label.fontSize = 27;
        }
        Button("GERİ AL", parent, GalleryAction.Undo, 0, 185, 390, 280, 110);
        Button("ESERİ BİTİR", parent, GalleryAction.Finish, 0, 540, 390, 350, 110);
        Button("TEMİZLE", parent, GalleryAction.Clear, 0, 890, 390, 280, 110);
    }
    private static void BuildCollection(Transform parent)
    {
        Panel("Collection Paper", parent, Cream, 540, 1040, 1080, 1440);
        game.collection = Text("Collection Count", parent, "", 40, Ink, 540, 1650, 980, 130);
        Text("Collection Hint", parent, "Eserler hemen satılmaz. Koleksiyonu tamamla, sergiyi aç.", 30, Ink, 540, 1535, 990, 80);
        game.inventoryArt = new GalleryArtView[6];
        for (int i = 0; i < 6; i++)
        {
            var card = Panel("Stored Artwork " + (i + 1), parent, new Color(0.88f,0.83f,0.73f), 285 + i % 2 * 510, 1305 - i / 2 * 324, 465, 300);
            var view = card.gameObject.AddComponent<GalleryArtView>();
            var area = Rect("Image Area", card.transform); Box(area, 232, 187, 390, 183);
            var r = Rect("Actual Painting", area); Stretch(r);
            view.picture = r.gameObject.AddComponent<RawImage>(); view.picture.raycastTarget = false;
            view.aspect = r.gameObject.AddComponent<AspectRatioFitter>(); view.aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            view.caption = Text("Artwork Caption", card.transform, "", 28, Ink, 232, 53, 430, 82);
            game.inventoryArt[i] = view;
        }
        Button("HAZIR / SATILMAYAN", parent, GalleryAction.Filter, 0, 285, 455, 460, 110);
        Button("SONRAKİ SAYFA", parent, GalleryAction.Next, 0, 795, 455, 460, 110);
        Button("SATILMAYANLARI KALDIR", parent, GalleryAction.Discard, 0, 540, 348, 680, 88);
    }
    private static void BuildComputer(Transform parent, bool ads)
    {
        Panel("Computer Monitor Bezel", parent, Ink, 540, 1060, 1040, 1440);
        var screen = Panel("Computer Display", parent, Cream, 540, 1050, 980, 1360);
        Text("Computer Title", parent, "ATELIER  /  MASAÜSTÜ", 40, Ink, 540, 1705, 900, 70);
        Button("MAĞAZA", parent, GalleryAction.Page, 3, 295, 1590, 445, 120);
        Button("REKLAM", parent, GalleryAction.Page, 4, 785, 1590, 445, 120);
        if (!ads)
        {
            Text("Shop Heading", parent, "MALZEMELER & GALERİ", 37, Ink, 540, 1460, 900, 70);
            Button("10 TUVAL • " + game.rules.canvasPackPrice + " ALTIN", parent, GalleryAction.Canvases, 0, 540, 1350, 870, 120);
            for (int i = 0; i < game.rules.painting.colors.Count; i++)
            {
                var b = Button("", parent, GalleryAction.Tube, i, 295 + i % 2 * 490, 1195 - i / 2 * 156, 430, 136);
                var swatch = Panel("Pigment Stripe", b.transform, game.rules.painting.colors[i].pigment, 22, 68, 12, 108);
                b.label.fontSize = 31;
            }
            Button("", parent, GalleryAction.Expand, 0, 540, 660, 870, 130);
            Button("", parent, GalleryAction.Decorate, 0, 540, 490, 870, 130);
        }
        else
        {
            Text("Ad Heading", parent, "SERGİNİ DUYUR", 44, Ink, 540, 1440, 900, 80);
            game.campaignLabel = Text("Campaign Details", parent, "", 34, Ink, 540, 1290, 860, 170);
            string[] titles = { "MAHALLE AFİŞLERİ", "SANAT BÜLTENİ", "ŞEHİR KAMPANYASI" };
            for (int i = 0; i < 3; i++)
                Button(titles[i] + "\n" + game.rules.advertisingPrice * (i + 1) + " altın", parent, GalleryAction.Advertise, i + 1, 540, 1060 - i * 210, 850, 160);
            Text("Ad Guidance", parent, "İyi sergiler itibar kazandırır.\nReklam ziyaret sayısını artırır; her ziyaret satış değildir.", 33, Ink, 540, 490, 850, 170);
        }
    }
    private static CustomerProfile[] CreateCustomers()
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Customers.png");
        string[] names = { "Defne", "Mert", "Nermin", "Ada", "Deniz", "Kemal" };
        Color[] preferences = { new Color(.8f,.2f,.1f), new Color(.1f,.4f,.6f), new Color(.9f,.7f,.2f), new Color(.1f,.6f,.3f), new Color(.7f,.2f,.5f), new Color(.8f,.5f,.15f) };
        var profiles = new CustomerProfile[6];
        var animation = AssetDatabase.LoadAssetAtPath<AnimatorController>(Data + "Customer.controller");
        if (animation == null)
        {
            animation = AnimatorController.CreateAnimatorControllerAtPath(Data + "Customer.controller");
            animation.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var idle = Clip("Customer_Idle", .9f, 1.4f, false);
            var walk = Clip("Customer_Walk", 5f, .48f, true);
            var machine = animation.layers[0].stateMachine;
            var rest = machine.AddState("Looking at art"); rest.motion = idle;
            var walking = machine.AddState("Walking"); walking.motion = walk; machine.defaultState = rest;
            var go = rest.AddTransition(walking); go.hasExitTime = false; go.duration = .12f; go.AddCondition(AnimatorConditionMode.If,0,"Walking");
            var stop = walking.AddTransition(rest); stop.hasExitTime = false; stop.duration = .12f; stop.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
        }
        for (int i = 0; i < profiles.Length; i++)
        {
            var profile = Asset<CustomerProfile>(Data + "Customer_" + names[i] + ".asset");
            profile.displayName = names[i]; profile.preferredColor = preferences[i]; profile.budgetMultiplier = .8f + i * .13f; profile.animationController = animation;
            float cellW = texture.width / 3f, cellH = texture.height / 2f;
            var sprite = Sprite.Create(texture, new Rect(i % 3 * cellW, (1 - i / 3) * cellH, cellW, cellH), new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            AssetDatabase.CreateAsset(sprite, Art + "Customer_" + names[i] + ".asset"); profile.portrait = sprite;
            CustomerSprites(profile,i,texture);
            EditorUtility.SetDirty(profile); profiles[i] = profile;
        }
        return profiles;
    }
    private static GalleryArtView[] WallArt(Transform image)
    {
        // Source-image pixels, clockwise from top left. Each painting is projected into its frame opening.
        float[,] corners={
            {184,217,252,200,252,322,184,344}, {310,218,361,205,361,281,310,298},
            {418,145,482,131,482,265,418,282}, {690,228,729,244,729,316,690,297},
            {783,211,860,244,860,391,783,356}, {929,325,978,350,978,445,929,420}};
        var views=new GalleryArtView[6];
        for(int i=0;i<6;i++)
        {
            var r=Rect("Live Wall Painting "+(i+1),image);Stretch(r);
            var graphic=r.gameObject.AddComponent<GalleryPerspectiveImage>();graphic.raycastTarget=false;
            graphic.topLeft=Point(corners[i,0],corners[i,1]);graphic.topRight=Point(corners[i,2],corners[i,3]);
            graphic.bottomRight=Point(corners[i,4],corners[i,5]);graphic.bottomLeft=Point(corners[i,6],corners[i,7]);
            graphic.color=new Color(.99f,.97f,.91f);
            var view=r.gameObject.AddComponent<GalleryArtView>();view.picture=graphic;views[i]=view;
        }
        return views;
    }
    private static Vector2 Point(float x,float y)=>new Vector2(x/1024f,1-y/1536f);
    private static void BuildExtensions(Transform room)
    {
        if(game.extensions!=null)foreach(var old in game.extensions)if(old!=null)Object.DestroyImmediate(old);
        var walls=game.wallArt.ToList();game.extensions=new GameObject[4];
        for(int i=0;i<4;i++)
        {
            var easel=Rect("Expanded Gallery Display "+(i+1),room);Box(easel,290+i%2*420,650+i/2*260,155,220);
            var dark=new Color(.27f,.17f,.10f);
            var left=Panel("Left Easel Leg",easel,dark,45,80,12,160);left.rectTransform.localRotation=Quaternion.Euler(0,0,-8);
            var right=Panel("Right Easel Leg",easel,dark,112,80,12,160);right.rectTransform.localRotation=Quaternion.Euler(0,0,8);
            Panel("Picture Frame",easel,Gold,78,145,142,138);
            var paper=Panel("Canvas Inside Frame",easel,Cream,78,145,124,120);
            var r=Rect("Player Artwork",paper.transform);Stretch(r);var view=r.gameObject.AddComponent<GalleryArtView>();view.picture=r.gameObject.AddComponent<RawImage>();view.picture.raycastTarget=false;
            Panel("Easel Ledge",easel,dark,78,72,155,12);
            game.extensions[i]=easel.gameObject;walls.Add(view);
        }
        game.wallArt=walls.ToArray();
    }
    private static void CustomerSprites(CustomerProfile profile,int index,Texture2D atlas)
    {
        float w=atlas.width/3f,h=atlas.height/2f,x=index%3*w,y=(1-index/3)*h;
        profile.head=Part(profile,"Head",atlas,new Rect(x,y+h*.72f,w,h*.28f),new Vector2(.5f,0));
        profile.torso=Part(profile,"Torso",atlas,new Rect(x,y+h*.36f,w,h*.36f),new Vector2(.5f,.5f));
        profile.leftLeg=Part(profile,"LeftLeg",atlas,new Rect(x,y,w*.5f,h*.36f),new Vector2(.5f,1));
        profile.rightLeg=Part(profile,"RightLeg",atlas,new Rect(x+w*.5f,y,w*.5f,h*.36f),new Vector2(.5f,1));
        EditorUtility.SetDirty(profile);
    }
    private static Sprite Part(CustomerProfile owner,string name,Texture2D texture,Rect area,Vector2 pivot)
    {
        var sprite=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(owner)).OfType<Sprite>().FirstOrDefault(s=>s.name==name);
        if(sprite!=null)return sprite;
        sprite=Sprite.Create(texture,area,pivot,100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.AddObjectToAsset(sprite,owner);return sprite;
    }
    private static void CustomerPieces(GalleryCustomerView view,CustomerProfile profile)
    {
        view.body.enabled=false;
        view.body.rectTransform.sizeDelta=new Vector2(220,220);
        if(view.head==null)
        {
            view.leftLeg=Piece("Left Leg",view.body.transform,profile.leftLeg,55,79.2f,110,79.2f,new Vector2(.5f,1));
            view.rightLeg=Piece("Right Leg",view.body.transform,profile.rightLeg,165,79.2f,110,79.2f,new Vector2(.5f,1));
            view.torso=Piece("Torso",view.body.transform,profile.torso,110,118.8f,220,79.2f,new Vector2(.5f,.5f));
            view.head=Piece("Head",view.body.transform,profile.head,110,158.4f,220,61.6f,new Vector2(.5f,0));
        }
        else
        {
            Box(view.leftLeg.rectTransform,55,79.2f,110,79.2f);view.leftLeg.rectTransform.pivot=new Vector2(.5f,1);
            Box(view.rightLeg.rectTransform,165,79.2f,110,79.2f);view.rightLeg.rectTransform.pivot=new Vector2(.5f,1);
            Box(view.torso.rectTransform,110,118.8f,220,79.2f);
            Box(view.head.rectTransform,110,158.4f,220,61.6f);view.head.rectTransform.pivot=new Vector2(.5f,0);
        }
    }
    private static AtelierConfig OilPalette(AtelierConfig source)
    {
        var config=AssetDatabase.LoadAssetAtPath<AtelierConfig>(Data+"GalleryPainting.asset");
        if(config==null){config=Asset<AtelierConfig>(Data+"GalleryPainting.asset");EditorUtility.CopySerialized(source,config);}
        var white=Asset<PaintColorDefinition>(Data+"TitaniumWhite.asset");white.id="titanium-white";white.displayName="Beyaz";white.paletteSlot=ColorType.Pink;white.pigment=new Color(.97f,.95f,.89f);white.price=20;
        var umber=Asset<PaintColorDefinition>(Data+"BurntUmber.asset");umber.id="burnt-umber";umber.displayName="Toprak";umber.paletteSlot=ColorType.Orange;umber.pigment=new Color(.35f,.19f,.10f);umber.price=25;
        config.colors[3]=white;config.colors[5]=umber;config.allowTestRefill=false;config.touchLift=0;
        EditorUtility.SetDirty(white);EditorUtility.SetDirty(umber);EditorUtility.SetDirty(config);return config;
    }
    private static Image Piece(string name,Transform parent,Sprite sprite,float x,float y,float w,float h,Vector2 pivot)
    {var p=Panel(name,parent,Color.white,x,y,w,h);p.rectTransform.pivot=pivot;p.sprite=sprite;return p;}
    private static void RefreshCustomers()
    {
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Customers.png");
        for(int i=0;i<game.rules.customers.Length;i++){CustomerSprites(game.rules.customers[i],i,atlas);CustomerPieces(game.customers[i],game.rules.customers[i]);}
        foreach(string name in new[]{"Customer_Idle","Customer_Walk"})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Data+name+".anim");bool walking=name.EndsWith("Walk");float t=walking?.48f:1.4f;
            clip.SetCurve("Left Leg",typeof(RectTransform),"m_LocalEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,walking?-9:0),new Keyframe(t/2,walking?9:0),new Keyframe(t,walking?-9:0)));
            clip.SetCurve("Right Leg",typeof(RectTransform),"m_LocalEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,walking?9:0),new Keyframe(t/2,walking?-9:0),new Keyframe(t,walking?9:0)));
            clip.SetCurve("Head",typeof(RectTransform),"m_LocalEulerAnglesRaw.z",new AnimationCurve(new Keyframe(0,-1),new Keyframe(t/2,1),new Keyframe(t,-1)));
            EditorUtility.SetDirty(clip);
        }
    }
    private static AnimationClip Clip(string name, float sway, float duration, bool walking)
    {
        var clip = new AnimationClip { name = name, frameRate = 24 };
        clip.SetCurve("", typeof(RectTransform), "m_LocalEulerAnglesRaw.z", AnimationCurve.EaseInOut(0,-sway,duration/2,sway));
        var rotation = new AnimationCurve(new Keyframe(0,-sway),new Keyframe(duration/2,sway),new Keyframe(duration,-sway));
        clip.SetCurve("",typeof(RectTransform),"m_LocalEulerAnglesRaw.z", rotation);
        clip.SetCurve("",typeof(RectTransform),"m_AnchoredPosition.y", new AnimationCurve(new Keyframe(0,165),new Keyframe(duration/4,walking?173:167),new Keyframe(duration/2,165),new Keyframe(duration*3/4,walking?173:167),new Keyframe(duration,165)));
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,Data + name + ".anim"); return clip;
    }
    private static void Import(string path, bool transparent)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = transparent; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
    }
    private static T Asset<T>(string path) where T : ScriptableObject
    { var a = AssetDatabase.LoadAssetAtPath<T>(path); if (a != null) return a; a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a,path); return a; }
    private static RectTransform Rect(string name, Transform parent)
    { var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.gameObject.layer = 5; r.SetParent(parent,false); return r; }
    private static void Box(RectTransform r,float x,float y,float w,float h)
    { r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = Vector2.one*.5f; r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(w,h); }
    private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    private static Image Panel(string name,Transform parent,Color color,float x,float y,float w,float h)
    { var r = Rect(name,parent); Box(r,x,y,w,h); var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
    private static TMP_Text Text(string name,Transform parent,string value,float size,Color color,float x,float y,float w,float h)
    { var r = Rect(name,parent); Box(r,x,y,w,h); var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.enableAutoSizing = true; t.fontSizeMin = size*.82f; t.fontSizeMax = size; return t; }
    private static GalleryButton Button(string value,Transform parent,GalleryAction action,int index,float x,float y,float w,float h)
    {
        var shadow = Panel(value + " Shadow",parent,new Color(0,0,0,.16f),x,y-5,w,h);
        var p = Panel(value,parent,Green,x,y,w,h); p.raycastTarget = true;
        var b = p.gameObject.AddComponent<Button>(); b.targetGraphic = p;
        var colors = b.colors; colors.highlightedColor = new Color(.95f,.88f,.72f); colors.pressedColor = new Color(.7f,.64f,.5f); b.colors = colors;
        var view = p.gameObject.AddComponent<GalleryButton>(); view.game = game; view.action = action; view.index = index;
        view.label = Text("Label",p.transform,value,34,Cream,w/2,h/2,w-20,h-15);
        view.selection = Panel("Selection Line",p.transform,Gold,w/2,7,w-22,7); view.selection.enabled = false;
        buttons.Add(view); return view;
    }
}
