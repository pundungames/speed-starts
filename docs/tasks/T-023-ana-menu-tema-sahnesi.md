---
id: T-023
title: Ana menu Serhat'in tema sahnesinde, karakter kaidede, tema degistirme (environment butonu)
status: merged
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneHomeCharacter.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneHomeTheme.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ThemeCatalogSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/ShotTool.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Themes/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/ThemeContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuTheme*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/**"
touches_scene: true
branch: agent/T-023-ana-menu-tema
commits: [0fc8f43]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/ThemeServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "ProjectInstaller: field [SerializeField] ThemeCatalog themes + BindConfig(themes); Container.Bind<IThemeService>().FromMethod(c => new ThemeService(c.Container.Resolve<ThemeCatalog>(), c.Container.Resolve<ProgressRepository>(), c.Container.Resolve<WalletService>(), Application.isEditor || Debug.isDebugBuild)).AsSingle() (using SpeedStars.Meta.Themes). ProjectContextBuilder: Prop(installerSo,'themes') = LoadRequired<ThemeCatalog>(ConfigAssetSeeder.ThemeCatalogPath, seeder). HomeSceneInstaller: nothing (MenuThemeView is injected by the SceneContext). -> 0fc8f43"
  - tip: SCENE
    durum: closed
    not: "Run Faz1Builder.RebuildAll (seeds ThemeCatalog.asset, rebuilds Home from Serhat's scene). HomeScreenView.themeSelectScreen stays empty until the ThemeSelectScreen prefab exists (T-027): SceneWiring.WireHome must set it then. -> 0fc8f43"
  - tip: DECISION
    durum: closed
    not: "Tema fiyatlari (Nakit) kullanicida; ThemeCatalog asset'inde 0 = fiyat yok. Yaris sahnesi secili temayi kullansin mi (Malik/Serhat) - bu task'ta yok -> 0fc8f43"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Ana menü (Home) Serhat'ın `Assets/Speed-Staes/Scenes/Anamenu Themaları.unity` sahnesinin **kopyası** üzerine kurulur: ışığı (Menu Sun),
skybox'ı, kamerası (`Main Menu Camera`, FOV 36, konum/açı aynen), 5 tema görüntüsü ve `MainMenuThemeShowcase` ile. Serhat'ın dosyasına
yazılmaz; builder `AssetDatabase.CopyAsset` ile `Scenes/Menu/Home.unity`'yi üretir (Race'in `CopyPhysicalTest` yolu gibi), sonra bizim
nesnelerimizi ekler (SceneContext + HomeSceneInstaller, Canvas + ekranlar, EventSystem). Kopyadaki `Main Menu Character - Existing Model`
(unpack edilmiş kopya, Malik'in script'leri kapalı) kaldırılır; yerine **Malik'in prefab'ının instance'ı** (`SceneHomeCharacter`'ın bugünkü
yolu: MenuCharacter + MenuIdlePose, input/debug kapalı) aynı ebeveyne (`Foreground Character Platform`) aynı yerel konum/açıyla konur. Bizim
düz mavi kamera, kaide ve ek ışık bu sahnede kurulmaz (Serhat'ınki kullanılır). Showcase'in OnGUI paneli kapanır (`showSelection = false`,
sahne override'ı). Ana ekranın sağ üstündeki **environment** (resim ikonlu) butonu temayı değiştirir: tasarımı yok, şimdilik her basış bir
sonraki temaya geçer (5'ten sonra 1'e döner). Temalar ileride Nakit ile satın alınacak: `ThemeService` sahiplik + satın alma kuralını
şimdiden taşır, ama geliştirme sürümünde (Editor ya da Development Build) bütün temalar serbest (kullanıcı: "edit mode'dayız, direkt geçiş").
Seçili tema kayıtta durur; Home açılınca kayıttaki tema gösterilir.

**Kapsam değişikliği (kullanıcı, tema seçim ekranı tasarımı geldi):** `environment` butonu döngüsel değiştirmez, `themeSelectScreen` (UiScreen, T-027) açar.
`IThemeService`: Active (kayıtlı) / Shown (önizlenen), BeginBrowse/Preview/Step (uçta durur)/CancelBrowse/Use/TryPurchase, `ThemeActionState`
(saf `ThemeActionRules`); geliştirmede her tema sahip sayılır. `MenuThemeView` ShownChanged'i izler, gezinirken karakter+kaideyi gizler.

## Kabul kriterleri
- [ ] `Faz1Builder.RebuildAll` Home'u Serhat'ın sahnesinin kopyasından üretir; Serhat'ın `.unity` dosyası değişmez (`git diff` boş)
- [ ] Home'da kamera, ışık, skybox Serhat'ınkiyle aynı; karakter kaidenin üstünde, kameraya dönük; ekran görüntüsü `docs/gdd` dışındaki
      tasarımla (`Assets/Speed-Staes/UI/Art/Home/preview.jpg`) aynı kadraj: karakter ortada, tam boy, kaide altta
- [ ] `ThemeCatalog` (ScriptableObject, `Data/Config/ThemeCatalog.asset`): 5 tema; id (kalıcı), görünen ad (İngilizce), showcase sırası,
      Nakit fiyatı (0 = fiyat yok), varsayılan tema. Tohum builder'ı var olan asset'i ezmez
- [ ] `IThemeService` (kapsam degisikligi notu: Active/Shown/Browse/Preview/Step/CancelBrowse/Use/TryPurchase/GetActionState); satın alma
      `IWallet` üzerinden Nakit, idempotent; seçim ve sahiplik `PlayerProgress`'te (yeni alanlar, eski kayıt boş listeyle açılır)
- [ ] `MenuThemeView` (Bridge, asmdef'siz): `IThemeService.Changed`'i dinler, `MainMenuThemeShowcase.SelectTheme(index)` çağırır; açılışta
      kayıttaki temayı uygular
- [ ] HomeScreenView: environment butonu "yapılmamış" listesinde değil; basınca `themeSelectScreen` açılır
- [ ] EditMode testi: SelectNext döngüsü, sahip olunmayan tema geliştirme dışı modda seçilemez, satın alma parayı bir kez düşer, yetersiz
      para reddedilir, kayıt geri yüklenir. Sabit fiyat assert edilmez (fiyat test içinde kurulan katalogdan)
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : buton ButtonFeedback (var olan scale punch); tema değişince sahne anında değişir
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok (kamera sabit; showcase kamera ön ayarları kapalı kalır)

## Kapsam disi
- Tema seçim ekranı / satın alma UI'ı (tasarımı yok), yarış sahnesinde tema
- Serhat'ın sahnesine, prefab'larına, `MainMenuThemeShowcase.cs`'e yazmak (Serhat'ın; değişiklik gerekirse BLOCKED ile yaz)
- Malik'in prefab'ı ve kodu (hook kilitli)

## Calisma notu
1. Home artık Serhat'ın sahnesinin kopyası (SceneBuilder.CopyScene); SceneHomeCharacter unpacked modeli Malik prefab instance'ıyla değiştirir, SceneHomeTheme showcase panelini kapatır + MenuThemeView ekler; kendi kamera/kaide/ışık kaldırıldı.
2. Meta/Themes: ThemeCatalog, ThemeService, ThemeActionRules; PlayerProgress'e selectedThemeId/ownedThemeIds; ThemeCatalogSeed (5 tema, fiyatlar 0 = karar yok) ConfigAssetSeeder'a bağlandı.
3. HomeScreenView: environment -> themeSelectScreen; ShotTool.CaptureHomeThemes (-shotDir, isteğe bağlı -shotUi) her tema için home_theme_N.png.
4. Test: ThemeServiceTests (durum tablosu, Back geri yükleme, idempotent satın alma, kayıt geri yükleme). Unity/derleme ÇALIŞTIRILMADI.
5. Açık: WIRING + SCENE (yukarıda); Serhat'ın showcase'i ok tuşları/G ile temayı servisten habersiz değiştirebilir (yalnız Play).
