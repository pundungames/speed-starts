---
id: T-151
title: Tek ayak ve cift ayak sekisi kisa ritimde alcak ve yumusak; kalca ucgen dalga yapmaz
status: review
owner: gameplay-agent
depends_on: [T-149]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-151-tek-ayak-sekis-yumusak.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [c86df12a, 14fc2247, 9b44b39a, a2035100, b2231290, a99f5145, f6621997]
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
Kullanici 06.11: "tek bacak kosarken hizli hizli basarsam falan veya normal basarken de karakter cok titriyor, bir anda asagiya iniyor falan, ziplama konusunda problemi var, saglikli smooth calismiyor."

## Sebep (kayit t149-b, hopSwap 2.40-2.93, tus 0.27; spamHop ayni)
1. **Sekis cok yuksek:** kalca 99 -> 124 cm, 25 cm; 0.27 sn'de bir, saniyede ~4 kez. Sure ne olursa olsun `hopRise` 0.26 tam. Tepe hizi 3 m/s (`hopHipRiseMax` 3.2, `hopDropSpeed` 3.5 sinirlari). Asil "titreme" bu.
2. **Dipte ani donus:** `Push` karesinde `d_bodyYVel` -1.19, sonraki kare +2.44: bir karede 3.6 m/s. Kalca ikinci farki 5.6 cm/kare^2. Sebep `rise * 4w(1-w)` parabolu: basta tam hizla baslar.
3. **Ayak yere degmiyor:** `Land` 2.62-2.65 ayak ucu 0.09-0.10 havada (`landHover` 0.04), R+ gelince tek kare `Push 1 1`, sonra `Recover`. Basma yok; `Push` karesinde kalca 2 cm birden duser.
4. Denge bacagi `hopSway` hizdan okur: `L_thigh` kare basina 5-7 derece oynar; kalca sakinlesince duzelmeli.

Kod: `UpdateBodyHeight` ucus dali (`TryIncoming`, `rise`, `target`, `landHover`), `Balance`, `hopLead`, `TickLegs` `Land`.

## Yon
- Yukseklik sureye bagli: `T = _flightT + remaining`; hop/pair payi `hopRise * Clamp01(Pow(T / hopRiseTime, 1.5))`, `hopRiseTime` ~0.45 (yeni alan). 0.22 sn ucus ~9 cm; 0.45+ tam.
- Sekil: hop/pair payi `4w(1-w)` yerine `sin^2(pi w)`: iki uc da sifir hiz, dip yuvarlak. Kosu payi (`flightRise`) aynen parabol.
- Ayak gercekten basar: hop agirliginda `landHover` sifira gider, `hopLead` ~0.08 (kontrol et: `Land` otomatik planted olsun). Basis `Push`'u yerde bulur.

## Kabul kriterleri (sayilar kayittan)
- [ ] hopSwap 0.27: kalca araligi 8-12 cm (25'ti); spamHop 0.32: 14 cm alti; yeni `slowHop` 0.45: 20 cm ustu.
- [ ] Yerlesik sekiste kalca kare hareketi 3 cm alti, ikinci farki 1.5 cm alti (5.6-8.6 idi). runToHop, hopToRun, runSpamHop gecislerinde de 5 cm alti tek kare, ikinci fark 3 cm alti.
- [ ] 0.27 sekiste ayak her dongude en az 3 kare yerde (`R_grounded`), ayak ucu -1 cm altina inmez.
- [ ] Denge bacagi uyluk kare hareketi 4 derece alti.
- [ ] pairOrder 0.35: kalca araligi 20 cm alti, kare hareketi 3.5 cm alti.
- [ ] Sim degismez. Tutma, dalis (`_ballistic`), duz kosu, kalkis, fren 0.0 fark.

## Kapsam disi
- Kollar (T-152).

## Calisma notu
- Yukseklik: ucus dalinda hop/pair payi `hopRise * Max(_hopBodyW,_pair) * Clamp01((T/hopRiseTime)^1.5) * sin^2(pi w)`, `hopRiseTime` 0.45 (yeni). `T` basista ziplayabildigi icin `_hopFlightT` ile 10/s hizla izlenir (flightSpring 14 Hz adimi yutmaz); kosu payi aynen. `LimitHeightSpeed` degismedi.
- Ayak: `landHover * (1 - hopWeight)`; `hopLead` 0.05 -> 0.08 (prefab/asset'te kayitli degil, git grep bos). `Land -> Push` esigi (4.5 cm) degismedi: ayak yine de uste kalirsa orayi genisletmek gerekir.
- Toe-off: grounded `Push` bacagi plain kosu degilse `OtherTakesOver`'a bakmaz; cikis `SolveStance`'te kalcanin gerisinde ve `d >= reach*Overstretch` (uzunluk) ya da `UpdateBodyHeight`'ta `legCap < max(legNeed, bodyY)`. Sureye bagli degil, bu yuzden alcak sekiste ayak erken kalkabilir; olculmedi.
- Olculmedi (Unity yok); sayilar kabul listesine gore ilk deneme.
- Tur 2: `hopContact` 24/158 (prefab'ta yok); `BeginStance`te hop > 0.5, Recover/Mid, `SwingShare` < 0.25, ayak yerde ise yakalama yerine `StartSwing`. Olculmedi.
- Tur 3: `_sameStreak` (ayni tus art arda, aralik >= pressFollowSame); >= 2 iken `PressStillBusy` sorulmaz. `BeginStance` isaretli toe-off'u silmez (kalkis karesi sicramasi). Olculmedi.
