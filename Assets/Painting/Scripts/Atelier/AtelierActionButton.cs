using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public enum AtelierAction { Tab, Tool, Color, Canvas, Palette, Expand, NewCanvas, Exhibit, Undo, Clear, Challenge }

    [RequireComponent(typeof(Button))]
    public sealed class AtelierActionButton : MonoBehaviour
    {
        public AtelierShopController owner;
        public AtelierAction action;
        public int index;
        public TMP_Text label;
        private Button button;
        private void Awake() => button = GetComponent<Button>();
        private void OnEnable() => GetComponent<Button>().onClick.AddListener(Invoke);
        private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Invoke);
        private void Invoke() { if (owner != null) owner.Act(action, index); }

        public void Refresh()
        {
            if (owner == null || owner.Store == null) return;
            if (button == null) button = GetComponent<Button>();
            AtelierProgress data = owner.Store.Data;
            bool owned;
            string text = null;
            switch (action)
            {
                case AtelierAction.Tool:
                    owned = owner.Store.ToolUnlocked((AtelierTool)index);
                    text = (owned && owner.painter.Tool == (AtelierTool)index ? "• " : "") + AtelierRules.ToolNames[index] +
                           (owned ? "" : "\n" + AtelierRules.ToolPrices[index] + " ALTIN");
                    break;
                case AtelierAction.Color:
                    owned = index < 4 || data.expandedPalette;
                    text = !owned ? "90" : owner.SelectedColor == index ? "•" : "";
                    break;
                case AtelierAction.Canvas:
                    text = AtelierRules.CanvasNames[index] + "\n" + (index > data.unlockedCanvasTier
                        ? "AÇ: " + AtelierRules.CanvasUnlockPrices[index]
                        : (index == data.selectedCanvasTier ? "SEÇİLİ • " : "SEÇ • ") + AtelierRules.CanvasPrices[index] + " / TUVAL");
                    break;
                case AtelierAction.Palette:
                    text = data.expandedPalette ? "ÖZEL PİGMENTLER\nALINDI" : "ÖZEL PİGMENTLER\n90 ALTIN";
                    button.interactable = !data.expandedPalette;
                    break;
                case AtelierAction.Expand:
                    text = data.shopLevel >= 5 ? "MAĞAZAN TAMAMEN AÇIK" : "MAĞAZAYI GENİŞLET\n" + AtelierRules.ExpansionPrice(data.shopLevel) + " ALTIN";
                    button.interactable = data.shopLevel < 5;
                    break;
                case AtelierAction.NewCanvas:
                    text = "YENİ TUVAL • " + AtelierRules.CanvasPrices[data.selectedCanvasTier];
                    button.interactable = owner.Ready && data.activeCanvasTier < 0;
                    break;
                case AtelierAction.Exhibit:
                    button.interactable = owner.Ready && data.activeCanvasTier >= 0 && owner.painter.UsedPaint > 0;
                    break;
                case AtelierAction.Undo:
                    button.interactable = owner.Ready && owner.painter.CanUndo;
                    break;
            }
            if (text != null && label != null && label.text != text) label.text = text;
        }
    }
}
