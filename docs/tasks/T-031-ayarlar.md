---
id: T-031
title: Ayarlar penceresi (Settings_Moduler_UI_v1) - Music, Language, Flag, Player Name, Done/X; ana ekran ses butonu
status: review
owner: ui-agent
depends_on: [T-026, T-029]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Settings/**"
  - "Assets/Speed-Staes/Scripts/UI/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/SettingsContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiSettingsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SettingsConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/**"
touches_scene: true
branch: agent/T-031-ayarlar
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/PlayerSettingsServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/PlayerNameRulesTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/PresetStepperTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: SettingsConfig + IPlayerSettings AsSingle (exact lines in Calisma notu)"
  - tip: SCENE
    durum: open
    not: "ProjectContext prefab: assign Data/Config/SettingsConfig.asset to the new ProjectInstaller 'settings' field. Then run the Home scene builder (it nests SettingsPopup under Canvas Popups and binds HomeScreenView.settingsPopup) and rebuild the UI prefabs with overwrite = true (HomeScreen/EventSelect keep nothing new; SettingsRow, LanguageRow, FlagCell, SettingsPopup are new; UiV2Prefabs now builds Settings/Sound/Environment/Previous/Next interactable, so HomeScreen needs the overwrite rebuild)."
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Ana ekranın sağ üstündeki dişli (settings) butonu Ayarlar penceresini açar. Sanat: `C:\Users\Monster\Downloads\Settings_Moduler_UI_v1.zip`
(açılmış hâli süpervizör scratchpad'inde `settings-ui\`; `IMPLEMENTATION.md` = hiyerarşi ve davranış, `manifest.json` = grup tuvalleri,
9-slice kenarları ve bayrak kod→ad eşlemesi; `preview_settings.jpg`, `preview_music_off.jpg`, `preview_flag_popup.png`,
`approved_design_reference.jpg` kontrol görselleri; layout.json YOK — yerleşim IMPLEMENTATION.md'deki ölçülerle: satır 752×112, aralık 8,
header 128, footer 112, ScrollRect + Content'te ContentSizeFitter). Satırlar tek bir **satır prefab'ı**ndan çoğaltılır (ikon + etiket +
sağda kontrol yuvası); popup'lar (dil listesi, bayrak ızgarası) panel viewport'unda kırpılmaz, en üst katmanda.

Kararlar (kullanıcı kararları süpervizöre bıraktı; hepsi config asset'inde, kod sabiti değil):
- **Music**: aç/kapa, anında kaydedilir; yalnız müzik (ses efektleri ayrı). Varsayılan açık. Projede müzik yok: değer `IPlayerSettings`'te
  durur, `Changed` olayı yayınlanır (ses sistemi gelince dinler).
- **Ana ekran hoparlör butonu** (şu an "yapılmamış" listesinde): ses efektlerini aç/kapa (`SfxOn`), kapalıyken ikon soluk (yer tutucu; sanatta
  kapalı ikon yok).
- **Language**: liste config'ten (kod + kendi dilindeki ad); bugün yalnız `en` / "English" — çeviri sistemi yok, seçim kaydedilir.
- **Flag**: paketteki 16 bayrak (TR, US, GB, DE, FR, IT, NL, BE, PL, UA, JP, SE, NO, DK, FI, CH), ızgara popup; varsayılan TR (tasarımdaki
  gibi). CN bayrağı pakette yok (Çin pazarı) → not düş, eklenince config'e bir satır.
- **Player Name**: boşluklar kırpılır, 1-16 karakter (Unicode harf/rakam/boşluk/`_`/`-`; Çince ad geçerli), kontrol karakteri yok;
  geçersizse field_error + kısa hata metni, kaydedilmez. Varsayılan "Player". İleride High Scores (Faz 6) bu adı ve bayrağı kullanır.
- **Done**: açık isim düzenlemesini doğrulayıp kaydeder, kapatır. **X**: kaydedilmemiş isim metnini atar, kapatır. Diğer ayarlar anında kayıt.
- Dimscreen tam ekran, arkadaki menüye tıklamayı keser (overlay; bu bir popup, karakteri karartması sorun değil).

## Kabul kriterleri
- [ ] `IPlayerSettings` (Meta/Contracts/SettingsContracts.cs): MusicOn, SfxOn, LanguageCode, FlagCode, PlayerName, setter'lar/TrySetName → sonuç
      enum'u, Changed; `PlayerProgress`'te yeni alanlar, eski kayıt varsayılanla açılır (şema sürümü gerekiyorsa artar, ADR kısa)
- [ ] Saf `PlayerNameRules` + servis EditMode testli (sınırlar config'ten okunur, sabit sayı assert edilmez)
- [ ] Prefab'lar builder'dan (kod kalıcı UI kurmaz): ayarlar popup'ı, satır prefab'ı, dil satırı, bayrak hücresi; 9-slice kenarları manifest'ten
      sprite import ayarı olarak (builder TextureImporter.spriteBorder yazar)
- [ ] `UiPrefabPaths.HomePopups`'a eklenir, `SceneWiring.WireHome` HomeScreenView'daki yeni `settingsPopup` alanını bağlar
- [ ] Legacy Text + legacy `InputField` (TMP yok), İngilizce, best fit; DOTween yalnız çekirdek (TUZAKLAR)
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : toggle thumb DOTween çekirdeğiyle kayar; seçilen bayrak hücresi punch; hatalı isimde alan kırmızı + hafif sallanma
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Çeviri/yerelleştirme sistemi, müzik/ses içeriği, High Scores (Faz 6), slider satırları (sanatta hazır, kullanılmıyor)

## Calisma notu
- Yapildi: Meta (IPlayerSettings, SettingsConfig, PlayerNameRules, PlayerSettingsService, PlayerProgress'e 5 alan, semaya dokunulmadi: JsonUtility eksik alani varsayilanla acar), UI (SettingsPopupView/ToggleView/LanguageRowView/FlagCellView/SettingsTexts), builder (UiSettingsPrefabs + seed + yollar + SceneWiring), HomeScreenView (dis, hoparlor, preset oklari), UiV2Prefabs 5 dugme interactable. PresetStepper dosyasi Scripts/UI/Settings'te (UI/Home bana ait degil), namespace SpeedStars.UI.Home.
- WIRING satirlari (ProjectInstaller): alan `[SerializeField] private SettingsConfig settings;` + `BindConfig(settings);` + `Container.Bind<IPlayerSettings>().FromMethod(c => new PlayerSettingsService(c.Container.Resolve<SettingsConfig>(), c.Container.Resolve<ProgressRepository>())).AsSingle();` (using SpeedStars.Meta.Settings). IPlayerSettings ve ICosmeticService HomeScreenView'a [InjectOptional]; SettingsPopupView'da IPlayerSettings zorunlu.
- Zorlandim: Unity/derleme calistirmak yasak, hicbir sey derlenmedi; art'ta layout.json yok, yerlesim IMPLEMENTATION.md olculerinin x1.15 (1080/937) hali; scrollbar PNG'si 32x512 tuvalde sadece ustte pill, builder kirpilmis kopya uretiyor (scrollbar_*_tight.png). Yeni PNG ve .cs icin .meta'lar yok (hook), kopyada derleyip GeriAl ile alinmali.
- Birakildi: CN bayragi (pakette yok; config'e bir satir), slider satirlari, ses/Music icerigi (sadece MusicOn/SfxOn degeri ve Changed), hoparlor kapali ikonu yer tutucu (alfa 0.4).
- Not: sprite yerlesimleri (bayrak boyu, toggle ON/OFF yazi konumu, popup konumlari) onayli referanstan tahmin; ekran goruntusuyle ince ayar gerekebilir. PlayMode: Home'da dis/hoparlor/oklar tiklama akisi kopyada kosulmali.
