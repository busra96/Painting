using UnityEngine;

namespace PaintingGame.Atelier
{
    // One oil colour sold in tubes. Pigment drives the simulation; the market will use price/capacity.
    [CreateAssetMenu(menuName = "Painting/Atelier/Paint Color", fileName = "Color_")]
    public sealed class PaintColorDefinition : ScriptableObject
    {
        [Tooltip("Stable save key. Do not rename after release.")]
        public string id = "red";
        public string displayName = "Kırmızı";
        [Tooltip("Palette button this colour is bound to in the workshop scene.")]
        public ColorType paletteSlot = ColorType.Red;
        public Color pigment = new Color(0.82f, 0.11f, 0.13f);
        [Min(1)] public float tubeCapacity = 80;
        [Tooltip("Fill level of a brand new save.")]
        [Range(0, 1)] public float startingFill = 1;
        [Min(0)] public int price = 25;
    }
}
