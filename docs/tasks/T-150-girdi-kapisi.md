---
id: T-150
title: Bir girdinin hareketi bitmeden ayni girdi poz tarafindan kabul edilmez (kosu, tek ayak, cift ayak, gecisler)
status: merged
owner: gameplay-agent
depends_on: [T-148]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-150-girdi-kapisi.md"
touches_scene: false
branch: agent/ik-kosucu
commits:
  - 6c710cc1
  - 785bc809
  - 7545fdf8
  - f92e9855
  - 5da8ea64
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
Kullanici 06.11: "bir input verdiysem o inputun etkisi bitene kadar o inputu vermemi engelle, kabul etme. tek bacak ustunde sekerken spamlarsam inputu kabul etme, animasyon bitsin oyle kabul et. tum her sey icin gecerli olsun: kosma, ziplama, tek ayak, cift ayak, her seye gecisler smooth olur bu sayede."

## Karar: yalniz poz
`OnTapped`'teki kapi genisler; `RunnerBody` -> `Sim.Press` degismez, sim her basisi sayar. Olcum (`simprobe`, ayni kapiyi fizige koyunca): saniyede 20 basan iki tus oyuncusu 100 m 40+ sn yerine 10.39 sn, vmax 12.1 (en iyi oyuncu 9.78, ritim 0.28 = 11.13). Fizige koymak ezme cezasini kaldirir, spam en iyi oyun olur. Kullanici isterse ayri denge isi.

## Bugunku kapi
`OnTapped`: sabit `PoseMinGap` 0.13 sn, `_poseSkippedOther` adim sirasini korur, `pairSecond` istisna. Sabit sure yerine hareketin bitip bitmedigi sorulacak.

## "Etki bitti" tanimi
- **Ayni tus (tek ayak):** son alinan bacak yere dondu ya da donmek uzere: `grounded`, ya da ayak `TouchHeight` payinda, ya da salinimin sonuna yakin. Havadayken ve itis bitmeden red.
- **Diger tus (kosu):** o bacagin salinimi baldir atisini gorecek kadar ilerledi (`pressGateShare`, Tooltip'li, ~0.6, salinim payi), ya da yerde, ya da `Rest`. Yoksa red, `_poseSkippedOther`.
- **Iki tus (cift ayak):** son cift havadayken red; bir ayak inince acik. `pairSecond` istisnasi kalir.
- **Durus:** `_idleStock` iken kapi acik.
- En az `PoseMinGap` her zaman.
- Reddedilen basis kuyruga girmez. `l.held` ve `l.pressedAt` aynen (tutma calisir). `Enter`, `NoteCycle`, kol saati, hop hedefi sayilmaz.

## Yeni senaryolar
`spamHop`: tek tus 3 sn, 0.08 aralik. `spamRun`: sirali 3 sn, 0.08. `spamPair`: iki tus 3 sn, 0.10. `runSpamHop`: kos 2.5 sn sonra tek tus spam 2.5 sn. `spamToRun`: tek tus spam 2.5 sn sonra sirali 0.27. Seritler gecis anlarinda. Kabul edilen basis sayisi icin CSV'ye `d_posePresses` sutunu.

## Kabul kriterleri
- [ ] Ritim bozulmaz: rhythm032, slowRun, roughRun, `runToHop` duzenli kismi, `pairOrder`: basislarin hepsi alinir. fastRun (0.20) en az %90.
- [ ] `spamHop`: kabul edilen basis arasi ayak yere doner; her alinan basisin ardindan ayak en az 15 cm kalkar ve iner. Spamsiz 0.27 sn sekisle ayni gorunur.
- [ ] `spamRun`: bacaklar sirayla adim atar, tek bacakta sekmeye donmez, ayak ucu yerde kaymaz.
- [ ] Bir karede: uyluk 30 derece alti, kalca egimi 4 derece alti, spam senaryolarinda kalca yuksekligi ikinci farki t147-a'nin spam oncesi degerinden buyuk degil.
- [ ] Gecisler: runToHop, hopToRun, runToPair, pairToRun, pairToHop kare farklari artmaz, seritte kesik salinim yok.
- [ ] Tutma ve dalis: holdOne, holdBoth, holdStand, holdBothStand ayni. T-148 `start`, `restart`, `walkStart` ilk adimi bozulmaz.
- [ ] Sim degismez: `simprobe` ve scenario hiz sutunlari ayni.

## Kapsam disi
- Sim, hiz, ezme, Good/Late hesabi, fren (T-149).

## Calisma notu
- Commit 1 6c710cc1: spamHop, spamRun, spamPair, runSpamHop, spamToRun + `d_posePresses` sutunu. Commit 2: `PressStillBusy` kapisi (yalniz poz; sim ve `RunnerBody` ayni).
- Sayilar: `pressGateShare` 0.6 (diger tus), `pressGateSameShare` 0.9 (ayni tus), `pressGateMax` 0.3 sn (son alinan basistan sonra kapi en cok bu kadar kapali). `PoseMinGap` ve `pairSecond` ayni; `_idleStock` (fromStand) iken kapi acik.
- Basilan bacak `Recover/Mid`: pay esigin altindaysa red; `Push` + havada (catch) red; ayni tus `Push` yerdeyse ancak arada salinim yaptiysa (`swungSincePress`) serbest. Reddedilen yol eski: `_poseSkippedOther`, `l.pressedAt`; `_posePresses` artmaz.
- Olculmedi. Belirsiz: fastRun'da (0.20) 0.3 sn tavan ve %90 kabul; hop'ta ayni tus salinim sonunda (0.9) yeterince erken acar mi.
- Tur 2: ritim tustan ogrenilir (`Leg.lastKey`, `pressFollowSame` 0.22, `pressFollowRun` 0.30): reddedilen basis da `cycle`'i besler, `NoteCycle` anahtar araligini alir; `pairSecond` penceresi 1.5 * PairWindow.
- Tur 3: `_poseSkippedOther` kalkti; sirali tus `alternating` (bir onceki tus, alinsin alinmasin, digeri ve durak yok) ile okunur: eslem ve `PressStillBusy.sameKey` buna bakar.
- Tur 4: `_altStreak` (sirali ve tus kendi araligi >= pressFollowRun, art arda 3, `SettledStreak`): oturmus siralı ritimde kapi reddetmez; `PoseMinGap` kalir, spam sayaci sifirlar.
- Olcu (t150-d; alinan / tus): spamHop 10/38 (0.32 sn, her birinde ayak kalkar), spamRun 13/38, spamPair 16/60, runSpamHop 18/42; rhythm032, slowRun, roughRun, start, restart, stop, pairOrder, runToPair %100; fastRun %96; hopSwap 16/18, runToHop 18/20, hopToRun 16/18: redler yalniz gecis aninda. Sim hizi 0.0 fark.
- Dort tur: kapi yarim ritme kilitledi (`cycle` kabul edilenden ogreniyordu) -> `lastKey`; yalanci cift -> `pairSecond` 0.06; tus-bacak eslemesi kaydi -> `alternating`; ritim tuslardan yarim adim kayinca tus hep yeni kalkan ayaga denk geldi -> `_altStreak >= 3` kapi karismaz.
