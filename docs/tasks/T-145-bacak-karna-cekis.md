---
id: T-145
title: Duz kosuda bacak once karna cekilir, sonra kaval kemigi one atilir
status: review
owner: gameplay-agent
depends_on: [T-144]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "docs/tasks/T-145-bacak-karna-cekis.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips + scripts/runner-olcum/swing_table.py supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 05.11: "bacagini one atarken basmadan once karin tarafina dogru iyice cekip sonrasinda kaval kemigini ileri dogru atmasi lazim." Duz kosunun salinimi: uyluk yukari cikarken diz kivrik kalir (karna cekili), uyluk tepeden inerken baldir one firlar, ayak `contactRun` pozuyla basar. T-133'teki "tepede diz tam acik" isteginin yerine gecer. T-134 uyluk tepesini "balerin" diye indirmisti; balerin duz bacaktandi, diz kivrikken uyluk yeniden yukselebilir.

## Onceki olcu (`D:\Pundun\form-probe\t144-after`, `python -I scripts/runner-olcum/swing_table.py <klasor>`, son yari ortalamasi)
| senaryo | hiz | uyluk tepe | tepede diz | salinimda en kapali diz | tepeden sonra baldir acisi |
|---|---|---|---|---|---|
| slowRun | 4.1 | 54.6 | 161.5 | 74.3 | 37.6 |
| rhythm032 | 8.1 | 67.7 | 154.0 | 57.4 | 49.8 |
| fastRun | 6.5 | 60.4 | 112.6 | 66.2 | 19.0 |
| roughRun | 9.6 | 57.8 | 134.9 | 63.7 | 23.9 |

## Kabul kriterleri
- [ ] Dort senaryoda (slowRun, rhythm032, fastRun, roughRun) uyluk tepesindeki diz belirgin kivrik: 100 derece ve alti. Uyluk tepesi 70 ve ustu.
- [ ] Tepeden sonra baldir one atilir: `shin_after` 20 ve ustu; diz acilmasi uyluk indikten sonra degil, inerken olur. Adim kisa oldugu icin (fastRun 17 kare) yay ve `Track` sinirlari altinda gercek acilar olculur, anahtar degil.
- [ ] Basma pozu degismez (`contactRun`, T-137: basarken diz kirik), basista diz sarsintisi ve ayak sicramasi T-141/T-142 seviyesinden kotulesmez.
- [ ] Bir karede sicrama yok: `snap_check.py` / `transition_check.py` uyluk ve diz kare farki onceki olcuden belirgin artmaz.
- [ ] Tek ayak, cift ziplama, tutma, dalis ayni: yeni anahtar yalniz `_plainRun` agirligiyla karisir, hop ve pair `swingKeys`, `kneeDrive`, `contact` ile eskisi gibi. Sim'e, hiza, zamanlamaya dokunulmaz.

## Kapsam disi
- Kollar (T-146). Kol uylugu izledigi icin genligi biraz degisebilir: not et, duzeltme.
- Kosudan tek ayaga gecis (T-147). Govde zinciri, zemin kirpma, kamera.

## Calisma notu
- Yol: ikinci bir 6 anahtarli egri (`tuckKeys`: baslangic, recover, mid, tepe 76/92, firlatma 55/155, contact), eski 5 anahtarli egri aynen duruyor; `EvalSwing` ikisini `_plainRun` ile karistiriyor (agirlik 0 iken eski egri birebir).
- Yeni alanlar: `tuckPeak`, `shinThrow`, `tuckThrowKeys` (0.66, 0.84); hepsi Tooltip'li, prefab/sahnede yok (git grep).
- Hizli kosuda dizin acilmasi: 17 kare ~0.28 sn; 0.66..0.84 = ~50 ms, 92 -> 155 = 63 derece, ortalama ~1250 deg/s (legMaxSpeed 1800 altinda); Hermite tepesi ~1.5x ile sinira yakin.
- Catch/Land/knots[1..2] eski diziden okumaya devam ediyor; kol, sim, govde dokunulmadi. Olculmedi.
- Belirsiz: fastRun'da uyluk tepesi 70+ yay/Track altinda tutar mi (olcum bekliyor); kol genligi uyluga bagli degisebilir (T-146).
- Tur 2: `tuckLead` 0.05 sn (tuck egrisi mid knot'una dogru yumusakca one alinir), `tuckPeak` 86/82, `shinThrow` 56/160; `Catch` dizi duz kosuda erken acar (agirlik 0 iken eski formul).
- Olculmedi.
- Tur 3: `tuckLead` one almasi salinimin basindan (0) mid knot'una yayildi; uyluk kare hizi `legMaxSpeed`'e dayanmasin.
