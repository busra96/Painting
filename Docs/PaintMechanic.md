# Boya mekaniği

Ana sahne: `Assets/Test2D/Scenes/Test 1.unity`.

Kağıda basılı tutmak boya bırakır; hareket etmek aynı akışı kesintisiz bir çizgiye dağıtır. Aynı noktada beklemek kalınlığı artırır. **DONE** düğmesinden sonra metal spatulayı aşağı sürükle. Spatula parmak bırakıldıktan sonra hedefe yumuşakça yetişir. Sonuç ekranındaki **RETRY** kağıdı ve boya haznesini sıfırlar.

## Atölye görünümü

`WorkshopVisualController` her ana sahne yüklemesinde, Retry dahil, kullanıcı tarafından sağlanan sıcak ışıklı ahşap atölye görselini kurar. Mevcut altı renk düğmesinin tıklanabilir alanları görseldeki boya gözlerine taşınır. Seçili boyanın altında küçük bir işaret bulunur. Yedi kırmızı `Amount` parçası kalan boya oranına göre kararır. Lacivert bölüm oyun durumunu, krem düğme ise aynı `UIManager` Done/Retry akışını gösterir.

Referans görsel `Assets/Test2D/Resources/Workshop/WorkshopBackground.png` altında tutulur. Boya simülasyonunun dünya ölçüleri değiştirilmez; ortografik kamera bu alanı referanstaki tuval içine yansıtır. UI konumları normalize edilmiş aynı koordinatları kullanır. Boş bölgelerde shader arka plandaki tuval dokumasını gösterir; renk ve kalınlık gerçek simülasyondan çizilir. Temiz pigment ve puan hesapları arka planı içermez. 9:16 portre ana tasarımdır; farklı ekran oranlarında tüm düzen aynı koordinatlarla ölçeklenir. Sol üstteki örnek resim bu seviye için referans görselin bir parçasıdır; farklı seviye hedefleri için ayrı bir thumbnail katmanı gerekir.

`Tools → Painting → Check Workshop Visual` görünüm, tuval hizası, gerçek UI raycast alanları ve renk seçimini doğrular; başlangıç ve boyalı ekran görüntülerini `Logs/WorkshopPreview` altına kaydeder. Ana mekanik kontrolü Done, spatula, sonuç ve Retry sonrasında atölyenin tek kez kurulmasını da sınar.

Unity 6000.3.8f1 izole editör doğrulaması: 540×960 ve 540×1200 görünüm kontrolleri 33/33; tam mekanik akışı 67 kontrol içerir. Çizim performansı senaryosunda döküş ortalaması 0,502 ms, yerleşme 0,639 ms, texture gönderimi 0,947 ms; döngü içi managed allocation 0 bayt. Bu ölçüm editördeki CPU işini raporlar, telefonda FPS garantisi değildir. Görsellerin kaynağı ve kullanılan üretim prompt'u `Docs/WorkshopAssets.md` içindedir.

## Paint paketi ile ilişki

CW PaintIn2D'nin decal komutları, PaintCore'un karışım shader'ları ve Flow örnekleri incelendi. Bunlar renk/normal çizme veya mevcut pikselleri taşıma altyapısı sağlıyor. Ana oyunun ihtiyaç duyduğu, bırakılan miktarı koruyan boya haznesi ve kalınlık hesabı ayrıca eklendi.

`ImpastoPaintSurface` kağıdın üzerinde toplam kalınlığı, toplam pigment kütlesini ve görünür en son boya katmanını tutar. Görünür katman toplam miktarın bir parçasıdır; ikinci bir boya kaynağı oluşturmaz. Aynı noktaya farklı renk koymak önceki boyayı anında homojen bir karışıma dönüştürmez. Yeni katmanın optik örtücülüğü kendi kalınlığına bağlıdır: yeterli boya eski rengi örter, çok ince boya altındaki rengi gösterebilir. Her döküşün ağırlıkları normalize edilir: kenara dökülen boya da verilen hacmi taşır. Kalın yığınlar kısa süre boyunca, hacmini ve pigmentini koruyarak gevşer. Gevşeme sırasında eski pigment yeni katmanın üstüne geçirilmez.

Spatula, geçtiği her satırı bir kez işler. Aldığı boya temas yüzeyindeki taze katman ve arkada taşınan ana hazne arasında korunur. Farklı bir boya katmanına gelince önceki temas boyası ana hazneye alınır; yeni yüzey boyası temas yüzeyine geçer. Farklı renkteki iki açık temas katmanı yana akarken doğrudan birbirinin içinden geçirilmez. Bu yeni katmanın önünde tutulabilecek miktar sabit küçük bir hazneyle kesilmez. Bırakılan tabaka önce bu sonlu taze kaynaktan beslenir. Yeterli alt boya varken üst sıradan taşınan eski renk içinden çıkmaz; taze kaynak inceldikçe eski hazne yumuşak bir geçişle yeniden katkı verir. Bu yüzden alt boyanın miktarı hem rengin örtücülüğünü hem o rengin kaç dünya birimi ilerleyebileceğini belirler.

Boya yükü arttıkça yanlara doğru akış güçlenir. Akış katsayısı dünya mesafesine göre hesaplanır. Hacim ve üç pigment kanalı aynı örtük difüzyon sistemiyle çözülür; bu, çok sayıda küçük adım gerektirmeden kütleyi ve pozitifliği korur. Kağıt kenarında dışarı akış yapılmaz.

Spatulanın bıraktığı tabaka hem yerel yüke hem temas dokusuna bağlıdır. İzler yol boyunca hafifçe kıvrılıp değişir; sabit bir tarak deseni tekrarlanmaz. Miktar azaldıkça boya incelir ve düzensiz ince izler halinde tükenir. Sayfa bittiğinde kalan boya haznede kalır. Hızlı ve yavaş sürüklemelerde taşınan miktar aynıdır; hareketsiz spatula boya harcamaz.

