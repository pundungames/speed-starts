---
id: T-130
title: Kosuda yerdeki ayak, one giden ayak yere basarken kalksin; havada dunyada geri gitmesin
status: review
owner: gameplay-agent
depends_on: [T-129]
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
Kullanici 05.11: "insan kosarken bir ayagini ileri atar, diger ayak oldugu yerde kalir. bizde bir ayagi ileri atarken diger de geri gidiyor, bu dogru degil. o ayak sabit kalacak, diger ayak ileri gidecek; ayagini yere basmaya yakinken veya bastiginda diger kalkmali."

## Kok sebep (kod okundu)
- Yerdeki ayak `AdvanceStance`'ta `behind >= endBehind` ya da `stanceMax` ile kalkiyor; diger ayaga bakmiyor.
- Kalkinca salinimin ilk anahtari `recover` uylugu havada daha geri goturuyor. T-128 (`strideMin` 0.8) bunu yavas kosuda buyuttu.
- T-128 `endBehind`'i genisletti, topuk gec kalkiyor, bacak boyu daha erken bitiyor.

## Isler (tek commit, yalniz duz kosu)
1. Yerdeki ayak, diger ayak inene ya da inmeye yakinlasana kadar kalkmaz; bacak boyu biterse yine kalkar.
2. Ayak kalkinca havada dunyada geriye gitmez: uyluk en fazla govdenin kendi hareketi kadar geri gider.
3. Topuk, bacak boyu bitmeden kalkar; ayak daha uzun yerde kalir.

## Kapsam disi
- Tek ayak, cift ziplama, tutma, dalis, kollar, govde. Kosudan cift ziplamaya gecis sonraki is. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Duz kosuda yerdeki ayak, diger ayak inince/inmeye yakinken TickLegs'te kalkar (OtherTakesOver, Leg.plantedAt); AdvanceStance'ta diger ayak yoldaysa bekler.
- Havada uyluk en fazla govdenin hareketi kadar geri gider (swingBackLimit, RecoverKeyFor); topuk HeelEndShare 0.55 ile erken kalkar.
- Bacak boyu bitince (Overstretch, StanceCap) kalkis eskisi gibi. Hop/pair/hold/dive _plainRun ve _idleStock kapilariyla eski yolda.
- Unity, test, olcum calistirilmadi; derleme ve oyun icinde gorsel kontrol bekliyor.
