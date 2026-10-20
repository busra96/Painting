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
    public sealed class AtelierShopController : MonoBehaviour
    {
        public AtelierCanvasPainter painter;
        public SpriteRenderer paper;
        public GameObject workshopPanel, shopPanel, suppliesPanel;
        public TMP_Text economyLabel, statusLabel, paintLabel, shopLabel, customerLabel, titleLabel;
        public TMP_InputField artworkTitle;
        public AtelierActionButton[] buttons;
        public AtelierArtworkCard[] cards;
        public AtelierCanvasLayout layout;
        public AtelierShopStore Store { get; private set; }
        public bool Ready { get; private set; }
        public bool CanPaint => Ready && Store != null && CurrentTab == 0 &&
            Store.Data.activeCanvasTier >= 0 && (artworkTitle == null || !artworkTitle.isFocused);
        public int SelectedColor { get; private set; }
        public int CurrentTab { get; private set; }
        [SerializeField, HideInInspector] private string storageFolderOverride;
        public string StorageFolderOverride { get => storageFolderOverride; set => storageFolderOverride = value; }
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private float refreshCountdown, valueCountdown;
        private float coverage;
        private int observedRevision = -1;
        private int displayedCanvasTier = -2;
        private bool clearConfirmation;
        private string message = "Boş bir tuval alarak başla. Her resim mağazanın yeni hikâyesi.";

        private IEnumerator Start()
        {
            Store = new AtelierShopStore(string.IsNullOrWhiteSpace(StorageFolderOverride)
                ? Path.Combine(Application.persistentDataPath, "AtelierShop") : StorageFolderOverride);
            var model = paper.GetComponent<CwPaintableSprite>();
            model.Activate();
            yield return null;
            painter.surface.Initialize();
            if (!painter.surface.IsReady) { message = "Tuval hazırlanamadı."; Refresh(); yield break; }
            if (Store.Data.activeCanvasTier >= 0 && painter.surface.ReadArtistDraft(Store.DraftPath))
                painter.RestoreBudget((float)(painter.surface.DepositedVolume / painter.surface.UnitVolume), Store.Data.draftToolMask);
            Ready = true;
            layout.RefreshLayout();
            SetTab(0);
        }

        private void Update()
        {
            if (!Ready) return;
            if (Store.TickSales(Time.unscaledDeltaTime)) { message = Store.LastMessage; RefreshCards(); }
            painter.AllowInput = CanPaint;
            valueCountdown -= Time.unscaledDeltaTime;
            if (valueCountdown <= 0 && observedRevision != painter.Revision)
            {
                observedRevision = painter.Revision;
                coverage = painter.surface.CoveredArea() / (paper.bounds.size.x * paper.bounds.size.y);
                valueCountdown = 0.5f;
            }
            refreshCountdown -= Time.unscaledDeltaTime;
            if (refreshCountdown <= 0) { Refresh(); refreshCountdown = 0.2f; }
        }

        public void Act(AtelierAction action, int index)
        {
            if (!Ready) return;
            painter.EndStroke();
            bool wasClear = clearConfirmation;
            clearConfirmation = false;
            switch (action)
            {
                case AtelierAction.Tab: SetTab(index); break;
                case AtelierAction.Tool:
                    AtelierTool tool = (AtelierTool)index;
                    if (!Store.ToolUnlocked(tool))
                    { if (!Store.BuyTool(tool)) { message = Store.LastMessage; break; } message = Store.LastMessage; }
                    painter.SetTool(tool); break;
                case AtelierAction.Color:
                    if (index >= 4 && !Store.Data.expandedPalette)
                    { message = "Bu renkler Özel Pigment Seti ile açılır. Malzemeler sekmesine bak."; break; }
                    SelectedColor = index; painter.SetColor(AtelierRules.Colors[index]); break;
                case AtelierAction.Canvas: Store.SelectCanvas(index); message = Store.LastMessage; break;
                case AtelierAction.Palette: Store.BuyPalette(); message = Store.LastMessage; break;
                case AtelierAction.Expand: Store.Expand(); message = Store.LastMessage; RefreshCards(); break;
                case AtelierAction.NewCanvas:
                    if (Store.BeginCanvas()) { painter.ClearCanvas(); coverage = 0; if (artworkTitle != null) artworkTitle.text = ""; }
                    message = Store.LastMessage; break;
                case AtelierAction.Undo: painter.Undo(); break;
                case AtelierAction.Clear:
                    if (!wasClear) { clearConfirmation = true; message = "Tuvaldeki boyayı silmek için TEMİZLE'ye tekrar bas."; }
                    else { painter.ClearCanvas(); coverage = 0; message = "Aynı tuval yeniden hazır."; }
                    break;
                case AtelierAction.Exhibit: Exhibit(); break;
                case AtelierAction.Challenge:
                    SaveDraft(); SceneManager.LoadScene("Assets/Painting/Scenes/Painting.unity"); break;
            }
            Refresh();
        }

        public bool TryBeginPainting()
        {
            if (!Ready || CurrentTab != 0 || (artworkTitle != null && artworkTitle.isFocused)) return false;
            if (Store.Data.activeCanvasTier < 0)
            {
                // Clicking the visible blank canvas buys it once, then starts this same gesture.
                if (!Store.BeginCanvas()) { message = Store.LastMessage; Refresh(); return false; }
                painter.ClearCanvas(); coverage = 0;
                if (artworkTitle != null) artworkTitle.text = "";
                message = Store.LastMessage;
            }
            painter.AllowInput = true;
            Refresh();
            return true;
        }

        public void SetTab(int tab)
        {
            painter.EndStroke(); CurrentTab = Mathf.Clamp(tab, 0, 2);
            workshopPanel.SetActive(CurrentTab == 0);
            shopPanel.SetActive(CurrentTab == 1);
            suppliesPanel.SetActive(CurrentTab == 2);
            paper.enabled = CurrentTab == 0;
            if (Ready) coverage = painter.surface.CoveredArea() / (paper.bounds.size.x * paper.bounds.size.y);
            if (CurrentTab == 1) RefreshCards();
            Refresh();
        }

        public bool Exhibit()
        {
            painter.EndStroke();
            coverage = painter.surface.CoveredArea() / (paper.bounds.size.x * paper.bounds.size.y);
            if (coverage < 0.005f || Store.Data.activeCanvasTier < 0 ||
                Store.Data.artworks.Count >= AtelierRules.Capacity(Store.Data.shopLevel))
            { message = coverage < 0.005f ? "Biraz daha boya ekle; boş tuval sergilenemez." : "Yeni tuval al veya vitrinde yer aç."; return false; }
            Texture2D image = null;
            try
            {
                painter.surface.Flush();
                image = AtelierArtworkCapture.Capture(painter.paintCamera, paper);
                if (image == null) return false;
                bool success = Store.ListArtwork(image.EncodeToPNG(), coverage, painter.UsedTools,
                    artworkTitle != null ? artworkTitle.text : "");
                message = Store.LastMessage;
                if (success) { painter.ClearCanvas(); coverage = 0; SetTab(1); }
                return success;
            }
            catch (Exception exception) { message = "Tablo kaydedilemedi. Tuvalin tezgâhta duruyor."; Debug.LogException(exception); return false; }
            finally { if (image != null) Destroy(image); Refresh(); }
        }

        private void Refresh()
        {
            if (Store == null) return;
            AtelierProgress data = Store.Data;
            int frameTier = Mathf.Max(0, data.activeCanvasTier >= 0 ? data.activeCanvasTier : data.selectedCanvasTier);
            if (displayedCanvasTier != frameTier)
            {
                displayedCanvasTier = frameTier;
                Color frameColor = frameTier == 0 ? new Color(0.44f, 0.25f, 0.11f) :
                    frameTier == 1 ? new Color(0.13f, 0.35f, 0.31f) : new Color(0.71f, 0.47f, 0.14f);
                foreach (Image edge in layout.paperRegion.GetComponentsInChildren<Image>()) edge.color = frameColor;
            }
            economyLabel.text = data.coins + " ALTIN    •    MAĞAZA " + data.shopLevel + "    •    " + data.soldCount + " SATIŞ";
            statusLabel.text = message;
            titleLabel.text = data.activeCanvasTier < 0 ? "BOYAMAYA BAŞLA • " + AtelierRules.CanvasPrices[data.selectedCanvasTier] + " ALTIN" : AtelierRules.CanvasNames[data.activeCanvasTier];
            paintLabel.text = "BOYA %" + Mathf.RoundToInt(painter.Remaining / AtelierRules.PaintAllowance * 100f) +
                "    •    " + AtelierRules.ToolNames[(int)painter.Tool] + "    •    TAHMİNİ " +
                (coverage >= 0.005f ? AtelierRules.Quote(coverage, painter.UsedTools, Mathf.Max(0, data.activeCanvasTier)) : 0) + " ALTIN";
            shopLabel.text = "VİTRİN  " + data.artworks.Count + " / " + AtelierRules.Capacity(data.shopLevel) +
                "    •    TOPLAM KAZANÇ " + data.totalRevenue;
            customerLabel.text = data.artworks.Count == 0 ? "Vitrine bir tablo koy. İlk müşterini bekleyelim." :
                "Bir müşteri resimleri inceliyor...  " + Mathf.CeilToInt(data.secondsToSale) + " sn";
            foreach (AtelierActionButton button in buttons) if (button != null) button.Refresh();
        }

        private void RefreshCards()
        {
            if (Store == null) return;
            // Keep only currently displayed textures; sold paintings cannot accumulate GPU memory.
            var live = new HashSet<string>();
            foreach (AtelierArtwork item in Store.Data.artworks) live.Add(item.id);
            var expired = new List<string>();
            foreach (var pair in textures) if (!live.Contains(pair.Key)) { Destroy(pair.Value); expired.Add(pair.Key); }
            foreach (string id in expired) textures.Remove(id);
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].gameObject.SetActive(i < AtelierRules.Capacity(Store.Data.shopLevel));
                AtelierArtwork item = i < Store.Data.artworks.Count ? Store.Data.artworks[i] : null;
                Texture2D image = null;
                if (item != null && !textures.TryGetValue(item.id, out image) && File.Exists(Store.ImagePath(item.id)))
                {
                    image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    try
                    {
                        if (image.LoadImage(File.ReadAllBytes(Store.ImagePath(item.id)), true)) textures[item.id] = image;
                        else { Destroy(image); image = null; }
                    }
                    catch (IOException exception) { Destroy(image); image = null; Debug.LogWarning(exception.Message); }
                }
                cards[i].Show(item, image, i);
            }
        }

        public void SaveDraft()
        {
            if (!Ready || Store == null) return;
            painter.EndStroke();
            try
            {
                if (Store.Data.activeCanvasTier >= 0)
                {
                    painter.surface.WriteArtistDraft(Store.DraftPath);
                    Store.Data.draftUsedPaint = painter.UsedPaint;
                    Store.Data.draftToolMask = painter.UsedTools;
                }
                Store.Save();
            }
            catch (IOException exception) { Debug.LogWarning("Atelier draft save failed: " + exception.Message); }
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveDraft(); }
        private void OnApplicationQuit() => SaveDraft();
        private void OnDestroy() { foreach (Texture2D texture in textures.Values) if (texture != null) Destroy(texture); }
    }
}
