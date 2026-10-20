using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    // One reusable popup (result, empty tube, confirmations) on the overlay canvas.
    public sealed class ModalView : MonoBehaviour
    {
        public GameObject root;
        public RectTransform card;
        public TMP_Text title;
        public TMP_Text body;
        public Image[] stars;
        public Color starOn = new Color(1f, 0.78f, 0.15f);
        public Color starOff = new Color(0.55f, 0.48f, 0.40f, 0.45f);
        public Button primary, secondary;
        public TMP_Text primaryLabel, secondaryLabel;

        public bool IsOpen => root != null && root.activeSelf;
        public event Action Closed;
        private Action onPrimary, onSecondary;
        private Sequence sequence;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (primary != null) primary.onClick.AddListener(() => Choose(onPrimary));
            if (secondary != null) secondary.onClick.AddListener(() => Choose(onSecondary));
        }

        private void OnDestroy() => sequence?.Kill();

        // stars < 0 hides the star row.
        public void Show(string heading, string text, int starCount, string primaryText, Action primaryAction,
            string secondaryText = null, Action secondaryAction = null)
        {
            title.text = heading;
            body.text = text;
            onPrimary = primaryAction; onSecondary = secondaryAction;
            primaryLabel.text = primaryText;
            bool hasSecondary = !string.IsNullOrEmpty(secondaryText);
            secondary.gameObject.SetActive(hasSecondary);
            if (hasSecondary) secondaryLabel.text = secondaryText;
            root.SetActive(true);
            sequence?.Kill();
            card.localScale = Vector3.one * 0.7f;
            sequence = DOTween.Sequence().Append(card.DOScale(1, 0.32f).SetEase(Ease.OutBack));
            for (int i = 0; i < stars.Length; i++)
            {
                Image star = stars[i];
                star.gameObject.SetActive(starCount >= 0);
                if (starCount < 0) continue;
                bool on = i < starCount;
                star.color = starOff;
                star.rectTransform.localScale = Vector3.one;
                if (!on) continue;
                // Stars land one after another for a small reward moment.
                star.rectTransform.localScale = Vector3.one * 0.4f;
                sequence.Append(star.rectTransform.DOScale(1.35f, 0.14f))
                    .Join(star.DOColor(starOn, 0.14f))
                    .Append(star.rectTransform.DOScale(1, 0.12f));
            }
        }

        public void Hide()
        {
            sequence?.Kill();
            if (root != null) root.SetActive(false);
            Closed?.Invoke();
        }

        private void Choose(Action action)
        {
            Hide();
            action?.Invoke();
        }
    }
}
