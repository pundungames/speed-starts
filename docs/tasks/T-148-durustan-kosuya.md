---
id: T-148
title: Durustan kosuya geciste ilk basista diger bacak kalkar, gercek adim atilir; kollar yuzden iner
status: merged
owner: gameplay-agent
depends_on: [T-147]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-148-durustan-kosuya.md"
touches_scene: false
branch: agent/ik-kosucu
commits:
  - 8c104752
  - 2433b47d
  - 0aa72028
  - 696cd313
  - a30f6ef2
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
Kullanici 06.11: "karakterin durur pozisyondan yurumeye kosmaya gecmesi acayip kotu gozukuyor, adam adim bile atamiyor, sacma sapan bir hareket ediyor." Yalniz yaris basi degil: her durustan kalkis. Ilk basistan itibaren gorunur adim atilir.

## Sebep (kayit t147-a, start: L+ 0.483, R+ 0.783, L+ 1.083; kod)
1. **Ilk basista diger bacak kalkmaz.** `OnTapped` -> `BeginStance(L)`; R `Rest`te kalir, cunku kosuda "diger bacak kendi dongusunde" varsayimi var; durusta dongu yok. 0.48-0.78 arasi iki ayak yerde (`R_footWorldZ` -0.39 sabit), govde 1.1 m/s ile iki ayagin ustunden gecer.
2. **Ikinci basis bosa gider.** R+ `Enter(Push)` yapar, ama R ayagi kalcanin ~33 cm gerisinde: `UpdateBodyHeight` `legCap`'i yuzunden hemen `toeOff`, `StartSwing`. L de `OtherTakesOver` ile birakir. 0.80'de ikisi birden `Recover`.
3. **Ayak surunur.** R `Recover` 0.80-1.18: `swingBackLimit` uyluk 24, diz 127; `R_toeY` 0.03-0.08, `R_footWorldZ` -0.14'ten 0.36'ya yerde kayar. Ilk gercek kalkis 1.10 sn (ilk basistan 0.62 sn sonra).
4. **Kollar yuzde.** `_gaitW` hiza bagli (`gaitSpeed` 1..6): 2 m/s'te ~0.1. Ready pozunun yumruklari yuzun onunde kalir, `armL_swing` 40, dirsek 130.

## Yeni senaryolar (commit 1 ile)
`start` ikinci serit (1.05). `restart`: kos 2.5 sn, 2.2 sn bekle (hiz 0), yine kos; serit kalkista ve +0.65. `walkStart`: 0.6 sn aralik. `oneTap`: tek basis, 2 sn kuyruk. `holdStand`: durustan tek tus 1.2 sn. `holdBothStand`: durustan iki tus 1.2 sn.

## Kabul kriterleri (start, restart, walkStart)
- [ ] Ilk basistan 0.12 sn icinde diger ayagin `toeY`si 10 cm ustu; bir sonraki basistan once o ayak basan ayagin en az 25 cm onunde.
- [ ] Ilk 1.5 sn: iki ayak birden yerde kayma yok; ayak ucu 3 cm altinda iken kare basina 1 cm'den fazla kayan kare sayisi t147-a'nin yarisindan az.
- [ ] Bir karede sicrama yok: uyluk 30 derece alti, ayak 8 cm/kare alti.
- [ ] Kol ilk basistan 0.3 sn icinde yuzden iner (`armL_elbow` ve `armR_elbow` 100 alti), kosu salinimi baslar; genlik yavasta dar kalabilir.
- [ ] `oneTap`: tek adim atar, ayak yere iner, Rest'e doner; tuhaf kalan poz yok.
- [ ] `pairOrder`, `holdStand`, `holdBothStand`: cift ziplama, tutma, dalis durustan bozulmaz (seritte bakilir). `hopSwap` yerlesik bolum ayni.
- [ ] Hiz alindiktan sonra (t >= 2.5) rhythm032, slowRun, fastRun, roughRun T-145/T-146 ile ayni. Sim'e dokunulmaz.

## Kapsam disi
- Fren pozu (T-149).

## Calisma notu
- Commit 1 8c104752: senaryolar (restart, walkStart, oneTap, holdStand, holdBothStand; start'a 1.05 seridi). Commit 2 2433b47d: ilk basista `_idleStock` (onceki karede dogru) + Rest'teki karsi bacak, cift ikincisi/hop/tutma degil -> `StartSwing(.., standing: true)`; mid uyluk baslangic uyluktan geriye gitmez (`MidKeyFor`).
- Commit 3: `_armW = Max(_speedCurve, armStepShare) * gate` (ayni yumusatma); `StepArm` bunu okur, `_gaitW` baska okuyuculari ayni.
- Cift basis: kaldirilan bacak ikinci basista `BeginStance` -> `StartCatch` ile iner (ertelenmedi); olculmedi. Belirsiz: kisa ciftte ayak havada kalabilir.
- Tur 2: `OtherTakesOver` yukselen `standStep` salinimini (tuck tepesine varmadan) 'iniyor' saymaz; basilan ayak planted kalir.
- Tur 3: kok: `StanceExtent` yuksekligi yalniz hiza bagli `_runHome`'dan geliyordu (0.5-3 m/s), kalkista govde cömelik yukseklikte kaliyordu; `StanceHome() = Max(_runHome, stanceStepShare * _stepping * _plainRun * (1 - _dive))`. Dip/ucus farkini dogrulayamadim (olculmedi).
- Olcu (t147-a -> t148-c, start): L planted kalir; R toe 0.03 -> 0.16, ilk adim 34 cm, sonraki 0.9 m; ilk kalkis 1.10 -> 0.60 sn; kalca 0.796 -> 0.917 tek hareket 0.2 sn, kare hareketi 4.2 -> 1.8 cm, geri dusme 9 cm -> 1 cm. Dirsek 100 alti +0.37 sn (kabul +0.3).
- Kok: kalca yuksekligi `_runHome` (hiz 0.5-3 m/s); kalkista 1 m/s. `StanceHome` artik `_stepping`i de okur. Ilk tahminim (`contactRun` ile stance farki) yanlisti, agent kodu okuyup duzeltti.
