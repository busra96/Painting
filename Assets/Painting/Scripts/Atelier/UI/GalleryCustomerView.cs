using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public sealed class GalleryCustomerView : MonoBehaviour
    {
        public Image body;
        public Animator animator;
        public Image head, torso, leftLeg, rightLeg;
        public TMP_Text bubble;
        public RectTransform bubbleRoot;
        private Sequence journey;
        public void Visit(CustomerProfile profile, bool purchased, int price, int lane)
        {
            journey?.Kill(); gameObject.SetActive(true);
            if (profile != null)
            {
                if (head != null)
                { head.sprite = profile.head; torso.sprite = profile.torso; leftLeg.sprite = profile.leftLeg; rightLeg.sprite = profile.rightLeg; }
                else body.sprite = profile.portrait;
                animator.runtimeAnimatorController = profile.animationController;
            }
            var root = (RectTransform)transform;
            root.anchoredPosition = new Vector2(510, 220);
            bubbleRoot.gameObject.SetActive(false);
            animator.SetBool("Walking", true);
            Vector2 target = new Vector2(220 + lane % 3 * 260, 700 + lane % 2 * 180);
            journey = DOTween.Sequence().SetUpdate(true)
                .Append(root.DOAnchorPos(target, 1.2f).SetEase(Ease.InOutSine))
                .AppendCallback(() => { animator.SetBool("Walking", false); bubbleRoot.gameObject.SetActive(true); bubble.text = purchased ? "Bayıldım!\n+" + price + " altın" : "Biraz düşüneyim…"; })
                .AppendInterval(1.7f)
                .AppendCallback(() => { bubbleRoot.gameObject.SetActive(false); animator.SetBool("Walking", true); })
                .Append(root.DOAnchorPos(new Vector2(560, 170), 1.2f).SetEase(Ease.InOutSine))
                .OnComplete(() => gameObject.SetActive(false));
        }
        private void OnDisable() => journey?.Kill();
        private void OnDestroy() => journey?.Kill();
    }
}
