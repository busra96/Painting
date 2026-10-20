# Gallery Atelier doğrulaması — 20 Ekim 2026

Unity 6000.3.8f1, Windows Editor, GPU açık batch Play Mode. Oyuncu kayıtları yerine `Logs` altında ayrı test klasörleri kullanıldı.

| Kontrol grubu | Geçen |
| --- | ---: |
| GalleryGameChecks | 127 |
| AtelierToolChecks | 8 |
| AtelierShopChecks | 77 |
| PaintingMechanicChecks | 76 |
| PaintHistoryChecks | 19 |
| WorkshopVisualChecks | 66 |
| AssetLayoutChecks | 11 |
| Toplam | 384 |

Son loglar `Logs/<KontrolSınıfı>-Final.log` içinde. Tüm gruplar exit code 0 ile tamamlandı; ilgili kaynaklarda `git diff --check` geçti.

Yeni galeri kontrolleri: 15 eserlik stok koşulu; sergi dışında satış olmaması; süreli sergi ve satılmayan eserlerin dönüşü; tercih/bütçe ve kampanya; stok ve para işlemleri; mükerrer ödeme olmaması; tüp satın alma ve undo sınırı; diske yazma hatasında geri alma; bozuk ana JSON'dan fiziksel taslağıyla birlikte yedek kurtarma; boyama sırasında arka plan satışının taslak/tüp nesillerini karıştırmaması.

Boyama kontrolleri: gerçek EventSystem fare ve dokunma jestleri; farklı yönlerde spatula; baskıda hacim/pigment korunumu; tam vuruş geri alma; bağımsız tüp tüketimi; taslağın yeniden yüklenmesi; gerçek render'ın esere kaydı. Kırpılmış tuval UV kontrolünde damla, beklenen noktada 24×24 piksel ölçüldü.

Performans örneği: geniş fırçanın gerçek çalışma yarıçapıyla fırça+baskı çekirdeği yaklaşık **0,32 ms/işlem çifti**, ısınma sonrası **0 B managed allocation**. Bu değer bütün kare süresi veya Android FPS ölçümü değildir.

720×1280, 720×1560 ve 768×1024 ekran oranlarında görüntüler üretildi. Atölye, koleksiyon, Mağaza ve Reklam panelleri; perspektif duvar tabloları; mobil düzen ve karakter parçaları görsel olarak incelendi. Son örnekler `Docs/Previews/GalleryAtelier-*.png` altında. Konsept ve kullanılan raster asset'ler yerleşik imagegen ile üretildi; promptlar `Docs/GalleryArtPrompts.md` içinde.

Fiziksel Android cihazında APK/FPS testi yapılmadı. Hedef görsele benzetme sahnesi ve önceki AtelierShop prototipi build listesinde korunuyor; bu yeni döngüyü denemek için `Assets/Painting/Scenes/GalleryAtelier.unity` açılmalı.

## 20 Ekim: spatula teması ve boya yükü

Spatula revizyonundan sonra GalleryGameChecks 152, AtelierToolChecks 8, PaintingMechanicChecks 76, WorkshopVisualChecks 66 ve PaintHistoryChecks 19 yeniden çalıştırıldı: **321 kontrol**, beş süreç de exit 0. Loglar `Logs/*-KnifeVerified.log`; davranış ve performans ayrıntıları [PaletteKnifeRevision.md](PaletteKnifeRevision.md) içinde. Bu sayım, yukarıdaki önceki yedi gruplu doğrulamanın yerine geçen bir toplam değildir; son revizyonda tekrar çalıştırılan beş grubu gösterir.

## Kaldırma izi ve baskı pivotu düzeltmesi

Kullanıcının uzun kahverengi şerit örneğiyle yeni bir görsel regresyon eklendi. Kaldırma öncesi/sonrası render'da **0 piksel değişiyor**. Üç farklı noktada yeni baskı temasının mouse konumunda olduğu, çap ve pivotun fiziksel alanla eşleştiği, bırakınca eski konumda görsel kalmadığı doğrulandı.

Bu düzeltmede açık Unity Editor içinde GalleryGameChecks **163**, AtelierToolChecks **8** yeniden geçti; toplam **171**. Test kayıtları oyuncu profilinden ayrıdır. Güncel model ve ölçümler [PaletteKnifeRevision.md](PaletteKnifeRevision.md) içinde.
