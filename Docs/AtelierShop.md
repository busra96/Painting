# Renk Atölyem: boyama ve mağaza prototipi

Sahne: `Assets/Painting/Scenes/AtelierShop.unity`. Unity'de bu sahneyi açıp Play'e bas. Mevcut hedef görseli eşleştirme oyunu `Painting.unity` sahnesinde korunur. Yeni sahnenin başlığındaki **HEDEF GÖRSEL MODU** düğmesi o sahneye geçer ve önce atölyedeki taslağı kaydeder. Projeye ait 3D model boyama prototipi kaldırıldı.

## Oyun döngüsü

1. **YENİ TUVAL** ile boş tuval satın al veya doğrudan görünen boş tuvale dokun. İlk dokunuş tuvali bir kez satın alır ve aynı hareketle boyamayı başlatır. Başlangıçta 90 altın, pamuk tuvalin bedeli 8 altındır.
2. Paletten renk seç; basılı tutarak veya sürükleyerek resim yap. Boyut ve basınç sürgüleri darbenin genişliğini ve bıraktığı boya miktarını değiştirir. Her tuval 120 birim boya içerir.
3. **VİTRİNE KOY** eserin gerçek, ışıklandırılmış görüntüsünü kaydeder ve mağazaya yerleştirir. Satış için tuvalin en az %0,5'inde görünür boya gerekir; boş tuval sergilenemez.
4. Müşteriler vitrindeki eserleri sırayla otomatik alır. İlk mağazada satış aralığı 12,5 saniyedir. Süre oyun açıkken, hangi sekmede olursa olsun ilerler. Çevrimdışı satış hesaplanmaz.
5. Kazançla aletler, dört özel pigment ve kaliteli tuval stokları açılır. Mağaza genişlemesi iki sergi alanı ekler ve müşterileri hızlandırır. Beş mağaza seviyesi, 4–12 sergi alanı vardır.

Bir eserin fiyatı görünür kaplama alanı, en fazla üç ek teknik bonusu ve kullanılan tuvalin kalitesinden hesaplanır. Hedef resme benzerlik yeni modda aranmaz. Fiyatlar ve süreler ilk oynanabilir denge içindir; `AtelierRules` içinde bulunur. Son paranın yükseltmeye harcanıp oyuncunun tuvalsiz kalması engellenir.

## Aletler

| Alet | Davranış | Açılma bedeli |
| --- | --- | --- |
| Tüp | Dar, kabarık boya bırakır; basılı tutunca birikir | Başlangıç |
| Yuvarlak fırça | Kesintisiz, yumuşak kenarlı serbest çizgi | Başlangıç |
| Geniş spatula | İstenen satırdan aşağı doğru geniş boya filmi yayar | Başlangıç |
| Bastır | Parmak altında mevcut boyayı yerel olarak düzleştirir | Başlangıç |
| Düz fırça | Geniş, eliptik ve kıllı iz | 35 |
| Fan fırça | Taranmış, yayılan fırça dokusu | 60 |
| Çiçek | Yedi yapraklı kabartma baskı; sürüklerken aralıklı tekrar | 75 |
| Yıldız | Beş köşeli baskı | 95 |
| Palet bıçağı | Mevcut boyayı sürükleme yönüne taşır | 110 |

Bastırma, spatula ve bıçak boya üretmez veya bütçe harcamaz; pigment ve hacmi taşır. Fırça ve baskılarda bırakılan hacim harcanan boya ile orantılıdır. Tuval kenarında kesilen baskılar da aynı gerçek boya miktarını korur. Tek bir basılı tutma bir undo adımıdır. Son sekiz hareket, en fazla 24 MB geçmiş bütçesi içinde tutulur.

**TEMİZLE** iki kez basılarak mevcut tuvali ve boya payını sıfırlar; yeni tuval satın almaz. **GERİ AL** boya alanını ve harcanan miktarı geri getirir. Desen damgasını sabit tutmak yeni baskılar ve sınırsız boya tüketimi oluşturmaz.

