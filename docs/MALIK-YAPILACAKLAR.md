# Malik'e: yarım kalan işler (rakip botlar, akış düzeltmeleri) — 02.11.2026

> Kullanıcı kararı: "Malik yapacak, yarıda bıraktıklarımızı". UI/meta oturumu (Pundun) bu işleri bırakıyor; hepsi senin.
> Bu dosya tek başına yeter; ayrıntı için: `docs/MALIK-DEVIR-rakip-botlar.md` (bot sistemi senin koduna nasıl dokunuyor),
> `docs/DEVIR-2026-10-30-aksam.md` (30.10 akşam notu), `docs/analiz/2026-10-30-referans-oyun-rakipler.md` (referans oyun ölçümleri).

## 0. Nereden başlıyorsun
- **main** (f93d28b9, push'lu) çalışıyor: yarışta 5 bot, HUD'da yer ("1st"), sonuç listesi, yarışta seçili tema, menüde karakter
  döndürme. Kopyada yeşildi (EditMode 111, PlayMode 27 + 2 çekim).
- **Yarım işler dalda:** `agent/T-077-080-duzeltmeler` (77a3c763, 3 commit, 28 dosya). **HİÇ DERLENMEDİ, TEST EDİLMEDİ.**
  Dal uzakta yok: kullanıcı `git push origin agent/T-077-080-duzeltmeler` yapacak. Sen: `git fetch` → `git checkout agent/T-077-080-duzeltmeler`
  → main'in son halini üstüne al (`git merge origin/main`) → aşağıdaki sıra.
- Task dosyaları dalda: `docs/tasks/T-077..T-080-*.md` (her birinde çalışma notu + açık `needs`).

## 1. Kullanıcının oyunda bulduğu sorunlar → durum
| # | Sorun (kullanıcı, 30.10) | Sebep | Dalda yapılan | Kalan |
|---|---|---|---|---|
| 1 | "1. olmadım ama hep 1. gösteriyor" | **Kesin:** oyuncunun süresi `RaceController.LastRaceTime` = ilk basıştan; botların süresi de ilk basıştan → tepkisi yavaş oyuncuya 0.2-0.4 sn avantaj. **Kesin:** en hızlı bot 15.97 sn (sürücü periyot 0.32 / tutma 0.10 sabit), oyuncu 15.6 koşuyor. Varsayım: perspektif (uzak şerit 0.1-0.35 m önde görünür) | T-078: herkes **GO'dan** ölçülür (tepki dahil); oyuncunun bitişi kare içinde ara değerli | Sürücü araması (§2 adım 5) + yeniden kalibrasyon |
| 2 | Stats'a diğer AI'lar gelmeli | `RaceFlowLogic.PresentAnalysis` rakipleri `null` veriyor | T-077: `AnalysisScreenView` `IRaceTraces`'i okur, botları ekler (eğri + bayraklı satır); T-078: `RaceBots : IRaceTraces` (GO'dan mesafe kaydı) | Bağlama + prefab üretimi |
| 3 | Klavye yön tuşlarıyla oynama | Ok tuşlarını sen N/M'ye çevirmiştin (Scene görünümü kayıyordu) | T-078: `Bridge/KeyboardRunnerInput.cs` (sol/sağ ok → `RunnerInput.Inject`, kapı `RunnerBody.acceptInput`); `SceneRaceBots.AddArrowKeys` oyuncuya ekler | Race yeniden kurulumu + deneme |
| 4 | 3-4 Race again'den sonra başlamıyor | Adet Bileti (tavan 3) bitiyor; Race'te `IEntryGatePrompt` hiç uygulanmamış → sadece log (`RaceFlowLogic.cs:242`) | T-079: `UI/Race/RaceFlowEntryGatePrompt.cs` (Home'daki EntryGateModal'ı açar; USE TICKET Race'i yeniden yükler) | Bağlama + Race kurulumu |
| 5 | Practice ve diğer modlar çalışmıyor | `Data/Config/Events/training.asset` `isPlayable 0`, `distance 0`; engelli/bayrak/Race of the Day mekanikte yok | T-079: `EventSelectConfigSeed.EnablePractice` (ücretsiz, 100 m), kilitli modlarda Continue = "COMING SOON" | `ApplyEventAvailability` + kurulum; 200/400 m kararı (§3) |
| 6 | Ana menü geç yükleniyor | Yükleme zaten async; `Home.unity` 5 tema dünyasının hepsini yüklüyor (~160 MB, `Home.unity:4580 themeDisplays`); geçiş örtüsü alfa 0 → donmuş görünür | T-080: yalnız ölçüm (`Core/Runtime/Scenes/SceneLoadProbe.cs`, PlayMode `SceneLoadTimingTests`) | Örtü opak + "Loading" (builder), yalnız seçili dünyayı yükleme (§3) |

## 2. Bitirme sırası
1. **Derle.** Üç agent aynı anda yazdı, hiçbiri derlemedi. İlk iş konsolu temizlemek (özellikle `RaceBots` ↔ `AnalysisScreenView`
   `IRaceTraces`; `RaceSessionAdapter` ↔ `RaceFlowLogic`; `NotifyPlayerFinished` artık `double` döndürüyor).
2. **`RaceSceneInstaller`** (`Scripts/Core/Installers/`; bizim oturumda hook korumalıydı, senin kitinde değil). Bugünkü
   `if (raceBots != null) Container.Bind<IRaceStandings>()...` satırı yerine:
   ```csharp
   [SerializeField] private EntryGateModalView entryGateModal;   // alan
   // InstallSceneBindings içinde:
   if (raceBots != null) Container.Bind(typeof(IRaceStandings), typeof(IRaceTraces)).FromInstance(raceBots).AsSingle();
   if (entryGateModal != null) Container.Bind<IEntryGatePrompt>().To<RaceFlowEntryGatePrompt>().AsSingle().WithArguments(entryGateModal);
   ```
3. **Yeniden kur** (Edit mode, açık sahneni kaydet; Home/Race builder çıktısıdır, elle düzenleme kaybolur). "Faz 1 - all (overwrite)"
   KULLANMA (config asset'lerini ezer). Geçici menü betiği (commit'leme):
   ```csharp
   // Assets/Speed-Staes/Tools/Editor/TurDevir.cs
   using SpeedStars.Tools.Builders; using UnityEditor;
   public static class TurDevir {
       [MenuItem("Tools/Speed Stars/Devir - UI + Home + Race")]
       public static void Run() {
           SceneBuilder.ApplyEventAvailability();   // Practice: ücretsiz, 100 m
           UiPrefabBuilder.BuildAll(true);          // analiz satırı bayrağı, sonuç/HUD prefab'ları
           SceneBuilder.BuildHome(true);
           SceneBuilder.BuildRace(true);            // installer alanları (raceBots, entryGateModal), ok tuşları, botlar
           SceneBuilder.SetBuildSettings();
           AssetDatabase.SaveAssets();
       }
   }
   ```
   Sonra `git diff --stat`: yalnız prefab/sahne/`training.asset` değişmeli. YAML'da `flagImage`, `flags`, `comingSoonLabel`,
   `entryGateModal`, `raceBots` `fileID: 0` OLMAMALI.
4. **Geçiş örtüsü:** `SceneTransitionBlocker` görüntüsü alfa 0 → opak koyu + "Loading" metni; builder'da (örtüyü kuran sahne
   builder'ı, büyük ihtimalle `SceneUiLayout`), sahneye elle değil. İstersen `UnitySceneLoader.Load()` başına
   `#if UNITY_EDITOR || DEVELOPMENT_BUILD SpeedStars.Core.Scenes.SceneLoadProbe.Begin(sceneName); #endif` (ölçüm logu).
5. **Bot sürücü araması (YAZILMADI, T-078 `blocked`):** `BotCalibration`'a `Search`: periyot 0.20-0.40 (0.02 adım) × tutma
   0.06-0.16 (0.02 adım), 10 seed, sınırlı döngü, satır başına log; en hızlı (periyot, tutma) kombinasyonlarını GO'dan ortalama
   100 m süresiyle yaz. En iyisini `BotRaceConfig`'in `periodBest`/`holdBest`'ine koy, `Run -apply` ile tabloyu yeniden ölç,
   **tabloyu `BotRaceConfig.cs` varsayılanına da kopyala** (asset = kod). Dikkat: `RunnerSim.Advance(dt)` dt ≤ 1e-6'yı yok sayar;
   kalibrasyon döngüleri buna karşı korumalı kalsın (bir kez sonsuz döngüye girdi). Menü: `Tools/Speed Stars/Run bot calibration`.
   Fizik/tuning sayılarını değiştirirsen bu tablo kayar → yeniden koş.
6. **Test:** EditMode (`Bot|RaceStandings|RaceTrace|Analysis|EventSelect|RaceFlow`), PlayMode tamamı, `Shots_HomeAndRace`
   (`-shotDir` ile). `Tools/Speed Stars/Validate Zenject (build scenes)`.
7. **Gözle (Play):** yavaş koşunca HUD 2./3., sonuç sırası doğru; Stats'ta bot eğrileri; ok tuşlarıyla koşu, GO'dan önce basış
   koşturmuyor; 3 yarıştan sonra Race again pencereyi açıyor, reklamla bilet → yarış; Practice biletsiz; kilitli modda "COMING SOON".
8. Task dosyalarını kapat (`status`, `commits`, `needs: closed`), main'e birleştir.

## 3. Karar bekleyenler (kullanıcıyla)
- **Kişisel rekor tabanı:** süre artık GO'dan (tepki dahil) → yeni süreler eski PB/liderlik sürelerinden ~0.2-0.6 sn kötü.
  Eski kayıtlar sıfırlansın mı, olduğu gibi mi kalsın? Bot zorluğunun referansı PB.
- **Ana menü hızı:** yalnız seçili tema dünyası yüklensin mi (her dünya ayrı ek sahne, builder üretir; Serhat'ın prefab'ları
  değişmez). Bugün Home her girişte 5 dünyayı yüklüyor.
- **200 / 400 m:** `RaceController.Start` bitiş çizgisini sahnedeki 100 m'ye koyuyor; `RaceSessionAdapter.RequestStart`
  `raceDistance`'ı değiştiriyor ama çizgiyi değil. Açılacaksa çizgi + botlar için mesafeye göre kalibrasyon.
- **Practice 100 m ve ücretsiz** agent'ın seçimi (katalog 0 m'ydi) — onaylat.

