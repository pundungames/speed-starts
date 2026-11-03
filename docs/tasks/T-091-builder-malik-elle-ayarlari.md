---
id: T-091
title: Builder'a Malik'in 03.11 elle ayarlari - EventModal ve 7 gun Close sag kenara sabit, ResultsScreen replaySkin
status: todo
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiLeaderboardPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRewardPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/**"
touches_scene: false
branch: agent/T-091-builder-malik-ayarlari
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
Malik 03.11'de builder ciktisini Editor'de elle duzeltti (merge e7d269f2); builder bunlari bilmedigi icin bir sonraki
`UiPrefabBuilder.BuildAll` / `SceneBuilder.BuildHome` onlari sessizce siler (docs/TUZAKLAR.md, "Builder ciktisinda git'in
satir birlestirmesi"). Ayarlari builder'a tasi, boylece prefab'in kendisi dogru uretilsin:
1) `HighScoresScreen/EventModal/Window/Close` (UiLeaderboardPrefabs, ~618. satir `At(window, "Close", ...)`): Malik Home sahnesinde
   override ile pencerenin SAG kenarina sabitledi: anchorMin.x = anchorMax.x = 1, pivot.x = 1, anchoredPosition = (0, 0), boyut ayni
   (y ekseni, ust kenar: anchor/pivot y = 1 aynen). Bugun sol uste gore x = 690 ile yerlesiyor; pencere genisleyince buton icerde kaliyor.
2) `SevenDayCheckInPanel/Window/Close` (UiRewardPrefabs, ~240. satir `PlaceTopLeft(close, layout, CloseBox)`): ayni duzeltme
   (anchor/pivot x = 1, anchoredPosition (0, 0), boyut CloseBox'taki gibi).
   Degerleri gormek icin: `git show 6b6f1914 -- Assets/Speed-Staes/Scenes/Menu/Home.unity` (hedef fileID 3782173811630114901 =
   EventModal Close, 2013364779080102800 = 7 gun Close).
3) `ResultsScreen` prefab'i: Malik `ResultsScreenView.replaySkin` alanini elle `Assets/Speed-Staes/UI/Widgets/ReplayControlsSkin.asset`'e
   bagladi (cbefbf82). UiResultsPrefabs bu alani baglamiyor (~267. satirdaki `Ref(so, "watchReplayButton", ...)` yaninda): asset'i
   yukleyip bagla (asset yoksa builder hata loglasin, sessiz gecmesin).
Diger Close butonlarina, yerlesime, renge DOKUNMA. Builder'i calistirma (merkezi: supervizor kopyada calistirir).

## Kabul kriterleri
- [ ] Builder calisinca iki Close prefab'ta sag kenara sabit (anchor/pivot x = 1, anchoredPosition (0, 0)); ResultsScreen.replaySkin dolu
- [ ] EditMode testi: uretilmis prefab'lari (AssetDatabase) okuyup bu uc ayari dogrular - gelecekte bir builder degisikligi silerse kirmizi
- [ ] needs'e SCENE: supervizor kopyada `UiPrefabBuilder.BuildAll(true)` + `SceneBuilder.BuildHome(true)` kosar (Race YENIDEN KURULMAZ;
      Race'in prefab fileID'leri yeni prefab'larda duruyor mu bakilir)

## Kapsam disi
- Malik'in `ReplayControlsView` kodu (UI'i runtime'da kuruyor; ayri karar), diger popup'larin Close'lari

## Calisma notu
