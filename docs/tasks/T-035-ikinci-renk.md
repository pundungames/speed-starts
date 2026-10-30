---
id: T-035
title: Kostum editoru - ogenin 2. (ve varsa 3.) rengi; renk ornek kutulari kanal secer ve canli rengi gosterir
status: review
owner: systems-agent
depends_on: [T-025, T-026, T-032]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/CosmeticContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Scripts/Meta/Config/Cosmetic*.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/ProgressRepository.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/CosmeticApplier.cs"
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CosmeticCatalogSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
  - "docs/decisions/**"
touches_scene: true
branch: agent/T-035-ikinci-renk
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/CosmeticServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/CostumeEditorModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: open
    not: "Assets/Speed-Staes/Data/Config/Cosmetics/*.asset (CosmeticDefinition) (alanlar: channels, channelDefaultColors; primaryChannel ile tutarli): CosmeticCatalogSeed.Seed overwrite=false yolu BackfillChannels ile kanal listesi bos olan her ogeyi prefab materyallerinden (Color1/2/3_Toon _Color) doldurur; ConfigAssetSeeder calistirilmali. Doldurulana kadar her ogenin tek kutusu ve beyaz varsayilan rengi olur."
  - tip: SCENE
    durum: open
    not: "CosmeticsScreen.prefab yeniden uretilmeli (UiCostumePrefabs.BuildAll, overwrite): ColorPanel'de SwatchColor yerine 3 kanal kutusu (Channels/Channel1..3, CostumeChannelBoxView); yeni CostumeChannelBoxView.cs icin .meta kopyada uretilip GeriAl ile alinmali."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanıcı (30.10): "kıyafet seçerken renk seçiyorum ama 2. renk seçimi yapamıyorum, onu da ekleyelim. Altına kayıtlı renk seçeneği koymuşsun
ama çalışmıyor." Bugün her öğenin yalnız **birincil** renk kanalı düzenlenebiliyor (`CosmeticSlot.ColorHex` tek renk;
`CosmeticDefinition.PrimaryChannel`); renk panelindeki örnek kutusu yalnız gösterim ve öğenin rengi yoksa lacivert kalıyor.
Prefab'taki öğelerin 1-3 renk kanalı var (materyal adı `Color1_Toon` / `Color2_Toon` / `Color3_Toon`, MaterialPropertyBlock `_Color`;
`reports/kozmetik-hazirlik-2026-10-28.md`).

Yapılacak:
1) **Veri**: öğe tanımı kendi kanallarını sıralı taşır (birincil önce, sonra öbürleri; tohum prefab materyallerinden okur). Kıyafette her
   kategori için kanal başına renk (`CosmeticSlot` kanal → hex; boş = materyalin kendi rengi). Kayıtta kanal başına renk; eski kayıt (tek
   `colorHex`) birincil kanala taşınır, bozulmaz (şema artarsa kısa ADR `docs/decisions/`).
2) **Uygulayıcı**: `CosmeticApplier`/`OutfitPlan` her kanalın rengini kendi materyal yuvasına yazar (paylaşılan materyale dokunmaz).
3) **UI**: renk panelinde, SV karesinin altındaki örnek kutusu yerine seçili öğenin kanalları kadar **renk kutusu** (1., 2., varsa 3.):
   her kutu o kanalın şu anki rengini gösterir (kayıtlı renk yoksa materyalin varsayılan rengi — tohumda katalogdan, lacivert değil);
   kutuya dokununca renk seçici o kanalı düzenler, seçili kutu çerçeveyle belli; renk değişince kutu ve karakter anında güncellenir.
   Tek kanallı öğede tek kutu. Kutu sanatı: `UI/Art/Costume/color_swatch_frame.png` (+ seçili için aynı çerçeve vurgulu/renkli; yer tutucu).
4) Random kanal başına renk verir (palet config'te); Save/Back davranışı aynı.

## Kabul kriterleri
- [ ] İki/üç kanallı öğede 2. ve 3. renk seçilip kaydedilebilir, karakterde görünür; menü ve yarış koşucusu aynı renkleri giyer
- [ ] Renk kutuları gerçek rengi gösterir (lacivert yer tutucu yok), dokununca kanal değişir
- [ ] EditMode: kanal başına renk kaydı + eski kayıt göçü + plan (hangi materyal yuvasına hangi renk); sabit renk/sayı assert edilmez
- [ ] PlayMode duman testleri yeşil (gerçek ışınlı Tap); gerekirse kostüm ekranında kutuya dokunan bir adım
- [ ] Legacy Text, DOTween yalnız çekirdek, kod kalıcı UI kurmaz (TUZAKLAR)

## Geri bildirim
GORSEL  : seçili kutu vurgulu; renk değişimi anında karakterde; kutuya dokununca küçük punch (UiMotionConfig)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Yeni kanal/materyal eklemek (Malik'in prefab'ı), ten rengi kategorisi

## Calisma notu
- Yapilan: renk indeksi = ogenin kanal sirasi (0 birincil); CosmeticSlot 3 renk, kayit colorHex (birincil, eski anahtar) + moreColorHexes; sema 2 kalir (ADR-2026-10-30). Eski kayit kod gocu gerektirmeden birincil kanala duser.
- OutfitPlan/CosmeticApplier her kanal rengini adiyla eslesen materyal yuvasina yazar; Random kanal basina palet rengi verir; tohum kanallari + varsayilan renkleri prefab materyallerinden okur (mevcut ogeler icin BackfillChannels).
- UI: CostumeChannelBoxView (yeni) x3 colour panel altinda; CostumeEditorModel kanal secimi (ChannelIndex/ChannelHex), picker secili kutuyu canli boyar, ekran kutuya dokununca kanali degistirir + punch; builder SwatchColor yerine 3 kutu uretir (secili = kirmizi halka, yer tutucu).
- Zorlandigim: yeni .cs icin .meta yazamam (hook); prefab/asset yeniden uretimi needs'te (ASSET + SCENE). Unity/test kosulmadi.
- Testler: CosmeticServiceTests, CosmeticPreviewAndPlanTests, CostumeEditorModelTests genisletildi; PlayMode duman testi kutulara gercek isinli Tap atar. Kayit degisti: tam suite gerekir.
