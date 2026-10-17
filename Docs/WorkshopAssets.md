# Düzenlenebilir atölye UI

Sahne: `Assets/Painting/Scenes/Painting.unity`, hiyerarşi: `Canvas/Workshop`.

Güncel tasarım, kullanıcının **Sıcak Stüdyoda Resim Paleti.png** referansındaki üstten masa görünümüne göre hazırlanmıştır. Tuval dokusu ve geri al/çöp kartları doğrudan verilen görselin Sprite bölgeleridir; arka plan ve ayrıştırılmış parçalar aynı referansa göre üretilmiştir. Miktar yazısı başlangıçta gerçek bütçeyi (`100%`) gösterir; referanstaki `60%` sabit bir resim değildir. Büyük durum paneli kaldırılmıştır; yayma/sonuç mesajları alt boşlukta görünür.

Panel içindeki parçalar oransal RectTransform anchor'ları kullanır; kamera/Canvas ölçeği değişince tüp ve palet taşmaz. Başlık, yüzde, durum ve DONE/RETRY yazılarında `WorkshopTextScale/Font Size At 941`, referans genişlikteki düzenlenebilir yazı boyutudur. Font seçimi ve rengi TMP bileşeninde kalır. Ekran görüntüsü kontrolleri geçici render hedefinin gerçek ölçeğini kullanır ve tüp/palet parçalarının panel içinde kaldığını ayrıca sınar.

| Nesne | Düzenleme |
| --- | --- |
| Background | Arka plan Image sprite'ı ve rengi |
| Target Card/Target Artwork | Hedef resmin sprite'ı, konumu ve ölçüsü |
| Amount Panel/Title Tape/Title | Amount başlığı, font ve rengi |
| Amount Panel/Paint Tube | Sabit kalan metal tüp içinde boya miktarını gösteren seviye; RectTransform, folyo/etiket/çizgi renkleri ve doluluk önizlemesi |
| Amount Panel/Percent Tape/Remaining Paint | Gerçek kalan boyanın yüzde metni, fontu ve yerleşimi |
| Status Text | Yayma ve sonuç yazısının fontu, rengi ve ölçüsü |
| Canvas Frame | Boyama çerçevesinin sprite'ı ve RectTransform'u |
| Canvas Frame/Paint Area | Gerçek boyanın eşleneceği alan; çerçeveyi taşırken otomatik birlikte taşınır |
| Palette Panel/Red…Orange | Altı ayrı Button + Image; sprite, tint ve yerleşim |
| Palette Panel/Selected Paint | Seçim işaretinin görünümü ve ilk boyaya göre uzaklığı |
| Done Button/Button Label | Buton sprite'ı, fontu ve yazı stili |
| Undo Button / Clear Button | Geri al ve tuvali temizle düğmelerinin Image'ı ve hit alanları |

`UIManager` durum ve Done/Retry metnini oyun aşamasına göre değiştirir. Boya rengini `ColorSelection` üzerindeki pigment renklerinden, düğme görünümünü düğmenin Image tint'inden ayrı ayrı değiştirebilirsiniz. `WorkshopVisualController` yeni UI nesnesi oluşturmaz; seçim işaretini günceller. `PaintTubeAmountView` gerçek `PaintAmount` bütçesini izler. Harcanan boya, sabit tüp gövdesinin içindeki pigment alanını azaltır; metal tüp ve ucu şekil değiştirmez. Seviye değişiminin yumuşaklığı Inspector'daki `Fill Response Speed` ile ayarlanabilir. `PaintTubeGraphic` içindeki `Preview Remaining` ile Edit Mode'da dolu/boş görünümü incelenebilir. Folyo, kağıt ve kontur renkleri Inspector'dan düzenlenebilir.

Tüp, `MetalTube.png` sprite'ını sabit bir UI mesh üzerinde gösterir ve pigmenti miktarla orantılı olarak iç yüzeyden azaltır. Metal folyo, gövde ve ağız tüm miktarlarda aynı şekli korur. Parçacık, çalışma anında nesne üretimi veya texture güncellemesi kullanmaz. `PaintTubeChecks.RunBatch` gerçek bütçe tüketimi, sabit tüp silueti, yarım/boş boya seviyesi, seçili pigment, spatula ilerlemesinden bağımsızlık ve bellek tahsisini kontrol eder; dolu, yarım ve boş ekran görüntülerini `Logs/WorkshopPreview` altına kaydeder.

`Canvas` üzerindeki `PaintHistory`, varsayılan olarak en fazla 8 çizgi ve yaklaşık 32 MB piksel verisi saklar. Yalnız çizgi başlangıcında dolu piksellerin boya hacmi, pigmenti, üst katmanı, katman sırası ve gevşeme durumu kaydedilir. Sürekli çizim karelerinde snapshot alınmaz. Geri al, harcanan boya ve çizim sayaçlarını da geri yükler. Çöp düğmesi tuvali/geçmişi temizler ve tüpü doldurur. DONE sonrasında iki düzenleme düğmesi kilitlenir ve geçmiş serbest bırakılır. `PaintHistoryChecks.RunBatch` katmanların tam geri yüklenmesini, boş tüpten geri almayı, bellek/çizgi sınırlarını ve bu aşama geçişini test eder.

