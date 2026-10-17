# Boya mekaniği

Ana sahne: `Assets/Test2D/Scenes/Test 1.unity`.

Kağıda basılı tutmak boya bırakır; hareket etmek aynı akışı kesintisiz bir çizgiye dağıtır. Aynı noktada beklemek kalınlığı artırır. **DONE** düğmesinden sonra metal spatulayı aşağı sürükle. Spatula parmak bırakıldıktan sonra hedefe yumuşakça yetişir. Sonuç ekranındaki **RETRY** kağıdı ve boya haznesini sıfırlar.

## Atölye görünümü

Ana sahnedeki `Canvas/Workshop` bütün UI nesnelerini kalıcı olarak içerir: Background, Target Card/Target Artwork, Amount Panel/Amount Segments, Status Panel/Status Text, Canvas Frame, Paint Area, Palette Panel ve Done Button/Button Label. Eski UI panelleri silinmiştir. Sprite, tint, font, yazı, RectTransform ve buton ayarları Inspector'dan düzenlenebilir. Çalışma anında UI nesnesi üretilmez ve tasarım konumları yeniden yazılmaz. `WorkshopVisualController` yalnızca kalan miktarı, seçili renk işaretini ve kamera/çizim alanı eşlemesini günceller.

Mat guaj stilindeki ayrı görseller `Assets/Test2D/UI/Art` altında bulunur. `PaintedFrame` sprite'ı 9-slice ile çerçevelere ve butona uygulanır; `MattePaintDab` renk seçimi ve miktar parçaları için kullanılır. Hedef resim bağımsız bir Image'dır ve değiştirilebilir. Canvas, Main Camera ile Screen Space Camera modunda boyanabilir dünya yüzeyinin arkasında çizilir; böylece ahşap çerçeve ve tuval üzerindeki gerçek boya aynı render içinde görünür. Boya simülasyonunun dünya ölçüleri ve kütle hesabı değiştirilmez. Kamera, Canvas altındaki düzenlenebilir Paint Area'ya hizalanır. Temiz pigment/puan hesapları UI görsellerini içermez.

`Tools → Painting → Check Workshop Visual` görünüm, tuval hizası, gerçek UI raycast alanları ve renk seçimini doğrular; başlangıç ve boyalı ekran görüntülerini `Logs/WorkshopPreview` altına kaydeder. Ana mekanik kontrolü Done, spatula, sonuç ve Retry sonrasında atölyenin tek kez kurulmasını da sınar.

`WorkshopSceneAuthoring` bir defalık editör kurulum aracıdır. Mevcut Workshop bulunduğunda yeniden kurmayı reddeder; elle yapılan düzeni korur. Normal düzenleme için sahneyi açıp Canvas altındaki nesneleri değiştirin. Görsel test ayrıca Play öncesinde nesnelerin sahnede bulunduğunu ve konum/tint değişikliklerinin çalışma anındaki güncellemeler tarafından geri alınmadığını denetler. Görsel kaynakları ve üretim prompt'ları `Docs/WorkshopAssets.md` içindedir.

Düzenlenebilir sahnenin Unity 6000.3.8f1 doğrulaması: 36/36 görünüm, 67/67 mekanik kontrolü geçti. 540×960 ve 540×1200 render'ları ve sonuç/Retry ekranları kontrol edildi. Uzun çizgide ortalama döküş 0,509 ms, yerleşme 0,632 ms, texture gönderimi 0,876 ms; döngü içi managed allocation 0 bayt. Bunlar izole editör CPU ölçümleridir, cihaz FPS garantisi değildir. Kanıtlar yerel `Logs/HandPaintedWorkshop` klasöründedir.

## Paint paketi ile ilişki

CW PaintIn2D'nin decal komutları, PaintCore'un karışım shader'ları ve Flow örnekleri incelendi. Bunlar renk/normal çizme veya mevcut pikselleri taşıma altyapısı sağlıyor. Ana oyunun ihtiyaç duyduğu, bırakılan miktarı koruyan boya haznesi ve kalınlık hesabı ayrıca eklendi.

`ImpastoPaintSurface` kağıdın üzerinde toplam kalınlığı, toplam pigment kütlesini ve görünür en son boya katmanını tutar. Görünür katman toplam miktarın bir parçasıdır; ikinci bir boya kaynağı oluşturmaz. Aynı noktaya farklı renk koymak önceki boyayı anında homojen bir karışıma dönüştürmez. Yeni katmanın optik örtücülüğü kendi kalınlığına bağlıdır: yeterli boya eski rengi örter, çok ince boya altındaki rengi gösterebilir. Her döküşün ağırlıkları normalize edilir: kenara dökülen boya da verilen hacmi taşır. Kalın yığınlar kısa süre boyunca, hacmini ve pigmentini koruyarak gevşer. Gevşeme sırasında eski pigment yeni katmanın üstüne geçirilmez.

