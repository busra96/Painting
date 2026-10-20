using System;
using System.Collections.Generic;
using System.Linq;
using PaintingGame.Atelier;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Builds editable scene objects and data assets. No tool picker objects are created during play.
public static class GalleryReferenceToolsAuthoring
{
    private const string Data="Assets/Painting/Settings/Gallery/";
    private const string Art="Assets/Painting/Art/Gallery/";
    public static void ApplyBatch()
    {
        try{EditorSceneManager.OpenScene(GalleryGameAuthoring.ScenePath);Apply();EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    [MenuItem("Tools/Painting/Apply Reference Artist Tool Set")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before applying tools.");
        var game=Object.FindFirstObjectByType<GalleryGameController>();
        if(game==null || game.gameObject.scene.path!=GalleryGameAuthoring.ScenePath)
            throw new InvalidOperationException("Open GalleryAtelier first.");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"ReferenceArtistTools.png");
        if(importer==null)throw new InvalidOperationException("ReferenceArtistTools.png is missing.");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.isReadable=true;importer.mipmapEnabled=false;
        importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"ReferenceArtistTools.png");
        var icons=new Sprite[10];var cursors=new Sprite[10];var tips=new Sprite[10];
        for(int i=0;i<10;i++)
        {
            Rect rect=FindTool(texture,i);
            icons[i]=SpriteAsset("ReferenceTool_"+i+"_Icon",texture,rect,Vector2.one*.5f);
            Rect contact=rect;contact.y+=rect.height*.43f;contact.height=rect.height*.57f;
            float tipX=FindTipX(texture,rect);
            cursors[i]=SpriteAsset("ReferenceTool_"+i+"_Cursor",texture,contact,new Vector2(tipX,.97f));
            if(i!=4)
            {
                Rect tip=rect;tip.height=rect.height*(i<5?.17f:.26f);tip.y=rect.yMax-tip.height;
                float contactY=contact.y+contact.height*.97f;
                tips[i]=SpriteAsset("ReferenceTool_"+i+"_Paint",texture,tip,new Vector2(tipX,(contactY-tip.y)/tip.height));
            }
        }
        var tools=new List<PaintToolDefinition>();
        tools.Add(Brush("brush-wide","Düz 12",.05f,.32f,.92f,12,icons[0],cursors[0]));
        tools.Add(Brush("brush-medium","Düz 6",.028f,.38f,.88f,9,icons[1],cursors[1]));
        tools.Add(Brush("brush-thin","Yuvarlak",.014f,.9f,.08f,5,icons[2],cursors[2]));
        tools.Add(Brush("brush-liner","İnce Uç",.007f,1.1f,0,3,icons[3],cursors[3]));
        var pencil=Asset<PencilToolDefinition>("Oil_pencil");
        Set(pencil,"pencil","Eskiz",.0055f,icons[4],cursors[4],1);
        pencil.hint="İnce grafit eskizi çiz. Tüp boyası tüketmez.";pencil.graphitePerCanvasWidth=.06f;
        tools.Add(pencil);EditorUtility.SetDirty(pencil);
        tools.Add(Knife("knife","Damla",.10f,1.1f,.8f,0,.12f,icons[5],cursors[5]));
        tools.Add(Knife("knife-wide","Geniş Palet",.18f,.95f,.08f,.32f,.04f,icons[6],cursors[6]));
        tools.Add(Knife("knife-oval","Oval Palet",.10f,1.35f,.62f,0,.18f,icons[7],cursors[7]));
        tools.Add(Knife("knife-straight","Dar Palet",.065f,1.45f,.08f,0,.025f,icons[8],cursors[8]));
        tools.Add(Knife("knife-leaf","Sivri Palet",.05f,1.6f,.95f,0,.16f,icons[9],cursors[9]));
        var pour=AssetDatabase.LoadAssetAtPath<PourToolDefinition>(Data+"Oil_pour.asset");tools.Add(pour);
        for(int i=0;i<10;i++){tools[i].cursorTip=tips[i];EditorUtility.SetDirty(tools[i]);}
        game.rules.painting.tools=tools;game.rules.painting.defaultTool=tools[0];EditorUtility.SetDirty(game.rules.painting);
        BuildRack(game,tools);
        // Pixel access is needed only to author Sprite bounds, not in the mobile player.
        importer.isReadable=false;importer.SaveAndReimport();
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        AssetDatabase.SaveAssets();Debug.Log("REFERENCE ARTIST TOOLS APPLIED: four brushes, pencil, five knives, tube; press removed.");
    }
    private static BrushToolDefinition Brush(string id,string name,float size,float length,float square,int bristles,Sprite icon,Sprite cursor)
    {
        var b=Asset<BrushToolDefinition>("Oil_"+id);Set(b,id,name,size,icon,cursor,1);
        b.lengthRatio=length;b.squareness=square;b.bristles=bristles;b.bristleContrast=.24f;
        b.spacing=.10f;b.smudge=.2f;b.release=.5f;b.unitsPerCanvasWidth=id=="brush-wide"?6:id=="brush-medium"?3.6f:id=="brush-thin"?1.8f:.7f;
        b.unitsPerSecondStill=.25f;b.hint="Seçili tüpten boya alarak "+name.ToLowerInvariant()+" fırçayla boya.";
        EditorUtility.SetDirty(b);return b;
    }
    private static KnifeToolDefinition Knife(string id,string name,float size,float length,float taper,float asymmetry,float curve,Sprite icon,Sprite cursor)
    {
        var k=Asset<KnifeToolDefinition>("Oil_"+id);Set(k,id,name,size,icon,cursor,.5f);
        k.bladeLength=length;k.tipTaper=taper;k.asymmetry=asymmetry;k.edgeCurve=curve;k.filmScale=.85f;k.pickup=.92f;
        k.hint="Tuvaldeki boyayı "+name.ToLowerInvariant()+" bıçakla taşı. Yeni boya tüketmez.";
        EditorUtility.SetDirty(k);return k;
    }
    private static void Set(PaintToolDefinition t,string id,string name,float size,Sprite icon,Sprite cursor,float scale)
    {t.id=id;t.displayName=name;t.size=size;t.icon=icon;t.cursor=cursor;t.cursorTip=null;t.cursorScale=scale;t.cursorFollowsStroke=true;}
    private static T Asset<T>(string name)where T:ScriptableObject
    {
        string path=Data+name+".asset";var a=AssetDatabase.LoadAssetAtPath<T>(path);
        if(a==null){a=ScriptableObject.CreateInstance<T>();a.name=name;AssetDatabase.CreateAsset(a,path);}return a;
    }
    private static Rect FindTool(Texture2D texture,int index)
    {
        int cw=texture.width/5,ch=texture.height/2,ox=index%5*cw,oy=(1-index/5)*ch;
        int minX=ox+cw,minY=oy+ch,maxX=ox,maxY=oy;
        // Alpha bounds inside a known atlas cell. The bitmap's generated alpha is preserved.
        for(int y=oy;y<oy+ch;y++)for(int x=ox;x<ox+cw;x++)
        {
            if(texture.GetPixel(x,y).a<.5f)continue;
            minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);
        }
        if(maxX<=minX || maxY<=minY)throw new InvalidOperationException("Empty tool atlas cell "+index);
        return UnityEngine.Rect.MinMaxRect(Mathf.Max(ox,minX-2),Mathf.Max(oy,minY-2),Mathf.Min(ox+cw,maxX+3),Mathf.Min(oy+ch,maxY+3));
    }
    private static Sprite SpriteAsset(string name,Texture2D texture,Rect rect,Vector2 pivot)
    {
        string path=Art+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var s=Sprite.Create(texture,rect,pivot,100,0,SpriteMeshType.FullRect);s.name=name;
        if(existing==null){AssetDatabase.CreateAsset(s,path);return s;}
        EditorUtility.CopySerialized(s,existing);Object.DestroyImmediate(s);EditorUtility.SetDirty(existing);return existing;
    }
    private static float FindTipX(Texture2D texture,Rect rect)
    {
        double sum=0;int count=0;
        int y0=Mathf.Max((int)rect.y,Mathf.FloorToInt(rect.yMax-rect.height*.04f));
        for(int y=y0;y<(int)rect.yMax;y++)for(int x=(int)rect.x;x<(int)rect.xMax;x++)
            if(texture.GetPixel(x,y).a>.5f){sum+=x+.5f;count++;}
        return count>0?Mathf.Clamp01(((float)(sum/count)-rect.x)/rect.width):.5f;
    }
    private static void BuildRack(GalleryGameController game,List<PaintToolDefinition> tools)
    {
        var parent=game.pages[1].transform;var old=game.buttons.First(b=>b.action==GalleryAction.Tool);
        var template=Object.Instantiate(old.gameObject);template.name="Tool Button Template";template.SetActive(false);
        foreach(var b in parent.GetComponentsInChildren<GalleryButton>(true).Where(b=>b.action==GalleryAction.Tool).ToArray())
        {
            var shadow=parent.Find(b.name+" Shadow");if(shadow!=null)Object.DestroyImmediate(shadow.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }
        foreach(string name in new[]{"Artist Tool Rack","Tool Rack Hint"}){var t=parent.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        var rack=Rect("Artist Tool Rack",parent);Box(rack,540,550,1040,160);
        var scroll=rack.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;
        scroll.movementType=ScrollRect.MovementType.Clamped;scroll.inertia=true;scroll.decelerationRate=.08f;
        var viewport=Rect("Viewport",rack);Stretch(viewport);viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;hit.canvasRenderer.cullTransparentMesh=false;
        var content=Rect("Editable Tool Buttons",viewport);content.anchorMin=new Vector2(0,0);content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,.5f);
        content.sizeDelta=new Vector2(tools.Count*176,0);content.anchoredPosition=Vector2.zero;
        scroll.viewport=viewport;scroll.content=content;
        var theme=AssetDatabase.LoadAssetAtPath<GalleryUITheme>(Data+"ArtisanUITheme.asset");
        for(int i=0;i<tools.Count;i++)
        {
            var go=Object.Instantiate(template,content);go.name=tools[i].displayName+" Tool";go.SetActive(true);
            var b=go.GetComponent<GalleryButton>();b.game=game;b.action=GalleryAction.Tool;b.index=i;
            var r=(RectTransform)go.transform;Box(r,88+i*176,80,166,154);
            b.idleSprite=theme.card;b.selectedSprite=theme.primary;b.idleText=theme.ink;b.selectedText=theme.ivory;
            b.label.text=tools[i].displayName;b.label.fontSize=23;b.label.enableAutoSizing=false;Box(b.label.rectTransform,83,24,158,42);
            var icon=go.transform.Find("Tool Illustration").GetComponent<Image>();icon.sprite=tools[i].icon;icon.preserveAspect=true;
            Box(icon.rectTransform,83,99,145,100);icon.raycastTarget=false;
            if(b.selection!=null)
            {
                b.selection.sprite=null;b.selection.type=Image.Type.Simple;b.selection.color=new Color(.8f,.64f,.3f,.9f);
                Box(b.selection.rectTransform,83,6,150,5);b.selection.raycastTarget=false;
            }
        }
        Object.DestroyImmediate(template);
        var hint=Rect("Tool Rack Hint",parent);Box(hint,540,456,1000,24);
        var label=hint.gameObject.AddComponent<TextMeshProUGUI>();label.font=theme.readableFont;label.text="FIRÇALAR  •  PALET BIÇAKLARI  →  YANA KAYDIR";
        label.fontSize=20;label.color=theme.ivory;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        game.buttons=game.layout.canvas.GetComponentsInChildren<GalleryButton>(true);
    }
    private static RectTransform Rect(string name,Transform parent)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);return r;}
    private static void Box(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static void Stretch(RectTransform r)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
}
