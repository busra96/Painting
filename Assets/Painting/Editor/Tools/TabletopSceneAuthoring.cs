using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TabletopSceneAuthoring
{
    private const string Art = "Assets/Painting/Art/UI/";
    private static readonly Color Ink = new Color(0.29f, 0.17f, 0.09f);
    public static void BuildBatch()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Painting/Apply Reference Tabletop Layout")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Painting/Scenes/Painting.unity");
        WorkshopVisualController view = Object.FindFirstObjectByType<WorkshopVisualController>();
        if (view == null) throw new InvalidOperationException("The editable workshop must already exist.");
        if (view.tabletopLayout) return;
        foreach (string name in new[] { "TabletopBackground", "TabletopPlank", "MetalTube", "PaletteDab", "TargetPaper", "DonePlate", "TabletopReference" })
            Import(name, name == "PaletteDab" ? 512 : 2048);
        Sprite plank = Trimmed("TabletopPlank"), dab = Trimmed("PaletteDab"), card = Trimmed("TargetPaper");
        Sprite doneArt = Trimmed("DonePlate");
        Sprite canvasArt = Reference("ReferenceCanvas", new Rect(72, 1672 - 1224, 808, 900));
        Sprite undoArt = Reference("UndoCard", new Rect(54, 1672 - 1567, 149, 126));
        Sprite trashArt = Reference("TrashCard", new Rect(750, 1672 - 1571, 137, 131));
        // A blank paper patch in the supplied artwork supplies the tape labels' original texture.
        Sprite paperPatch = Reference("PaperTape", new Rect(109, 1672 - 1120, 300, 130));
        TMP_FontAsset font = CreateFont();
        Transform root = view.transform.Find("Workshop");
        UIManager ui = view.GetComponent<UIManager>();
        view.backgroundImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "TabletopBackground.png");

        Image target = root.Find("Target Card").GetComponent<Image>();
        SetImage(target, card); Place(target.rectTransform, 154, 190, 263, 323);
        target.rectTransform.localRotation = Quaternion.Euler(0, 0, 5.5f);
        Image targetDrawing = target.transform.Find("Target Artwork").GetComponent<Image>();
        Local(targetDrawing.rectTransform, 0, -13, 198, 239);
        targetDrawing.color = Color.white;
        targetDrawing.preserveAspect = false;

        Image amountPanel = view.paintAmount.GetComponent<Image>();
        SetImage(amountPanel, plank); Place(amountPanel.rectTransform, 596, 179, 610, 184);
        TMP_Text title = amountPanel.transform.Find("Title").GetComponent<TMP_Text>();
        title.font = font; title.text = "Amount"; title.fontSize = 34; title.color = Ink;
        title.transform.SetParent(NewImage("Title Tape", amountPanel.transform, paperPatch).transform, false);
        RectTransform titleTape = title.transform.parent as RectTransform;
        Local(titleTape, -5, 78, 272, 51);
        Stretch(title.rectTransform); title.rectTransform.offsetMin = new Vector2(8, 0); title.rectTransform.offsetMax = new Vector2(-8, 0);
        PaintTubeAmountView tube = view.amountTube;
        tube.tube.artwork = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "MetalTube.png");
        Local(tube.tube.rectTransform, -36, -7, 510, 178);
        Image percentTape = NewImage("Percent Tape", amountPanel.transform, paperPatch);
        Local(percentTape.rectTransform, 248, 0, 112, 73);
        percentTape.rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
        tube.remainingLabel.transform.SetParent(percentTape.transform, false);
        tube.remainingLabel.font = font; tube.remainingLabel.fontSize = 37; tube.remainingLabel.color = Ink;
        Stretch(tube.remainingLabel.rectTransform);

        RectTransform oldStatus = root.Find("Status Panel") as RectTransform;
        TMP_Text status = oldStatus.Find("Status Text").GetComponent<TMP_Text>();
        status.transform.SetParent(root, false); status.name = "Status Text";
        Place(status.rectTransform, 470, 1614, 680, 42);
        status.font = font; status.fontSize = 25; status.color = new Color(1, 0.93f, 0.76f); status.text = "";
        Object.DestroyImmediate(oldStatus.gameObject);
        ui.StatusLabel = status; ui.ShowPaintingHint = false;

        Image canvas = root.Find("Canvas Frame").GetComponent<Image>();
        SetImage(canvas, canvasArt); Place(canvas.rectTransform, 476, 776, 808, 900);
        // The reference's softly uneven edge is decorative; the actual paint region stays rectangular.
        view.paintRegion.anchorMin = new Vector2(20f / 808, 24f / 900);
        view.paintRegion.anchorMax = Vector2.one - view.paintRegion.anchorMin;
        view.paintRegion.offsetMin = view.paintRegion.offsetMax = Vector2.zero;

        Image palette = root.Find("Palette Panel").GetComponent<Image>();
        SetImage(palette, plank); Place(palette.rectTransform, 470, 1340, 883, 184);
        Color[] colors = { new Color(0.96f, 0.10f, 0.08f), new Color(1, 0.81f, 0.01f), new Color(0.02f, 0.64f, 0.30f),
            new Color(0.94f, 0.01f, 0.64f), new Color(0.02f, 0.15f, 0.94f), new Color(1, 0.29f, 0.01f) };
        for (int i = 0; i < view.paletteButtons.Length; i++)
        {
            Image image = view.paletteButtons[i].GetComponent<Image>();
            SetImage(image, dab); image.color = colors[i]; image.raycastTarget = true; image.preserveAspect = true;
            Local(image.rectTransform, -352 + i * 143, 0, 118, 121);
        }
        // Preserve pigment parameters; visual tints can be edited independently in Inspector.
        Image oldMark = view.selectedMark.GetComponent<Image>();
        Object.DestroyImmediate(oldMark);
        var mark = view.selectedMark.gameObject.AddComponent<PaletteSelectionGraphic>();
        mark.color = new Color(1, 0.94f, 0.78f); mark.raycastTarget = false;
        Local(view.selectedMark, -352, 0, 128, 137);

        Image done = ui.DoneButton.GetComponent<Image>();
        SetImage(done, doneArt); done.raycastTarget = true;
        Place(done.rectTransform, 468, 1517, 390, 123);
        TMP_Text doneLabel = done.GetComponentInChildren<TMP_Text>();
        doneLabel.font = font; doneLabel.fontSize = 52; doneLabel.color = new Color(1, 0.94f, 0.77f);
        ui.UndoButton = NewButton("Undo Button", root, undoArt, 130, 1504, 149, 126);
        ui.ClearButton = NewButton("Clear Button", root, trashArt, 818, 1505, 137, 131);
        PaintHistory history = ui.gameObject.AddComponent<PaintHistory>();
        history.surface = view.paper.GetComponent<ImpastoPaintSurface>(); history.amount = view.paintAmount;
        history.spawner = ui.RaycastAndSpawn; ui.RaycastAndSpawn.history = history; ui.PaintHistory = history;
        view.tabletopLayout = true;
        PolishArtwork(view);
        view.RefreshLayout();
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        EditorSceneManager.SaveScene(view.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("TABLETOP SAVED: reference layout, editable artwork, live metallic tube and undo/clear buttons.");
    }

    public static void PolishBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Painting/Scenes/Painting.unity");
            var view = Object.FindFirstObjectByType<WorkshopVisualController>();
            PolishArtwork(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            EditorSceneManager.SaveScene(view.gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    private static void PolishArtwork(WorkshopVisualController view)
    {
        const string path = "Assets/Painting/Materials/PaletteGouache.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Painting/Palette Gouache"));
            AssetDatabase.CreateAsset(material, path);
        }
        float[] angles = { 0, -8, 6, -5, 9, -6 };
        for (int i = 0; i < view.paletteButtons.Length; i++)
        {
            view.paletteButtons[i].GetComponent<Image>().material = material;
            // Relative anchors already preserve screen proportions under the painting projection.
            view.paletteButtons[i].GetComponent<Image>().preserveAspect = false;
            view.paletteButtons[i].localRotation = Quaternion.Euler(0, 0, angles[i]);
        }
        foreach (string name in new[] { "TabletopPlank", "PaletteDab", "TargetPaper", "DonePlate" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + name + ".png");
            if (!importer.isReadable) continue;
            importer.isReadable = false; importer.SaveAndReimport();
        }
        ResponsiveLayout(view);
    }

    private static void ResponsiveLayout(WorkshopVisualController view)
    {
        Transform root = view.transform.Find("Workshop");
        Relative(root.Find("Target Card/Target Artwork") as RectTransform, 0, -13, 198, 239, 263, 323);
        Relative(view.paintAmount.transform.Find("Title Tape") as RectTransform, -5, 78, 272, 51, 610, 184);
        Relative(view.amountTube.tube.rectTransform, -36, -3, 510, 178, 610, 184);
        Relative(view.paintAmount.transform.Find("Percent Tape") as RectTransform, 248, 0, 112, 73, 610, 184);
        for (int i = 0; i < view.paletteButtons.Length; i++)
            Relative(view.paletteButtons[i], -352 + i * 143, 0, 108, 118, 883, 184);
        Relative(view.selectedMark, -352, 0, 128, 137, 883, 184);
        ScaleText(view, view.paintAmount.transform.Find("Title Tape/Title").GetComponent<TMP_Text>(), 34);
        ScaleText(view, view.amountTube.remainingLabel, 37);
        ScaleText(view, root.Find("Status Text").GetComponent<TMP_Text>(), 25);
        ScaleText(view, root.Find("Done Button/Button Label").GetComponent<TMP_Text>(), 52);
    }

    private static void ScaleText(WorkshopVisualController view, TMP_Text label, float size)
    {
        WorkshopTextScale scale = label.GetComponent<WorkshopTextScale>();
        if (scale == null) scale = label.gameObject.AddComponent<WorkshopTextScale>();
        scale.referenceRoot = view.transform as RectTransform; scale.label = label; scale.fontSizeAt941 = size; scale.Refresh();
    }

    private static void Relative(RectTransform rect, float x, float y, float width, float height, float parentWidth, float parentHeight)
    {
        Vector2 center = new Vector2(0.5f + x / parentWidth, 0.5f + y / parentHeight);
        Vector2 half = new Vector2(width / parentWidth, height / parentHeight) * 0.5f;
        rect.anchorMin = center - half; rect.anchorMax = center + half; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Import(string name, int maxSize)
    {
        string path = Art + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.isReadable = name == "TabletopPlank" || name == "PaletteDab" || name == "TargetPaper" || name == "DonePlate";
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static Sprite Trimmed(string name)
    {
        string path = Art + name + "Trimmed.asset";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + name + ".png");
        Color32[] pixels = texture.GetPixels32();
        int x0 = texture.width, y0 = texture.height, x1 = 0, y1 = 0;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < 64) continue;
            x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
        }
        Sprite sprite = Sprite.Create(texture, new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = name; AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    private static Sprite Reference(string name, Rect rect)
    {
        string path = Art + name + ".asset";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "TabletopReference.png");
        sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        sprite.name = name; AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    private static TMP_FontAsset CreateFont()
    {
        const string path = "Assets/Painting/Art/Fonts/Kalam SDF.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Painting/Art/Fonts/Kalam-Regular.ttf");
        font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Kalam SDF";
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789%:/.,!? -ÇĞİÖŞÜçğıöşü", out string missing);
        if (!string.IsNullOrEmpty(missing)) throw new Exception("Font is missing: " + missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font, path);
        foreach (Texture2D texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        AssetDatabase.AddObjectToAsset(font.material, font);
        return font;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        image.sprite = sprite; image.color = Color.white; image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }
    private static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI"); obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>(); SetImage(image, sprite); image.raycastTarget = false; return image;
    }
    private static Button NewButton(string name, Transform parent, Sprite sprite, float x, float y, float width, float height)
    {
        Image image = NewImage(name, parent, sprite); image.raycastTarget = true;
        Place(image.rectTransform, x, y, width, height);
        Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.disabledColor = Color.white; button.colors = colors;
        return button;
    }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        Vector2 center = new Vector2(x / 941, 1 - y / 1672), half = new Vector2(width / 941, height / 1672) * 0.5f;
        rect.anchorMin = center - half; rect.anchorMax = center + half; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Local(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
