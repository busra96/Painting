using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Atelier
{
    public sealed class GalleryArtView : MonoBehaviour
    {
        public RawImage picture;
        public TMP_Text caption;
        public AspectRatioFitter aspect;
        public void Show(GalleryArtwork art, Texture2D texture)
        {
            picture.texture = texture; picture.enabled = texture != null;
            if (aspect != null && texture != null) aspect.aspectRatio = texture.width / (float)texture.height;
            if (caption != null) caption.text = art == null ? "Yeni bir hikâye bekliyor" : art.title + "\n" + art.price + " altın • " +
                (art.status == ArtworkStatus.Unsold ? "SATILMADI" : art.status == ArtworkStatus.Exhibited ? "SERGİDE" : "DEPODA");
        }
    }
}
