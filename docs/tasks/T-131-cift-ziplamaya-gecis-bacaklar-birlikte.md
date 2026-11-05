---
id: T-131
title: Kosudan cift ziplamaya geciste iki bacak birlikte kalksin ve birlikte insin
status: review
owner: gameplay-agent
depends_on: [T-130]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: []
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
Kullanici 05.11: "normal kosarken tekli bacaga veya cift bacak ziplamaya gecerken arada saçma sapan, input vermiyorum ama karakter farkli hareketler yapiyor."

## Kok sebep (kod okundu)
Kosarken bacaklar ters fazda. Iki tusa birlikte basilinca yerdeki bacak kendi kuralina gore kalkar, havadaki yakalanip iner; bacaklar ayri kalkip ayri iner, ilk ziplama kosu adimi gibi gorunur. Bacak suzmeleri (`cycle`) de farkli oldugu icin inisler de ayri.

## Isler (tek commit)
1. Cift ziplamada yerdeki ayak, oburu inip kendi durusunu bitirene kadar bekler; ikisi ayni karede kalkar.
2. Cift basista iki bacagin `cycle` ve `pressedAt` degerleri esitlenir, birlikte inerler.

## Kapsam disi
- Duz kosu, tek ayak, tutma, dalis, kollar, govde, sayilar. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Push/grounded dalinda `_pair > 0.5` iken ortagi hala Push'ta (yerde ya da yakalanirken) ve `toeOff` almamissa bacak kalkmaz; `toeOff` Enter'a kadar kalici oldugu icin ikisi ayni TickLegs gecisinde kalkar.
- OnTapped'ta `pairSecond` ise iki bacagin `cycle` degeri min'e, `first.pressedAt` ise `time`'a esitlenir; ayni `len` cikar (`lead` iki bacak icin ayni).
- Kilitlenme yok: `_plainRun` < 0.5 iken `waits` kapali, `stanceMax`/`endBehind` kurali isler; yakalama `MaxCatch`/`CatchGrace` ile sinirli.
- Calistirilmadi, olculmedi.
