---
id: T-146
title: Duz kosuda kol uylugun kare dalgasini kopyalamaz, adim suresine gore uzun yumusak salinir
status: review
owner: gameplay-agent
depends_on: [T-145]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "docs/tasks/T-146-kol-hiza-gore-yumusak.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips + scripts/runner-olcum/arm_table.py supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 05.11: "cok robot kol gibi hareket ediyor. hizina gore ayarlaman lazim, yavas kosarken direkt tak tak diye gecis olmasin." Kol yavasta adim boyunca uzun ve yumusak salinir, hizlandikca sertlesir; bekleyip firlamaz.

## Sebep (kayit t145-c, slowRun, sol kol ve sag uyluk)
Arka bacak bekler: `R_thighWorld` 3.80-4.03 sn arasi -45'te sabit. `StepArm` kolu dogrudan bu uyluktan surer: kol -34'te 0.23 sn durur, sonra 4.03-4.23 arasi 0.2 sn'de +21'e firlar, dirsek ayni surede 22'den 100'e. `limbSpring` 14 Hz bunu yumusatamaz. Uyluk `armThighRange` (-45, 55) disina ciktikca `t` kirpilir, T-145 tepeyi 80'e cikardigi icin kol ustte de bekliyor.

## Onceki olcu (`python -I scripts/runner-olcum/arm_table.py D:\Pundun\form-probe\t145-c`)
| senaryo | aralik | en buyuk kare hareketi | durgun pay |
|---|---|---|---|
| slowRun | -36..27 | 8.0 | 0.30 |
| rhythm032 | -45..41 | 15.0 | 0.32 |
| fastRun | -31..32 | 15.5 | 0.06 |
| roughRun | -47..42 | 21.4 | 0.18 |

## Kabul kriterleri
- [ ] slowRun: kare hareketi 4.5 ve alti, durgun pay 0.12 ve alti, aralik en az 54 derece.
- [ ] rhythm032 kare hareketi 9 ve alti, aralik en az 75 derece. roughRun kare hareketi 13 ve alti, aralik en az 75 derece. fastRun aralik en az 50 derece.
- [ ] Kol hicbir karede atlamaz; tus uzuv resetlemez. Durustan ilk basista ve duruna kadar sicrama yok.
- [ ] Tek ayak, cift ziplama, tutma, dalista kol ayni: hopSwap ve pairOrder satirlari eskisiyle ayni, runToHop ve holdOne'da kol kare hareketi artmaz.
- [ ] Bacaklar, sim, govde degismez.

## Kapsam disi
- Bacak (T-145 kapandi), kosudan tek ayaga gecis (T-147). Kolun hop ve pair saatleri.

## Calisma notu
- Yol: `StepArm`'te uyluktan gelen `t` kol basina kritik sonumlu yaydan gecer (`arm.drive`), Hz = `armPumpRate` (yeni, 1.0) / adim suresi (`_armPeriod`); `pump` ve dizin `open`'u `Lerp(ham t, yay, _plainRun)`'dan okur. `hw`, `pw` dallari ve limbSpring/armMaxSpeed degismedi.
- Gait agirligi < 0.05 iken ya da ilk karede yay ham t'ye oturur; `ResetGait` `driveSet`'i sifirlar.
- Hiz egrisi eklenmedi; olculmedi. Belirsiz: hizli tarafta aralik (fastRun >= 50) yay yuzunden daralabilir.
- Tur 2: `armPumpWidth` 1.12 filtreli kol surusunu ortasi etrafinda acar (-0.2..1.2 kirpili); `_plainRun` 0'da eski.
