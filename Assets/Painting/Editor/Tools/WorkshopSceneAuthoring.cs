using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit one-time authoring. It refuses to rebuild an existing layout, preserving manual edits.
public static class WorkshopSceneAuthoring
{
    private const string ScenePath = "Assets/Painting/Scenes/Painting.unity";
    private const string ArtPath = "Assets/Painting/Art/UI/";
    private static readonly Color Ink = new Color(0.25f, 0.14f, 0.08f);
    private static Sprite frame, dab;
    private static TMP_FontAsset font;

    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Painting/Create Editable Workshop Canvas")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        if (Object.FindFirstObjectByType<WorkshopVisualController>() != null)
            throw new InvalidOperationException("Workshop is already authored. Edit its Canvas children directly; the builder will not overwrite them.");

        ConfigureTexture("StudioGouache.png", 2048, Vector4.zero);
        ConfigureTexture("PaintedFrame.png", 2048, new Vector4(180, 180, 180, 180));
        ConfigureTexture("MattePaintDab.png", 512, Vector4.zero);
        frame = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "PaintedFrame.png");
        dab = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "MattePaintDab.png");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/ThirdParty/GUIProKit/ResourcesData/Fonts/LilitaOne-Regular SDF.asset");

        UIManager manager = Object.FindFirstObjectByType<UIManager>();
        Canvas canvas = manager.GetComponent<Canvas>();
        ColorSelection selection = manager.ColorSelection;
        PaintAmount oldAmount = Object.FindFirstObjectByType<PaintAmount>();
        DragYPos_Spatula drag = manager.Spatula.GetComponentInChildren<DragYPos_Spatula>(true);
        GridDataComparer oldComparer = drag.gridDataComparer;
        var evaluation = new GameObject("Painting Evaluation").AddComponent<GridDataComparer>();
        EditorUtility.CopySerialized(oldComparer, evaluation);
        evaluation.slider = null;
        evaluation.yuzdelikText = null;
        drag.gridDataComparer = evaluation;
        float maxAmount = oldAmount.MaxAmount;
        EventSystem events = Object.FindFirstObjectByType<EventSystem>();
        if (events != null) events.transform.SetParent(null, true);

        // Unpack old UI instances before deleting them, so no inactive legacy panels remain.
        foreach (Transform child in canvas.transform)
        {
            GameObject instance = PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject);
            if (instance != null && instance.transform.IsChildOf(canvas.transform))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        while (canvas.transform.childCount > 0) Object.DestroyImmediate(canvas.transform.GetChild(0).gameObject);
        if (oldComparer != null) Object.DestroyImmediate(oldComparer.gameObject);

        SpriteRenderer paper = GameObject.Find("Paintable Book  Paint").GetComponent<SpriteRenderer>();
        const string previewPath = "Assets/Painting/Materials/PaperPreview.mat";
        Material paperPreview = AssetDatabase.LoadAssetAtPath<Material>(previewPath);
        if (paperPreview == null)
        {
            paperPreview = new Material(Shader.Find("Painting/Impasto Paper"));
            paperPreview.SetFloat("_BackdropCanvas", 1);
            AssetDatabase.CreateAsset(paperPreview, previewPath);
        }
        paper.sharedMaterial = paperPreview;
        Camera camera = Camera.main;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 12f;
        canvas.sortingLayerID = paper.sortingLayerID;
        canvas.sortingOrder = paper.sortingOrder - 100;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(941, 1672);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0;

        RectTransform root = Rect("Workshop", canvas.transform);
        Stretch(root);
        Image background = Image("Background", root, AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "StudioGouache.png"));
        Stretch(background.rectTransform);

        Image targetCard = Board("Target Card", root, 155, 182, 238, 300);
        targetCard.rectTransform.localRotation = Quaternion.Euler(0, 0, 4);
        Image target = Image("Target Artwork", targetCard.transform,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Painting/Art/Sprites/Last.png"));
        target.preserveAspect = true;
        Inset(target.rectTransform, 23);

        Image amountBoard = Board("Amount Panel", root, 587, 126, 548, 145);
        TMP_Text title = Label("Title", amountBoard.transform, "Amount", 34, Ink);
        Local(title.rectTransform, 0, 35, 430, 42);
        PaintAmount amount = amountBoard.gameObject.AddComponent<PaintAmount>();
        amount.MaxAmount = maxAmount;
        Slider amountData = amountBoard.gameObject.AddComponent<Slider>();
        amountData.interactable = false;
        amountData.transition = Selectable.Transition.None;
        amountData.targetGraphic = null;
        amountBoard.color = Color.white;
        amountData.minValue = 0;
        amountData.maxValue = amountData.value = 100;
        amount.PaintAmountSlider = amountData;
        PaintTubeAmountView tube = CreatePaintTube(amount, selection);

        Image status = Board("Status Panel", root, 587, 268, 548, 100);
        status.color = new Color(0.30f, 0.37f, 0.42f);
        TMP_Text statusLabel = Label("Status Text", status.transform, "BASILI TUT: BOYA BIRAK", 23,
            new Color(1f, 0.92f, 0.75f));
        Inset(statusLabel.rectTransform, 26);
        manager.StatusLabel = statusLabel;

        Image canvasFrame = Board("Canvas Frame", root, 476, 764, 801, 879);
        RectTransform paintRegion = Rect("Paint Area", canvasFrame.transform);
        paintRegion.anchorMin = new Vector2(36.5f / 801, 39.5f / 879);
        paintRegion.anchorMax = Vector2.one - paintRegion.anchorMin;
        paintRegion.offsetMin = paintRegion.offsetMax = Vector2.zero;

        Image palette = Board("Palette Panel", root, 470, 1351, 906, 211);
        palette.color = new Color(0.79f, 0.59f, 0.37f);
        Button[] buttons = new Button[6];
        string[] names = { "Red", "Yellow", "Green", "Pink", "Blue", "Orange" };
        Color[] colors = { selection.RedColor, selection.YellowColor, selection.GreenColor,
            selection.PinkColor, selection.BlueColor, selection.OrangeColor };
        for (int i = 0; i < buttons.Length; i++)
        {
            Image image = Image(names[i], palette.transform, dab);
            image.color = colors[i];
            image.preserveAspect = true;
            image.raycastTarget = true;
            Local(image.rectTransform, -362 + i * 145, 0, 126, 130);
            buttons[i] = image.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = image;
            SetButtonColors(buttons[i]);
        }
        selection.RedButton = buttons[0]; selection.YellowButton = buttons[1];
        selection.GreenButton = buttons[2]; selection.PinkButton = buttons[3];
        selection.BlueButton = buttons[4]; selection.OrangeButton = buttons[5];
        selection.WhiteButton = null;
        Image marker = Image("Selected Paint", palette.transform, dab);
        marker.color = new Color(1f, 0.89f, 0.62f);
        Local(marker.rectTransform, -362, -77, 35, 6);

        Image doneImage = Board("Done Button", root, 470, 1542, 417, 124);
        doneImage.raycastTarget = true;
        Button done = doneImage.gameObject.AddComponent<Button>();
        done.targetGraphic = doneImage;
        SetButtonColors(done);
        TMP_Text doneLabel = Label("Button Label", done.transform, "DONE", 49, Ink);
        Inset(doneLabel.rectTransform, 20);
        manager.DoneButton = done;

        WorkshopVisualController controller = canvas.gameObject.AddComponent<WorkshopVisualController>();
        controller.mainCamera = camera; controller.canvas = canvas; controller.paper = paper;
        controller.paintRegion = paintRegion; controller.backgroundImage = background;
        controller.paintAmount = amount; controller.selection = selection;
        controller.amountTube = tube; controller.selectedMark = marker.rectTransform;
        controller.paletteButtons = new RectTransform[buttons.Length];
        for (int i = 0; i < buttons.Length; i++) controller.paletteButtons[i] = buttons[i].GetComponent<RectTransform>();

        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
            if (renderer.name == "Paintable Book  BlackLine" || renderer.name == "Last Image Sprite Renderer  Grid Processors")
                renderer.enabled = false;
        manager.Spatula.SetActive(false);
        controller.RefreshLayout();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("EDITABLE WORKSHOP SAVED: Canvas/Workshop with separate sprites, buttons, labels and paint tube.");
    }

    public static void InstallPaintTubeBatch()
    {
        try { InstallPaintTube(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Painting/Replace Amount Segments With Paint Tube")]
    public static void InstallPaintTube()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath);
        WorkshopVisualController controller = Object.FindFirstObjectByType<WorkshopVisualController>();
        if (controller == null) throw new InvalidOperationException("Author the Workshop Canvas first.");
        if (controller.amountTube != null) return; // Never rebuild a manually edited tube.
        PaintAmount amount = controller.paintAmount;
        Transform oldSegments = amount.transform.Find("Amount Segments");
        if (oldSegments != null) Object.DestroyImmediate(oldSegments.gameObject);
        controller.amountTube = CreatePaintTube(amount, controller.selection);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorSceneManager.SaveScene(controller.gameObject.scene, ScenePath);
        Debug.Log("PAINT TUBE SAVED: only the Amount children changed; existing Canvas layout retained.");
    }

    private static PaintTubeAmountView CreatePaintTube(PaintAmount amount, ColorSelection selection)
    {
        TMP_Text title = amount.transform.Find("Title").GetComponent<TMP_Text>();
        font = title.font;
        // Leave the authored title position and the enclosing panel untouched.
        RectTransform graphicRect = Rect("Paint Tube", amount.transform);
        Local(graphicRect, 0, -23, 460, 90);
        graphicRect.gameObject.AddComponent<CanvasRenderer>();
        PaintTubeGraphic graphic = graphicRect.gameObject.AddComponent<PaintTubeGraphic>();
        graphic.raycastTarget = false;
        TMP_Text percent = Label("Remaining Paint", amount.transform, "100%", 22, Ink);
        Local(percent.rectTransform, 193, 35, 76, 32);
        PaintTubeAmountView view = graphicRect.gameObject.AddComponent<PaintTubeAmountView>();
        view.amount = amount; view.selection = selection; view.tube = graphic; view.remainingLabel = percent;
        return view;
    }

    private static void ConfigureTexture(string file, int size, Vector4 border)
    {
        string path = ArtPath + file;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = border;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = size;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }
    private static Image Image(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }
    private static Image Board(string name, Transform parent, float x, float y, float width, float height)
    {
        Image image = Image(name, parent, frame);
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 8;
        Place(image.rectTransform, x, y, width, height);
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
    private static void SetButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.90f);
        colors.pressedColor = new Color(0.84f, 0.76f, 0.65f);
        colors.disabledColor = new Color(0.85f, 0.81f, 0.73f, 1f);
        button.colors = colors;
    }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        Vector2 center = new Vector2(x / 941, 1 - y / 1672);
        Vector2 half = new Vector2(width / 941, height / 1672) * 0.5f;
        rect.anchorMin = center - half; rect.anchorMax = center + half;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Local(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Inset(RectTransform rect, float padding)
    {
        Stretch(rect); rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding;
    }
}
