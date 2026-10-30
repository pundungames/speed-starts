---
id: T-049
title: Cila - olu kod temizligi, analizde DNF cizgisi, READY/SET/GO olcegi, emote kart yerlesimi
status: review
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
needs:
  - tip: ASSET
    durum: open
    not: "Data/Config/RaceStartConfig.asset: groupScale 0.55 -> 0.8, holdTensionMinAlpha 0.6 -> 0.85 (asset still holds the old defaults; all other fields are default, so re-seed with overwrite or edit in the Inspector). Then rebuild UiRaceStartPrefabs so Widgets/RaceStart.prefab Group scale follows (0.8)."
  - tip: SCENE
    durum: open
    not: "Rebuild UI prefabs (EmoteCard.prefab, EmoteScreen.prefab changed: name above lock, price beside it, list from the panel top, Equipped label inside the button) and commit with the scene; take Race_countdown, Race_analysis, Home_emote screenshots."
started: 2026-10-30
finished: 2026-10-30
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
- Olu kod silindi (grep ile referanssiz kanitlandi, .cs + builder): EventRowView.cs, ResultRowView.cs (.meta'lari supervizor silsin), UiRowPrefabs sinifi (EventRow, CosmeticCell, SevenDayCell, HighScoreRow), UiWidgetPrefabs.RewardFlight (UiRewardPrefabs.Flight kullaniliyor), UiScreenPrefabs.EventSelect/Cosmetics/HighScores/Results + yardimcilari, UiPopupPrefabs.DailyGift/SevenDayCheckIn, UiViewBinder.BindEventSelect/BindResults/BindEventRow/BindResultRow, UiPrefabPaths.EventRow/CosmeticCell/SevenDayCell/HighScoreRow.
- Artik uretilmeyen prefab dosyalari (supervizor silsin, .meta ile): Assets/Speed-Staes/UI/Rows/EventRow.prefab, Rows/HighScoreRow.prefab, Rows/CosmeticCell.prefab, Rows/SevenDayCell.prefab. (Eski EventRow/HighScoreRow prefab'lari silinen scriptlere baglidir; silinmeden Missing Script uyarisi verir.)
- Analiz DNF: SplitSeries.HasLine artik Finished ister; bitirmeyen seri cizgi, nokta ve balon cizmez (3 test: DNF oyuncu, DNF rakip, duz 0 s serisi). Ekran: _hasCursor da HasLine'a bagli.
- READY/SET/GO: RaceStartConfig varsayilanlari groupScale 0.8, holdTensionMinAlpha 0.85 (mevcut asset eski degerde: needs ASSET). Ekran goruntusu eldeki worktree'de yok, onizleme storyboard ile karsilastirildi.
- Emote karti: ad kilidin ustunde tam genislik, fiyat kilidin solunda (kilit art'indan hesaplanir); liste panel ustunden baslar; Equipped etiketi 40 pt, sag kenar 0.86, ortali. Gorsel dogrulama yapilmadi (Unity calistirilmadi).