**Tools → Painting → Apply Reference Tabletop Layout**, mevcut Workshop'u ilk kez referans düzenine dönüştürür; zaten dönüştürülmüş sahnede elle düzenlenen yerleşimi yeniden kurmaz. Sonrasında doğrudan Canvas çocuklarını düzenleyin. `PaletteGouache.mat`, boya Image tint'ini korurken nötr sprite'ın açık renkli fırça izlerini okunur tutar. `Fonts/Kalam SDF.asset`, [Kalam](https://github.com/google/fonts/tree/main/ofl/kalam) fontunun sabit atlasıdır; kaynak font ve SIL OFL lisansı aynı klasördedir.

Eski tek parça UI arka planı ve Retry örtüsü kaldırılmıştır. Görseller `Resources` yüklemesi yerine sahnedeki sprite referanslarıyla kullanılır. `WorkshopSceneAuthoring` mevcut bir Workshop'u yeniden kurmaz; elle yapılan tasarımı silmez.

## Görseller ve üretim

### Güncel masa parçaları

Son dosyalar **`Assets/Painting/Art/UI/`** altındadır: `TabletopBackground.png`, `TabletopPlank.png`, `MetalTube.png`, `PaletteDab.png`, `TargetPaper.png`, `DonePlate.png`. Bunlar yerleşik `image_gen` ile, kullanıcının görseli referans alınarak oluşturulmuştur; insan tarafından çizildiği iddia edilmez. Şeffaf PNG alfaları korunmuştur. `*Trimmed.asset` Sprite'ları PNG'yi değiştirmeden şeffaf kenar boşluğunu içe aktarma aşamasında sınırlar; texture okuma izni sonrasında kapatılır.

`TabletopReference.png`, kullanıcının verdiği görselin projedeki kopyasıdır. `ReferenceCanvas.asset`, `UndoCard.asset`, `TrashCard.asset`, `PaperTape.asset` aynı texture'ın ayrı Sprite bölgeleridir. UI kontrolü tüm arka plana gömülmez; sahnede ayrı Image/Button/TMP nesneleri olarak durur.

Üretim komutları (tümünde referans: **Sıcak Stüdyoda Resim Paleti.png**, yerleşik araç):

**TabletopBackground.png** — şeffaflık kapalı:

> Use case: precise-object-edit. Asset type: portrait 941:1672 background for a Unity painting game. Edit the reference, preserve EXACT overhead view, warm brown worn wood table, gouache brush strokes, lighting, edge decorations: water glasses at far left, brushes at far right, paint-smeared edge paper scraps, soft foreground plant at lower left. Remove ALL interface objects: top left pinned target card, top Amount plank/tube/labels, central white canvas, lower palette plank/paint dabs, Done button and undo/trash cards. In all removed areas continue the same brown hand-painted wooden tabletop, respecting the original plank grain and light. The large center must be bare wood, NO white canvas or rectangle. Keep composition and colors of the reference, not a new room. No text, no UI, no paint tube, no buttons, no palette, no target drawing. Entire portrait background filled, not transparent.

**TabletopPlank.png** — şeffaflık açık:

> Use case: background-extraction. Asset type: separate blank horizontal Unity UI plank. Reference role: exact style and material of the Amount plank and lower paint palette plank. Extract/recreate ONE pale raw wooden artist plank, hand-painted gouache, scuffed warm beige with tiny paint flecks and dark uneven edges, a few subtle screw heads near corners. Straight-on top view, rectangular long plank about 4.8:1, very slight imperfect rounded corners, full object visible, minimal padding. BLANK CENTER. Remove the tube, all paint dabs, labels, writing, symbols. Transparent background outside plank. Exact warm tactile brush-painted style of reference, no shiny 3D bevel, no gold trim, no additional objects.

**MetalTube.png** — şeffaflık açık:

> Use case: background-extraction. Asset type: one horizontal paint tube sprite for deformable Unity UI. Isolate the metal tube from the reference Amount panel, full object on true transparent background, NO plank, labels or text. EXACT reference gouache painting style, folded silver crimp on LEFT, gently bulging squarish metal tube body with rounded shoulder, threaded metal neck and ribbed BLACK CAP on RIGHT. Change red paint region to NEUTRAL warm ivory/silver so the game can tint the inner body dynamically. Interior body a broad quiet silver-cream panel bordered by metal rim, maintain brush marks and white hand-painted highlights like original. Top-down view, long horizontal about 4.5:1 aspect, tube occupies almost full image width, centered, minimum padding. Entire tube visible. No red pigment, no writing, no brand, no other objects, no photographic rendering, no glossy plastic, keep traditional gouache texture and fine dark uneven contour.

**PaletteDab.png** — şeffaflık açık:

