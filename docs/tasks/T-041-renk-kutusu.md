---
id: T-041
title: Kostum renk kutulari secilen rengi gostermiyordu (lacivert cerceve plakasi rengin ustundeydi)
status: merged
owner: systems-agent
depends_on: [T-035]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-041-renk-kutusu
commits: [26740a63, bdbd066f, bd271198]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/CostumeEditorModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanıcı (30.10): "second color ve base color'u değiştirdiğim kısımdaki renk geçiş butonu seçtiğim renk olmalı". Kanal kutusu her zaman o
kanalın karakterdeki rengini göstermeli (düzenlenen > kayıtlı > materyal varsayılanı), sürüklerken canlı güncellenmeli.

## Calisma notu
- Sebep: `color_swatch_frame.png` halka değil opak lacivert plaka; builder onu rengin ÜSTÜNE koyuyordu (T-032'deki SV çerçevesiyle aynı tuzak). Sıra düzeltildi.
