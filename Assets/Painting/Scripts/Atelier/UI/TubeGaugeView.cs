using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    // Fill gauge sitting on a palette paint dab: shows what is left in that colour's tube.
    public sealed class TubeGaugeView : MonoBehaviour
    {
        public ColorType slot;
        public Image fill;
        public GameObject emptyMark;
        public Graphic lowWarning;

        private bool wasLow;
        private Tween pulse;

        public void Show(float fraction, float lowThreshold)
        {
            if (fill != null) fill.fillAmount = fraction;
            bool empty = fraction <= 0.0001f, low = !empty && fraction <= lowThreshold;
            if (emptyMark != null && emptyMark.activeSelf != empty) emptyMark.SetActive(empty);
            if (lowWarning == null || low == wasLow) return;
            wasLow = low;
            pulse?.Kill();
            lowWarning.enabled = low;
            if (!low) return;
            Color c = lowWarning.color; c.a = 1; lowWarning.color = c;
            pulse = lowWarning.DOFade(0.25f, 0.45f).SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDestroy() => pulse?.Kill();
    }
}
