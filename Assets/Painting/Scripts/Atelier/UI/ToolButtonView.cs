using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    // One slot in the tool tray. Cloned from the authored template for every tool in AtelierConfig.
    public sealed class ToolButtonView : MonoBehaviour
    {
        public Button button;
        public Image icon;
        public TMP_Text label;
        public Graphic selectedGlow;
        public RectTransform lift;
        [Tooltip("How far the selected tool rises, as a fraction of the slot height.")]
        public float liftAmount = 0.12f;

        public PaintToolDefinition Tool { get; private set; }
        private Tween tween;

        public void Setup(PaintToolDefinition tool, UnityEngine.Events.UnityAction onClick)
        {
            Tool = tool;
            name = "Tool " + tool.id;
            if (icon != null) { icon.sprite = tool.icon; icon.enabled = tool.icon != null; icon.preserveAspect = true; }
            if (label != null) label.text = tool.displayName;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            SetSelected(false, true);
        }

        public void SetSelected(bool selected, bool instant = false)
        {
            if (selectedGlow != null) selectedGlow.enabled = selected;
            if (lift == null) return;
            tween?.Kill();
            float height = ((RectTransform)transform).rect.height;
            Vector2 goal = new Vector2(0, selected ? height * liftAmount : 0);
            if (instant) lift.anchoredPosition = goal;
            else
            {
                tween = DOTween.Sequence()
                    .Append(lift.DOAnchorPos(goal, 0.18f).SetEase(Ease.OutBack))
                    .Join(lift.DOPunchRotation(new Vector3(0, 0, selected ? 8 : 0), 0.3f, 8));
            }
        }

        private void OnDestroy() => tween?.Kill();
    }
}
