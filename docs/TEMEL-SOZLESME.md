# TEMEL SÖZLEŞME — Speed Stars (speed-starts)

Tarih: 28.10.2026 · Durum: **taslak v2** (GDD ile birleştirildi, onay bekliyor; v1 koddan geriye dönük çıkarılmıştı)
Kaynaklar, yetki sırasıyla: (1) `docs/gdd/Speed_Stars_V1.0_Turkce.pdf` (**PDF**; ekonomi/UI'da bağlayıcı, hükmü üstündür) ·
(2) `docs/gdd/Speed_Stars_Clone_V1_Unity_IK_Teknik_Tasarim.md` (**MD**; `[TASARIM]` maddeleri öneridir, PDF'ye üstün değildir) ·
(3) `docs/gdd/Speed_Stars_Clone_V1_Developer_Yol_Haritasi_Checklist.html` (**HTML**; JS `phases` = Faz 00–08 görev listesi) ·
(4) kod (`Assets/Speed-Staes/Scripts/**`), `Packages/manifest.json`, `ProjectSettings/*`, Malik'in `CLAUDE.md` + `PlayerController.md`.
**Kapsam KİLİTLİ** (kullanıcı, 28.10): "proje bu olacak, sadece bunları yapacağız". Belgelerde olmayan özellik eklenmez.

> Bu dosya projenin mimari anayasasıdır. GDD eskir, bu dosya **güncellenir**.
> Kod ile burası çeliştiğinde: önce burayı düzelt, sonra kodu.

Etiketler: **VAR** = kodda var (dosya:satır) · **KARAR** = verildi (kim, tarih), kodda henüz yok · **ÖNERİ** = bu belgenin
varsayımı, onay ister · **BİLİNMİYOR**. Atıf: `PDF §1.2`, `MD §6.3`, `HTML Faz 05`. Yollar aksi yazılmadıkça
`Assets/Speed-Staes/` altındadır (klasör adı `Speed-Staes` yazımıyla sabittir). **PDF'nin sayıları bu belgeye yazılmaz** (§6).

---

## 0. Alan sahipliği (projeye özgü)

| Kim | Alan | Yollar | UI/meta oturumları |
|---|---|---|---|
| Malik | **Yalnız** karakterin IK/bacak hareketi ve koşu sistemi: girdi, fizik, poz/IK, yarış akışı (`RaceController`); rakip koşucular (NPC) (KARAR, kullanıcı 28.10) | `Scripts/Physical/**` (`Physical/Editor/PhysicalSetup.cs` dahil), `Physical/Poses/`, `Prefabs/Character/`, `Scenes/Test/PhysicalTest.unity`, `Scripts/Runner/RunnerInput.cs`, `Scripts/Runner/SideFollowCamera.cs` | **Yazmaz**, okur ve bağlanır (`agent-kit.config.psd1` → `KorunanBelgeler`). Koşu sistemine girilmez |
| Serhat | Dünya, tema, harita, kostüm parçaları | `Scripts/Runner/LoopWay*`, `Scripts/Themes/**`, `Prefabs/Themes/`, `Materials/*`, `Prefabs/StreetProps`, `Buildings`, `LoopWay*`, ilgili `Editor/*` | Yazmaz, okur |
| Kullanıcı + biz | Geri kalan her şey (KARAR, kullanıcı 28.10): UI prefab'ları, Home, Event Select, Entry Gate, HUD, Results, ekonomi (Nakit, Mor Mücevher, Adet Bileti, Süreli Bilet), Giriş Hediyesi, 7 Günlük Giriş Ödülü, kozmetik ekranı, High Scores, kayıt, saat servisi, kompozisyon kökü, mekanik adaptörü | `Scripts/Core/**`, `Scripts/Meta/**`, `Scripts/UI/**`, `Scripts/Bridge/**`, `Tests/**`, `UI/**`, `Data/Config/**`, `Scenes/Menu/`, `Scenes/Game/Race.unity`, `Assets/Resources/ProjectContext.prefab` (`Bridge`, `Tests`, `Data/Config` ve Race sahnesi ÖNERİ, §3–6) | Yazar |
| Sona bırakıldı | "Yarış başladı / bitti" olayları: Malik ya da biz (KARAR, kullanıcı 28.10) | seam `IRaceSession` (§4); o gelene kadar `Scripts/Bridge/Dev/` | — |

Listede olmayan, git yazarı Serhat olanlar: `Scripts/Runner/Accessory*`, `Pastel*`, `CloudVariation`, `RunnerContactShadow`.
Yeni yollar (`Bridge`, `Tests`, `Data/Config`, `Scenes/Game/Race.unity`) onaylanınca `docs/CALISMA-KURALLARI.md` §1'e de yazılır (hook
korumalı; kullanıcı). Mekanik ↔ UI kalıcı bağlantısı sonra ayrı bir md arayüz belgesiyle kurulur (KARAR;
`docs/ARAYUZ-MEKANIK.md`, `docs/YOL-HARITASI.md` Faz 7); o gelene kadar geçerli olan §4 seam'i ile §10–11.

HTML fazlarının sahibi: Faz 02–03 ve Faz 04'ün mekanik maddeleri (mesafe/engel/bayrak kuralları, rakipler, replay kaydı,
kamera) **Malik** · Faz 01'in sahne/klasör/test maddeleri, Faz 04'ün katalog ve sonuç ekranı, Faz 05–07 **biz** · Faz 00 ve
08 ortak. MD §13 ağacında `RunnerController`, `OpponentService`, `CameraController`, `ReplayService`, `RaceStateMachine`,
`RaceSimulationClock` Malik'in; `SaveSystem`, `ClockService`, `EconomyService`, `DailyRewardsService`, `RaceEventCatalog`,
`RaceEntryGate`, `RaceResultService`, `RaceRewardResolver`, `UI` bizim.

---

## 1. Dikey dilim (ÖNERİ)

**Tek cümle:** Oyuncu Home'da dört kaynağı görür, Event Select'ten 100m'yi seçer, Entry Gate'te bir Adet Bileti harcayarak
(tek sefer) girer, Race sahnesinde Malik'in koşucusuyla bizim HUD'umuz altında koşar, bitiş (geçici tetikleyici) Results'ı
açar, "Menu" Home'a döndürür; bilet sayısı ve yenilenme sayacı uygulama kapanıp açılınca korunur.

Kabul satırları (hepsi oynanarak doğrulanır; mantık içerenler ayrıca EditMode testli):
- [ ] Uygulama Home ile açılır (Build Settings: `Home`, `Race`; bugün `m_Scenes: []`). Kaynak çubuğu Mor Mücevher, Adet
      Bileti (adet / üst sınır + geri sayım), Süreli Bilet ve Nakit'i kayıttan ve `EconomyConfig`'ten gösterir; üst
      sınırdayken sayaç durmuş görünür. Ekranların hepsi sahnede prefab örneğidir, kodla kurulmaz.
- [ ] Event Select listesi `RaceEventCatalog`'tan satır prefab'ıyla dolar. 100m + Play → Entry Gate. Onay **bir** Adet
      Bileti düşürür; çift tıklama / tekrar onay ikinci bileti düşürmez (makbuz idempotent); bilet yoksa onay pasiftir.
- [ ] Race sahnesinde Malik'in koşucusu (`RunnerBody`, `RaceController`) değiştirilmeden koşar; HUD süre ve mesafeyi
      `IRaceSession`'dan gösterir; `RunnerBodyHUD` görünmez; yarışta tıklanabilir UI yoktur (§9 #3).
- [ ] 100 m geçişi (ya da editörde Dev tuşu) `IRaceSession.Finished`'i yarış başına **bir kez** tetikler; Results süreyi,
      sırayı ve en iyi süreyi gösterir, açıkken basışlar koşucuya gitmez. "Menu" → Home, "Race Again" → yine Entry Gate.
      Yarışı geri tuşuyla terk etmek bileti iade etmez.
