---
id: T-031
title: Ayarlar penceresi (Settings_Moduler_UI_v1) - Music, Language, Flag, Player Name, Done/X; ana ekran ses butonu
status: todo
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
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: SettingsConfig + IPlayerSettings AsSingle"
started:
finished:
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
(Agent doldurur)
