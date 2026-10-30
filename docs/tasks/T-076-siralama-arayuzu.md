---
id: T-076
title: Siralama arayuzu - yarista oyuncunun yeri (1st/2nd...) ve sonuc ekraninda tum kosucular (rakipler bitirdikce dolar)
status: review
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/**"
  - "Assets/Speed-Staes/Scripts/UI/Results/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRowPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Results/**"
touches_scene: true
branch: agent/T-074-076-rakipler
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceStandingsFormatTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "UiPrefabBuilder.BuildAll(overwrite: true) ile yeniden uretilecek: RaceHud.prefab (yeni Place paneli: Place/Label, RaceHudView.placeRoot+placeText bagli), ResultsTableRow.prefab (yeni Flag + CanvasGroup, ResultsTableRowView.flagImage+revealGroup), ResultsScreen.prefab (Rows artik ResultsPanel/RowsScroll/Viewport/Rows; ResultsScreenView.rowsScroll + flags[] + unknownFlag bagli). Sonra sahnedeki RaceHud ve ResultsScreen ornekleri prefab'tan guncellenir (SceneBuilder/SceneWiring). Yeniden uretimden sonra grep: placeRoot, placeText, flagImage, revealGroup, rowsScroll fileID != 0."
  - tip: SCENE
    durum: open
    not: "Shots_HomeAndRace (ShotTool, T-075 alani): yarisa 3. sirada HUD cekimi ('3rd' Place paneli gorunur) ve 7-8 kosuculu sonuc ekrani cekimi eklenmeli; ShotTool bu task'in yazma alani disinda."
  - tip: ASSET
    durum: open
    not: "Yeni .cs icin .cs.meta (kopyada derlenip GeriAl ile alinacak): Scripts/UI/Race/RaceStandingsFormat.cs, Tests/EditMode/UI/Race/RaceStandingsFormatTests.cs"
  - tip: WIRING
    durum: open
    not: "IRaceStandings (T-075 bot oturumu) -> RaceSceneInstaller: Container.Bind<IRaceStandings>() (RaceHudView.InjectStandings ve ResultsScreenView.InjectStandings [InjectOptional]); bagli degilse HUD sirasi gizli, sonuc tablosu eskisi gibi tek satir."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Analiz: `docs/analiz/2026-10-30-referans-oyun-rakipler.md` (OKU). Yarista rakip botlar geliyor (T-074/T-075, ayni anda
yaziliyor). Sozlesme hazir: `Scripts/Core/Runtime/Race/IRaceStandings.cs` (`RaceEntry`: isim, ulke kodu, oyuncu mu, mesafe,
bitti mi, sure; `Entries` sirali, `PlayerRank`, `RunnerCount`, `Changed`). Bu task yalniz arayuz:

1. **Yaris HUD'unda oyuncunun yeri**: "1st / 2nd / 3rd / 4th ..." (Ingilizce sira eki, saf bicimlendirme fonksiyonu + test),
   `Changed`'de guncellenir, degisince kucuk vurgu (DOTween, `UiMotionConfig` receteleri). Rakip yoksa gizli.
2. **Sonuc ekraninda tum kosucular**: satir prefab'i + content parent (kalici UI hiyerarsisi kodla KURULMAZ; prefab builder'da).
   Satir: sira, bayrak (mevcut bayrak sanati/`LeaderboardFlagEntry` deseni), isim, sure ("27.69"; bitmeyen "-"), oyuncu satiri
   vurgulu. Rakipler bitirdikce liste dolar (`Changed`); oyuncu bitirince ekran acilir, botlar sonra eklenir.
   Mevcut sonuc ekrani duzenini bozma; yer yoksa listeyi mevcut alana sigdir (kaydirma gerekiyorsa ScrollRect prefab'ta).
3. `IRaceStandings` `[InjectOptional]` (bot yoksa ekranlar bugunku gibi calisir). Baglama (installer) supervizorde.

Bicimlendirme ve siralama metni saf fonksiyonlarda, EditMode testli; gorsel yerlesim `gerekmiyor: gorsel` olabilir ama sira eki
ve sure metni testli. Legacy `UnityEngine.UI.Text` (TMP yok), metin Ingilizce. Prefab/sahne uretimi supervizorde (needs SCENE),
builder'a eklenen her ekran icin `Shots_HomeAndRace`'e cekim. Unity/batchmode/verify CALISTIRMA, commit atma. Calisma klasoru
`D:\Pundun\speed-starts-rakip`; ayni worktree'de T-074 (Core/Race/Bots) ve T-075 (Bridge, SceneBuilder, ShotTool) var —
onlarin dosyalarina dokunma. Kendi dosyalarini once bul (sonuc ekrani view'i, HUD view'i, builder'lari) ve owns_paths disina
yazman gerekirse task'a `BLOCKED:` notu birak.

## Kabul kriterleri
- [ ] HUD'da oyuncunun yeri dogru ekle ("1st", "2nd", "3rd", "11th", "12th", "13th", "21st"), rakip yoksa gizli
- [ ] Sonuc ekraninda tum kosucular sirayla; botlar bitirdikce eklenir; oyuncu satiri vurgulu
- [ ] EditMode: sira eki ve satir metni testleri

## Geri bildirim
GORSEL  : yer degisince HUD'da kucuk punch; sonuc satiri eklenirken kisa fade/slide (UiMotionConfig)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- Yapildi: saf `RaceStandingsFormat` (sira eki "1st/11th/21st", HUD metni, satir listesi), HUD'a `Place` paneli (Cash'in yaninda, `Changed`'de DOTween punch, rakip yoksa gizli), sonuc tablosuna bayrak + kaydirma (ScrollRect, ayni 6 satirlik pencere) + bitirdikce eklenen satirda fade/scale-in.
- Sonuc tablosu: `RunnerCount > 1` ise tum kosucular standings sirasiyla (oyuncu satiri modelin adi/suresi/PB-DB rozetleriyle), bitmeyen "-" ; yoksa eski tek satirli tablo. Satirlar yerinde yenilenir, yalniz yeni olan animasyonla gelir. Oyuncu satiri gorunur kalacak sekilde kaydirilir.
- Sure metni mevcut tablo ile tutarli "27.69s" (soneki var); istenen "27.69" ise `ResultsTexts.Time`den ayri bir cagri gerekir. Bayrak: lider tablosunun `flag_XX` ikonlari, kod bos = bayraksiz, kodu olup sanati olmayan = notr bayrak.
- Yazma alani: `UiViewBinder.cs` owns_paths disinda; HUD yeni alanlari `UiV2Prefabs.BindPlace` ile kendi SerializedObject'inden baglanir (UiViewBinder'a dokunulmadi). `UiPrefabPaths.cs` degismedi.
- Birakilan: prefab/sahne uretimi, ShotTool cekimi, installer baglamasi (needs); Unity/test kosulmadi, derleme dogrulanmadi. Adlandirilmis Place paneli `counter_panel` sanatini yeniden kullanir (ozel sanat yok).