Spatula, geçtiği her satırı bir kez işler. Aldığı boya temas yüzeyindeki taze katman ve arkada taşınan ana hazne arasında korunur. Farklı bir boya katmanına gelince önceki temas boyası ana hazneye alınır; yeni yüzey boyası temas yüzeyine geçer. Farklı renkteki iki açık temas katmanı yana akarken doğrudan birbirinin içinden geçirilmez. Bu yeni katmanın önünde tutulabilecek miktar sabit küçük bir hazneyle kesilmez. Bırakılan tabaka bu sonlu taze kaynaktan beslenir. Eski hazneden alınabilen miktar, taze temas boyasının akışına ve `ColorRibbons` oranına bağlıdır. Taze yüzey tükenince arkada kalmış eski boya yeniden ayrı bir iz olarak çıkmaz; spatulada kalır ve toplam hacim/pigment bütçesinde korunur. `ColorRibbons=0` bilinçli tam karışım seçeneğidir. Böylece yeni boya miktarı hem örtücülüğü hem o rengin kaç dünya birimi ilerleyebileceğini belirler.

Yüzeye bırakılırken eski haznenin pigmenti ile yeni temas katmanı tek bir görünür karışım olarak kaydedilmez. Eski karışım toplam kütlede altta kalır; görünür yeni katman yalnızca gerçekten bırakılan taze pigmenti içerir. Bu ayrım, daha önceki izlerin yeni rengin içinde fazladan koyu çizgiler gibi belirmesini engeller. Taze katman tükenirken kalınlığı yumuşakça azalır; çok az boya hâlâ eski rengi kısmen gösterebilir.

Boya yükü arttıkça yanlara doğru akış güçlenir. Akış katsayısı dünya mesafesine göre hesaplanır. Hacim ve üç pigment kanalı aynı örtük difüzyon sistemiyle çözülür; bu, çok sayıda küçük adım gerektirmeden kütleyi ve pozitifliği korur. Kağıt kenarında dışarı akış yapılmaz.

Spatulanın bıraktığı tabaka hem yerel yüke hem temas dokusuna bağlıdır. İzler yol boyunca hafifçe kıvrılıp değişir; sabit bir tarak deseni tekrarlanmaz. Miktar azaldıkça boya incelir ve düzensiz ince izler halinde tükenir. Sayfa bittiğinde kalan boya haznede kalır. Hızlı ve yavaş sürüklemelerde taşınan miktar aynıdır; hareketsiz spatula boya harcamaz.

Yığınların gevşemesi 60 Hz simülasyon adımlarıyla, 32×32 hücrelik yerel bölgelerde ilerler. Her bölgenin kendi kalan süresi vardır; yeni boya eski çizginin tamamını yeniden etkinleştirmez. Dengeye ulaşmış bölgeler hemen uykuya geçer. Normal kare güncellemesinde mobilde en fazla 6, masaüstünde 12 bölge işlenir; fazla iş sonraki kareye bırakılır ve gecikme birikimi sınırlanır. Ağır yükte gevşeme biraz daha geç tamamlanabilir. Render texture güncellemesi ve puan için görüntü okuma fiziği ilerletmez.

Geri alma, her çizgi başında tüm mevcut boyayı kopyalamak yerine değişiklik günlüğü tutar. Döküş veya gevşeme bir 32×32 bölgeyi ilk kez değiştirmeden önce o bölgenin eski kalınlığı, pigmenti ve katman sırası saklanır. Başlangıçta boş olan bölgeler pixel dizisi gerektirmez. Etkilenmeyen tuval kopyalanmaz. Geçmişten çıkarılan çizgilerin küçük bölge tamponları yeniden kullanılır; havuz en fazla 64 bölge tutar. Çizgi sayısı ve kayıtlı veri bütçesi `PaintHistory` üzerinden sınırlanır. Undo sonrası devam eden gevşeme de kalan önceki çizginin günlüğüne işlenir.

Pigment karışımı doğrusal RGB ortalaması yerine ışık soğurması üzerinden hesaplanır. Bu, üst üste taşınan farklı boyaların daha doğal koyulaşmasını sağlar.

`ImpastoPaper.shader`, gerçek kalınlık alanından yüzey normalini, ıslak parlamayı ve yakın çevre gölgesini hesaplar. Spatulanın önündeki birikinti de aynı hazneden beslenir. Eski bağımsız siyah gölge damgaları ana sahnede kullanılmaz. `SteelSpatula.shader` metal bıçak görünümünü sağlar.

