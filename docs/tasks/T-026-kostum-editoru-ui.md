---
id: T-026
title: Kostum editoru ekrani (Edit racer) - kategori, oge izgarasi, renk secici, on ayar, Random/Save, satin alma
status: review
owner: ui-agent
depends_on: [T-025]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Costume/**"
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/CostumeConfigSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/**"
touches_scene: true
branch: agent/T-026-kostum-editoru
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/CostumeEditorModelTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Costume/HsvColorTests.cs"
  # ShakeCurve (denied-feedback curve) is tested in CostumeEditorModelTests.cs
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller -> Container.Bind<ICosmeticService>().To<CosmeticService>().AsSingle() ve Container.Bind<ICosmeticPreview>().To<CosmeticPreview>().AsSingle() (CosmeticService(ProgressRepository, WalletService, CosmeticCatalog); CostumeEditorScreenView + CostumeResourceView bunlari ve IWallet/IEntryTickets/IInstantiator/UiMotionConfig'i [Inject] ile alir). Home tarafi degismez: SceneWiring zaten CosmeticsScreen yolunu UiScreen olarak baglar, yeni prefab bir UiScreen"
  - tip: SCENE
    durum: open
    not: "Dimscreen karakterin ARKASINDA olmali (overlay tuvalde karakteri de karartir): Home sahnesine Screen Space - Camera 'Canvas Dim' (plane distance: karakterden uzak, dekordan yakin, yani karakter dim'in onunde cizilir) + icinde stretch Image (UI/Art/Costume/dimscreen.png, raycast kapali), baslangicta kapali; CostumeEditorScreenView.dimBackdrop = o nesne. Bos birakilirsa ekran calisir, yalniz karartma olmaz"
  - tip: ASSET
    durum: open
    not: "Kopyada builder: UiPrefabBuilder.BuildAll(overwrite: true) -> yalniz UI/Screens/CosmeticsScreen.prefab, UI/Rows/CostumeItemCard.prefab, UI/Rows/CostumePresetRow.prefab ve Data/Config/CostumeEditorConfig.asset (+ .meta'lar) ana agaca alinir; digerleri (kullanicinin duzenledigi prefablar) alinmaz. Art PNG/.json/.jpg .meta'lari Unity uretir"
  - tip: DECISION
    durum: open
    not: "Power/Stamina/Height PDF'te yok: panel yalniz gosterim (config'teki yer tutucu degerler). Emote kategorisinin icerigi yok: sekme pasif. Face = Glasses varsayimi"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Ana ekrandaki "Edit racer" (tişört) butonu kostüm editörünü açar. Sanat: `C:\Users\Monster\Downloads\Kostum_Editor_UI_v1.zip`
(README + `layout.json`, referans 1678×937, metin gömülü değil; birleşik kontrol `preview_no_text.jpg`; kullanıcının attığı ekran
görüntüsü aynı düzen). Kamera ve karakter ana ekranla aynı (T-023: Serhat'ın sahnesi, kaide); ekran yalnız UI + dimscreen.
- Sol üst Back: kaydedilmemiş değişikliği atar (önizleme kayıtlı hâle döner) ve kapatır.
- Sol sütun 4 kaynak kutusu (Mor Mücevher, Adet Bileti, Süreli Bilet, Nakit) sayılarıyla (var olan kaynak çubuğu mantığı).
- Renk paneli: SV karesi (seçili ton düz renk + white_overlay + black_overlay, README'deki sıra), ton şeridi + tutamaç, imleç, örnek
  kutusu. Seçili kategorideki öğenin birincil rengini değiştirir, karakterde anında görünür.
- İstatistik paneli (Power, Stamina dolgu maskeli, Height alanı): yalnız gösterim, değerler config'te (PDF'te yok, DECISION).
- Random (sahip olunan öğeler + palet) ve Save (seçili ön ayara yazar; kilitli öğe varken pasif).
- Kaide altı satın alma butonu (mücevher ikonu + fiyat): yalnız seçili öğe kilitli premium ise görünür; basınca `TryPurchase`; para
  yetmezse sallanma + kırmızı.
- Sağ panel: "Custom N" açılır listesi (ön ayar seçer; açık liste + satır durumları ayrı sanat), 9 sekme: Face (= Glasses), Head
  (= Headwear), Body (= Tops), Shorts, Leggings, Hands (= Gloves), Shoes, Socks, Emote (içerik yok → pasif). Eşleme config asset'inde.
  Seçili sekme kırmızı zemin + beyaz ikon (selected), diğerleri normal.
- Öğe ızgarası: 2 sütun, ScrollRect + content parent + **öğe kartı prefab'ı** (kod Instantiate eder; kalıcı UI kurmaz). Kart: normal /
  seçili zemin, seçili tik, kilit kaplaması, premium = parlak panel (sanatta yok: kart zemini altın tonlu yer tutucu). Premium'lar
  listede en üstte (PDF). İkon: Serhat'ın öğe ikonları (UI/Art/Costume/Items/<Kategori>/<ParçaAdı>.png, 600×500); öğenin ikonu = İLK parça adının PNG'si, id→Sprite eşlemesi UI tarafı config asset'inde (CostumeEditorConfig, builder katalogdan doldurur).
- Dimscreen: README'ye göre sahnenin önünde, karakter ve UI'ın arkasında (yaklaşım çalışma notunda; ör. Screen Space - Camera tuvali
  karakterin hemen arkasında).

## Kabul kriterleri
- [ ] Sanat `UI/Art/Costume/`'a PNG olarak kopyalanır; prefab builder `layout.json`'u var olan `UiArt` ile okur; Home ekran listesindeki
      kozmetik ekranı bu prefab olur
- [ ] Saf mantık sınıfları (oturum: çalışan kıyafet, değişti mi, öğe seç, kilitli öğe = önizleme, Save izinli mi, Random; HSV↔RGB)
      EditMode testli; DOTween/Zenject tipi taşımaz (TUZAKLAR)
- [ ] Ekrandaki her buton çalışır ya da açıkça pasiftir (Emote); kapat kuralları UiPanel ile
- [ ] Legacy Text, İngilizce, best fit açık (TUZAKLAR: dikeyde sığmayan Text çizilmez)
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : kart seçimi scale punch; satın alma başarılı → kart kilidi kalkar + punch; yetersiz → sallanma + kırmızı flaş (UiMotionConfig)
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Öğe ikonu üretimi (Serhat), emote içeriği, premium efektleri, istatistiklerin oynanışa etkisi
- Kozmetik çekirdeği (T-025), Home sahnesi builder'ı (T-023)

## Calisma notu
- Yapildi: 7 saf/view sinifi + model (CostumeEditorModel, HsvColor, ShakeCurve), CostumeEditorScreenView (UiScreen) ve alt gorunumler (renk secici, sekme, kart, on ayar listesi, kaydirma gostergesi, kaynak kutulari), CostumeEditorConfig asset'i + CostumeConfigSeed, UiCostumePrefabs (ekran + kart + on ayar satiri), UiPrefabBuilder'a tek satir; sanat + 72 oge ikonu Art/Costume'a kopyalandi.
- Kararlar: kart kilitliyken giyilebilir (onizleme), Save kilitli oge varken pasif; giyili karta tekrar basmak ogeyi cikarir (bos slot); yeni oge kategorinin rengini korur; Dim prefab'ta yalniz dokunma yakalayici (alfa 0), gorunur karartma sahne nesnesi (SCENE needs); Emote sekmesi config'te hasContent=false -> pasif.
- Zorlandigim: overlay tuvalde dimscreen karakteri de karartir (TUZAKLAR'a yazildi); UI modul kisayollari (DOShakeAnchorPos, DOFade, Image.DOColor) gorunmez -> sallanma/flas DOTween.To ile; dolgu cubuklari Image.Filled + tam tuval katmani, imlec/tutamac/ok isareti alfa-sinir dugumleri.
- Birakilan: WIRING (ICosmeticService/ICosmeticPreview baglari), SCENE (Canvas Dim + dimBackdrop), builder'in kopyada calistirilip yalniz 3 prefab + 1 config'in alinmasi (ASSET); .meta'lar Unity/kopya ile uretilir; Unity/test calistirilmadi.
- Acik risk: ekranlar ilk calistirmada derlenmedi (merkezi dogrulama bekler); Home ust kaynak cubugu (ResourceBar) bu ekranin arkasinda/ustunde gorunur mu bakilmadi; on ayar listesi 3 satira gore dolgulu (katalogda PresetCount degisirse arka plan hafif gerilir).
