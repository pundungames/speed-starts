---
id: T-049
title: Cila - olu kod temizligi, analizde DNF cizgisi, READY/SET/GO olcegi, emote kart yerlesimi
status: todo
owner: ui-agent
depends_on: [T-036, T-039, T-040]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/EventRowView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultRowView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/Analysis/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceStart*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Emotes/**"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/RaceStartConfig.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRowPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiScreenPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiWidgetPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPopupPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEmotePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRaceStartPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/RaceStartConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/**"
  - "docs/tasks/_backlog.md"
touches_scene: true
branch: agent/T-049-cila
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/AnalysisChartModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
`docs/tasks/_backlog.md`'deki dört madde:
1) **Ölü kod**: T-024/T-033/T-038/T-043 sonrası kullanılmayan `EventRowView`, `ResultRowView`, `UiRowPrefabs.EventRow` (ve artık
   kullanılmıyorsa `HighScoreRow`), `UiViewBinder.BindEventSelect`/`BindResults`, `UiScreenPrefabs.EventSelect`/`Results`/`HighScores` yer
   tutucuları, eski `DailyGift`/`SevenDayCheckIn`/`RewardFlight` yer tutucu builder'ları. Silmeden önce grep ile referans yok kanıtı; artık
   üretilmeyen prefab dosyalarının (Rows/EventRow.prefab vb.) listesini çalışma notuna yaz — .prefab dosyalarını süpervizör siler.
2) **Analiz DNF**: bitirmeyen serinin çizgisi ve balonu çizilmez (IMPLEMENTATION.md: eksik örnek 0 sayılmaz); testli.
3) **READY/SET/GO ölçeği**: yazı tasarımdakinden küçük, SET gerilim çizgileri görünmüyor
   (`pipeline-logs/shots-t039-2026-10-30_112551/Race_countdown.png`) → `RaceStartConfig`/builder ölçeği ve efekt alfası tasarım
   önizlemesine yaklaşsın (scratchpad `baslangic-ui\Yaris_Baslangic_UI_v1\storyboard.jpg`).
4) **Emote kartı**: kilit ikonu ad metninin üstüne biniyor (Number One, Salute), panelin üstü boş, "Equipped" etiketi taşıyor
   (`pipeline-logs/shots-t040b-2026-10-30_113358/Home_emote.png`) → ad ve fiyat kilit alanından ayrılsın, liste üstten başlasın, buton etiketi sığsın.
Bitenleri `_backlog.md`'den sil.

## Kabul kriterleri
- [ ] Derleme + bütün testler yeşil (silinen kod referanssız)
- [ ] Görüntüler: `Race_countdown`, `Race_analysis`, `Home_emote` düzelmiş

## Calisma notu
(Agent doldurur)
