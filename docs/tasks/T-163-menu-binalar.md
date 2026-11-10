---
id: T-163
title: Ana menude yola dusen binalar: sahneye kayitli bina gruplari silinir
status: merged
owner: supervisor
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scenes/Menu/Home.unity"
  - "Assets/Speed-Staes/Scenes/Anamenu Themaları.unity"
  - "docs/tasks/T-163-menu-binalar.md"
touches_scene: true
branch: agent/T-163-menu-binalar
commits: [41b2ccf7, 941a8bf7]
tests:
  - "gerekmiyor: gorsel; sahne temizligi, Home smoke testleri kopyada kosar"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-09
finished: 2026-11-10
blocked_reason:
---

## Hedef
Serhat'in a5ea6bab commit'i `LoopWay.prefab`'taki uc `BackgroundCity` nesnesini ayni yerde ama yeni fileID ile kurdu.
Home (ve Serhat'in "Anamenu Themaları" sahnesi) `PastelBuildingRow`'un sahne acilinca urettigi "Generated Buildings"
gruplarini bu nesnelerin altina override olarak kaydetmisti; ebeveyn bulunamayinca gruplar yolun ve karakterin ustune
dustu, prefab'in kendi binalarini kaldiran kayitlar da bayatladigi icin binalar ikilendi. Sahnelerdeki eklenmis bina
gruplari, onlara giden `generatedRoot` override'lari ve hedefi kalmayan kaldirma kayitlari silinir; menu prefab'in
kendi binalarini gosterir (Race gibi).

## Kabul kriterleri
- Home'da `PastelBuildingSurface` kalmaz; LoopWay ornegi `m_AddedGameObjects: []`, bayat `m_Removed*` kaydi yok.
- Sahnede cozulmeyen yerel fileID yok; LoopWay'e giden her fileID prefab'ta var.
- Home smoke testleri yesil; kullanici menude binalari yolun disinda gorur.

## Çalışma notu
- Duzeltme betikle (yalniz silme): Home 7 grup / 799 belge, Anamenu 4 grup / 373 belge; diff yalniz silme + 2 satir `[]`.
- Unity Home'u 09.11 18:05'te bozuk haliyle yeniden kaydetmisti (yetim gruplari LoopWay kokune baglamis); o kayittaki 3 UI yerlesim degeri korundu.
- Home'da `automaticUpdate` 0 override'i: acik kalan 3 Building Row (2263638959723061151, 2296153393761645365, 6327596869865797829).
- Kopyada Home PlayMode smoke (HomeRaceButton|HomeSharedResourceBar) 18/18 yesil.
- Anamenu Themaları (Serhat'in sahnesi) yalniz temizlendi; orada `automaticUpdate` acik satirlar acilista yine uretir (`_backlog.md`).
