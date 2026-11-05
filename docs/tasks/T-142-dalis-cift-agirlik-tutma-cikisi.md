---
id: T-142
title: Iki tus tutmada ikisi de tutar; cift basista agirlik dusmez; tutma cikisinda gogus kivrilmaz
status: review
owner: gameplay-agent
depends_on: [T-141]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [e254bf11, 4a2b6dc0, 9aadc3d2]
tests:
  - "olcum: RunnerTransitions_WriteFrameStrips ve transition_check.py, once t141-after; sonrasi supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
T-141 sonrasi olcumde (D:\Pundun\form-probe\t141-after) kalanlar. Biri T-140'in yan etkisi.

## Olculen
| Bulgu | Olcu | Kaynak |
|---|---|---|
| Dalis eksik | `holdBoth`'ta yalniz bir Hold; oburu `Rcv>Rch>Frt` | T-140 `holdAfter`: ilk bacagin `ToHold`'u, Recover'daki basili oburunu Reach'e atiyor |
| Cift basista agirlik dususu | `_pair` 1 -> 0.49, 2-4 kare | Ilk tus `lastPress`'i yazinca fark buyuyor, ikinci tus gelene kadar `PairWeight` 0 |
| Tutma cikisinda gogus | 72 -> 89 -> 46 | Agirlik duserken eski zincir (bel 0.94 x bas) geri geliyor, bas hala onde |

## Isler (uc commit)
1. Basili oburu `Reach`'e atilmaz, kendi tutmasina gider.
2. Pencere icinde `PairWeight` onceki degerini korur.
3. Tutma zinciri bas geri gelene kadar kalir.

## Kapsam disi
- Sayilar, kosu, tek ayak, kollar, `RunnerSim.cs`, prefab, sahne.

## Calisma notu
- Uc commit RunnerPoseDriver.cs'te: ToHold'a `partner.held`; PairWeight `_pairTargetLast` ile 2*PairWindow bekleme; `_holdChain`/`_holdChainOn` zinciri.
- Hicbir sey calistirilmadi; olcum supervizorde.
- `w` = Max(zincir, tip/holdLean): giriste zincir tip'ten geri kalmasin diye (brifte yalniz zincir).
- Zincir kapanisi (full 25): kararlida bel ucu ~20 derece farkli; yaylanir, adim degil. Gorunurse HoldChainOffLean dusurulur (10'da ~8).
- Mid/Land'de tusu basili, basisi PoseMinGap'ten atlanmis bacak hold'a girmeyebilir (BeginStance gormedi); simdilik dokunulmadi.
