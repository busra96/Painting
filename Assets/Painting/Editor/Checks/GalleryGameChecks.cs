using System;
using System.IO;
using PaintingGame.Atelier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GalleryGameChecks
{
    private const string Key = "Painting.GalleryGameChecks";
    private static double deadline;
    private static int passed;
    private static int readyFrame = -1;
    static GalleryGameChecks() { if (SessionState.GetBool(Key,false)) EditorApplication.playModeStateChanged += State; }
    public static void RunBatch()
    {
        try
        {
            var folder = Path.GetFullPath("Logs/GalleryChecks/" + Guid.NewGuid().ToString("N"));
            SessionState.SetString(Key + ".Folder",folder); Directory.CreateDirectory(folder);
            var scene = EditorSceneManager.OpenScene(GalleryGameAuthoring.ScenePath);
            var game = Object.FindFirstObjectByType<GalleryGameController>();
            Check(game != null && game.pages.Length == 5, "Authored gallery, workshop, storage, shop and advertising pages exist");
            int missing=0;
            foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true))
                missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            Check(missing==0,"All authored scene objects have valid script references");
            Economy(game.rules,Path.Combine(folder,"Economy"));
            game.storageFolderOverride = Path.Combine(folder,"Runtime");
            SessionState.SetInt(Key + ".Count",passed);
            SessionState.SetBool(Key,true); EditorApplication.playModeStateChanged -= State; EditorApplication.playModeStateChanged += State;
            EditorApplication.isPlaying = true;
        }
        catch(Exception e) { Finish(e); }
    }
    private static void Economy(GalleryRules rules,string folder)
    {
        var tubes = new TubeInventory(rules.painting.colors,null);
        var store = new GalleryStore(folder,rules,tubes);
        Check(store.StoredCount == 0 && !store.IsOpen && store.Data.coins == rules.startingCoins, "Fresh profile starts outside an exhibition");
        int coins = store.Data.coins;
        for(int i=0;i<20;i++)store.Tick();
        Check(store.Data.coins == coins && store.Data.sold == 0, "Closed gallery cannot make automatic sales");
        Check(!store.OpenExhibition(), "Empty exhibition is blocked");
        var sample = new Texture2D(8,8,TextureFormat.RGB24,false); var png = sample.EncodeToPNG(); Object.DestroyImmediate(sample);
        Check(!store.CompleteArtwork(png,.5f,Color.red,2), "Finishing requires an owned canvas");
        for(int i=0;i<rules.exhibitionSize;i++)
        {
            Check(store.BeginCanvas(), "Canvas stock can start a painting"); int stock=store.Data.canvases;
            Check(store.BeginCanvas() && store.Data.canvases == stock, "Repeated paint gestures do not consume another canvas");
            Check(!store.CompleteArtwork(png,0,Color.red,2), "Blank painting cannot be farmed for money");
            tubes.Spend(rules.painting.colors[0],.5f);
            Check(store.CompleteArtwork(png,.35f,rules.painting.colors[i % rules.painting.colors.Count].pigment,2), "Finished painting enters storage");
            if(i < rules.exhibitionSize-1)Check(!store.OpenExhibition(), "Incomplete collection cannot start exhibition");
        }
        Check(store.Data.coins == coins && store.StoredCount == rules.exhibitionSize, "Fifteen finished paintings earn zero coins until an actual sale");
        float expectedTube = tubes.Level(rules.painting.colors[0]);
        var restored = new GalleryStore(folder,rules,new TubeInventory(rules.painting.colors,null));
        Check(Mathf.Approximately(restored.Tubes.Level(rules.painting.colors[0]),expectedTube), "Each tube's depletion persists by stable color ID");
        Check(store.Advertise(1) && !store.Advertise(2), "One paid campaign per exhibition; no duplicate charge");
        Check(store.OpenExhibition() && !store.OpenExhibition(), "A full collection opens exactly once");
        Check(store.Count(ArtworkStatus.Exhibited)==rules.exhibitionSize && store.StoredCount==0,"Opening moves exactly the required stock onto display");
        Check(!store.Advertise(2),"An active exhibition cannot swap its paid campaign");
        float checkpointTube=tubes.Level(rules.painting.colors[0]);
        store.BeginCanvas();tubes.Spend(rules.painting.colors[0],1);
        store.Tick();store.Tick();
        var duringStroke=new GalleryStore(folder,rules,new TubeInventory(rules.painting.colors,null));
        Check(Mathf.Approximately(duringStroke.Tubes.Level(rules.painting.colors[0]),checkpointTube),"A background sale preserves checkpointed tubes instead of saving unfinished stroke depletion beside an older draft");
        for(int i=0;i<12;i++)store.Tick();
        restored = new GalleryStore(folder,rules,new TubeInventory(rules.painting.colors,null));
        Check(restored.Data.coins==store.Data.coins && restored.Data.visitorIndex==store.Data.visitorIndex,"Reload resumes the last committed visitor and money");
        for(int i=0;i<rules.exhibitionSeconds+1;i++){store.Tick(); restored.Tick();}
        Check(!store.IsOpen && store.Count(ArtworkStatus.Exhibited)==0,"Exhibition has a definite ending");
        Check(store.Data.sold>0 && store.Data.revenue==store.Data.coins-coins+rules.advertisingPrice,"Wallet equals actual sales minus paid advertising");
        Check(restored.Data.coins==store.Data.coins && restored.Data.sold==store.Data.sold,"Reloading cannot reroll visitors or duplicate sales");
        Check(store.UnsoldCount+store.Data.sold==rules.exhibitionSize,"Every exhibited artwork is sold or returned unsold");
        Check(store.Data.campaign==0 && !store.OpenExhibition(),"Consumed campaigns and unsold artworks cannot open another exhibition");
        int before=store.Data.coins;
        Check(store.DiscardUnsold() && store.Data.coins==before && store.UnsoldCount==0,"Discarding unsold stock pays nothing");
        var color=rules.painting.colors[0]; store.Tubes.Spend(color,20);
        before=store.Data.coins;
        Check(store.BuyTube(color) && store.Data.coins==before-color.price && store.Tubes.Fraction(color)>.999f,"Computer purchases refill only the paid color");
        before=store.Data.coins;Check(!store.BuyTube(color) && store.Data.coins==before,"Full tubes cannot charge again");
        if(store.Data.coins>=rules.ExpansionCost(store.Data.expansion))Check(store.Expand() && store.Data.expansion==1,"Expansion upgrades actual visitor throughput");
        store.Data.coins=rules.canvasPackPrice;store.Data.canvases=0;store.Save();
        Check(store.BuyCanvases() && store.Data.canvases==rules.canvasesPerPack && store.Data.coins==0,"Last production coins can buy the next canvas pack");
        Check(!store.Decorate() && store.Data.coins==0,"Unaffordable upgrades do not spend money");
        string blocked=Path.Combine(folder,"blocked");File.WriteAllText(blocked,"not a directory");
        var broken=new GalleryStore(blocked,rules,new TubeInventory(rules.painting.colors,null));
        int original=broken.Data.canvases;
        Check(!broken.BeginCanvas() && broken.Data.canvases==original && !broken.Data.activeCanvas,"Failed disk writes roll back production and wallet changes");
    }
    private static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
        passed=SessionState.GetInt(Key+".Count",0); deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        try
        {
            var game=Object.FindFirstObjectByType<GalleryGameController>();
            if(game==null || !game.Ready){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Gallery initialization timeout");return;}
            if(readyFrame<0){game.SetPage(1);readyFrame=Time.frameCount;return;}
            if(Time.frameCount<=readyFrame+1)return;
            EditorApplication.update-=Tick;
            Check(game.Store.Folder.Contains("Logs"),"Runtime tests use an isolated save folder");
            Runtime(game); Finish(null);
        }
        catch(Exception e){Finish(e);}
    }
    private static void Runtime(GalleryGameController game)
    {
        var tools=game.tools;var surface=tools.surface;
        Check(surface.Width<=320 && surface.Height<330,"Mobile canvas bounds simulation to about 90k cells");
        Check(game.rules.customers.Length==6 && game.customers.Length==6,"Six customer identities have reusable scene views");
        foreach(var c in game.rules.customers)Check(c.portrait!=null && c.animationController!=null,"Customer has sprite and Unity Animator: "+c.displayName);
        game.SetPage(1);Canvas.ForceUpdateCanvases();game.layout.RefreshLayout();
        var area=game.layout.paperRegion;var input=area.GetComponent<GalleryPaintInput>();
        var center=surface.transform.position;
        Vector2 screen=tools.mainCamera.WorldToScreenPoint(center);
        var pointer=new PointerEventData(EventSystem.current){position=screen,pointerId=-1,button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Debug.Log("GALLERY POINTER: screen="+screen+" dimensions="+Screen.width+"x"+Screen.height+" canvas="+game.layout.canvas.pixelRect+" region="+area.rect+" hits="+hits.Count+" first="+(hits.Count>0?hits[0].gameObject.name:"none"));
        Preview(game,"Input-Debug",1,720,1280);
        Check(hits.Count>0 && hits[0].gameObject==area.gameObject,"Canvas gesture area receives the actual EventSystem raycast");
        float before=tools.Inventory.Level(tools.CurrentColor);
        ExecuteEvents.Execute(area.gameObject,pointer,ExecuteEvents.pointerDownHandler);
        pointer.position=tools.mainCamera.WorldToScreenPoint(center+new Vector3(1,.4f,0));
        ExecuteEvents.Execute(area.gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(area.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        Check(surface.PaperVolume>0 && tools.Inventory.Level(tools.CurrentColor)<before,"Mouse tap/drag really paints and spends the selected tube");
        double added=surface.MeasureVolume();
        Check(tools.Undo() && surface.MeasureVolume()<added*.001 && Mathf.Approximately(tools.Inventory.Level(tools.CurrentColor),before),"Undo restores both the full stroke and its tube paint");
        // A second touch must still be accepted after ending the first gesture.
        pointer.pointerId=12;pointer.position=screen;ExecuteEvents.Execute(area.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(area.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        Check(surface.PaperVolume>0,"Touch input accepts another gesture after the previous one ends");
        tools.ClearCanvas();
        Vector3 mark=center+new Vector3(1.4f,-1.2f,0);
        surface.Deposit(mark,Color.red,3,Vector2.zero);surface.Flush();
        if(tools.cursor!=null)tools.cursor.HideNow();
        var capture=AtelierArtworkCapture.Capture(tools.mainCamera,game.paper);
        var bounds=game.paper.bounds;
        int px=Mathf.RoundToInt((mark.x-bounds.min.x)/bounds.size.x*capture.width);
        int py=Mathf.RoundToInt((mark.y-bounds.min.y)/bounds.size.y*capture.height);
        Color pixel=capture.GetPixel(px,py);
        Check(pixel.r>pixel.g*1.5f && pixel.r>pixel.b*1.5f,"Rendered paint lands at the same world point as the finger on a cropped canvas sprite");
        int minX=capture.width,maxX=0,minY=capture.height,maxY=0;
        for(int y=0;y<capture.height;y++)for(int x=0;x<capture.width;x++){var p=capture.GetPixel(x,y);if(p.r>p.g*1.5f && p.r>p.b*1.5f){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}}
        Debug.Log("GALLERY ROUND DEPOSIT: "+(maxX-minX)+"x"+(maxY-minY)+" sprite="+game.paper.sprite.rect+" texture="+game.paper.sprite.texture.width+"x"+game.paper.sprite.texture.height+" scale="+surface.VisualScale);
        Directory.CreateDirectory("Logs/GalleryPreview");File.WriteAllBytes("Logs/GalleryPreview/Round-Deposit.png",capture.EncodeToPNG());
        Check(Math.Abs((maxX-minX)-(maxY-minY))<5,"A round deposit stays round in the rendered artwork, without UV stretching");
        Object.DestroyImmediate(capture);tools.ClearCanvas();
        surface.Deposit(center,Color.red,6,Vector2.zero);double mass=surface.MeasureVolume();Vector3 pigment=surface.MeasurePigmentMass();
        var knife=surface.BeginKnife(null,.25f*surface.CanvasWidth);
        surface.KnifeSweep(center,center+Vector3.right*1.5f,.25f*surface.CanvasWidth,1,.85f,knife);surface.EndKnife(knife,.25f*surface.CanvasWidth);
        Check(Math.Abs(surface.MeasureVolume()-mass)<mass*.003 && (surface.MeasurePigmentMass()-pigment).magnitude<pigment.magnitude*.006f,"Horizontal spatula movement conserves paint and pigment");
        surface.Press(center,.12f*surface.CanvasWidth,.5f,.18f,3);
        Check(Math.Abs(surface.MeasureVolume()-mass)<mass*.003,"Pressing anywhere redistributes paint without manufacturing it");
        ValidateSculpting(game);
        tools.ClearCanvas();
        var red=game.rules.painting.colors[0];tools.SelectColor(red);tools.SelectTool(game.rules.painting.tools[0]);
        DrawLandscape(game);
        surface.Flush();
        Check(surface.PaperVolume>0 && surface.CoveredArea()>0,"All brush strokes leave visible oily paint on the physical canvas");
        game.SaveDraft();var store=new GalleryStore(game.Store.Folder,game.rules,new TubeInventory(game.rules.painting.colors,null));
        Check(store.DraftPath!=null && File.Exists(store.DraftPath),"Physical paint draft and per-tube inventory checkpoint together");
        string firstDraft=store.DraftPath;
        Check(game.Store.Checkpoint(surface) && File.Exists(firstDraft),"A new checkpoint retains the draft referenced by the backup profile");
        string recovery=Path.Combine(game.Store.Folder,"Recovery");Directory.CreateDirectory(recovery);
        foreach(string file in Directory.EnumerateFiles(game.Store.Folder))File.Copy(file,Path.Combine(recovery,Path.GetFileName(file)),true);
        File.WriteAllText(Path.Combine(recovery,"gallery.json"),"{ broken");
        var recovered=new GalleryStore(recovery,game.rules,new TubeInventory(game.rules.painting.colors,null));
        Check(recovered.DraftPath!=null && File.Exists(recovered.DraftPath),"Corrupt primary JSON recovers a backup with its physical paint snapshot still available");
        double savedVolume=surface.MeasureVolume();tools.ClearCanvas();
        Check(surface.ReadArtistDraft(store.DraftPath) && Math.Abs(surface.MeasureVolume()-savedVolume)<savedVolume*.002,"Reload restores the actual pigment field without losing the painting");
        tools.History.Clear();
        Stroke(game,0,new[]{new Vector2(-2.3f,-1.6f),new Vector2(-2.1f,-1.3f)},1);
        int walletBefore=game.Store.Data.coins;float other=tools.Inventory.Level(game.rules.painting.colors[1]);
        game.Act(GalleryAction.Tube,0);
        Check(game.Store.Data.coins==walletBefore-game.rules.painting.colors[0].price && tools.Inventory.Fraction(game.rules.painting.colors[0])>.999f &&
            Mathf.Approximately(other,tools.Inventory.Level(game.rules.painting.colors[1])) && !tools.History.CanUndo,"Paid tube replacement preserves other tubes and closes pre-purchase undo history");
        var dab=new ImpastoPaintSurface.BrushDab{position=center,radius=game.rules.painting.tools[0].WorldSize(surface),lengthRatio=.4f,bristles=22,bristleContrast=.6f,direction=Vector2.right,order=surface.NewLayerOrder()};
        var load=new ImpastoPaintSurface.BrushLoad();surface.Brush(dab,Color.red,.02f,load);surface.ReleaseBrush(center,load,dab.order);
        var watch=System.Diagnostics.Stopwatch.StartNew();long start=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<30;i++){surface.Brush(dab,Color.red,.02f,load);surface.ReleaseBrush(center,load,dab.order);surface.Press(center,.25f,.1f,.18f,3);}
        long allocation=GC.GetAllocatedBytesForCurrentThread()-start;watch.Stop();
        Check(allocation==0,"Steady-state brush and press kernels allocate 0 managed bytes");
        Debug.Log("GALLERY PERFORMANCE: brush+press "+(watch.Elapsed.TotalMilliseconds/30).ToString("F3")+" ms per pair; "+allocation+" B (desktop Editor)");
        Preview(game,"Workshop",1,720,1280);
        Check(game.FinishArtwork() && game.Store.StoredCount==1,"Actual rendered painting can finish into persistent gallery stock");
        Preview(game,"Collection",2,720,1280);Preview(game,"Computer-Shop",3,720,1280);Preview(game,"Computer-Ads",4,720,1280);
        var art=game.Store.Data.artworks[0];
        for(int i=1;i<game.rules.exhibitionSize;i++){game.Store.BeginCanvas();game.Store.CompleteArtwork(File.ReadAllBytes(game.Store.ImagePath(art.id)),.35f,game.rules.painting.colors[i%6].pigment,2);}
        game.Act(GalleryAction.Open,0);Check(game.Store.IsOpen && game.CurrentPage==0,"Exhibition UI action starts the timed gallery flow");game.Store.Tick();game.Store.Tick();
        Preview(game,"Gallery",0,720,1280);
        foreach(var size in new[]{new Vector2Int(720,1560),new Vector2Int(768,1024)})Preview(game,"Gallery-"+size.x+"x"+size.y,0,size.x,size.y);
        Check(EditorBuildSettings.scenes[0].path.EndsWith("Painting.unity") && EditorBuildSettings.scenes.AnyScene(GalleryGameAuthoring.ScenePath),"Original target mode and new gallery both remain in build settings");
    }
    private static void ValidateSculpting(GalleryGameController game)
    {
        var s=game.tools.surface;var tools=game.tools;Vector3 c=s.transform.position;
        tools.ClearCanvas();s.Deposit(c,Color.red,6,Vector2.zero);
        float peak=s.SampleThickness(c);float area=s.CoveredArea();double volume=s.MeasureVolume();
        for(int i=0;i<24;i++)s.Press(c,.7f,.18f,.18f,2);
        Check(s.SampleThickness(c)<peak*.85f && s.SampleThickness(c)>peak*.02f && s.CoveredArea()>area*1.1f,
            "Press lowers a mound and expands contact without digging a hollow ring");
        Check(Math.Abs(s.MeasureVolume()-volume)<volume*.003,"Sculpting press conserves the mound's full volume");
        tools.SelectTool(game.rules.painting.tools[3]);tools.SetControls(1,.15f);
        tools.ClearCanvas();s.Deposit(c,Color.red,6,Vector2.zero);tools.BeginGesture(c);
        for(int i=0;i<10;i++)tools.ContinueGesture(c,.05f);tools.EndStroke();float gentle=s.SampleThickness(c);
        tools.ClearCanvas();s.Deposit(c,Color.red,6,Vector2.zero);tools.SetControls(1,.95f);tools.BeginGesture(c);
        for(int i=0;i<10;i++)tools.ContinueGesture(c,.05f);tools.EndStroke();
        Check(s.SampleThickness(c)<gentle*.95f,"The pressure control changes physical flattening strength");
        tools.SelectTool(game.rules.painting.tools[4]);tools.ClearCanvas();tools.SetControls(.5f,.75f);tools.BeginGesture(c);tools.EndStroke();
        float narrow=s.SampleThickness(c);double smallVolume=s.MeasureVolume();
        tools.ClearCanvas();tools.SetControls(1.4f,.75f);tools.BeginGesture(c);tools.EndStroke();
        Check(narrow>s.SampleThickness(c)*1.5f && Math.Abs(s.MeasureVolume()-smallVolume)<smallVolume*.005f,
            "Equal paint in a wider footprint becomes thinner rather than creating more material");
        var controls=game.GetComponent<GalleryToolControls>();
        Check(controls!=null && controls.size!=null && controls.pressure!=null,"Editable tool width and pressure controls are authored in Canvas");
        controls.toggle.onClick.Invoke();Check(controls.popup.activeSelf && tools.InputLocked,"Tool settings own the pointer while open");
        controls.size.value=1.3f;controls.pressure.value=.25f;
        Check(Mathf.Abs(tools.SizeMultiplier-1.3f)<.001f && Mathf.Abs(tools.Pressure-.25f)<.001f,"Authored sliders change the live tool parameters");
        Canvas.ForceUpdateCanvases();
        Vector3[] outer=new Vector3[4],inner=new Vector3[4];((RectTransform)controls.size.transform).GetWorldCorners(outer);controls.size.fillRect.GetWorldCorners(inner);
        Check(inner[0].x>=outer[0].x-.01f && inner[2].x<=outer[2].x+.01f,"Slider artwork remains inside its authored track");
        controls.size.handleRect.GetWorldCorners(inner);
        Check(inner[1].y-inner[0].y<(outer[1].y-outer[0].y)*1.4f,"Slider thumb keeps its intended height without overlapping the caption");
        Preview(game,"Tool-Settings",1,720,1280);
        controls.close.onClick.Invoke();Check(!controls.popup.activeSelf && !tools.InputLocked,"Closing settings returns control to painting");
        tools.SetControls(1,.75f);tools.ClearCanvas();
        s.Deposit(c,Color.red,6,Vector2.zero);var fast=s.BeginKnife(null,1.2f);
        s.KnifeSweep(c,c+Vector3.down*1.5f,1.2f,.8f,.9f,fast);s.EndKnife(fast,1.2f);
        float fastArea=s.CoveredArea();double bladeVolume=s.MeasureVolume();
        tools.ClearCanvas();s.Deposit(c,Color.red,6,Vector2.zero);var slow=s.BeginKnife(null,1.2f);Vector3 previous=c;
        for(int i=1;i<=120;i++){var next=c+Vector3.down*(i*1.5f/120);s.KnifeSweep(previous,next,1.2f,.8f,.9f,slow);previous=next;}
        s.EndKnife(slow,1.2f);
        Check(Math.Abs(s.MeasureVolume()-bladeVolume)<bladeVolume*.003f && Mathf.Abs(s.CoveredArea()-fastArea)<fastArea*.16f,
            "Slow and fast palette-knife pulls preserve volume and produce comparable coverage");
        tools.ClearCanvas();
        // Visual regression sample: rounded oil heaps, a short blade pull, and a pressed heap.
        s.Deposit(c+new Vector3(-2,1.5f,0),Color.red,4,Vector2.zero);
        s.Deposit(c+new Vector3(0,1.5f,0),Color.yellow,7,Vector2.zero);
        s.Deposit(c+new Vector3(2,1.5f,0),Color.blue,3,Vector2.zero);
        var knife=s.BeginKnife(null,1.2f);Vector3 from=c+new Vector3(0,1.5f,0);
        for(int i=1;i<=50;i++){Vector3 to=c+new Vector3(0,1.5f-i*.05f,0);s.KnifeSweep(from,to,1.2f,.7f,.9f,knife);from=to;}
        s.EndKnife(knife,1.2f);
        for(int i=0;i<15;i++)s.Press(c+new Vector3(2,1.5f,0),.7f,.16f,.18f,2);
        s.Flush();if(tools.cursor!=null)tools.cursor.HideNow();
        Preview(game,"Oil-Sculpting",1,720,1280);
        var image=AtelierArtworkCapture.Capture(tools.mainCamera,game.paper,896);
        File.WriteAllBytes("Logs/GalleryPreview/Oil-Relief.png",image.EncodeToPNG());Object.DestroyImmediate(image);
        tools.ClearCanvas();
    }
    private static void DrawLandscape(GalleryGameController game)
    {
        for(int row=0;row<7;row++)Stroke(game,4,new[]{new Vector2(-3,2.7f-row*.44f),new Vector2(3,2.7f-row*.44f)});
        for(int row=0;row<4;row++)Stroke(game,3,new[]{new Vector2(-3,2.4f-row*.6f),new Vector2(3,2.4f-row*.6f)});
        // Hills are made with the same real tools and finite tubes that the player uses.
        for(int row=0;row<4;row++)Stroke(game,2,new[]{new Vector2(-3,.4f-row*.4f),new Vector2(-1.8f,1.1f-row*.4f),new Vector2(-.3f,.25f-row*.4f),new Vector2(1.5f,.8f-row*.4f),new Vector2(3,.1f-row*.4f)});
        for(int row=0;row<4;row++)Stroke(game,row%2==0?1:2,new[]{new Vector2(-3,-1.3f-row*.44f),new Vector2(3,-1.3f-row*.44f)});
        Stroke(game,5,new[]{new Vector2(1.1f,-2.8f),new Vector2(.1f,-1.9f),new Vector2(-.7f,-1),new Vector2(.2f,-.3f)},1);
        Stroke(game,1,new[]{new Vector2(2.05f,1.9f),new Vector2(2.05f,1.9f)},4);
        foreach(float x in new[]{-2.5f,-1.7f,1.4f,2.3f})Stroke(game,0,new[]{new Vector2(x,-2.4f),new Vector2(x+.09f,-2.1f)},1);
    }
    private static void Stroke(GalleryGameController game,int color,Vector2[] points,int tool=0)
    {
        var c=game.tools;Vector3 center=c.surface.transform.position;
        c.SelectColor(game.rules.painting.colors[color]);c.SelectTool(game.rules.painting.tools[tool]);
        Vector3 from=center+(Vector3)points[0];c.BeginGesture(from);
        for(int n=1;n<points.Length;n++)
        {
            Vector3 to=center+(Vector3)points[n];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.12f));
            for(int s=1;s<=steps;s++)c.ContinueGesture(Vector3.Lerp(from,to,s/(float)steps),.016f);
            from=to;
        }
        c.EndStroke();
    }
    private static void Preview(GalleryGameController game,string name,int page,int width,int height)
    {
        game.SetPage(page);var canvas=game.layout.canvas;var camera=game.tools.mainCamera;var scaler=canvas.GetComponent<CanvasScaler>();
        var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var target=camera.targetTexture;var active=RenderTexture.active;bool scaled=scaler.enabled;float factor=canvas.scaleFactor;int mask=camera.cullingMask;
        var render=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        var temp=new GameObject("Preview UI Camera",typeof(Camera));var ui=temp.GetComponent<Camera>();ui.enabled=false;ui.orthographic=true;ui.orthographicSize=5;ui.transform.position=new Vector3(0,0,-10);ui.cullingMask=1<<5;ui.clearFlags=CameraClearFlags.Depth;ui.targetTexture=render;
        try
        {
            camera.targetTexture=render;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=5;scaler.enabled=false;canvas.scaleFactor=Mathf.Min(width/1080f,height/1920f);
            Canvas.ForceUpdateCanvases();canvas.GetComponentInChildren<GallerySafeArea>(true).Refresh();Canvas.ForceUpdateCanvases();game.layout.RefreshLayout();camera.cullingMask&=~(1<<5);camera.Render();ui.Render();RenderTexture.active=render;
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply(false);Directory.CreateDirectory("Logs/GalleryPreview");File.WriteAllBytes("Logs/GalleryPreview/"+name+".png",image.EncodeToPNG());
        }
        finally{camera.targetTexture=target;RenderTexture.active=active;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;scaler.enabled=scaled;canvas.scaleFactor=factor;camera.cullingMask=mask;Object.DestroyImmediate(temp);RenderTexture.ReleaseTemporary(render);Object.DestroyImmediate(image);game.layout.RefreshLayout();}
    }
    private static bool AnyScene(this EditorBuildSettingsScene[] scenes,string path){foreach(var s in scenes)if(s.path==path && s.enabled)return true;return false;}
    private static void Check(bool condition,string message){if(!condition)throw new Exception("GALLERY FAIL: "+message);passed++;Debug.Log("GALLERY PASS: "+message);}
    private static void Finish(Exception exception)
    {EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;SessionState.EraseBool(Key);if(exception!=null)Debug.LogException(exception);else Debug.Log("GALLERY GAME CHECKS PASSED: "+passed);EditorApplication.Exit(exception==null?0:1);}
}
