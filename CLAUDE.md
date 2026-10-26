# Speed Stars (speed-starts) — Proje Devir Notu

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Projeye yeni bir oturumla devam ederken
> bağlam buradan gelir. Son güncelleme: 2026-10-26 (gece).
> **Ayrıntılı rehber:** `Assets/Speed-Staes/Scripts/Physical/PlayerController.md` — oturumun başında oku.

## Proje nedir

Mobil, yandan görünümlü bir **sprint (100 m) koşu oyunu prototipi** (örnek: orijinal "Speed Stars").
Oyuncu iki tuşla (←/→ veya ekranın sol/sağ yarısı) koşucunun **her bir ayağını ayrı** kontrol eder.
Animasyon klip ile değil, **prosedürel**: Animation Rigging IK hedefleri koddan yazılır.

- Unity **2022.3.62** (Built-in RP). Paketler: Animation Rigging 1.2.1, Input System 1.19.0 (Active Input
  Handling = Both), Device Simulator, **MCP for Unity** (Claude editörü buradan kontrol eder),
  postprocessing, recorder, shadergraph.
- Git: `https://github.com/pundungames/speed-starts.git`, branch `main`. Ekipte başka biri de (Serhat) çalışıyor.
- Kullanıcı Türkçe konuşur; HUD metinleri Türkçe, kod ve yorumlar İngilizce (kısa, "neden"i anlatan
  `<summary>` blokları; mevcut stile uy).

## Kullanıcıyla çalışma kuralları (önemli)

- **Git'e izinsiz dokunma:** commit, push, pull, merge, stash, dosya silme — hiçbiri kullanıcı açıkça
  istemeden yapılmaz. Git hakkında soru sorarsa sadece bilgi ver.
- **Önce anla, sonra yap:** Kullanıcı tarif ederken sık sık "ne anladın" diye sorar. Bu durumda **kod
  yazma**; anladığını kısa ve net anlat, onay bekle. "İşlem yapma" / "tespit istiyorum" dediyse kesinlikle
  değişiklik yapma.
- **Play mode testi:** Varsayılan olarak kullanıcı kendisi test eder. "Test et / ölç" derse MCP ile play mode'da
  test et: `Application.runInBackground = true` (Unity arka plandayken kare ilerlemez), olayları
  `RunnerInput.Inject` ile `EditorApplication.update` içinden zamanlı ver, ölçümü dosyaya yaz
  (`Temp/…csv`; MCP köprüsü arada yeniden bağlanınca bellekteki kayıt kaybolabiliyor), iş bitince dosyayı sil.
  `EditorApplication.isPaused` ile durdur.
- **Sahne / prefab:** Kullanıcının kaydedilmemiş değişiklikleri olabilir; sahne ya da prefab'ı kaydetme (kullanıcı
  kaydeder). Sürücü değerleri **prefab dosyasında**: dosyada kayıtlı alan kodun varsayılanını ezer; dosyada olmayan
  (yeni) alan kodun varsayılanıyla çalışır. Derleme sonrası bellekteki örnek eski değeri tutabilir — test ederken
  değeri kontrol et, gerekirse play içinde ayarla.
- **Referans videolar:** Kullanıcı orijinal oyundan video/ekran görüntüsü atar. Videoyu kare kare incele: Range
  destekli yerel sunucu + canvas'a kare ızgarası (yerleşik tarayıcı), ızgarayı PNG olarak kaydet ve oku (ekran
  görüntüsü 800×600'e küçülür). Dokunma noktaları tuşların ne zaman basılı olduğunu gösterir. Gördüğünü tablo
  hâlinde anlat.
- Değişiklikten sonra `read_console` ile derleme hatası kontrol et.
- **Md dosyaları (`PlayerController.md`, bu dosya) konu bitince tek seferde güncellenir**, her adımda değil.

## Sistem: yay tabanlı koşucu (`Assets/Speed-Staes/Scripts/Physical`, namespace `SpeedStars.Physical`)

