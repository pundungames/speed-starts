---
id: T-137
title: One atilan bacak yere bastiginda diz hafif kirik olsun
status: review
owner: gameplay-agent
depends_on: [T-136]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [ae7549d1]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11 (kosu dongusu referans gorseli, kare 1): "one atilan bacak tam yere bastigi anda diz full acik durumda oluyor. onun yerine full acik olmucak, diz birazcik kirik olucak."

## Kok sebep (kod okundu)
- `contact` 22/165: basma pozu neredeyse duz. `kneeOpen` tepeden geliyor (170), iyice duz gorunuyor.
- `StanceExtent` kalca yuksekligini `Stride(contact)`'tan aliyor. Yalniz anahtar degisirse govde ayni yukseklikte kalir, ayak basinca `SolveStance` dizi yine acar. Ikisi birlikte degismeli.

## Isler (tek commit, yalniz duz kosu)
Yeni `contactRun` (30, 145). Ayak yaklasik ayni yere iner (~17 cm onde; eski ~21 cm), gorunen diz ~146-155, govde ~2 cm alcalir. Salinim, yakalama ve kalca yuksekligi ayni anahtari okur.

## Kapsam disi
- Tek ayak, cift ziplama, tutma, dalis, kollar, govde. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Yeni `contactRun` (30,145) + `ContactPose()` (contact ile contactRun arasi, `_plainRun` agirligi); `contact` dokunulmadi.
- StartSwing/RefreshSwingKeys k[4], StartCatch c ve StanceExtent ayni pozu okur; `_plainRun` 0 iken ifadeler eskisiyle ayni.
- `Land`, `landY`, near testi ve LockKnee otomatik yeni pozu izler; Overstretch/StanceCap icin kalca daha alcak, ayak daha uzun kalir.
- Calistirilmadi, olculmedi; derleme supervizorde.
