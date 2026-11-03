---
id: T-096
title: FPS toleransi testi - ayni girdi 30 / 60 / 120 FPS'te ayni yaris sonucunu verir mi (olcum + PlayMode testi)
status: todo
owner: test-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/FrameRateToleranceTests.cs"
touches_scene: false
branch: agent/T-096-fps-tolerans
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
Yol haritasi listesi 02-05 / 03-07 / 08-02: "30/60/120 FPS ayni girdi icin tolerans icinde benzer sonuc"; olculmedi. Kosu fizigi Malik'in
(`Scripts/Physical/RunnerSim.cs` saf C#, `RunnerBody.cs` girdiyi zaman damgasiyla sim'e verir; CLAUDE.md "Akis"). PlayMode testi yaz:
ayni basis takvimi (ornegin ritim 0.32 sn sol/sag sirayla, ve bir "sik basma" takvimi) `Time.captureFramerate` = 30, 60, 120 ile ayni
oyun saatinde verilir; her kosu icin 100 m bitis suresi (ya da bitmezse 6. saniyedeki mesafe ve hiz) olculur ve karsilastirilir.
Girdiyi gercek girdi yoluyla ver (`RunnerInput.Inject` ya da bizim `RunnerTapTranslator` / `ArrowKeyRunnerInput` hangisi zaman damgasini
oyun saatinden aliyorsa - once oku; CLAUDE.md "Play mode testi" Malik'in kendi olcum yontemi). PlayMode asmdef'i Assembly-CSharp'i
gormez: mevcut testler gibi Core arayuzleri (IRaceSession, IRaceTelemetry) ya da reflection (`PerfectStepFeedbackTests`,
`StepFeedbackVisualTests` kalibi). Tolerans: bitis suresinde %1 (ya da 6. sn mesafede %1); her FPS'in sayilari `Debug.Log` ile
"FPS_TOLERANCE fps=.. finish=.. dist6=.. speed6=.." satiri olarak yazilsin (supervizor merkezi kosuda okuyacak).
Malik'in koduna YAZMA. Fark tolerans disindaysa test kirmizi kalir - [Ignore] koyma; calisma notuna olcum beklentisini ve kodda FPS'e
bagli olabilecek yeri (dosya:satir, ornegin Update'te deltaTime ile entegrasyon) yaz; supervizor Malik'e iletir.

## Kabul kriterleri
- [ ] FrameRateToleranceTests: iki takvim x 3 FPS, sonuclar loglanir, tolerans assert'i
- [ ] Calisma notunda: girdi yolu, olcum noktasi, FPS'e bagli olabilecek kod yerleri
- [ ] Unity calistirma (merkezi dogrulama supervizorde)

## Kapsam disi
- RunnerSim / RunnerBody duzeltmesi (Malik), cihazda FPS olcumu

## Calisma notu
