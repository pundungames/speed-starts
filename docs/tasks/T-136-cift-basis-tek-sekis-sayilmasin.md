---
id: T-136
title: Cift basis tek sekis sayilmasin; kollar ve bacaklar ayni ziplamayi yapsin
status: review
owner: gameplay-agent
depends_on: [T-135]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [eaf9533e]
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
Kullanici 05.11: "cift ziplama yapiyorum ama bazen tek ziplama olarak aliyor. kollar cift ziplama yapiyor ama bacaklar tek ziplama olarak calisiyor."

## Kok sebep (kod okundu, oyunda gorulmedi)
- `UpdateHop` kareye bir kez bakar ve yalniz son basisi gorur. Iki cift basis ayni sirayla gelirse (R,L sonra R,L) son bacak ayni kalir: `same` dogru, `_hopTarget` 1.
- Ters sira (R,L sonra L,R): ilk tus `same`, ikinci tus T-135'in `swap` kurali ile hop'u acik tutar. T-135 bunu agirlastirdi.
- Kollar `_pair` okur, `hw = _hop * (1 - _pair)`. `leg.idle` ise `_hop` okur, `_pair`'a bakmaz; `lean -= hopBackLean * _hop` ve T-127'nin bloğu da oyle. Sonuc: kollar cift, bacaklar tek.
- Tuslar 0.052-0.08 sn arayla basilirsa `pairSecond` degil: ikinci tusun durusu alinmaz, tek bacak iner.

## Isler (tek commit)
1. Birlikte basilan iki tus ve onceki ciftin tekrari hop sayilmaz.
2. `leg.idle` ve hop hedefi `_pair`'a uyar.
3. `pairSecond` penceresi `PairWeight` ile ayni olur (0.08 sn).

## Kapsam disi
- Kosu, hop ve cift sayilari, kollar, govde, tutma, dalis. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- UpdateHop: `together` (iki bacagin lastPress farki < 0.08 sn) ve `_pair < 0.5` hop'u (same/swap) kapatir; hop hedefi `1 - PairWeight` ile soner.
- `leg.idle` hedefi `_hop * (1 - _pair)`: cift basista diger bacak denge bacagi olmaz.
- OnTapped `pairSecond` penceresi PoseMinGap*0.4 yerine 2*PairWindow (0.08 sn).
- Bilinen kalinti: uzun duraktan sonra ayni bacak + cift ilk tusunda `_hopTarget` 1 olabilir, T-127 blogu diger bacagi bir iki kare Rest'e sokar; ikinci tus BeginStance ile alir.
- Calistirilmadi, olculmedi.
