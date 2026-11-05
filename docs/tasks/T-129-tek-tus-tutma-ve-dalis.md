---
id: T-129
title: Tek tus tutma ve dalis Malik'in dalindaki gibi; kosu, tek ayak ve cift ziplama degismez
status: review
owner: gameplay-agent
depends_on: [T-128]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [d976a851]
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
Kullanici 05.11: Malik'in tek tus tutma ve dalisini alalim, "bizim branchde de aynisini yap ama bozmadan". Malik'in dali birlestirilmez; davranis IKTest'in kendi yapisina yazilir.

## Isler (tek commit)
1. Gercek tutmada govde hizla devrilir (`holdLean` 32, `holdTipSpring`), iki tusta dalisa gider (`bothLean` 55); yataya yakin kambur (`spineLevelCurve`, `headDip`).
2. Tutmada bel zinciri Malik'in paylariyla (`holdChestShare` 0.78, `holdWaistShare` 0.6, kayitli `hipShare`); koşuda eski zincir.
3. Tek tusta tutulan bacak kendi geri acisina gider, diz duzlesir (`holdThigh` -65, `holdStraightKnee` 165, `holdReachTime` 0.18).
4. Ondeki destek bacagi govdeyle acilir (`holdSupportStart` 55/110 -> kayitli `holdSupport`).
5. Birakis: govde dikse salinim, egikse dogrudan govdenin altina; dalista havadayken bacaklar cizgide kalir; takla ve bir karede sicrama yok.
6. `airTip`, `airTipBack`, `settleSpring`, `settleHeadUp`, `UpdateTip`, `_tip`, `_settle` kalkar: dalis basi yere degince durur.
7. Gercek tutma tek tuslu sekmeyi bitirir.

## Kapsam disi
- Kosu, tek ayak sekme, cift ziplama, kollar. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
RunnerPoseDriver'a elle tasindi (gercek tutma agirligi HeldWeight, govde zinciri, tutulan bacak, on destek, birakis, sekme sonu); airTip/settle/_tip/_settle kalkti.
Sapmalar: _bothHeld _heldNow ile carpildi (hizli iki tus basisi govdeyi devirmesin); on destek baslangici HeldWeight(otherHold) ile olceklendi (tap'te hedef eskisi gibi); gercek tutma esigi holdLean'in %25'i.
Bacak izleyicisi: FollowHips diz'i UpdateLeg'den sonra ezer, kneeRate ezilmis diz'den hesaplanir; ReleaseHold gercek tutmada hizlari sifirlar, tap yolunda etkisi tutma agirligiyla olcekli (kucuk).
Calistirilmadi, derlenmedi, olculmedi. PinFeet _dive ile soner: tutmada on ayak civilenmez (dokunulmadi).
Duzeltme: _ballistic salt dalis degil (cift ziplama/sekme uzun basis); cizgide kalma yalniz holdPeak >= HoldRealPeak olan bacakta, birakis holdPeak ile ayrilir.
