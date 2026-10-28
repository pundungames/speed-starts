---
id: T-007
title: Yer tutucu UI prefab'lari (ekranlar, popup'lar, widget'lar, satirlar)
status: todo
owner: scene-integrator
depends_on: [T-005, T-006]
owns_paths:
  - "Assets/Speed-Staes/UI/**"
touches_scene: true
branch: agent/faz1-iskele
commits: []
tests:
  - "gerekmiyor: prefab; kabul gozle"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Sözleşme §5 prefab listesi (`UI/Screens`, `UI/Popups`, `UI/Widgets`, `UI/Rows`): HomeScreen, EventSelectScreen, CosmeticsScreen,
HighScoresScreen, RaceHud, ResultsScreen, EntryGateModal, DailyGiftPopup, SevenDayCheckInPanel, ResourceBar, RedDot, RewardFlight,
EventRow, CosmeticCell, SevenDayCell, HighScoreRow. Yatay 1920×1080, TMP metinler, butonlar; `UiScreen`/`UiPopup` taban bileşenleri (T-005).
Yerleşim referansı `docs/gdd/referans-ekranlar/` ve PDF ekran görüntüleri. Home/Race sahnelerinde §5 hiyerarşisine yerleştirilir.
Görsel taslak; ikonlar yer tutucu (Serhat sonra değiştirir). View mantığı yok (Faz 2).

## Kabul kriterleri
- [ ] Bütün prefab'lar Inspector'dan düzenlenebilir, sahnede görülür
- [ ] Kodla UI kurulmaz; hiçbir prefab runtime'da üretilmez

## Çalışma notu
