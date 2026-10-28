---
id: T-004
title: Config ScriptableObject tipleri (ekonomi, etkinlik, odul, 7 gun, kozmetik)
status: review
owner: systems-agent
depends_on: [T-002]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Config/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/**"
touches_scene: false
branch: agent/faz1-iskele
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/EconomyConfigTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/RaceEventCatalogTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/GemRewardConfigTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/SevenDayRewardConfigTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/CosmeticCatalogTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/ConfigFixture.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "Data/Config/*.asset ornekleri PDF degerleriyle (T-006, scene-integrator)"
  - tip: WIRING
    durum: closed
    not: "Config SO'lari -> ProjectInstaller Inspector referansi (T-008) -> T-008 Installers (ProjectInstaller, SceneUiInstaller)"
  - tip: WIRING
    durum: closed
    not: "EconomyConfig.StartingCash/StartingPurpleGems/StartingEntryTickets/StartingTimedTickets -> ProgressRepository Func<StartingBalances> (T-003 seam; installer lambdasi, T-008) -> T-008 Installers (ProjectInstaller, SceneUiInstaller)"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`docs/TEMEL-SOZLESME.md` §6'daki config asset'lerinin **tipleri** (namespace `SpeedStars.Meta.Config`). Sayılar bu tiplerin
asset örneklerinde durur ve sahibi kullanıcıdır; kodda denge sayısı yoktur (alan varsayılanları 0/boş).

## Kabul kriterleri
- [x] `EconomyConfig`: Adet Bileti üst sınırı, yenilenme aralığı (dk), günlük hediye miktarı, Süreli erişim süresi (dk),
      başlangıç bakiyeleri (Nakit, Mor Mücevher, Adet Bileti, Süreli Bilet), bilet satın alma fiyatları (Nakit) ve ödüllü reklamla
      bilet (açık/kapalı + miktar) — fiyatı olmayan seçenek "kapalı" sayılır (PDF'te fiyat yok, MD §18.4), gün sınırı politikası (enum).
- [x] `RaceEventDefinition` (kimlik, görünen ad, mesafe m, `RequiresTicket`, `AwardsPurpleGems`, oynanabilir mi) + `RaceEventCatalog`
      (liste, kimlikle bulma, katalog sırası). Kimlik `SpeedStars.Core.Race.RaceEventId`'ye çevrilir.
- [x] `GemRewardConfig`: sıraya göre Mor Mücevher tablosu; tablo dışındaki sıra 0 (tek tablo, etkinlik başına kopya yok).
- [x] `SevenDayRewardConfig`: 7 gün; gün başına ödül türü (`None`, `Cash`, `EntryTicket`, `TimedTicket`, `Cosmetic`) + miktar ya da
      kozmetik referansı; boş gün desteklenir (PDF'te 6. gün yok); döngü politikası (enum).
- [x] `CosmeticDefinition` (kimlik, kategori, prefab'taki parça yolu/adı, premium mi, fiyat + para birimi `Cash|PurpleGem`, liste sırası)
      + `CosmeticCatalog` (kategoriye göre liste: **premium önce**, sonra sıra — PDF §2.3). `CosmeticCategory` enum'u prefab yuvalarıyla
      aynı adlarda: Headwear, Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes (`reports/kozmetik-hazirlik-2026-10-28.md`).
- [x] Her tipte `[CreateAssetMenu(menuName = "Speed Stars/Config/...")]`; `OnValidate` yinelenen kimlikte uyarır.
- [x] EditMode testleri: katalogda kimlikle bulma, kozmetik sıralaması (premium önce), ödül tablosu dışı sıra = 0, boş 7. gün.
      Testler `ScriptableObject.CreateInstance` ile kendi değerlerini verir ve **ilişkiyi** assert eder (PDF sayısı assert edilmez).
- [x] MonoBehaviour yok, Zenject referansı yok.

## Kapsam dışı
- Asset örnekleri ve PDF değerleri (T-006), bilet/ödül hesaplayan servisler (Faz 2-3), UI.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz1` (worktree, dal `agent/faz1-iskele`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §2 (isim sözlüğü), §6, `docs/TUZAKLAR.md`,
  `agent-kit/02-MIMARI-STANDARTLARI.md`, `Assets/Speed-Staes/Scripts/Core/Runtime/**` (DEĞİŞTİRME).
- Paralelde T-003 aynı asmdef'te (`Scripts/Meta/Persistence`, `Meta/Time`) çalışıyor: onun klasörlerine yazma.
- Unity, batchmode, `unity-verify` YASAK. `git commit` YAPMA. `.meta` yazma.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yapılan: `Scripts/Meta/Config`'te 7 SO tipi + enum'lar (`DayBoundaryPolicy`, `SevenDayRewardKind/CyclePolicy`, `CosmeticCategory/Currency`), bütün alanlar 0/boş, Tooltip'li; iki katalogda `OnValidate` yinelenen ve boş kimliği uyarır (`ConfigIdCheck`, `FindDuplicateIds()` testlenebilir). 36 test metodu / 38 vaka; private alanlara SerializedObject ile (Inspector yolu) yazar, alan adlarını da sınar.
- Kararlar: kozmetik parçası tek alan değil `partNames[]` (`Accessories/<Category>/` altındaki nesne adları; GL03 + GL03SMLEL/R tek set, yol yazılırsa kategori iki yerde olurdu). Bilet fiyatı = bir biletin Nakit fiyatı, 0 = kapalı (`TryGet...CashPrice`); ödüllü reklam yalnız Adet Bileti (PDF §1.2), açık + miktar > 0 değilse kapalı. 7 gün 1 tabanlı `GetDay`; None, miktarsız, kozmetiksiz ya da listede olmayan gün boş. `DayCount = 7` yapısal sabit.
- Zorlanılan: Unity yasak olduğu için scratchpad'de dotnet + Unity 2022.3.62f2 referans DLL'leri (UnityEngine/UnityEditor/nunit) ile derleme denetimi: Config + T-003'ün o anki dosyaları + bütün Meta testleri birlikte 0 hata (yalnız CS0649). Testler KOŞULMADI; bellek içi SO'ya `objectReferenceValue` ve enum `intValue` yazımı Unity'de doğrulanmadı.
- Bırakılan: kaçırılan gün politikası alanı yok (task yalnız döngü istedi; OPEN #2). Kozmetikte görünen ad/ikon yok (task listesinde yok). Premium = PurpleGem kuralı asset'te zorlanmıyor (satın alma servisi, Faz 3).
- Risk (T-003 kaynaklı, kodda düzeltilmedi): `SpeedStars.Meta.Time` namespace'i, `SpeedStars.Meta.*` içindeki kodda `Time.x`'i `UnityEngine.Time` yerine o namespace'e çözer (CS0234). Config `Time` kullanmıyor.
