---
id: T-147
title: Duz kosudan tek ayaga geciste govde yavas geri oturur, ilk sekis sivri olmaz
status: review
owner: gameplay-agent
depends_on: [T-146]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "docs/tasks/T-147-kosudan-tek-ayaga-gecis.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
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
Kullanici 05.11: "normal kosudan tek bacak kosuya gecerken cok sert puruzlu geciyor." Ayni tusa ikinci basista govde ve kalca kisa surede yeni duruma atlamasin. Yerlesik tek ayak hareketi (sekisler oturduktan sonra) degismez.

## Sebep (kayit t146-a, runToHop, ikinci R+ 4.00 sn)
1. **Kalca egimi:** `lean -= hopBackLean * _hop`; `_hop` adim hedefini `gaitBlendSpring` 5 Hz ile alir (0.133 sn'de 0.92). Kalca +16.9'dan -27.5'e 0.18 sn'de iner, kare basina en cok 8.3 derece.
2. **Ilk sekis sivri:** `UpdateBodyHeight` ucus yayi `rise = hopRise * Max(_hop, _pair)` okur. Inecek bacak (R, thigh -59, 0.22 sn sonra yerde) kisa bir ucusa bu yuksekligi sigdirir: `hipY` 0.945'ten 1.201'e 5 karede +5.2 cm/kare cikar, sonra bir karede -4.9 cm/kareye doner (`hopHipRiseMax` ile `hopDropSpeed` sinirinda). Yerlesik sekisin tepesi ise yumusak (4.43-4.53 sn).
3. **Iki karelik carry:** `L_carry` 4.017 ve 4.033'te 1, sonra 0. Bu iki karede govde -1.8, -3.0 cm/kare cokuyor, sonra ucus basliyor ve hiz bir karede -1.49'dan +3.0 m/sn'ye donuyor. Rest'e giren L neden carry saydi: bul.

## Kabul kriterleri (runToHop, ikinci R+ sonrasi 0.35 sn)
- [ ] Kalca egimi kare basina 4 derece ve alti; hedefe ~0.4 sn'de varir.
- [ ] Kalca yuksekligi kare basina 3 cm ve alti; bir karede yon degisimi (ikinci fark) 3 cm/kare ve alti.
- [ ] Iki karelik carry cokusu yok.
- [ ] Yerlesik sekis ayni: `hopSwap` 2. saniyeden sonra kalca, bacak, kol satirlari eskisiyle ayni.
- [ ] Tutma, dalis, cift ziplama bozulmaz: `holdOne`, `holdBoth`, `pairOrder`, `runToPair`, `pairToHop` kare farklari artmaz. Tutmada geri oturma ileri devrilmeye karsi kalmaz.
- [ ] Sim, hiz, zamanlama, T-145 bacagi, T-146 kolu degismez.

## Kapsam disi
- Yerlesik tek ayak sekisinin yuksekligi ve egimi. Kol `hw` karisimi (once govdeye bakilir; olcumde hala sert gorunurse ayri tur).

## Calisma notu
- Commit 1 (296e8fec): `_hopBody` yavas yay (`hopBodySpring` 2 Hz), govde `Min(_hopBody, _hop)` okur (cikis eskisi gibi hizli, giris yavas); kalca egimi ayrica `(1 - _heldNow)` ile.
- Commit 2: carry kok sebebi: hop acilinca digeri `Enter(Rest)` ile serbest birakiliyor ama `idle` 0'dan basliyor; `Rest` carry'si `idle < IdleCarry` oldugu icin ~2 kare "evde duruyor" sayiliyordu. `_hopFreed` (hop hedefi acik, tutma yok) ile basilmayan bacak Rest'te carry etmez.
- Olculmedi.
