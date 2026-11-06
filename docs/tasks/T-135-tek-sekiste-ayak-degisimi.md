---
id: T-135
title: Tek sekiste oburu tusa basmak dogrudan oburu ayakla tek sekis olsun; aradaki cift ziplama gorunumu kalksin
status: review
owner: gameplay-agent
depends_on: [T-134]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [657b7094]
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
Kullanici 05.11: "sag/sol tek sekisten digerine gecerken input veriyorum, ikili ziplama algiliyor bir tane, sonra tek sekise geciyor. bunu da duzeltelim."

## Kok sebep (kod okundu, oyunda gorulmedi)
`UpdateHop`: `_hopTarget`, ayni bacak tekrar basilirsa 1, baska bacaksa 0. Oburu tusa ilk basista hop agirligi duser; denge bacagi iner, eski sekis bacagi da yerde; iki ayak birlikte iner ve kalkar, govde `hopRise` ile hala yukselir. `PairWeight` burada 0: gercek cift basis degil, cift gibi gorunen tek ayak dususu. Ancak ikinci basista hop yeniden acilir.

## Isler (tek commit)
Tek sekisteyken oburu tusa basmak hop'u kapatmaz, ayak degistirir; eski sekis bacagi hemen yerden kalkar (T-127 ile ayni yol). Iki degisim ust uste gelirse (R R L R) kosu olur. Duz kosu (L R L R) ve kosudan hop'a gecis degismez.

## Kapsam disi
- Hop sayilari, kollar, govde, kosu, tutma, dalis. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
`_hopRepeated` alani + `UpdateHop`'ta `swap`: hop icinde tus degisimi hedefi 1 tutar, eski sekis bacagi hemen Enter(Rest) ile kalkar; ikinci ust uste degisim (R R L R) kosuya duser.
L R L R ve kosudan hop'a gecis kod yolu olarak ayni (swap hic dogru olmaz). Cift basis: ikinci tus degisim, `_hopRepeated` false, hedef 0, PairWeight yukselir.
Yeni sekis bacagi (denge bacagi) BeginStance'ten gecer: havadaysa StartCatch, tek karede yere konmaz.
Oyunda calistirilmadi, derlenmedi.
