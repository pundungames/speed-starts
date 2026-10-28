---
id: T-024
title: Yaris secimi ekrani v1 - kategori kartlari (Sprints/Hurdles/Relays), mesafe, Race of the Day, Practice, Continue
status: merged
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/UI/Art/EventSelect/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/EventSelect*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/EventCategory*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/EventRowView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEventSelectPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/EventSelectConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/EventSelect*.cs"
touches_scene: true
branch: agent/T-024-yaris-secimi
commits: [0fc8f43]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/EventSelectModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "EventSelectLayout view'a SerializeField (layout) olarak baglandi, builder prefab'a atar; installer baglamasi GEREKMIYOR"
  - tip: ASSET
    durum: closed
    not: "Builder kopyada (batchmode) kosulur: UiPrefabBuilder.BuildAll ile UI/Screens/EventSelectScreen.prefab YENIDEN uretilir (diskteki eski liste surumu overwrite olmadan korunur; yalniz bu prefab icin silinip uretilmeli, Home vb. kullanici duzenlemesi ezilmesin) + ConfigAssetSeeder.SeedAll(false) ile Data/Config/EventSelectLayout.asset + yeni .cs/.png .meta dosyalari (GeriAl ile gelir). Sonra Home sahnesinde EventSelectScreen ornegi prefab'dan guncel mi bak (SceneBuilder ya da prefab ornegi) -> 0fc8f43"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Ana ekrandaki yarış seçimi butonunun açtığı ekran yeni sanatla (`C:\Users\Monster\Downloads\Yaris_Secimi_UI_v1.zip`; README ve
`layout.json` içinde, referans 1678×937, metin gömülü değil) yeniden kurulur. Ekran: Back, "SELECT EVENT" başlığı, üç kategori kartı
(Sprints, Hurdles, Relays; her kartta normal/seçili zemin + başlık + illüstrasyon + 3 mesafe butonu), alt sırada Race of the Day ve
Practice, sağ altta Continue. Aynı anda tek kart seçili; mesafe butonları yalnız seçili kartta görünür. Kategori → yarış eşlemesi bir
config asset'inde (kod sabiti değil): Sprints = 100m/200m/400m, Hurdles = 60mH/110mH/400mH, Relays = 4×100m/Medley (3. buton gizli),
Race of the Day = `race_of_the_day`, Practice = `training`. Etiketler yarışın `DisplayName`'inden gelir (legacy Text, İngilizce;
`training`'in görünen adı Türkçe "Antrenman Alanı" → ekranda "PRACTICE" yazısı sanattaki gibi sabit etiket). Oynanamayan yarış
(`IsPlayable = false`) seçilebilir görünür ama Continue onu kabul etmez (buton pasif + kısa sallanma/kırmızı geri bildirim); bugün yalnız
100m oynanabilir. Continue seçimi `IRaceSelection`'a yazar ve ekranı kapatır; Back seçimi değiştirmeden kapatır. Açılışta mevcut seçim
(kart + mesafe) seçili gelir. Dimscreen tam ekran yarı saydam katman (sahne arkada görünür).

## Kabul kriterleri
- [ ] Sanat `UI/Art/EventSelect/`'e kopyalanır (PNG'ler; `.meta` kopyada Unity üretir, GeriAl ile gelir); builder `layout.json`'u
      var olan `UiArt` okuyucusuyla okur, yerleşim %100 ölçek, saydam kenarlar kırpılmaz
- [ ] Prefab builder'dan üretilir (kod kalıcı UI kurmaz); view hazır nesneleri SerializeField ile bağlar; prefab var olan
      `UiPrefabPaths`'teki event select yolunu kullanır (Home sahnesine bağlama değişmez)
- [ ] Saf mantık sınıfı (ör. `EventSelectModel`): kategori seçimi, mesafe seçimi, Continue'nun izinli olup olmadığı, açılış seçimi;
      EditMode testi var (yarış listesi test içinde kurulur, sabit sayı assert edilmez)
- [ ] Back ve Continue kapatır (UiPanel kapat kuralı), seçili kart kırmızı çerçeve/başlık, diğerleri normal
- [ ] `docs/TUZAKLAR.md`: testler DOTween/Zenject görmez → mantık saf sınıfta
- [ ] needs listesinde open WIRING/SCENE yok (WIRING süpervizörde)

## Geri bildirim
GORSEL  : kart seçilince küçük scale punch (UiMotionConfig), oynanamayan yarışta Continue sallanır
SES     : yok (ses sistemi yok)
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Hurdles/Relays oynanışı, Race of the Day kuralları (Malik / Faz 7), bilet kuralı değişikliği
- Home ekranının diğer butonları

## Calisma notu
- Yapildi: sanat UI/Art/EventSelect'e kopyalandi; saf EventSelectModel (+15 EditMode testi), EventSelectLayout SO (kategori->yaris esleme, sabit etiketler, deny-shake sayilari) + seed; EventCategoryCardView + yeni EventSelectScreenView (UiScreen, Back = UiPanel.closeButton); UiEventSelectPrefabs builder (layout.json + PNG alfa sinirlarindan yerlesim).
- Karar: layout asset'i installer yerine view'a SerializeField (builder atar) -> WIRING yok; smoke test sozlesmesi ayni (eventSelectScreen, Back kapatir).
- Geri bildirim: kart secimi = ButtonFeedback punch (UiMotionConfig); oynanamayan yarista Continue DOShakeAnchorPos + gri 'Dimmed' katman. UiMotionConfig'te deny-shake tarifi yok (dosya owns disi): sayilar EventSelectLayout'ta.
- Birakilan: EventRowView.cs + UiRowPrefabs.EventRow + UiViewBinder.BindEventSelect + UiScreenPrefabs.EventSelect artik kullanilmiyor (owns disi oldugu icin silinmedi; EventRow prefab'i builder'da artik uretilmiyor). Bottom butonlarin secili sanati yok: kirmizi Outline ile isaretleniyor. Yeni .cs'lerin .meta'si yok (hook), kopyada uretilmeli.
- Dogrulanmadi: Unity derleme/test calistirilmadi; prefab'ta alan baglari (fileID) builder kosulduktan sonra grep'lenmeli.
