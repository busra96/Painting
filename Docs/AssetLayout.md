# Assets düzeni

`Assets` kökü sahipliğe göre ayrılır. Oyun ekibinin değiştirdiği içerik `Painting`, dış paketler `Plugins` ve `ThirdParty`, eski deneyler `Archive` altındadır. Taşımalar Unity `AssetDatabase.MoveAsset` ile yapıldığı için `.meta` GUID'leri ve sahne/prefab bağlantıları korunur.

```text
Assets/
  Painting/                       # Üretim oyunu
    Art/Fonts, UI, Sprites, Targets, Atelier, Gallery, Legacy
    Editor/Checks, Tools
    Materials
    Prefabs
    Resources/Shaders
    Scenes/Painting.unity
    Scenes/AtelierShop.unity
    Scenes/GalleryAtelier.unity
    Scripts/Painting, Atelier, Gallery, Input, UI, Scoring, Settings, Compatibility
    Settings
    Shaders
  Plugins/                        # Unity'nin özel Plugins derleme alanı
    CW/PaintCore, PaintIn2D, PaintIn3D
    Demigiant/DOTween
  ThirdParty/                     # Kod sahipliği bizde olmayan görsel/UI paketleri
    GUIProKit
    TextMeshPro
  Archive/Prototypes/             # Build'e girmeyen eski araştırmalar
    Legacy, Legacy2D, MeshGeneration, Painting3D
  _Recovery/                      # Yerel Unity kurtarma kopyası; gitignore'da
```

- Hedef görsele benzetme sahnesi `Assets/Painting/Scenes/Painting.unity`; önceki mağaza prototipi `AtelierShop.unity`; güncel sergi/galeri oyunu `GalleryAtelier.unity` konumundadır. Üçü build listesinde bulunur; ilk sahne korunmuştur.
- Oyun kodu, düzenlenebilir UI ve üretim sanat içeriği yalnızca `Assets/Painting` altında tutulur.
- PaintCore, Paint in 2D/3D ve DOTween, Unity'nin `Plugins` derleme davranışına bağlı olduğu için `Assets/Plugins` altında kalır.
- GUI Pro Kit ve TextMesh Pro dış paketleri `Assets/ThirdParty` altında tutulur; proje kodu bu klasörlere eklenmez.
- `Archive/Prototypes` build'e eklenmez ve yalnızca referans amaçlı eski denemeleri içerir.
- `Resources/Shaders/ImpastoReadback.shader` çalışma anında `Shaders/ImpastoReadback` anahtarıyla yüklenir; yolu değiştirilmemelidir.
- `Assets/_Recovery` Unity'nin yerel kurtarma içeriğidir ve git tarafından izlenmez.

Kalıcı `AssetLayoutChecks` kontrolü kök klasör kurallarını, en az 2.300 asset GUID'inin çözümlenmesini, hedef görsel ve mağaza sahnelerindeki eksik scriptleri ve galeri kataloğunun sabit kimliklerini doğrular. Projeye ait 3D model boyama prototipi kaldırılmıştır; üçüncü taraf paketler ve eski arşivler kendi klasörlerinde saklanır.
