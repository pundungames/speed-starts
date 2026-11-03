---
id: T-088
title: Ayarlar v2 - referanstaki bolumler (Competitors, Gameplay, Visuals, Emote, Reset options) bizim sistemlere bagli olanlar
status: todo
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
tests: []
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
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
(Agent doldurur: 5 satir)
