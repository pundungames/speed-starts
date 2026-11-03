---
id: T-092
title: Practice'te bitiste sonuc ekrani acilmaz (referans) - HUD'da Exit / Restart kalir, odul ve rekor yok
status: todo
owner: ui-agent
depends_on: [T-090]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlowLogic.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowLogicTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowPracticeTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PracticePanelSmokeTests.cs"
touches_scene: false
branch: agent/T-092-practice-sonuc-yok
commits: []
tests: []
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Referans (03.11 telefon, T-090 notu): Practice'te sonuc/odul ekrani yok; Exit / Restart ust solda. Bizde Practice bitince normal
sonuc ekrani (ResultsScreen) aciliyor. Practice etkinliginde (`training`) bitis cizgisi gecilince: sonuc ekrani ACILMAZ, HUD acik kalir
(Exit / Restart ve Practice paneli calisir, saat bitis suresinde durur), odul / PB / gunluk en iyi / liderlik kaydi YAZILMAZ (bugun Practice
odulsuz; rekor yaziliyorsa da kesilir - once mevcut akista ne yaziliyor oku). Diger etkinliklerde hicbir sey degismez.
Akis `RaceFlowLogic` (saf) + `RaceFlow` (MonoBehaviour); Practice'i anlamak icin `Scripts/Bridge/PracticeSession.cs`,
`Meta/Practice/**` (T-090) oku, onlara YAZMA. Malik'in kodu (`Scripts/Physical/**`) okunur, yazilmaz.

## Kabul kriterleri
- [ ] Practice'te bitis -> ResultsScreen acilmaz, HUD + Exit/Restart calisir; Restart yeni kosu baslatir
- [ ] Practice'te bitis odul, PB, gunluk en iyi, liderlik kaydi yazmaz; 100m ve diger etkinliklerde akis aynen
- [ ] EditMode testi (RaceFlowLogic karari: etkinlik -> sonuc ekrani acilir mi / kayit yazilir mi); PlayMode: PracticePanelSmokeTests'e
      "bitiste sonuc ekrani yok" adimi (gercek tiklama, mevcut dev bitis tetikleyicisiyle)
- [ ] Sabit denge sayisi assert yok

## Geri bildirim
GORSEL  : bitiste HUD saatinin durdugu an mevcut bitis efektiyle (yeni efekt yok)
SES     : mevcut bitis Sfx
DOKUNSAL: mevcut
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Watch Replay (T-094), Practice paneli (T-090), enerji / ayakta cikis (Malik)

## Calisma notu
