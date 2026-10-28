---
id: T-032
title: Ekran duzeltmeleri - kaynak cubugu cakismasi, kostum editorunde kayip yazilar ve renk secici, yaris seciminde karartma
status: merged
owner: ui-agent
depends_on: [T-024, T-026, T-031]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/**"
  - "Assets/Speed-Staes/Scripts/UI/Costume/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneUiLayout.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
touches_scene: true
branch: agent/T-032-ekran-duzeltmeleri
commits: [d67419d]
tests:
  - "gerekmiyor: gorsel duzeltme; kabul PlayMode ekran goruntusu (Shots_HomeAndRace). Kaynak cubugu gorunurluk kurali saf siniftaysa EditMode testi"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Builder'lari yeniden calistir: ResourceBar + CosmeticsScreen prefab'lari ve Home sahnesi wiring (SceneWiring.WireHome ResourceBarView.homeScreen baglar; yeni alanlar CostumeTabView.label/renkler; CostumeTexts.cs.meta Unity uretir)"

started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
PlayMode ekran görüntüleri (`D:\Pundun\speed-starts\pipeline-logs\shots-t031-2026-10-28_171756\`: Home_play, Home_costume,
Home_eventselect, Home_themeselect, Home_settings, Home_entrygate) tasarımla karşılaştırıldı; düzeltilecekler:
1) **Ana ekran kaynak çubuğu (ResourceBar widget) her ekranın üstünde kalıyor.** Yarış seçiminde Sprints kartını ve 100m butonunu örtüyor;
   kostüm editöründe ekranın kendi kaynak sütununun üstüne biniyor (iki sütun). Kural: global kaynak çubuğu yalnız Home ekranı en üstteyken
   görünür; başka bir tam ekran (UiScreen) açıkken gizlenir (kostüm ve tema seçimi kendi sütunlarını gösterir; yarış seçiminde sütun yok).
   Popup'lar (ayarlar, giriş kapısı) açıkken görünür kalır. Kural tek yerde (navigator olayı + küçük bir görünürlük bileşeni ya da saf sınıf).
2) **Kostüm editöründe yazılar çizilmiyor:** sekme adları (Face, Head, Body, Shorts, Leggings, Hands, Shoes, Socks, Emote), "Color" başlığı,
   Power/Stamina/Height etiketleri, Random ve Save yazıları yok (tasarım: kullanıcının attığı görüntü + `UI/Art/Costume/preview.jpg`).
   Sebebi bul (Text hiç oluşturulmadı mı, boyut/best fit, renk, sıralama?) ve düzelt; TUZAKLAR'daki "dikeyde sığmayan Text çizilmez" kuralı.
3) **Renk seçici boş:** SV karesi ve ton şeridi koyu lacivert görünüyor. README sırası: SV iç alanına seçili tonun düz rengi, üstüne
   `color_sv_white_overlay`, sonra `color_sv_black_overlay`; frame/cursor ayrı; ton şeridi `color_hue_spectrum` + frame + handle. Seçili
   kategorinin rengi açılışta gösterilmeli (örnek kutusu dahil).
4) **Yarış seçiminde karartma yok:** tasarımda arka plan (3B sahne) yarı saydam karartılıyor (`UI/Art/EventSelect/dimscreen.png`). Ekranın
   prefab'ında karartma katmanı olmalı (bu bir tam ekran seçim ekranı; karakterin de kararması tasarıma uygun).
Builder'lar güncellenir (kod kalıcı UI kurmaz); prefab'lar süpervizör tarafından yeniden üretilir.

## Kabul kriterleri
- [ ] Shots_HomeAndRace yeni çekimlerinde: yarış seçimi ve kostüm editöründe global kaynak çubuğu yok; Home ve popup'larda var
- [ ] Kostüm editöründe bütün etiketler okunur; renk seçici seçili tonu gösterir
- [ ] Yarış seçiminde arka plan kararır
- [ ] Var olan PlayMode duman testleri yeşil (alan adları değişmez)

## Kapsam disi
- Giriş kapısı sanatı (henüz gelmedi; yer tutucu görünüm kalır), yeni özellik

## Calisma notu
1) Kaynak cubugu: ScreenNavigator.CurrentScreenChanged olayi; ResourceBarView (CanvasGroup) yalniz Home en ustteyken gorunur, popup etkilemez; homeScreen SceneWiring'de baglanir. Test: ScreenNavigatorTests.
2) Yazilar: sanatta yazi yok (README) ve UiCostumePrefabs sekme adi, Color basligi, Power/Stamina, Random/Save icin hic Text OLUSTURMUYORDU. Eklendi (CostumeTexts, best-fit Slot); secili sekmede etiket beyaz.
3) Renk secici: color_sv_frame / color_hue_frame PNG'leri tam opak lacivert plaka; kod bunlari gradyanin USTUNE koyuyordu. Simdi altta (frame, base, white, black; frame, spectrum). Gecersiz hex'te parcalar yine cizilir.
4) Karartma: EventSelectScreen.prefab'ta zaten 'Dim' (dimscreen, alfa ~%51) var ve dogru; goruntude karakter de koyu. Degisiklik yok.
