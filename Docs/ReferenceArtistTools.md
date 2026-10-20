# Referans sanatçı aletleri

Aktif sahne: `Assets/Painting/Scenes/GalleryAtelier.unity`.

Referans fotoğraftaki turuncu ahşap saplı fırçalar, siyah kalem ve açık saplı metal palet bıçakları oyun alet setine uyarlandı. Baskı bu sahnenin veri listesinden ve alet çubuğundan çıkarıldı. Tüp boya sıkma aracı korunur.

| Alet | Kullanım |
| --- | --- |
| Düz 12 | Geniş, düz yağlı boya izi |
| Düz 6 | Orta genişlikte düz boya izi |
| Yuvarlak | Yuvarlak uçla daha dar çizgi |
| İnce Uç | Küçük detay ve ince çizgiler |
| Eskiz | Tüp boyası tüketmeden ince grafit rehber çizgileri |
| Damla | Damla/rombik uçla boya taşıma |
| Geniş Palet | Geniş, asimetrik metal bıçakla yayma |
| Oval Palet | Yuvarlatılmış, uzun temas alanı |
| Dar Palet | Dar ve düz kenarlı boya taşıma |
| Sivri Palet | Uzun sivri uçla küçük bölgelerde çalışma |
| Tüp | Tuvale yeni yağlı boya sıkma |

Fırçalar seçili tüpten gerçek miktarda boya harcar. Kalem ayrı ince grafit izi üretir; sıfır dolulukta da çalışır ve undo boya iadesi oluşturmaz. Palet bıçakları tuvaldeki boyayı taşır; kaldırma anında yeni bir şekil eklemez. Bıçak verilerindeki uzunluk, sivrilik, asimetri ve kenar kavisi gerçek temas alanını etkiler.

## Düzenleme

- `Settings/Gallery/GalleryPainting.asset`: alet listesi ve başlangıç seçimi.
- `Settings/Gallery/Oil_*.asset`: tek tek fırça/bıçak boyutları, boya harcama ve geometri ayarları.
- `Art/Gallery/ReferenceArtistTools.png`: transparan ortak atlas.
- `Art/Gallery/ReferenceTool_*`: tam alet ikonları, temas ucu cursor Sprite'ları ve renklendirilen boya uçları.
- Sahne Canvas'ı, atölye sayfası, `Artist Tool Rack / Viewport / Editable Tool Buttons`: yatay kaydırılabilir alet çubuğu; bütün düğmeler sahnede saklanır. Çalışma anında arayüz nesnesi üretilmez.

Bir defalık kurulum: `Tools > Painting > Apply Reference Artist Tool Set`. Bu araç referans alet asset'lerini ve alet çubuğunu günceller; günlük değişiklikler doğrudan sahne ve ScriptableObject'ler üzerinden yapılabilir.

Atlasın CPU piksel erişimi sadece Sprite sınırlarını oluştururken açılır, işlem bitince kapatılır. Böylece mobil oyuncuda kullanılmayan okunabilir doku kopyası tutulmaz.

## Doğrulama

GalleryGameChecks **224**, AtelierToolChecks **8**, PaintingMechanicChecks **76**, PaintHistoryChecks **19**: toplam **327 kontrol** geçti. Gerçek pointer sürüklemesiyle alet çubuğu kaydırma ve en son aleti tıklama; dört farklı fırça izi ve tüp tüketimi/undo; boyasız kalem çizimi; bıçak geometrileri, hacim/pigment korunumu, kaldırmada değişmeyen yüzey; sahnede eksik script olmaması ve legacy hedef boyama modu doğrulandı. Loglar `Logs/*-ReferenceFinal.log` altında. Fiziksel Android cihaz testi yapılmadı.

720×1280 ve 720×1560 render'ları incelendi. `Docs/Previews/GalleryAtelier-ReferenceBrushes.png` ve `GalleryAtelier-ReferenceKnives.png` gerçek oyun arayüzünü gösterir.

## Görsel üretimi

Yerleşik imagegen aracı kullanıldı. Kaynak fotoğraf yalnızca alet biçimi ve malzeme referansıdır. İlk atlasın ardından kenar temizliği istendi; seçilen son PNG alpha kanalı korunarak proje içine kopyalandı. Sprite ayırma Unity Sprite asset'leriyle yapılır; boyama, kesme veya arka plan silme için harici bir bitmap betiği kullanılmadı.

Üretim promptu:

Create a production sprite atlas for a mobile oil-painting game. The attached photograph is a SHAPE/MATERIAL REFERENCE only, not an image to reproduce as a photo. Exactly TEN isolated full-length artist tools on a genuinely transparent alpha background, arranged in an EXACT regular FIVE COLUMNS by TWO ROWS grid; each tool is upright with working tip at top, handle at bottom, one tool centered per cell, equal cell dimensions and generous empty margins. No overlap, no cast shadow outside the tools, no background surface, no text or branding, no press/stamp/sponge. Restrained hand-painted gouache game art, clean accurate recognizable silhouettes, warm orange varnished wood brush handles, brass ferrules, warm tan bristles, pale ivory palette-knife handles and satin silver steel blades with gentle warm highlights. Crisp edges, modest painterly brushwork, practical believable art supplies, no ornate swirls or blobs, no thick paint already on the tools. TOP ROW left-to-right: 1 broad flat square-end oil brush; 2 medium flat square-end oil brush; 3 small pointed round brush; 4 extremely fine long liner/detail brush; 5 sharpened black graphite drawing pencil with yellow band near eraser. BOTTOM ROW left-to-right: 6 small pointed diamond/teardrop palette knife with thin offset metal neck; 7 broad asymmetric angular trapezoid palette knife; 8 medium rounded oval straight palette knife; 9 long narrow parallel-sided rectangular palette knife with subtly rounded tip; 10 slender long pointed leaf palette knife. Every blade/brush head must be visibly distinct and match the reference shapes. Flat front-on orthographic view, consistent scale and soft upper-left light. All ten handles completely visible. Output atlas 5 columns and 2 rows, with transparent gaps; no frames and no labels.

Kenar düzenleme promptu:

Clean this exact game sprite atlas. Preserve the exact five columns by two rows layout and preserve all TEN tools, their shapes, materials, colors, positions, scale, full handles and hand-painted style. Remove ALL red, yellow, white and gray stray pixels, noisy halos, fuzzy fringes and paint speckles around and between the objects. Make the silhouette edges clean with natural antialiasing, against a genuinely transparent alpha background. Especially remove red/yellow fringe beside the orange wood handles and all white/yellow contamination around ivory knives. Every gap must be completely empty transparent. Do not add shadows or a surface or a background color. Do not add, remove, rearrange or redesign any tool. No text.
