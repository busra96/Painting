# Düzenlenebilir atölye UI

Sahne: `Assets/Test2D/Scenes/Test 1.unity`, hiyerarşi: `Canvas/Workshop`.

| Nesne | Düzenleme |
| --- | --- |
| Background | Arka plan Image sprite'ı ve rengi |
| Target Card/Target Artwork | Hedef resmin sprite'ı, konumu ve ölçüsü |
| Amount Panel/Title | Amount başlığı, font ve rengi |
| Amount Panel/Amount Segments | Yedi ayrı miktar Image'ı ve konumları |
| Status Panel/Status Text | Durum yazısının fontu, rengi ve ölçüsü |
| Canvas Frame | Boyama çerçevesinin sprite'ı ve RectTransform'u |
| Canvas Frame/Paint Area | Gerçek boyanın eşleneceği alan; çerçeveyi taşırken otomatik birlikte taşınır |
| Palette Panel/Red…Orange | Altı ayrı Button + Image; sprite, tint ve yerleşim |
| Palette Panel/Selected Paint | Seçim işaretinin görünümü ve ilk boyaya göre uzaklığı |
| Done Button/Button Label | Buton sprite'ı, fontu ve yazı stili |

`UIManager` durum ve Done/Retry metnini oyun aşamasına göre değiştirir. Boya rengini `ColorSelection` üzerindeki pigment renklerinden, düğme görünümünü düğmenin Image tint'inden ayrı ayrı değiştirebilirsiniz. `WorkshopVisualController` yeni UI nesnesi oluşturmaz; miktar renklerini ve seçim işaretini günceller. Miktarın dolu/boş renkleri bu bileşende Inspector'dan ayarlanır.

Eski tek parça UI arka planı ve Retry örtüsü kaldırılmıştır. Görseller `Resources` yüklemesi yerine sahnedeki sprite referanslarıyla kullanılır. `WorkshopSceneAuthoring` mevcut bir Workshop'u yeniden kurmaz; elle yapılan tasarımı silmez.

## Görseller ve üretim

Üç görsel yerleşik `image_gen` aracıyla üretilmiştir. İnsan tarafından çizildiği iddia edilmez; kullanıcı isteğine göre mat guaj, fırça izleri, hafif düzensiz kenarlar ve daha sakin ışıkla el boyaması görünümü hedeflenmiştir. Kaynak PNG şeffaflığı korunur; panel ve boya lekesi Unity'de Sprite olarak içe aktarılır. Panelin 9-slice sınırları importer'da düzenlenebilir.

### StudioGouache.png

Dosya: `Assets/Test2D/UI/Art/StudioGouache.png`. Referans, önceki atölyenin kompozisyonu ve sıcak renkleri için kullanılmıştır.

> Use case: style-transfer. Asset type: portrait 9:16 background illustration behind separately editable Unity game UI. Reference image: workshop layout and warm colors ONLY. Paint a quiet warm artist studio in traditional 2D gouache, visible opaque brush strokes on textured paper, simplified shapes, slightly imperfect perspective, restrained ochre, raw sienna, cream and sage palette, soft window light from upper right, a few leafy plants at edges and wooden workbench at bottom. Keep most central and upper area calm light plaster wall, for game panels to overlay. Only environment. Remove ALL interface panels, frames, easel canvas, pinned painting, paint blobs, progress meter and buttons from the reference. No text, no characters. Hand-illustrated storybook background, matte paint, deliberate brush shapes and limited detail. Avoid 3D render, plastic gloss, shiny bevels, photographic bokeh, cinematic blur, hyperrealistic textures, orange bloom, perfect gradients or excessive tiny decoration.

### PaintedFrame.png

Dosya: `Assets/Test2D/UI/Art/PaintedFrame.png`. Tuval, miktar, durum, palet ve buton için yeniden kullanılan ayrı sprite.

> Use case: stylized-concept. Asset type: blank scalable Unity UI panel sprite, hand-painted 2D gouache. Match the matte brush-painted artist-studio illustration in the reference. One isolated wide horizontal rectangular wooden frame, lightly rounded uneven corners, honey-brown wood rim with visible intentional flat brush strokes, dark umber hand-drawn edge, inner off-white warm paper panel. Outer frame occupies about 8 percent of height and 3 percent of width; large QUIET EMPTY CREAM CENTER, no writing or icons. Straight-on flat 2D view, roughly 3:1 aspect ratio. Entire frame visible on genuine transparent background, minimal padding. Restrained ochre, cream, raw sienna colors. Matte gouache paper texture and a few human irregularities, no glossy bevel, no 3D extrusion, no metallic gold, no photographic shadows, no cinematic glow, no extra objects, no text. This same editable sprite will be nine-sliced into an amount board, canvas frame, palette tray and Done button.

### MattePaintDab.png

Dosya: `Assets/Test2D/UI/Art/MattePaintDab.png`. Image tint ile renklenen boya düğmeleri ve miktar parçaları.

> Use case: stylized-concept. Asset type: tintable 2D Unity palette button sprite. Match the handmade matte gouache style of the studio and wooden frame references. One small isolated neutral warm-white paint dab seen from above: an irregular rounded puddle with three broad overlapping brush strokes and a little curled ridge. Simple chunky silhouette, subtle cool gray shadows painted by hand, a few broken dry-brush marks at edges. Limited tonal range so Unity can tint it into red, yellow, green, magenta, blue and orange. Matte actual paint, flat illustration on paper, charming handmade asymmetry. Centered circular composition with minimal transparent padding. Genuinely transparent background. No text, no palette board, no glossy specular streaks, no 3D render, no plastic, no extra objects, no cast shadow outside the dab.
