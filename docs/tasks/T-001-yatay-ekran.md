---
id: T-001
title: Ekran yonu yatay + referans cozunurluk
status: todo
owner: scene-integrator
depends_on: []
owns_paths:
  - "ProjectSettings/ProjectSettings.asset"
touches_scene: false
branch: agent/faz1-iskele
commits: []
tests:
  - "gerekmiyor: proje ayari; kabul gozle (cihazda yatay)"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: open
    not: "Ana klasorde ProjectSettings.asset'te kullanicinin/Unity'nin commit'siz degisikligi var; once git diff ile bak, kullaniciya sor"
started:
finished:
blocked_reason:
---

## Hedef
Oyun yalnız yatay (kullanıcı kararı 28.10): Player Settings → Default Orientation = Auto Rotation, yalnız Landscape Left/Right
izinli (dikey kapalı). UI referans çözünürlüğü 1920×1080 (CanvasScaler kuralı sözleşme §5'te). Unity MCP ile Editor'de
`PlayerSettings` üzerinden yapılır (dosya elle yazılmaz).

## Kabul kriterleri
- [ ] Cihazda/Device Simulator'da yatay açılır, dikeye dönmez
- [ ] Kullanıcının mevcut commit'siz ProjectSettings değişikliği korunur

## Çalışma notu
