# Devir notu — 30.10.2026 akşam (Malik'e ve sonraki oturuma)

> Kullanıcı bilgisayarı kapattı; bu turun işleri **test edilmedi**. Main çalışan halde (rakip botlar dahil, kopyada yeşil:
> EditMode 111, PlayMode 27 + 2 çekim). Yarım işler ayrı dalda: **`agent/T-077-080-duzeltmeler`** (worktree
> `D:\Pundun\speed-starts-duzeltme`). Dal main'e GİRMEDİ. Uzaktan çalışacaksan kullanıcının bu dalı push etmesi gerekir.
> Bot sistemi için önce oku: `docs/MALIK-DEVIR-rakip-botlar.md`.

## Kullanıcının oyunda bulduğu sorunlar ve dalda yapılanlar
| Sorun | Sebep (bulunan) | Dalda yapılan | Durum |
|---|---|---|---|
| Race again 3-4 kez sonra çalışmıyor | Adet Bileti (tavan 3) bitiyor; Race sahnesinde `IEntryGatePrompt` hiç uygulanmamış/bağlanmamış, akış sadece log basıyor (`RaceFlowLogic.cs:242`) | T-079: `UI/Race/RaceFlowEntryGatePrompt.cs` (Home'daki EntryGateModal'ı açar) | derlenmedi, bağlanmadı |
| Practice / diğer modlar çalışmıyor | `Data/Config/Events/training.asset` `isPlayable 0`, `distance 0`; engelli/bayrak/Race of the Day mekanikte yok | T-079: `EventSelectConfigSeed.EnablePractice` (ücretsiz, 100 m), kilitli modlarda "COMING SOON" | derlenmedi, asset üretilmedi |
| 200/400 m | `RaceController.Start` bitiş çizgisini sahnedeki 100 m'ye koyuyor; `RequestStart` mesafeyi değiştirince çizgi gelmiyor | yok (kilitli) | KARAR: adaptör çizgiyi taşısın + 200/400 için bot kalibrasyonu |
| Stats'ta botlar yok | `RaceFlowLogic.PresentAnalysis` rakipleri `null` veriyor | T-077: `AnalysisScreenView` `IRaceTraces`'i okuyup botları ekler (eğri + bayraklı satır) | derlenmedi, prefab üretilmedi |
| "Hep 1. gösteriyor" | KODDA KESİN: oyuncu ilk basıştan, botlar da tepkileri hariç ölçülüyordu → yavaş tepkili oyuncuya sıralamada 0.2-0.4 sn avantaj. KESİN: en hızlı bot 15.97 sn (sürücü periyot 0.32 / tutma 0.10 sabit), oyuncu 15.6 koşuyor. Varsayım: perspektif (uzak şerit ~0.1-0.35 m önde görünür, küçük) | T-078: herkes GO'dan (tepki dahil), oyuncunun bitişi kare içinde ara değerli, mesafe-zaman kaydı (`RaceTraceRecorder`, `RaceBots : IRaceTraces`) | `blocked`: kalibrasyonda sürücü arama (periyot 0.20-0.40 × tutma) YAZILMADI; config tablosu hâlâ eski (ilk basış) tabanda → botlar hedeften ~0.2-0.45 sn yavaş, yeniden kalibrasyon şart |
| Klavye ok tuşları | Malik ok tuşlarını N/M'ye çevirmişti (Scene görünümü kayıyordu) | T-078: `Bridge/KeyboardRunnerInput.cs` (sol/sağ ok → `RunnerInput.Inject`) | derlenmedi |
| Ana menü geç yükleniyor | Yükleme zaten async; Home.unity 5 tema dünyasının hepsini yüklüyor (~160 MB, `Home.unity:4580`); geçiş örtüsü alfa 0 → donmuş görünüyor | T-080: yalnız ölçüm (`Core/Runtime/Scenes/SceneLoadProbe.cs`, PlayMode `SceneLoadTimingTests`) | KARAR: yalnız seçili dünyayı yükle (ek sahne); örtü opak + "Loading" |

