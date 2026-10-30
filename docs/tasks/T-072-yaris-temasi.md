---
id: T-072
title: Yaris sahnesinde ana menude secilen tema (Serhat'in akan dunyasi, aktif temaya gore)
status: review
owner: gameplay-agent
depends_on: [T-070]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/RaceThemeView.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Themes/RaceThemeSlots.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneRaceTheme.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/ShotTool.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/RaceThemeSlotsTests.cs"
touches_scene: true
branch: agent/T-072-073
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/RaceThemeSlotsTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "Race.unity yeniden uretilmeli (SceneBuilder.BuildRace overwrite): 'Race Worlds' (RaceThemeView + Standard City + 4 ThemedLoopWay), Track kapali. Sonra ShotTool.CaptureRaceThemes ile 5 cekim."
  - tip: ASSET
    durum: open
    not: "Yeni .cs dosyalari icin .meta Unity'den gelir (RaceThemeView, RaceThemeSlots, SceneRaceTheme, RaceThemeSlotsTests) - kopyada derleyip .meta'yi al (TUZAKLAR 183)."

started:
finished:
blocked_reason:
---

## Hedef
Kullanici (30.10): "kosu sahnesine bizim ana menude sectigimiz arka planlari temayi ekle". Bugun `Scenes/Game/Race.unity` (Malik'in
PhysicalTest kopyasi, `SceneBuilder.BuildRace`) duz zemin + serit cizgileri + bitis cizgisiyle aciliyor; tema hic yok. Ana menude secilen
(kaydedilen) tema `IThemeService.ActiveShowcaseIndex` (ProjectContext'te bagli, Race'te de inject edilir): 0 Standart Sehir, 1 Misir,
2 Roma/Bizans, 3 Deniz/Korsan, 4 Gokyuzu Adalari (`MainMenuThemeShowcase` ve `ThemeRunPlayground` ile ayni sira).

Serhat'in akan dunyalari hazir ve kosucuyla calisiyor (`Scenes/Test/ThemeRunPlayground.unity` + `Scripts/Themes/ThemeRunPlayground.cs`):
standart sehir = yapilandirilmis bir `LoopWayGenerator` (ayarlari sahnedeki bilesende, HideInInspector), diger dordu = `ThemedLoopWay`
+ `Prefabs/Themes/<Konsept>/Definition.asset`. `ThemeRunPlayground.SelectTheme` birini acip digerlerini kapatir, `runner`'i verir,
kosucunun z'sine gore konumlar, `Generate()` cagirir. Ayni seyi yaris sahnesinde aktif temaya gore yap:

1. **Saf secim kurali** (test edilebilir, asmdef'li tarafta: `SpeedStars.Meta.Themes.RaceThemeSlots`): showcase indeksi -> dunya yuvasi
   (0 = standart sehir, 1..4 = themed dizideki 0..3), aralik disi -> standart (0). Yuva sayisi parametre, sabit 5 assert etme.
2. **`SpeedStars.Bridge.RaceThemeView`** (MonoBehaviour, Tooltip'li `[SerializeField] private` alanlar): `[Inject] IThemeService`; yarisin
   basinda aktif temanin dunyasini acar, digerlerini kapatir, `runner` = sahnedeki kosucu (referans Inspector/builder'dan, Find yok),
   ThemeRunPlayground'daki konumlama + `Generate()`. Yaris yeniden baslayinca (kosucu basa donunce; `RaceSessionAdapter`/`IRaceSession`
   yeniden baslatma yolunu bul) dunya da bastan kurulur. Idempotent `Initialize()` (Awake/OnEnable/Construct; TUZAKLAR 1).
3. **Builder** `SceneRaceTheme` (`SceneBuilder.BuildRace` icinden): dunya nesnelerini Race sahnesine getirir — yapilandirilmis
   `LoopWayGenerator`'i ThemeRunPlayground sahnesinden uretilmis cocuklari OLMADAN kopyala (ya da hazir prefab varsa onu kullan), her
   Definition icin bir `ThemedLoopWay`, `RaceThemeView` bagli. Serhat'in sahne dosyasi ASLA yazilmaz (yalniz okunur/kopyalanir). Duz
   `Ground`/`Track`/serit cizgileri temanin yolunu kapatiyor ya da ustune biniyorsa kapat; `FinishLine` gorunur kalsin. Kosucunun x'i ile
   tema yolunun x'i (temalarda yol x=-5.63) uyusuyor mu olc; ayni hizada degilse dunya kokunu kaydir (kosucuyu/Malik'in kodunu degil).
4. **Cekim**: `ShotTool`'a yaris sahnesi icin her tema bir cekim (`Race_theme_<n>`), kosucu birkac metre ilerlemisken; supervizor bakacak.

Kisitlar: Malik'in dosyalari (Scripts/Physical, Runner/RunnerInput, SideFollowCamera, PhysicalTest) hook'la kilitli — dokunma. Serhat'in
`Scripts/Themes`, `Scripts/Runner/LoopWay*` dosyalarini da DEGISTIRME; gerekiyorsa task'a `BLOCKED:` notu birak (kullanici izin verdi ama
once supervizor bakar). Unity/batchmode/verify CALISTIRMA; sahne uretimi supervizorde (needs SCENE). Calisma klasoru:
`D:\Pundun\speed-starts-tema` (worktree, dal `agent/T-072-073`). Commit atma; supervizor atar. Ayni worktree'de T-073 calisiyor
(`MenuCharacterSpin`, `SceneHomeCharacter.cs`) — onun dosyalarina dokunma.

## Kabul kriterleri
- [ ] Ana menude tema X secili -> Race acilinca X'in dunyasi gorunur (5 temanin her biri icin cekim), digerleri kapali
- [ ] Yaris yeniden baslayinca dunya bastan, kosucu yolun uzerinde; bitis cizgisi gorunur
- [ ] EditMode: secim kurali (aralik ici/disi) testi; sabit tema sayisi assert etmiyor
- [ ] Serhat'in ve Malik'in dosyalarinda diff yok

## Geri bildirim
GORSEL  : yarista secilen temanin dunyasi
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Tema ekraninin kendisi, fiyat/sahiplik, rakip kosucular, kamera ayari

## Calisma notu
- Yapildi: RaceThemeSlots (saf secim) + testi, RaceThemeView (Bridge; ThemeRunPlayground.SelectTheme mantigi, RunnerBody.RunReset'te dunyayi bastan kurar), SceneRaceTheme (builder), BuildRace'e cagri, ShotTool.CaptureRaceThemes (race_theme_0..4).
- Standart sehir: Serhat'in yapilandirilmis `LoopWay.prefab`i (Kevin Iglesias/.../Models) instance'i, loopCount 3; 4 tema: Definition.asset basina bir ThemedLoopWay (sira Egypt, Rome, Pirate, Sky = enum sirasi, builder dogruluyor). Hepsi kapali baslar, view acar.
- Hizalama: oyuncu x=-1.184, dunya yolu x=-5.63 icin yazilmis; view dunyayi (runner.x - authoredRunnerX) kadar kaydirir (Serhat/Malik kodu degismedi). Olculmedi (Unity calistirilmadi): cekimlerde yol kosucunun altinda mi bakilmali.
- Duz `Track` (Ground, Ground(1), serit cizgileri) kapatildi; FinishLine (y=0.005, genislik 12, x=0) acik: tema yolunun ustunde z-fight/gorunmeme riski, cekimde bakilacak.
- Zorlandigim/birakilan: sahne uretimi ve cekim supervizorde (needs); isik/gokyuzu temaya gore degismiyor (kapsam disi); LoopWayGenerator.Start kendi Generate'ini bir kez daha cagirir (zararsiz).
