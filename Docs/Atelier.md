# Atölye: boyama aletleri ve tüp ekonomisi

Güncel oynanabilir galeri oyunu `GalleryAtelier.unity` sahnesindedir; kurulum ve oyun döngüsü için [GalleryAtelier.md](GalleryAtelier.md). Bu belge Claude'un ayrı Atelier prototipinin ve ortak alet altyapısının geçmiş kurulum notlarıdır. Yeni sahne bu aletleri sergi, müşteri ve bilgisayar ekonomisiyle birleştirir; prototipi ayrıca üretmek gerekmez.

## Kurulum (bir kez)

1. `Painting.unity` üzerinde galeri paneli kurulmadıysa: **Tools → Painting → Create Gallery Panel**.
2. **Tools → Painting → Create Atelier Scene**
   - `Assets/Painting/Art/Atelier` görsellerini Sprite olarak içe aktarır (imleç pivotları, 9-slice kenarları dahil).
   - `Assets/Painting/Settings/Atelier/` altında veri asset'lerini oluşturur: 6 renk (`Color_*`), 5 alet (`Tool_*`), `AtelierConfig`.
   - `Painting.unity`'yi `Atelier.unity` olarak kopyalar. Eski döküp-çekme akışını kapatır, alet tepsisini, tüp göstergelerini, imleci ve popup'ı kurar.
   - Atelier sahnesini Build Settings listesinin sonuna ekler; `Painting.unity` ilk sahne olarak kalır (`AtelierShopChecks` bunu bekler). `Painting.unity` ve onun kontrolleri değişmez.
   - Atelier sahnesi varsa yeniden kurmayı reddeder; sonraki düzenlemeler Inspector'dan.
3. **Tools → Painting → Run Atelier Tool Checks**: aletlerin hacmi ve pigmenti koruduğunu, fırçanın tüpten aldığı kadar boya eklediğini, geri almanın tüpü iade ettiğini ve alet döngülerinin 0 byte tahsis yaptığını Play Mode'da ölçer. Batch: `AtelierToolChecks.RunBatch`.

## Aletler

| Alet | Ne yapar | Boya harcar |
| --- | --- | --- |
| Geniş Fırça | Yassı uç, kıl izi. Islak boyayı alır ve sürükler (yaş üstüne yaş karışım). Tüp boşken karıştırma fırçası. | Evet |
| İnce Fırça | Yuvarlak uç, çizgi ve ayrıntı. Hızlı çekince incelir. | Evet |
| Spatula | Herhangi bir yöne, istenen uzunlukta çekilir. Taze rengi şerit halinde taşır; kaldırınca ucunda sırt bırakır. | Hayır |
| Baskı | Basılı tutunca boyayı dışa ezer ve sünger dokusu basar; sürükleyince dokulu iz. | Hayır |
| Tüp | Kalın boya sıkar, beklemek kabartır (eski döküş mekaniği). | Evet |

Aletler tuvalin her yerinde, istenen boyanın üstünde çalışır. Dokunmatik ekranda alet parmağın biraz üstünde çalışır (`AtelierConfig.touchLift`), böylece boya parmağın altında kalmaz.

## AtelierShop ile ilişki

Projede ayrıca `AtelierShop.unity` var: serbest boyama + vitrin + satış döngüsünün ilk prototipi (`PaintingGame.Atelier` içindeki `AtelierShopController`, `AtelierShopStore`, `ImpastoArtistTools`). Bu çalışma ona dokunmaz; iki sahne yan yana durur. Fark:

- AtelierShop'ta boya tek bir ortak bütçeden (`PaintAllowance`) harcanır, kurallar koddaki sabit dizilerdedir, spatula yalnızca aşağı çeker.
- Atölye'de her rengin kendi tüpü vardır, aletler ve renkler ScriptableObject'tir, spatula her yöne çalışır, geri al tüpü iade eder.

Galeri/sergi/satış aşamasında AtelierShop'un ekonomisi (altın, vitrin kapasitesi, satış zamanlayıcısı) bu atölyeye bağlanabilir veya veri asset'lerine taşınabilir; karar o aşamada verilmeli.

## Mimari

```text
Scripts/Atelier/            (namespace PaintingGame.Atelier; kök dosyalar AtelierShop prototipine ait)
  Data/   PaintColorDefinition, PaintToolDefinition (+ Brush/Knife/Press/Pour), AtelierConfig   ← ScriptableObject
  Tools/  PaintTool (strateji) + BrushTool, KnifeTool, PressTool, PourTool                       ← saf C#
  Core/   PaintToolController (tek input döngüsü), TubeInventory, StrokeHistory, PaintToolContext, ToolCursorView
  UI/     ToolTrayView/ToolButtonView, TubeRackView/TubeGaugeView, SelectedTubeView, ModalView, AtelierHUD
Scripts/Painting/ImpastoPaintTools.cs   ← ImpastoPaintSurface'e eklenen Brush, KnifeSweep, Press işlemleri
```

