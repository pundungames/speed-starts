---
id: T-008
title: Kompozisyon koku (ProjectInstaller, HomeSceneInstaller, RaceSceneInstaller)
status: merged
owner: supervisor
depends_on: [T-003, T-004, T-005]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Installers/**"
touches_scene: false
branch: agent/faz1-iskele
commits: [c3dbe46]
tests:
  - "gerekmiyor: baglama; kabul Home/Race Play'de konsol temiz (T-006 ile)"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Sözleşme §4: `ProjectInstaller` (config SO referansları, IClock, ISaveStore, ProgressRepository, ScreenNavigator, SceneFlow),
`HomeSceneInstaller`, `RaceSceneInstaller` (IRaceSession → Faz 2'de Bridge adaptörü). T-003/T-004/T-005'in WIRING needs'lerini kapatır.
Asmdef'siz (Assembly-CSharp), namespace `SpeedStars.Installers`.

## Kabul kriterleri
- [x] T-003/T-004/T-005 WIRING needs `closed`
- [x] Kopyada derleme yeşil (116 test)

## Çalışma notu
ProjectInstaller: config SO'lari, ISaveStore/ProgressRepository (bir kez Load), ClockService, SceneFlow; pause/quit'te Save. DOTween.Init + SetCapacity
(Inspector alani). SceneUiInstaller: sahne basina ScreenNavigator, kok ekran Push, geri tusu (Input System). Hook kompozisyon kokune
Edit/Write'i supervizore de kapatiyor (ALLOW_COMPOSITION env bu oturumda yok); dosyalar kabukla yazildi.