Yığınların gevşemesi 60 Hz simülasyon adımlarıyla, 32×32 hücrelik yerel bölgelerde ilerler. Her bölgenin kendi kalan süresi vardır; yeni boya eski çizginin tamamını yeniden etkinleştirmez. Dengeye ulaşmış bölgeler hemen uykuya geçer. Normal kare güncellemesinde en fazla 24 bölge işlenir; fazla iş sonraki kareye bırakılır ve gecikme birikimi sınırlanır. Ağır yükte gevşeme biraz daha geç tamamlanabilir; işaretçi girişini bloke etmez. Render texture güncellemesi ve puan için görüntü okuma fiziği ilerletmez.

Pigment karışımı doğrusal RGB ortalaması yerine ışık soğurması üzerinden hesaplanır. Bu, üst üste taşınan farklı boyaların daha doğal koyulaşmasını sağlar.

`ImpastoPaper.shader`, gerçek kalınlık alanından yüzey normalini, ıslak parlamayı ve yakın çevre gölgesini hesaplar. Spatulanın önündeki birikinti de aynı hazneden beslenir. Eski bağımsız siyah gölge damgaları ana sahnede kullanılmaz. `SteelSpatula.shader` metal bıçak görünümünü sağlar.

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

Kağıt üzerindeki `ImpastoPaintSurface.resolution` ana sahnede 512'dir. Renk hesabı yalnızca değişen küçük bölgelerde yapılır; birbirinden uzak iki boya arasındaki boş alan taranmaz. Texture CPU belleğine doğrudan yazılır; her karede tüm managed diziyi kopyalamak gerekmez. Boya haznesinin texture'ı yalnızca hazne değiştiğinde güncellenir. Döküş şekli önceden hesaplanan tablodan örneklenir, her hücrede yeniden sinüs ve üs hesabı yapılmaz. Her döküş yine verilen hacme normalize edilir. Döküş başına GameObject üretilmez. Shader referansları sahneye kaydedilmiştir.

`Legacy decal scenes` altındaki ayarlar eski test sahnelerinin `Movement`/decal davranışı içindir.

## Doğrulama

Unity menüsü: **Tools → Painting → Run Mechanic Checks**. Aktif sahneyi kaydetme seçeneği sunduktan sonra ana sahneyi açar, Play Mode içinde kontrolleri çalıştırır ve çıkar. Batch giriş noktası: `PaintingMechanicChecks.RunBatch`.

Kontroller hacim ve üç pigment kanalının korunmasını, miktar/alan ilişkisini, yanal açılmayı, taze renk şeritlerini, hızlı/yavaş sürükleme ve yığın gevşemesi tutarlılığını, pigment renginin okunmasını, gölgenin puandan ayrılmasını, spatula hit alanını, parmak bırakılınca devam eden yumuşamayı, sonuç ve tekrar akışını kapsar. Unity'den alınan görüntüler `Logs/PaintingPreview` altına, yayılmanın ardışık kareleri `Animation` alt klasörüne yazılır; bu klasörler Git'e dahil değildir.

Katman kontrolleri ayrıca aynı noktaya yeni renk koymayı, gevşerken katman sırasının korunmasını ve büyük bir eski boya haznesi üzerinde az/çok yeni boya taşımayı sınar. `Amount-Layers.png` karşılaştırmasında iki üst mavi yığın eşit miktardadır; sol alt sarı 0,12 birim, sağ alt sarı 2,4 birimdir. Sarı miktarı arttıkça eski rengi örttüğü mesafe uzamalıdır.

Doğrulanan editör: Unity 6000.3.8f1. UI metin sarma API'si için Unity 2022/TMP 3 ve Unity 6 arasında derleme koşulu bulunur; yerel proje/paket yükseltmeleri mekanik değişikliklerinin parçası değildir.

Yoğun boya kontrolü, 16 büyük boya yığınını tam genişlikte sürerken CPU simülasyon ve texture gönderim sürelerini ayrı raporlar. Bu ölçüm GPU kare süresini veya belirli bir cihazda FPS garantisini ifade etmez.

## Çizim performansı

**Tools → Painting → Profile Drawing Performance** ana sahnede çalışır. Batch giriş noktası: `PaintingPerformanceChecks.RunBatch`. Test, 120 kare boyunca kağıdı dolaşan uzun bir çizgi çizer, uzak iki köşeye dönüşümlü boya koyar ve 720×1280 render örneği alır. Döküş, gevşeme ve texture gönderimi ayrı ölçülür. Çizim döngüsünde managed bellek tahsisi ve hacim korunumu da kontrol edilir.

Aynı makinede Unity 6000.3.8f1 ile ölçülen ortalama CPU süreleri:

| Senaryo | Önce | Sonra |
| --- | --- | --- |
| Uzun çizgi: döküş | 1,38 ms | 0,54 ms |
| Uzun çizgi: gevşeme | 22,87 ms | 0,66 ms |
| Uzun çizgi: renk/texture | 10,39 ms | 1,00 ms |
| Uzak köşelerde boya: toplam | 46,13 ms | 1,57 ms |

Bunlar editör içindeki boya işlemlerinin ölçümüdür; oyunun tüm kare süresi veya cihazlar arası FPS garantisi değildir. Testlerdeki çözünürlük 512 olarak korunmuştur. Uzun çizgi döngüsünde 0 byte managed tahsis ölçülmüştür. Unity Profiler'da `Painting.Deposit`, `Painting.SettleTiles` ve `Painting.Upload` örnekleri ayrı izlenebilir.
