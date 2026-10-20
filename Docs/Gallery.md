# Galeri: tablo seçme ve kaydetme

Atölyenin üst şeridinde iki düğme var: **KAYDET** ve **GALERİ**.

- **KAYDET** tuvalin o anki görüntüsünü (kabartma, parlama ve gölge dahil) cihaza kaydeder. Boyama sırasında ve sonuç ekranında çalışır; spatula sürülürken ve puan hesaplanırken pasiftir. Sonuç ekranında kaydedilen tabloya eşleşme puanı da yazılır. Kaydettikten sonra düğme "KAYDEDİLDİ" olur; tabloya yeni boya eklenince yeniden etkinleşir.
- **GALERİ** üstteki overlay paneli açar:
  - **YENİ TABLO**: katalogdaki hedef tablolar ve **SERBEST TUVAL**. Kartı seçip **BAŞLA**'ya basınca sahne yeniden yüklenir (RETRY ile aynı temiz başlangıç), seçilen hedef hem Target Card'da görünür hem puanlamada kullanılır. Serbest tuvalde hedef kartı gizlenir; spatuladan sonra puan yerine "TABLON HAZIR" yazar.
  - **GALERİM**: kaydedilen tablolar, en yenisi önce. Karta dokununca büyük görünüm, puan ve tarih açılır. **SİL** iki dokunuşla (EMİN MİSİN?) siler.
  - Panel açıkken arka plan dokunuşları engellenir; Android geri tuşu / Escape paneli kapatır.

Seçim `PlayerPrefs` içinde `Painting.SelectedTarget` anahtarıyla saklanır; uygulama yeniden açılınca aynı tablo gelir.

## Kurulum

Unity'de **Tools → Painting → Create Gallery Panel** bir kez çalıştırılır. Araç:

1. `Assets/Painting/Art/Targets` altındaki hedefleri Sprite + Read/Write + sıkıştırmasız olarak içe aktarır.
2. `Assets/Painting/Settings/PaintingCatalog.asset` oluşturur (Renkli Kule, Gün Batımı, Deniz Kıyısı, Çiçek Tarlası).
3. Workshop Canvas'a `PaintingTargetApplier` ve `PaintingSaver`, `Canvas/Workshop` altına **Save Button** ve **Gallery Button** ekler.
4. Sahne köküne Screen Space Overlay **Gallery Canvas** (GalleryPanel) kurar.

Panel zaten varsa araç yeniden kurmayı reddeder; sonraki düzenlemeler Inspector'dan yapılır.

## Yeni hedef tablo eklemek

1. PNG'yi `Assets/Painting/Art/Targets` içine koy. Kâğıtla aynı oranda (851×1261) ve atölye paletindeki renklerle (kırmızı, sarı, yeşil, pembe, mavi, turuncu) olması puanlamayı adil tutar.
2. Import ayarı: Texture Type **Sprite**, **Read/Write** açık, Compression **None**. Read/Write kapalıysa `ImageGridProcessor` hata verir ve puan hesaplanmaz.
3. `PaintingCatalog.asset` listesine `id` (sabit, Türkçe karaktersiz), `title` ve `artwork` ekle. `id` yayından sonra değiştirilmemeli; kayıtlar ve seçim bu anahtarla tutulur.

Başlıklar Kalam SDF statik atlasıyla çizilir: A–Z, a–z, rakamlar, `% : / . , ! ? -` ve Türkçe harfler. Bunun dışındaki karakterler (ör. parantez, kesme işareti) görünmez.

## Depolama

Kayıtlar projeye değil cihaza yazılır: `Application.persistentDataPath/Gallery/`

- `<id>.png`: en fazla 1024 px genişlikte tam görüntü
- `<id>_thumb.png`: 256 px küçük resim
- `index.json`: başlık, hedef id, puan, tarih, boyut

Görüntü, ana kameranın bir kez ekran dışı texture'a çizilip kâğıt alanının kırpılmasıyla alınır. Overlay galeri paneli bu çizime girmez. Windows editörde klasör: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Gallery`.

## Kodlar

`Assets/Painting/Scripts/Gallery`: `PaintingCatalog`, `PaintingSession`, `PaintingTargetApplier`, `PaintingGallery`, `PaintingCapture`, `PaintingSaver`, `GalleryPanel`, `GalleryItemView`. Kodlar `PaintingGame.Gallery` namespace'i altındadır. `UIManager` yalnızca durum bilgisi (`IsBusy`, `CanSaveArtwork`, `ResultScore`) ve serbest tuval sonucu için genişletildi.

Kayıt yakalama çözünürlüğü ekran boyutuna göre değil, kâğıdın en fazla 1024 px olması için render öncesinde sınırlandırılır. Kayıt kimlikleri zaman damgası ve rastgele ek içerir. Dosyalar geçici dosya ve yedek üzerinden değiştirilir; başarısız kaydın kartı ve yarım görüntüleri temizlenir. Katalogdaki boş ve tekrarlanan kimlikler `TryValidate` ile kontrol edilir.

Kayıt düğmesi boya sayısını değil, her çizme/undo/temizleme işleminde artan `ArtworkRevision` değerini izler. Böylece geri alınan bir darbenin yerine aynı sayıda farklı boya konulsa bile tablo yeniden kaydedilebilir.

Mekanik, geri alma ve atölye kontrolleri başlamadan önce seçimi varsayılana döndürür (`PaintingSession.Select("")`), böylece testler her zaman sahnedeki hedefle çalışır. Görüntü yakalayan kontroller artık ilk bulunan Canvas yerine Workshop Canvas'ı gizler.
