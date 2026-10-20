using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName = "Painting/Gallery/Rules")]
    public sealed class GalleryRules : ScriptableObject
    {
        public AtelierConfig painting;
        [Min(1)] public int exhibitionSize = 15;
        [Min(10)] public int exhibitionSeconds = 100;
        [Min(2)] public int visitorInterval = 8;
        [Min(0)] public int startingCoins = 280;
        [Min(1)] public int startingCanvases = 20;
        [Min(1)] public int canvasPackPrice = 35;
        [Min(1)] public int canvasesPerPack = 10;
        [Min(1)] public int expansionBasePrice = 160;
        [Min(1)] public int decorationBasePrice = 95;
        [Min(1)] public int advertisingPrice = 45;
        [Min(1)] public int visitorBudget = 160;
        [Range(0, 1)] public float basePurchaseChance = 0.58f;
        [Range(0.005f,0.3f)] public float minimumCoverage = 0.035f;
        public CustomerProfile[] customers;
        public int ExpansionCost(int level) => expansionBasePrice * (level + 1);
        public int DecorationCost(int level) => decorationBasePrice * (level + 1);
        public int ArtworkPrice(float coverage, int techniques) =>
            Mathf.RoundToInt(32 + Mathf.Sqrt(Mathf.Clamp01(coverage)) * 76 + Mathf.Min(4, techniques) * 5);
    }
}
