---
id: T-107
title: Kosucu olcum duzenegi - senaryo basina CSV + kare kare goruntu (once/sonra karsilastirmasi)
status: merged
owner: test-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [0ee142e3, 923f3fa0, 76fd8428, 81c0bbfa, ac9ec5bc]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Karakter IK işinin (T-108..T-112) her adımı sayıyla ve görüntüyle ölçülsün. Bu oturumda Unity MCP yok; Play'de elle
bakamıyoruz. Gerçek prefab (`HumanCharacterDummy_F`) ve gerçek `RunnerPoseDriver` ile koşan, girdileri
`RunnerInput.Inject` ile veren, sonucu dosyaya yazan **tek bir PlayMode ölçüm testi** yazılacak. Mekanik kodu
değişmez; yalnız okur (Assembly-CSharp'a reflection, `WatchReplaySmokeTests` gibi).

## Kabul kriterleri
- [ ] `[UnityTest]`, `[Category("RunnerProbe")]`, `[Explicit]`: normal suite'te koşmaz. Denge sayısı assert etmez; yalnız "senaryo koştu, dosyalar yazıldı" assert eder.
- [ ] `Time.captureFramerate = 60` ile deterministik; test sonunda geri alınır.
- [ ] Senaryolar: `perfect` (iniş anında bas, `sim.Airborne` karedeki kontrolle, ardışık ayak), `rhythm026` (0.26 sn), `late050` (0.50 sn), `mash012` (0.125 sn, tuş 0.04 sn), `both030` (iki tuş birlikte 0.30 sn), `same030` (aynı ayak 0.30 sn), `holdOne` (tek tuş 2 sn basılı), `holdBoth` (iki tuş 2 sn basılı). Tuş süresi aksi yazılmadıysa 0.07 sn. Her senaryo durağan hâle gelecek kadar sürer (~6 sn); yarış bitmesin (`raceDistance` reflection ile büyütülür, bkz. `WatchReplaySmokeTests`).
- [ ] Her senaryo için `<shotDir>/form_<senaryo>.csv`, kare başına: t, speed, head (`sim.HeadAngle`), hopY, lastResult, lastEfficiency; her bacak: phase, thigh (`angles.thigh`), thighWorld, knee (`angles.knee`), ankleY, kneeY (`Leg.ankle/knee`, runner space); hipY, hipPitch (`hipTarget` runner space); `B-hips` ve `B-chest`'in dünya yaw/roll/pitch'i; kollar: `Arm.swing`, `Arm.elbow`.
- [ ] `<shotDir>/form_summary.csv`, senaryo başına durağan bölümden: ort. hız, tepe hız; ort. ve maks. gövde eğimi; bacak başına thigh min/maks; iki uyluk arası en büyük açıklık (split); uyluk en büyük açısal hızı (deg/s); **takılma sayısı** (bir karede açısal hız değişimi > eşik, eşik dosyada sabit ve açıklamalı); hipY aralığı; en alçak diz ve kalça yüksekliği; kol swing min/maks; kalça ve göğüs yaw aralığı.
- [ ] Kare kare görüntü: dünyaya bakan geçici yakın kamera (koşucunun sağında, kalçaya bakar, tüm vücut kadrajda; RenderTexture'a elle `Render`, `WaitForEndOfFrame` YOK, bkz. TUZAKLAR "batchmode"). Senaryo başına tek PNG `<shotDir>/form_<senaryo>_sheet.png`: durağan bölümden 2 tam adımı kapsayan 12 kare (4x3 ızgara). Ek olarak aynı karelerden biri oyunun kendi `SideFollowCamera` görüntüsüyle (`form_<senaryo>_game.png`).
- [ ] `<shotDir>` `ScreenshotTests`'in kullandığı yoldan alınır (`-shotDir`); `Temp/` altına yazılmaz (TUZAKLAR: Unity kapanırken siler).
- [ ] Test dosyasının `<summary>`'sinde tam koşu komutu yazar (`scripts/kopya.ps1` / `scripts/unity-verify.ps1`'i oku; grafik açık, `-nographics` olmadan).

## Çözülmüş bilgi (yeniden araştırma)
- Koşucu ve girdiye ulaşma: `WatchReplaySmokeTests.cs:138-152` ve `:370-404`. Yeşil başlangıç: `FalseStartSmokeTests`, `PlaySmokeSupport.LoadScene("Race", ...)`.
- `RunnerInput.Inject(int leg, bool pressed, double gameTime)`; `gameTime` = `Time.timeAsDouble`.
- `Leg`/`Arm` alanları public (`[HideInInspector]`): `phase, angles, thighWorld, knee, ankle`, `Arm.swing/elbow`. Driver alan adları: `leftLeg, rightLeg, leftArm, rightArm, hipTarget`.
- Sim özellikleri: `Speed, HeadAngle, HopY, Airborne, LastResult, LastEfficiency`, `T` (tuning).

## Kapsam dışı
- `Scripts/Physical/`, prefab, sahne: yazılmaz. Unity/batchmode çalıştırılmaz: süpervizör koşar.

## Çalışma notu
- Yazıldı: worktree `D:\Pundun\speed-starts-ik` içinde `Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs` (tek test, [Explicit], Category RunnerProbe). Derlenmedi/koşulmadı.
- Her senaryo için Race sahnesi yeniden yüklenir; GO + Running beklenir, `raceDistance` 2000 m'ye çekilir, 18 kare ısınma, sonra senaryo saati başlar. Kare örnekleme LateUpdate'te (execution order 32000) ayrı bir kanca bileşeniyle.
- Girdi: `Inject(leg, down, Time.timeAsDouble)`, `Time.captureFramerate = 60`. `perfect`: `sim.Airborne` true->false karesinde sıradaki bacağa bas (inmezse 0.6 sn sonra yine bas, sayısı özette `fallbackPresses`).
- Zorlandığım yer: kemik eksenleri bilinmediği için kalça/göğüs yaw/roll ilk örneğe göre dönüş olarak (koşucu ekseninde) yazılır; mutlak değil. Takılma eşiği (900 deg/s/kare) ilk tahmin, ilk baseline'dan sonra ayarlanmalı.
- 2. tur (ilk baseline sonrası, derlenmedi): CSV'ye L/R_ankleZ, L/R_toeY, L/R_footY, L/R_footWorldZ, stepping eklendi; kalça/göğüs yaw-roll artık kemiğin sağ vektöründen (Euler yok); özete holdShare, swapShare, corrHipSplit, hipAtWide, hipAtPass, passKnee, flightShare, footSlide, steppingAvg eklendi. Eşikler dosyada adlı sabit (HoldRate 120, SwapRate 600, PassThigh 15, TouchTolerance 0.02, SplitMin 1, SplitEdgeShare 0.2).
- Belirsiz (2. tur): `_stepping` tipi bilinmiyor (bool ise 1/0, sayıysa değeri; alan yoksa boş). Dinlenme ayak yükseklikleri GO sonrası 18 kare ısınmadan hemen sonra, ilk basıştan önce ölçülür (koşucu o an gerçekten duruyorsa doğru). Yaw=atan2(z,x), roll=asin(y) işaretleri koşucu eksenine göre; `hips_pitch` sütunu hâlâ Euler.
- 3. tur (derlenmedi): senaryo başında `RaceStartCamera` isimle bulunup `enabled=false` (giriş sallanması unscaled zamanda, captureFramerate ile yarışıyordu); oyun karesinde `CAMERA_OK <senaryo> yawDeg pitchDeg fov` logu; yeni test `RaceCamera_FarLanes_WriteShots` (LanePreference 1 ve 99, rhythm026 3 sn, `form_lane_near/far_game.png`).
- Bırakılan: hiçbir şey çalıştırılmadı; kamera yönü/kadraj, `-testFilter` ile [Explicit] seçimi ve AddComponent ile iç sınıf kancası doğrulanmadı.
- 4. tur (derlenmedi): senaryolar tap012, tap016, human (System.Random sabit tohum, aralık 0.27±0.04 sn, tuş 0.06-0.16 sn); CSV sonuna L_held,R_held; özete holdFrames, speedRipple, firstSecondSpeedSteps. `Leg.held` alanı yansımayla okunur (yoksa test Assert ile düşer).
- Süpervizör: kopyada koştu, 13 senaryo. Takılma eşiği (900) işe yaramadı; asıl göstergeler holdShare, corrHipSplit, passKnee, kare başı hipY. footSlide salınımdaki kareleri de sayıyor, gerçek basışta ayak ~0.9 m/s.
