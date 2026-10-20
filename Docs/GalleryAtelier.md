# Gallery Atelier

Yeni oyun sahnesi: `Assets/Painting/Scenes/GalleryAtelier.unity`. `Painting.unity` hedef görsele benzetme modunu, `AtelierShop.unity` önceki prototipi korur. Galeri oyunu için **GalleryAtelier** sahnesini açıp Play'e bas.

Kabarık yağlı boya, geliştirilmiş spatula/baskı ve yeni ahşap–pirinç UI düzeni için [GalleryOilRevision.md](GalleryOilRevision.md). **ALET AYARI** düğmesi, alet genişliğini ve baskı kuvvetini açar.

## Oyun döngüsü

- **Galeri:** atölyeye ve bilgisayara odanın içindeki etiketlerden veya alt menüden git.
- **Boya:** geniş/dar fırça, herhangi bir yöne çekilen spatula, yerel baskı ve tüp. Tuvale ilk dokunma stoktan tek bir tuval ayırır. Her rengin kendi tüpü vardır; boş tüp boya üretemez. Geri al son hareketin boyasını aynı tüpe iade eder. Temizlemek tüpleri doldurmaz.
- **Eseri bitir:** aydınlatılmış gerçek tuval görüntüsü ve fiyat depoya kaydedilir. Bu adım para kazandırmaz.
- **Sergi:** varsayılan 15 yeni eserle açılır. 100 saniyelik sergi boyunca farklı zevk ve bütçedeki ziyaretçiler gelir. Satın alan ziyaretçi tablonun bedelini öder. Sergi sonunda satılmayanlar ayrı depoya döner, sonraki serginin yeni eser koşuluna sayılmaz; kaldırılmaları para kazandırmaz.
- **Bilgisayar / Mağaza:** tuval paketi, ayrı boya tüpleri, galeri genişletmesi, dekorasyon ve ışık. Genişletme ziyaret aralığını, dekorasyon satın alma olasılığını etkiler.
- **Bilgisayar / Reklam:** bir sonraki sergi için üç kampanyadan biri seçilir. Reklam daha sık ziyaretçi getirir, satışı garanti etmez. Sergi bitince kampanya tükenir.

## Düzenlenebilir sahne ve veriler

Tüm paneller `Gallery Canvas / Safe Area / Portrait Composition 1080x1920` altında, sahnede düzenlenebilir. Çalışma anında ekran hiyerarşisi üretilmez. `Painting Workshop / Paint Gesture Area` üzerindeki saydam raycast Image ve `GalleryPaintInput` korunmalı. Dekoratif grafikler dokunmaları engellemez.

`Settings/Gallery/GalleryRules.asset`: ekonomi, başlangıç kaynakları, sergi eşiği/süresi, ziyaret aralığı, minimum tuval doluluğu, fiyatlar ve müşteri listesi. `Settings/Gallery/Customer_*.asset`: ad, renk tercihi, tercih ağırlığı, bütçe, sprite ve Animator. `Settings/Gallery/GalleryPainting.asset`: galeriye ait fırça ve renk listeleri. Beyaz ve toprak pigmentleri bu palete aittir; orijinal hedef oyununun renkleri değiştirilmez. Fırça şekli, kıl izi, boya tüketimi, baskı ve spatula davranışı ayrı ScriptableObject'lerde.

`Customer.controller` iki durumlu Unity Animator kullanır: yürüyüş ve tablo inceleme. Altı farklı karakter görünümü aynı denetleyiciyi paylaşır. Baş, gövde ve iki bacak ayrı sprite parçalarıdır; yürüyüşte bacaklar karşılıklı hareket eder, baş ve gövde salınır. SpriteSkin deformasyonu kullanılmaz. Hareket yolları havuzdaki altı sahne nesnesinde Tween ile oynar.

## Mimari ve mobil maliyet

