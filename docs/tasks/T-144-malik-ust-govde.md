---
id: T-144
title: Malik'in sirt ve kafa zinciri (UstGovdeAktarim.md bolum 2 ve 4) duz kosu ve tutmada
status: review
owner: gameplay-agent
depends_on: [T-143]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
touches_scene: false
branch: agent/ik-kosucu
commits:
  - 1176dd99
  - ade4e614
  - 9169217d
tests:
  - "olcum: RunnerTransitions_WriteFrameStrips; belgedeki olculerle karsilastirma supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11: "malik'in branchinde ust vucut hareketini alicaz ... her sey cok guzel oldu, bu ust vucut hareketini alabilirsen islem tamamdir." Kaynak: `claude/karakter-hareketleri-baec15` `73622b96`, `Assets/Speed-Staes/Scripts/Physical/UstGovdeAktarim.md`. Dal birlestirilmez.

## Kapsam karari
- Alinir: bolum 2 (sirt ve ust vucut), bolum 4 (kafa ve boyun).
- Alinmaz: bolum 3 kollar (kullanici omuz ve dirsegi kendisi ayarlatti), bolum 1 ziplama ve bolum 6 bacaklar (bizdeki kalir).

## Kritik fark (olculdu)
Malik'in zinciri `sim.HeadAngle` okur. Onun fiziginde basis itisi sabit (`headKick`), bizde (T-114) zamanlamaya bagli: ritimli kosuda bizim bas sabit -15, zincir kipirdamaz. Fizigin hiz belirleyen acisina dokunulmaz; `RunnerSim`'de yalniz gorsel icin ikinci bas acisi (`ShownHeadAngle`).

## Isler (uc commit)
1. `RunnerSim.ShownHeadAngle`.
2. Zincir: `UpdateLean` Malik'in formulu; tutma egimi ve kambur bizim yaylarla.
3. Duz kosuda bizim eklerimiz kapanir: basis sallanmasi, surus, govde egimi, burgu. Tek ayak ve cift ziplama aynen.

## Karsilastirma olcusu (belge)
Ritim 0.32: kalca 16, bel 18, gogus 16, boyun 28, bas ~7. Tek tus tutma: 26 / 31 / 39 / 51. Dalis: 88 / 89 / 102 / 116.

## Calisma notu
- Uc commit: ShownHeadAngle (sim, yalniz gorsel), zincir (UpdateLean Malik formulu, bizim tip/hump yaylari), duz kosuda ek kapama (UpdateGait, runRock/twist/roll 0). Test: RunnerFormProbe rhythm032 senaryosu.
- Derleme ve olcum calistirilmadi; supervizor olcer. hump hedefi artik hold kapisi (w) olmadan hips ustunden (Malik gibi): hizli bas-cek ~45 derece ustunde hump acabilir.
- Kalan farklar: tip/gate bizim (_heldNow, _bothHeld), neckTrim artik _lean.Value (tip yok), hump yayi var. Twist alanlari prefab/sahnede kayitli degil; hipFollow/waistBaseShare/waistTopShare kayitli ama okunmuyor.
- Hop ve pair: _headAngle pair agirligiyla simin kendi acisina doner, UpdateBounceLean ride bob/pelvisRockShare yalniz hop (hw).
