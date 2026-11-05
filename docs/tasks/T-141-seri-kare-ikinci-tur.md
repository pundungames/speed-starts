---
id: T-141
title: Seri kare ikinci tur - basma karesi, tek ayak yayi, tutma cikisi, kol donusu
status: review
owner: gameplay-agent
depends_on: [T-140]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [9b01cfa1, 45128afc, 5e2bad9d, 159982b8]
tests:
  - "olcum: RunnerTransitions_WriteFrameStrips ve transition_check.py, once t140-after; sonrasi supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
T-140 sonrasi olcumde (D:\Pundun\form-probe\t140-after) kalan sicramalar.

## Olculen
| Bulgu | Olcu | Kaynak |
|---|---|---|
| Basma karesinde diz sivrisi | 148 -> 160 -> 143 | Ayak yerden 3-5 cm yukarida civilenince `SolveStance` onu yere indiriyor |
| Basarken ayak one atliyor | +10 cm/kare | Civi karesinde acilar donuk, kok ilerlemis |
| Tek ayakta gövde | Kalkis 7.5 cm/kare, inis 2.5 cm/kare (sinir), ayak 2 kare yerde, 10 cm/kare sivri | `maxDropSpeed` 1.5 tek ayaga da uygulaniyor; ayak bacak boyunda degiyor |
| Tutma cikisi | Kalca 15 deg/kare | `holdTipSpring` 7 Hz iki yonde |
| Kol donusu | 28 deg/kare sivri | Fren serbest, `_armKick` basista 1'e atliyor |

## Isler (dort commit)
1. Basma karesi: civi son karenin yeri; `plantGap` eriyerek iner.
2. Tek ayak ve cift yayi: inis siniri kalkisla dengeli.
3. Tutma: giris 5 Hz, cikis 3 Hz.
4. Fren siniri ve `_armKick` yumusak baslangic.

## Kapsam disi
- Anahtar sayilari, kol sayilari, `RunnerSim.cs`, prefab, sahne.

## Calisma notu
- Dort commit, hepsi RunnerPoseDriver.cs: civi rootStep geri + plantGap (9b01cfa1), hopDropSpeed 3.5 + hopHipRiseMax 3.2 (45128afc), holdTipBackSpring 3 Hz / giris 5 Hz (5e2bad9d), BrakeShare 4 + ArmRise 0.05 (159982b8).
- Hicbir sey calistirilmadi; olcum supervizorde. Fren siniri sonrasi 24 deg/kare'den durus 9-13 deg ASMA beklenen kucuk tasmadir, sifir degil.
- plantGap: `leg.ankle.y + _bodyY.Value` son karenin cizilen yuksekligi (KeepAboveGround/JumpY/LimitHipRise ayrintisi dahil degil, birkac mm sapabilir).
- Civi duzeltmesi yalniz `!pinned && pinOffset == 0` iken; eriyen (pinOffset) durumda eskisi gibi.
