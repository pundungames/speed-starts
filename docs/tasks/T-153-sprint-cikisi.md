---
id: T-153
title: Yaris basi sprint dizisi: blok durusu (READY), kalca yukari (SET), cikis ve one yatik ivmelenme (GO)
status: review
owner: gameplay-agent
depends_on: [T-152]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBots.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-153-sprint-cikisi.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [4826ef02, 239cecba, 27eb46d6]
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips + Play duman testleri supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11, 4 kareli sprint cikisi gorseli: "sunlari da yap." Durus su an Ready boks cokmesi (yumruklar yuzde, dik). Gorseldeki dizi:
1. **Yerine gec (READY):** on ayak blokta, arka diz yere yakin, govde one kapanmis, kollar duz asagi, eller yerde, bas asagi.
2. **Hazir (SET):** kalca omuzlarin ustune kalkar; on diz ~90, arka diz ~120, kollar dikey, eller yerde, bas asagi.
3. **Cikis (ilk basis):** govde cok alcak ve one yatik (yataydan ~30 derece), arka bacak iter, on diz one gider, bir kol one digeri arkaya.
4. **Ivmelenme:** govde ~45 derece one, arka bacak uzamis, on diz yukarda, kollar genis; birkac adimda kosu durusuna dogrulur.

## Yerler
- `RaceStartPhase` (Ready, Set, Go, FalseStart, Idle): `RaceSessionAdapter.OnSequencePhase` ve `RaceBots.OnStartPhase` burada. Girdi GO'ya kadar kapali (`IRaceStartSignal`).
- Oyuncu ve botlar ayni dizi. Sahne, prefab ve asset yok; kod yeterli (blok modeli yok, istenirse ayri asset isi).
- PhysicalTest ve sayaci olmayan oturumlar eskisi gibi: `startStance` varsayilan `None`.

## Yon
- Surucu: `public enum RunnerStartStance { None, Marks, Set }`, `startStance` alani. `_stanceW`, `_stanceSet` yaylari (~2.5 Hz, sonum 1). Rest hedefi bacak basina `marksFront/marksRear`, `setFront/setRear`; `lean += stanceLean` (one); kollar `armStance` dali; ayak ucu `heelRock` ile degil, on ayak duz, arka topuk kalkik.
- `startStance` ilk alinan basista `None` olur ve `_driveW = 1` (agirlik); `driveLean` ~45 hizla ve zamanla (~1 sn) soner. `fromStand` kalkis da ayni `_driveW`'yi alir (T-148 duz kalkis kullanici "dik" diyebilir; kucuk `driveLean` payi).
- Kollar `_driveW` iken genis (`amp` -> 1).
- Bridge: Ready -> Marks, Set -> Set, Go -> dokunma (basisa kadar Set), Idle/FalseStart -> None. Bot ve oyuncu ayni.

## Kabul kriterleri
- [ ] Marks: kalca 0.40-0.55, arka diz yerden 10 cm alti, eller yerden 8 cm alti ve yere girmiyor, ayak ucu -1 ustu.
- [ ] Set: kalca omuzlarin ustunde, kol dikey (swing dunyaya gore +-10), eller yerden 8 cm alti, on diz 80-105, arka diz 110-135.
- [ ] Gecis Marks -> Set -> ilk basis: kalca kare 4 cm, egim 5 derece alti, ayak yerde kalir, tek karede sicrama yok.
- [ ] Ilk basistan sonra 0.2 sn icinde govde egimi 40-60 one; kosu durusuna (egim ~16) ~1 sn'de doner; ayak ucu zemine girmez.
- [ ] Iki tus, tutma, tek tus tekrar: blok durusundan da yumusak baslar (`blockPair`, `blockHold`, `blockHop`).
- [ ] T-145..T-152 senaryolari `startStance` None iken 0.0 fark (fromStand kalkisin izin verilen `_driveW`'si haric); hiz ve sim 0.0 fark.
- [ ] Play duman testleri: yeni kirmizi yok; FAIL (false start) sonrasi yeniden READY'de poz gelir, yaris sonu ve yeniden baslama bozulmaz.

## Calisma notu
- Senaryolar: `blockStart/blockPair/blockHold/blockHop`; harness `Ev.Stance` ile surucunun `startStance`ini yazar (Bridge gibi); CSV'ye `L_handY/R_handY` (onkol kemiginin cocugu).
- Surucu: `RunnerStartStance`, `UpdateStart` (`_stanceW`, `_stanceSet`, `_driveW` yaylari), Rest hedefi marks/set front(sol)/rear, `lean += stanceLean*_stanceW + driveLean*_driveW`, kalca siniri `stanceHipMax`, arka topuk `stanceRearToe`, `armStance`, `StanceHome` ve kol genisligi `_driveW` ile. Ilk alinan basis `startStance`i None yapar; `fromStand` ise surus baslatir (blokta 1, degilse `driveLeanStand/driveLean`).
- Bacak acilari uzanma esitligiyle secildi (arka ayagin ToeRise'i dahil): Marks on (85,60) arka (25,74); Set on (60,90) arka (15,108). Kol uzunlugu, kalca yuksekligi, eller yere degiyor mu OLCULMEDI; sayilar tahmin.
- Bridge: `RaceSessionAdapter.ApplyStartStance` (surucu bir kez `GetComponentInChildren`), `RaceBots.ApplyStartStance` (Bot.Driver CreateRunner'da). Ready->Marks, Set->Set, Go dokunmaz, digerleri None.
- Tur 2: drive hiz carpani `SmoothStep(driveSpeed.x, .y, v)` yanlisti (ters-lerp degil, negatif hedef); `SmoothStep(0,1,InverseLerp)`; `UpdateBrake` baştan dogruydu. `blockOutSpring` 2.5 Hz, `driveTime` 1.2, harness her senaryo basinda `startStance`i None yapar, bacak/lean sayilari olculen L=0.436'ya gore.
- Kol: swing govdeye gore oldugundan duruşta `swing = ParentTilt(arm) + armStance.x` (kol ebeveyninin dunyadaki one egimi); `armStance` artik dikeyden. Olculmedi.
- Tur 3: `stanceLean.x` 84, `blockOutSpring` 2 Hz, `armDriveQuick` 1.5 / `armDriveWidth` 0.35 (`StepArm`, `_driveW` 0 iken eski). Olculmedi.
- PlayMode: `FalseStartSmokeTests` iki test (stance READY/SET/GO/ilk basis, FalseStart/yeniden READY); `startStance` reflection ile okunur, bot yoksa atlanir. Calistirilmadi.
