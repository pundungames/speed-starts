---
id: T-025
title: Kozmetik cekirdegi - yarisci on ayarlari (Custom N), sahiplik, premium satin alma, onizleme, CosmeticApplier
status: todo
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
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: ICosmeticService + ICosmeticPreview AsSingle; Home/Race sahnelerine applier bileseni builder (SceneCosmetics) ile, SceneBuilder'a tek satir cagri supervizorde"
  - tip: DECISION
    durum: open
    not: "On ayar sayisi (Custom 1..N) PDF'te yok: config'te, tohum 3. Normal kozmetiklerin fiyati yok (OPEN #6): normal = sahip, premium = 50 Mor Mucevher (PDF)"
started:
finished:
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
(Agent doldurur)
