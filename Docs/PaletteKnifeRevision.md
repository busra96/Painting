# Spatula ve baskı teması

Sahne: `Assets/Painting/Scenes/GalleryAtelier.unity`.

Güncel alet setinde baskı kaldırıldı; aşağıdaki baskı bölümü önceki düzeltmenin tarihçesidir. Yeni fırça, kalem ve bıçaklar için [ReferenceArtistTools.md](ReferenceArtistTools.md).

## Spatulayı kaldırırken parça oluşması

Önceki uygulama, bıçakta tuttuğu boyayı `EndKnife` içinde kısa bir ayak izine döküyordu. Hacim korunsa bile bu, fare bırakılınca yeni bir hilal/parça oluşturuyordu. İnce kalan film de özellikle kahverengide gri görünüyordu.

Spatula artık `ImpastoPaletteKnife.cs` içindeki korunumlu yüzey taşımasını kullanır:

- Her temas, bıçağın altındaki boyayı önce geçici bir çalışma tamponuna alır, ardından aynı işlem içinde ileri ve yana dağıtır. Kalıcı boya her zaman tuval üzerindedir; gizli bir hazneye alınmaz.
- Metalin temas yüksekliği, kuvvet ve temas eden boya miktarına göre değişir. Daha çok boya daha dolgun film bırakır. Güçlü baskı çalışılan filmi inceltir ve kapsanan alanı genişletir.
- Temaslar sabit kat edilen mesafeye göre işlenir. Kare başına olay sayısı değişince aynı çekişin profili değişmez.
- Kavisli kenar, yuvarlatılmış omuzlar, alt hücre dağıtımı ve yerel düzleştirme çapraz/eğri izlerde kopmayı azaltır. Boyanın kendi rengi ve kabarıklığı korunur.
- `EndKnife` yalnızca alet durumunu temizler. Tuvalde boya eklemez, taşımaz veya geri damgalamaz. Bırakmadan önce görülen yüzey ile bıraktıktan sonraki yüzey aynıdır.
- Tüm kaynak hücreler dağıtımdan önce örneklenir; tarama yönü gelen boyayı aynı temasta tekrar tekrar taşımaz. Yalnızca döndürülmüş temas alanının sınırları taranır. Tampon boyut değiştiğinde büyür, sabit boyutta hareket ve kaldırma sırasında managed allocation yapılmaz.

## Baskı fare ucunda

`PressContactBody.asset` mevcut şeffaf baskı görselini merkez pivotla kullanır. Görünen çap gerçek çalışma çapıyla eşleşir. Temas konumu gecikmeden takip edilir, yeni tıklamada eski konumdan kaymaz, bırakıldığında önceki noktada görsel kalmaz. Bir sürüklemenin son noktası örnek aralığına denk gelmediyse son temas o noktada tamamlanır.

## Ayarlar ve sahne

`Settings/Gallery/Oil_knife.asset`: `size`, `filmScale`, `pickup`; `Oil_press.asset`: boyut, tutma ve sürükleme kuvveti. **ALET AYARI** genişlik ve kuvveti değiştirir. `GalleryOilPaint.asset / SurfaceVariation` ince yüzey düzensizliğini belirler.

Baskı Sprite'ı `Art/Gallery/PressContactBody.asset` içindedir. Sprite, alet ayarı ve sahne bağlantıları düzenlenebilir asset'lerdir. Günlük düzenleme için sahneyi yeniden üretmek gerekmez.

## Doğrulama

Unity 6000.3.8f1, açık Windows Editor içinde izole test kaydıyla: **GalleryGameChecks 163**, **AtelierToolChecks 8**; toplam **171 kontrol** geçti. Önceki doğrulama sayımları `GalleryAtelierValidation.md` içindedir.

Yeni regresyon, gerçek tüp/fırçayla çizilmiş uzun kahverengi şeritleri spatulayla işler; alet görseli hariç kaldırmadan önceki ve sonraki render'ı piksel piksel karşılaştırır: **0 değişen piksel**. Ayrıca üç ayrı baskı konumu, merkez pivot/çap, bırakma, boya miktarı/kuvveti, 1.200 küçük hareketle tek çekiş karşılaştırması, dönüş/kenar/undo, hacim/pigment ve sıfır managed allocation kontrol edilir.

Loglar `Logs/GalleryInteractiveTrace.log`, `GalleryInteractiveChecks.txt`, `AtelierInteractiveChecks.txt` altında. Açık editör testleri uygulamayı kapatmaz; galeri testinden sonra önceki kayıtlı sahne düzeni geri yüklenir. Yerel editör doğrulama yardımcısı `GalleryEditorValidation`, `Logs/GalleryEditor.request` dosyasında yalnızca adlandırılmış test/yenileme işlemlerini kabul eder.

Son masaüstü çekirdek ölçümleri yaklaşık **17–19 ms / üç dünya birimlik tam çekiş**, **0 B managed allocation**. Bu tüm bir çekişin maliyetidir; normal bir karenin veya Android FPS'nin ölçümü değildir. Fiziksel Android cihaz testi yapılmadı.

Gerçek Unity render'ları: `Docs/Previews/GalleryAtelier-KnifeBeforeLift.png`, `GalleryAtelier-KnifeAfterLift.png`, `GalleryAtelier-KnifeStrokes.png`.
