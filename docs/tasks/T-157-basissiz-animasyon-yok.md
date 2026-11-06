---
id: T-157
title: Her hareket basistan: basissiz animasyon yok, ham tus niyeti, bacak degistirme calisir
status: todo
owner: gameplay-agent
depends_on: [T-154]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-157-basissiz-animasyon-yok.md"
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
Kullanici 06.11 (T-154 sonrasi denedi): "hizli giderken tek bacak input veriyorum 2 kere yapiyo o hareketi. bir bacaktan digerine gecerken gecisi yapamiyor normal yuruyor falan. input yoksa kendisi animasyon uretmesin direkt stop durumuna gecsin. KENDIN ANIMASYON YAPMA GECISLERDE. kontrolleri daha optimize edelim. gecis yaparken inputlar karisiyor."

## Olculen bugunku hal (`D:\Pundun\form-probe\t154-f`)
Son basistan sonra baslayan salinimlar (saniye, son basisa gore):
| Senaryo | Son basis hizi | Salinimlar |
|---|---|---|
| `stop` | 9.0 | R +0.08, L +0.42 |
| `fastRun` | 7.1 | R +0.12, L +0.32, R +0.57 |
| `rhythm032` | 8.5 | R +0.13, L +0.48 |
| `hopStop` | 5.1 | R +0.43, R +0.97 (iki sekis, basis yok) |

## Sebepler (koddan)
1. **Basissiz kalkis.** `TickLegs` `case Push`: `leg.toeOff` olunca `_idleStock` degilse `StartSwing`. Erisim siniri (`StanceCap`, `Overstretch`, `behind >= endBehind`) basis yokken de ayagi kaldirir. T-154'te bunu bilerek sekisin "kendi dongusu" yapmistim, yanlis. `_stepNow` penceresi de sekiste `HopRepeatWindow`'a acildi (2436), basis yokken 1 sn daha adim atiyor.
2. **Niyet kabul edilen basistan okunuyor.** `UpdateHop` (2613) `same`/`swap`'i `_posePresses` degisince hesapliyor, yani yalniz kapidan gecen basislarla. Sert kapi basislarin cogunu dusurunce `_hopRepeated` false kaliyor: `L L L R`'de `swap` tutmuyor, hedef 0, kosuya donuyor. Kullanicinin "normal yuruyor" dedigi bu.
3. **`use` cevirme** (1324): `alternating && leg == _poseLastLeg` ise basis oteki bacaga gidiyor. Kapi basis dusurunce sekiste yanlis bacak: `L L R(reddedildi) L(alindi)` kosu sayilip R'ye gidiyor. "Inputlar karisiyor".
4. **Kaybolan basis.** Havadaki basis tamamen dusuyor; inisten hemen once basan oyuncunun basisi bosa gidiyor.

## Istenen davranis
**A. Bir basis = bir hareket.** Salinim yalniz basistan cikar.
- Kosuda: basilan bacak iter ve salinir, oteki iner. Baska salinim yok. Yerdeki ayak basissiz erisim sinirina gelirse `StartSwing` degil stok yol (`Enter(Rest)`). `leg.swungSincePress` false ise salinim serbest (basis var), true ise yok.
- Sekiste: bir basis bir sekis. Inis sonrasi bekleyen basis yoksa ikinci sekis yok.
- Basis yoksa **direkt stop**: hareket biter bitmez (`hopStopDelay` ~0.2 sn inisten sonra; kosuda son bacak indikten sonra) `_idleStock`, `_hop` hedefi 0. Fren pozu hiza gore kendi gelir. `_stepNow`'un yalniz zamana bagli penceresi bu ise engel olmasin. `HopRepeatWindow` serbest bacagi ayakta tutmak icin kullanilmaz.