## Bitirmek için sıra
1. Dal `agent/T-077-080-duzeltmeler` son commit 77a3c763: dört task'ın hepsi commit'li (hiçbiri derlenmedi). T-078 `blocked`
   (sürücü araması), diğerleri `review`. `IRaceTraces` bağlaması aynı `raceBots` alanından: `Container.Bind<IRaceTraces>().FromInstance(raceBots)`.
2. Derle (kopyada ya da Editor'de). Hata varsa önce onu düzelt — kod hiç derlenmedi.
3. `RaceSceneInstaller`'a (kompozisyon kökü, hook korumalı, kullanıcı onayı ister) iki bağlama:
   `[SerializeField] private EntryGateModalView entryGateModal;` → doluysa
   `Container.Bind<IEntryGatePrompt>().To<RaceFlowEntryGatePrompt>().AsSingle().WithArguments(entryGateModal);`
   ve `IRaceTraces` → `RaceBots` (ya da T-078'in kaydedicisi; T-078 task'ındaki WIRING notu kesin kodu verir).
4. Builder: `SceneBuilder.ApplyEventAvailability()` → `UiPrefabBuilder.BuildAll(true)` (analiz satırı/ekranı) →
   `SceneBuilder.BuildHome(true)` + `BuildRace(true)` (installer alanları dolsun) → `BotCalibration` (yeni sürücü aramasıyla) → config.
5. Test: EditMode (Bot|RaceStandings|Analysis|EventSelect|RaceFlow), PlayMode tamamı + `Shots_HomeAndRace` çekimleri.
6. Gözle: bilet bitince pencere açılıyor mu, Practice biletsiz başlıyor mu, Stats'ta botlar, ok tuşları, yavaş koşunca 2./3.

## Olası sorunlar (test edilmedi — dikkat)
- **Derleme**: üç agent aynı anda yazdı, hiçbiri derlemedi. İsim/imza uyuşmazlığı olabilir (özellikle `IRaceTraces` uygulayıcısı
  T-078 ↔ okuyucu T-077; `RaceSessionAdapter` T-078 ↔ `RaceFlowLogic`).
- **Bağlama yoksa**: `IEntryGatePrompt` / `IRaceTraces` `[InjectOptional]` — bağlanmazsa hata vermez ama özellik görünmez
  (bilet bitince yine sessiz, Stats'ta bot yok). "Derlendi" ≠ "çalışıyor".
- **Prefab alanları boş**: builder koşulmadan yeni alanlar (`flagImage`, `flags`, `comingSoonLabel`, `entryGateModal`) prefab/sahnede
  `fileID: 0` kalır; view null kontrolü yoksa NullReference. Builder'dan sonra YAML'da grep'le.
- **Giriş penceresi Race sahnesinde**: Home'daki bağımlılıkları (UiMotionConfig, bilet kaynağı, ProgressRepository) ProjectContext'ten
  bekliyor; Race'te hiç denenmedi. Onaylanınca Race sahnesini yeniden yüklüyor — T-080'in ölçüm koduyla etkileşimi denenmedi.
- **Saat GO'dan**: oyuncunun süresi tepki dahil olunca kişisel rekorlar ve liderlik süreleri eskisinden ~0.2-0.5 sn kötü görünür;
  eski PB'ler ilk basıştan ölçülmüştü (karışık taban). Karar gerekebilir.
- **Ok tuşları**: Editor'de Scene görünümü odaktayken oklar sahneyi kaydırır (Malik'in kaldırma sebebi); Game görünümü odakta olmalı.
  GO'dan önce basış kapısı adaptörün kapısına bağlı olmalı — yanlış bağlanırsa false start / erken kalkış.
- **Bot hızı**: sürücü araması fiziğin verebildiği en iyiyi bulamazsa botlar yine ~16 sn'nin altına inemez → iyi oyuncu hep 1.
- **COMING SOON** yazısı Continue butonunun etiketine sığmayabilir (dar sanat).
- **Analiz renkleri**: palet 6 renk; 5'ten fazla bot olursa renkler tekrar eder. Oyuncudan sonra bitiren bot, `Changed` gelene kadar
  DNF görünür.
- **Practice 100 m ve ücretsiz** agent'ın seçimi (katalog 0 m'ydi) — kullanıcıya onaylat.
- **Telefonda performans**: 5 ek rig ölçülmedi.
