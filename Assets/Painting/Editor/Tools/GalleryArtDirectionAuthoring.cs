using System;
using System.Linq;
using PaintingGame.Atelier;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class GalleryArtDirectionAuthoring
{
    private const string Data="Assets/Painting/Settings/Gallery/";
    private const string Art="Assets/Painting/Art/Gallery/";
    private static GalleryUITheme theme;
    public static void ApplyBatch()
    {try{Apply();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    [MenuItem("Tools/Painting/Apply Gallery Oil & Artisan UI")]
    public static void Apply()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(GalleryGameAuthoring.ScenePath);
        var game=Object.FindFirstObjectByType<GalleryGameController>();
        var settings=Asset<SettingsContain>(Data+"GalleryOilPaint.asset");
        if(game.tools.surface.settings!=settings)EditorUtility.CopySerialized(game.tools.surface.settings,settings);
        settings.PaintVolumePerUnit=.16f;settings.FilmThickness=.065f;settings.ReliefHeight=.16f;
        settings.WetGloss=.62f;settings.ContactShadow=.42f;settings.ColorRibbons=.90f;
        settings.SurfaceVariation=.48f;settings.SculptedOilLighting=1;
        game.tools.surface.settings=settings;EditorUtility.SetDirty(settings);
        for(int i=0;i<game.rules.painting.tools.Count;i++)
        {
            var source=game.rules.painting.tools[i];string path=Data+"Oil_"+source.id+".asset";
            var copy=AssetDatabase.LoadAssetAtPath<PaintToolDefinition>(path);
            if(copy==null){copy=Object.Instantiate(source);AssetDatabase.CreateAsset(copy,path);}
            if(copy is BrushToolDefinition brush)
            {brush.size=i==0?.055f:.018f;brush.bristleContrast=.35f;brush.bristles=i==0?12:6;brush.smudge=.2f;brush.release=.5f;brush.spacing=.10f;}
            if(copy is PourToolDefinition pour){pour.size=.032f;pour.unitsPerSecond=7;}
            if(copy is KnifeToolDefinition knife){knife.size=.20f;knife.filmScale=.85f;knife.pickup=.92f;}
            if(copy is PressToolDefinition press){press.size=.09f;press.holdStrength=4.8f;press.dragStrength=.6f;}
            game.rules.painting.tools[i]=copy;EditorUtility.SetDirty(copy);
        }
        game.rules.painting.defaultTool=game.rules.painting.tools[0];EditorUtility.SetDirty(game.rules.painting);
        theme=Theme();
        var canvas=game.layout.canvas;
        foreach(var label in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name=="Brand")continue;
            label.font=theme.readableFont;
            if(label.GetComponentInParent<GalleryButton>()!=null)label.fontStyle=FontStyles.Bold;
        }
        foreach(var image in canvas.GetComponentsInChildren<Image>(true))
        {
            string n=image.name;
            if(n=="Gallery Header"||n=="Status Paper"||n=="Customer Thought")Skin(image,theme.paper);
            else if(n=="Exhibition Sign")Skin(image,theme.primary);
            else if(n=="Bottom Navigation"||n=="Top Desk"||n=="Bottom Desk")Skin(image,theme.rail);
            else if(n=="Computer Monitor Bezel")Skin(image,theme.monitor);
            else if(n=="Computer Display")image.color=new Color(.97f,.94f,.86f);
            else if(n.StartsWith("Stored Artwork "))Skin(image,theme.card);
            else if(n=="Collection Paper")Skin(image,theme.paper);
        }
        foreach(var b in game.buttons)
        {
            bool small=b.action==GalleryAction.Tool||b.action==GalleryAction.Color;
            bool primary=b.action==GalleryAction.Finish||b.action==GalleryAction.Open;
            b.idleSprite=small?theme.card:primary?theme.primary:theme.secondary;
            b.selectedSprite=theme.primary;
            b.idleText=small?theme.ink:theme.ivory;b.selectedText=theme.ivory;
            Skin(b.GetComponent<Image>(),b.idleSprite);
            if(b.label!=null){b.label.color=b.idleText;b.label.font=theme.readableFont;}
            var pigment=b.transform.Find("Pigment Stripe");
            if(pigment!=null && b.action==GalleryAction.Tube)pigment.GetComponent<Image>().color=game.rules.painting.colors[b.index].pigment;
            var shadow=b.transform.parent.Find(b.name+" Shadow");if(shadow!=null)shadow.gameObject.SetActive(false);
        }
        foreach(var t in canvas.GetComponentsInChildren<Transform>(true))if(t.name.EndsWith(" Shadow"))t.gameObject.SetActive(false);
        // Keep the gesture rectangle and the physical canvas unchanged; only its surround is replaced.
        var area=game.layout.paperRegion;
        foreach(string edge in new[]{"Left Frame","Right Frame","Top Frame","Bottom Frame"})
        {var t=area.Find(edge);if(t!=null)t.GetComponent<Image>().enabled=false;}
        var existing=area.parent.Find("Artisan Canvas Frame");
        if(existing==null)
        {
            var frame=Panel("Artisan Canvas Frame",area.parent,theme.frame,540,1175,972,842);
            frame.fillCenter=false;frame.pixelsPerUnitMultiplier=2.5f;frame.transform.SetSiblingIndex(area.GetSiblingIndex()+1);
        }
        else existing.GetComponent<Image>().raycastTarget=false;
        // Paper under the desktop controls and an inset label for status replace flat colour boards.
        foreach(int page in new[]{3,4})
        {
            var title=game.pages[page].transform.Find("Computer Title").GetComponent<TMP_Text>();title.color=theme.ink;
        }
        var galleryMatte=game.pages[0].transform.Find("Gallery Matte");
        if(galleryMatte==null)
        {var matte=Panel("Gallery Matte",game.pages[0].transform,theme.rail,540,1040,1080,1500);matte.transform.SetAsFirstSibling();}
        var selected=game.selectedTube.rectTransform;Box(selected,365,1720,610,50);
        game.selectedTube.color=theme.ivory;
        var tube=(RectTransform)game.tubeGraphic.transform;Box(tube,540,1640,840,90);
        AddControls(game);
        var toolControls=game.GetComponent<GalleryToolControls>();
        RebuildSlider(toolControls.size);RebuildSlider(toolControls.pressure);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();Debug.Log("GALLERY OIL PAINT AND ARTISAN UI APPLIED");
    }
    private static GalleryUITheme Theme()
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"ArtisanUI.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ArtisanUI.png");
        var result=Asset<GalleryUITheme>(Data+"ArtisanUITheme.asset");
        var sprites=new Sprite[8];
        float[,] regions={{36,54,582,244},{640,65,588,225},{25,345,600,250},{645,342,590,258},
            {24,665,610,235},{650,630,585,260},{35,987,578,190},{642,942,585,270}};
        for(int i=0;i<8;i++)
        {
            string path=Art+"ArtisanPanel_"+i+".asset";sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            float scale=texture.width/1280f;
            Rect rect=new Rect(regions[i,0]*scale,texture.height-(regions[i,1]+regions[i,3])*scale,regions[i,2]*scale,regions[i,3]*scale);
            var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(55,42,55,42)*scale);
            sprite.name="Artisan Panel "+i;
            if(sprites[i]==null){sprites[i]=sprite;AssetDatabase.CreateAsset(sprite,path);}
            else {EditorUtility.CopySerialized(sprite,sprites[i]);Object.DestroyImmediate(sprite);EditorUtility.SetDirty(sprites[i]);}
        }
        result.paper=sprites[0];result.primary=sprites[1];result.secondary=sprites[2];result.card=sprites[3];
        result.rail=sprites[4];result.monitor=sprites[5];result.label=sprites[6];result.frame=sprites[7];
        result.readableFont=ReadableFont();EditorUtility.SetDirty(result);return result;
    }
    private static TMP_FontAsset ReadableFont()
    {
        string path=Data+"GalleryReadable SDF.asset";var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(font!=null)return font;
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/TextMeshPro/Fonts/LiberationSans.ttf");
        font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024);
        string letters=new string(Enumerable.Range(32,95).Select(i=>(char)i).ToArray())+"ÇĞİÖŞÜçğıöşü•…‘’–×";
        font.TryAddCharacters(letters);font.atlasPopulationMode=AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font,path);
        foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
        AssetDatabase.AddObjectToAsset(font.material,font);EditorUtility.SetDirty(font);return font;
    }
    private static void AddControls(GalleryGameController game)
    {
        if(game.GetComponent<GalleryToolControls>()!=null)return;
        var controls=game.gameObject.AddComponent<GalleryToolControls>();controls.game=game;
        controls.toggle=Button("ALET AYARI",game.pages[1].transform,theme.secondary,875,1720,280,80);
        var root=Rect("Tool Settings Overlay",game.pages[1].transform.parent);Stretch(root);
        var blocker=root.gameObject.AddComponent<Image>();blocker.color=new Color(.08f,.05f,.03f,.65f);blocker.raycastTarget=true;
        var panel=Panel("Tool Settings Paper",root,theme.paper,540,1110,950,720);
        Label("Title",panel.transform,"ALET AYARLARI",42,475,637,820,80);
        controls.sizeLabel=Label("Width Label",panel.transform,"",34,475,530,790,60);
        controls.size=Slider("Tool Width",panel.transform,475,445,750);
        controls.pressureLabel=Label("Pressure Label",panel.transform,"",34,475,350,790,60);
        controls.pressure=Slider("Tool Pressure",panel.transform,475,265,750);
        Label("Hint",panel.transform,"Hafif baskı: kalın iz. Güçlü baskı: ince ve uzun yayılım.",28,475,170,800,70);
        controls.close=Button("BOYAMAYA DÖN",panel.transform,theme.primary,475,76,650,100);
        controls.popup=root.gameObject;root.gameObject.SetActive(false);
    }
    private static Slider Slider(string name,Transform parent,float x,float y,float width)
    {
        var root=Panel(name,parent,theme.rail,x,y,width,75);root.raycastTarget=true;
        var slider=root.gameObject.AddComponent<Slider>();RebuildSlider(slider);return slider;
    }
    private static void RebuildSlider(Slider slider)
    {
        slider.fillRect=null;slider.handleRect=null;
        for(int i=slider.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(slider.transform.GetChild(i).gameObject);
        var fillArea=Rect("Fill Area",slider.transform);Stretch(fillArea);fillArea.offsetMin=new Vector2(25,24);fillArea.offsetMax=new Vector2(-25,-24);
        var fill=Panel("Fill",fillArea,theme.secondary,0,0,0,0);Stretch(fill.rectTransform);fill.pixelsPerUnitMultiplier=4;
        var handleArea=Rect("Handle Area",slider.transform);Stretch(handleArea);handleArea.offsetMin=new Vector2(35,0);handleArea.offsetMax=new Vector2(-35,0);
        // Slider stretches the thumb vertically to its handle area; this is extra height, not total height.
        var handle=Panel("Handle",handleArea,theme.paper,0,0,65,20);
        handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=Vector2.one*.5f;handle.rectTransform.anchoredPosition=Vector2.zero;
        slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
    }
    private static TMP_Text Label(string name,Transform parent,string text,float size,float x,float y,float w,float h)
    {
        var r=Rect(name,parent);Box(r,x,y,w,h);var label=r.gameObject.AddComponent<TextMeshProUGUI>();label.font=theme.readableFont;label.text=text;
        label.fontSize=size;label.color=theme.ink;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    private static Button Button(string text,Transform parent,Sprite sprite,float x,float y,float w,float h)
    {
        var p=Panel(text,parent,sprite,x,y,w,h);p.raycastTarget=true;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;
        var label=Label("Label",p.transform,text,30,w/2,h/2,w-50,h-16);label.color=theme.ivory;label.fontStyle=FontStyles.Bold;return b;
    }
    private static Image Panel(string name,Transform parent,Sprite sprite,float x,float y,float w,float h)
    {var r=Rect(name,parent);Box(r,x,y,w,h);var image=r.gameObject.AddComponent<Image>();Skin(image,sprite);image.raycastTarget=false;return image;}
    private static void Skin(Image image,Sprite sprite)
    {image.sprite=sprite;image.type=Image.Type.Sliced;image.color=Color.white;image.pixelsPerUnitMultiplier=2;}
    private static RectTransform Rect(string name,Transform parent)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);return r;}
    private static void Box(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    private static T Asset<T>(string path)where T:ScriptableObject
    {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a!=null)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);return a;}
}
