---
id: T-152
title: Kollar bacaklar gibi yumusak: tek ayak ve cift ayakta atis yayla, kosuda dirsek sakin
status: review
owner: gameplay-agent
depends_on: [T-151]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "docs/tasks/T-152-kollar-yumusak.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [ed9349e7]
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11: "kollari da bacaklar gibi smooth hareket ettirebilir miyiz. kollar cok hizli ve gozukmuyor, tekliyor gibi oluyor."

## Sebep (kayit t149-b; kare basina en buyuk hareket, en buyuk ikinci fark, derece)
| senaryo | kol | dirsek |
|---|---|---|
| hopSwap | 20.7 / 17.0 | 9.0 / 4.9 |
| pairOrder | 25.2 / 16.3 | 16.3 / 11.4 |
| rhythm032 | 9.5 / 1.8 | 14.1 / 3.4 |
| roughRun | 13.4 / 3.9 | 18.2 / 5.9 |

Tek ayakta kol (hopSwap 2.40-2.57): 52, 41, 24, 4, -14, -14.5 (bir karede durur), -6, 6, 24, 43, 54, 58, 60, sonra 0.1 sn tepede bekler. `_armKick` dogrusal saat (`ArmRise`, `armFall`), `StepArm` `Lerp(armHopDown, armHopUp, kick)`; `limbSpring` 14 Hz bunu yumusatamaz.
Hesap: 0.27 sn'de 128 derecelik salinim sinusle bile 25 derece/kare eder; genlik ritimle kuculmeli.

## Yon
1. **Tek ve cift ayak:** `_armKick` kol icin sabit frekansli yaydan gecsin (`armThrowSpring` ~2.6 Hz, sonum 1, yeni alan); `hw` ve `pw` dallari onu okur (`_rockKick` ve baskalari degil). Sabit frekans, kisa ritimde genligi kendiliginden kisar: 0.27'de ~55, 0.5'te ~100, 0.7'de ~125 derece.
2. **Kosu omuz:** `armPumpRate` 0.85, `armPumpWidth` 1.21 (genislik ayni kalsin).
3. **Kosu dirsek:** `open`'daki `SmoothStep` ortada dik; yeni alan `armElbowEase` (0..1, ~0.7) ile dogrusala karissin.

## Kabul kriterleri
- [ ] hopSwap kol kare hareketi 10 alti, ikinci fark 3 alti; aralik 45 derece ustu; `slowHop` 90 ustu.
- [ ] pairOrder kol 14 alti, ikinci fark 4 alti, dirsek 10 alti.
- [ ] rhythm032 kol 8.5 alti, dirsek 10.5 alti; roughRun kol 11 alti, dirsek 14 alti; aralik -8 dereceden fazla kaybolmaz.
- [ ] Bacak, kalca, sim 0.0 fark (T-151 uzerine). Fren kolu (`armBrake`) ve kalkis kolu (`armStepShare`) bozulmaz; start, restart, stop seritlerinde bakilir.

## Calisma notu
- `_armThrow` = `_armKick`'in `armThrowSpring` (2.6 Hz, 1) yayindan gecmisi; yalniz `StepArm` hop/pair dallari okur, `_armKick` (lean, `_rockKick`) ayni kaldi.
- `armPumpRate` 0.85, `armPumpWidth` 1.21 (prefab/asset'te yok), `armElbowEase` 0.7: `open` smoothstep ile `t/armElbowCloseAt` arasi karisim. Olculmedi.
