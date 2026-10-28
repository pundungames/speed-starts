---
id: T-003
title: Kayit ve saat servisleri (ISaveStore, IClock, PlayerProgress)
status: done
owner: systems-agent
depends_on: [T-002]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/**"
  - "Assets/Speed-Staes/Scripts/Meta/Time/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Persistence/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Time/**"
touches_scene: false
branch: agent/faz1-iskele
commits: [c3dbe46]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Persistence/FileSaveStoreTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Persistence/ProgressRepositoryTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Time/ClockServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "ProjectInstaller (T-008): FileSaveStore.CreateDefault() -> ISaveStore; ProgressRepository(store, () => new StartingBalances(EconomyConfig.StartingCash, .StartingPurpleGems, .StartingEntryTickets, .StartingTimedTickets)) AsSingle, kurulumda bir kez Load(); servisler repo.Current okur (PlayerProgress ornegini ayrica bind etme, Load yeniler); ClockService(() => repo.Current.LastSeenUtcTicks, t => repo.Current.LastSeenUtcTicks = t) -> IClock, Load sonrasi kurulur; repo.Save() OnApplicationPause(true) ve OnApplicationQuit aninda (ince yasam dongusu bileseni) -> T-008 Installers (ProjectInstaller, SceneUiInstaller)"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`SpeedStars.Core.Services`'teki `ISaveStore` ve `IClock`'un uygulamaları ile oyuncu ilerlemesinin kalıcı modeli
(`docs/TEMEL-SOZLESME.md` §4 seam tablosu, MD §14 "PlayerProgress"). Bilet, cüzdan ve ödül mantığı sonraki fazlarda bu
modelin üstüne kurulacak; bu task yalnız saklama ve saat.

## Kabul kriterleri
- [ ] `FileSaveStore : ISaveStore` (namespace `SpeedStars.Meta.Persistence`): kök klasör yapıcıdan gelir; `Write` atomiktir
      (geçici dosyaya yaz → yer değiştir; önceki içerik `.bak` olarak kalır); `TryRead` ana dosya yok/bozuksa yedeğe düşer;
      `Delete`. `Application.persistentDataPath` yalnız varsayılan kök için kullanılır, testler geçici klasör verir.
- [ ] `ClockService : IClock` (`SpeedStars.Meta.Time`): cihaz UTC'si; son görülen UTC dışarıdan verilen bir kaynaktan okunur/yazılır;
      cihaz saati geri alınırsa `UtcNow` son görülenin altına inmez ve geri alma bir kez bildirilir (olay ya da bayrak).
      Gerçek zamanlı süreler oyun kapalıyken de akar (yalnız cihaz saatine dayanır) — PDF §1.1.
- [ ] `PlayerProgress` (`[Serializable]`, `JsonUtility` uyumlu — Dictionary yok, liste/çift kullan): şema sürümü, Nakit,
      Mor Mücevher, Adet Bileti, Süreli Bilet stoğu, Süreli erişim bitişi (UTC ticks), Adet Bileti yenilenme zamanı (UTC ticks),
      günlük hediye gün anahtarı, 7 gün durumu (alınan sayı, son alınan gün anahtarı), sahip olunan kozmetik kimlikleri,
      seçili kozmetikler (kategori → kimlik + renk), etkinlik başına en iyi süre, işlenmiş işlem kimlikleri, son görülen UTC.
- [ ] `ProgressRepository`: `Load()` (kayıt yoksa yeni ilerleme; başlangıç değerleri dışarıdan bir sağlayıcıdan gelir — kodda sayı yok),
      `Save()`, şema sürümü kontrolü.
- [ ] EditMode testleri (`Tests/EditMode/Meta/...`): atomik yazım (yarım kalan geçici dosya eski içeriği bozmaz), yedekten kurtarma,
      gidiş-dönüş serileştirme, saat geri alma. Sabit denge sayısı assert edilmez.
- [ ] `SpeedStars.Meta` Zenject'e referans vermez; MonoBehaviour yok; kodda denge sayısı yok.
- [ ] needs listesindeki WIRING'i süpervizör T-008'de kapatır.

## Kapsam dışı
- Bilet/cüzdan/ödül kuralları (Faz 2-3), installer bağlama (T-008), PlayerPrefs (ekonomi için kullanılmaz).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz1` (worktree, dal `agent/faz1-iskele`). Ana klasöre (`D:\Pundun\speed-starts`) yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §3-§6, `docs/TUZAKLAR.md`, `agent-kit/02-MIMARI-STANDARTLARI.md`,
  `Assets/Speed-Staes/Scripts/Core/Runtime/**` (seam'ler hazır, DEĞİŞTİRME — değişiklik gerekiyorsa BLOCKED yaz).
- Unity, batchmode, `unity-verify` YASAK. `git commit` YAPMA (süpervizör commit'ler). `.meta` yazma (Unity üretir).
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yapılan: `FileSaveStore` (dosya başına tek satır başlık `SSAVE1 <FNV-1a 64 hex>` + UTF-8 içerik; yazım tmp -> eski ana `.bak` -> tmp ana, `Flush(true)`; bozuk/eksik anada `.bak`; bozuk ana yedeğe dönüştürülmez), `PlayerProgress` (+ `SevenDayCheckInState`, `SelectedCosmetic`, `EventBestTime`; `[SerializeField] private` alan adları kayıt anahtarı), `ProgressRepository` (`StartingBalances` sağlayıcısı, şema 1, daha yeni/okunamayan kayıtta yeni ilerlemeyle çalışır ve `Save` false döner, dosyaya dokunmaz), `ClockService` (son görülen UTC delege ile, geri almada donar, `RolledBack` olayı bölüm başına bir kez + `IsRolledBack`).
- Test: 47 EditMode testi. Unity koşulmadı; Unity'nin Roslyn'i ile Core+Meta+Meta testleri (T-004 dosyaları dahil) derlendi, motor gerektirmeyen 38 test Unity'nin Mono'sunda geçti; JsonUtility kullanan 9 depo testi yalnız Unity'de koşar.
- Zorlanılan: `ISaveStore` ham metin tuttuğu için bozukluğu depo göremez -> bütünlük kontrolü dosya başlığında; bu yüzden elle düzenlenen kayıt dosyası bozuk sayılır.
- Süpervizör düzeltmesi: ad alanı `SpeedStars.Meta.Time` -> `SpeedStars.Meta.Clock` (testler `SpeedStars.Tests.Meta.Clock`), klasörler aynı; sebep `UnityEngine.Time` gölgelemesi (CS0234, derleyiciyle denendi), TUZAKLAR §12'de. Yeniden derlendi, 33 motorsuz test yine geçti.
- Bırakılan: kayıt formatı için ADR (docs/decisions owns_paths dışında), `ProcessedTransactionIds` budama politikası ve `EntryRegenDueUtcTicks` 0 = sayaç yok anlamı Faz 2 ekonomi servisine; WIRING açık (T-008).
