using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public sealed class AtelierArtworkCard : MonoBehaviour
    {
        public RawImage picture;
        public TMP_Text caption;
        public AspectRatioFitter aspect;

        public void Show(AtelierArtwork artwork, Texture2D texture, int slot)
        {
            picture.texture = texture;
            picture.enabled = texture != null;
            if (texture != null) aspect.aspectRatio = texture.width / (float)texture.height;
            caption.text = artwork != null ? artwork.title + "\n" + artwork.price + " ALTIN" :
                "BOŞ SERGİ ALANI\n" + (slot + 1);
        }
    }
}
