---
id: T-002
title: Assembly tanimlari ve Core seam'leri (ortak dosyalar)
status: review
owner: supervisor
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Runtime/**"
  - "Assets/Speed-Staes/Scripts/Meta/SpeedStars.Meta.asmdef"
  - "Assets/Speed-Staes/Scripts/UI/SpeedStars.UI.asmdef"
  - "Assets/Speed-Staes/Tests/EditMode/SpeedStars.Tests.asmdef"
touches_scene: false
branch: agent/faz1-iskele
commits: []
tests:
  - "gerekmiyor: yalniz arayuz ve veri tipleri; davranis T-003..T-005 testlerinde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Paralel task'lar (T-003, T-004, T-005) çakışmasın diye ortak dosyaları süpervizör kurar (`agent-kit/07-AKIS.md` §4):
`SpeedStars.Core` (Scripts/Core/Runtime), `SpeedStars.Meta`, `SpeedStars.UI`, `SpeedStars.Tests` asmdef'leri ve sözleşme §4'teki
Core seam'leri: `IRaceSession`, `RaceEventId`, `RaceStartRequest`, `RaceOutcome`, `RaceSessionState`, `IClock`, `ISaveStore`.

## Kabul kriterleri
- [x] Dört asmdef, sözleşme §3 ok yönleriyle (Meta → Core; UI → Core, Meta, Zenject, TMP, uGUI; Tests → hepsi + TestRunner)
- [x] Core seam'leri; Core'un başka assembly'ye referansı yok
- [ ] Kopyada derleme yeşil (T-003..T-005 ile birlikte)

## Çalışma notu
Installers ve Bridge asmdef'siz kalır (Assembly-CSharp; Malik'in koduna erişmek için). `ILeaderboard` Faz 6'da açılır.