Sahne: **`Scenes/Test/PhysicalTest.unity`** (koşucu: `HumanCharacterDummy_F`). Yeniden kurulum:
**Tools → Speed Stars → Set Up PhysicalTest Scene**. Eski prosedürel sistem ve `TestScene.unity` **silindi**
(2026-10-26); `Scripts/Runner`'da yeni sistemin kullandığı `RunnerInput` ve `SideFollowCamera` ile Serhat'ın dünya kodu kaldı.

Akış: **RunnerInput → RunnerBody (+RunnerSim: fizik) → RunnerPoseDriver (sadece görsel, IK)**.
Görsel taraf hızı/zamanlamayı asla değiştirmez.

| Dosya | Görev |
|---|---|
| `RunnerSim.cs` | Saf C# fizik: hız (impulse + sürtünme), bacak `ext`/`hold` yayları, eğim yayı (sadece oyun mantığı: sendeleme), zamanlama için görünmeyen uçuş, enerji. Basış verimi fiziksel durumdan (havada = erken, geç, bacak hâlâ geride). İki tuşa özel kural **yok**: diğer ayaktan hemen sonra gelen basış (`togetherWindow`) erken sayılmaz; tutma itişi ayak başına. **Basma/tutma ayrımı burada hâlâ var** (bkz. Açık işler). |
| `RunnerBody.cs` | Girdiyi zaman damgasıyla sim'e verir, kökü taşır; `Tapped`/`RunReset` olayları. |
| `RunnerPoseDriver.cs` | Görsel: pozlar (Ready/Run/Stumble), **bacak açıları** (faz → uyluk/diz açısı → FK → IK), baş → kalça eğimi, topuklar üstünde geriye yaslanma, gövde yüksekliği, dalış, zemin, kollar. |
| `RunnerPose*.cs` | Poz verisi ve varsayılanlar (Ready, Run, Stumble). Tutma/dalış/yatma pozu **yok**. |
| `RaceController.cs` | 100 m; bitişte süreyi kaydedip direkt başa döner (R tuşu yok). |
| `RunnerReplay.cs` / `ReferenceSequences.cs` | Videolardan ölçülen basış dizileri, 1–4 tuşları. |
| `RunnerBodyHUD.cs` | Ayar paneli (H gizler). `Editor/PhysicalSetup.cs` kurulum aracı. |

