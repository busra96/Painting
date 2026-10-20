using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PaintingGame.Gallery
{
// One card in the gallery grid. The authored template is cloned for each painting.
public sealed class GalleryItemView : MonoBehaviour
{
    public Button button;
    public RawImage picture;
    public AspectRatioFitter pictureFitter;
    public TMP_Text caption;
    public TMP_Text detail;
    public GameObject currentMark;

    public void SetPicture(Texture texture, Rect uv, float aspect)
    {
        if (picture == null) return;
        picture.texture = texture != null ? texture : Texture2D.whiteTexture;
        picture.uvRect = uv;
        if (pictureFitter != null) pictureFitter.aspectRatio = aspect > 0 ? aspect : 1;
    }

    public static Rect SpriteUV(Sprite sprite)
    {
        if (sprite == null) return new Rect(0, 0, 1, 1);
        Rect r = sprite.textureRect;
        return new Rect(r.x / sprite.texture.width, r.y / sprite.texture.height,
            r.width / sprite.texture.width, r.height / sprite.texture.height);
    }
}
}
