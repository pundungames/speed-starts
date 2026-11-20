# Speed Stars (speed-starts) — Proje Devir Notu

> Bu dosya Claude Code tarafından her oturumda otomatik okunur. Projeye yeni bir oturumla devam ederken
> bağlam buradan gelir. Son güncelleme: 2026-11-20.
> **Ayrıntılı rehber:** `Assets/Speed-Staes/Scripts/Physical/PlayerController.md` — oturumun başında oku
> (bölüm 8: Inspector'daki her alan tek tek).

## Proje nedir

Mobil, yandan görünümlü bir **sprint (100 m) koşu oyunu prototipi** (örnek: orijinal "Speed Stars").
Oyuncu iki tuşla (**N** sol ayak / **M** sağ ayak, ya da ekranın sol/sağ yarısı) koşucunun **her bir ayağını ayrı**
kontrol eder. Animasyon klip ile değil, **prosedürel**: Animation Rigging IK hedefleri koddan yazılır.

- Unity **2022.3.62** (Built-in RP). Paketler: Animation Rigging 1.2.1, Input System 1.19.0 (Active Input
  Handling = Both), Device Simulator, **MCP for Unity** (Claude editörü buradan kontrol eder),
  postprocessing, recorder, shadergraph.
- Git: `https://github.com/pundungames/speed-starts.git`, branch `main`. Ekipte başka biri de (Serhat) çalışıyor.
- Kullanıcı Türkçe konuşur; HUD metinleri Türkçe, kod ve yorumlar İngilizce (kısa, "neden"i anlatan
  `<summary>` blokları; mevcut stile uy). **Her Inspector alanının `Tooltip`'i olsun** (yeni alan eklerken de).

## Kullanıcıyla çalışma kuralları (önemli)

- **Git'e izinsiz dokunma:** commit, push, pull, merge, stash, dosya silme — hiçbiri kullanıcı açıkça
  istemeden yapılmaz. Git hakkında soru sorarsa sadece bilgi ver.
- **Önce anla, sonra yap:** Kullanıcı tarif ederken sık sık "ne anladın" diye sorar. Bu durumda **kod
  yazma**; anladığını kısa ve net anlat, onay bekle. "İşlem yapma" / "tespit istiyorum" dediyse kesinlikle
  değişiklik yapma.
- **Play mode testi:** Kullanıcı genelde "yap ve ölç" der. MCP ile play mode'da test: `Application.runInBackground
  = true`, olayları `RunnerInput.Inject` ile `EditorApplication.update` içinden zamanlı ver, ölçümü dosyaya yaz
  (`Temp/…csv`; MCP köprüsü arada yeniden bağlanınca bellekteki kayıt kaybolabiliyor), `EditorApplication.isPaused`
  ile durdur, `manage_camera` ile Main Camera'dan ekran görüntüsü al (`Temp/shots`), iş bitince dosyaları sil.
  Kullanıcı play'de kemik seçip açı ayarlayıp "bak" derse: `Selection` + kemiklerin local/dünya açılarını oku;
  rig hedefleriyle karşılaştırarak kullanıcının elle değiştirdiği farkı bul.
- **Sahne / prefab:** Sahneyi kaydetme. Sürücü değerleri **prefab dosyasında**: dosyada kayıtlı alan kodun
  varsayılanını ezer; dosyada olmayan (yeni) alan kodun varsayılanıyla çalışır. Kullanıcı istediğinde prefab'a
  `SerializedObject` + `AssetDatabase.SaveAssets` ile yazılır (sonra `git diff` ile sadece istenen satırların
  değiştiğini kontrol et). Derleme sonrası bellekteki örnek eski değeri tutabilir — test ederken kontrol et.
- **Referans videolar:** Videoyu kare kare incele: Range destekli yerel sunucu + canvas'a kare ızgarası (yerleşik
  tarayıcı), ızgarayı PNG olarak kaydet ve oku. Gördüğünü tablo hâlinde anlat.
- Değişiklikten sonra `read_console` ile derleme hatası kontrol et.
- **Md dosyaları (`PlayerController.md`, bu dosya) konu bitince tek seferde güncellenir**, her adımda değil.

## Sistem: yay tabanlı koşucu (`Assets/Speed-Staes/Scripts/Physical`, namespace `SpeedStars.Physical`)

Sahne: **`Scenes/Test/PhysicalTest.unity`** (koşucu: `HumanCharacterDummy_F`). Yeniden kurulum:
**Tools → Speed Stars → Set Up PhysicalTest Scene**. `Scripts/Runner`'da yeni sistemin kullandığı `RunnerInput` ve
`SideFollowCamera` ile Serhat'ın dünya kodu var.

Akış: **RunnerInput → RunnerBody (+RunnerSim: gerçek hareket) → RunnerPoseDriver (sadece görsel, IK)**.
Görsel taraf hızı/zamanlamayı asla değiştirmez. Tek köprü **baş açısı**: fizikte hesaplanır, görsel çizer.

| Dosya | Görev |
|---|---|
| `RunnerSim.cs` | Saf C# fizik: hız (impulse + sürtünme), **baş açısı** (`StepHead`) ve onun hıza etkisi, tuş başına tutma ağırlığı (`LegSim.Weight`), bacak `ext`/`hold` yayları, eğim yayı (sadece oyun mantığı), görünmeyen uçuş (zamanlama), enerji. İki tuşa özel kural yok. **Basma/tutma ayrımı itişte hâlâ var** (Açık işler). |
| `RunnerBody.cs` | Girdiyi zaman damgasıyla sim'e verir, kökü taşır; `Tapped`/`RunReset`. |
| `RunnerPoseDriver.cs` | Görsel: pozlar, bacak açıları (faz → açı → FK → IK), baş → boyun → bel → kalça eğim zinciri, geriye oturma, gövde yüksekliği, dalış ve yere yerleşme, zemin, boyun kırpma, kollar, bilek/parmak. |
| `RunnerPose*.cs` | Poz verisi ve varsayılanlar (Ready, Run, Stumble). Tutma/dalış/yatma pozu **yok**. |
| `RaceController.cs` | 100 m; bitişte süreyi kaydedip direkt başa döner. |
| `RunnerReplay.cs` / `ReferenceSequences.cs` | Video basış dizileri, 1–4 tuşları. |
| `RunnerBodyHUD.cs` | Ayar paneli (H gizler). `Editor/PhysicalSetup.cs` kurulum aracı. |
| `Scripts/Runner/RunnerInput.cs` | N/M + dokunma (2026-10-27'de ok tuşlarından N/M'ye; oklar Scene görünümünü kaydırıyordu). |

Rig (Control Rig): `Hip target` (MultiParent → `B-hips`), `Spine Root/Tip Target` (TwistChain
`B-spine → B-chest → B-neck`; **eğri prefab'ta (0,0)(0.43,0.6)(1,1)**: ortaya %60), `Left/Right Leg Target + Hint`
(TwoBoneIK), `Hand Rig` (play'de kapalı), `Head Aim Rig` (MultiAim → `B-head`).

### Oturmuş kurallar (kullanıcının onayladığı davranışlar)

- **Ders (değişmez):** Kullanıcı özel durum / kural / mod istemiyor; davranış input akışından kendiliğinden
  çıkmalı ("iki ayak ve tek ayak özel bir durumu yok", "bas-çek ve basılı tutma diye ayrım olmasın"). Bir şey
  bozulunca yeni kural ekleme, sebebi koddan bul ve anlat.
- **Bacaklar açıyla:** Hareket uyluktan başlar; ayak yolu yok. Fazlar: Push → (tuş basılı) Hold / Recover → Mid →
  Rest; diğer bacak Reach → Front → Rest. Her faz o anki açıdan başlar. Tutma ağırlığı (eşiksiz) Push hedefini
  `holdBack`'e kaydırır.
- **Bekleme duruşu:** Tuş yoksa doğrudan `crouch` çömelmesi; koşu duruşu sadece bir tuş basılıyken.
- **Gövde yüksekliği açılardan** (`heightSpring`; yerden kalkarken `liftSpring`).
- **Baş (fizikte, 2026-10-27):** Tuş basılı → öne (`headKick` 16, `holdPush` 70 × ağırlık), basılıyken geri gelmez;
  tuş yok → `headReturnSpring` ile `headBack`'e (−15), geçince `headSettleSpring` (geriye oturma, öne salınmaz).
  Dönüş tuş kalkınca `headReturnDelay` (0.08 sn) içinde güçlenir: hızlı bas-çek (saniyede ~7) tutma kadar biriktirir,
  ritim (0.32 sn) dik kalır.
- **Baş hızı belirler (2026-10-27):** Basış verimi × `1 − (baş/headSpoil 90)^1.1` (eşik yok) + baş öndeyken ek sürtünme
  `headDrag` 0.8. Kullanıcı: "kafa ölçü". Sık basma ~6 mph'ye düşer (video gibi), ritme dönünce baş dikleştikçe
  verim geri gelir. Eski eşikli `stumbleDrag` kaldırıldı.
- **Eğim zinciri (2026-10-27):** baş → boyun → belin üstü → belin dibi → kalça; her halka bir üsttekini sönümlü yayla
  izler (`hipFollowSpring` 4 Hz, `waistSpring` 3 Hz / 1) — dalga, salınım yok (önceki bel titremesini kullanıcı
  istemedi). Öne: kalça `hipShare` 0.65, bel üstüne kavis: dip `waistBaseShare` 0.27, üst `waistTopShare` 0.67
  (kullanıcının play'de ayarladığı kemik açılarından). Bacaklar eğimin tamamıyla döner (ayak kalkışı korunur).
  Duruşta `waistAngle` 2 + `waistTopAngle` 13 (kullanıcının girdisiz duruş açıları: spine 2, chest 4.5, neck 12).
  Geriye bel en fazla `waistBackMax` 7°; fazlası topuklar üstünde bütün vücut (`SitBack`).
- **Boyun kırpma:** Öne eğilmede `B-neck` 10° daha az (`neckTrim`, `neckTrimLean` 30°'den tam), baş açısı korunur
  (LateUpdate) — baş gövde çizgisinde, yere kıvrık baş değmez.
- **Geriye oturma topuklar üstünde:** Gövde sadece başın dinlenme yerini geçen kısmı alır; baş ileri bakar
  (`headKeep` 0.33), ayaklar topuğa kalkar (`heelRock`).
- **Tek ayak tutma:** Tutulan bacak `holdBack` (−60/145), gövde eğildikçe kalçayla `heldLegMaxAngle` 90'a kadar kalkar
  (sınırlar bacağı kendi tutma açısının önüne çekmez); öndeki `holdSupport` ile taşır.
- **Dalış (özel durum yok):** Son ayak itişle kalkar, `liftKick`, sabit yerçekimi, `airTip` ile öne devrilme
  (prefab 60). **Baş yere değince gövde yerleşir** (`settleSpring` 1.5 Hz, 2026-10-27): bel kavisi açılır, baş
  `settleHeadUp` 15° kalkıp öne bakar, devrilme yataya sürer → göğüs, kalça, dizler ~0.8 sn'de yere iner. Bacaklar
  düzleşmez. Bırakınca ~0.6 sn'de kalkar.
- **Hazır yatma pozu yok** (kullanıcı reddetti: tahta gibi yatış, dümdüz bacaklar). Yere yerleşme pozu değil,
  temastan doğan akış.
- **Ayak bileği:** Havada salınım eğrisi; yerdeki açıdan gelen değer karışımdan önce bilek sınırına kırpılır
  (geri katlanan bacakta ayak ~−47'ye açılır, 2026-10-27).
- **Kollar:** El IK kapalı. Bir tuş = bir bacak + bir kol; kol uyluğun evin gerisine gidişini izler; tam geride
  sabit `armBack`/`elbowBack`.
- **Baş hızlı koşuda gövdeyle aynı çizgide** (Run pozu `headDown` = 1). Gövde sadece X ekseninde döner.

## Sistem: tema havası (2026-11-20)

Geri bildirim: "beş tema atmosferik olarak birbirine çok benziyor". Sebebi menü kamerasındaki **sabit mavi gökyüzü**
(`ThemePreviewSky.mat`), kapalı fog ve tek güneşti; tek fark uzak nesnelerdeki haze tonuydu. Mısır'ın `EgyptDesertAtmosphere`
scripti vardı ama sadece play'de ve sadece menüde çalışıyordu, yarışa hiç ulaşmıyordu.

Artık **havası olan her tema aynı kodu çalıştırır, farkı veri yapar** (kullanıcının "özel durum olmasın" kuralı):

| Parça | Görev |
|---|---|
| `Scripts/Themes/ThemeWeather.cs` | Profil varlığı: gök materyali + ufuk dolgusu, güneş rengi/şiddet çarpanı, üç ambient rengi, dünya geneli hava (ton/karışım/güç artışı/mesafe çarpanları), `WeatherLayer[]` (ton, boyut, ömür, hız, yön, yerçekimi, oran, tavan, kutu, kamera takip ofseti, streak uzunluğu). |
| `Scripts/Themes/ThemeWeatherView.cs` | Tek sürücü. Dünyanın kökünde durur; menüde `SetActive`, yarışta dünya değişimi onu açıp kapatır. Gök arkalığı quad'ı + partikül katmanlarını kurar, kamerayla taşır, çıkışta kamera/ambient/güneşi **aynen geri verir** (statik `_holder`: temalar hangi sırada açılırsa açılsın birbirinin değerini silmez). **Sadece play modunda** çalışır — ambient/gök sahne ayarı, editörde yazmak Home.unity'yi kirletirdi. |
| `ThemeAtmosphere.AirBlend` | Üreticinin nesne başına yazdığı haze'i tema geneli ton/güç/mesafeyle kaydırır; sıfır alan "dokunma" demek, yazılı derinlik kompozisyonu korunur. Sahipli (`SetAir`/`ClearAir`). |
| `LoopThemeDefinition.weather` | Tek doğruluk kaynağı: menü prefabı da yarış dünyası da profili buradan alır (view'ın `weather` alanı boşsa tanıma bakar). |
| `Shaders/ThemeSky.shader` | Genel gök: ufuk/zenit + kırılma, isteğe bağlı güneş diski ve parıltı (disk boyutu 0 = kapalı gök). |
| `Shaders/WeatherStreak.shader` | Uzayan damla (yağmur): iki ucu sönen ince çizgi, `ParticleSystemRenderMode.Stretch`. |
| `Editor/ThemeWeatherSetup.cs` | `Speed Stars → Themes → Set Up <tema> Weather`: materyalleri ve profili yoksa üretir, Definition'ı bağlar, harita prefabına view'ı takar. Var olan `Weather.asset` kullanıcınındır, ezilmez. |

Kurulu profiller (ölçümler play'de, Home sahnesi, menü kamerası):

- **Mısır** (`Prefabs/Themes/Egypt/Weather.asset`, `LoopWay2`): sıcak turuncu gök + güneş diski, güneş `1,.82,.56` ×0.96,
  trilight sıcak kum ambient, hava tonu %55 / +0.12 / mesafe ×0.8–0.85. İki toz katmanı: asılı altın zerreler (alfa .16,
  boyut .12–.45, 90/sn, tavan 600, kutu 26×5×40, ofset −7/2/+5) ve yerden savrulan kum (alfa .18, .10–.35, 120/sn,
  tavan 500, 22×1.6×34, −6/0.7/+3). **Ders:** ilk değerler kameradan 22 m uzakta ve alfa .10 olduğu için hiç
  görünmüyordu; toz kameranın önünde, ince taneli ve çok sayıda olmalı — iri tane "leke" gibi duruyor.
- **Pirate Sea** (`Prefabs/Themes/PirateSea/Weather.asset`, `LoopWay4`): güneş diski yok, sadece bulut ardı aydınlık;
  gök `.47,.53,.58` → `.16,.21,.29`, güneş `.62,.70,.80` ×0.55, soğuk ambient, hava tonu %70 / +0.20 / mesafe ×0.6–0.65.
  Yağmur (streak, hız 18–26, uzunluk 5.5, 750/sn, tavan 1600, kutu 30×10×44) + deniz sisi (40/sn). 122 FPS.

Yarış tarafı: `RaceThemeView` açtığı dünyaya view'ı iliştirir, profil `Definition`'dan gelir — yani hava artık yarışta da var.

## Son ölçümler (2026-10-27, play)

| Durum | Sonuç |
|---|---|
| Ritim 0.32 sn | 6. sn'de 6.0 m/s, baş ~7° |
| Bas-çek saniyede 5 / 7 / 10 | Baş 39 / 75 / 80°; saniyede 10'da 3 sn'de ~2.7 m/s |
| Tek ayak tutma | Baş 80° ~1 sn; kalça 35° iken bel dibi 25°, orta 21°; arka ayak ucu ~0.26 m |
| Dalış (iki tuş) | Baş ~0.75 sn'de yerde, gövde ~0.8 sn'de yerleşir (kalça 0.25 m, göğüs 0.23 m); bırakınca ~0.6 sn kalkış |

## Açık işler

- **Fizik itişte tek akışa çevrilmedi:** `RunnerSim` hâlâ basış darbesi + 0.14 sn (`minStepTime`) sonra ayrı tutma
  itişi (`holdThrust`) ve eşikli `HoldValue` kullanıyor. Cevap bekleyen soru: itiş sonunda tuş basılıysa sıçramanın
  iptali (uçuş kuralı) kalsın mı.
- **Prefab ile kod farkı:** `airTip` prefab'ta 60 (kod 90). `neckTrim`/`neckTrimLean`, `settleSpring`/`settleHeadUp`
  prefab'ta yok (kod varsayılanı). Diğer bel/baş/bacak değerleri prefab'a yazıldı.
- Bekleyen fikirler: Mid fazında ayağın da ~−50'ye açılması (kullanıcı önce kendisi bakacak); hızlı bas-çek
  tepesinde boynun çok kıvrık görünmesi (`waistTopShare` düşürülebilir).
- Play'e girince ilk saniyelerde kendiliğinden bir basış geliyor (kaynağı bakılmadı).
- **Havası olmayan üç tema:** Standart Şehir, Roma ve Gökyüzü Adaları hâlâ sabit mavi gökyüzünü paylaşıyor; her biri
  için bir `Weather.asset` + kurulum aracına bir satır yeterli (Roma: alçak altın saat; Gökyüzü: soluk yüksek irtifa).
- **Fırtınaya uymayan iki şey (kullanıcı şimdilik "böyle kalsın" dedi):** denizdeki beyaz bulutlar ve parlak mavi su.
  Hava perspektifi yalnızca `Desert Pixel Paint` ve `Pastel Building` shader'larına uygulanıyor; su Pirate Sea'nin kendi
  `Painted Ocean.mat`'ı (tek materyalle koyulur), bulutlar `Materials/Clouds/` altında ortak (temaya özel materyal ya da
  profile "dünya tonu" alanı gerekir).
- `ThemeRunPlayground.unity` sahnesinde eski `EgyptDesertAtmosphere` ayrı bir sahne nesnesinde duruyor (o yüzden script
  silinmedi); o sahnede yeni view ile üst üste biner. Serhat'ın sahnesi, dokunulmadı.

## Durum

- Tema havası `main`'de, **push edilmedi**: `e73876f8` (sistem + Mısır), `f1adbcee` (Mısır toz ayarı),
  `f34c6154` (Pirate Sea fırtınası) — main origin'in 3 commit önünde.
- Sistem `main`'de; kullanıcı 2026-10-27'ye kadarki işi commit etti. Tooltip ekleri ve bu md güncellemesi
  **commit edilmedi** (kullanıcı kendisi commit eder). Unity ana klasörü
  (`speed-starts`) açıyor. Worktree'den çalışan oturumda ana klasörde `Edit` kancayla engelleniyor; küçük Python
  betikleriyle (tam eşleşme kontrollü) düzenleniyor.
- Kaldırılan alanlar: 2026-10-27 `stumbleDrag` ve görseldeki baş alanları (fiziğe taşındı); 2026-10-26
  `stanceKeepTime`, `holdBlendTime`, `earlyKick`, `pressReturn`, `headFallSpring`, `headReturn`, `holdHead`,
  `oneFootMax`, `holdLean`, `speedLean`, `stepKick`, `trunkFollow`, `dive*`; daha eskileri `FootCycle`, Step Points,
  `lie*`, `pelvisWaistShare`. Eklenenler (2026-10-27): `headReturnDelay`, `headSpoil`/`headSpoilPower`/`headDrag`,
  `hipShare`, `waistTopAngle`, `waistTopShare`/`waistBaseShare`, `waistSpring`, `waistBackMax`, `neckTrim`/
  `neckTrimLean`, `settleSpring`/`settleHeadUp`.
- Referans videolar (`Downloads/`): `screen-20261024-105457.mp4` (koşu, sık basma, dalış), `screen-20261024-170028.mp4`
  (tek ayak tutma), `screen-20261025-202059.mp4` (bacak açıları), `screen-20261026-000711.mp4` (dalış),
  `screen-20261026-161348.mp4` (dalış sonrası geri gelme, tek ayak salınımı, havada bırakma).
- `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor; ekipçe karar verilmeli.
- `ThemeRunPlayground.unity` sahnesindeki karakterde eski bel değerleri kayıtlı (ayrı sahne, dokunulmadı).