Rig (Control Rig): `Hip target` (MultiParent → `B-hips`), `Spine Root/Tip Target` (TwistChain
`B-spine → B-neck`), `Left/Right Leg Target + Hint` (TwoBoneIK), `Hand Rig` (play'de kapatılır),
`Head Aim Rig` (MultiAim → `B-head`, hedef `Head LookAt Target`). `Step Points` prefab'tan silindi. Kemikler isimle bulunur (`B-shin.L`, `B-toe.R`, `B-upperArm.L` …).

### Oturmuş kurallar (kullanıcının onayladığı davranışlar)

- **Ders (değişmez):** Kullanıcı özel durum / kural / mod istemiyor; davranış input akışından kendiliğinden
  çıkmalı ("iki ayak ve tek ayak özel bir durumu yok", "bas-çek ve basılı tutma diye ayrım olmasın"). Bir şey
  bozulunca yeni kural ekleme, sebebi koddan bul ve anlat.
- **Bacaklar açıyla (2026-10-25, videodan):** Hareket **uyluktan başlar**; alt bacağa hareket yönüne göre açı
  verilir, ayak nereye düşerse adım odur. Ayak yolu / elips / Step Points **yok**. Her bacak: uyluk açısı
  (dikeye göre, + = önde) + diz açısı (180 = düz) → FK → IK (target = bilek, hint = dizin önü). Fazlar:
  basılan bacak **Push** → tuş basılıysa **Hold** (aynı hareketin devamı, blend yok), değilse **Recover** (uyluk
  geride sabit, alt bacak katlanır) → kendiliğinden **Mid** → **Rest**. Diğer bacak aynı anda **Reach** (uyluk
  önde sabit, alt bacak öne açılır, ayak önde yere iner) → **Front** (yerde bekler) → diğeri itmiyor/tutulmuyorsa
  Rest. Her faz **o anki açıdan** başlar.
- **Basma = kısa tutma, tek akış (2026-10-26):** Her tuşun tek bilgisi var: basılı mı. Görselde eşik / mod yok.
  Tuşun **tutma ağırlığı** (`holdIn`/`holdOut`) basıldığı an dolmaya başlar; fizikteki `HoldValue` (0.14 sn
  eşikli) görselde kullanılmaz. `Push` hedefi baştan `Lerp(pushEnd, holdBack, ağırlık)`; ayak açısı da itişten
  tutmaya ağırlıkla geçer.
- **Bekleme duruşu:** Karakter hep **Ready çömelmesine** (`crouch` açıları) yönelir. Koşu duruşu (ev = `runHome`,
  Run pozu) **sadece bir tuş basılıyken**; hiçbir tuş basılı değilken bacaklar doğrudan çömelmeye iner, arada
  dik duruş yok (`stanceKeepTime` kaldırıldı; kullanıcı: "input yoksa bitmeli").
- **Gövde yüksekliği açılardan:** Kalça, en alttaki taşıyan ayak tam zemine değecek yükseklikte (`heightSpring`).
  Açı genişleyince bacak uzar → gövde kalkar; kapanınca iner. Yerden kalkarken (gövde ayakta durma yüksekliğinin
  `liftBelow` 0.25 m'den fazla altındayken) daha yumuşak `liftSpring` (~0.5 sn).
- **Baş (`UpdateHead`, 2026-10-26):** Tek bir baş açısı. **Verime bakılmaz.** Tuş basılıyken baş öne gider:
  ilk anda `headKick` (24°, ~0.1 sn içinde; her basış aynı), basılı kaldıkça `holdPush` (40°/sn × ağırlık); tuş
  basılıyken **geri gelmez**. Hiç tuş yokken iki parçalı yayla döner: dinlenme yerine (`headBack` −15 × koşma
  oranı; çömelmede 0) kadar hızlı (`headReturnSpring` 1 Hz / 0.42), onu geçince yumuşak ve sönümlü
  (`headSettleSpring` 0.4 Hz / 1): savrulduğu kadar geri gider, öne geçmeden yavaşça döner. Baş geri gelmeden
  basmak itişleri biriktirir (sık basma) → baş öne yatar; ritimde dengede, seyrekte geride. Kullanıcının tarifi:
  "basınca öne gider, bırakınca geri gelir; geri gelmeden basarsan daha öne".
- **Kalça başı izler, bel sabit:** `B-spine` X = `waistAngle` (kullanıcı 6 yaptı, prefab'ta), bel hiç bükülmez
  (eğimi bel de paylaşsın seçeneği konuşuldu, kullanıcı **sabit belde** kaldı). **Öne** eğim kalçadan: kalça baş
  açısını baştan sona takip eder (`hipFollow` 1, `hipFollowSpring`, eşik yok — önce baş gider). Kalça açısı = pozun
  `pelvisPitch` + eğim, `pelvisPitchRange` (−10 … 90) içinde. Baş gövdenin en fazla `headLead` önüne geçer (dalışta
  kalça yatayda durunca baş altına katlanmasın). Havadaki ve tutulan bacaklar kalçanın eğimiyle döner, yere basan
  bacak yere göre kalır (`FollowHips`). Tutulan bacak en fazla `heldLegMaxAngle` (79) ve en fazla gövde çizgisine
  kadar (`heldLegArch` −10: kalça açısının 10° altı, dizin hafif bükülmesi farkı kapatır) kalkar — ama bu sınırlar
  bacağı **kendi tutma açısının önüne asla çekmez** (önce çekiyordu: tutmada bacak yerde takılıp yavaş geri gidiyordu).
- **Geriye yaslanma topuklar üstünde (2026-10-26, referans `Downloads/screen-20261026-161348.mp4`):** Gövde
  sadece başın dinlenme yerini **geçen** kısmı alır (koşarken/kayarken sürekli yaslanma yok). Bu kısım bütün
  vücudu yerdeki ayakların **topukları** etrafında döndürür (`SitBack`): kalça geriye ve biraz aşağı, ayaklar topuk
  üstüne kalkar (parmak uçları yukarı, `heelRock` 1 = yaslanma kadar), bacaklar öne açılı. **Baş ileri bakar:**
  yaslanmanın sadece `headKeep` (0.33) kadarını alır, boyun gövdeye göre öne bükülür (çene içeride). Kullanıcı
  onayladı ("sallanma gayet iyi"; topuk ve baş görüntülerden, 2026-10-26).
- **Tek ayak tutma:** Tutulan bacak `holdBack` (−45/159, diz hafif bükük); öndeki bacak `holdSupport`'a geçer ve
  taşır. Basılı kaldıkça baş öne akar, kalça takip eder, gövde ön bacağın üstüne katlanır, arka bacak kalçayla
  kalkar. Bırakınca topuklar üstünde geriye yaslanma.
- **Dalış (iki/tek ayak özel durumu yok):** Son ayak itişle yerden kalkar (tutulurken diğerine basmak da, iki tuş
  birden de aynı). İten bacaklar gövdeyi aşağı çekemez; yerden ayrılırken `liftKick` (1.5 m/s) yukarı hız verir.
  Taşıyan ayak kalmayınca gövde sabit yerçekimiyle uçar, koşu hızıyla ileri gider; **bütün vücut (baş dahil)** baş
  ne kadar öndeyse o hızla öne devrilir (`airTip`), baş yere değene ya da kalça yataya (`pelvisPitchRange.y` 90)
  gelene kadar → dümdüz yatar (videodaki gibi). Havada vücut düz bir çizgi (bacaklar gövde çizgisinde, baş önde
  aşağı); kollar arkada (kullanıcı onayladı). Referans duruş: `screen-20261026-161348.mp4` 13.00 sn.
  **Zemin:** kafa (`headRest`), kalça (`MinPelvis`), taşımayan bacakların dizi (`kneeRest`) ve ayağı zemine girmez,
  gövde ilk değende durur; taşıyan bacağın dizi gövdeyi yaylarla kaldırır. İki tuş basılıyken her şey sınırında
  sabit. Bırakınca bacaklar toparlanır, gövde ~0.5 sn'de kalkar, topuklar üstünde geriye yaslanır.
- **Hazır yatma pozu yok** (kullanıcı reddetti). Çok sık basmada gövde eğilir ama bacaklar fazlarında koşar.
- **Kollar:** El IK kapalı. **Bir tuş = bir bacak + bir kol** (sağ tuş: sağ bacak + sol kol). Kol, kendi tuşunun
  bacağının uyluk açısının sadece evin gerisine gidişini izler; yay/gecikme yok; tam geride sabit
  `armBack`/`elbowBack`.
- **Baş hızlı koşuda gövdeyle aynı çizgide** (Run pozu `headDown` = 1), dururken/yavaşken ileri bakar.
- Gövde sadece X ekseninde döner. (Pelvis/göğüs Y/Z dönüşü ve el IK denendi, kullanıcı geri aldırdı.)

## Son ölçümler (2026-10-26, play; kalça kemiği açısı, çömelmede taban −6°)

| Durum | Sonuç |
|---|---|
| Ritim 0.3 sn | Kalça 2…19°, baş −3…27° |
| Yavaş ritim 0.6 sn | Kalça −10…7° (gövde yaslanmıyor, sadece baş geri gidiyor) |
| Sık basma 0.1 sn | Kalça ~86°, baş ~78° |
| Tek ayak tutma | ~1.5 sn'de kalça 75°+ |
| Bırakınca (tek ayak / sık basma / dalış) | En geri −30 / −41 / −41° (+0.85 sn), ayaklar kalçanın 0.3–0.4 m önünde, +2 sn'de −11…−18°, öne salınım yok |
| Dalış (çömelmeden iki tuş; `airTip` 60) | Gövde +0.2 sn 44°, +0.3 58°, +0.4 65–70°, +0.5 78–80° (video 55–60 / 65 / 70 / 78); bacaklar gövde çizgisinde; ~0.67 sn'de dümdüz yatar (kalça 0.16 m, baş 0.18 m); kalkış ~0.55 sn |
| Tek ayak tutma (düzeltme sonrası) | Arka bacak ~0.2 sn'de tutma açısında (−43°), gövde ~65°'yi geçince kalçayla kalkar |

Videoya göre: tek ayaktan sonra yaslanma az (video ~45°), en geri noktaya geç varıyor (video ~0.4–0.6 sn).
Kullanıcı mevcut hâli "en yakın ve en verimli" kabul etti.

## Açık işler

- **Fizik tek akışa çevrilmedi:** `RunnerSim` hâlâ basış darbesi + 0.14 sn (`minStepTime`) sonra ayrı tutma itişi
  (`holdThrust`, 0.6 sn penceresi) ve eşikli `HoldValue` kullanıyor. Öneri konuşuldu (itiş basılı kaldıkça
  sürer, eşik yok; kısa basışın hızı değişmez; video değerleriyle önce/sonra ölçülür). Kullanıcıdan cevap
  bekleyen soru: itiş sonunda tuş basılıysa sıçramanın iptal olması (uçuş kuralı) kalsın mı, sürekli mi olsun.
- **Prefab ile kod farkı:** Kullanıcı prefab'ı 2026-10-26 gece kaydetti (bütün alanlar dosyada). Konuşulup onaylanan
  `airTip` 90 ve `headLead` 30 **sadece kod varsayılanında**; prefab'ta `airTip` 60, `headLead` 15 kayıtlı ve oyunda
  bunlar geçerli. Kullanıcı Inspector'dan ayarlayacak ya da prefab'a yazılması istenecek.
- Video ile kalan farklar: havada bırakınca ayak üstüne iniş ölçülmedi; bırakınca en geri noktaya geç varıyor.
- Play'e girince ilk saniyelerde kendiliğinden bir basış geliyor (kaynağı bakılmadı).

## Durum

- Sistem `main`'de; kullanıcı 2026-10-26 16:52'de commit + push yaptı (`a5507b8 runner upgrade`), bu oturumun son
  hâlini de push edecek. Unity ana klasörü
  (`speed-starts`) açıyor. Worktree'den çalışan oturumda ana klasörde `Edit` kancayla engelleniyor; kullanıcının
  onayıyla küçük Python betikleriyle (tam eşleşme kontrollü) düzenlendi.
- Kaldırılan alanlar (2026-10-26): `stanceKeepTime`, `holdBlendTime`, `earlyKick`, `pressReturn`, `headFallSpring`,
  `headReturn`, `holdHead`, `oneFootMax`, `waistSpring`, `holdLean`, `speedLean`, `stepKick`, `trunkFollow`,
  `diveTime`, `diveSpeed`, `diveLean`, `diveRecover`, `UpdateDive`/`DiveStage`. Eklenenler: `headSettleSpring`,
  `holdIn`/`holdOut`, `liftSpring`/`liftBelow`/`liftRange`, `liftKick`, `airTip`/`airTipBack`, `headRest`/`kneeRest`,
  `headLead`, `headKeep`, `heelRock`, `heldLegArch`. Daha eskileri: `FootCycle`, Step Points,
  elips/taban alanları, `pelvisClearance`, `lie*`, `pelvisWaistShare`, pozlardaki `trunkPitch`/`armSwing`.
- **Temizlik (2026-10-26):** silinenler: eski prosedürel sistem (`RunnerGaitDriver`(+.md), `RunnerStepController`,
  `LegCycle`, `PushEvaluator`, `RunnerDebugHUD`, `RunnerMovement`), `TestScene.unity`, `Lunge*/Dive` pozları. Serhat'ın
  `LoopWayGenerator`/`LoopWayGeneratorEditor` koşucuyu `RunnerBody` ile buluyor. Dokunulmayanlar: `RunnerContactShadow`,
  `LoopWay*`, `Pastel*`, `CloudVariation`, `AccessoryPhotoStudio`, `UndoRecoveryBackup.unity`, eski worktree klasörleri.
- Referans videolar (`Downloads/`): `screen-20261024-105457.mp4` (koşu, sık basma, dalış), `screen-20261024-170028.mp4`
  (tek ayak tutma), `screen-20261025-202059.mp4` (bacak açıları), `screen-20261026-000711.mp4` (dalış),
  `screen-20261026-161348.mp4` (dalış sonrası geri gelme 1.2–6.0 sn, tek ayak salınımı 6.1–9.0, dalışta havada
  bırakma 9.9–14.5).
- `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor; ekipçe karar verilmeli.