**B. Niyet ham tustan.** `same`, `swap`, `_hopRepeated`, `_hopPrevLeg`, `_hopPrevTime` `OnTapped`'te, kabul edilsin edilmesin her basista hesaplanir:
- Ayni tus (pencere icinde): sekis o bacakta.
- Sekisteyken tus degisti: o bacakta sekis (`swap`). Ust uste iki degisim: kosu, sekis hedefi hemen 0, kapi kosu kapisina doner.
- Ayaktan ilk basis ve diger tus: kosu (T-154'te calisiyor, bozma). Cift (`together`) ayni.
- `use` cevirme yalniz kosuda (`_hop * (1-_pair) < 0.5`). Sekiste basilan tus ne ise o.

**C. Tek basis tamponu (sekis).** Kapi kalir, animasyon bitmeden alinmaz. Ama inise `hopBufferTime` (~0.25 sn) kala ya da daha sonra basilan son tus saklanir, ayak inip `hopStanceMin` dolunca o basis alinir (ayni tus: yeni sekis; diger tus: bacak degistirme). Daha erken basis (cift tiklama) yok sayilir. Yalniz son tus. Ham niyet (B) her basista isler, tampon yalniz pozu tetikler.

**D. Bacak degistirme.** `L L L R R R` hem 0.27 hem 0.60 ritminde: `_hop` hic dusmez (>= 0.9), L serbest bacaga gecer, R sekise. Iki bacak ayni anda yerde en fazla catch suresi. Kosu gibi adim yok.

## Kabul kriterleri
- [ ] Son basistan sonra en fazla 1 salinim (basilan bacagin kendisi): `stop`, `slowStop`, `fastRun`, `rhythm032`, `hopStop`, `hopSwap`, `spamHop`, `slowHop`, yeni `fastTap`. Ayaktan tek basista 1 sekis (`oneTap`).
- [ ] `fastTap` (kosu 0.24, 3 sn, sonra ayni tusa bir kere, 2 sn bosluk): tek sekis ya da tek adim, sonra fren pozu.
- [ ] `hopTap` (ayni tus 1.0 sn aralik, 4 sn): basis sayisi = sekis sayisi.
- [ ] `hopSwap` (0.27) ve yeni `hopSwapSlow` (0.60, 2 sn + 2 sn): degisimde `d_hop` >= 0.9, yeni bacak sekiste, eski bacak 55/175.
- [ ] `hopToRun`, `runToHop`, `start`, `blockStart`, `blockPair`, `spamRun`: kirilma yok, T-154'ten kotu degil. Tus basilmaya devam ederken `rhythm032`, `fastRun`, `roughRun`, `slowRun` kosu seritleri ayni kalip.
- [ ] Sekis olculeri (T-154) bozulmadi: kalca 0.28-0.32, uyluk -57, serbest bacak ve sabit kol sabit, ikinci fark <= 2.5 cm.

## Kurallar
- Oku: `docs/TUZAKLAR.md` son 40 satir, `docs/tasks/T-154-tek-ayak-sekis.md` calisma notu, `T-150`.
- Yerler: `OnTapped` (~1280), `HopGateOn`, `HopLegInAir`, `BeginStance`, `TickLegs` `case Push` (~1831) ve `Land` (~1880), `SolveStance` toe-off (~2280), `_stepNow` (2436), `UpdateHop` (2600 civari), `_standLeg` blogu.
- Kapi yalniz pozda; sim her basisi sayar (T-150), sim'e dokunma (`RunnerSim.cs` baska agent'ta).
- Input hic bir uzvu snap etmez. Basis yalniz hedef degistirir.
- Yeni alanda Tooltip. Kullanilmayan kalan alan ve sabiti sil (grep `Assets`). Prefab ve sahneye dokunma.
- Unity, batchmode, verify calistirma; derleyici yok, C# 9 ve imza hatasi yapma. Olcumu supervizor yapar.
- Her adim ayri commit: `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasi scratch'te (`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\1c7d0dba-289b-4948-ba09-42f88d086602\scratchpad\`), adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. `git add -A`, push, reset yok. Calisma yeri `D:\Pundun\speed-starts-ik`, dal `agent/ik-kosucu`.

## Calisma notu