## 4. Olası sorunlar (test edilmedi)
- Derleme hatası (§2.1). Bağlama yoksa `[InjectOptional]` sessizce boş kalır: hata yok ama özellik görünmez ("derlendi" ≠ "çalışıyor").
- Builder koşulmadan yeni alanlar boş → view null kontrolü yoksa NullReference.
- Giriş penceresi Race'te hiç denenmedi: Home'daki bağımlılıkları (UiMotionConfig, bilet kaynağı, ProgressRepository) ProjectContext'ten
  bekliyor; onaylanınca Race sahnesini yeniden yüklüyor.
- Ok tuşları: Editor'de Scene görünümü odaktaysa sahneyi kaydırır (senin kaldırma sebebin) — Game görünümü odakta olmalı.
- Botların takvimi açık döngü (koşucunun hızına/başına bakmaz); config tablosu eski (ilk basış) tabanda → botlar hedeften ~0.2-0.45 sn yavaş.
- "COMING SOON" Continue etiketine sığmayabilir. Analiz paleti 6 renk (5'ten fazla botta tekrar). Oyuncudan sonra bitiren bot,
  `Changed` gelene kadar Stats'ta DNF.
- Oyuncu bitirince `RaceController` onu başa alıyor ve dünya yeniden kuruluyor; botlar kadraj dışında koşmaya devam ediyor.
- Telefonda 5 ek rig'in maliyeti ölçülmedi (`BotRaceConfig.botCount` düşürülebilir).

## 5. Bizim taraftan bilmen gerekenler
- Bizim kod `Scripts/Bridge`, `Core`, `UI`, `Meta`; UI kalıcı olarak kodla kurulmaz (prefab builder'dan), sahne/prefab'lar builder çıktısıdır.
- Denge sayıları config asset'lerinde (`Data/Config/*.asset`); asset ile kod varsayılanı aynı tutulur.
- Bu notu bitirince güncelle ya da sil; kendi DURUM'una işle.
