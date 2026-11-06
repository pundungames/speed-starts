---
id: T-132
title: Hareket once uyluktan, diz sonra acilir; kosuda omuz az, dirsek cok; kosu ziplamasi yariya
status: review
owner: gameplay-agent
depends_on: [T-131]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [1ae44ce4, 176d56f5, 24fc81e7]
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
Kullanici 05.11: "hareket ederken ilk basta belden hareket edecek sonrasinda dizi acacak, kol hareketleri icin de omuz ekseni acisi azalacak dirsek daha cok on planda olacak. kosarken de ziplamayi %50 oraninda azaltalim."

## Varsayimlar (kullanici sayi vermedi)
- "Belden" = bacakta hareket kalca ekleminden baslar: uyluk once, diz sonra. Kullanici govde demek istediyse duzeltir.
- Omuz acisi %30 az: `armRunBack` -68 -> -48, `armRunFront` 62 -> 44.
- Dirsek: on 85 -> 105, arka 28 -> 15; yavas kosuda da oynar. Tek ayak ve cift ziplama kollari degismez (o sayilari kullanici istedi).

## Isler (uc commit, her biri ayri geri alinabilir)
1. `runBounce` 0.5: duz kosuda ucus yuksekligi ve durus cokmesi yariya; tek ayak ve cift ziplama ayni.
2. Kosuda kol ve dirsek sayilari, `armElbowMinShare` 0.6.
3. `kneeOpenDelay` 2: salinimda ve yakalamada diz gec acilir, uclari ayni.

## Kapsam disi
- Hop ve cift ziplama sayilari, tutma, dalis, ayak kurali. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Uc commit: runBounce 0.5 (1ae44ce4), kol/dirsek + armElbowMinShare (176d56f5), kneeOpenDelay 2 (24fc81e7).
- EvalSwing/Catch ornek metot oldu; diz gecikmesi tum salinimlarda (kosu, hop, cift). Calistirilmadi, derlenmedi.
- Okuma kontrolu: Touching/OtherTakesOver uyluk acisina bakar (degismedi) ama ayak yuksekligine de bakar; diz gec acildigi icin ayak biraz gec "indi" sayilabilir.
- StartCatch `near` testi dizi de karsilastirir; acilan dizde catch daha sik baslayabilir. Bozuk bir sey gorulmedi, degistirilmedi.
