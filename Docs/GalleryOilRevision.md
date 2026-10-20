# Yağlı boya hissi ve Artisan UI

Sahne: `Assets/Painting/Scenes/GalleryAtelier.unity`.

## Boya malzemesi

Galeri artık kendine ait `Settings/Gallery/GalleryOilPaint.asset` kullanır. Kabarıklık, ıslak yansıma ve temas gölgesi bu ayarlardan düzenlenir. `SculptedOilLighting` yeni yağlı boya ışığını açar; orijinal hedef sahnesinin ayarı korunur. Bir birim boyanın hacmi 0,16; tüpler ve kayıtlar harcanan gerçek birimleri izlemeye devam eder.

Fırçanın çalışma alanı daraltıldı; aynı boya daha belirgin kalınlık bırakır. Kıl sayısı ve gürültüsü, hücre ızgarası gibi görünmemesi için azaltıldı; damgalar daha sık örtüşür. Tüpün ucu daha küçük: bekleyerek boyayı kabartabilir, bu birikintiyi spatula veya baskıyla yayabilirsin. Tüp göstergesinin dış şekli sabittir.

## Spatula ve baskı

Spatulanın daha sonraki temas/yük/kenar ve performans düzenlemesi: [PaletteKnifeRevision.md](PaletteKnifeRevision.md).

- Spatula ince filmi tamamen kazımaz; fazlalığı taşır. Aynı hücrede tekrar eden alt piksel hareketleri boyayı tekrar tekrar toplamaz. Başlangıç temasını da işler.
- Spatula boyayı hareket sırasında tuval üzerinde taşır. Kaldırma anında yeni bir sırt/parça eklenmez; hacim ve pigment korunur. Güncel model ve görüntü karşılaştırması `PaletteKnifeRevision.md` içindedir.
- Baskı, temas alanındaki yüksekliği dikkate alarak yüksek boyayı düzleştirir. Merkezde sürekli oyuk açan radyal itme sınırlandı. Yerel ve korunumlu komşu alışverişi ızgara/yıldız izlerini yumuşatır.
- Sürüklenen baskının örnekleri kat edilen mesafeye bağlıdır; her karede sabit bir darbe uygulanmaz.
- **ALET AYARI** düğmesinden genişliği ve kuvveti değiştirebilirsin. Hafif spatula kalın film, güçlü spatula ince ve uzun film bırakır. Fırça basınçla genişler. Baskı aracı yüksek kuvvette daha hızlı düzleştirir.

## UI

Yeni UI materyalleri `Art/Gallery/ArtisanUI.png` atlasındadır. Ahşap tepsiler, pirinç kenarlar, boyalı yeşil levhalar, kâğıt etiketler ve tuval çerçevesi aynı görsel aileyi kullanır. Dil ve sayılar için Türkçe karakterleri içeren Liberation Sans tabanlı ayrı bir SDF font üretildi. Görseller sahnedeki Image bileşenlerine atanmıştır; çalışma anında panel üretilmez.

`Settings/Gallery/ArtisanUITheme.asset` sprite ve renkleri; `Oil_*.asset` dosyaları galeriye ait alet davranışlarını tutar. UI nesneleri `Gallery Canvas / Safe Area / Portrait Composition 1080x1920` altındadır. Ayar penceresi `Tool Settings Overlay` altında düzenlenebilir.

Bir defalık düzenleme aracı: `Tools > Painting > Apply Gallery Oil & Artisan UI`. Günlük UI değişiklikleri için sahnedeki nesneleri düzenle; eski prototip sahne üreticisini yeniden çalıştırmak gerekmez.

## Son doğrulama

Unity 6000.3.8f1 Play Mode: GalleryGameChecks 138, AtelierToolChecks 8, PaintingMechanicChecks 76, WorkshopVisualChecks 66; toplam **288 kontrol** geçti. Yeni kontroller baskının tepe yüksekliğini azaltırken merkezde boya bırakmasını, alet kuvvetinin fiziksel etkisini, eşit miktarın geniş alanda incelmesini, yavaş/hızlı spatula hacim ve kapsamasını, gerçek sürgü bağlantılarını ve UI sınırlarını doğrular. Fırça+baskı çekirdeği örneği yaklaşık 0,31 ms/çift, ısınma sonrası 0 B managed allocation (Windows Editor). Bu, Android FPS ölçümü değildir.

Loglar `Logs/*-OilFinal.log`; güncel görseller `Docs/Previews/GalleryAtelier-Workshop.png`, `GalleryAtelier-ToolSettings.png` ve `GalleryAtelier-OilRelief.png` altında. Fiziksel Android cihaz testi yapılmadı.

## Görsel üretimi ve prompt

Yerleşik imagegen kullanıldı. Yeni atlas, oyunda tüketilen Sprite asset'leriyle birlikte proje içine kaydedildi; genel galeri arka planı yeniden üretilmedi.

Üretim promptu:

Production UI sprite atlas for a hand-painted cozy isometric art gallery mobile game. 2048x2048 PNG, REAL transparent background. EXACT 2 columns by 4 rows regular grid, eight separate components, no text or icons anywhere. Each cell 1024x512; one rectangular component centered in each cell, fills about 920x410 with generous clear margin, all isolated. Beautiful restrained artisan gouache textures and dimensional painted bevels, soft light upper left, walnut wood, patinated brass, deep forest green, warm cream linen. Deliberate simplified material brushwork, crisp game-readable outlines, warm luxurious boutique gallery aesthetic, no glossy plastic, no ornamental AI swirls. Components reading left to right, top to bottom: 1 cream rag-paper panel with irregular torn edges and fine aged brass corner clips; 2 walnut wood plaque with recessed deep green painted face, understated brass thin edge, for main action button; 3 muted sage green painted wood panel with narrow ivory inset line and gently worn corners, secondary button; 4 cream linen padded art inventory card with soft raised walnut frame; 5 long dark walnut shallow tray with brass edge, empty dark interior, tool rack; 6 wooden computer monitor bezel with brass trim, completely empty warm cream screen; 7 narrow cream paper label taped to a thin wood backing, subtle edge shadow; 8 simple walnut canvas frame with thin warm ivory inner lip, completely clear transparent central opening. Make surfaces flat enough for legible UI text overlays. Consistent light and art style across eight elements, front-on orthographic view, no perspective tilt, no lettering, no symbols, no objects inside panels.
