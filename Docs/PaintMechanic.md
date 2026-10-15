# Boya mekaniği

Ana sahne: `Assets/Test2D/Scenes/Test 1.unity`.

Kağıda basılı tutmak boya bırakır; hareket etmek aynı akışı kesintisiz bir çizgiye dağıtır. Aynı noktada beklemek kalınlığı artırır. **BOYAYI YAY** düğmesinden sonra metal spatulayı aşağı sürükle. Spatula parmak bırakıldıktan sonra hedefe yumuşakça yetişir. Sonuç ekranındaki **TEKRAR DENE** kağıdı ve boya haznesini sıfırlar.

## Paint paketi ile ilişki

CW PaintIn2D'nin decal komutları, PaintCore'un karışım shader'ları ve Flow örnekleri incelendi. Bunlar renk/normal çizme veya mevcut pikselleri taşıma altyapısı sağlıyor. Ana oyunun ihtiyaç duyduğu, bırakılan miktarı koruyan boya haznesi ve kalınlık hesabı ayrıca eklendi.

`ImpastoPaintSurface` kağıdın üzerinde bir kalınlık ve pigment alanı tutar. Her döküşün ağırlıkları normalize edilir: kenara dökülen boya da verilen hacmi taşır. Kalın yığınlar kısa süre boyunca, hacmini ve pigmentini koruyarak gevşer.

Spatula, geçtiği her satırı bir kez işler. Aldığı boya yatay bir haznede tutulur, komşu alanlarla hafifçe karışır ve arkasına ince tabaka halinde bırakılır. Miktar azaldıkça iz incelir, çizgiler halinde tükenir. Sayfa bittiğinde kalan boya haznede kalır. Hızlı ve yavaş sürüklemelerde taşınan miktar aynıdır; hareketsiz spatula boya harcamaz.

Pigment karışımı doğrusal RGB ortalaması yerine ışık soğurması üzerinden hesaplanır. Bu, üst üste taşınan farklı boyaların daha doğal koyulaşmasını sağlar.

`ImpastoPaper.shader`, gerçek kalınlık alanından yüzey normalini, ıslak parlamayı ve yakın çevre gölgesini hesaplar. Spatulanın önündeki birikinti de aynı hazneden beslenir. Eski bağımsız siyah gölge damgaları ana sahnede kullanılmaz. `SteelSpatula.shader` metal bıçak görünümünü sağlar.

Temiz renk görüntüsü CW'nin `CwPaintableSpriteTexture.Current` render texture'ına aktarılır. Eşleşme puanı bu görüntüden okunur; aydınlatma, kağıdın sıcak tonu, gölge ve parlama puanı etkilemez.

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
| ReliefHeight | Kalınlığın görsel kabarıklığı |
| WetGloss | Islak yüzey parlaması |
| ContactShadow | Kalınlıktan üretilen yakın gölge |

Kağıt üzerindeki `ImpastoPaintSurface.resolution` ana sahnede 512'dir. Renk hesabı yalnızca değişen bölgelerde yapılır; döküş başına GameObject üretilmez. Shader referansları sahneye kaydedilmiştir.

`Legacy decal scenes` altındaki ayarlar eski test sahnelerinin `Movement`/decal davranışı içindir.

## Doğrulama

Unity menüsü: **Tools → Painting → Run Mechanic Checks**. Aktif sahneyi kaydetme seçeneği sunduktan sonra ana sahneyi açar, Play Mode içinde kontrolleri çalıştırır ve çıkar. Batch giriş noktası: `PaintingMechanicChecks.RunBatch`.

Kontroller hacim korunmasını, miktar/alan ilişkisini, hızlı/yavaş sürükleme tutarlılığını, pigment renginin okunmasını, gölgenin puandan ayrılmasını, spatula hit alanını, parmak bırakılınca devam eden yumuşamayı, sonuç ve tekrar akışını kapsar. Unity'den alınan görüntüler `Logs/PaintingPreview` altına yazılır; bu klasör Git'e dahil değildir.
