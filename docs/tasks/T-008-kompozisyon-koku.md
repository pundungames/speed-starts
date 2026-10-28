---
id: T-008
title: Kompozisyon koku (ProjectInstaller, HomeSceneInstaller, RaceSceneInstaller)
status: todo
owner: supervisor
depends_on: [T-003, T-004, T-005]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Installers/**"
touches_scene: false
branch: agent/faz1-iskele
commits: []
tests:
  - "gerekmiyor: baglama; kabul Home/Race Play'de konsol temiz (T-006 ile)"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Sözleşme §4: `ProjectInstaller` (config SO referansları, IClock, ISaveStore, ProgressRepository, ScreenNavigator, SceneFlow),
`HomeSceneInstaller`, `RaceSceneInstaller` (IRaceSession → Faz 2'de Bridge adaptörü). T-003/T-004/T-005'in WIRING needs'lerini kapatır.
Asmdef'siz (Assembly-CSharp), namespace `SpeedStars.Installers`.

## Kabul kriterleri
- [ ] T-003/T-004/T-005 WIRING needs `closed`
- [ ] Kopyada derleme yeşil

## Çalışma notu
