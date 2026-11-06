---
id: T-134
title: On bacak yataydan yukari cikmasin; arka bacak geride acik kalsin, oburu inince toplansin
status: review
owner: gameplay-agent
depends_on: [T-133]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [26efc554, be1e3a54, f11b560d]
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
Kullanici 05.11 (ekran goruntusu + cizim): "balerin gibi oldu, bu kadar acmasin yukari. arka bacak hep 90 kaliyor, toplarken biraz daha geride olabilir." Cizimde on bacak yaklasik yatay; arka bacak kalcadan geri ve asagi, uyluk ~-55, diz ~155.

## Ekran goruntusunden okunan (yaklasik, kamera acili)
- On bacak ekranda ~114 (anahtar 92). Hedef cizim ~84.
- Arka bacak ekranda uyluk ~+15, diz ~30: `mid` pozu. On bacak tepedeyken arka bacak toplanmis.

## Isler (uc commit)
1. On bacak: `kneeOpen` uyluk 85 -> 78, `kneeOpenFast` uyluk 92 -> 70.
2. Arka bacak anahtarlari: `recoverFast` (-60, 125), yeni `recoverOpen` (-55, 125), yeni `midRun` (0, 55). Yalniz duz kosuda.
3. Arka bacak, oburu onde havadayken toplanmaz: salinim saati `recover` ile `mid` arasinda bekler.

## Kapsam disi
- Tek ayak ve cift ziplama sayilari, kollar, tutma, dalis, yerdeki ayak kurali. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
Uc ayri commit (26efc554 on bacak, be1e3a54 arka bacak anahtarlari, f11b560d bekleme), hepsi yalniz RunnerPoseDriver.cs.
Yeni alanlar recoverOpen, midRun (Tooltip'li); MidKey() k[2] icin; Touching/OtherTakesOver mid'i okumaya devam eder.
Bekleme yalniz duz kosuda: kendi payi knots[1]..knots[2] arasinda ve oburu havada Mid iken swingT0 += dt.
Derlenmedi, calistirilmadi, olculmedi; sayilar kullanicinin gozuyle ayarlanacak.
