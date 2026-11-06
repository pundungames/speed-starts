---
id: T-149
title: Frende ayaklar onde, tum govde geriye yatik (cizgi film kaymasi)
status: todo
owner: gameplay-agent
depends_on: [T-150]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-149-frende-geriye-yatma.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici 06.11: "karakteri biraktigimda fren yaparken ayaklar onde karakter komple geriye dogru yatsin, uzanir gibi, cizgi film karakterleri gibi."

## Bugunku hal (kayit t147-a, stop)
Son tus ~3.8 sn. 4.15'te `_idleStock`, iki bacak `Rest`. Sim freni 4.2-4.9: 8 -> 0 m/s (12 m/s^2), sim'e dokunulmaz. Bu surede karakter Ready cokmesinde (kalca -5..-10) dik, yumruklar yuzde, ayaklar kokle birlikte yerde kayiyor.

## Yon
- `_brake` agirligi: hedef `_idleStock` iken hiza gore (`brakeSpeed` x..y m/s arasi 0..1), tus gelince 0. Giris yayi ~4 Hz sonum 1; cikis ~2.5 Hz sonum ~0.7 (duruyorken hafif sekip Ready'ye oturur). Tek ayak, cift ayak, tutma, dalis agirligi varken 0.
- **Govde:** `lean -= brakeLean * _brake` (yeni alan, ~45): mevcut `SitBack` topuk pivotu tum vucudu topuklarin ustunde dondurur, ayak ucu kalkar (`heelRock`). Bel kivrilmaz, cizgi duz. Bas one bakar (`headKeep`).
- **Bacaklar:** `Rest` hedefi `_brake` ile `brakeFront` (son basan olmayan bacak onde, uzak, diz neredeyse duz) ve `brakeBack` (biraz geride, diz kirik) arasina karisir. Rest'ten once ayak yere: zemin kirpmasi gövde hareketinden SONRA (T-113 tuzagi).
- **Kollar:** `bw = _brake` dali: kollar one ve yukari denge icin (`armBrake`), yumruk yuze gitmez.
- Ayaklar frende kayar: kilit yok (kayma istenen sey). Toz yok, ayri is.
- Serit: `stop` senaryosuna `LastRelease + 0.65`, `+ 1.3`. Yeni `brakeRestart`: kos 3 sn, son tustan 0.8 sn sonra (fren ortasi) sirali basis.

## Kabul kriterleri
- [ ] Fren sirasinda hiz 3 m/s ustunde iken: kalca egimi -35 ve otesi, on ayak kalcanin en az 40 cm onunde, topuk yerde (ayak ucu -1 cm altina inmez).
- [ ] Kare basina: kalca egimi 4 derece alti, uyluk ve diz 15 derece alti; bir karede sicrama yok.
- [ ] Hiz 0 olduktan sonra 0.6 sn icinde Ready durusu; hafif sekme kalabilir, titreme yok.
- [ ] `brakeRestart`: fren ortasinda basis yatik govdeyi yumusak toparlar, ilk adim atilir (T-148).
- [ ] Yavas kosudan (hiz < brakeSpeed.x) birakinca yatma yok.
- [ ] Tek ayak ve cift ayaktan birakinca, tutma, dalis: ayni kalir ya da yumusak gecer; bitis sekansi (`RaceFinishGate`, kutlama) Play duman testleri yesil.
- [ ] Sim degismez.

## Kapsam disi
- Sim freni, kayma tozu, ses.

## Calisma notu
