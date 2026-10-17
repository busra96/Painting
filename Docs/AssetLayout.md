# Assets düzeni

Ana oyun `Assets/Painting`, eski denemeler `Assets/Prototypes` altındadır. Unity asset taşıma API'si kullanılmıştır; asset ve `.meta` dosyaları birlikte taşındığından sahne/prefab bağlantılarındaki GUID'ler korunmuştur.

```text
Assets/
  Painting/
    Art/Fonts, UI, Sprites, Legacy
    Editor/Checks, Tools
    Materials/
    Prefabs/
    Resources/Shaders/
    Scenes/Painting.unity
    Scripts/Painting, Input, UI, Scoring, Settings, Compatibility
    Settings/Settings.asset
    Shaders/
  Prototypes/
    Legacy/
    Legacy2D/
    MeshGeneration/
    Painting3D/
  Plugins/
  TextMesh Pro/
  GUI PRO Kit - Casual Game/
```

- Oynanabilir sahne: `Assets/Painting/Scenes/Painting.unity`. Build Settings bu sahneyi kullanır. UI nesneleri sahnedeki `Canvas/Workshop` altında düzenlenebilir.
- Boya fiziği ve miktar ayarları: `Assets/Painting/Settings/Settings.asset`.
- Boya simülasyonu ve geri alma: `Assets/Painting/Scripts/Painting`.
- Kontroller ve spatula sürüklemesi: `Assets/Painting/Scripts/Input`.
- Atölye görselleri: `Assets/Painting/Art/UI`; fontlar: `Art/Fonts`.
- `Scripts/Compatibility`, mevcut prefabların referans verdiği eski decal bileşenlerini içerir.
- `Resources/Shaders/ImpastoReadback.shader`, pigmentin beyaz zemin üzerinde puanlanabilmesi için çalışma anında yüklenir. Resources anahtarı `Shaders/ImpastoReadback` olarak kalmalıdır.
- Eski deney sahneleri ve örnekleri Prototypes altında saklanır. Paketlerin kurulum konumları ve yerel `_Recovery` yedekleri korunmuştur.

Editor testleri ve kurulum araçlarındaki yollar yeni konumlara güncellenmiştir. Taşıma kontrolü 2.280 asset GUID'ini ve ana sahnenin 77 bağımlılığını doğruladı; eksik script sayısı sıfırdır. Yerel kanıtlar `Logs/AssetLayout/Before.json`, `After.json` ve `Migration.log` içindedir.