> Use case: background-extraction. Asset type: tintable Unity paint button sprite. Isolate ONE paint dab matching the thick curled impasto dabs in the bottom palette of reference. Same top-down view, irregular round puddle and 3 broad swirled ridges, dry painted contour, tactile hand-painted gouache marks, a few thick ivory highlight strokes. Make it neutral WHITE/gray for recoloring via Unity Image tint: dark-medium gray shadows and nearly white raised ridges, do not bake yellow or warm orange. Full object visible with very little transparent padding. Actual transparent background. No wooden palette, no text, no other objects, no photographic 3D render, not a perfect spiral.

**TargetPaper.png** — şeffaflık açık:

> Use case: background-extraction. Asset type: separate Unity target card sprite. Isolate the top-left reference card including its metal paperclip at the upper edge, warm off-white textured paper, irregular hand-painted edges and a subtle small shadow. Remove all red/yellow/blue/green drawing inside, leave paper completely blank. Portrait rectangular card about 0.8:1 width/height, straighten its long edges to vertical so Unity can rotate it at layout time. Gouache/brush-painted illustration EXACTLY matching reference. Full object and paperclip visible, minimal padding, true transparent background. No table, no other objects, no text, no pins.

**DonePlate.png** — şeffaflık açık:

> Use case: background-extraction. Asset type: Unity UI Done button background. Extract/recreate ONLY the teal green button at bottom center of the reference, remove the DONE letters leaving a completely blank quiet center. Horizontal rectangle about 3.3:1, slightly rounded imperfect corners, dark fine outer outline and thin scuffed ochre/cream painted inner edge. Opaque desaturated green-blue paint with broad hand-painted brush marks and worn corners EXACTLY like reference. Straight-on overhead view. Full object visible, minimum transparent margin, true transparent background. No table, no text, no icons, no extra objects, no shiny plastic bevel, no 3D render.

### Önceki atölye tasarımının kaynakları

Önceki üç görsel yerleşik `image_gen` aracıyla üretilmiştir; güncel masa sahnesinde bunların yerine yukarıdaki parçalar kullanılır. Aşağıdaki komutlar eski tasarımın üretim kaydıdır.

### StudioGouache.png

Dosya: `Assets/Painting/Art/UI/StudioGouache.png`. Referans, önceki atölyenin kompozisyonu ve sıcak renkleri için kullanılmıştır.

> Use case: style-transfer. Asset type: portrait 9:16 background illustration behind separately editable Unity game UI. Reference image: workshop layout and warm colors ONLY. Paint a quiet warm artist studio in traditional 2D gouache, visible opaque brush strokes on textured paper, simplified shapes, slightly imperfect perspective, restrained ochre, raw sienna, cream and sage palette, soft window light from upper right, a few leafy plants at edges and wooden workbench at bottom. Keep most central and upper area calm light plaster wall, for game panels to overlay. Only environment. Remove ALL interface panels, frames, easel canvas, pinned painting, paint blobs, progress meter and buttons from the reference. No text, no characters. Hand-illustrated storybook background, matte paint, deliberate brush shapes and limited detail. Avoid 3D render, plastic gloss, shiny bevels, photographic bokeh, cinematic blur, hyperrealistic textures, orange bloom, perfect gradients or excessive tiny decoration.

### PaintedFrame.png

Dosya: `Assets/Painting/Art/UI/PaintedFrame.png`. Tuval, miktar, durum, palet ve buton için yeniden kullanılan ayrı sprite.

> Use case: stylized-concept. Asset type: blank scalable Unity UI panel sprite, hand-painted 2D gouache. Match the matte brush-painted artist-studio illustration in the reference. One isolated wide horizontal rectangular wooden frame, lightly rounded uneven corners, honey-brown wood rim with visible intentional flat brush strokes, dark umber hand-drawn edge, inner off-white warm paper panel. Outer frame occupies about 8 percent of height and 3 percent of width; large QUIET EMPTY CREAM CENTER, no writing or icons. Straight-on flat 2D view, roughly 3:1 aspect ratio. Entire frame visible on genuine transparent background, minimal padding. Restrained ochre, cream, raw sienna colors. Matte gouache paper texture and a few human irregularities, no glossy bevel, no 3D extrusion, no metallic gold, no photographic shadows, no cinematic glow, no extra objects, no text. This same editable sprite will be nine-sliced into an amount board, canvas frame, palette tray and Done button.

### MattePaintDab.png

Dosya: `Assets/Painting/Art/UI/MattePaintDab.png`. Image tint ile renklenen boya düğmeleri.

> Use case: stylized-concept. Asset type: tintable 2D Unity palette button sprite. Match the handmade matte gouache style of the studio and wooden frame references. One small isolated neutral warm-white paint dab seen from above: an irregular rounded puddle with three broad overlapping brush strokes and a little curled ridge. Simple chunky silhouette, subtle cool gray shadows painted by hand, a few broken dry-brush marks at edges. Limited tonal range so Unity can tint it into red, yellow, green, magenta, blue and orange. Matte actual paint, flat illustration on paper, charming handmade asymmetry. Centered circular composition with minimal transparent padding. Genuinely transparent background. No text, no palette board, no glossy specular streaks, no 3D render, no plastic, no extra objects, no cast shadow outside the dab.
