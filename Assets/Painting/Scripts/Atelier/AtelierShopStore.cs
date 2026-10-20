using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PaintingGame.Atelier
{
    public enum AtelierTool { Tube, RoundBrush, FlatBrush, FanBrush, Press, Flower, Star, Knife, WideBlade }

    public static class AtelierRules
    {
        public static readonly string[] ToolNames = { "TÜP", "YUVARLAK", "DÜZ FIRÇA", "FAN FIRÇA", "BASTIR", "ÇİÇEK", "YILDIZ", "PALET BIÇAĞI", "GENİŞ SPATULA" };
        public static readonly int[] ToolPrices = { 0, 0, 35, 60, 0, 75, 95, 110, 0 };
        public static readonly string[] CanvasNames = { "PAMUK TUVAL", "KETEN TUVAL", "USTA TUVALİ" };
        public static readonly int[] CanvasPrices = { 8, 18, 35 };
        public static readonly int[] CanvasUnlockPrices = { 0, 80, 200 };
        public static readonly Color[] Colors = {
            new Color(0.90f, 0.055f, 0.04f), new Color(1f, 0.80f, 0.08f),
            new Color(0.055f, 0.22f, 0.9f), new Color(0.96f, 0.94f, 0.84f),
            new Color(0.03f, 0.56f, 0.25f), new Color(0.82f, 0.05f, 0.57f),
            new Color(0.02f, 0.64f, 0.67f), new Color(0.10f, 0.065f, 0.05f) };
        public const float PaintAllowance = 120f;
        public static int Capacity(int level) => 2 + level * 2;
        public static int ExpansionPrice(int level) => 100 + level * level * 50;
        public static int Quote(float coverage, int toolMask, int canvasTier)
        {
            int techniques = 0;
            for (int i = 0; i < ToolNames.Length; i++) if ((toolMask & (1 << i)) != 0) techniques++;
            float quality = 1f + canvasTier * 0.65f;
            return Mathf.RoundToInt((18f + Mathf.Sqrt(Mathf.Clamp01(coverage)) * 125f +
                Mathf.Min(3, Mathf.Max(0, techniques - 1)) * 5f) * quality);
        }
    }

    [Serializable]
    public sealed class AtelierArtwork
    {
        public string id, title;
        public int price, canvasTier;
        public float coverage;
    }

    [Serializable]
    public sealed class AtelierProgress
    {
        public int version = 1;
        public int coins = 90, shopLevel = 1, soldCount, totalRevenue;
        public int tools = (1 << (int)AtelierTool.Tube) | (1 << (int)AtelierTool.RoundBrush) | (1 << (int)AtelierTool.Press);
        public bool expandedPalette;
        public int unlockedCanvasTier, selectedCanvasTier, activeCanvasTier = -1;
        public float draftUsedPaint;
        public int draftToolMask;
        public float secondsToSale = 10f;
        public List<AtelierArtwork> artworks = new List<AtelierArtwork>();
    }

    // Scene-independent economy; a folder argument also allows checks to use isolated temporary saves.
    public sealed class AtelierShopStore
    {
        public string Folder { get; }
        public string DraftPath => Path.Combine(Folder, "draft.paint");
        public AtelierProgress Data { get; private set; }
        public string LastMessage { get; private set; } = "İlk boş tuvalini al ve atölyeni aç.";
        public int Revision { get; private set; }
        public bool ToolUnlocked(AtelierTool tool) => (Data.tools & (1 << (int)tool)) != 0;
        public string ImagePath(string id) => Path.Combine(Folder, id + ".png");

        public AtelierShopStore(string folder)
        {
            Folder = folder;
            Data = ReadProfile(Path.Combine(folder, "shop.json")) ??
                   ReadProfile(Path.Combine(folder, "shop.json.bak")) ?? new AtelierProgress();
            if (Data.artworks == null) Data.artworks = new List<AtelierArtwork>();
            Data.coins = Mathf.Max(0, Data.coins);
            Data.tools |= 1 << (int)AtelierTool.WideBlade;
            Data.shopLevel = Mathf.Clamp(Data.shopLevel, 1, 5);
            Data.unlockedCanvasTier = Mathf.Clamp(Data.unlockedCanvasTier, 0, 2);
            Data.selectedCanvasTier = Mathf.Clamp(Data.selectedCanvasTier, 0, Data.unlockedCanvasTier);
            Data.activeCanvasTier = Mathf.Clamp(Data.activeCanvasTier, -1, 2);
            Data.artworks.RemoveAll(a => a == null || string.IsNullOrEmpty(a.id) || !File.Exists(ImagePath(a.id)));
        }

        private static AtelierProgress ReadProfile(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                AtelierProgress data = JsonUtility.FromJson<AtelierProgress>(File.ReadAllText(path));
                return data != null && data.version == 1 ? data : null;
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            { Debug.LogWarning("Atelier profile could not be read: " + exception.Message); return null; }
        }

        public void Save() => AtomicWrite(Path.Combine(Folder, "shop.json"), System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Data, true)));

        public static void AtomicWrite(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp", backup = path + ".bak";
            File.WriteAllBytes(temp, bytes);
            try
            {
                if (File.Exists(backup)) File.Delete(backup);
                if (File.Exists(path)) File.Move(path, backup);
                File.Move(temp, path);
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
                throw;
            }
        }

        private bool Commit(Action change, string message)
        {
            string before = JsonUtility.ToJson(Data);
            try { change(); Save(); }
            catch (Exception exception)
            {
                Data = JsonUtility.FromJson<AtelierProgress>(before);
                LastMessage = "Kayıt tamamlanamadı. İşlem geri alındı.";
                Debug.LogException(exception);
                return false;
            }
            LastMessage = message; Revision++; return true;
        }

        private bool Reject(string message) { LastMessage = message; Revision++; return false; }

        private bool CanBuyUpgrade(int price)
        {
            // Keep a route back to painting. Spending the last coin on an unlock must not strand
            // an empty shop with no canvas and no sale pending.
            if (Data.coins < price) return Reject("Bu alışveriş için yeterli altının yok.");
            if (Data.activeCanvasTier < 0 && Data.artworks.Count == 0 && Data.coins - price < AtelierRules.CanvasPrices[0])
                return Reject("Yeni bir tuval alabilmek için 8 altın ayırmalısın.");
            return true;
        }

        public bool BeginCanvas()
        {
            if (Data.activeCanvasTier >= 0) return Reject("Tezgâhta zaten bir tuval var. Önce vitrinde sergile.");
            int price = AtelierRules.CanvasPrices[Data.selectedCanvasTier];
            if (Data.coins < price) return Reject("Bu tuval için yeterli altının yok.");
            bool success = Commit(() => {
                Data.coins -= price; Data.activeCanvasTier = Data.selectedCanvasTier;
                Data.draftUsedPaint = 0; Data.draftToolMask = 0;
            }, "Boş tuval hazır. Hayalindeki resmi yap!");
            if (success) DeleteDraft();
            return success;
        }

        public bool BuyTool(AtelierTool tool)
        {
            if (ToolUnlocked(tool)) return Reject("Bu alet zaten senin.");
            int price = AtelierRules.ToolPrices[(int)tool];
            if (!CanBuyUpgrade(price)) return false;
            return Commit(() => { Data.coins -= price; Data.tools |= 1 << (int)tool; },
                AtelierRules.ToolNames[(int)tool] + " alet çantana eklendi.");
        }

        public bool BuyPalette()
        {
            if (Data.expandedPalette) return Reject("Özel pigmentler zaten açık.");
            if (!CanBuyUpgrade(90)) return false;
            return Commit(() => { Data.coins -= 90; Data.expandedPalette = true; }, "Dört yeni pigment paletine eklendi.");
        }

        public bool SelectCanvas(int tier)
        {
            if (tier < 0 || tier > 2) return false;
            if (tier <= Data.unlockedCanvasTier)
                return Commit(() => Data.selectedCanvasTier = tier, AtelierRules.CanvasNames[tier] + " seçildi.");
            if (tier != Data.unlockedCanvasTier + 1) return Reject("Önce bir önceki tuval türünü aç.");
            int price = AtelierRules.CanvasUnlockPrices[tier];
            if (!CanBuyUpgrade(price)) return false;
            return Commit(() => { Data.coins -= price; Data.unlockedCanvasTier = tier; Data.selectedCanvasTier = tier; },
                "Yeni tuval türü açıldı. Sonraki boş tuvalinde kullanabilirsin.");
        }

        public bool Expand()
        {
            if (Data.shopLevel >= 5) return Reject("Mağazan en büyük boyuta ulaştı.");
            int price = AtelierRules.ExpansionPrice(Data.shopLevel);
            if (!CanBuyUpgrade(price)) return false;
            return Commit(() => { Data.coins -= price; Data.shopLevel++; }, "Yeni sergi duvarı açıldı. Daha fazla tablo sergileyebilirsin.");
        }

        public bool ListArtwork(byte[] png, float coverage, int toolMask, string title)
        {
            if (Data.activeCanvasTier < 0) return Reject("Önce boş bir tuval al.");
            if (coverage < 0.005f || png == null || png.Length == 0) return Reject("Sergilemeden önce tuvaline biraz daha boya ekle.");
            if (Data.artworks.Count >= AtelierRules.Capacity(Data.shopLevel)) return Reject("Vitrin dolu. Bir tablo sat veya mağazanı genişlet.");
            var artwork = new AtelierArtwork {
                id = Guid.NewGuid().ToString("N"), title = string.IsNullOrWhiteSpace(title) ? "Tablo " + (Data.soldCount + Data.artworks.Count + 1) : title,
                canvasTier = Data.activeCanvasTier, coverage = coverage,
                price = AtelierRules.Quote(coverage, toolMask, Data.activeCanvasTier) };
            AtomicWrite(ImagePath(artwork.id), png);
            bool success = Commit(() => {
                if (Data.artworks.Count == 0) Data.secondsToSale = SaleInterval;
                Data.artworks.Add(artwork); Data.activeCanvasTier = -1;
                Data.draftUsedPaint = 0; Data.draftToolMask = 0;
            }, "Tablon vitrinde. Müşteriler birazdan gelmeye başlayacak.");
            if (!success) File.Delete(ImagePath(artwork.id));
            else DeleteDraft();
            return success;
        }

        private void DeleteDraft()
        {
            // Old backups must not be recovered as the next purchased canvas.
            foreach (string path in new[] { DraftPath, DraftPath + ".bak", DraftPath + ".tmp" })
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException exception) { Debug.LogWarning(exception.Message); }
            }
        }

        public float SaleInterval => Mathf.Max(6f, 14f - Data.shopLevel * 1.5f);

        public bool TickSales(float seconds)
        {
            if (Data.artworks.Count == 0 || seconds <= 0) return false;
            Data.secondsToSale -= Mathf.Min(seconds, 1f);
            if (Data.secondsToSale > 0) return false;
            AtelierArtwork sold = Data.artworks[0];
            bool success = Commit(() => {
                Data.artworks.RemoveAt(0); Data.coins += sold.price;
                Data.soldCount++; Data.totalRevenue += sold.price; Data.secondsToSale = SaleInterval;
            }, sold.title + " satıldı!  +" + sold.price + " altın");
            if (success)
            {
                try { if (File.Exists(ImagePath(sold.id))) File.Delete(ImagePath(sold.id)); }
                catch (IOException exception) { Debug.LogWarning(exception.Message); }
            }
            return success;
        }
    }
}
