using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public sealed class GalleryToolControls : MonoBehaviour
    {
        public GalleryGameController game;
        public GameObject popup;
        public Button toggle,close;
        public Slider size,pressure;
        public TMP_Text sizeLabel,pressureLabel;
        private void Start()
        {
            size.minValue=.45f;size.maxValue=1.6f;
            pressure.minValue=0;pressure.maxValue=1;
            size.SetValueWithoutNotify(1);pressure.SetValueWithoutNotify(.75f);
            toggle.onClick.AddListener(Open);close.onClick.AddListener(Close);
            size.onValueChanged.AddListener(SizeChanged);pressure.onValueChanged.AddListener(PressureChanged);
            UpdateLabels();
        }
        private void Open()
        {
            if(!game.Ready)return;
            game.tools.EndStroke();game.tools.InputLocked=true;popup.SetActive(true);
        }
        private void Close(){popup.SetActive(false);game.tools.InputLocked=game.CurrentPage!=1;}
        private void SizeChanged(float _) => Apply();
        private void PressureChanged(float _) => Apply();
        private void Apply(){game.tools.SetControls(size.value,pressure.value);UpdateLabels();}
        private void UpdateLabels()
        {
            sizeLabel.text="Alet genişliği  •  %"+Mathf.RoundToInt(size.value*100);
            pressureLabel.text="Baskı kuvveti  •  %"+Mathf.RoundToInt(pressure.value*100);
        }
        private void OnDestroy()
        {
            if(toggle!=null)toggle.onClick.RemoveListener(Open);if(close!=null)close.onClick.RemoveListener(Close);
            if(size!=null)size.onValueChanged.RemoveListener(SizeChanged);if(pressure!=null)pressure.onValueChanged.RemoveListener(PressureChanged);
        }
    }
}
