using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Gallery/Customer")]
    public sealed class CustomerProfile : ScriptableObject
    {
        public string displayName = "Sanatsever";
        public Color preferredColor = Color.red;
        [Range(0, 1)] public float colorPreference = 0.25f;
        [Range(0.5f, 2)] public float budgetMultiplier = 1;
        public Sprite portrait;
        public Sprite head, torso, leftLeg, rightLeg;
        public RuntimeAnimatorController animationController;
    }
}
