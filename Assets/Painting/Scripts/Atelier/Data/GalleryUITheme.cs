using TMPro;
using UnityEngine;

namespace PaintingGame.Atelier
{
    [CreateAssetMenu(menuName="Painting/Gallery/UI Theme")]
    public sealed class GalleryUITheme : ScriptableObject
    {
        public Sprite paper,primary,secondary,card,rail,monitor,label,frame;
        public TMP_FontAsset readableFont;
        public Color ink=new Color(.19f,.14f,.09f);
        public Color ivory=new Color(.98f,.94f,.82f);
    }
}
