# Atölye görselleri

`Assets/Test2D/Resources/Workshop/WorkshopBackground.png`, kullanıcının sağladığı **Sıcak Işıkta Boyama Atölyesi.png** görselinin değiştirilmeden projeye alınmış kopyasıdır. Ana atölye kompozisyonu bu görselden gelir. Boya, miktar, seçim işareti, durum ve sonuç metni çalışma zamanında eklenir.

`Assets/Test2D/Resources/Workshop/BlankButton.png`, yerleşik `image_gen` aracıyla referanstaki düğmeden üretilmiştir. Şeffaflığı korunur; Unity importer en uzun kenarı 1024 piksele indirir. Bu düğme sonuç ekranında RETRY metninin arkasında kullanılır. Üretim için kullanılan son prompt:

> Use case: background-extraction. Asset type: reusable Unity game button sprite. Edit target: the bottom DONE button in the supplied workshop reference. Extract ONLY that bottom wide rounded cream-and-gold painted wooden button, erase the letters DONE and reconstruct the same smooth buttery cream paint texture underneath. Preserve the exact thick brown wooden rim, golden bevel, warm upper-left light and hand-painted brush texture. Straight front view, full button entirely visible, centered on a truly transparent background. No text, no table, no other UI, no cast shadow beyond the button, no watermark. Wide 3.2:1 button shape, minimal transparent margin. This blank button will carry a live RETRY text label in Unity.

İlk taslaklarda ayrı arka plan, damla, fırça ve miktar parçaları da denendi. Son sürüm referansa sadık kalmak için özgün arka planı kullanır; kullanılmayan taslaklar oyuna dahil edilmez.
