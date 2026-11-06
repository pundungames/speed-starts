---
id: T-127
title: Kosudan tek ayaga gecis her seferinde ayni olsun; diger ayak yerde kalip govdeyi tutmasin
status: review
owner: gameplay-agent
depends_on: [T-126]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [2e70102c]
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
Kullanici 05.11: "normal kosarken tek tarafta sekerken gecis cok berbat; bazen normal geciyor, bazen sadece o tek ayak animasyon yapiyor, baska hicbir sey olmuyor. Bu gecisleri tek duze yapalim."

## Kok sebep (kod okundu)
Gecis, hop basladigi anda diger bacagin fazina bagli:
- Havada (`Recover`, `Mid`): denge bacagi olur, govde ziplar: iyi.
- Yerde (`Reach`, `Front`, `Rest` civili, `Push` grounded): `UpdateCarry` onu tasiyan sayar, `PinFeet` civiler; `carry` varken ucus yayi calismaz. Govde kalkmaz, yalniz basilan ayak hareket eder.
- `leg.idle` hedefi `i != sim.LastLeg ? _hop : 0`: sim'in son bacagi; `PoseMinGap` ile poz baska bacak almis olabilir.

## Isler (tek commit)
1. `leg.idle` hedefi `_poseLastLeg` okusun.
2. Hop acildigi anda (poz basisinda `_hopTarget` 0 -> 1) diger bacak, hangi fazda olursa olsun, kalkar: planted ise `grounded`, `needLock`, `toeOff`, pin ve `pinOffset` temizlenir; `Hold` ve jump, dalis disinda `Rest`'e (denge pozu, `idle` ile) girer. `Track` o anki aci ve hizdan devam eder, snap yok, yeni ara poz yok.
3. `UpdateCarry`: `leg.idle > IdleCarry` olan bacak hic bir fazda tasimaz (`Hold`, jump, dalis haric). `PinFeet`: `idle > IdleCarry` bacak civilenmez.
4. Hop kapanirken (diger tus, `_hopTarget` 1 -> 0) `idle` zaten soner; yukaridan inen denge bacagi basisla `BeginStance`/`StartCatch` ile alir: oldugu gibi.
5. Cift ayak, normal kosu, tussuz stock poz, tek tus zaten hop iken davranis degismez.

## Kapsam disi
- Tek ayak sayilari, kol, govde. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
Hop acilinca diger bacak Enter(Rest, stockReturnTime) ile kalkar; carry/pin balance bacakta kapali. pinned/pinOffset elle silinmedi: PinFeet erimesi snap onler.
