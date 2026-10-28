---
id: T-025
title: Kozmetik cekirdegi - yarisci on ayarlari (Custom N), sahiplik, premium satin alma, onizleme, CosmeticApplier
status: review
owner: systems-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/CosmeticContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Config/Cosmetic*.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/ProgressRepository.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/Cosmetic*.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/MenuCharacter.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceRunnerOutfit.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CosmeticCatalogSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneCosmetics.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/**"
touches_scene: true
branch: agent/T-025-kozmetik-cekirdek
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/CosmeticServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/CosmeticPreviewAndPlanTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller.InstallBindings, WalletService bind'inin altina: Container.Bind<ICosmeticService>().To<CosmeticService>().AsSingle(); Container.Bind<ICosmeticPreview>().To<CosmeticPreview>().AsSingle(); (using SpeedStars.Meta.Cosmetics; CosmeticService ctor: ProgressRepository, WalletService [concrete, zaten bagli], CosmeticCatalog [zaten BindConfig]). SceneBuilder.BuildRace: DisableRunnerDebugTools(scene) satirindan sonra tek satir: SceneCosmetics.AddToRaceRunner(scene);"
  - tip: ASSET
    durum: open
    not: "Data/Config/CosmeticCatalog.asset + Data/Config/Cosmetics/*.asset yeniden tohumlanmali (ConfigAssetSeeder overwrite, CosmeticCatalogSeed.Seed): yeni alanlar presetCount (3), presetNameFormat, defaultOutfit (6 kayit), colorPalette (8 renk), legacyOutfitObjects, ve oge basina primaryChannel. Tohumlanana kadar asset'te bunlar kod varsayilaninda: varsayilan kiyafet BOS, palet BOS, primaryChannel hep Color1. Yeniden tohumlama 7. gun odulu ve diger config referanslarini korur (CreateOrReset yerinde sifirlar)."
  - tip: ASSET
    durum: open
    not: "Yeni .cs dosyalarinin .meta'lari Unity'de uretilmeli (kopyada derle, .meta'yi al): Scripts/Meta/Cosmetics/*, Meta/Config/CosmeticOutfitEntry.cs, Meta/Contracts/CosmeticContracts.cs, Bridge/CosmeticApplier.cs, Bridge/RaceRunnerOutfit.cs, Tools/Editor/Builders/Scene/SceneCosmetics.cs, Tests/EditMode/Meta/Cosmetics/*, Scripts/Meta/Cosmetics klasoru"
  - tip: DECISION
    durum: open
    not: "On ayar sayisi (Custom 1..N) PDF'te yok: config'te, tohum 3. Normal kozmetiklerin fiyati yok (OPEN #6): normal = sahip, premium = 50 Mor Mucevher (PDF)"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Kostüm editörünün (T-026) ve menü/yarış karakterinin dayandığı çekirdek. Katalog zaten var (`Data/Config/CosmeticCatalog.asset`,
72 öğe = 8 kategori × 9; premium 50 Mor Mücevher; `CosmeticCatalogSeed` prefab'tan okuyor). Eklenenler:
1) **Yarışçı ön ayarları** ("Custom 1".."Custom N", N config'te): her ön ayar = kategori başına öğe (ya da yok) + o öğenin birincil
   rengi. Seçili ön ayar kayıtta. Eski `selectedCosmetics` listesi varsa ilk ön ayara taşınır (şema sürümü artar, eski kayıt bozulmaz).
2) **Varsayılan kıyafet** katalog asset'inde (kod sabiti değil): yeni kayıtta her ön ayar bununla başlar (tasarımdaki gibi şapka, gözlük,
   atlet, şort, tayt, ayakkabı; renkler asset'te).
3) **Sahiplik ve satın alma:** fiyatı olmayan (normal) öğe sahip sayılır; premium öğe satın alınana kadar kilitli. `TryPurchase` Mor
   Mücevherle, `IWallet` üzerinden, idempotent (aynı öğe iki kez para düşmez), sonuç enum'u (Purchased, AlreadyOwned, NotEnough, NotForSale).
   Kayıt: kilitli öğe içeren kıyafet kaydedilemez (sonuç enum'u).
4) **Önizleme** (`ICosmeticPreview`): menü karakterinin o an giydiği kıyafet + Changed olayı. Varsayılan = seçili ön ayar. Kostüm
   ekranı kaydetmeden önizler; Back'te kayıtlı hâle döner.
5) **Rastgele:** sahip olunan öğeler + config'teki renk paletinden; `System.Random` dışarıdan verilir (testte tohumlu).
6) **CosmeticApplier** (Bridge, asmdef'siz): bir koşucu kökü + kıyafet alır; `Accessories/<Kategori>/` altında seçili öğenin parçalarını
   açar, diğerlerini kapatır; birincil rengi MaterialPropertyBlock ile `_Color`'a yazar (paylaşılan materyale asla dokunmaz). Birincil
   kanal öğe başına katalogda (materyal adı: `Color1_Toon`/`Color2_Toon`/`Color3_Toon`); tohum ilk bulunanı yazar. Kökteki eski temel
   kıyafet (Tshirt, Short, Leggings, Shoes, Glove, Helmet) çalışma anında gizlenir (ad listesi config'te). Malik'in prefab'ı değişmez.
7) **MenuCharacter** açılışta önizlemeyi uygular ve Changed'i dinler; **RaceRunnerOutfit** (Race sahnesindeki koşucuya builder ile
   eklenen bileşen) seçili ön ayarı yarış başında bir kez uygular.

Ayrıntı ve prefab yapısı: `reports/kozmetik-hazirlik-2026-10-28.md`. Kozmetik ikonları bu task'ta yok (Serhat; T-026 yer tutucu koyar).

## Kabul kriterleri
- [ ] `ICosmeticService` + `ICosmeticPreview` + kıyafet değer tipi `Meta/Contracts/CosmeticContracts.cs`'te; UI (T-026) yalnız bunları kullanır
- [ ] Kayıt: ön ayarlar + seçili ön ayar + sahip olunan premium'lar; eski kayıt (ön ayarsız) açılınca varsayılanla dolar, eski seçim korunur
- [ ] EditMode testleri: satın alma bir kez para düşer, yetersiz para reddedilir, normal öğe sahip, kilitli öğeyle kaydetme reddedilir,
      ön ayar seçimi/kaydı geri yüklenir, rastgele yalnız sahip olunan öğe seçer, göç eski seçimi taşır. Sabit fiyat/adet assert edilmez
- [ ] Applier saf kısmı (hangi parça açık, hangi kanal) test edilebilir sınıfta; MonoBehaviour yalnız uygular
- [ ] Home'da menü karakteri varsayılan kıyafetle görünür (ekran görüntüsü süpervizörde)
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Kapsam disi
- Kostüm editörü UI'ı (T-026), ikonlar, premium trail/partikül (efekt asset'i yok)
- Malik'in prefab'ına/koduna yazmak; Serhat'ın `AccessoryPhotoStudio`'suna dokunmak (yalnız okunur)
- Home sahnesi builder'ı (T-023'ün); Race'e bileşen eklemek için yeni `SceneCosmetics.cs` + süpervizöre tek satırlık çağrı notu

## Calisma notu
1. Yapildi: katalogda preset sayisi/ad bicimi/varsayilan kiyafet/palet/eski nesne adlari + oge basina birincil kanal; PlayerProgress sema 2 (cosmeticPresets, selectedPresetIndex; ADR docs/decisions/ADR-2026-10-28-kozmetik-kayit-v2.md); CosmeticService, CosmeticPreview, OutfitPlan (saf), CosmeticApplier/RaceRunnerOutfit, MenuCharacter onizleme.
2. Sahiplik: fiyatsiz + premium-olmayan = sahip; baska her sey ownedCosmeticIds. Satin alma IWallet'ten degil: IWallet salt-okunur ve MetaContracts bizim degil; bakiye PlayerProgress'ten dusulup WalletService.NotifyChanged cagriliyor (ctor somut WalletService alir).
3. Zorlanma: Zenject `[InjectOptional]` ile MenuCharacter/RaceRunnerOutfit baglama yokken cokmez, Start'ta uyari verir. Eski temel kiyafet adlari kokte dogrudan cocuk aranir (Shoes/Leggings Accessories altindaki slotlarla ayni adda).
4. Birakilan: ProjectInstaller/SceneBuilder baglamasi, katalog yeniden tohumlama, .meta uretimi (needs). Kostum ekrani icin tum sozlesme CosmeticContracts.cs'te; ikon yok (isim yer tutucu CosmeticItemInfo.Name).
5. Test (koşulmadi; merkezi dogrulama): Tests/EditMode/Meta/Cosmetics, sabit fiyat/adet assert yok. Sema 1->2 degistigi icin TAM SUITE gerekir. Gercek asset'i okuyan test yok (asset yeniden tohumlanmadan varsayilan kiyafet bos).
