using PaintingGame.Gallery;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    // Atelier flow: undo / clear / finish buttons, tool hints, empty-tube popup and the finished-artwork result.
    // Finished paintings are stored in PaintingGallery; they are the stock for the future exhibition.
    public sealed class AtelierHUD : MonoBehaviour
    {
        public PaintToolController controller;
        public ImpastoPaintSurface surface;
        public GridDataComparer comparer;
        public GalleryPanel gallery;
        public ModalView modal;
        public Button undoButton, clearButton, finishButton;
        public TMP_Text finishLabel;
        public TMP_Text statusLabel;
        public TMP_Text artworkCounter;
        [Min(0.5f)] public float hintSeconds = 2.6f;

        // "Geniş" → "GENİŞ" needs Turkish casing; fall back if the player strips culture data.
        private static readonly System.Globalization.CultureInfo Turkish = TurkishCulture();
        private static System.Globalization.CultureInfo TurkishCulture()
        {
            try { return System.Globalization.CultureInfo.GetCultureInfo("tr-TR"); }
            catch (System.Exception) { return System.Globalization.CultureInfo.InvariantCulture; }
        }
        private bool finishing;
        private Tween hintTween;

        private void Start()
        {
            undoButton.onClick.AddListener(Undo);
            clearButton.onClick.AddListener(AskClear);
            finishButton.onClick.AddListener(Finish);
            controller.StrokeEnded += OnStrokeEnded;
            controller.ToolChanged += ShowToolHint;
            controller.TubeEmpty += ShowEmptyTube;
            controller.ColorChanged += OnColorChanged;
            if (comparer != null)
            {
                comparer.OnCompared += OnScored;
                comparer.OnComparisonFailed += OnScoreFailed;
            }
            if (finishLabel != null) finishLabel.text = "BİTİR";
            RefreshButtons();
            RefreshCounter();
        }

        private void OnDestroy()
        {
            hintTween?.Kill();
            if (gallery != null) gallery.Closed -= RestartAfterPicker;
            if (controller != null)
            {
                controller.StrokeEnded -= OnStrokeEnded;
                controller.ToolChanged -= ShowToolHint;
                controller.TubeEmpty -= ShowEmptyTube;
                controller.ColorChanged -= OnColorChanged;
            }
            if (comparer != null)
            {
                comparer.OnCompared -= OnScored;
                comparer.OnComparisonFailed -= OnScoreFailed;
            }
        }

        private bool HasPaint => surface != null && surface.IsReady && surface.PaperVolume > 1e-6;

        private void RefreshButtons()
        {
            bool idle = !finishing && (modal == null || !modal.IsOpen);
            undoButton.interactable = idle && controller.History.CanUndo;
            clearButton.interactable = idle && HasPaint;
            finishButton.interactable = idle && HasPaint;
        }

        private void RefreshCounter()
        {
            if (artworkCounter == null) return;
            int count = PaintingGallery.Items.Count, needed = controller.config.artworksForExhibition;
            artworkCounter.text = count >= needed ? "SERGİYE HAZIR " + count + "/" + needed : "ESER " + count + "/" + needed;
        }

        private void OnStrokeEnded(PaintToolDefinition tool) => RefreshButtons();

        private void Undo()
        {
            controller.Undo();
            RefreshButtons();
        }

        private void AskClear()
        {
            Lock(true);
            modal.Show("Tuvali temizle?", "Tuvaldeki bütün boya silinir. Bu işlem geri alınamaz. Harcanan boya tüpe dönmez.", -1,
                "TEMİZLE", () => { controller.ClearCanvas(); Lock(false); }, "VAZGEÇ", () => Lock(false));
        }

        private void ShowToolHint(PaintToolDefinition tool)
        {
            if (tool != null) Hint(tool.displayName.ToUpper(Turkish) + (string.IsNullOrEmpty(tool.hint) ? "" : ": " + tool.hint));
        }

        private void Hint(string text)
        {
            if (statusLabel == null) return;
            hintTween?.Kill();
            statusLabel.text = text;
            statusLabel.alpha = 1;
            hintTween = statusLabel.DOFade(0, 0.5f).SetDelay(hintSeconds);
        }

        private void OnColorChanged(PaintColorDefinition color)
        {
            if (color == null) return;
            if (controller.Inventory.IsEmpty(color)) ShowEmptyTube(color);
            else Hint(color.displayName.ToUpper(Turkish));
        }

        private void ShowEmptyTube(PaintColorDefinition color)
        {
            if (color == null || modal.IsOpen) return;
            Lock(true);
            string body = color.displayName + " tüpü bitti. Yeni tüpü bilgisayardaki Mağazadan alabileceksin.\n" +
                          "Fırça boyasız da kullanılabilir: tuvaldeki boyayı karıştırır.";
            if (controller.config.allowTestRefill)
                modal.Show("Tüp boş", body, -1, "TEST: DOLDUR", () => { controller.Inventory.Refill(color); controller.Inventory.FlushNotifications(); Lock(false); },
                    "TAMAM", () => Lock(false));
            else modal.Show("Tüp boş", body, -1, "TAMAM", () => Lock(false));
        }

        private void Finish()
        {
            if (finishing || !HasPaint) return;
            finishing = true;
            Lock(true);
            Hint("TABLO DEĞERLENDİRİLİYOR");
            if (PaintingSession.ActiveIsFreePainting || comparer == null) StartCoroutine(Complete(false, 0));
            else comparer.Test();
        }

        private void OnScored(float score) { if (finishing) StartCoroutine(Complete(true, score)); }
        private void OnScoreFailed() { if (finishing) StartCoroutine(Complete(false, 0)); }

        private IEnumerator Complete(bool scored, float score)
        {
            // Let wet heaps finish their last settle step and hide the cursor before the photo.
            yield return null;
            if (controller.cursor != null) controller.cursor.HideNow();
            yield return new WaitForEndOfFrame();
            Texture2D image = PaintingCapture.CapturePaper(controller.mainCamera, surface.GetComponent<SpriteRenderer>());
            if (image != null)
            {
                try { PaintingGallery.Save(image, PaintingSession.ActiveTitle, PaintingSession.ActiveId, scored, score); }
                finally { Destroy(image); }
            }
            RefreshCounter();
            int count = PaintingGallery.Items.Count, needed = controller.config.artworksForExhibition;
            string stock = count >= needed ? "Sergi açmak için yeterli eserin var!" : "Sergi için " + (needed - count) + " eser daha.";
            string body = (scored ? "Eşleşme %" + Mathf.RoundToInt(score) + "\n" : "Serbest çalışma\n") +
                          "Eser atölyeye kaldırıldı. " + stock;
            modal.Show("Tablo bitti!", body, scored ? controller.config.StarsFor(score) : -1,
                "YENİ TABLO", OpenNewPainting, "TEKRAR", () => PaintingSession.StartNewPainting(PaintingSession.SelectedId));
        }

        private void OpenNewPainting()
        {
            if (gallery == null) { PaintingSession.StartNewPainting(PaintingSession.SelectedId); return; }
            // The finished canvas is already stored; closing the picker without a choice starts the same target fresh.
            gallery.Closed -= RestartAfterPicker;
            gallery.Closed += RestartAfterPicker;
            gallery.Open(GalleryPanel.Tab.NewPainting);
        }

        private void RestartAfterPicker()
        {
            gallery.Closed -= RestartAfterPicker;
            if (!gallery.IsStartingPainting) PaintingSession.StartNewPainting(PaintingSession.SelectedId);
        }

        private void Lock(bool locked)
        {
            controller.InputLocked = locked || finishing;
            RefreshButtons();
        }
    }
}
