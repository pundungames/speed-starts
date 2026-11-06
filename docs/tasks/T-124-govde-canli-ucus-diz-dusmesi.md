---
id: T-124
title: Kollar az geri, ust beden hareketli, perfect kosu daha havada, gec basista dizin ustune dusme yok
status: review
owner: gameplay-agent
depends_on: [T-123]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [a8f63b83, 4d7c1e68, cce030ec, f048344a]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11 (`1eef3a0d`): "cogunlukla guzel". Ayak kilidi tamam, sahte cift basis duzeldi, ayakkabi ucu cozuldu, tek ayak daha iyi ama gelisebilir. Bu durumu bozma. Istekler:
- "ust vucut hala cok sabit duruyor, daha hareketli olmali, one arkaya egilmeli"
- "kosarken bazen ayagi yetismiyor, karakter dizinin ustune dusuyormus gibi oluyor"
- "ne kadar perfect ise o kadar karakter havada uzun kalmali, birazcik ziplamali yukari"
- "cift ziplamada kollari cok geriye gidiyor, 10 derece azalt; tek ziplamada da ayni sekilde"

## Isler (uc ayri commit, bu sira)
**A. Kollar ve ust beden (bacak ve kalcaya dokunmaz):**
- `armPairBack` −92 -> −82, `armHopDown` −65 -> −55.
- Kosuda ust beden: `UpdateBounceLean`'e kosu agirligi `_gaitW * (1 - _hop) * (1 - _pair) * (1 - _dive)`. Her adimda one geri sallanma (`runRock` ~8 derece, itiste one, havada geri; `_armKick` saati) ve ivmeye tepki (`accelLean` ~10 derece: hizlanirken one, yavaslarken geri; `_accel`). Yalniz `baseBend`, `bend`; `lean` ve `legLean` yok. Gerekirse `bounceLeanSpring` frekansi ~7 Hz: 5 Hz 4 adim/sn'de hareketi eziyor olabilir. `hopRock` 7 -> 12. Olcum: `trunkLean` kosuda 17-20 derece arasi neredeyse sabit, hedef belirgin dalga.

**B. Perfect daha havada:** `flightRise` 0.09 -> 0.15, `formWeight` 0.4 -> 0.8: ritim iyiyse kalca yayi yuksek, bozuksa alcak (`Form`; zaten hesapta). Hiz, zamanlama yok. Hiz sinirlari (`maxRiseSpeed`, `hipRiseMax`) yayi duzlestiriyorsa yalniz gereken kadar ac; tek tepe kalsin.

**C. Dizin ustune dusme:** veri `D:\Pundun\form-probe\t117f\form_human.csv` t=3.52-3.60:
| t | kalca | L | R |
|---|---|---|---|
| 3.52 | 0.883 | Recover 18/105 | Reach 13/43 |
| 3.55 | 0.833 | Recover −12/83 | Reach 19/51 |
| 3.58 | 0.783 | Recover −43/64 | Reach 30/79 |
| 3.60 | 0.779 | Recover −52/57 | Reach 34/97 |
En alt kalca: perfect 0.925, human 0.779, rough 0.769, tap012 0.787, tap016 0.809. Iki ayak havada, `UpdateCarry`'de `Reach` hep `carry`; diz katli bacak "tasiyor" sayilip govde ona iniyor. Basis gecikince oluyor. Duzeltme: yeri olmayan ayak govdeyi asagi cekmez; `carry` ayak gercekten yerdeyken sayilir (dalis, tutma, bekleme durusu aynen). Ayak havadaysa bacak yere dogru acilir (`Reach` dizi hizla acilsin), govde yayda kalir. Kosarken kalca stance yuksekliginin ~3-4 cm altina dusmez (hiza gore surekli). Gec basista ayak yetisir.

**D. Direkt gecis:** kullanici: "normal kosarken tek ziplama ya da cift ziplamaya gecerken cok karmasik goruntuler cikiyor, direkt verdigim inputlara gecmesi lazim." Ara karisimlar kisa olsun: ayni ayak tekrar = hop hedefi hemen tam (ilk tekrar yari degil), diger ayak = hemen 0; birlikte basis = cift hemen tam. Agirlik hizli (~5-6 kare) ama snap yok, uzuvlar `Track` ile surekli. Denge bacaginin `idle` gecisi de ayni hizda. Salinimdaki bacak eski anahtarla bitmesin: anahtarlar agirligi her kare okusun ya da hedef yenilensin.

## Kapsam disi
- Baska hic bir sey. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- A: armPairBack -82, armHopDown -55, hopRock 12, bounce yay 7 Hz, runRock 8 + accelRock 10 (yalniz baseBend/bend). B: flightRise 0.15, formWeight 0.8, maxRiseSpeed 2.8, hipRiseMax 2.5.
- C: Reach carry yalniz yere ReachCarryHeight (0.08) icinde ya da _dive > 0.3; TryIncoming'e Reach eklendi (govde inise yaylanir); Reach dizi 1.8x hizli acilir. carry etkisi: KeepAboveGround havadaki Reach'in diz/ayagini zemin kontrolune alir, UpdateBodyHeight carry yoksa ucus yayina gecer, SitBack pivotu yalniz yerdeki Reach'e verilir, FreeFootRise/PinFeet havadaki Reach'e de uygulanir.
- D: hop hedefi basistan (ayni ayak <1 sn = 1, diger ayak = 0), HopBlend 18, IdleRate 12, RefreshSwingKeys. Geriye yatma `lean -= hopBackLean*_hop` dogrudan _hop ile; zincirden gecmez.
- Olculmedi. Emin degilim: ilk tekrarda hop agirliginin hemen 1 olmasi, Reach carry esiginin carry'yi kareler arasi titretmesi, RefreshSwingKeys'in swing ortasinda hedefi kaydirmasi.