- **Veri odaklı:** Yeni bir fırça için kod yazılmaz. `Create → Painting → Atelier → Tools → Brush` ile asset oluşturup `AtelierConfig.tools` listesine ekle; tepsi düğmesi otomatik çıkar. Renkler de aynı şekilde (`paletteSlot` hangi palet düğmesine bağlandığını belirler).
- **Strateji deseni:** Her `PaintToolDefinition`, `CreateTool()` ile kendi çalışma zamanı nesnesini üretir. Controller yalnızca `Begin / Drag / End` çağırır.
- **Olay tabanlı UI:** Sadece `PaintToolController.Update` input okur. Tüp göstergeleri, seçili tüp, tepsi ve HUD `ToolChanged`, `ColorChanged`, `StrokeEnded`, `TubeEmpty` ve `TubeInventory.Changed` olaylarıyla güncellenir. Envanter bildirimleri kare başına bir kez birleştirilir (`FlushNotifications`); boya harcandıkça her damla için UI yenilenmez. İmleç yalnızca görünürken `LateUpdate` çalıştırır.
- **Tüp ekonomisi:** `TubeInventory` her rengin doluluğunu tutar ve `PlayerPrefs` (`Atelier.Tubes`) içine kaydeder. Mağaza aşamasında `Refill` çağrılacak. Şimdilik boş tüp popup'ında `AtelierConfig.allowTestRefill` açıkken test doldurma düğmesi var; yayında kapatılmalı.
- **Geri al:** `StrokeHistory`, yüzeyin kopyala-yaz tile günlüğünü kullanır ve vuruştan önceki tüp seviyelerini saklar. Geri al, harcanan boyayı tüpe iade eder. Tuvali temizlemek geri alınamaz.
- **Görsel ölçek:** Atölye kamerası kağıdı (2.02×5.74 dünya birimi) çerçeveye yaklaşık 2,5:1 oranında yatayda gerer. Aletler ekran oranında çalışır (`ImpastoPaintSurface.VisualScale`), böylece yuvarlak fırça ekranda yuvarlak, spatula ekrana göre dik görünür. İmleç de bu gerilmeyi iptal eden bir parent altında. Eski sahnede `VisualScale` (1,1) kalır; orijinal döküş birebir aynıdır.
- **Bitirme:** BİTİR, hedefli tabloda eşleşme puanını hesaplar (mevcut `GridDataComparer`), tuvali ışıklı haliyle fotoğraflar ve `PaintingGallery`'ye eser olarak kaydeder. Puan yıldıza çevrilir (`starThresholds`). Sayaç sergi için gereken eser sayısını gösterir.

## Performans

- Tüm alet işlemleri yalnızca aletin kapladığı bölgede çalışır, işlemeden önce undo tile'larını kaydeder, gevşeme ve yeniden renklendirme için sadece değişen 32×32 tile'ları işaretler.
- Döngülerde managed tahsis yoktur; tamponlar yeniden kullanılır (kontrol ölçer). Baskı aracının sıralı hücre listesi yalnızca boyut değişince yeniden kurulur.
- Profiler örnekleri: `Painting.Brush`, `Painting.Knife`, `Painting.Press` (mevcut `Painting.Deposit`, `Painting.SettleTiles`, `Painting.Upload` ile birlikte).
- Bu sayılar editörde ölçülmedi. Cihazda Profiler ile kontrol edilmeli.

## Ayar rehberi

| Hissetmek istediğin | Değiştir |
| --- | --- |
| Fırça daha çok boya bıraksın | `unitsPerCanvasWidth` |
| Fırça ıslak boyayı daha çok karıştırsın | `smudge` ↑, `release` ↑ |
| Kıl izleri belirgin olsun | `bristleContrast` ↑, `bristles` |
| Spatula daha ince tabaka bıraksın, boyayı daha uzağa taşısın | `filmScale` ↓ |
| Baskı daha hızlı ezsin | `holdStrength` ↑ |
| Tüpler daha uzun dayansın | `Color_*.tubeCapacity` ↑ |
| Alet parmaktan daha yukarıda çalışsın | `AtelierConfig.touchLift` |

## Bilinen sınırlar ve sonraki adımlar

- Unity'de çalıştırılıp oynanarak denenmedi. Tüm runtime ve editör scriptleri, projenin güncel kaynaklarıyla birlikte Unity 6000.3.8f1 DLL'lerine karşı (Mono `mcs` ile) hatasız derlendi; Unity'nin kendi derleyicisi ve Play Mode kontrolleri yine de çalıştırılmalı. Alet hissi (miktarlar, boyutlar) cihazda denenip SO'lardan ayarlanmalı.
- Ses ve titreşim yok. Boya parçacıkları yok.
- Galeri sahnesi, sergi dönemi, müşteri (parçalı sprite + Animator), bilgisayar (Mağaza/Reklam) ve para sistemi sonraki aşamalardır.
