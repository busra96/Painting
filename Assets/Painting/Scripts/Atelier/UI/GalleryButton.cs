using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public enum GalleryAction { Page, Tool, Color, Finish, Undo, Clear, Open, Canvases, Tube, Expand, Decorate, Advertise, Discard, Filter, Next, Challenge }
    [RequireComponent(typeof(Button))]
    public sealed class GalleryButton : MonoBehaviour
    {
        private static readonly string[] CampaignNames = { "", "MAHALLE AFİŞLERİ", "SANAT BÜLTENİ", "ŞEHİR KAMPANYASI" };
        public GalleryGameController game;
        public GalleryAction action;
        public int index;
        public TMP_Text label;
        public Image selection;
        public Image fill;
        public Sprite idleSprite,selectedSprite;
        public Color idleText=Color.white,selectedText=Color.white;
        private Image background;
        private int lastPercent = -1;
        private void Start() { GetComponent<Button>().onClick.AddListener(Click); Refresh(); }
        private void Click() => game.Act(action, index);
        public void Refresh()
        {
            if (game == null || !game.Ready) return;
            bool selected = action == GalleryAction.Tool && game.tools.CurrentTool == game.rules.painting.tools[index] ||
                action == GalleryAction.Color && game.tools.CurrentColor == game.rules.painting.colors[index] ||
                action == GalleryAction.Page && game.CurrentPage == index;
            if (selection != null) selection.enabled = selected;
            if(idleSprite!=null)
            {
                if(background==null)background=GetComponent<Image>();
                Sprite desired=selected && selectedSprite!=null?selectedSprite:idleSprite;
                if(background.sprite!=desired)background.sprite=desired;
                if(label!=null)label.color=selected?selectedText:idleText;
            }
            if (action == GalleryAction.Color || action == GalleryAction.Tube)
            {
                var color = game.rules.painting.colors[index];
                float fraction = game.tools.Inventory.Fraction(color);
                if (fill != null) fill.fillAmount = fraction;
                int percent = Mathf.RoundToInt(fraction * 100);
                if (label != null && percent != lastPercent)
                { label.text = color.displayName + "\n" + percent + "%" + (action == GalleryAction.Tube ? " • " + color.price + " altın" : ""); lastPercent = percent; }
            }
            if (action == GalleryAction.Finish) GetComponent<Button>().interactable = game.Store.Data.activeCanvas && game.tools.surface.PaperVolume > 1e-6;
            if (action == GalleryAction.Open) { GetComponent<Button>().interactable = !game.Store.IsOpen && game.Store.StoredCount >= game.rules.exhibitionSize;
                GetComponent<Image>().enabled=!game.Store.IsOpen;label.enabled=!game.Store.IsOpen;
                label.text = game.Store.IsOpen ? "SERGİ DEVAM EDİYOR" : "SERGİ AÇ • " + game.Store.StoredCount + "/" + game.rules.exhibitionSize; }
            if (action == GalleryAction.Expand) label.text = "GALERİYİ GENİŞLET\n" + (game.Store.Data.expansion >= 4 ? "TAMAMLANDI" : game.rules.ExpansionCost(game.Store.Data.expansion) + " altın");
            if (action == GalleryAction.Decorate) label.text = "DEKORASYON & IŞIK\n" + (game.Store.Data.decoration >= 4 ? "TAMAMLANDI" : game.rules.DecorationCost(game.Store.Data.decoration) + " altın");
            if (action == GalleryAction.Canvases) label.text = game.rules.canvasesPerPack + " TUVAL • " + game.rules.canvasPackPrice + " ALTIN";
            if (action == GalleryAction.Advertise) label.text = CampaignNames[index] + "\n" + game.rules.advertisingPrice * index + " altın";
        }
    }
}
