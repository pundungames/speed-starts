---
id: T-007
title: Yer tutucu UI prefab'lari - editor builder'i (16 prefab)
status: merged
owner: ui-agent
depends_on: [T-005]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/**"
touches_scene: false
branch: agent/faz1-ui
commits: []
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu + prefab'larin gozle kontrolu"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: closed
    not: "TMP Essentials (Assets/TextMesh Pro + .meta) worktree'de untracked, main HEAD'de yok. Builder TMP Settings'i Resources'tan yukler; dogrulama kopyasinda yoksa ilk adimda 'TMP Essentials are missing' ile durur (FAZ1_BUILD_FAILED). Commit'lenmeli ya da kopyaya HeadOnly senkrondan sonra elle konmali -> Faz1Builder batchmode (kopya) + GeriAl, agent/faz1-ui"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Unity MCP bu oturumda yok; prefab'lar **editörde çalışan bir builder** ile üretilir. Süpervizör builder'ı doğrulama kopyasında
batchmode'da koşturur (`SpeedStars.Tools.Builders.Faz1Builder.BuildAll`), çıkan prefab'ları `.meta`'larıyla geri alır.
Bu task yalnız builder **kodunu** yazar: `public static class UiPrefabBuilder` (namespace `SpeedStars.Tools.Builders`) ve
`public static void BuildAll(bool overwrite)`. Çıktı yolları **`UiPrefabPaths.cs`'teki sabitler** (değiştirme; ortak dosya).

Builder editör zamanında prefab üretir — bu "runtime'da UI kurma" yasağına girmez; oyun çalışırken hiçbir UI kodla kurulmaz.

## Prefab'lar (sözleşme §5, yatay 1920×1080, referans: `docs/gdd/referans-ekranlar/` + PDF ekran görüntüleri)
Kapsam kuralı: **PDF/MD'de olan girer** (`docs/YOL-HARITASI.md` "Kapsam kuralı" tablosu). Upgrade, Players, Emote, Stats, ayarlar,
banner reklam, "Unlock full version" YOK.
- `HomeScreen` (kök ekran): sağ üstte ResourceBar yeri (widget sahnede ayrı overlay'de durur, burada boşluk), "Event select: 100m"
  butonu + büyük **Play**, **Edit racer**, **High scores**, yarışçı ön ayar seçici (◀ Custom 1 ▶), **7 Günlük Ödül** ikonu + RedDot.
- `EventSelectScreen`: başlık, ScrollRect + `Content` (satır = EventRow), Back.
- `CosmeticsScreen` (Edit racer): üstte yarışçı adı, 8 kategori butonu (Head, Face, Top, Hands, Bottoms, Leggings, Socks, Shoes —
  prefab yuvaları: Headwear, Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes) + **Body** (ten rengi); öğe listesi ScrollRect + `Content`
  (CosmeticCell), renk seçici alanı (yer tutucu kare + ton çubuğu) + 5 hazır renk, fiyat etiketi, Back / Choose / Save.
- `HighScoresScreen`: başlık, etkinlik seçici, dönem seçici (Günlük / Haftalık / Tüm zamanlar), oyuncunun satırı (sıra, ad, süre, yüzde),
  ScrollRect + `Content` (HighScoreRow), Back. "Global" ve "Find player" YOK (yerel tablo).
- `RaceHud`: yarış süresi, mesafe/ilerleme çubuğu. (Duraklat yok — PDF'te yok.)
- `ResultsScreen`: sol panel — etkinlik başlığı, Personal Best, World Record, "Results" listesi `Content` (satır = HighScoreRow);
  sağ — **Watch replay, Race again, High scores, Practice, Main menu** (MD §2.1).
- `EntryGateModal`: etkinlik adı, "Adet Bileti Kullan" (+ kalan bilet), Süreli Bilet durumu, bilet edinme seçenekleri (Nakit, ödüllü reklam —
  ikisi de gri başlar), Kapat.
- `DailyGiftPopup`: "Welcome back" metni, bilet görseli, CLAIM.
- `SevenDayCheckInPanel`: 7 gün hücresi yeri (SevenDayCell × 7 — prefab örneği olarak içine konabilir), Kapat.
- Widget'lar: `ResourceBar` (Mor Mücevher · Adet Bileti `2/3` + geri sayım metni · Süreli Bilet · Nakit; her birinde ikon + sayı + "+" butonu),
  `RedDot`, `RewardFlight` (bilet ikonu).
- Satırlar: `EventRow` (ad, mesafe, biletli rozeti, kilit), `CosmeticCell` (ikon, kilit, fiyat, premium parlak panel), `SevenDayCell`
  (gün, ödül ikonu, alındı işareti), `HighScoreRow` (sıra, bayrak yeri, ad, süre, tarih/PB).

## Kabul kriterleri
- [ ] Ekran prefab kökünde `UiScreen`, popup kökünde `UiPopup` (T-005; CanvasGroup kendiliğinden); kök RectTransform tam ekran stretch.
- [ ] Her `Button` nesnesinde `ButtonFeedback`. Metinler `TextMeshProUGUI` (TMP Essentials projede: `Assets/TextMesh Pro`).
- [ ] Liste alanları: `ScrollRect` + `Viewport` + `Content` (VerticalLayoutGroup/GridLayoutGroup + ContentSizeFitter); satırlar ayrı prefab.
- [ ] Görsel yer tutucu: Unity yerleşik UI sprite'ları (`AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")` vb.),
      orijinal oyunun beyaz panel + koyu kenar + kalın yazı tarzına yakın renkler. Hiyerarşi adları anlamlı (Play, EditRacer, Content …);
      Faz 2 view'ları bunları `[SerializeField]` ile bağlayacak.
- [ ] `overwrite == false` iken var olan prefab'a dokunmaz (kullanıcının elle düzenlediği prefab ezilmez); `true` iken yeniden üretir (GUID korunur:
      `PrefabUtility.SaveAsPrefabAsset` aynı yola).
- [ ] Satır prefab'ları önce üretilir (ekranlar onları örnek olarak içerebilir: `PrefabUtility.InstantiatePrefab`).
- [ ] Kodda oyun/denge sayısı yok; yerleşim ölçüleri (piksel) builder'da olabilir.

## Kapsam dışı
- View mantığı (Faz 2), sahneye yerleştirme (T-006), ikon/efekt sanatı (Serhat sonra değiştirir).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-ui1` (worktree, dal `agent/faz1-ui`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §5, `docs/TUZAKLAR.md`, `agent-kit/05-DOTWEEN-UI-ANIMASYON.md`,
  `Assets/Speed-Staes/Scripts/UI/Framework/**` (UiScreen, UiPopup, ButtonFeedback API'si — DEĞİŞTİRME),
  `Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs` ve `Faz1Builder.cs` (DEĞİŞTİRME).
- Paralelde scene-integrator `Tools/Editor/Builders/Scene/**`'de çalışıyor (T-006): ona yazma.
- Unity, batchmode, `unity-verify` YASAK (builder'ı süpervizör koşturur). `git commit` YAPMA. `.meta`/`.prefab` yazma — yalnız `.cs`.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- `Tools/Editor/Builders/Ui/` altına 4 dosya: `UiPrefabBuilder` (BuildAll: önizleme sahnesinde kurar, `SaveAsPrefabAsset` aynı yola = GUID korunur; overwrite=false iken dosya varsa dokunmaz ve iç içe koymak için onu kullanır), `UiBuildKit` (düğüm/yerleşim/panel/etiket/buton/liste yardımcıları), `UiRowPrefabs` (4 satır + 3 widget), `UiScreenPrefabs` (6 ekran + 3 popup). Sıra: satırlar → widget'lar → ekranlar → popup'lar; Home'a RedDot, 7 gün paneline 7 SevenDayCell örneği gömülü (ad/metin override'ı `RecordPrefabInstancePropertyModifications` ile). Liste Content'leri boş (view doldurur).
- Unity açılmadı; Unity'nin kendi Roslyn'i (`DotNetSdkRoslyn/csc.dll`) ile UI Framework + builder, gerçek Unity 2022.3.62 / uGUI / TMP 3.0.7 / DOTween / Zenject DLL'lerine karşı derlendi: 0 hata, 0 uyarı. Çalışma anı (prefab kaydı, yerleşimin görüntüsü) koşulmadı: süpervizörün batchmode koşusu + gözle kontrol.
- Zorlanılan: TMP statik atlası (LiberationSans SDF) ü/Ö/ç içeriyor, ğ/ı/ş/İ yok → metinler buna göre seçildi (dönemler "Daily / Weekly / All time"; "Haftalık" ı içerir). Geniş telefonda kanvas ~2120×978 (match 0.5) olduğu için Cosmetics yerleşimi kısaltıldı.
- Yer tutucu bırakılanlar: Body + 8 kategori (Arms yok); ItemPanel kapalı başlar (view CategoryPanel ile değiştirir); durum işaretleri (Lock, Claimed, PremiumGlow, PbMark) kapalı; Nakit/reklam butonları interactable=false; renk seçici kare/ton çubuğu düz renk (gradyan yok), 5 hazır renk prefab'ta. Sabit buton metinleri prefab'ta (projede string tablosu yok).
- Açık: TMP Essentials kopyada olmalı (needs ASSET). Faz 2 view'ları alanları `[SerializeField]` ile bağlayacak; şu an bağlanacak view alanı yok.
