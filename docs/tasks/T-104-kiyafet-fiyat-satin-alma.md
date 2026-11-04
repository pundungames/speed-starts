---
id: T-104
title: Kostum magazasi - kilitli kartta fiyat, normal kiyafet Nakit ile satin alinabilsin
status: review
owner: ui-agent
depends_on: [T-084]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/T-104-kiyafet-fiyat
commits: []
tests:
  - Assets/Speed-Staes/Tests/EditMode/UI/Costume/CostumeEditorModelTests.cs
  - Assets/Speed-Staes/Tests/EditMode/UI/Costume/PremiumCardStateTests.cs
  - Assets/Speed-Staes/Tests/EditMode/UI/Builders/CostumePricePrefabTests.cs
  - Assets/Speed-Staes/Tests/PlayMode/CostumePriceSmokeTests.cs
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "Yama metodu kopyada kosulacak: CostumeItemCard.prefab + CosmeticsScreen.prefab (fiyat ve para birimi ikonlari); supervizor kosar ve alir"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Kullanici (04.11): "shop'da sadece kilit iconu var, maliyeti yazmiyor". Kok neden iki parca:
1) `CostumeItemCardView` kartinda fiyat alani hic yok; yalniz kilit katmani var.
2) T-084 normal kiyafetlere fiyat verdi (`CosmeticCatalog`: 44 oge 500, 6 oge 1.000 Nakit; 6 oge 0 = ucretsiz; 16 premium 50 Mor Mucevher), ama
   `CostumeEditorModel.TryGetBuyableItem` satin alma dugmesini yalniz `IsPremium` icin aciyor. Yani 50 normal kiyafet kilitli, fiyati gorunmuyor, satin alinamiyor,
   Kaydet de `LockedItem` ile reddediyor. `CosmeticService.TryPurchase` Nakit'i zaten destekliyor; eksik olan UI yolu.

Yapilacak:
1. `TryGetBuyableItem`: premium sarti kalkar, `IsLocked && IsForSale` yeter. `OnlyALockedPremiumItemOffersAPurchase` testi 04.11 kararina gore guncellenir
   (fiyatli kilitli normal oge teklif edilir; ucretsiz oge kilitli degildir, teklif edilmez; sahip olunan teklif edilmez).
2. Kart: kilitli ve satilik kartta fiyat + para birimi ikonu (Nakit / Mor Mucevher). Yeni alanlar `[SerializeField] private`, Tooltip'li; `Show()` kalibiyla
   null'a dayanikli. Hangi katmanin gorundugu saf mantikta (`PremiumCardLayers`'a eklenir) ve testli. Ornek: `EmoteCardView` (Card/Price yalniz kilitliyken),
   `ThemeSelectScreenView` (CurrencyIcon / GemIcon). Fiyat metni `CosmeticItemInfo.Price`'tan; yazi `TMP_Text`.
3. Satin alma dugmesi (`Purchase`, `purchasePriceText`): ikon secili ogenin para birimine gore. Bugun tek ikon var; Nakit fiyatini gem ikonuyla gostermek yanlis olur.
4. Prefab: `UiCostumePrefabs`'a hedefli yama metodu (`-executeMethod` ile cagrilabilir, ornek `UiRaceHudPrefabs.PatchRaceHudPrefab` + `SavePatchedPrefab`). Kullanici bu prefab'lari
   ELLE duzenledi: yama yalniz EKSIK nesneyi ekler, var olan nesnenin boyut/konum/rengine dokunmaz, ikinci kosuda dosyada fark yaratmaz. Sifirdan kurulum (`ItemCard` builder'i) da ayni nesneleri
   uretir. Yeni yazi TextMeshPro + American Captain (`UiFontPaths`). Sanat: `Assets/Speed-Staes/UI/Art` altindaki mevcut Nakit/gem ikonlari, kirpma/alfa sinirina dikkat.
5. Runtime'da hiyerarsi kurulmaz (kural 3.1). Prefab/sahne dosyasina ELLE YAZMA; Unity/test CALISTIRMA: supervizor kopyada yamayi ve testleri kosar.

## Kabul kriterleri
- [ ] Kilitli, fiyatli kartta fiyat ve dogru para birimi ikonu gorunur; sahip olunan ya da ucretsiz kartta gorunmez
- [ ] Kilitli normal ogeye dokununca Purchase dugmesi Nakit ikonu ve katalogdaki fiyatla acilir; yeterli Nakit varsa alinir, Nakit duser, kilit kalkar, Kaydet acilir
- [ ] Yetersiz Nakit'te mevcut red geri bildirimi calisir, Nakit ve sahiplik degismez
- [ ] Premium davranisi bozulmaz (gem ikonu, 50 Mor Mucevher katalogdan)
- [ ] Yama ikinci kez kosunca dosyada fark yok; kullanicinin elle ayarlari (mevcut nesnelerin geometrisi) degismez
- [ ] Testi var ve sabit denge sayisi assert etmiyor (fiyat katalogdan okunur): model, katman mantigi, prefab baglama, PlayMode dokunma akisi (`PlaySmokeSupport.Tap`)
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : mevcut: alinca kart punch (`PunchCard`), yetersizde `_purchaseDenied`
SES     : mevcut akista ne varsa aynisi; yeni ses yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Fiyat sayilari (degismez). Emote ve tema ekrani (fiyatlari zaten gorunuyor). Kaydet akisi. Premium sanati. Yeni para birimi. Kart yerlesiminde kullanicinin ayarlarini degistirmek.

## Calisma notu
- Yapilan: model premium sartini birakti (`IsLocked && IsForSale`); kart fiyat seridi + para birimi ikonlari (`PremiumCardLayers` fiyat alanlari); Purchase dugmesinde Nakit/gem ikonu secili ogeye gore; builder + yama metodu.
- Yama: `SpeedStars.Tools.Builders.UiCostumePrefabs.PatchCostumePricePrefabs` (CostumeItemCard.prefab + CosmeticsScreen.prefab; var olani degistirmez, ikinci kosuda kaydetmez, sonra TMP donusumu). Kopyada kosulacak (SCENE needs acik).
- Fiyat karti: sol alt serit (kilit sag alt kosede, cakisma yok): solda ikon, sagda sayi; Purchase'taki mevcut "Icon" gem ikonu olarak baglanir, yanina "CashIcon" (Costume/diamond_icon) eklenir.
- Testler: EditMode model + katman + prefab baglama (yama kosulana kadar kirmizi), PlayMode gercek dokunma (Nakit satin alma, yetersiz Nakit, premium gem, sahip olunan kartta fiyat yok). Hicbiri kosulmadi.
- Emin degilim: fiyat seridi yerlesimi/boyutu (ekran goruntusuyle bakilmadi), premium kartin altin cercevesiyle sol alt serit ortusmesi; yeni .cs: CostumePricePrefabTests.cs, CostumePriceSmokeTests.cs (.meta yok).
