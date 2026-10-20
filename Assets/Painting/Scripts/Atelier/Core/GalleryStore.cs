using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PaintingGame.Atelier
{
    public enum ArtworkStatus { Stored, Exhibited, Unsold, Sold }
    [Serializable] public sealed class GalleryArtwork
    {
        public string id, title;
        public int price;
        public Color dominant;
        public float coverage;
        public ArtworkStatus status;
    }
    [Serializable] public sealed class GalleryProgress
    {
        public int version = 1, coins, canvases, expansion, decoration, campaign, sold, revenue;
        public int exhibitionNumber, secondsRemaining, visitorCountdown, visitorIndex, exhibitionRevenue, exhibitionSold;
        public string draftFile;
        public bool activeCanvas;
        public List<GalleryArtwork> artworks = new List<GalleryArtwork>();
        public List<string> tubeIds = new List<string>();
        public List<float> tubeLevels = new List<float>();
    }

    // The only authority for money, stock and exhibition state. Every paid operation commits once or rolls back.
    public sealed class GalleryStore
    {
        public GalleryProgress Data { get; private set; }
        public GalleryRules Rules { get; }
        public TubeInventory Tubes { get; }
        public string Folder { get; }
        public bool IsOpen => Data.secondsRemaining > 0;
        public string Message { get; private set; } = "Atölyene hoş geldin. İlk koleksiyonunu hazırlayalım.";
        public CustomerProfile LastVisitor { get; private set; }
        public GalleryArtwork LastViewed { get; private set; }
        public bool LastPurchased { get; private set; }
        public int StoredCount => Count(ArtworkStatus.Stored);
        public int UnsoldCount => Count(ArtworkStatus.Unsold);
        public event Action Changed;
        public event Action Visited;

        public GalleryStore(string folder, GalleryRules rules, TubeInventory inventory)
        {
            Folder = folder; Rules = rules; Tubes = inventory;
            Data = Read(Path.Combine(folder, "gallery.json")) ?? Read(Path.Combine(folder, "gallery.json.bak")) ??
                new GalleryProgress { coins = rules.startingCoins, canvases = rules.startingCanvases };
            if (Data.artworks == null) Data.artworks = new List<GalleryArtwork>();
            if (Data.tubeIds == null) Data.tubeIds = new List<string>();
            if (Data.tubeLevels == null) Data.tubeLevels = new List<float>();
            Data.coins = Mathf.Max(0, Data.coins); Data.canvases = Mathf.Max(0, Data.canvases);
            Data.expansion = Mathf.Clamp(Data.expansion, 0, 4); Data.decoration = Mathf.Clamp(Data.decoration, 0, 4);
            Data.campaign = Mathf.Clamp(Data.campaign, 0, 3);
            var ids = new HashSet<string>();
            Data.artworks.RemoveAll(a => a == null || !SafeId(a.id) || !ids.Add(a.id) ||
                a.status != ArtworkStatus.Sold && !File.Exists(ImagePath(a.id)));
            if (Data.secondsRemaining <= 0)
                foreach (var art in Data.artworks) if (art.status == ArtworkStatus.Exhibited) art.status = ArtworkStatus.Unsold;
            float[] levels = Tubes.Snapshot();
            for (int i = 0; i < Tubes.Colors.Count; i++)
            {
                int n = Data.tubeIds.IndexOf(Tubes.Colors[i].id);
                if (n >= 0 && n < Data.tubeLevels.Count) levels[i] = Data.tubeLevels[n];
            }
            Tubes.Restore(levels); Tubes.FlushNotifications();
            CopyTubes();
        }
        public string ImagePath(string id) => Path.Combine(Folder, id + ".png");
        public string DraftPath => SafeId(Data.draftFile) ? Path.Combine(Folder, Data.draftFile + ".paint") : null;
        private static bool SafeId(string id) => Guid.TryParseExact(id, "N", out _);
        public int Count(ArtworkStatus status) { int count = 0; foreach (var a in Data.artworks) if (a.status == status) count++; return count; }
        private static GalleryProgress Read(string path)
        {
            if (!File.Exists(path)) return null;
            try { var p = JsonUtility.FromJson<GalleryProgress>(File.ReadAllText(path)); return p?.version == 1 ? p : null; }
            catch (Exception e) when (e is IOException || e is ArgumentException) { Debug.LogWarning("Gallery save recovery: " + e.Message); return null; }
        }
        private void CopyTubes()
        {
            Data.tubeIds.Clear(); Data.tubeLevels.Clear();
            foreach (var c in Tubes.Colors) { Data.tubeIds.Add(c.id); Data.tubeLevels.Add(Tubes.Level(c)); }
        }
        public bool Save()
        {
            try { AtelierShopStore.AtomicWrite(Path.Combine(Folder, "gallery.json"), System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Data, true))); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Message = "Kayıt yazılamadı. İşlem tamamlanmadı."; Debug.LogWarning(e.Message); return false; }
        }
        private bool Commit(Action change, string message, bool commitTubes = false)
        {
            string before = JsonUtility.ToJson(Data); float[] tubes = Tubes.Snapshot();
            change();
            if (commitTubes) CopyTubes();
            if (!Save()) { Data = JsonUtility.FromJson<GalleryProgress>(before); Tubes.Restore(tubes); Tubes.FlushNotifications(); Changed?.Invoke(); return false; }
            if (message != null) Message = message;
            Tubes.FlushNotifications(); Changed?.Invoke(); return true;
        }
        private bool Fail(string message) { Message = message; Changed?.Invoke(); return false; }
        public bool BeginCanvas()
        {
            if (Data.activeCanvas) return true;
            if (Data.canvases <= 0) return Fail("Tuvalin bitti. Bilgisayar > Mağaza'dan tuval paketi al.");
            return Commit(() => { Data.canvases--; Data.activeCanvas = true; Data.draftFile = null; }, "Yeni tuval hazır. Bir alet ve renk seç.");
        }
        public bool CompleteArtwork(byte[] png, float coverage, Color dominant, int techniques, string title = "")
        {
            if (!Data.activeCanvas || float.IsNaN(coverage) || float.IsInfinity(coverage) || coverage < Rules.minimumCoverage || png == null || png.Length == 0) return Fail("Önce tuvaline biraz daha resim yap.");
            if (StoredCount >= 60) return Fail("Depo dolu. Bir sergi açarak yer aç.");
            string id = Guid.NewGuid().ToString("N");
            try { AtelierShopStore.AtomicWrite(ImagePath(id), png); }
            catch (IOException e) { Debug.LogWarning(e.Message); return Fail("Eser kaydedilemedi; tuvalin korunuyor."); }
            bool ok = Commit(() => {
                Data.artworks.Add(new GalleryArtwork { id = id, title = string.IsNullOrWhiteSpace(title) ? "Çalışma " + (Data.artworks.Count + 1) : title.Trim().Substring(0, Mathf.Min(32, title.Trim().Length)),
                    price = Rules.ArtworkPrice(coverage, techniques), coverage = Mathf.Clamp01(coverage), dominant = dominant, status = ArtworkStatus.Stored });
                Data.activeCanvas = false; Data.draftFile = null;
            }, "Eser depoya alındı. Sergi için " + (StoredCount + 1) + "/" + Rules.exhibitionSize + " hazır.", true);
            if (!ok) DeleteFile(ImagePath(id)); else PruneDrafts();
            return ok;
        }
        public bool OpenExhibition()
        {
            if (IsOpen) return Fail("Sergin zaten açık.");
            int needed = Mathf.Max(1, Rules.exhibitionSize);
            if (StoredCount < needed) return Fail("Sergi açmak için " + (needed - StoredCount) + " yeni eser daha gerekiyor.");
            return Commit(() => {
                int n = 0; foreach (var art in Data.artworks) if (art.status == ArtworkStatus.Stored && n++ < needed) art.status = ArtworkStatus.Exhibited;
                Data.secondsRemaining = Rules.exhibitionSeconds; Data.visitorCountdown = 2; Data.visitorIndex = 0;
                Data.exhibitionNumber++; Data.exhibitionRevenue = Data.exhibitionSold = 0;
            }, "Sergi açıldı. Ziyaretçiler tablolarını incelemeye geliyor.");
        }
        // One-second ticks only during an exhibition. Save each decision before showing it; resuming cannot sell twice.
        public void Tick()
        {
            if (!IsOpen) return;
            if (Data.secondsRemaining > 1 && Data.visitorCountdown > 1)
            { Data.secondsRemaining--; Data.visitorCountdown--; Changed?.Invoke(); return; }
            int visitor = Data.visitorIndex;
            bool committed = Commit(() => {
                Data.secondsRemaining--;
                Data.visitorCountdown--;
                if (Data.visitorCountdown <= 0 && Count(ArtworkStatus.Exhibited) > 0) Visit();
                if (Data.secondsRemaining <= 0 || Count(ArtworkStatus.Exhibited) == 0)
                {
                    Data.secondsRemaining = 0;
                    foreach (var art in Data.artworks) if (art.status == ArtworkStatus.Exhibited) art.status = ArtworkStatus.Unsold;
                    Data.campaign = 0;
                    Message = "Sergi bitti: " + Data.exhibitionSold + " satış, " + Data.exhibitionRevenue + " altın. Satılmayanlar depoda.";
                }
            }, null);
            if (committed && visitor != Data.visitorIndex) Visited?.Invoke();
        }
        private void Visit()
        {
            int index = Data.visitorIndex++;
            var profiles = Rules.customers;
            LastVisitor = profiles != null && profiles.Length > 0 ? profiles[(index + Data.exhibitionNumber) % profiles.Length] : null;
            GalleryArtwork candidate = null;
            float best = -1;
            foreach (var art in Data.artworks)
            {
                if (art.status != ArtworkStatus.Exhibited) continue;
                float fit = LastVisitor == null ? 0.5f : 1 - Vector3.Distance((Vector4)art.dominant, (Vector4)LastVisitor.preferredColor) / 1.733f;
                float rank = fit + Random01(index, Convert.ToInt32(art.id.Substring(0, 7), 16)) * 0.25f;
                if (rank > best) { best = rank; candidate = art; }
            }
            LastViewed = candidate;
            float chance = Mathf.Clamp(Rules.basePurchaseChance + best * (LastVisitor != null ? LastVisitor.colorPreference : 0.15f) + Data.decoration * 0.035f, 0.08f, 0.9f);
            if (candidate != null) chance *= Mathf.Lerp(.5f,1,Mathf.Clamp01(candidate.coverage/.5f));
            int budget = Mathf.RoundToInt(Rules.visitorBudget * (LastVisitor != null ? LastVisitor.budgetMultiplier : 1));
            LastPurchased = candidate != null && candidate.price <= budget && Random01(index, 971) < chance;
            if (LastPurchased)
            { candidate.status = ArtworkStatus.Sold; Data.coins += candidate.price; Data.sold++; Data.revenue += candidate.price; Data.exhibitionRevenue += candidate.price; Data.exhibitionSold++; }
            Message = (LastVisitor != null ? LastVisitor.displayName : "Ziyaretçi") + (LastPurchased ? " ‘" + candidate.title + "’ aldı. +" + candidate.price + " altın" : "eserleri inceledi, satın almadan ayrıldı.");
            Data.visitorCountdown = Mathf.Max(2, Rules.visitorInterval - Data.expansion / 2 - Data.campaign);
            // Deferred until Commit succeeds; the controller consumes the new visitor index after the tick.
        }
        private float Random01(int visit, int salt)
        { unchecked { uint x = (uint)(visit * 747796405 + Data.exhibitionNumber * 289133645 + salt); x = (x ^ x >> 16) * 2246822519u; return (x ^ x >> 13) / (float)uint.MaxValue; } }
        public bool BuyCanvases() => Purchase(Rules.canvasPackPrice, () => Data.canvases += Rules.canvasesPerPack, "Tuval paketi atölyene geldi.", true);
        public bool BuyTube(PaintColorDefinition color)
        {
            if (color == null || !Tubes.Colors.ContainsReference(color)) return Fail("Bilinmeyen boya.");
            if (Tubes.Fraction(color) >= 0.999f) return Fail("Bu tüp zaten dolu.");
            return Purchase(color.price, () => Tubes.Refill(color), color.displayName + " tüpü yenilendi.", false, true);
        }
        public bool Expand() => Data.expansion >= 4 ? Fail("Galeri en büyük boyutunda.") : Purchase(Rules.ExpansionCost(Data.expansion), () => Data.expansion++, "Galeri genişledi; daha çok ziyaretçi gelebilir.");
        public bool Decorate() => Data.decoration >= 4 ? Fail("Dekorasyon tamamlandı.") : Purchase(Rules.DecorationCost(Data.decoration), () => Data.decoration++, "Yeni ışıklar ve dekorlar galeriye yerleşti.");
        public bool Advertise(int level)
        {
            if (IsOpen) return Fail("Reklamı sıradaki sergiden önce planla.");
            if (level < 1 || level > 3 || Data.campaign != 0) return Fail("Sıradaki sergi için bir kampanya seçebilirsin.");
            return Purchase(Rules.advertisingPrice * level, () => Data.campaign = level, "Kampanya hazır. Sıradaki sergide ziyaret sıklığı artacak.");
        }
        private bool Purchase(int price, Action apply, string message, bool productionPurchase = false, bool commitTubes = false)
        {
            if (price < 0 || Data.coins < price) return Fail("Altının bu alışveriş için yeterli değil.");
            // Keep a route back to painting when upgrades would spend the last production money.
            if (!productionPurchase && Data.canvases == 0 && !Data.activeCanvas && StoredCount < Rules.exhibitionSize && Data.coins - price < Rules.canvasPackPrice)
                return Fail("Önce üretim için bir tuval paketi al.");
            return Commit(() => { Data.coins -= price; apply(); }, message, commitTubes);
        }
        public bool DiscardUnsold()
        {
            var paths = new List<string>(); foreach (var a in Data.artworks) if (a.status == ArtworkStatus.Unsold) paths.Add(ImagePath(a.id));
            if (!Commit(() => Data.artworks.RemoveAll(a => a.status == ArtworkStatus.Unsold), "Satılmayan eserler kaldırıldı. Bu eserlerden gelir kazanılmaz.")) return false;
            foreach (var path in paths) DeleteFile(path); return true;
        }
        public bool Checkpoint(ImpastoPaintSurface surface)
        {
            if (!Data.activeCanvas) return Save();
            string id = Guid.NewGuid().ToString("N");
            if (!surface.IsReady || surface.IsSpreading) return false;
            try { surface.WriteArtistDraft(Path.Combine(Folder, id + ".paint")); }
            catch (IOException e) { Debug.LogWarning(e.Message); return false; }
            string before = JsonUtility.ToJson(Data); Data.draftFile = id; CopyTubes();
            if (!Save()) { Data = JsonUtility.FromJson<GalleryProgress>(before); DeleteDraft(Path.Combine(Folder, id + ".paint")); return false; }
            PruneDrafts(); return true;
        }
        private void PruneDrafts()
        {
            // Keep the snapshot referenced by the backup JSON as well as the current one.
            // Background sales preserve this checkpoint's tube levels until a new physical snapshot commits.
            string backup = Read(Path.Combine(Folder,"gallery.json.bak"))?.draftFile;
            try
            {
                foreach (string path in Directory.EnumerateFiles(Folder,"*.paint"))
                {
                    string id = Path.GetFileNameWithoutExtension(path);
                    if (SafeId(id) && id != Data.draftFile && id != backup) DeleteDraft(path);
                }
            }
            catch (IOException e) { Debug.LogWarning(e.Message); }
        }
        private static void DeleteFile(string path) { if (string.IsNullOrEmpty(path)) return; try { if (File.Exists(path)) File.Delete(path); } catch (IOException e) { Debug.LogWarning(e.Message); } }
        private static void DeleteDraft(string path) { if (string.IsNullOrEmpty(path)) return; DeleteFile(path); DeleteFile(path + ".bak"); DeleteFile(path + ".tmp"); }
    }
    internal static class ColorListExtensions
    {
        public static bool ContainsReference(this IReadOnlyList<PaintColorDefinition> colors, PaintColorDefinition color)
        { for (int i = 0; i < colors.Count; i++) if (colors[i] == color) return true; return false; }
    }
}