## Gelişim

- Özel pigmentler: 90 altın; yeşil, magenta, turkuaz ve koyu pigment.
- Keten tuval: 80 altın stok açma, sonraki boş tuvaller 18 altın. Çerçevesi yeşildir.
- Usta tuvali: 200 altın stok açma, sonraki tuvaller 35 altın. Çerçevesi altın renklidir.
- Yeni tuval seçimi eldeki resmi değiştirmez; sonraki satın alınan tuvale uygulanır.
- Tuval kalitesi satış çarpanını artırır; tablo aynı serbest boya simülasyonunu kullanır.
- Dolu vitrinde sergileme reddedilir ve eldeki resim silinmez. Satıştan sonra vitrindeki alan boşalır.

## Düzenleme ve kayıt

Tüm arayüz `Atelier Canvas` altındaki sahne nesneleridir. `Workshop`, `Shop` ve `Supplies` panelleri Inspector/Scene görünümünden düzenlenebilir; çalışma anında arayüz üretilmez. Galeri kartlarının tamamı da sahnede bulunur. `AtelierActionButton` düğmelerin hangi işlemi ve indeksi kullandığını gösterir. `Workshop/Paint Area` üzerindeki şeffaf, raycast alan `Image` ve `AtelierPaintInput`, fare ve dokunma olaylarını alır. Bu bileşenler dekoratif olmadığı için silinmemelidir. Diğer UI düğmeleri ve sürgüler kendi olaylarını alır; tuvale boya bırakmaz.

`AtelierSettings.asset` ortak impasto görsel ve boya ayarlarını, `AtelierRules` ekonomi ve araç bedellerini içerir. Yeni aletler `ImpastoArtistTools.cs` içinde mevcut pigment/hacim simülasyonuna eklenen ayrı metotlardır; hedef sahnenin eski boya işlemleri değiştirilmez.

Kayıt yeri `Application.persistentDataPath/AtelierShop`:

- `shop.json` ve yedeği: altın, satışlar, vitrin, açılan aletler, tuval türleri ve boya payı.
- `<id>.png`: vitrindeki resmin 448 piksel genişlikte tam tuval görüntüsü. Satış sonrası dosya kaldırılır.
- `draft.paint`: uygulama durakladığında, kapandığında veya hedef moduna geçildiğinde kaydedilen sıkıştırılmış fiziksel boya alanı. Yükleme yalnızca görüntüyü değil, boya kalınlığını ve pigmentleri geri getirir. Undo geçmişi kayıt dosyasına yazılmaz.

`Tools → Painting → Create Atelier Shop Scene` yalnızca yeni sahne oluşturur; var olan sahneyi yeniden üretmez ve elle düzenlenmiş UI'ı korur.

## Doğrulama

`AtelierShopChecks.RunBatch` gerçek grafik cihazıyla çalıştırılır; `-nographics` kullanılmaz. Kontroller kendi geçici profil klasörlerini kullanır ve oyuncunun kaydını değiştirmez. 77 kontrol; gerçek UI raycast sonucu üzerinden fare/dokunma pointer olayları, ilk dokunuşta tuval alıp boyama, sürükleme, sürgü hit alanları, alışveriş/satış, kapasite, tekrar satın alma, tuvalsiz kalma koruması, eski taslak yedeğinin yeni tuvale taşınmaması, kayıttan yükleme, tüm fırça tiplerinde hacim, yerel yaymada pigment korunumu, undo ve geniş spatulanın hızlı/yavaş sürülmesini kapsar. Pointer olayları otomatik olarak gönderilir; fiziksel Android cihaz testi değildir.

Windows editör ölçümünde 60 ardışık geniş fırça baskısı ortalama yaklaşık 1,23 ms ve 0 byte managed allocation gösterdi. Bu bir Android cihaz ölçümü değildir. Önizlemeler `Logs/AtelierPreview`, test logu `Logs/AtelierShopChecks.log` altındadır.
