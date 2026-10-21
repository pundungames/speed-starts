# Speed Stars (speed-starts) — Proje Devir Notu

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Projeye yeni bir hesap/oturumla
> devam ederken bağlam buradan gelir. Son güncelleme: 2026-10-21.

## Proje nedir

Mobil, yandan görünümlü bir **sprint (100 m tarzı) koşu oyunu prototipi**. Oyuncu iki tuşla
(sol/sağ ok veya ekranın sol/sağ yarısına dokunarak) koşucunun **her bir ayağını ayrı ayrı**
kontrol eder. Hız, basışların **zamanlamasına** göre kazanılır (ritim / timing oyunu).
Animasyon klip ile değil, **prosedürel** olarak Animation Rigging IK hedefleriyle üretilir.

- Unity **2022.3.62f3** (Built-in render pipeline)
- Paketler: Animation Rigging 1.2.1, Input System 1.19.0 (Active Input Handling = Both),
  Device Simulator, **MCP for Unity** (`com.coplaydev.unity-mcp`, Claude'un editörü kontrol etmesi için)
- Git remote: `https://github.com/pundungames/speed-starts.git`, branch `main`
- Kullanıcı Türkçe konuşuyor; HUD metinleri Türkçe, kod/yorumlar İngilizce.

## Klasör yapısı

- `Assets/Speed-Staes/` — **projenin kendi içeriği** (klasör adı "Staes" yazım hatası, ama değiştirme; referanslar bozulur)
  - `Scenes/Test/TestScene.unity` — tek çalışma sahnesi
  - `Scripts/Runner/` — tüm oyun kodu (namespace `SpeedStars.Runner`)
  - `Materials/` — pist malzemeleri (Track_Asphalt, Track_Line, Track_Finish)
- `Assets/Ash Assets/Control Rig/` — hazır Control Rig asset'i (rig yapısı, `ControlRigBuilder` editör aracı)
- `Assets/Kevin Iglesias/Human Character Dummy/` — karakter modeli (sahnede `HumanCharacterDummy_F`)
- Sahnede ayrıca `LowPoly Male` modeli var.

## Sahne kurulumu (TestScene)

Koşucu objesi üzerinde: `RunnerInput`, `RunnerStepController`, `RunnerMovement`,
`RunnerGaitDriver`, `RunnerDebugHUD`. Kamera üzerinde `SideFollowCamera`.

Rig hiyerarşisi (Control Rig): `Rig` altında `Hip Rig`, `Spine Rig`, `Twist Spine Rig`,
`Leg Rig` (Left/Right Leg Target + Hint, TwoBoneIK), `Hand Rig` (kol IK — runtime'da **kapatılır**),
`Head Aim Rig` (MultiAim). `Step Points` altında her ayak için `Front` ve `Back` noktaları
(ayağın top/ball noktası için düzenlenebilir adım noktaları). Kemik adları `B-shin.L`, `B-foot.R`,
`B-toe.L`, `B-upperArm.R`, `B-forearm.L`, `B-hand.L` vb. — kod bunları **isimle** buluyor.

Pist: `Track`, `Ground`, `LaneLine_-2 … LaneLine_3`.

## Mimari (Scripts/Runner)

Akış: **Input → StepController (+Timing) → Movement**; görsel taraf (`RunnerGaitDriver`) sadece okur.
Execution order: Input −200, StepController −100, GaitDriver −50.

| Dosya | Görev |
|---|---|
| `RunnerInput.cs` | Basma/bırakma olaylarını **kendi zaman damgalarıyla** kuyruğa atar (klavye ←/→, dokunmatik sol/sağ yarı). Input System zamanını game-time'a çevirir. `Inject()` test/bot için. |
| `LegCycle.cs` | Tek bacağın döngüsü, saf zaman tabanlı: basılan `Drive (Mid→Back)` → tutuluyorsa `Hold (Back'te)`, değilse/bırakınca `Recover (Back→Mid)`; diğeri `Reach (Mid→Front)` → öteki tutuluyorsa `FrontWait (Front'ta)`, değilse `Land (Front→Mid)`; sonra `Ready` (ayaklar altta birleşik, yerde). |
| `RunnerStepController.cs` | **Adım modeli:** her basış = bir adım (`strideDuration=0.18`, `strideSplit`). Basılı tutma: ayak Back'te, diğeri Front'ta bekler (uçak pozu); iki tuş tutulursa yüzüstü; bırakınca ikisi Mid'e. Tutma gücü değiştirmez. Güç = `PushEvaluator(hazırlık)`, hazırlık = min(adım ilerlemesi, tuşun kendi şarjı (2 adımda dolar)). Ritimde sol-sağ = tam güç, aynı tuş art arda = düşük, iki tuş birlikte (`simultaneousWindow`) = tek adım, 2 itiş. Ayaklar birleşikken beklemenin cezası yok. |
| `PushEvaluator.cs` | Güç eğrisi (AnimationCurve, X = hazırlık 0..1, Y = güç), düz çizginin hafif altında (0.5 → ~0.38). |
| `RunnerMovement.cs` | Sadece ileri hareket: `Push(eff)` hız kazancını `pushDuration` boyunca yayar, `drag` ile kayıp, `maxSpeed=11.5`. |
| `RunnerGaitDriver.cs` (~840 satır) | **Sadece görsel.** LegCycle'ları okuyup IK hedeflerini yazar: ayak yörüngesi (Front–Back arası çarpık elips, alt yay zeminde kırpılır), topuk vuruşu, ayak/ayak bileği/parmak profilleri (hızlanma ↔ max hız profili hıza göre blend), çömelik pelvis (`runHeight`) + adım ortasında yükselme (bob), Mid noktası (`midAlong`, zeminde), gövde eğimi (ayakların ne kadar geride olduğuna göre, ayak tutulunca tam), **hold pozları** (tek ayak tutulu = uçak, iki ayak = yüzüstü/dalış), sarkaç kollar (çapraz: sağ ayak → sol kol), omuz/klavikula takibi, diz twist düzeltmesi (`FixLegTwist`), el pozu (`flatHand`). Kollar/ayak bileği/parmaklar `LateUpdate`'te IK'dan sonra pozlanır. |
| `RunnerDebugHUD.cs` | IMGUI ölçüm paneli: hız, mesafe, bacak durumları, son basış sonucu, zamanlama grafiği (mavi=sol, turuncu=sağ). |
| `SideFollowCamera.cs` | Yandan takip kamerası, sadece koşu ekseninde takip (bob kamerayı sallamaz). |

### Tasarım kuralları (kullanıcıyla oturmuş)
- Mantık ve görsel kesin ayrık: GaitDriver **asla** hızı/zamanlamayı değiştirmez.
- Gövde sadece X ekseninde döner (pitch); Y/Z rotasyonu 0.
- Leg IK sadece ayak bileği pozisyonunu koyar (`targetRotationWeight = 0`); ayak açısı IK sonrası
  baldıra göre uygulanır ve bilek sınırlarına clamp edilir.
- Kol IK kapalı; kollar kemik rotasyonlarıyla sarkaç olarak sürülür.
- Kodda yorumlar İngilizce, kısa, "neden"i anlatan `<summary>` blokları; mevcut stile uy.

## Mevcut durum (2026-10-21)

- Commit geçmişi: `fded600 basic running`, ardından iki "push" commit'i (log/layout dosyaları).
- **Commit edilmemiş değişiklikler var** (en son çalışma):
  - `RunnerGaitDriver.cs`: forefoot → **topuk vuruşu (heel-strike)** ayak profiline geçiş
    (`touchdownPitch`, `HeelBehindAnkle`, `HeelRollTime`), yeni ayak profili eğrileri,
    diz twist düzeltmesi (`FixLegTwist`, `restHingeInThigh/Shin`), omuzun geri çekilişi için ayrı
    değerler (`shoulderSwingBack`, `shoulderLiftBack`), el pozu sistemi (`HandRig`, `flatHand`),
    yüzüstü pozda kolların yukarı kalkması kaldırıldı (kollar geri salınımda kalıyor).
  - `TestScene.unity`: sahne ayarları/inspector değerleri.
  - Yeni materyal: `Kevin Iglesias/.../HumanCharacterDummy_ColorPalette.mat`.
- `.gitignore` sadece Library/Temp/Obj/Build içeriyor; `Logs/` ve `UserSettings/` da repoya giriyor
  (istenirse eklenebilir).
- `Scripts/Runner/.idea/` Rider klasörü yanlışlıkla Assets içinde; zararsız.

## Muhtemel sonraki adımlar (henüz yapılmadı)
- Heel-strike animasyonunun oyunda göz ile doğrulanması/ince ayarı.
- Start pozu (çömelme/blok çıkışı — `flatHand` bunun için hazırlandı).
- Bitiş çizgisi / yarış akışı, rakipler, UI (şu an sadece debug HUD).
- Mobil test (Device Simulator).

## Claude için çalışma notları
- Unity editörü açıkken **MCP for Unity** araçlarıyla (`mcp__UnityMCP__*`) sahne/console/play mode
  kontrol edilebilir. Script değişikliğinden sonra `read_console` ile derleme hatası kontrol et.
- Test: Play mode'a gir, ←/→ ile koş, R ile sıfırla; HUD'daki zamanlama grafiğine bak.
- Kullanıcıya Türkçe yanıt ver.
