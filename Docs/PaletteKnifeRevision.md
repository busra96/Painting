# Spatula: temas, boya taşıma ve kaldırma

Sahne: `Assets/Painting/Scenes/GalleryAtelier.unity`.

Önceki sürümde spatula boyayı sabit ve çok ince bir filme indiriyor, gövdedeki boyanın çıkışını üst renk katmanına bağlıyordu. Bu yüzden bıçakta kalan büyük hacim kaldırırken sert bir duvar oluşturuyordu. Dar bir damlanın üst katmanı yana taşınmadığından geniş bıçak da dar ve cetvel gibi bir iz bırakıyordu.

Yeni davranış:

- Temas örnekleri kare sayısından bağımsız, sabit kat edilen mesafeye bağlı. İşlenmeyen mesafe sonraki giriş olayına taşınıyor. Çok küçük hareketler kaybolmuyor.
- Bıçağın dönüşü kat edilen mesafeye göre ilerliyor. Renk haznesinin sağ/sol sırası dönüş ve ters çekişte korunuyor; ekrandaki bıçak aynı yönü kullanıyor.
- Hafif kavisli kenar ve yumuşak omuzlar, boyayı bıçak yüküne göre değişen kalınlıkta seriyor. Gövde ve üst renk katmanı çekiş boyunca boşalıyor; iki katman da bıçak boyunca yana hareket edebiliyor.
- Güçlü baskı daha ince ve daha uzağa taşınan film bırakıyor. Aynı genişlik ve çekiş mesafesinde fazla boya daha kalın iz veriyor. Boş spatula temiz tuvale boya eklemiyor.
- Alt hücre konumuna göre dört hücreye dağıtım, çapraz/eğri izlerdeki delikleri kapatıyor. Sadece temas bandındaki korunumlu yerel düzleştirme, ızgara kabartısını azaltıyor; tüm tuvali bulanıklaştırmıyor.
- Kaldırırken kalan boya kısa bir çizgiye yığılmak yerine kavisli, yumuşak bir ayak izine bırakılıyor. Hacim, pigment ve geri alma kayıtları korunuyor.
- Görünen spatula çalışma alanıyla aynı genişlikte. Temas noktası parmağın gerisinde gecikmiyor. Şeffaf mevcut spatula görseli kullanılıyor; uçtaki renk seçili tüpten değil, tuvalden alınan gerçek boyadan geliyor.

## Düzenleme

`Settings/Gallery/Oil_knife.asset`: `size`, `filmScale`, `pickup`. Ekrandaki **ALET AYARI** genişlik ve kuvveti değiştirir. Yanal boya hareketi `GalleryOilPaint.asset / LateralFlow` ile ilişkilidir. Üst katman/gövde ayrımı, eski hedef görsel sahnesinin renk sırası sistemini kullanmaya devam eder.

`GalleryArtDirectionAuthoring.ConfigureKnifeCursor` düzenlenebilir Sprite asset'lerini ve sahnedeki `Carried Paint On Blade` renderer'ını bağlar. Günlük görsel düzenleme için sahneyi yeniden üretmek gerekmez. Kaynak görseller `Art/Atelier/Cursor_Knife*.png`, yeni Sprite tanımları `Art/Gallery/KnifeContact*.asset` içindedir.

## Doğrulama

Unity 6000.3.8f1, Windows Editor Play Mode; testler oyuncu kaydı yerine `Logs` altında izole kayıtlar kullanır. GalleryGameChecks, 1.200 küçük hareketle tek uzun çekişin film profilini; yükteki/tuvaldeki toplam hacmi; boya miktarı ve baskı etkisini; dönüş, ters çekiş ve kenarda kaldırmayı; gerçek cursor rengi/genişliğini; geri almayı; çapraz filmde kesintisiz kapsama alanını ve ısınma sonrası sıfır managed allocation'ı doğrular.

Son çalıştırma: GalleryGameChecks **152**, AtelierToolChecks **8**, PaintingMechanicChecks **76**, WorkshopVisualChecks **66**, PaintHistoryChecks **19**; toplam **321** kontrol geçti. Beş süreç de exit code 0 ile tamamlandı. Loglar `Logs/*-KnifeVerified.log` altında.

Son görüntüler: `Docs/Previews/GalleryAtelier-KnifeStrokes.png`, `GalleryAtelier-KnifeContact.png`, `GalleryAtelier-OilRelief.png`. Bunlar Unity'nin gerçek boya alanı ve kamera render'ından alınır.

Son test ölçümü yaklaşık **12,2 ms / üç dünya birimlik tam çekiş** ve **0 B managed allocation** (yüklenmiş boyanın art arda ileri/geri taşındığı masaüstü çekirdek testi). Bu, bir normal karedeki kısa parmak hareketinin veya Android APK'nın FPS ölçümü değildir. Fiziksel Android cihaz testi yapılmadı.