- `GalleryStore`: Unity sahnesinden bağımsız ekonomi, stok, tüp envanteri ve sergi durumu. Para değişiklikleri atomik JSON kaydından sonra kesinleşir; başarısız yazma işlemi geri alınır. Ziyaret kararları, kayıttan çıkıp girerek yeniden zar atılmaması için kararlı girdilerden hesaplanır.
- `GalleryGameController`: bileşenleri bağlar, sayfaları ve kayıt akışını yönetir. `Update` kullanmaz. Sergi saati yalnızca sergi açıkken saniyelik coroutine ile çalışır. UI olaylarla yenilenir; kart dokuları sadece eser durumu değişince yüklenir ve görünmeyenler serbest bırakılır.
- `PaintToolController`: strateji nesnelerine Begin/Drag/End iletir. Yeni sahnede giriş EventSystem'den gelir, eski prototipteki ekran polling'i kapalıdır. Boyama coroutine'i yalnızca parmak basılıyken çalışır.
- Fırça, spatula ve baskı sadece temas bölgesindeki hücreleri işler. 320 piksel genişliğinde, fiziksel tuval oranında yaklaşık 88 bin hücre; değişen tile'lar yüklenir. Undo kopyala-yaz günlüğüyle sınırlıdır.
- Fırçanın kıl gürültüsü hücre başına yeniden hesaplanmaz; 129 örnekten enterpole edilir. Ayak izi polinomlarla hesaplanır, iç döngüde genel üs alma kullanılmaz. Fırça kaldırılınca taşınan boya, uçta tek bir sivri boncuk yerine fırça genişliğine yayılır.
- Her fırça örneğinde diske yazılmaz. Taslak 15 saniye boşlukta, sayfa değişiminde, uygulama duraklatılınca veya kapanınca kaydedilir. Boya alanı ve tüpler aynı kayıt nesline aittir. Ani süreç sonlandırmasında son otomatik kayıt sonrası vuruşlar kaybolabilir.
- Oyuncu boyarken gelen satışlar, yarım vuruşun tüp tüketimini eski bir taslakla birlikte kaydetmez. Arka plan ekonomisi son fiziksel taslağa ait tüp değerlerini korur. JSON yedeğinin işaret ettiği önceki boya dosyası da tutulur; ana kayıt bozulduğunda yedek yalnızca metaveriye dönmez.
- Boya tüpü satın alındığında undo geçmişi kapanır; önceki bir geri alma satın alınan tüpü veya parasını geriye çeviremez.
- 1080×1920 kompozisyon safe area içine oranı korunarak sığar. Uzun telefon ve tablet oranlarında tuval koordinatları ekran alanından yeniden hesaplanır.

## Claude kodunda düzeltilen noktalar

Önceki `AtelierSceneAuthoring` yalnızca bir boyama prototipi tarif ediyordu; sergi/müşteri/bilgisayar akışı yoktu ve Atelier sahnesi üretilmemişti. Yeni ana sahne, mevcut PaintIn2D/impasto yüzeyi ile ScriptableObject aletlerini birlikte kullanır. Kısa fırça hareketlerinde aralık birikimi düzeltildi; tuval dışında boya harcanması/taşınan boyanın kaybı engellendi; vuruş başına PlayerPrefs yazma kaldırıldı. Tüp envanteri artık galeri kayıt alanından yönetilebilir; eski sahnelerin kayıt anahtarı korunur.

Kırpılmış tuval sprite'larının UV bölgesi shader'da tuvalin 0–1 aralığına çevrilir. Bu düzeltme damlaların ovalleşmesini ve görüntünün dokunma noktasından kaymasını önler; tam dokuyu kullanan eski sahnelerde dönüşüm birim dönüşümdür. Render edilen bir damlanın konumu ve yuvarlaklığı piksel üzerinden kontrol edilir.

## Görsel kaynaklar

Yerleşik imagegen ile üretilen ve projeye alınan dosyalar `Art/Gallery/` altında: `GalleryInterior.png`, `Customers.png`, `ArtistTools.png`. Konsept yön: sıcak ışıklı butik galeri; krem sıva, ceviz, yeşil ve terracotta; üç çeyrek açıyla boyanmış 2D çevre. Karakter atlası 3×2, alet atlası 5 sütun. Tam üretim promptları `Docs/GalleryArtPrompts.md` içinde.

## Kontroller

Unity batch giriş noktası: `GalleryGameChecks.RunBatch`. Test verileri `Logs/GalleryChecks/<guid>/` altında oluşturulur; oyuncu kaydı kullanılmaz. Testler stok/para/tüp, 15 eser eşiği, sergi sonu, satın almayan müşteriler, yükleme sonrası tekrarlı ödeme, dosya hatasında geri alma, gerçek fare/dokunma EventSystem girişi, impasto hacim/pigment korunumu, undo ve sabit durumda managed allocation kontrollerini içerir. Görsel çıktılar `Logs/GalleryPreview/` altında.

Android cihaz performansı, dokunma hissi ve ticari oyun dengesi ayrıca gerçek cihazda değerlendirilmelidir. Editör ekran görüntüsü veya batch testi fiziksel APK testi yerine geçmez.
