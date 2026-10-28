---
id: T-024
title: Yaris secimi ekrani v1 - kategori kartlari (Sprints/Hurdles/Relays), mesafe, Race of the Day, Practice, Continue
status: todo
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
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/EventSelectModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: EventSelectLayout config asset'i bind (ya da view'a SerializeField; hangisi secildiyse calisma notunda)"
started:
finished:
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
(Agent doldurur)