Birikinti gizliyken shader onun texture okumalarını ve üstel profil hesaplarını atlar. Görünürken aynı hazne örneği tekrar kullanılır. Yakın gölge üç kalınlık örneğiyle hesaplanır; normal ve gölge için gereksiz hazne okumaları yapılmaz. Gölge gücü ve kabarıklık, koyu lekelerin ayrı boya izleri gibi görünmesini azaltacak şekilde ayarlanmıştır.

İnce boya uçları, puan görüntüsünün beyaz tabanını ekrana taşımak yerine kalınlığa bağlı optik örtücülükle gerçek Canvas dokusunu gösterir. Bu yüzden boya azalırken geniş beyaz bir sis oluşmaz; puanın okuduğu beyaz tabanlı pigment görüntüsü aynı kalır.

Canlı çizim `_PigmentTex` üzerinden gösterilir. Temiz renk görüntüsü yalnızca sonuç okunacağı zaman `SyncAlbedo()` ile CW'nin `CwPaintableSpriteTexture.Current` render texture'ına aktarılır. Eşleşme puanı bu görüntüden okunur; aydınlatma, kağıdın sıcak tonu, gölge ve parlama puanı etkilemez.

Bu model kıvamlı akrilik boya için bir yükseklik alanı yaklaşımıdır. Üç boyutlu sıvı parçacıkları, taşan damlalar veya kuruma kimyası simüle edilmez.

## Ayarlar

`Assets/Test2D/SO/Settings.asset` üzerinden:

| Ayar | Etkisi |
| --- | --- |
| PaintVolumePerUnit | Harcanan her birimin gerçek boya hacmi |
| DepositRadius | Döküş ağzının genişliği |
| PourUnitsPerSecond | Basılı tutarken akış hızı |
| PourUnitsPerWorldUnit | Hareketli çizgide minimum boya miktarı |
| FilmThickness | Spatuladan sonra kalan tabaka; azaltmak aynı boyayla daha uzun iz oluşturur |
| FeatherDistance | Boya tükenirken incelen bölümün uzunluğu |
| PigmentOpacity | Birim yüzey kalınlığının örtücülüğü; yüksek değerde ince boya daha opak görünür, hacmi veya yayılma miktarını değiştirmez |
| LateralFlow | Spatulanın önünde biriken boyanın yana açılması |
| ColorRibbons | Yeni yüzey boyasının temas katmanında tutulma oranı; yüksek değer yeni rengin daha uzun süre örtmesini sağlar, 0 tüm pigmenti ana haznede karıştırır |
| SurfaceVariation | Yol boyunca değişen ıslak spatula izinin belirginliği |
| ReliefHeight | Kalınlığın görsel kabarıklığı |
| WetGloss | Islak yüzey parlaması |
| ContactShadow | Kalınlıktan üretilen yakın gölge |

Kağıt üzerindeki `ImpastoPaintSurface.resolution` ana sahnede 512'dir; mobil cihazda genişlik en fazla 384 hücre olur. Hücre alanı dünya ölçülerinden yeniden hesaplandığı için boya miktarı ve yayıldığı dünya mesafesi korunur. Mobil renk/yükseklik texture verisi 512 ayarına göre yaklaşık %44 azalır. Renk hesabı yalnızca değişen küçük bölgelerde yapılır; birbirinden uzak iki boya arasındaki boş alan taranmaz. Texture CPU belleğine doğrudan yazılır; her karede tüm managed diziyi kopyalamak gerekmez. Boya haznesinin texture'ı yalnızca hazne değiştiğinde güncellenir. Döküş şekli önceden hesaplanan tablodan örneklenir; her döküşte ağırlıklar bir kez hesaplanıp hacim normalizasyonu ve uygulamada tekrar kullanılır. Döküş tamponu yeniden kullanılır; fırça boyutu büyütülmedikçe yeni döküş dizisi ayrılmaz. Döküş başına GameObject üretilmez. Shader referansları sahneye kaydedilmiştir.

`Legacy decal scenes` altındaki ayarlar eski test sahnelerinin `Movement`/decal davranışı içindir.

## Doğrulama

Unity menüsü: **Tools → Painting → Run Mechanic Checks**. Aktif sahneyi kaydetme seçeneği sunduktan sonra ana sahneyi açar, Play Mode içinde kontrolleri çalıştırır ve çıkar. Batch giriş noktası: `PaintingMechanicChecks.RunBatch`.

