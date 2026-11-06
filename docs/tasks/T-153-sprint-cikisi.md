---
id: T-153
title: Yaris basi sprint dizisi: blok durusu (READY), kalca yukari (SET), cikis ve one yatik ivmelenme (GO)
status: todo
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
commits: []
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips + Play duman testleri supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
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
