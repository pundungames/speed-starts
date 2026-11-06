---
id: T-139
title: Gecis olcumu - her geciste ardisik kare seridi ve kareler arasi sicrama analizi
status: review
owner: test-agent
depends_on: [T-138]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "scripts/runner-olcum/transition_check.py"
touches_scene: false
branch: agent/ik-kosucu
commits: [78fd292d]
tests:
  - "bu is olcum araci: pass/fail degil, dosya uretir; kosu supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11: "kare kare cok aralikli aliyorsun, daha seri kare alirsan daha guzel inceleyebilirsin. seri kare al ve incele." Tum gecisler (tek ayak, cift, uzun basis, start, durma) tekliyor ve titriyor.

## Isler
1. Senaryo: zamanlanmis basis listesi; her geciste 60 fps ardisik 40 kare serit + kare haritasi.
2. CSV: faz, yerde, yuk, agirliklar, govde yuksekligi, govde egimi, ayak dunya konumu.
3. Analiz: kareler arasi sicrama ve hiz degisimi, sebep olan faz degisimi ile.

## Kapsam disi
- Hareket kodu. Prefab, sahne. Pass/fail esigi yok.

## Calisma notu
- Yeni test RunnerTransitions_WriteFrameStrips: 15 senaryo, zamanlanmis tus olaylari (her olay kendi tam damgasiyla verilir), trans_<ad>.csv her kare, 40 ardisik karelik 8x5 seritler.
- Analiz scripts/runner-olcum/transition_check.py (python -I, yalniz stdlib); elle yapilmis kucuk csv ile denendi.
- Unity/test KOSULMADI; kosu supervizorde (derleme dahil dogrulanmadi).
- Zorlanma: iki tus arasi 15 ms kare adiminden kisa; olaylar bir kare onceden gelecek damgayla enjekte edilir.
- Bos birakilan: kollarin ayri kemik acisi yok, mevcut armX_swing/elbow sutunlari kullanildi.
