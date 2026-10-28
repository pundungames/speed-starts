---
id: T-026
title: Kostum editoru ekrani (Edit racer) - kategori, oge izgarasi, renk secici, on ayar, Random/Save, satin alma
status: todo
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
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "Home: cosmeticsScreen = CostumeEditorScreen prefab'i; view ICosmeticService/ICosmeticPreview/IWallet/IEntryTickets alir (installer supervizorde)"
  - tip: DECISION
    durum: open
    not: "Power/Stamina/Height PDF'te yok: panel yalniz gosterim (config'teki yer tutucu degerler). Emote kategorisinin icerigi yok: sekme pasif. Face = Glasses varsayimi"
started:
finished:
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
  listede en üstte (PDF). İkon: öğe ikonları yok (Serhat) → kategori ikonu öğenin varsayılan rengiyle boyanmış yer tutucu.
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
(Agent doldurur)
