---
id: T-088
title: Ayarlar v2 - referanstaki bolumler (Competitors, Gameplay, Visuals, Emote, Reset options) bizim sistemlere bagli olanlar
status: review
owner: ui-agent
depends_on: [T-084, T-086]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Settings/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/SettingsContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/ProgressRepository.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Economy/RaceCashRewardResolver.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/Bots/**"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBots.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/SettingsRunnerAdapter.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiSettingsPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/**"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/**"
touches_scene: false
branch: agent/T-088-ayarlar-v2
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Settings/SettingsV2Tests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/BotSettingsOverrideTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/RaceCashRewardResolverTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Settings/ResetConfirmFlowTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "SettingsPopup prefab'ina yeni bolumleri ekle (tasarimcinin elle duzenlemelerine dokunmadan, yalniz ExtraSections dugumu + siralama): kopyada Unity batchmode, TEK komut: -executeMethod SpeedStars.Tools.Builders.SettingsV2Rebuild.Run (UiSettingsPrefabs.cs icinde; basari isareti log'da SETTINGS_V2_REBUILD_OK; baska prefab/sahne/config yazmaz). Uretir: SettingsPopupView.extraSections -> ExtraSections (SettingsExtraSectionsView: Competitors/Gameplay/Visuals/Emote/Reset options satirlari). Sonra prefab'da extraSections ve satir alanlarinin fileID: 0 olmadigini grep ile dogrula. Yeni .cs dosyalarinin .meta'lari kopyada uretilir, geri alinip commit'lenir (SettingsRules, BotLanes, SettingsRunnerAdapter, SettingsExtraSectionsView, SettingsChoiceRowView, SettingsSliderRowView, SettingsToggleRowView, iki yeni test dosyasi)."
  - tip: DECISION
    durum: open
    not: "Time trial'da rakip yok (RunnerCount 1). RaceRewardResolver (Assets/Speed-Staes/Scripts/Meta/Economy/RaceRewardResolver.cs, bu task'in yolu degil) Mor Elmas'i yalniz rank'a ve awardsPurpleGems'e bakarak verir: gem ceren bir etkinlikte Time trial secen oyuncu rank 1 ile elmas kazanir. Cash tarafi (RaceCashRewardResolver) rakipsizde zaten 0. Karar: RaceRewardResolver'a rakipsiz kosuda (RunnerCount < 2) 0 koşulu eklensin mi (onerilen: evet)."
  - tip: DECISION
    durum: open
    not: "Zorluk bonusu artik round(config bonusu x oyuncu zorlugu): varsayilan zorluk 0.5 oldugu icin yeni oyuncuya bugunku sabit bonusun yarisi odenir (gorev metni bunu istiyor). Bugunku tam bonusu korumak istenirse SettingsConfig.defaultCompetitorDifficulty 1 yapilmaz (bot sahasini da degistirir); ayri 'bonus carpani' alani gerekir."
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11): "ayarlar kismini yap" (referans ne ise o). Referans ekranlari: `docs/eksik-ui/08-ayarlar-1.png`, `09-ayarlar-2.png`,
`10-ayarlar-3.png`. Bizde bugun: Muzik, Dil, Bayrak, Ad, Kamera mesafesi (Malik fd891f4d), yasal satirlar (URL gelene kadar gri),
ilerleme sifirlama (T-046). Eklenecekler - yalniz bizim sisteme baglanabilenler, her biri kayitli ve yeniden baslatmada korunur:
1) **Competitors**: Competitor type = Bots / Time trial (rakipsiz, Practice gibi solo); Difficulty kaydiricisi (0..1) ve Skill range
   kaydiricisi (0..1) bugun `BotRaceConfig.Difficulty/SkillRange`'ten okunan degerin yerine oyuncu ayarindan (varsayilan = config degeri).
   Referansta "Difficulty bonus" zorlukla olcekli (en dusukte 0, yuksekte +10): `RaceCashRewardResolver` zorluk bonusu =
   round(config zorluk bonusu x oyuncu zorlugu). Race my ghost / Global players / Friends YOK (ghost sistemi yok) - satir eklenmez.
2) **Gameplay**: Lane preference (oyuncunun serit sirasi, RaceBots seritleri; "?" = otomatik), Hurdle input sensitivity (Malik'in
   `RunnerInput.swipeUpDistance` alanina oyuncu ayarindan yazan kucuk Bridge adaptoru; Malik'in dosyasina yazilmaz).
3) **Visuals**: Particle effects (acik/kapali: adim geri bildirimleri ve premium kozmetik parilti/iz efektleri gizlenir - bilesenleri kapatarak,
   Serhat/Malik dosyasina yazmadan); Camera distance zaten var. Screen stretch, Classic animations, Cadence meter, Tip notifications
   bizde karsiligi yok -> satir eklenmez, calisma notuna yaz.
4) **Emote**: Show emote button, Emote automatically: Myself / Competitors - yalniz kayitli ayar degeri + satir. `IPlayerSettings`'e su adlarla:
   `bool ShowEmoteButton`, `bool AutoEmoteSelf`, `bool AutoEmoteCompetitors` (+ setter'lar, Changed). Davranisi T-089 yazar.
5) **Reset options**: Clear custom racers (Custom 1..9 preset'leri varsayilana), Reset a personal best (etkinlik secip o etkinligin PB'si),
   Restart all progress (var). Onay penceresiyle. Delete account = Restart all progress ile ayni (yerel oyun) - ayri satir gerekmiyorsa ekleme.
UI kalici olarak kodla kurulmaz: satirlar `UiSettingsPrefabs` builder'inda, view baglar (CALISMA-KURALLARI §3.1). Metin Ingilizce.
Bot zorlugu ayari BotFieldPlanner'a disaridan girer (config + oyuncu degeri); 100m ve diger etkinlik tablolari degismez.

## Kabul kriterleri
- [ ] Her yeni ayar kaydedilir, yeniden baslatmada korunur, "Restart all progress" ayarlari silmez (bugunku kural neyse ona uy, testle)
- [ ] Difficulty/Skill range bot sahasini degistirir (iliski testi); zorluk bonusu zorlukla olcekli
- [ ] Time trial'da rakip yok (Practice gibi); Lane preference oyuncunun seridini degistirir
- [ ] Particle effects kapaliyken efekt bilesenleri calismaz
- [ ] EditMode testleri; sabit denge sayisi yok; needs'te WIRING/SCENE tam

## Kapsam disi
- Ghost / global oyuncular / arkadaslar; Screen stretch, Classic animations, Cadence meter, Tip notifications; emote davranisi (T-089)

## Calisma notu
1. Eklendi (hepsi kayitli, eski kayit acilir, varsayilan config'te): Competitor type (Bots/Time trial), Difficulty, Skill range, Lane preference (Auto, 1..N), Hurdle input sensitivity, Particle effects, Show emote button, Emote myself/competitors, Clear custom racers, Reset a personal best (etkinlik secimli); satirlar tek bir ExtraSections dugumunde, builder'da.
2. Baglandi: bot sahasi BotFieldPlanner.Plan(difficulty, skillRange) (yalniz oyuncu dokunduysa; dokunmadiysa BotRaceConfig kendi degeri), Time trial RaceBots.HasOpponents, serit RaceBots.StartPositionOf + BotLanes (oyuncu sahnedeki yerinde, botlar diger seritleri doldurur: kamera tarafina gecen bot oyuncuyu kapatabilir), zorluk bonusu RaceCashRewardResolver (ctor'a istege bagli IPlayerSettings), hurdle + particle SettingsRunnerAdapter (RaceBots otomatik ekler, sahne degisikligi yok).
3. Eklenmedi: Race my ghost / Global players / Friends (ghost yok), Screen stretch, Classic animations, Cadence meter, Tip notifications (bizde karsiligi yok), Delete account (Restart all progress ile ayni). Emote davranisi T-089'un; burada yalniz IPlayerSettings.ShowEmoteButton/AutoEmoteSelf/AutoEmoteCompetitors + satirlar (auto varsayilanlari kapali: bugun otomatik emote yok).
4. Zorlandigim: IPlayerSettings buyudugu icin iki test sahtesi (AudioServiceTests, LocalLeaderboardTests) kapsam disi yollarda minimal guncellendi; T-089 ayni yeri ekleyecek, birlestirmede cakisma olabilir. Zenject/Unity/test kosulmadi.
5. Birakilan: SCENE (SettingsV2Rebuild.Run) ve iki DECISION (time trial elmasi, zorluk bonusu carpani) needs'te; WIRING gerekmedi (IPlayerSettings zaten bagli, resolver ctor'u istege bagli parametre).
