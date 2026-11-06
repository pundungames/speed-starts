---
id: T-159
title: Kamera daha yandan baksin, hizlandikca daha cok uzaklassin
status: todo
owner: gameplay-agent
depends_on: [T-158]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Runner/SideFollowCamera.cs"
  - "docs/tasks/T-159-kamera-yandan-uzak.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "gerekmiyor: gorsel; kullanici gozle bakar"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici 06.11: "kamera daha yandan baksin ve hizlandikca daha cok uzaklassin."

## Istenen (`SideFollowCamera.cs`)
- `sideYaw` 12 -> 4 (0 tam yandan; sahnede kayitli degil, kod varsayilani calisir).
- Hizla zoom. `Race.unity` `zoomFullSpeed: 5.8` ve `zoomDistance: 1.7` kaydediyor, kod varsayilani orada islemez; sahneye dokunmadan yeni alan adlari: `speedZoomFull` 11 (bu hizda tam geride) ve `speedZoomDistance` 2.4. Eski iki alan kalkar, tum okuyuculari yeniler. 5.8 m/s'de mesafe eskisiyle ayni kalir (~1.74 kat), oradan 11 m/s'ye kadar artmaya devam eder.
- `StandingHeight` yeni `speedZoomDistance`'i okur: tam zoom'da aci dogru, yukseklik sabit kalir. Basis zoom'u (`zoomPerPress`) ayni.
- Tooltip'ler guncel.

## Kurallar
Yalniz `owns_paths`. Sahne, prefab yok. `SideFollowCamera`'nin alanlarini baska dosya okuyorsa (grep `Assets`, `RaceStartCamera`, `RaceBots`, `RaceFinishSequence`, `ThemeRunPlayground`, testler) BLOCKED yazma, bana bildir. Unity calistirma. Commit `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasi scratch'te, adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Calisma notu
