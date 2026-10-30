---
id: T-060
title: Liderlik - acilir pencerelerin ve durumlarin ekran goruntusu (bolge+arama, donem, etkinlik sekmeleri, bos, yukleniyor, hata, siralamasiz)
status: merged
owner: test-agent
depends_on: [T-038]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/T-060-liderlik-goruntuleri
commits: [08385dc7]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/ScreenshotTests.cs (Shots_LeaderboardStates, yalniz -shotDir)"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10) liderlik tasarimini tekrar gonderdi ve "koyduguna emin miyiz" diye sordu. Kodda (`Scripts/UI/Leaderboard/LeaderboardScreenView.cs`)
bolge penceresi (arama + ulke listesi + "No countries found"), donem penceresi (All time / This month / This week / Today + ipucu),
etkinlik penceresi (Sprints/Hurdles/Relays sekmeleri + mesafeler + Cancel/Apply + "No events available"), yukleniyor / bos / hata(+Retry)
gruplari ve YOU "NOT RANKED" satiri var; ama ekran goruntusu testlerimiz yalniz ana tabloyu (`Home_leaderboard`) cekiyor. Tasarim:
supervizor scratchpad'i `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\010558a9-f012-4284-b1f2-1285cf76ea87\scratchpad\leader-ui\`
(01_main_screen.jpg, 02_open_filters.jpg, 03_system_states.jpg, LEADERBOARD_MASTER.md).

`ScreenshotTests`'e `Shots_LeaderboardStates` (yalniz `-shotDir`; var olan Home/leaderboard acma yardimcilariyla, **gercek isinli Tap**):
`Home_lb_region` (bolge acik), `Home_lb_region_search_empty` (aramaya "xyz"), `Home_lb_period` (donem acik), `Home_lb_event_sprints`,
`Home_lb_event_hurdles`, `Home_lb_event_relays`, `Home_lb_empty` (kaydi olmayan bir etkinlik/donem secilerek), `Home_lb_notranked`
(oyuncu kaydi yok). Yukleniyor ve hata durumlari yerel liderlikte kendiliginden olusmaz: gorunumde/presenter'da test icin acik bir yol
varsa (public/internal metot, sahte `ILeaderboard` baglama) onunla `Home_lb_loading`, `Home_lb_error`; yoksa urun koduna DOKUNMA,
calisma notuna "su seam gerekir" yaz. Her goruntuden sonra pencereyi kapat (sonraki teste sizmasin).

## Kabul kriterleri
- [ ] Goruntuler `-shotDir` ile uretiliyor; var olan PlayMode testleri yesil
- [ ] Uretilemeyen durum varsa nedeni calisma notunda

## Geri bildirim
GORSEL  : yok (yalniz goruntu)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
1. `ScreenshotTests.Shots_LeaderboardStates` eklendi (yalniz -shotDir; koşulmadı, derlenmedi). Gercek yerel liderlikle: Home_lb_region, _region_search_empty, _period, _event_sprints/_hurdles/_relays (gercek isinli Tap; sunulmayan sekme atlanir, SHOT_SKIP loglanir).
2. Home_lb_empty, _notranked, _loading, _error: yerel liderlik bunlari uretmez; ekranin ozel `_leaderboard` alani reflection ile sahte ILeaderboard'a cevrilir (ilk acilistan once, taze Home sahnesi). Urun koduna dokunulmadi; resmi seam yok, ince yol bu.
3. Pencereler region/period icin cihaz Back'i (ScreenNavigator.Back) ile kapanir (yakalayici pencerenin altinda); event modal Cancel'a gercek Tap ile kapanir. Secim/filtre degismez, kayit SaveFolderSnapshot ile geri yuklenir.
4. Riskler: highScoresScreen sahne yuklenince zaten acik ise sahte-kaynak shotlari `_presenter` null assert'inde duser; alan adlari (_leaderboard, _presenter, categoryTabs, cancelButton, eventModal, searchInput) yeniden adlandirilirsa test mesajla kirilir.
5. Kapsam disi: yukleniyor/hata icin temiz seam (ornegin presenter'a verilebilir ILeaderboard ya da view'a public test kancasi) istenirse urun task'i gerekir.