Kontroller hacim ve üç pigment kanalının korunmasını, miktar/alan ilişkisini, yanal açılmayı, taze renk şeritlerini, hızlı/yavaş sürükleme ve yığın gevşemesi tutarlılığını, pigment renginin okunmasını, gölgenin puandan ayrılmasını, spatula hit alanını, parmak bırakılınca devam eden yumuşamayı, sonuç ve tekrar akışını kapsar. Unity'den alınan görüntüler `Logs/PaintingPreview` altına, yayılmanın ardışık kareleri `Animation` alt klasörüne yazılır; bu klasörler Git'e dahil değildir.

Katman kontrolleri ayrıca aynı noktaya yeni renk koymayı, gevşerken katman sırasının korunmasını ve büyük bir eski boya haznesi üzerinde az/çok yeni boya taşımayı sınar. `Amount-Layers.png` karşılaştırmasında iki üst mavi yığın eşit miktardadır; sol alt sarı 0,12 birim, sağ alt sarı 2,4 birimdir. Sarı miktarı arttıkça eski rengi örttüğü mesafe uzamalıdır.

`Stroke-Clarity.png` kontrolü iki eski kırmızı izi maviyle örter ve aşağıya üç yeni kırmızı iz koyar. Yayılma sonrasında mavi bölgede eski iki iz görünmemeli, ölçülen kırmızı şerit sayısı tam üç kalmalıdır. İz sonuna kadar ayrıca taranır: boya tükenirken gömülü eski renk yeniden çıkmamalı ve üçten fazla anlamlı şerit oluşmamalıdır. Fırça genişliğinden küçük kenar kırıntıları ayrı bir iz sayılmaz. 17 Ekim 2026 doğrulamasında 73 mekanik ve 17 geri alma kontrolü geçti. Geri alma kontrolü uzak iki bölgede boyamayı ve undo sonrasında devam eden gevşemeyi de kapsar. Batch giriş noktası `PaintHistoryChecks.RunBatch`.

Doğrulanan editör: Unity 6000.3.8f1. UI metin sarma API'si için Unity 2022/TMP 3 ve Unity 6 arasında derleme koşulu bulunur; yerel proje/paket yükseltmeleri mekanik değişikliklerinin parçası değildir.

Yoğun boya kontrolü, 16 büyük boya yığınını tam genişlikte sürerken CPU simülasyon ve texture gönderim sürelerini ayrı raporlar. Bu ölçüm GPU kare süresini veya belirli bir cihazda FPS garantisini ifade etmez.

## Çizim performansı

**Tools → Painting → Profile Drawing Performance** ana sahnede çalışır. Batch giriş noktası: `PaintingPerformanceChecks.RunBatch`. Test, 120 kare boyunca kağıdı dolaşan uzun bir çizgi çizer, uzak iki köşeye dönüşümlü boya koyar, dolu tuvalde geri alma açıkken art arda çizgilere başlar ve 720×1280 render örneği alır. Döküş, gevşeme ve texture gönderimi ayrı ölçülür. Çizim döngüsünde managed bellek tahsisi ve hacim korunumu da kontrol edilir.

Aynı makinede Unity 6000.3.8f1 ile ölçülen ortalama CPU süreleri:

| Senaryo | İlk ölçüm | Güncel |
| --- | --- | --- |
| Uzun çizgi: döküş | 1,38 ms | 0,24 ms |
| Uzun çizgi: gevşeme | 22,87 ms | 0,63 ms |
| Uzun çizgi: renk/texture | 10,39 ms | 0,88 ms |
| Uzak köşelerde boya: toplam | 46,13 ms | 1,41 ms |

Bunlar editör içindeki boya işlemlerinin ölçümüdür; oyunun tüm kare süresi veya cihazlar arası FPS garantisi değildir. Güncel ölçüm 17 Ekim 2026 tarihinde, 512 çözünürlükte alınmıştır. Dolu tuvalde tamponlar ısındıktan sonra çizgi başlangıcı + döküş ortalama 0,11 ms, p95 0,11 ms ve 0 byte managed tahsis verdi. Sekiz küçük çizginin geçmişi yaklaşık 1,18 MiB tuttu. Uzun çizgi döngüsünde de 0 byte managed tahsis ölçüldü. Texture gönderiminde p95 1,19 ms, en yüksek tek örnek 11,83 ms idi. Kanıtlar `Logs/PaintingPreview/*-Final.log` dosyalarındadır. Unity Profiler'da `Painting.Deposit`, `Painting.SettleTiles` ve `Painting.Upload` örnekleri ayrı izlenebilir. Bağlı Android cihaz bulunmadığından bu ölçümler APK'nin telefondaki FPS ölçümü değildir.
