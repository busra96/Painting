using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PaintIn2D;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public sealed class GalleryGameController : MonoBehaviour
    {
        public GalleryRules rules;
        public PaintToolController tools;
        public SpriteRenderer paper;
        public AtelierCanvasLayout layout;
        public GameObject[] pages;
        public TMP_Text wallet, status, exhibition, collection, selectedTube, campaignLabel;
        public Image tubeFill;
        public PaintTubeGraphic tubeGraphic;
        public TMP_InputField artworkName;
        public GalleryButton[] buttons;
        public GalleryArtView[] wallArt, inventoryArt;
        public GalleryCustomerView[] customers;
        public GameObject[] decorations, extensions;
        public string storageFolderOverride;
        public GalleryStore Store { get; private set; }
        public bool Ready { get; private set; }
        public int CurrentPage { get; private set; }
        public bool CanPaint => Ready && CurrentPage == 1 && Store.Data.activeCanvas && !tools.InputLocked;
        public bool ShowUnsold { get; private set; }
        private int collectionPage, cardVersion = -1, techniqueMask;
        private bool confirmClear, confirmDiscard;
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly List<GalleryArtwork> visible = new List<GalleryArtwork>();
        private Coroutine exhibitionLoop;
        private bool saving;
        private PaintColorDefinition displayedColor;
        private int displayedPercent = -1;

        private IEnumerator Start()
        {
            tools.InputLocked = true;
            Store = new GalleryStore(string.IsNullOrEmpty(storageFolderOverride) ? Path.Combine(Application.persistentDataPath, "GalleryAtelier") : storageFolderOverride, rules, tools.Inventory);
            paper.GetComponent<CwPaintableSprite>().Activate();
            yield return null;
            tools.surface.Initialize();
            if (!tools.surface.IsReady) { status.text = "Tuval hazırlanamadı."; yield break; }
            if (Store.Data.activeCanvas && Store.DraftPath != null) tools.surface.ReadArtistDraft(Store.DraftPath);
            Store.Changed += Refresh; Store.Visited += OnVisitor;
            tools.StrokeBegan += OnBegin; tools.StrokeEnded += OnStroke;
            tools.ColorChanged += OnColor; tools.ToolChanged += OnTool;
            tools.TubeEmpty += OnEmptyTube;
            tools.Inventory.Changed += OnTube;
            Ready = true; SetPage(0);
            if (Store.IsOpen) exhibitionLoop = StartCoroutine(ExhibitionClock());
        }
        public bool PrepareCanvas()
        {
            if (!Ready || CurrentPage != 1 || artworkName != null && artworkName.isFocused) return false;
            if (!Store.BeginCanvas()) return false;
            tools.InputLocked = false; return true;
        }
        public void SetPage(int page)
        {
            if (!Ready || page < 0 || page >= pages.Length) return;
            tools.EndStroke();
            CurrentPage = page;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == page);
            paper.enabled = page == 1;
            var controls=GetComponent<GalleryToolControls>();
            tools.InputLocked = page != 1 || controls!=null && controls.popup.activeSelf;
            layout.canvas.GetComponentInChildren<GallerySafeArea>(true).Refresh();
            if (page == 1) layout.RefreshLayout();
            if (tools.cursor != null) tools.cursor.HideNow();
            if (page != 1) SaveDraft();
            Refresh();
        }
        public void Act(GalleryAction action, int index)
        {
            if (!Ready || saving) return;
            tools.EndStroke();
            switch (action)
            {
                case GalleryAction.Page: SetPage(index); break;
                case GalleryAction.Tool: tools.SelectTool(rules.painting.tools[index]); break;
                case GalleryAction.Color: tools.SelectColor(rules.painting.colors[index]); break;
                case GalleryAction.Undo: tools.Undo(); tools.Inventory.FlushNotifications(); ScheduleSave(); break;
                case GalleryAction.Clear:
                    if (!confirmClear) { confirmClear = true; status.text = "Silmek için tekrar bas. Harcanan boya tüpe dönmez."; return; }
                    tools.ClearCanvas(); techniqueMask = 0; confirmClear = false; ScheduleSave(); break;
                case GalleryAction.Finish: FinishArtwork(); break;
                case GalleryAction.Open:
                    SaveDraft();
                    if (Store.OpenExhibition()) { SetPage(0); if (exhibitionLoop == null) exhibitionLoop = StartCoroutine(ExhibitionClock()); }
                    break;
                case GalleryAction.Canvases: Store.BuyCanvases(); break;
                case GalleryAction.Tube:
                    // A refill changes the inventory baseline: past strokes must not rewind paid purchases.
                    if (!Store.Checkpoint(tools.surface)) { status.text = "Tuval kaydedilemedi; tüp alışverişi bekliyor."; return; }
                    if (Store.BuyTube(rules.painting.colors[index])) { tools.History.Clear(); SaveDraft(); }
                    break;
                case GalleryAction.Expand: Store.Expand(); break;
                case GalleryAction.Decorate: Store.Decorate(); break;
                case GalleryAction.Advertise: Store.Advertise(index); break;
                case GalleryAction.Discard:
                    if (!confirmDiscard) { confirmDiscard = true; status.text = "Satılmayanları kalıcı kaldırmak için tekrar bas. Gelir getirmez."; return; }
                    Store.DiscardUnsold(); confirmDiscard = false; break;
                case GalleryAction.Filter: ShowUnsold = !ShowUnsold; collectionPage = 0; cardVersion = -1; break;
                case GalleryAction.Next: collectionPage++; cardVersion = -1; break;
                case GalleryAction.Challenge: SaveDraft(); SceneManager.LoadScene("Assets/Painting/Scenes/Painting.unity"); break;
            }
            if (action != GalleryAction.Clear) confirmClear = false;
            if (action != GalleryAction.Discard) confirmDiscard = false;
            Refresh();
        }
        public bool FinishArtwork()
        {
            if (!Ready || !Store.Data.activeCanvas) return false;
            tools.EndStroke();
            float coverage = tools.surface.CoveredArea() / (paper.bounds.size.x * paper.bounds.size.y);
            if (coverage < rules.minimumCoverage) { status.text = "Tuvaline biraz daha resim yap; tek bir nokta koleksiyon için yeterli değil."; return false; }
            Texture2D image = null;
            try
            {
                saving = true; tools.surface.Flush();
                if (tools.cursor != null) tools.cursor.HideNow();
                image = AtelierArtworkCapture.Capture(tools.mainCamera, paper);
                int techniques = 0; for (int i = 0; i < 16; i++) if ((techniqueMask & (1 << i)) != 0) techniques++;
                Vector3 mass = tools.surface.MeasurePigmentMass(); float volume = Mathf.Max(.000001f,(float)tools.surface.MeasureVolume());
                var dominant = new Color(Mathf.Exp(-mass.x/volume),Mathf.Exp(-mass.y/volume),Mathf.Exp(-mass.z/volume)).gamma;
                if (!Store.CompleteArtwork(image.EncodeToPNG(), coverage, dominant, techniques, artworkName != null ? artworkName.text : "")) return false;
                CancelInvoke(nameof(SaveDraft)); tools.ClearCanvas(); techniqueMask = 0;
                if (artworkName != null) artworkName.text = "";
                SetPage(2); return true;
            }
            catch (Exception e) { Debug.LogException(e); status.text = "Tablo kaydedilemedi. Tuvalin atölyede korunuyor."; return false; }
            finally { saving = false; if (image != null) Destroy(image); Refresh(); }
        }
        private IEnumerator ExhibitionClock()
        {
            var second = new WaitForSecondsRealtime(1);
            while (Ready && Store.IsOpen)
            { yield return second; Store.Tick(); }
            exhibitionLoop = null;
            if (Ready) { status.text = "Sergi bitti • " + Store.Data.exhibitionSold + " satış • " + Store.Data.exhibitionRevenue + " altın. Satılmayanlar depoda."; Refresh(); }
        }
        private void OnVisitor()
        {
            if (customers == null || customers.Length == 0) return;
            int i = (Store.Data.visitorIndex - 1) % customers.Length;
            customers[i].Visit(Store.LastVisitor, Store.LastPurchased, Store.LastViewed != null ? Store.LastViewed.price : 0, i);
        }
        private void OnBegin(PaintToolDefinition tool) { CancelInvoke(nameof(SaveDraft)); }
        private void OnStroke(PaintToolDefinition tool)
        {
            int i = rules.painting.tools.IndexOf(tool); if (i >= 0) techniqueMask |= 1 << i;
            ScheduleSave(); Refresh();
        }
        private void ScheduleSave() { CancelInvoke(nameof(SaveDraft)); Invoke(nameof(SaveDraft), 15f); }
        public void SaveDraft()
        {
            if (!Ready || tools.IsStroking) return;
            CancelInvoke(nameof(SaveDraft)); Store.Checkpoint(tools.surface);
        }
        private void OnTube(PaintColorDefinition _) => RefreshPaint();
        private void OnEmptyTube(PaintColorDefinition color)
        { if (Ready) status.text = color.displayName + " tüpü bitti. Bilgisayar > Mağaza'dan yenileyebilirsin. Baskı ve spatula mevcut boyayı taşır."; }
        private void OnTool(PaintToolDefinition _) => RefreshPaint();
        private void OnColor(PaintColorDefinition _) => RefreshPaint();
        private void RefreshPaint()
        {
            if (!Ready) return;
            var color = tools.CurrentColor;
            if (color != null)
            {
                float fraction = tools.Inventory.Fraction(color);
                int percent = Mathf.RoundToInt(fraction * 100);
                if (color != displayedColor || percent != displayedPercent)
                { selectedTube.text = color.displayName + "  •  " + percent + "%"; displayedColor = color; displayedPercent = percent; }
                tubeFill.color = color.pigment; tubeFill.fillAmount = fraction;
                if (tubeGraphic != null) tubeGraphic.SetState(fraction,color.pigment);
            }
            foreach (var button in buttons) if (button != null && button.isActiveAndEnabled) button.Refresh();
        }
        private void Refresh()
        {
            if (!Ready) return;
            var d = Store.Data;
            wallet.text = d.coins + " ALTIN  •  " + d.canvases + " TUVAL";
            status.text = Store.Message;
            exhibition.text = Store.IsOpen ? "SERGİ AÇIK  " + d.secondsRemaining / 60 + ":" + (d.secondsRemaining % 60).ToString("00") + "\n" + d.exhibitionSold + " satış  •  " + d.exhibitionRevenue + " altın" :
                "YENİ KOLEKSİYON\n" + Store.StoredCount + " / " + rules.exhibitionSize + " eser hazır";
            collection.text = (ShowUnsold ? "SATILMAYAN ESERLER" : "KOLEKSİYONUM") + "\n" + Store.StoredCount + " hazır • " + Store.UnsoldCount + " satılmadı";
            campaignLabel.text = d.campaign == 0 ? "Bir sonraki sergin için ziyaretçi çek.\nKampanya, sergi sonunda biter." : "KAMPANYA PLANLANDI • SEVİYE " + d.campaign + "\nSıradaki sergide daha sık müşteri gelecek.";
            for (int i = 0; i < decorations.Length; i++) decorations[i].SetActive(d.decoration > i);
            for (int i = 0; i < extensions.Length; i++) extensions[i].SetActive(d.expansion > i);
            RefreshPaint();
            int version = d.artworks.Count * 100000 + d.sold * 100 + Store.UnsoldCount + d.expansion * 1000 + (Store.IsOpen ? 10000000 : 0);
            if (version != cardVersion) { cardVersion = version; RefreshArt(); }
        }
        private void RefreshArt()
        {
            visible.Clear();
            foreach (var art in Store.Data.artworks) if (art.status == (ShowUnsold ? ArtworkStatus.Unsold : ArtworkStatus.Stored)) visible.Add(art);
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(visible.Count / (float)inventoryArt.Length));
            collectionPage %= pageCount;
            for (int i = 0; i < inventoryArt.Length; i++)
            { int n = collectionPage * inventoryArt.Length + i; var art = n < visible.Count ? visible[n] : null; inventoryArt[i].Show(art, TextureFor(art)); }
            visible.Clear();
            foreach (var art in Store.Data.artworks) if (art.status == ArtworkStatus.Exhibited) visible.Add(art);
            // Closed gallery displays the latest collection as a preview, without selling it.
            if (!Store.IsOpen) foreach (var art in Store.Data.artworks) if (art.status == ArtworkStatus.Stored) visible.Add(art);
            for (int i = 0; i < wallArt.Length; i++) { var art = i < visible.Count ? visible[i] : null; wallArt[i].Show(art, TextureFor(art)); }
            var keep = new HashSet<string>();
            foreach (var view in inventoryArt) if (view.picture.texture != null) keep.Add(view.picture.texture.name);
            foreach (var view in wallArt) if (view.picture.texture != null) keep.Add(view.picture.texture.name);
            var remove = new List<string>(); foreach (var pair in textures) if (!keep.Contains(pair.Key)) { Destroy(pair.Value); remove.Add(pair.Key); }
            foreach (var id in remove) textures.Remove(id);
        }
        private Texture2D TextureFor(GalleryArtwork art)
        {
            if (art == null) return null;
            if (textures.TryGetValue(art.id, out var texture)) return texture;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = art.id };
                if (!texture.LoadImage(File.ReadAllBytes(Store.ImagePath(art.id)))) { Destroy(texture); return null; }
                textures.Add(art.id, texture); return texture;
            }
            catch (IOException e) { if (texture != null) Destroy(texture); Debug.LogWarning(e.Message); return null; }
        }
        private void OnApplicationPause(bool paused) { if (paused && Ready) { tools.EndStroke(); SaveDraft(); } }
        private void OnApplicationQuit() { if (Ready) { tools.EndStroke(); SaveDraft(); } }
        private void OnDestroy()
        {
            if (Store != null) { Store.Changed -= Refresh; Store.Visited -= OnVisitor; }
            if (tools != null) { tools.StrokeBegan -= OnBegin; tools.StrokeEnded -= OnStroke; tools.ColorChanged -= OnColor; tools.ToolChanged -= OnTool; tools.TubeEmpty -= OnEmptyTube; if (tools.Inventory != null) tools.Inventory.Changed -= OnTube; }
            foreach (var texture in textures.Values) if (texture != null) Destroy(texture);
        }
    }
}
