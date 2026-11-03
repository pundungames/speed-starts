---
id: T-098
title: Watch Replay kontrolleri prefab'a - ReplayControlsView UI'i koddan kurmaz, builder'in urettigi prefab'i baglar (kural 3.1)
status: merged
owner: ui-agent
depends_on: [T-091, T-094]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/ReplayControlsView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ReplayControlsSkin.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiReplayPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/BuilderHandEditsTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/ReplayControlsPrefabTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/WatchReplaySmokeTests.cs"
touches_scene: false
branch: agent/T-098-replay-prefab
commits: [2fb44ab4, 6f7cd3e3]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/ReplayControlsPrefabTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/BuilderHandEditsTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Kopyada UiPrefabBuilder.BuildAll(overwrite: true): UiReplayPrefabs.BuildAll uretir ReplayControls.prefab; UiResultsPrefabs.BuildAll ResultsScreen.prefab uzerinde replayControlsPrefab baglar (eski replaySkin alani kalkti). Race.unity YENIDEN KURULMAZ; ResultsScreen orada prefab ornegi, referans prefab ile gelir. Sonra Results prefab + ReplayControls prefab git ciktisi alinir. -> 6f7cd3e3 (kopyada 03.11; Race kayitli halinde, 26 referans duruyor)"
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11): "Watch Replay kontrollerini prefab yapabilirsin" (Malik'in kodu; kullanici onay verdi). Bugun `ReplayControlsView.Open(replay,
skin, font)` (Malik, cbefbf82 + e0a7d595) `new GameObject` ile Canvas, CanvasScaler, SafeAreaFitter, butonlar, yazilar ve gorselleri
KODDAN kuruyor; Inspector'da duzenlenemiyor (docs/CALISMA-KURALLARI.md 3.1: UI asla runtime'da kurulmaz). Yapilacak:
1) Builder (`UiReplayPrefabs`, `UiPrefabBuilder.BuildAll` icinde cagrilir; yol `UiPrefabPaths`): bugun kodun kurdugu hiyerarsinin AYNISINI
   prefab olarak uretir (`Assets/Speed-Staes/UI/Widgets/ReplayControls.prefab`): ayni dugumler, yerlesim, renkler, fontlar, sprite'lar.
   Gorseller `UI/Widgets/ReplayControlsSkin.asset`'ten (Malik'in sectigi sanat) builder aninda okunur ve prefab'a islenir. Kanvas ayarlari
   (sortingOrder, referans cozunurluk, ScreenMatchMode, SafeAreaFitter) bugunku kodla ayni.
2) `ReplayControlsView` artik hiyerarsi KURMAZ: hazir prefab'in alanlarini `[SerializeField] private` ile baglar ve doldurur (davranis -
   oynat/duraklat, hiz, sarma, kamera, cikis, bitince kapanma - aynen). `Open(...)` prefab'i Instantiate eder (prefab referansi
   ResultsScreenView'da `[SerializeField]`, builder `UiResultsPrefabs`'ta baglar - T-091'in replaySkin baglamasi gibi). Skin runtime'da artik
   gerekmiyorsa ResultsScreenView.replaySkin alani kalkar ve T-091'in testi (BuilderHandEditsTests) prefab referansini dogrulayacak sekilde
   guncellenir; skin asset'i builder girdisi olarak kalir.
3) Davranis degismez: T-094'un WatchReplaySmokeTests'i (4 test, gercek tiklama) yesil kalmali; testin runtime-kurulum varsayimi varsa guncelle.
Malik'in `Scripts/Physical/**` (RaceRecorder, RacePlayback, ReplayCamera) ve `Bridge/RaceReplayAdapter.cs` okunur, YAZILMAZ.
Prefab/sahne/asset dosyasina YAZMA; builder'i calistirma (merkezi dogrulama).

## Kabul kriterleri
- [ ] ReplayControls.prefab builder'la uretilir; ReplayControlsView'da `new GameObject` / `AddComponent` ile UI kurulumu kalmaz
- [ ] Watch Replay eskisi gibi calisir (WatchReplaySmokeTests yesil); gorunum bugunkuyle ayni
- [ ] EditMode: uretilmis prefab'ta dugumler + view alanlari bagli; ResultsScreen prefab'inda prefab referansi dolu
- [ ] needs'e SCENE: supervizor kopyada UI prefab'larini yeniden uretir (Race YENIDEN KURULMAZ; ResultsScreen Race'te prefab ornegi)

## Kapsam disi
- Replay mekanigi (Malik), yeni ozellik, gorunum degisikligi

## Calisma notu

- ReplayControlsView artik hiyerarsi kurmaz: 12 [SerializeField] alani baglar, Open(prefab, replay) prefabi Instantiate eder.
- UiReplayPrefabs: eski koddaki dugum/rect/renk/kanvas degerleri birebir, skin asset builder girdisi (yoksa duz panel + uyari).
- ResultsScreenView: replaySkin kalkti, replayControlsPrefab geldi (bossa Watch Replay kapali); UiResultsPrefabs baglar.
- WatchReplaySmokeTests degismedi (Find("Exit") dogrudan cocuk, prefab de oyle); builder/Unity/test CALISTIRILMADI.
- Risk: eski kodda kok varsayilan katmandaydi, prefab UI katmaninda; skin.font bossa Results fontu yerine yerlesik font kullanilir.
- Supervizor 03.11: kopyada builder + EditMode 1160, Zenject, PlayMode 51/51 yesil (Watch Replay testleri prefab ile gecti).
