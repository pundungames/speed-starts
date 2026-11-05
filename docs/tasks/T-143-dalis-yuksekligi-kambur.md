---
id: T-143
title: Dalista govde ucmasin; yatayliktan cikista bas ve gogus sicramasin
status: review
owner: gameplay-agent
depends_on: [T-142]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [429454c8, 4e9bceaa]
tests:
  - "olcum: RunnerTransitions_WriteFrameStrips ve transition_check.py, once t142-after; sonrasi supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
T-142 sonrasi olcumde (D:\Pundun\form-probe\t142-after) dalis.

## Olculen
| Bulgu | Olcu | Kaynak |
|---|---|---|
| Dalista ucus | Kalca kemigi 2.75 m, karakter kameradan cikiyor | Serbest dusus girisi `Max(0, hiz) + liftKick`: cift ziplama ile zaten yukselirken uzerine ekleniyor |
| Yatayliktan cikista | Bas 46 -> 87, gogus 73 -> 89, iki karede | `hump` 45-90 araliginda yaysiz; `holdHips` hizla inince kambur bir anda kapaniyor |

## Isler (iki commit)
1. Serbest dusus kalkisi en fazla `liftKick`.
2. `hump` yayla.

## Kapsam disi
- Sayilar, kosu, tek ayak, kollar, `RunnerSim.cs`, prefab, sahne.

## Calisma notu
- 429454c8: serbest dusus kalkis hizi Min(..., liftKick) ile sinirli; alan okunmaya devam ediyor.
- Serbest dususte yukari iten baska yol yok: LimitHeightSpeed ve KeepAboveGround orada yok/yalniz zeminden kaldirir.
- 4e9bceaa: `_hump` yayi (kalkista holdTipSpring, inerken holdTipBackSpring), w>0 disinda da adimlanir, OnRunReset'te sifirlanir.
- WriteHead kelepcesi sicrama uretmez: sureklidir, kalca 90'da sabitlenince yalniz kirilma olur.
- Calistirilmadi; olcum supervizorde.
