---
id: T-145
title: Duz kosuda bacak once karna cekilir, sonra kaval kemigi one atilir
status: todo
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
started:
finished:
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
