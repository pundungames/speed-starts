---
id: T-149
title: Frende ayaklar onde, tum govde geriye yatik (cizgi film kaymasi)
status: review
owner: gameplay-agent
depends_on: [T-150]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RunnerCooldown.cs"
  - "docs/tasks/T-149-frende-geriye-yatma.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [014be9a7]
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
- [ ] Kare basina: kalca egimi 5 derece alti, hipY 4 cm alti, uyluk ve diz 15 derece alti; bir karede sicrama yok.
- [ ] Hiz 0 olduktan sonra 0.6 sn icinde Ready durusu; hafif sekme kalabilir, titreme yok.
- [ ] `brakeRestart`: fren ortasinda basis yatik govdeyi yumusak toparlar, ilk adim atilir (T-148).
- [ ] Yavas kosudan (hiz < brakeSpeed.x) birakinca yatma yok.
- [ ] Tek ayak ve cift ayaktan birakinca, tutma, dalis: ayni kalir ya da yumusak gecer; bitis sekansi (`RaceFinishGate`, kutlama) Play duman testleri yesil.
- [ ] Sim degismez.

## Kapsam disi
- Sim freni, kayma tozu, ses.

## Calisma notu
- Yapildi: `_brake` agirligi (`UpdateBrake`, hedef = `_idleStock` x hiz SmoothStep(brakeSpeed) x tus/hop/pair/dive/hold yok; giris 4 Hz/1, cikis 2.5 Hz/0.7); `lean -= brakeLean * _brakeRaw`; Rest hedefi `brakeFront`/`brakeBack`; kollarda `armBrake`. `_brake` 0 iken eski yol ayni.
- Bacak acilari kalcadan ayni dikey erisimde secildi (crouch 1.53L, front 46/168 -> 1.52L, back 55/110 -> 1.54L), iki ayak da yerde kalsin diye; gorevdeki 60/170 ve 38/150 farkli yukseklikte olurdu.
- Olculmedi (Unity calistirilmadi): SitBack'in iki topugun ortalamasina donmesi on ayagi nasil etkiliyor; ayak yerden kalkarsa AnkleGround kirpmasi yukari iter. brakeLean/brakeFront ilk ayar adayi.
- Cakisma: `RunnerCooldown` (bitis) kendi dik durusunu `crouch`/`ready` uzerine yaziyor, sim hizi hala yuksekken tus yoksa fren de devreye girer; ozel dal eklenmedi.
- Senaryolar ayri commit (014be9a7): stop +0.65/+1.3, brakeRestart, slowStop.
- Tur 2: brakeLean 34, brakeSpeed (2.5,5), yaylar 3 Hz/1 ve 2 Hz/0.75; bacaklar (40,172)/(52,118): istenen 36/172 ve 50/118 Extent 1.69 ve 1.62 verirdi, esitlik icin 40 ve 52 (1.61 / 1.60). Olculmedi.
- `brakePose` seam: `RunnerCooldown` aktifken false, `Restore`da true; fren bitis sekansinda kapali.
