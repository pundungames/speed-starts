---
id: T-083
title: PlayMode'daki 4 kirmizi test - eskimis varsayimlar (kamera height, ruzgar suresi) ve batchmode ekran goruntusu
status: review
owner: test-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PerfectStepFeedbackTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/StepFeedbackVisualTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/WardrobeIntegrationTests.cs"
touches_scene: false
branch: agent/T-083-playmode-testler
commits: []
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/HomeRaceButtonSmokeTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PerfectStepFeedbackTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/StepFeedbackVisualTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/WardrobeIntegrationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
main'de batchmode PlayMode 41 testten 4'u kirmizi (03.11, `pipeline-logs/playmode-2026-11-03_104541.xml`). Teshis (kod okuyarak):
1) `HomeRaceButtonSmokeTests.Race_RestartButton_ResetsPlayerClockCameraAndCountdown` - satir ~427 `GetField("height")` null: Malik fd891f4d'de
   `SideFollowCamera.height` alanini kaldirdi (pitch/zoom geldi). Beklenen kamera konumunu yeni public API'den hesapla (`StandingHeight` ozelligi
   ya da kaynakta ne varsa; once dogrula).
2) `PerfectStepFeedbackTests.MomentumWindFollowsSpeedAndClearsOnReset` - sabit 0.4 sn bekleme; Serhat dc730046'da `MomentumWindFeedback` suresini
   0.32 -> 0.9 yapti. Sabit sure yerine kosula bekle (`WaitUntil` + ust sinir), sure degisince kirilmasin.
3) `StepFeedbackVisualTests.CaptureActualRaceFeedback` - 180 sn zaman asimi, `WaitForEndOfFrame` batchmode'da donmuyor (docs/TUZAKLAR.md).
4) `WardrobeIntegrationTests.TabsStayInside_EmotesStayInline_BackgroundIsIsolated` - `CaptureScreenshotAsTexture` batchmode'da null.
3 ve 4: ekran goruntusu kismi `Application.isBatchMode`'da atlanir (ya da `yield return null` + kamerayi elle Render), mantik assert'leri kalir.
3 icin ayrica: PERFECT etiketini aramadan once basisin Good oldugunu ayri bir assert ile kontrol et (urun hatasi mi ortam mi ayrilsin).
Yalniz bu 4 test dosyasi; urun koduna dokunulmaz. Unity calistirilmaz (supervizor kopyada kosar). Once oku: docs/TUZAKLAR.md.

## Kabul kriterleri
- [ ] 4 testin her biri bugunku koda gore yazilmis; sabit sure/sabit denge sayisi yok
- [ ] Batchmode'da ekran goruntusu almayan testler mantik kontrolunu yine yapiyor
- [ ] Urun kodu degismedi

## Kapsam disi
- Urun kodu (SideFollowCamera, MomentumWindFeedback, PerfectStepFeedback), diger PlayMode testleri

## Calisma notu
1) Restart testi: beklenen kamera konumu artik distance*distanceScale ve lookHeight + back*tan(pitch) ile (SideFollowCamera public alanlari, SnapToTarget/Desired ile ayni formul).
2) Ruzgar testi: sabit 0.4 sn yerine ribbon pasif olana kadar bekler (5 sn ust sinir).
3) Gorsel test: WaitForEndOfFrame yok (2 kare); PERFECT oncesi LastResult==Good ve LastEfficiency>=0.7 ayri assert; Capture batchmode'da atlanir.
4) Gardrop testi: ekran goruntusu Shoot() yardimcisina alindi, batchmode'da yalniz bir kare bekler; mantik assert'leri duruyor.
Calistirilmadi (dogrulama supervizorde). Urun kodu degismedi.