- [ ] Uygulama kapatılıp açılınca bilet sayısı, sayacın kalan süresi (gerçek saate göre; kapalıyken de akar) ve en iyi
      süre korunur; kapalıyken dolan aralıklar kadar bilet eklenir, üst sınır aşılmaz (EditMode: sahte `IClock`, değerler
      asset'ten).

Bu dilim **her katmanı deler**: girdi (`RunnerInput`, Malik) → mantık (`RunnerSim`/`RaceController` + `RaceEntryGate`,
`EntryTicketService`) → görsel (koşucu, HUD, DOTween geçişleri) → veri (`ISaveStore`, `EconomyConfig`, `RaceEventCatalog`)
→ UI (Home, Event Select, Entry Gate, HUD, Results). **Malik'in eksik olaylarına bağımlı değildir:** bitiş `Bridge/Dev`
tetikleyicisinden gelir, tek koşucuda sıra 1'dir (§4).
Dilim dışı (sonraki dilimler): Süreli Bilet, Mor Mücevher ödülü, Giriş Hediyesi, 7 gün, kozmetik, High Scores, NPC,
SET/geri sayım, Watch Replay, satın alma.

---

## 2. İsim sözlüğü

### 2a. Oyun ve meta (biz; Türkçe ↔ İngilizce eşlemesi KARAR, kullanıcı 28.10; tip adları ÖNERİ)

| Terim (PDF/MD) | Anlamı | Kod adı | Namespace |
|---|---|---|---|
| Nakit | Orijinaldeki elmasın yerini alan para (PDF §2.2) | `Cash`, `CashWallet` | `SpeedStars.Meta` |
| Mor Mücevher | Ayrı özel para; yalnız tek yarış sırasından kazanılır, premium kozmetiğe harcanır (PDF §1.3) | `PurpleGem`, `PurpleGemWallet` | `Meta` |
| Giriş Bileti | Süreli + Adet biletinin ortak adı; kodda ortak tip yok, kararı `RaceEntryGate` verir (PDF §1) | — | — |
| Adet Bileti | Bir giriş = bir bilet; üst sınırlı, zamanla ve günlük yenilenir (PDF §1.2) | `EntryTicket`, `EntryTicketService` | `Meta` |
| Yenilenme sayacı / "Adet Bileti Dolu" | Sonraki +1'in zamanı; üst sınırda durur | `EntryRegenDueUtc`, `IsEntryTicketFull` | `Meta` |
| Süreli Bilet / Süre Aktif | Kullanınca gerçek saatle süreli sınırsız giriş; aktifken ikincisi kullanılmaz (PDF §1.1) | `TimedTicket`, `TimedTicketService`, `TimedAccessExpiresUtc`, `IsTimedAccessActive` | `Meta` |
| Etkinlik (yarış) | 100m, 200m, 400m, 60mH, 110mH, 400mH, 4×100m, Medley, Antrenman Alanı, Race of the Day (PDF §1.3, MD §12) | `RaceEventDefinition` (SO), `RaceEventCatalog` (SO), `RaceEventId` | `Meta` (kimlik `Core`) |
| Ücretli etkinlik | Orijinalde ücretliyse biletli, ücretsiz olan ücretsiz kalır (PDF §1) | `RaceEventDefinition.RequiresTicket` | `Meta` |
| Giriş kapısı | Tek merkezî kontrol: ücretsiz / Süre Aktif / Adet Bileti / bilet al (MD §6.4, §13) | `RaceEntryGate`; ekran `EntryGateModal` | `Meta` / `UI` |
| Giriş onayı, makbuz | Bileti tek sefer düşüren olay; aynı makbuz ikinci kez düşürmez (MD §3.1) | `RaceEntryConfirmed`, `EntryReceipt` | `Meta` |
| Yarış oturumu | Mekanikle tek bağlantı: başlat isteği + bitti(süre, sıra) (§4) | `IRaceSession`, `RaceStartRequest`, `RaceOutcome` | `SpeedStars.Core` |
| Sıra | Tek yarışın sonucu, 1 = birinci; günlük/haftalık sıralama değil (PDF §1.3) | `RaceOutcome.Rank` | `Core` |
| Ödül çözücü | Sonuçtan Mor Mücevher; işlem kimliğiyle tek sefer (MD §7.3) | `RaceRewardResolver`, `PayoutTransactionId` | `Meta` |
| Giriş Hediyesi | Günün ilk girişinde Adet Bileti penceresi + kaynak çubuğuna uçan kart (PDF §2.1) | `DailyEntryGift`; `DailyGiftPopup`, `RewardFlight` | `Meta` / `UI` |
| 7 Günlük Giriş Ödülü | Gün gün ödül paneli; Home'da ikon + kırmızı nokta (PDF §2.4) | `SevenDayCheckIn`, `SevenDayRewardConfig`; `SevenDayCheckInPanel`, `RedDot` | `Meta` / `UI` |
| Kaynak çubuğu | Home'daki dört kaynak göstergesi (PDF §2.2) | `ResourceBar` | `SpeedStars.UI` |
| Kozmetik, premium kozmetik | Kategori başına premium varyant, listede en üstte parlak panelle, Mor Mücevherle (PDF §2.3) | `CosmeticDefinition` (`Category`, `IsPremium`), `CosmeticCatalog`; `CosmeticsScreen` | `Meta` / `UI` |
| Kozmetik uygulayıcı | Seçili parçaları ve rengi karaktere giydirir, Malik'in prefab dosyasına dokunmaz | `ICosmeticApplier` (seam), `CosmeticApplier` | `Core` / `SpeedStars.Bridge` |
| High Scores, en iyi süre | Liderlik tablosu (Pundun'un Sticker Patch projesinden uyarlanacak, ayrı belge) + etkinlik başına yerel en iyi süre | `ILeaderboard` (seam), `BestTimesByEvent`; `HighScoresScreen` | `Core` / `Meta` / `UI` |
| Oyuncu ilerlemesi, kayıt | Kalıcı durumun tamamı (MD §14) | `PlayerProgress`; `ISaveStore` → `SaveService` | `Meta` / `Core` |
| Saat, gün anahtarı | Güvenilir UTC şimdi; günlük ödüllerin gün sınırı | `IClock.UtcNow` → `ClockService`; `DayKey` | `Core` / `Meta` |
| Ekranlar | Ana ekran, etkinlik seçimi, yarış HUD'u, sonuç | `HomeScreen`, `EventSelectScreen`, `RaceHud`, `ResultsScreen` | `UI` |

### 2b. Mekanik (VAR; Malik/Serhat'ın, biz değiştirmeyiz)

| Terim | Kod adı (dosya:satır) | Namespace |
|---|---|---|
| Koşucu / Sim / Ayar | `RunnerBody` (RunnerBody.cs:12) / `RunnerSim` (RunnerSim.cs:200) / `RunnerTuning` (RunnerSim.cs:20) | `SpeedStars.Physical` |
| Bacak (0 sol, 1 sağ), basış / bırakma | `TapEvent`, `TapEvent.Leg` (RunnerInput.cs:11, 14), `RunnerSim.Legs` (RunnerSim.cs:207) | `Runner` / `Physical` |
| Tutma (ayrı mod değil, uzun basış) | `LegSim.Holding` (RunnerSim.cs:168), `WeightValue` (:176) | `Physical` |
| Basış sonucu, verim | `PressResult` (RunnerSim.cs:5), `LastEfficiency` (:219) | `Physical` |
| Hız, mesafe, enerji | `Speed`, `Distance`, `Energy` (RunnerSim.cs:211–214) | `Physical` |
| Baş açısı, eğim, havada; dalış (yalnız görsel, API yok) | `HeadAngle` (:223), `Lean` (:221), `Airborne` (:230); `RunnerPoseDriver` (RunnerPoseDriver.cs:56) | `Physical` |
| Poz (Ready / Run / Stumble) | `RunnerPose` (RunnerPose.cs:71) | `Physical` |
| Yarış, yarış süresi, bitiş çizgisi | `RaceController` (RaceController.cs:10), `RaceTime` / `LastRaceTime` (:23, 25), `finishLine` (:19) | `Physical` |
| Tema, yol döngüsü | `LoopThemeDefinition` (LoopThemeDefinition.cs:4–6), `LoopWayGenerator` (LoopWayGenerator.cs:22), `ThemedLoopWay` (ThemedLoopWay.cs:13) | `Themes` / `Runner` |
| Kostüm seti | `AccessoryPhotoStudio.selectedSet` (AccessoryPhotoStudio.cs:18): 0 temel, 1–7 standart, 8–9 premium (kategori başına iki `P…` parçası, :82–83) | `SpeedStars.Runner` |
| Tekrar dizisi (debug, 1–4 tuşları) | `RunnerReplay`, `ReferenceSequences` | `Physical` |

Yazımı sabittir. Yeniden adlandırma bir ADR gerektirir. `SpeedStars.Runner` hem girdiyi (`RunnerInput`) hem dünyayı
(`LoopWay*`, `Pastel*`) içerir; yarış akışı `Physical`'dadır.
MD'nin mekanik önerileri uygulanmaz, karşılıkları VAR: §3.2 `RunnerInputEvent` → `TapEvent`; §13 `TouchInputReader` /
`TimingEvaluator` / `StepPhaseController` / `RunnerIKRig` → `RunnerInput` / `RunnerSim` / `RunnerPoseDriver`. **Ad çakışması:**
MD §13.1'deki `RunnerTuning` ScriptableObject'i mekanikteki `RunnerTuning` sınıfıyla çakışır (sayıları prefab'ta); bizim kod
bu adı kullanmaz.

---

## 3. Katman / assembly haritası

**Teknik zemin (VAR):** Unity 2022.3.62f2 (`ProjectVersion.txt:1`), Built-in RP. Paketler (`manifest.json`):
Animation Rigging 1.2.1, Input System 1.19.0 (Active Input Handling = Both, `activeInputHandler: 2`),
postprocessing 3.4.0, recorder 4.0.3, shadergraph 14.0.12, device simulator, MCP for Unity (git `#main`).

**VAR (2026-10-28, commit `875aab8`):** uGUI 1.0.0, TextMeshPro 3.0.7, Test Framework 1.1.33 (`manifest.json`);
`Assets/Plugins/Zenject/` (Extenject 9.2.0: çekirdek, Signals, TestFramework, MemoryPoolMonitor) ve
`Assets/Plugins/Demigiant/DOTween/` (1.3.045). Derleme temiz. DOTween "Setup" yapıldı: `Assets/Resources/DOTweenSettings.asset`
var (commit'siz, 28.10), ASMDEF oluşturulmadı. TMP Essentials ilk TMP nesnesinde içe aktarılır. Kullanan kod henüz yok.

**Bugün (VAR):** Hiç asmdef yok (`Assets/Speed-Staes` altında 0 `.asmdef`). Oyun kodunun hepsi `Assembly-CSharp`,
editör kodu `Assembly-CSharp-Editor`. Namespace bağımlılıkları:

```
SpeedStars.Physical ⇄ SpeedStars.Runner   DÖNGÜ: RunnerBody.cs:1 → RunnerInput; LoopWayGenerator.cs:51 → RunnerBody
SpeedStars.Themes   → Physical, Runner     ThemedLoopWay.cs:4–5, ThemeRunPlayground.cs:2–3
```
Physical ile Runner döngü yüzünden ayrı asmdef'lere bölünemez; ikisi de bizim alanımız değil.

**Hedef (ÖNERİ, §9 #2):**
```
3. parti: Zenject (zenject.asmdef) · DOTween.dll (+ DOTween.Modules asmdef, §9 #4) · Unity.TextMeshPro · UnityEngine.UI

SpeedStars.Core        asmdef  Scripts/Core/Runtime/  → (yok)     seam arayüzleri + ortak tipler: IRaceSession, RaceStartRequest,
                                                                  RaceOutcome, RaceEventId, IClock, ISaveStore, ILeaderboard
SpeedStars.Meta        asmdef  Scripts/Meta/          → Core      ekonomi, biletler, giriş kapısı, ödül, günlük/7 gün, kozmetik
                                                                  sahipliği, en iyi süre, PlayerProgress, config SO tipleri
SpeedStars.UI          asmdef  Scripts/UI/            → Core, Meta, Zenject, TMP, uGUI, DOTween   view'lar, SceneFlow, UiMotionConfig
SpeedStars.Meta.Tests  asmdef  Tests/EditMode/        → Meta, Core (EditMode, yalnız Editor)
Assembly-CSharp        asmdef'siz: Physical + Runner + Themes (Malik/Serhat)
                       + Scripts/Bridge/ (mekanik adaptörü + Dev/)    → Core, Meta, mekanik
                       + Scripts/Core/Installers/ (kompozisyon kökü)  → hepsi
```

- **Meta mekaniğe dokunmaz → asmdef'li ve test edilebilir.** Saf C# servisler + config SO tipleri; MonoBehaviour ve sahne
  yok. Zenject'e de referans vermez (yapıcı enjeksiyonu, öznitelik yok; bağlama Installers'ta). Ekonomi, kayıt ve saat
  mantığı Unity sahnesi olmadan sahte `IClock` + bellek içi `ISaveStore` ile test edilir.
- **Mekaniği okuyan her şey `Bridge`'dedir ve asmdef'siz olmak zorundadır:** asmdef'li bir assembly `Assembly-CSharp`'a
  referans veremez (TUZAKLAR §12). Bridge ince kalır: yalnız çeviri, karar yok. Testi gerekirse yerleşimi ilk test işinde
  denenir.
- Core asmdef'i `Scripts/Core/` köküne **konmaz**, yoksa Installers'ı da içine alır; `Scripts/Core/Runtime/`'a konur.
  Installers'ın kendi asmdef'i olamaz. Namespace: `SpeedStars.Core|Meta|UI|Bridge`, Installers `SpeedStars.Installers`.
- Leaderboard uyarlaması kendi SDK'sını getirirse ayrı asmdef olur (→ Core); arayüzü Core'dadır (ayrıntı ayrı belgede).
- DOTween modülleri (`Plugins/Demigiant/DOTween/Modules/*.cs`) asmdef'siz olduğu için `Assembly-CSharp-firstpass`'e
  derlenir; asmdef'li UI uGUI uzantılarını (`DOFade`, `DOAnchorPos` …) bu hâliyle göremez (§9 #4).

Kural: ok yönüne ters referans gerekiyorsa **seam açılır**, referans eklenmez.

---

## 4. Kompozisyon kökü

Dosya: `Assets/Speed-Staes/Scripts/Core/Installers/` (KARAR; klasör henüz yok). Installer adları ÖNERİ.

| Bağlam | Installer | Ne bağlar |
|---|---|---|
| `ProjectContext` (`Assets/Resources/ProjectContext.prefab`, Zenject'in aradığı yol) | `ProjectInstaller` | Oturum boyu: config SO'ları (Inspector referansı), `IClock`, `ISaveStore`, `PlayerProgress`, cüzdanlar, `EntryTicketService`, `TimedTicketService`, `RaceEntryGate` (bekleyen giriş: seçili etkinlik + makbuz), `RaceRewardResolver`, `DailyEntryGift`, `SevenDayCheckIn`, kozmetik, en iyi süre, `ILeaderboard`, `SceneFlow` |
| `Home` sahnesi `SceneContext` | `HomeSceneInstaller` | Home view'ları, satır factory'leri |
| `Race` sahnesi `SceneContext` | `RaceSceneInstaller` | `IRaceSession` → `RaceSessionAdapter` (sahnedeki `RaceController` / `RunnerBody` Inspector referansıyla instance bind), HUD, Results, Dev bitiş tetikleyicisi |

- Alt agent'lar buraya **yazmaz** (okuyabilir).
- Her sistem bir seam açar: `Func<>` provider, arayüz veya `Initialize(...)` parametresi.
- Bağlamayı yalnız supervisor yapar. Task frontmatter'ında `needs:` maddesi: `tip: WIRING`, `not: "<sistem> -> <seam>"`
- **Açık WIRING ile tur kapanmaz** (`scripts/tur-sonu.ps1` çıkış 1).
- Malik/Serhat koduna `[Inject]` **eklenmez**. Onların bileşenleri Installers'ta sahnedeki örnek olarak bağlanır
  (Inspector referansı → instance bind). Olayları adaptörde Core arayüzüne çevrilir.
- Sahneler arası veri yalnız ProjectContext servislerinde taşınır; statik alan ya da `DontDestroyOnLoad` taşıyıcı yok.

### Bizim seam'ler (Core'da tanımlı)

| Seam | Sözleşme | Bugünkü uygulama | Kalıcı |
|---|---|---|---|
| `IRaceSession` (KARAR: biçim kullanıcının, ayrıntı ÖNERİ) | **Başlat isteği** `RequestStart(RaceStartRequest: etkinlik kimliği, mesafe)` → koşucu başta, Ready, girdi açık. **Okunur:** durum (Ready / Running / Finished), yarış süresi, mesafe, hedef mesafe. **Olaylar:** `Started` (bir kez), `Finished(RaceOutcome: etkinlik, süre, mesafe, sıra, koşucu sayısı)` yarış başına **tam bir kez**, finish karesinin değerleriyle. Bilet bilmez (bilet seam'den önce Entry Gate'te düşer). Çıkış = sahne değişimi; iptal üyesi yok | `Bridge/RaceSessionAdapter`: istek → `raceDistance` yazılır + `RaceController.ResetRace()`; okunur durum §10'dan; `Started` = `Current` Ready → Running yoklaması. `Finished` geçici olarak `Bridge/Dev/DevRaceFinish`'ten (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`): `RunReset` anında `LastRaceTime` değiştiyse ya da editör tuşuyla; sıra 1, koşucu sayısı 1. `Finished`'ten sonra `RunReset` işleyicisinde `acceptInput = false` (`RunReset`, RunnerBody.cs:69'daki `acceptInput = true`'dan sonra, :70'te gelir). Release build'de Dev yok → dilim development build'de oynanır | "Bitti" olayını Malik ya da biz yazar (sona bırakıldı); seam değişmez, `Dev/` silinir |
| `IClock` | `UtcNow` | `ClockService`: cihaz UTC'si, geri alınan saat tespiti (§9 #22) | aynı |
| `ISaveStore` | `PlayerProgress` yükle / atomik kaydet; ekonomi işlemleri makbuz/işlem kimliğiyle tekil (MD §14) | ÖNERİ: `SaveService`, `persistentDataPath` altında JSON, geçici dosya + yer değiştirme + yedek; ekonomi için PlayerPrefs yok | aynı |
| `ILeaderboard` | skor gönder / liste al | dilimde yok | V1: Sticker Patch modülü, yerel backend + sahte botlar (ayrı belge); çevrimiçi sonra |
| `ICosmeticApplier` (ÖNERİ) | seçili parçayı ve rengi karaktere giydir | yok | `Bridge/CosmeticApplier`: Serhat'ın `Accessories/<Kategori>` yuvalarını açar/kapar, renk MaterialPropertyBlock ile; prefab dosyası değişmez (`reports/kozmetik-hazirlik-2026-10-28.md`) |

### Mekanik tarafındaki mevcut seam'ler (VAR): Inspector alanı, boşsa yedek arama

| Bileşen | Seam | Alan boşsa |
|---|---|---|
| `RaceController` | `body` (RaceController.cs:15) | `FindObjectOfType<RunnerBody>` (:31) |
| `RunnerBody` | `input` (RunnerBody.cs:15), `acceptInput` (:19) | `GetComponent<RunnerInput>` (:33) |
| `RunnerPoseDriver` | `body` (RunnerPoseDriver.cs:188) | `GetComponent<RunnerBody>` (:444) |
| `RunnerBodyHUD` | `body`, `race`, `replay` (RunnerBodyHUD.cs:13–17) | `FindObjectOfType` (:42–43) |
| `RunnerReplay` | `input`, `race` (RunnerReplay.cs:14–16) | `FindObjectOfType<RaceController>` (:27) |
| `SideFollowCamera` | `target` (SideFollowCamera.cs:11) | yok |
| `ThemedLoopWay` / `LoopWayGenerator` | `runner` (ThemedLoopWay.cs:20 / LoopWayGenerator.cs:30) | `FindObjectOfType<RunnerBody>` (ThemedLoopWay.cs:28 / LoopWayGenerator.cs:51) |

Bu yüzden sahne başına **tek** `RunnerBody` ve tek `RaceController` olmalı; yoksa yedek aramalar rastgele birini alır.
NPC'ler (birden çok `RunnerBody`) gelince Race sahnesindeki bu alanların hepsi Inspector'dan dolu olmalıdır.

Bu yol `scripts/hook-guard-write.ps1` içine de yazıldı mı? Kısmen: `agent-kit.config.psd1` → `KompozisyonKokleri`
bu yolu içeriyor; `scripts/hook-guard-write.ps1` + yerel `.claude/settings.json` ile zorlanıyor (2026-10-28 denendi:
kompozisyon kökü, Malik'in dosyaları, `.prefab` yazımı engellendi). [x]

---

## 5. Sahne ve prefab sözleşmesi

### **UI ASLA RUNTIME'DA KURULMAZ.** View bağlar, üretmez.

**Ekran yönü: yatay (landscape). KARAR (kullanıcı, 28.10)**; PDF'deki ekran görüntüleri de yatay. `ProjectSettings` bugün
AutoRotation (`defaultScreenOrientation: 4`); değişikliği ayrı task.

Bugün projede hiç Canvas/EventSystem yok. Var olan arayüzlerin hepsi debug amaçlı IMGUI (`RunnerBodyHUD.OnGUI`
RunnerBodyHUD.cs:75, `ThemeRunPlayground.OnGUI` ThemeRunPlayground.cs:71, `AccessoryPhotoStudio` / `AccessoryLineup`
`showControls`); oyun UI'ı değildir, oyun sahnelerinde kapalı olur.

| Sahne | Sahibi | Görevi | Build |
|---|---|---|---|
| `Scenes/Menu/Home.unity` (ÖNERİ) | biz | Home; Event Select, Entry Gate, Giriş Hediyesi, 7 gün, Kozmetik ve High Scores aynı sahnede **panel** | 0 |
| `Scenes/Game/Race.unity` (ÖNERİ) | biz (sahne) + Malik (mekanik kökleri) | Kökte PhysicalTest'teki adlarla: koşucu prefab'ı, `Race` (`RaceController`), FinishLine, Track, Main Camera (`SideFollowCamera`). Üstünde bizim HUD, Results paneli, Entry Gate örneği | 1 |
| `Scenes/Game/Run_100m.unity` | Malik? (27.10) | Boş (Main Camera + Directional Light). Dokunulmaz; Race sahnesi bu mu olsun, Malik'e sorulur (§9 #6) | hayır |
| `Scenes/Test/PhysicalTest.unity` | Malik | Mekanik testi: koşucu prefab'ı, `Race` (`raceDistance` 100), FinishLine, Track, Main Camera. Kurulum: Tools → Speed Stars → Set Up PhysicalTest Scene (PhysicalSetup.cs:48) | hayır |
| `Scenes/Test/ThemeRunPlayground.unity` | Serhat | Tema gezintisi + otomatik demo koşu (ThemeRunPlayground.cs:23), IMGUI | hayır |
| `Scenes/Test/SerhatSiri`, `AccessoryLineup`, `AccessoryPhotoStudio`, `UndoRecoveryBackup` | Serhat | Dünya ve kostüm araçları, yedek | hayır |

Gerekçe (ÖNERİ; HTML Faz 01'in "Bootstrap, Home, Race, Results" önerisi yerine iki sahne):
- **Bootstrap sahnesi yok:** Zenject `ProjectContext` ilk `SceneContext`'te sahnesiz kurulur; kayıt ve saat ilk ekrandan
  önce hazırdır. Editörde hangi sahneden Play'e basılırsa basılsın servisler gelir; ayrı Bootstrap "yanlış sahneden
  başlattım" hatası doğurur. (`docs/YOL-HARITASI.md` T-003 Bootstrap öneriyor; bu madde onaylanırsa T-003 düzeltilir.)
- **Results ayrı sahne değil, Race'te panel:** video sonuç panelini yarış sahnesinin üstünde gösteriyor (MD §2.1); Watch
  Replay aynı sahnede oynar; "Race Again" sahne yüklemeden Entry Gate → `RequestStart` ile döner.
- **Home panelleri tek sahnede:** kaynak çubuğu ve kırmızı nokta her panelde aynı kalır, Giriş Hediyesi Home'un üstünde
  açılır, panel geçişi sahne yüklemesi istemez.
- **Tek Race sahnesi bütün etkinlikler için;** mesafe `RaceEventDefinition`'dan gelir (§6). Engel/bayrak pisti ayrı sahne
  gerektirirse Malik/Serhat ile kararlaştırılır.

Hiyerarşi (ÖNERİ). UI nesneleri koddan isimle aranmaz, view'lar `[SerializeField]` ile bağlanır:
```
Home.unity                                        Race.unity
  SceneContext (HomeSceneInstaller)                 SceneContext (RaceSceneInstaller)
  EventSystem                                       EventSystem
  UI Root                                           <Malik'in kökleri: HumanCharacterDummy_F, Race, FinishLine, Track, Main Camera>
    Canvas Screens  HomeScreen, EventSelectScreen,  UI Root
                    CosmeticsScreen, HighScoresScreen  Canvas Hud     RaceHud
    Canvas Popups   EntryGateModal, DailyGiftPopup,    Canvas Popups  ResultsScreen, EntryGateModal
                    SevenDayCheckInPanel
    Canvas Overlay  ResourceBar, RewardFlight
```

Prefab yolları (ÖNERİ, `Assets/Speed-Staes/UI/`):

| Klasör | Prefab'lar |
|---|---|
| `UI/Screens/` | `HomeScreen`, `EventSelectScreen`, `CosmeticsScreen`, `HighScoresScreen`, `RaceHud`, `ResultsScreen` |
| `UI/Popups/` | `EntryGateModal` (Home ve Race'te birer örnek, mantık tek: `RaceEntryGate`), `DailyGiftPopup`, `SevenDayCheckInPanel` |
| `UI/Widgets/` | `ResourceBar` (dört gösterge prefab'ta sabit, satır değil), `RedDot`, `RewardFlight` |
| `UI/Rows/` | `EventRow`, `CosmeticCell`, `SevenDayCell`, `HighScoreRow` |

Liste/dinamik içerik kalıbı: sahnede **satır prefab'ı** + **content parent**. Kod satırı Zenject factory ile `Instantiate`
eder, görünümü tasarımcı belirler. Event Select ← `RaceEventCatalog`; Kozmetik ← `CosmeticCatalog` (premium üstte: sıralama
Meta'da, view yalnız sırayla dizer); 7 gün ← `SevenDayRewardConfig`; High Scores ← `ILeaderboard` / en iyi süreler.
Results butonları (video: Watch Replay, Race Again, High Scores, Menu, Practice) prefab'ta durur; dilimde Race Again ve Menu
çalışır, diğerleri pasif (§11).

Koşucu prefab'ı (`Prefabs/Character/HumanCharacterDummy_F.prefab`) şunları taşır: `RunnerInput`, `RunnerBody`,
`RunnerPoseDriver`, `RunnerReplay`, `RunnerBodyHUD` (ikisi de açık, `visible: 1`). Prefab bir oyun sahnesine
konunca **debug paneli ve 1–4 / Esc kısayolları da gelir** (RunnerReplay.cs:50–51; 1–4 yarışı sıfırlar, :33).
Sahnedeki örnekte kapatılır, prefab'a dokunulmaz (§9 #5). `RaceController` prefab'ta değil, sahnede ayrı `Race`
objesindedir.

Hiyerarşi isim yolları (koddan isimle bulunanlar, **yeniden adlandırılmaz**):
- Karakter: `Accessories/<Headwear|Glasses|Tops|Gloves|Shorts|Leggings|Socks|Shoes>/…`
  (AccessoryPhotoStudio.cs:38, 90, 97)
- Rig hedefleri: `Hip target`, `Spine Root Target`, `Spine Tip Target`, `Left/Right Leg Target`, `Left/Right Leg Hint`,
  `Left/Right Hand Target` (PhysicalSetup.cs:83–92, editör). Kemikler isimle: `B-hips`, `B-spine`, `B-neck`, `B-head`,
  `B-shin.L` … (RunnerPoseDriver)
- Tema: `Irregular Landmark Plots`, `Tapered Floating Rock` (ThemedLoopWay.cs:142, 196)

Prefab'lar nerede (ÖNERİ): `Assets/Resources/` yalnız Zenject `ProjectContext.prefab`, DOTween `DOTweenSettings.asset` (VAR)
ve TMP Essentials (`Assets/TextMesh Pro/Resources`) için. Oyun prefab'ları ve config'ler Inspector referansıyla bağlanır;
proje kodunda `Resources.Load` ve Addressables yok (3. partinin `Ash Assets/Control Rig/Resources` klasörü ayrı).

> Not: pasif (kapalı) nesneler `GameObject.Find` ile bulunamaz. Sahne
> köklerinden `Transform.Find` ile inilir.

---

## 6. Config sahipliği

| Asset | Tuttuğu sayılar | Sahibi |
|---|---|---|
| `Prefabs/Character/HumanCharacterDummy_F.prefab` → `RunnerBody.tuning` | Bütün fizik: hız, itiş, adım, verim, tutma, yaylar, eğim, baş, enerji (`RunnerTuning`, RunnerSim.cs:20–157) | Malik |
| aynı prefab → `RunnerPoseDriver` alanları | Görsel: bacak açıları, yükseklik, dalış, kollar | Malik |
| `Physical/Poses/Ready.asset`, `Run.asset`, `Stumble.asset` (`RunnerPose`) | Anahtar pozlar | Malik |
| Sahne → `Race` objesi → `RaceController.raceDistance` | Yarış uzunluğu. Race sahnesinde adaptör `RaceEventDefinition`'daki mesafeyi yazar, sahnedeki değer geçersiz (ÖNERİ, §9 #8) | Malik (test sahneleri) |
| `Prefabs/Themes/{Egypt,FloatingSky,PirateSea,RomeByzantium}/Definition.asset` (`LoopThemeDefinition`), `Prefabs/Themes/RomeByzantium/RomanComposition.asset` | Tema içerik referansları, `backgroundHeight` | Serhat |
| Kod sabitleri: `ThemedLoopWay.RoadLength`/`SectionLength` … (ThemedLoopWay.cs:22–25), `RunnerSim.SubStep` (RunnerSim.cs:202) | Yol geometrisi, sim adımı | Serhat / Malik |
| `Data/Config/EconomyConfig.asset` (ÖNERİ) | Adet Bileti üst sınırı, yenilenme aralığı, Giriş Hediyesi miktarı; Süreli Bilet süresi; gün sınırı politikası; başlangıç bakiyeleri; bilet satın alma fiyatları | kullanıcı |
| `Data/Config/RaceEventCatalog.asset` + `Data/Config/Events/*.asset` (`RaceEventDefinition`) | Etkinlik kimliği, görünen ad, mesafe, `RequiresTicket`, `AwardsPurpleGems` (MD §13.1), kullanılabilir mi | kullanıcı (etkinlik kuralları Malik) |
| `Data/Config/GemRewardConfig.asset` | Sıraya göre Mor Mücevher miktarı, tek tablo (ödül veren etkinliklerin hepsi aynı tabloyu kullanır, etkinlik başına kopya yok) | kullanıcı |
| `Data/Config/SevenDayRewardConfig.asset` | Gün başına ödül (tür + miktar ya da kozmetik referansı), boş gün, döngü politikası | kullanıcı |
| `Data/Config/CosmeticCatalog.asset` + `CosmeticDefinition` | Kategori, premium mi, fiyat ve para birimi, liste sırası, parça referansı | kullanıcı (parçalar Serhat) |
| `Data/Config/UiMotionConfig.asset` | UI animasyon süreleri ve eğrileri (`agent-kit/05-DOTWEEN-UI-ANIMASYON.md`) | kullanıcı |
| Leaderboard ayarları | ayrı belgede | kullanıcı |

**İlk doldurma kaynağı:** PDF §1.1–1.3 (biletler, Mor Mücevher ödülleri, premium fiyatı) ve §2.4 (7 gün). Gün 1'deki config
işi bu değerleri asset'e taşır; belgeye ya da koda kopyalanmaz. MD §6–§12'deki ve HTML'deki tablolar PDF'nin kopyasıdır;
çelişkide PDF geçerlidir. PDF'de olmayan değer (6. gün, bilet fiyatı, Nakit kaynakları, normal kozmetik fiyatları)
**uydurulmaz**: alan boş kalır, ilgili UI pasiftir (MD §1.5).

**Sahiplik cümlesi:** Tüm denge sayıları bu asset'lerde durur, sahibi kullanıcıdır (mekanik sayılarının sahibi Malik).
Kod varsayılanı yalnız ilk oluşturmada geçerlidir; çalışma zamanı **asset'i okur**. Testler bu sayıları sabit olarak
assert etmez. Belgelerdeki kabul testleri (MD §6.5, HTML Faz 05–06) ilişkiyle yazılır ("üst sınırdayken +1 eklenmez",
"fiyatın bir altında alınamaz"); değer asset'ten okunur.

Bu projeye özgü: mekaniğin sayıları ScriptableObject'te değil, **prefab'taki serileştirilmiş alanlarda** durur.
Prefab'ta kayıtlı değer kod varsayılanını ezer; prefab'ta olmayan yeni alan kod varsayılanıyla çalışır. Bugünkü fark:
`airTip` kodda 90 (RunnerPoseDriver.cs:285), prefab'ta 60; oyunda geçerli olan 60.

Bir sayının iki kaynağı olamaz. Olursa ADR açılır, biri silinir. Risk: etkinlik mesafesi hem katalogda hem sahnedeki
`raceDistance`'ta (§9 #8).

---

## 7. Doğrulama kapısı

Komut: `powershell -File scripts/unity-verify.ps1 -ProjectPath <kopya> [-CompileOnly] [-TestFilter <regex>]`
(`agent-kit.config.psd1` → `DogrulamaScripti`; sözleşme `scripts/verify.ps1.ORNEK`). Batchmode `2022.3.62f2-x86_64`
kurulumunu seçer; ek çıkışlar: 13 = Unity çalıştı ama sonuç güvenilmez (UPM/lisans/ağ), 19 = script iç hatası.
Unity'yi başlatan yol (0/1/2/3/13) henüz gerçek bir koşuyla denenmedi; 11 ve 12 denendi.

| Çıkış | Anlamı | Turu bloklar mı |
|---|---|---|
| 0 | yeşil | — |
| 1–3 | **kırmızı** (gerçek kod/test hatası) | evet |
| 10–19 | **doğrulanamadı** (ortam) | hayır, ama sayılır |

- Doğrulama **merkezîdir**: supervisor tur başına bir kez çevirir.
- Ana klasörde Unity editörü açık durur (MCP), Library kilidi tekildir. Doğrulama kopyalarda çalışır:
  `D:\Pundun\speed-starts-verify-1`, `-2` (config: `KopyaKoku`, `KopyaOnEki`, `KopyaSayisi`).
- Bugün test yok. İlk kapı: derleme (`-CompileOnly`). İlk test assembly'si `SpeedStars.Meta.Tests` (EditMode, §3):
  biletler, giriş kapısı, kayıt, saat ve ödül testleri burada.
- Ardışık 5 "doğrulanamadı" → tur durur, insana sorulur.

---

## 8. GDD'nin yetki süresi

> Bu belgenin yetkisi **ilk oynanabilir dikey dilime** kadardır. O günden sonra
> doğruluk kaynağı: çalışan kod + config asset'lerindeki mevcut değerler.

- GDD = `docs/gdd/` altındaki üç dosya (28.10.2026'da geldi, Gün 0). Yetki sırası: PDF > MD > HTML.
- Yetkinin bittiği tarih: henüz değil. §1'in beş kabul satırı oynanarak geçtiği gün buraya yazılır (hedef: Gün 2 sonu).
- Dilimden sonra PDF'nin sayıları asset'te yaşar, sahibi kullanıcıdır; PDF kuralları (tüketim anı, iade yok, üst sınır,
  Süre Aktif) kodda ve testtedir. **Kapsam kilidi süresizdir:** yetki bitince de belgelerde olmayan özellik eklenmez.
- Bu satır `docs/gdd/00-README.md`'nin en üstüne de yazıldı mı? [ ] (dosya yok; açılması ayrı iş.)
- Koşucu davranışı için Malik'in `CLAUDE.md` ve `PlayerController.md` notları geçerlidir. Dikkat: tuşlar kodda **N/M**
  (RunnerInput.cs:51, 57); `CLAUDE.md`'deki ←/→ ve ThemeRunPlayground yardım metnindeki "SOL / SAG OK" eskimiş.

---

## 9. Belirsizlikler ve varsayımlar

| # | Soru | Durum | Varsayım (cevap gelene kadar) |
|---|---|---|---|
| 1 | Yarış bitti olayı + sıra kimde? Bugün bitişte aynı karede `ResetRace` (RaceController.cs:55–58), olay yok | KARAR: sona bırakıldı (Malik ya da biz) | `IRaceSession` + `Bridge/Dev` geçici bitiş (§4). Results açıldığında koşucu başa dönmüş olur (dilimde kabul) |
| 2 | asmdef düzeni: §3'teki hedef mi, hiç asmdef yok mu? | açık (kullanıcı) | §3 hedefi: Core/Meta/UI + Meta.Tests asmdef'li; Bridge + Installers + mekanik `Assembly-CSharp`'ta |
| 3 | Dokunmatik girdi bütün ekranı dinliyor, UI'a dokunmak da basış sayılır (RunnerInput.cs:76–81). MD §3.2 ve §4.5 ayrılmasını istiyor | açık (Malik) | Yarışta tıklanabilir UI yok; Results/Entry Gate açıkken `acceptInput = false`. Duraklat düğmesi gerekirse Malik'ten filtre seam'i istenir |
| 4 | DOTween modülleri asmdef'siz (`Assembly-CSharp-firstpass`) | varsayımla geçildi | Utility Panel → "Create ASMDEF" (Setup yapıldı, ASMDEF yok); UI `DOTween.Modules`'a referans verir; olmazsa `DOTween.To` (TUZAKLAR §10) |
| 5 | Prefab'taki debug bileşenleri (`RunnerBodyHUD`, `RunnerReplay`) oyun sahnesine gelir | açık (Malik) | Race sahnesindeki örnekte kapatılır (sahne override'ı), prefab'a dokunulmaz; Malik'e bildirilir |
| 6 | Race sahnesi: yeni `Scenes/Game/Race.unity` mi, `Run_100m.unity` mi? Mekanik kökleri kim kurar? | açık (kullanıcı/Malik) | Yeni sahne (biz); mekanik kökler PhysicalTest'teki kurulumla (Malik'in aracı ya da kopya); `Run_100m`'e dokunulmaz |
| 7 | Adet Bileti ne zaman düşer? PDF: "giriş onaylandığında (geri sayım başladığında / yarış resmen başladığında)" | ÖNERİ | Dilimde Entry Gate onayı = giriş onayı, makbuzla idempotent; dilim Malik'e bağımlı kalmaz. SET gelince geri sayım başına taşınıp taşınmayacağı Faz 7'de (YOL-HARITASI) kararlaştırılır; makbuz kalıbı ikisini de taşır |
| 8 | Mesafe hem katalogda hem sahnede (`raceDistance`) | ÖNERİ | Tek kaynak `RaceEventDefinition`; adaptör `RequestStart`'ta `raceDistance`'a yazar (finishLine `Start`'ta konumlandığı için ondan önce, RaceController.cs:29–36); Malik'e bildirilir |
| 9 | Kozmetik: kategori listesi, premium parçalar, karaktere giydirme kimde, Home/Kozmetik vitrinindeki karakter (PDF görüntüleri; MD §10, §18.6) | açık (kullanıcı/Serhat) | Kategoriler karakterdeki 8 `Accessories/…` yuvası (AccessoryPhotoStudio.cs:38), her birinde 7 normal + 2 premium set (:82–83; `reports/kozmetik-hazirlik-2026-10-28.md`), PDF'nin "kategori başına iki"siyle uyumlu. Giydirme `ICosmeticApplier` (Bridge); vitrin karakteri `RunnerBody`'siz görsel kopya. Normal kozmetik fiyatı yok → Nakit ile satın alma pasif |
| 10 | Duraklatma ve arka plana geçiş: sim `Time.timeAsDouble` ile ilerliyor (RunnerBody.cs:42), `Time.timeScale = 0` test edilmedi | açık | Dilimde duraklatma yok. Arka planda bilet sayaçları gerçek saatten türetilir; süre yarış ortasında biterse yarış kesilmez (PDF §1.1) |
| 11 | Play'e girince kendiliğinden bir basış geliyor (CLAUDE.md "Açık işler") | açık (Malik) | UI bunu düzeltmez, Malik'e bildirilir |
| 12 | Ekran yönü | **KARAR** (kullanıcı, 28.10): yatay | `ProjectSettings` değişikliği ayrı task |
| 13 | `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor | açık (ekip) | 2022.3.62f2 (bugünkü dosya) |
| 14 | 7 günün 6. gün ödülü (PDF'de yok; MD §18.1) | açık (kullanıcı) | Config'te "ödülsüz gün": alınınca takvim ilerler, kaynak verilmez, hücre boş görünür. Uydurulmaz |
| 15 | Gün sınırı / reset saati; 7 gün ardışık mı, kaçırılan gün, 7. günden sonrası, elle mi otomatik mi alınır (MD §8, §11, §18.2) | açık (kullanıcı) | Cihazın yerel takvim günü, gece yarısı; Giriş Hediyesi ve 7 gün aynı `DayKey`'i kullanır. Kaçırılan gün sıfırlamaz, her yeni günde sıradaki gün alınır, 7'den sonra başa döner; alma elle, alınabilirken kırmızı nokta. Politika config alanı |
| 16 | Adet Bileti doluyken Giriş Hediyesi penceresi (MD §6.4, §8) | açık | Pencere açılır, düğme pasif + "Adet Bileti Dolu"; o günün hakkı yanar; uçan kart oynamaz |
| 17 | Orijinalde hangi etkinlikler ücretli (MD §6.1, §18.3; HTML Faz 04) | açık (orijinalden doğrulama) | `RequiresTicket` asset alanı. Dilim için yalnız 100m biletli (bilet akışını delmesi için), diğerleri doğrulanana kadar ücretsiz; kullanıcı asset'ten değiştirir |
| 18 | Bilet fiyatları (Nakit / reklam), Nakit kaynakları, diğer mağaza fiyatları (MD §18.4–5) | açık | Fiyat alanları boş → satın alma seçenekleri görünür ama pasif. Nakit yalnız 7 gün ödülünden gelir; yarış Nakit vermez |
| 19 | Reklam ve uygulama içi satın alma (PDF "reklam izleyerek"; kaynak çubuğundaki "+") kapsamda mı? | açık (kullanıcı) | V1'de reklam/IAP SDK'sı yok; "+" ve reklam seçenekleri pasif yer tutucu |
| 20 | PDF görüntülerinde olup metninde olmayan Home öğeleri: Upgrade, Power/Stamina/Height, Players, Emote, koşucu ön ayarı ("Custom 1"), ayar/ses/manzara düğmeleri | açık (kullanıcı; YOL-HARITASI soru 1–2) | Upgrade (Power/Stamina/Height), Players, Emote kapsam dışı: sistem yok, düğme ya yok ya pasif. Power/Stamina koşu sistemine dokunur; gelirse Malik. "Edit racer" = Kozmetik girişi; "Custom 1" ön ayarı ve ayar/ses düğmeleri YOL-HARITASI Faz 5 / 8'de. High Scores kapsamda (KARAR) |
| 21 | Leaderboard backend, rekor doğrulama, ghost (MD §15, §18.9) | açık (ayrı belge hazırlanıyor) | V1: Sticker Patch modülü `ILeaderboard` arkasında, yerel backend + sahte botlar; çevrimiçi sonra. Dilimde yok |
| 22 | Saat güvenliği: cihaz saatini değiştirme, çevrimdışı, kaçırılan aralıkların telafisi (MD §6.4, §14) | açık | Cihaz UTC'si, sunucu saati yok. Saat son kayıtlı andan geriye giderse sayaçlar ilerlemez (geri sayım uzar, kayıp yok). Kapalıyken dolan yenilenme aralıkları üst sınıra kadar telafi edilir |
| 23 | Rakipler (NPC), sıra, Watch Replay, engel/bayrak/Medley kuralları, Race of the Day içeriği (MD §15, §18.8; HTML Faz 04) | Malik (NPC, koşu sistemi) + doğrulama | Sıra NPC gelene kadar 1; Mor Mücevher ödülü sıra seam'den gerçek gelince açılır. Kuralı doğrulanmamış etkinlik katalogda "kullanılamaz"; Watch Replay pasif |
| 24 | Hedef cihaz, işletim sistemi sürümleri, FPS ve bellek bütçesi (MD §16, §18.11) | açık (kullanıcı) | Android telefon, yatay (iOS BİLİNMİYOR); FPS hedefi MD §16'nın önerisi; asgari sürüm BİLİNMİYOR |
| 25 | Oyuncuya görünen metin dili, yerelleştirme | açık (kullanıcı) | Tek dil; metinler prefab'taki TMP alanlarında; yerelleştirme sistemi yok |
| 26 | Orijinal oyuna özgü ad, ikon ve arayüz öğelerinin kullanım hakkı (MD §1.2, §18.12; HTML Faz 00) | risk (kullanıcı) | UI sanatı yer tutucu; orijinal ikon ve logo kullanılmaz |
| 27 | Sokak Yarışı temasının dekora yansıması (MD §1.3, §18.10) | açık (kullanıcı/Serhat) | Yalnız dekor, Serhat'ın tema sistemiyle; mekanik ve etkinlik listesi değişmez |

Açık olanlar için: `docs/decisions/OPEN-<tarih>-<konu>.md`. GDD'nin ürün kararları (#14–#24) ayrıca
`docs/decisions/OPEN-2026-10-28-gdd-acik-kararlar.md`'de; varsayımlar farklılaşırsa bu belge geçerlidir, OPEN dosyası güncellenir.

---

## 10. Mekanik → UI bağlantı noktaları (VAR)

UI ve meta bunlara **kompozisyon kökündeki adaptör üzerinden** bağlanır (§3: asmdef'li kod bunları doğrudan göremez).
Bugün bu adaptör `Bridge/RaceSessionAdapter`'dır (§4); kalıcı bağlantı ayrı md arayüz belgesiyle gelir (§0).

### Olaylar

| Olay | İmza | Ne zaman | Not |
|---|---|---|---|
| `RunnerBody.Tapped` | `Action<int leg, bool pressed, double simTime>` (RunnerBody.cs:24) | Sim bir basışı/bırakmayı aldı ve bacağın basılı durumu değişti (:55) | Basış olayında `Sim.LastResult` / `LastEfficiency` zaten günceldir (`Press` :53'te önce çalışır), basış geri bildirimi buradan okunur. `acceptInput = false` iken basışlar düşer, olay gelmez (:51); bırakmalar her zaman geçer |
| `RunnerBody.RunReset` | `Action` (RunnerBody.cs:26) | `ResetRun()` (:63–71). Bitişte de (RaceController.cs:58 → :66), tekrar dizisinde (RunnerReplay.cs:33), playground'da R (ThemeRunPlayground.cs:67) | Sim zamanı 0, enerji dolu, `acceptInput` yeniden `true` (:69) |
| `RaceController` | **olay yok** | — | Durum yoklanır |

### Okunabilir durum

| Üye | Tür / birim | Satır |
|---|---|---|
| `RaceController.Current` | `State { Ready, Running }` | RaceController.cs:12, 21 |
| `RaceController.RaceTime` | float s, ilk basıştan; Running'de her kare | :23, 54 |
| `RaceController.LastRaceTime` | float s, son biten yarış; ilk bitişe kadar 0 | :25 |
| `RaceController.raceDistance` | float m (alan) | :17 |
| `RunnerBody.Sim` | `RunnerSim`; `Awake`'te kurulur (öncesinde null), reset'te aynı nesne kalır | RunnerBody.cs:21, 36 |
| `Sim.Speed` | m/s, 0 … `T.maxSpeed` (10.5) | RunnerSim.cs:211, 24 |
| `Sim.Distance` | m | :212 |
| `Sim.Energy` | 0 … `T.energyMax` (100); `T.lowEnergy` (25) altında itiş zayıflar | :214, 146, 154 |
| `Sim.Presses` | int, toplam basış | :215 |
| `Sim.LastResult`, `LastEfficiency`, `LastLeg`, `LastPressTime` | `PressResult`, 0–1, 0/1 (−1 = yok), sim saniyesi | :216–219 |
| `Sim.HeadAngle`, `HeadUpright` | derece; 0–1 (basışın alacağı pay) | :223, 227 |
| `Sim.Lean` | derece | :221 |
| `Sim.Airborne`, `HoldingCount`, `Legs[i].Pressed` / `Holding` | bool, int, bool | :230–231, 161, 168 |
| `Sim.Time` | double s (sim saati, reset'te 0) | :210 |

### Komutlar

| Çağrı | Etkisi | Satır |
|---|---|---|
| `RaceController.ResetRace()` | Koşucuyu başa alır, Ready, `RaceTime` 0 | RaceController.cs:64 |
| `RunnerBody.ResetRun()` | Sim reset, kök başa, girdi kuyruğu temiz, `RunReset` | RunnerBody.cs:63 |
| `RunnerBody.acceptInput = false` | Basışları yok sayar (bırakmalar geçer) | RunnerBody.cs:19, 51 |
| `RunnerInput.enabled = false` | Girdi toplama durur (aksiyonlar bırakılır) | RunnerInput.cs:67 |
| `RunnerInput.Inject(leg, pressed, gameTime)` | Bot/test basışı | RunnerInput.cs:127 |
| `RunnerInput.Clear()` | Kuyruğu boşaltır | RunnerInput.cs:42 |

Kurallar:
- UI `RunnerInput.TryDequeue` çağırmaz; kuyruğun tek tüketicisi `RunnerBody`'dir (RunnerBody.cs:45).
- UI `Sim.Press` / `Release` / `Advance` / `Reset` çağırmaz; sim'e yalnız `RunnerInput` üzerinden girilir.
- Çalışma sırası (`DefaultExecutionOrder`): ThemeRunPlayground −250, RunnerInput −200, RunnerReplay −150, RunnerBody −100,
  RunnerPoseDriver −50, RaceController 0 (öznitelik yok). UI o karenin son değerini görmek için `LateUpdate`'te okur.

---

## 11. UI için mekanikte EKSİK olanlar

| İhtiyaç | Bugün | Geçici yol (adaptör) | Kalıcı çözüm |
|---|---|---|---|
| Yarış başladı olayı | yok; `Current` Ready → Running yoklanır (RaceController.cs:47–51) | durum değişimini yokla → `IRaceSession.Started` | Malik ya da biz (sona bırakıldı) |
| Yarış bitti olayı + süre | yok; bitişte aynı karede sıfırlanır (:55–58) | `Bridge/Dev/DevRaceFinish`: `RunReset` + `LastRaceTime` değişimi, editör tuşu (§4) | Malik ya da biz → `IRaceSession.Finished` |
| Finish anında sonuçların dondurulması (MD §3.5; HTML Faz 02: "bitiş zamanı ve mesafe aynı olayda kesinleşir") | `LastRaceTime` tutuluyor ama koşucu aynı karede başa dönüyor, sonuç açıkken koşucu durmuyor | süre `LastRaceTime`'dan; koşucunun başa dönmesi kabul | Malik ya da biz (sona): bitişte bekleme; `RaceOutcome` finish karesinde |
| SET / geri sayım / erken çıkış (MD §2.1, §3.1; HTML Faz 02: "iki defa race start çalışmaz") | yok; saat ilk basışla başlar (:50) | — (dilim dışı) | Malik ya da biz (sona); `Started` bir kez |
| Sıra (Mor Mücevher tek yarış sırasıyla, PDF §1.3) | yok; tek koşucu | sıra 1 | NPC'ler Malik'in; sıra `RaceOutcome.Rank` ile tek noktadan (sona) |
| Rakip / çok koşucu | yok; sahnede tek `RunnerBody` varsayılır (§4 yedek aramaları) | — | Malik (KARAR, 28.10) |
| Watch Replay (MD §15; HTML Faz 04) | `RunnerReplay` yalnız debug (1–4) | buton pasif | Malik ya da biz (sona) |
| Etkinlik mesafesini dışarıdan verme | `raceDistance` public alan, finishLine'ı `Start`'ta konumlar (RaceController.cs:17, 29–36) | adaptör `RequestStart`'ta yazar (§9 #8) | yeterli; Malik'e bildirilir |
| Practice / Antrenman Alanı (Results butonu) | — | katalogda ücretsiz etkinlik, aynı Race sahnesi | biz (katalog) + Malik (kuralı) |
| Basış sonucu olayı | ayrı olay yok | `Tapped` içinde `Sim.LastResult` | yeterli |
| Enerji bitti / sendeleme / düşme / dalış olayı | yok; dalış yalnız görselde (RunnerPoseDriver iç durumu, private) | — | Malik |
| Duraklatma, arka plan / odak kaybı (MD §3.2, §14) | yok, test edilmedi (§9 #10) | — | BİLİNMİYOR (Malik + biz) |
| UI dokunuşunu girdiden ayırma (MD §3.2, §4.5) | yok (§9 #3) | `acceptInput` / `RunnerInput.enabled` ile kapı | sona: `RunnerInput` Malik'in, filtre seam'i ondan istenir |
| Kozmetiğin karaktere giydirilmesi, premium iz/partikül (PDF §2.3; MD §10) | `AccessoryPhotoStudio` yalnız fotoğraf aracı | — | `CosmeticApplier` (Bridge); efekt asset'leri Serhat (§9 #9) |
| Kalan mesafe / ilerleme | yok ama türetilir: `raceDistance − Sim.Distance` | adaptör | yeterli |
| En iyi süre, kayıt | hiç kayıt yok (PlayerPrefs/dosya kullanımı 0) | — | Meta (biz): `BestTimesByEvent`, `ISaveStore` |
