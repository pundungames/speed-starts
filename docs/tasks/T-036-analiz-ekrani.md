---
id: T-036
title: Analiz ekrani (Analiz_UI_v1) - mesafe/sure grafigi gercek yaris verisinden, siralama, imlec balonu; sonuc ekranindaki Stats acar
status: todo
owner: ui-agent
depends_on: [T-033]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Analysis/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/Analysis/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/Results*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceViewContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiAnalysisPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/AnalysisConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-036-analiz
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceSplitRecorderTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/AnalysisChartModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Yarış sonu ekranındaki (T-033) pasif **Stats** butonu Analiz ekranını açar; Back sonuç ekranına döner. Sanat:
`C:\Users\Monster\Downloads\Analiz_UI_v1.zip` (açılmış hâli süpervizör scratchpad'inde `analiz-ui\`: README.md, **IMPLEMENTATION.md = bağlayıcı
davranış**, layout.json (referans 1678×937, `chart_plot_local` [71,80,971,418], altı çizgi rengi), approved_design.jpg, preview_no_text.jpg;
build.py alınmaz). Ekran: sol üst Nakit (elmas ikonu) + Back, kırmızı "ANALYSIS" başlığı, solda grafik (Y = kümülatif süre s, X = mesafe m),
sağda sıralama (yarış adı, POS/PLAYER/TIME, renk çizgisi, YOU vurgusu).

Kararlar (IMPLEMENTATION.md'ye uygun; süpervizör kararı):
- **Veri**: yarış sırasında oyuncunun (mesafe, kümülatif süre) örnekleri `IRaceSession` (RaceTime, Distance) üzerinden saf bir
  `RaceSplitRecorder`'la toplanır (örnekleme adımı metre cinsinden config'te; başlangıç 0,0 ve bitiş noktası dahil). Son yarışın serileri
  oturum boyunca tutulur (kayda yazılmaz). Rakip serisi bugün yok (Malik'in NPC'leri) → yalnız oyuncu çizgisi; liste koşucu listesi
  alacak şekilde (runner id → renk, sıra değişse de renk sabit). Bitirmeyen = DNF, süre uydurulmaz; eksik örnek 0 sayılmaz.
- **Eksenler**: X üst sınırı etkinliğin mesafesi (100m'de 100, 110mH'de 110); Y üst sınırı tüm bitirenleri kapsayan yuvarlak değer; tick
  aralıkları çalışma anında hesaplanır (saf `AnalysisChartModel`, testli). Tick etiketleri builder'ın kurduğu sabit havuz Text'leri, kod
  yerleştirir/doldurur.
- **Çizim**: çizgiler ve noktalar UI mesh (MaskableGraphic.OnPopulateMesh) ile, renk serinin rengi; plot alanı dışına taşmaz. Izgara
  sprite'ı yalnız şablon — gerekirse ızgara da mesh'te.
- **İmleç**: grafiğe dokunma/sürükleme en yakın örneği seçer (seçili seri; varsayılan oyuncu, sağdaki satıra dokununca o seri);
  guide yalnız X'te, marker noktada, balon noktanın üstünde, grafik sınırına clamp; metin "60 m / 10.42 s".
- **Sıralama**: gerçek bitiş sırası; YOU vurgusu oyuncunun satırında; satır prefab'ı + content parent (6'dan fazlası için kaydırma).
- Analiz ekranı Race sahnesinde bir UiScreen; Sonuç → Stats → Analiz → Back → Sonuç (navigator).

## Kabul kriterleri
- [ ] Gerçek yarışta grafik oyuncunun eğrisini çizer, son nokta sonuç tablosundaki süreyle aynı; 0 süreli/bitmemiş yarışta DNF/boş durum
- [ ] EditMode: recorder (adım, interpolasyon, başlangıç/bitiş, eksik örnek), chart model (eksen üst sınırları, tick'ler, normalize, en yakın nokta)
- [ ] PlayMode: Sonuç → Stats → Analiz açılır → Back döner (gerçek ışınlı Tap); ekran görüntüsü testine `Race_analysis` çekimi
- [ ] Legacy Text, İngilizce, best fit; DOTween yalnız çekirdek; kod kalıcı UI kurmaz; dokunma alanları görünür bölge (TUZAKLAR)

## Geri bildirim
GORSEL  : ekran açılınca çizgi soldan sağa çizilir (UiMotionConfig süresiyle), balon yer değiştirince küçük punch
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Rakip koşucular / ghost (Malik), kişisel rekor serisini kaydetmek, tekrar (replay)

## Calisma notu
(Agent doldurur)
