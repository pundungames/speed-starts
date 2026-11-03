---
id: T-096
title: FPS toleransi testi - ayni girdi 30 / 60 / 120 FPS'te ayni yaris sonucunu verir mi (olcum + PlayMode testi)
status: merged
owner: test-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/FrameRateToleranceTests.cs"
touches_scene: false
branch: agent/T-096-fps-tolerans
commits: [55dd9bff, 68022b57]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/FrameRateToleranceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-03
finished: 2026-11-03
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
- [x] FrameRateToleranceTests: iki takvim x 3 FPS, sonuclar loglanir, tolerans assert'i
- [x] Calisma notunda: girdi yolu, olcum noktasi, FPS'e bagli olabilecek kod yerleri
- [x] Unity calistirma (merkezi dogrulama supervizorde)

## Kapsam disi
- RunnerSim / RunnerBody duzeltmesi (Malik), cihazda FPS olcumu

## Calisma notu
1. Yazildi: FrameRateToleranceTests (2 UnityTest: 0.32 sn ritim, 0.1 sn sik basma; her biri 30/60/120 FPS, referans 60). Unity CALISTIRILMADI; log satiri `FPS_TOLERANCE fps= finish= dist6= speed6=`.
2. Girdi yolu: RunnerInput.Inject(leg, pressed, gameTime) kendi kesin zaman damgasiyla; bir sonraki kare araligina dusen olaylar kare sonunda kuyruga atilir (RunnerBody.Update tuketir, Sim tam o ana ilerler). Time.captureFramerate sabit kare suresi verir; Sim.Time/Distance/Speed reflection ile okunur.
3. Olcum: 6. sn mesafe+hiz (kareler arasi dogrusal duzeltmeli) ve 100 m bitis suresi (dogrusal interpolasyon); tolerans %1 (referansa gore 60 FPS). Bitis yalniz ikisi de bitirdiyse karsilastirilir, biri bitirip digeri bitirmediyse kirmizi.
4. FPS'e bagli olabilecek yerler: RunnerBody.cs:62,80 (Sim karenin sonuna kadar ilerletilir; kalan dt RunnerSim.cs:309-317'de 1/240 alt adimlarla biter, son parca kare uzunluguna gore degisir -> Euler entegrasyonu kucuk fark); RunnerBody.cs:68 (zaman damgasi `min(tap, now)`: olay kare icine gec ulasirsa basis o kareye kayar; gercek girdide ctx.time ile RunnerInput.cs:136-141 Enqueue yas hesabi ayni kareden gelir); RunnerSim.cs:437/447 (StepHead Time ile surekli ama h degisken); Sim disinda Time.deltaTime kullananlar yalniz gorsel/geri bildirim (PoseDriver:952, *StepFeedback) ve RaceRecorder.cs:104 (bitis sonrasi sure toplami). FixedUpdate yok.
5. Okunmadi/kapsam disi: RaceController bitis kontrolu (kare kuantali olabilir), cihaz FPS'i. Beklenti: Sim sabit alt adim + kesin zaman damgasi oldugu icin fark kucuk olmali; kirmizi cikarsa sebep yukaridaki ilk madde olabilir, Malik'e iletilir.
- Supervizor 03.11 olcum: ritim 0.32 sn -> bitis 17.5027 / 17.5028 / 17.5028 sn (30/60/120 FPS), 6. sn 29.635 m, 5.664 m/s - fark %0.001. Sik basma takvimi 6. sn'de 2.89 m ve hiz 0 verdi (kosucu durmus: takvim gercek sik basmayi temsil etmiyor, FPS farki yine %0.1); takvim duzeltmesi backlog.
