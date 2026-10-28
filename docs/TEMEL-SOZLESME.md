# TEMEL SÖZLEŞME — Speed Stars (speed-starts)

Tarih: 28.10.2026 · Durum: **taslak** (mevcut koddan geriye dönük çıkarıldı, onay bekliyor)
Kaynak: GDD **yok** (tasarım md'leri sonra gelecek). Bu sürümün kaynağı: kod (`Assets/Speed-Staes/Scripts/**`,
`Assets/Speed-Staes/Editor/**`), `Packages/manifest.json`, `ProjectSettings/*`, `CLAUDE.md` +
`Scripts/Physical/PlayerController.md` (Malik'in koşucu notları), git geçmişi.

> Bu dosya projenin mimari anayasasıdır. GDD eskir, bu dosya **güncellenir**.
> Kod ile burası çeliştiğinde: önce burayı düzelt, sonra kodu.

Etiketler: **VAR** = kodda var (dosya:satır) · **KARAR** = verildi, kodda henüz yok · **ÖNERİ** = bu belgenin
varsayımı, onay ister · **BİLİNMİYOR**. Yollar aksi yazılmadıkça `Assets/Speed-Staes/` altındadır
(klasör adı `Speed-Staes` yazımıyla sabittir).

---

## 0. Alan sahipliği (projeye özgü)

| Kim | Alan | Yollar | UI/meta oturumları |
|---|---|---|---|
| Malik | Koşucu mekaniği: girdi, fizik, poz/IK, yarış akışı | `Scripts/Physical/**` (`Physical/Editor/PhysicalSetup.cs` dahil), `Physical/Poses/`, `Prefabs/Character/`, `Scenes/Test/PhysicalTest.unity`, `Scripts/Runner/RunnerInput.cs`, `Scripts/Runner/SideFollowCamera.cs` | **Yazmaz**, okur ve bağlanır (`agent-kit.config.psd1` → `KorunanBelgeler`) |
| Serhat | Dünya, tema, harita | `Scripts/Runner/LoopWay*`, `Scripts/Themes/**`, `Prefabs/Themes/`, `Materials/*`, `Prefabs/StreetProps`, `Buildings`, `LoopWay*`, ilgili `Editor/*` | Yazmaz, okur |
| Kullanıcı + biz | Ana menü, UI, meta, yeni sahneler, kompozisyon kökü | `Scripts/Core/**` (KARAR); UI/Meta klasör yolları BİLİNMİYOR | Yazar |

Listede olmayan, git yazarı Serhat olanlar: `Scripts/Runner/Accessory*`, `Pastel*`, `CloudVariation`,
`RunnerContactShadow` (bkz. §9 #9).

---

## 1. Dikey dilim (ÖNERİ, GDD gelince yeniden yazılır)

**Tek cümle:** Oyuncu ana menüden "Koş"a basar, yarış sahnesinde 100 m'yi iki tuşla (N/M ya da ekranın sol/sağ
yarısı) koşar, HUD süreyi, hızı ve mesafeyi gösterir, bitişte sonuç ekranı süreyi ve en iyi süreyi gösterir, en iyi
süre kaydedilir, oyuncu "Tekrar" ya da "Menü"ye basar.

Kabul satırları (hepsi oynanarak doğrulanır):
- [ ] Uygulama ana menüyle açılır, "Koş" yarış sahnesini yükler. Bugün Build Settings boş:
      `ProjectSettings/EditorBuildSettings.asset` → `m_Scenes: []`.
- [ ] Yarışta HUD `RaceController.RaceTime`, `RunnerSim.Speed` ve `RunnerSim.Distance` / `raceDistance` gösterir.
      Malik'in IMGUI paneli (`RunnerBodyHUD`) görünmez.
- [ ] HUD'daki bir öğeye dokunmak ayak basışı sayılmaz (§9 #3).
- [ ] 100 m geçilince koşucu, sonuç ekranı kapanana kadar başa dönmez; sonuç ekranı süreyi gösterir. Bugün bitişte
      aynı karede başa dönüyor (§9 #1).
- [ ] En iyi süre kalıcıdır (uygulama yeniden açılınca menüde görünür). "Tekrar" yarışı Ready'ye, "Menü" ana menüye
      döndürür.

Bu dilim **her katmanı deler**: girdi (`RunnerInput`) → mantık (`RunnerSim`, `RaceController`) → görsel
(`RunnerPoseDriver`, tema) → veri (en iyi süre kaydı) → UI (menü, HUD, sonuç).

---

## 2. İsim sözlüğü

| Terim | Anlamı | Kod adı (dosya:satır) | Namespace |
|---|---|---|---|
| Koşucu | Fiziği sahnede taşıyan kök | `RunnerBody` (RunnerBody.cs:12) | `SpeedStars.Physical` |
| Sim | Saf C# fizik durumu | `RunnerSim` (RunnerSim.cs:200) | `SpeedStars.Physical` |
| Ayar (Tuning) | Fiziğin bütün sayıları | `RunnerTuning` (RunnerSim.cs:20) | `SpeedStars.Physical` |
| Bacak | 0 = sol, 1 = sağ | `TapEvent.Leg` (RunnerInput.cs:14), `RunnerSim.Legs` (RunnerSim.cs:207) | `Runner` / `Physical` |
| Basış / bırakma | Tuşun inmesi/kalkması, kendi zaman damgasıyla | `TapEvent` (RunnerInput.cs:11) | `SpeedStars.Runner` |
| Tutma | Basılı kalan tuş; ayrı mod değil, uzun basış | `LegSim.Holding` (RunnerSim.cs:168), `WeightValue` (:176) | `SpeedStars.Physical` |
| Basış sonucu | `Start` / `Good` / `Late` / `Early` (+ `None`) | `PressResult` (RunnerSim.cs:5) | `SpeedStars.Physical` |
| Verim | Basışın aldığı itiş payı, 0–1 | `RunnerSim.LastEfficiency` (RunnerSim.cs:219) | `SpeedStars.Physical` |
| Hız | m/s | `RunnerSim.Speed` (RunnerSim.cs:211) | `SpeedStars.Physical` |
| Mesafe | Başlangıçtan metre | `RunnerSim.Distance` (RunnerSim.cs:212) | `SpeedStars.Physical` |
| Enerji | 0 … `energyMax` (100) | `RunnerSim.Energy` (RunnerSim.cs:214) | `SpeedStars.Physical` |
| Baş açısı | Derece, + = öne; öndeyken basış az verir | `RunnerSim.HeadAngle` (RunnerSim.cs:223) | `SpeedStars.Physical` |
| Eğim | Oyun mantığı eğimi (sendeleme), ekrandaki gövde değil | `RunnerSim.Lean` (RunnerSim.cs:221) | `SpeedStars.Physical` |
| Havada | Adımlar arası sıçrama | `RunnerSim.Airborne` (RunnerSim.cs:230) | `SpeedStars.Physical` |
| Dalış | Son ayak yerden kalkınca öne devrilme. **Yalnız görsel**, API'si yok | `RunnerPoseDriver` (RunnerPoseDriver.cs:56) | `SpeedStars.Physical` |
| Poz | Ready / Run / Stumble anahtar pozu (ScriptableObject) | `RunnerPose` (RunnerPose.cs:71) | `SpeedStars.Physical` |
| Yarış | Ready → Running; bitişte başa döner | `RaceController` (RaceController.cs:10), `State` (:12) | `SpeedStars.Physical` |
| Yarış süresi / son süre | Saniye | `RaceTime` (RaceController.cs:23), `LastRaceTime` (:25) | `SpeedStars.Physical` |
| Bitiş çizgisi | Görsel; play başında `raceDistance` ileriye konur | `RaceController.finishLine` (RaceController.cs:19) | `SpeedStars.Physical` |
| Tema | Egypt, RomeByzantium, PirateSea, FloatingSky | `LoopThemeDefinition`, `EnvironmentConcept` (LoopThemeDefinition.cs:4–6) | `SpeedStars.Themes` |
| Yol döngüsü | Koşucunun önünde akan yol/çevre bölümleri | `LoopWayGenerator` (LoopWayGenerator.cs:22), `ThemedLoopWay` (ThemedLoopWay.cs:13) | `Runner` / `Themes` |
| Kostüm seti | 0 = temel, 1–7 standart, 8–9 premium | `AccessoryPhotoStudio.selectedSet` (AccessoryPhotoStudio.cs:18) | `SpeedStars.Runner` |
| Tekrar dizisi | Videodan ölçülen girdi dizisi (debug, 1–4 tuşları) | `RunnerReplay`, `ReferenceSequences` | `SpeedStars.Physical` |
| En iyi süre | ÖNERİ, yok | — | `SpeedStars.Meta` (ÖNERİ) |
| Ekran | ÖNERİ, yok (menü, HUD, sonuç) | — | `SpeedStars.UI` (ÖNERİ) |

Yazımı sabittir. Yeniden adlandırma bir ADR gerektirir. Malik/Serhat adları onların alanıdır, biz değiştirmeyiz.
Not: `SpeedStars.Runner` hem girdiyi (`RunnerInput`) hem dünyayı (`LoopWay*`, `Pastel*`) içerir; yarış akışı
`Physical`'dadır.

---

## 3. Katman / assembly haritası

**Teknik zemin (VAR):** Unity 2022.3.62f2 (`ProjectVersion.txt:1`), Built-in RP. Paketler (`manifest.json`):
Animation Rigging 1.2.1, Input System 1.19.0 (Active Input Handling = Both, `activeInputHandler: 2`),
postprocessing 3.4.0, recorder 4.0.3, shadergraph 14.0.12, device simulator, MCP for Unity (git `#main`).

**VAR (2026-10-28, commit `875aab8`):** uGUI 1.0.0, TextMeshPro 3.0.7, Test Framework 1.1.33 (`manifest.json`);
`Assets/Plugins/Zenject/` (Extenject 9.2.0: çekirdek, Signals, TestFramework, MemoryPoolMonitor) ve
`Assets/Plugins/Demigiant/DOTween/` (1.3.045). Unity içe aktardı, Editor.log'da derleme temiz ("Tundra build success",
`error CS` yok). DOTween Utility Panel → "Setup DOTween…" henüz yapılmadı (DOTweenSettings yok); TMP Essentials
ilk TMP nesnesinde içe aktarılır. Kullanan kod henüz yok.

**Bugün (VAR):** Hiç asmdef yok (`Assets/Speed-Staes` altında 0 `.asmdef`). Oyun kodunun hepsi `Assembly-CSharp`,
editör kodu `Assembly-CSharp-Editor`. Namespace bağımlılıkları:

```
SpeedStars.Physical ⇄ SpeedStars.Runner   DÖNGÜ: RunnerBody.cs:1 → RunnerInput; LoopWayGenerator.cs:51 → RunnerBody
SpeedStars.Themes   → Physical, Runner     ThemedLoopWay.cs:4–5, ThemeRunPlayground.cs:2–3
```
Physical ile Runner döngü yüzünden ayrı asmdef'lere bölünemez; ikisi de bizim alanımız değil.

**Hedef (ÖNERİ, §9 #2):**
```
Zenject (zenject.asmdef) · DOTween.dll · TextMeshPro · uGUI          ← 3. parti
SpeedStars.Core   asmdef, Scripts/Core/Runtime/   → Zenject           ← oyun kodunun hiçbirine referans vermez
SpeedStars.Meta   asmdef                          → Core              (Physical/Runner/Themes'i göremez)
SpeedStars.UI     asmdef                          → Core, Meta, TMP, uGUI, DOTween
Assembly-CSharp   asmdef'siz: Physical + Runner + Themes (Malik/Serhat)
                  + Scripts/Core/Installers/ (kompozisyon kökü)        → hepsini görür
```

- asmdef'li bir assembly `Assembly-CSharp`'a referans **veremez**. UI/Meta `RaceController`/`RunnerBody`'yi
  doğrudan göremez. Bunun için Core'da bir arayüz (yarış bilgisi + olaylar) olur, adaptörü Installers'ta
  (Assembly-CSharp) yazılıp bağlanır.
- Bu yüzden Core asmdef'i `Scripts/Core/` köküne **konmaz**, yoksa Installers'ı da içine alır. `Scripts/Core/Runtime/`
  gibi bir alt klasöre konur. Installers'ın kendi asmdef'i olamaz.
- DOTween modülleri (`Plugins/Demigiant/DOTween/Modules/*.cs`, ör. `DOTweenModuleUI.cs`) asmdef'siz olduğu için
  `Assembly-CSharp-firstpass`'e derlenir. asmdef'li UI uGUI uzantılarını (`DOFade`, `DOAnchorPos` …) bu hâliyle
  göremez (§9 #4).

Kural: ok yönüne ters referans gerekiyorsa **seam açılır**, referans eklenmez.

---

## 4. Kompozisyon kökü

Dosya: `Assets/Speed-Staes/Scripts/Core/Installers/` (KARAR; klasör henüz yok). Zenject installer'ları:
`ProjectInstaller` (ProjectContext, oturum boyu yaşayanlar) + sahne başına `SceneInstaller` (SceneContext).
İsimler ÖNERİ.

- Alt agent'lar buraya **yazmaz** (okuyabilir).
- Her sistem bir seam açar: `Func<>` provider, arayüz veya `Initialize(...)` parametresi.
- Bağlamayı yalnız supervisor yapar. Task frontmatter'ında `needs:` maddesi: `tip: WIRING`, `not: "<sistem> -> <seam>"`
- **Açık WIRING ile tur kapanmaz** (`scripts/tur-sonu.ps1` çıkış 1).
- Malik/Serhat koduna `[Inject]` **eklenmez**. Onların bileşenleri Installers'ta sahnedeki örnek olarak bağlanır
  (Inspector referansı → instance bind). Olayları adaptörde Core arayüzüne çevrilir.

Mekanik tarafındaki mevcut seam'ler (VAR): Inspector alanı, boşsa yedek arama.

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

Bu yol `scripts/hook-guard-write.ps1` içine de yazıldı mı? Kısmen: `agent-kit.config.psd1` → `KompozisyonKokleri`
bu yolu içeriyor; `scripts/hook-guard-write.ps1` + yerel `.claude/settings.json` ile zorlanıyor (2026-10-28 denendi:
kompozisyon kökü, Malik'in dosyaları, `.prefab` yazımı engellendi). [x]

---

## 5. Sahne ve prefab sözleşmesi

### **UI ASLA RUNTIME'DA KURULMAZ.** View bağlar, üretmez.

Bugün projede hiç Canvas/EventSystem yok (7 sahnede 0 Canvas). Var olan arayüzlerin hepsi debug amaçlı IMGUI:
`RunnerBodyHUD.OnGUI` (RunnerBodyHUD.cs:75), `ThemeRunPlayground.OnGUI` (ThemeRunPlayground.cs:71),
`AccessoryPhotoStudio` / `AccessoryLineup` `showControls`. Bunlar oyun UI'ı değildir, oyun sahnelerinde kapalı olur.

| Sahne | Sahibi | Görevi | Build'de |
|---|---|---|---|
| `Scenes/Game/Run_100m.unity` | Malik (27.10) | Boş sahne: yalnız Main Camera + Directional Light. Görevi BİLİNMİYOR (yarış sahnesi adayı?) | hayır |
| `Scenes/Test/PhysicalTest.unity` | Malik | Mekanik testi: koşucu prefab'ı, `Race` (`RaceController`, `raceDistance` 100), FinishLine, Track, Main Camera (`SideFollowCamera`). Kurulum: Tools → Speed Stars → Set Up PhysicalTest Scene (PhysicalSetup.cs:48) | hayır |
| `Scenes/Test/ThemeRunPlayground.unity` | Serhat | Tema gezintisi: temalar + koşucu + `RaceController` + otomatik demo koşu (ThemeRunPlayground.cs:23), IMGUI | hayır |
| `Scenes/Test/SerhatSiri`, `AccessoryLineup`, `AccessoryPhotoStudio`, `UndoRecoveryBackup` | Serhat | Dünya ve kostüm araçları, yedek | hayır |
| Ana menü | biz | ÖNERİ, yok. Adı BİLİNMİYOR | — |
| Yarış (oyun) | BİLİNMİYOR | ÖNERİ, yok. `Run_100m` mı, yeni sahne mi (§9 #6) | — |
| Boot | — | BİLİNMİYOR: gerekli mi (Zenject ProjectContext sahnesiz de kurulur) | — |

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
- UI: henüz yok. ÖNERİ: canvas'lar sahnede durur, view'lar `[SerializeField]` ile bağlanır. `Transform.Find` yolu
  gerekirse buraya yazılır.

Liste/dinamik içerik kalıbı: sahnede **satır prefab'ı** + **content parent**. Kod `Instantiate` eder, görünümü
tasarımcı belirler (kostüm listesi, tema listesi).

Prefab'lar nerede: bugün sahne/Inspector referansı. Proje kodunda `Resources.Load` ve Addressables **yok**
(`Assets`'teki tek `Resources` klasörü 3. partinin: `Ash Assets/Control Rig/Resources`). Zenject `ProjectContext`
için `Resources/ProjectContext.prefab` ister; TMP Essentials `Assets/TextMesh Pro/Resources`'a kurulur.
**karar:** BİLİNMİYOR. ÖNERİ: Resources yalnız bu ikisi için, oyun prefab'ları Inspector referansıyla, Addressables yok.

> Not: pasif (kapalı) nesneler `GameObject.Find` ile bulunamaz. Sahne
> köklerinden `Transform.Find` ile inilir.

---

## 6. Config sahipliği

| Asset | Tuttuğu sayılar | Sahibi |
|---|---|---|
| `Prefabs/Character/HumanCharacterDummy_F.prefab` → `RunnerBody.tuning` | Bütün fizik: hız, itiş, adım, verim, tutma, yaylar, eğim, baş, enerji (`RunnerTuning`, RunnerSim.cs:20–157) | Malik |
| aynı prefab → `RunnerPoseDriver` alanları | Görsel: bacak açıları, yükseklik, dalış, kollar | Malik |
| `Physical/Poses/Ready.asset`, `Run.asset`, `Stumble.asset` (`RunnerPose`) | Anahtar pozlar | Malik |
| Sahne → `Race` objesi → `RaceController.raceDistance` | Yarış uzunluğu (PhysicalTest'te 100) | Malik, **sahne başına** |
| `Prefabs/Themes/{Egypt,FloatingSky,PirateSea,RomeByzantium}/Definition.asset` (`LoopThemeDefinition`), `Prefabs/Themes/RomeByzantium/RomanComposition.asset` | Tema içerik referansları, `backgroundHeight` | Serhat |
| Kod sabitleri: `ThemedLoopWay.RoadLength`/`SectionLength` … (ThemedLoopWay.cs:22–25), `RunnerSim.SubStep` (RunnerSim.cs:202) | Yol geometrisi, sim adımı | Serhat / Malik |
| UI/Meta config'leri (ödül, fiyat, kilit …) | yok. ÖNERİ: ScriptableObject; yolu BİLİNMİYOR | kullanıcı |

**Sahiplik cümlesi:** Tüm denge sayıları bu asset'lerde durur, sahibi kullanıcıdır (mekanik sayılarının sahibi Malik).
Kod varsayılanı yalnız ilk oluşturmada geçerlidir; çalışma zamanı **asset'i okur**. Testler bu sayıları sabit olarak
assert etmez.

Bu projeye özgü: mekaniğin sayıları ScriptableObject'te değil, **prefab'taki serileştirilmiş alanlarda** durur.
Prefab'ta kayıtlı değer kod varsayılanını ezer; prefab'ta olmayan yeni alan kod varsayılanıyla çalışır. Bugünkü fark:
`airTip` kodda 90 (RunnerPoseDriver.cs:285), prefab'ta 60; oyunda geçerli olan 60.

Bir sayının iki kaynağı olamaz. Olursa ADR açılır, biri silinir. Risk: `raceDistance` sahneye gömülü, her yarış sahnesi
kendi kopyasını taşır (§9 #8).

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
- Bugün test yok: Test Framework kurulu (1.1.33) ama test assembly'si yok. İlk kapı: derleme (`-CompileOnly`).
- Ardışık 5 "doğrulanamadı" → tur durur, insana sorulur.

---

## 8. GDD'nin yetki süresi

> Bu belgenin yetkisi **ilk oynanabilir dikey dilime** kadardır. O günden sonra
> doğruluk kaynağı: çalışan kod + config asset'lerindeki mevcut değerler.

Bu satır `docs/gdd/00-README.md`'nin en üstüne de yazıldı mı? [ ] (GDD henüz yok; gelince `docs/gdd/` altına konur.)

Yetkinin bittiği tarih: henüz değil.

GDD gelene kadar doğruluk kaynağı: çalışan kod + prefab/asset değerleri. Koşucu davranışı için Malik'in `CLAUDE.md`
ve `PlayerController.md` notları. Dikkat: tuşlar kodda **N/M** (RunnerInput.cs:51, 57); `CLAUDE.md`'deki ←/→ ve
ThemeRunPlayground yardım metnindeki "SOL / SAG OK" eskimiş.

---

## 9. Belirsizlikler ve varsayımlar

| # | Soru | Durum | Varsayım (cevap gelene kadar) |
|---|---|---|---|
| 1 | Sonuç ekranı için `RaceController`'a "bitti" durumu ve olayı kim ekler? Bugün bitişte aynı karede `ResetRace` (RaceController.cs:55–58), olay yok | açık (Malik) | Malik `Finished` + olay ekler. O gelene kadar adaptör `RunReset` gelince `LastRaceTime` değiştiyse "bitti" der (kırılgan, koşucu yine başa döner) |
| 2 | asmdef düzeni: §3'teki hedef mi, yoksa hiç asmdef yok mu (katman kuralı yalnız namespace + denetimle korunur)? | açık (kullanıcı) | §3 hedefi: Core/UI/Meta asmdef'li, Installers + mekanik `Assembly-CSharp`'ta |
| 3 | Dokunmatik girdi bütün ekranı dinliyor, UI'a dokunmak da basış sayılır (RunnerInput.cs:76–81, EventSystem kontrolü yok) | açık (Malik) | Yarış sırasında tıklanabilir UI yok; sonuç/menü açıkken `RunnerBody.acceptInput = false` ya da `RunnerInput.enabled = false`. Duraklat düğmesi gerekirse Malik'ten filtre seam'i istenir |
| 4 | DOTween modülleri asmdef'siz (`Assembly-CSharp-firstpass`) | varsayımla geçildi | Kurulumda DOTween Utility Panel → "Create ASMDEF"; UI asmdef'i `DOTween.Modules`'a referans verir |
| 5 | Prefab'taki debug bileşenleri (`RunnerBodyHUD`, `RunnerReplay`) oyun sahnesine gelir | açık (Malik) | Oyun sahnesindeki örnekte kapatılır (sahne override'ı), prefab'a dokunulmaz; Malik'e bildirilir (prefab variant da olabilir) |
| 6 | Oyun sahneleri: `Run_100m.unity` (Malik, boş) yarış sahnesi mi, sahibi kim? Ana menü/Boot sahne adları? | açık (kullanıcı) | Ana menü + yarış olmak üzere iki sahne; yarış sahnesinin mekanik kısmı (koşucu, Race) Malik'in kurulumuyla gelir |
| 7 | Kayıt/ilerleme var mı (en iyi süre, kostüm kilidi, para)? Çevrimiçi, reklam, ödeme? | açık (GDD) | Yalnız en iyi süre, yerel kayıt; yöntem (PlayerPrefs/JSON) BİLİNMİYOR, Meta'nın arayüzü arkasında |
| 8 | `raceDistance` sahneye gömülü (sahne başına kopya) | varsayımla geçildi | Yarış sahnesinde tek `RaceController`, 100 m; tek kaynak o sahne |
| 9 | Kostüm: `AccessoryPhotoStudio` (Serhat'ın fotoğraf aracı; `SelectSet` :87, `NextSet` :179) meta kostüm sistemine temel olacak mı? Sahibi kim? | açık (GDD/Serhat) | İlk dilimde kostüm yok |
| 10 | Duraklatma: sim `Time.timeAsDouble` ile ilerliyor (RunnerBody.cs:42), `Time.timeScale = 0` davranışı test edilmedi | açık | İlk dilimde duraklatma yok |
| 11 | Play'e girince kendiliğinden bir basış geliyor (CLAUDE.md "Açık işler"), yarış kendiliğinden başlayabilir | açık (Malik) | UI bunu düzeltmez, Malik'e bildirilir |
| 12 | Ekran yönü: `defaultScreenOrientation: 4` (AutoRotation, dört yön açık). Oyun yatay mı? | açık (kullanıcı) | Yandan görünüm → yatay (landscape) |
| 13 | `ProjectVersion.txt` ekipte 62f2/62f3 arasında gidip geliyor | açık (ekip) | 2022.3.62f2 (bugünkü dosya) |

Açık olanlar için: `docs/decisions/OPEN-<tarih>-<konu>.md`

---

## 10. Mekanik → UI bağlantı noktaları (VAR)

UI ve meta bunlara **kompozisyon kökündeki adaptör üzerinden** bağlanır (§3: asmdef'li kod bunları doğrudan göremez).

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
| Yarış başladı olayı | yok; `Current` Ready → Running yoklanır (RaceController.cs:47–51) | durum değişimini yokla | Malik |
| Yarış bitti olayı + süre | yok; bitişte aynı karede sıfırlanır (:55–58) | `RunReset` + `LastRaceTime` değişimi (§9 #1) | Malik |
| Bitişte bekleme (sonuç açıkken koşucu durur) | yok, anında başa döner | — | Malik |
| Start işareti / geri sayım / erken çıkış | yok; saat ilk basışla başlar (:50) | — | GDD + Malik |
| Basış sonucu olayı | ayrı olay yok | `Tapped` içinde `Sim.LastResult` | yeterli |
| Enerji bitti / sendeleme / düşme / dalış olayı | yok; dalış yalnız görselde (RunnerPoseDriver iç durumu, private) | — | Malik |
| Duraklatma | yok, test edilmedi (§9 #10) | — | BİLİNMİYOR |
| UI dokunuşunu girdiden ayırma | yok (§9 #3) | `acceptInput` / `RunnerInput.enabled` ile kapı | Malik: filtre seam'i |
| Kalan mesafe / ilerleme | yok ama türetilir: `raceDistance − Sim.Distance` | adaptör | yeterli |
| En iyi süre, kayıt | hiç kayıt yok (PlayerPrefs/dosya kullanımı 0) | — | Meta (biz) |
| Rakip / çok koşucu | yok; sahnede tek `RunnerBody` varsayılır | — | GDD |
