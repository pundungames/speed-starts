---
id: T-074
title: Rakip botlarin plani (saf C#) - config, saha plani, basis takvimi, canli siralama modeli
status: todo
owner: systems-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/Bots/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/BotRaceConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/**"
touches_scene: false
branch: agent/T-074-076-rakipler
commits: []
tests: []
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici (30.10): referans oyun analizinden sonra "yapalim". Analiz: `docs/analiz/2026-10-30-referans-oyun-rakipler.md` (OKU).
Yarista rakip botlar olacak: her bot bizim kosucu fizigini (Malik'in RunnerSim'i) basis ureten bir surucuyle kosar. Bu task
yalniz SAF C# tarafi (Core asmdef, test edilebilir); sahnede kosturma T-075, arayuz T-076. Ortak sozlesme hazir:
`Scripts/Core/Runtime/Race/IRaceStandings.cs` (`RaceEntry`, `IRaceStandings`) — degistirme, gerekirse not dus.

1. **`BotRaceConfig`** (ScriptableObject, her alan Tooltip'li `[SerializeField] private` + okuma property'si): bot sayisi
   (vars. 5), `difficulty` 0..1, `skillRange` 0..1, oyuncu suresi yoksa `fallbackReferenceTime` (sn), zorluk -> hedef sure
   eslemesi (merkez = referans x lerp(kolayCarpan, zorCarpan, difficulty); yayilim = merkez x lerp(darYayilim, genisYayilim,
   skillRange)), yaris basi rastgelelik orani (referansta ayni bot iki yarista 1-7 sn farkli kostu), surucu araliklari
   (tepki sn min/max, basis periyodu en iyi/en kotu, tutma suresi, zamanlama titremesi ms, hata olasiligi), **kalibrasyon
   tablosu** (beceri 0..1 -> beklenen 100 m suresi; T-075'in araci doldurur, simdilik monoton yer tutucu), isim+ulke listesi
   (~30 KENDI uydurdugumuz uluslararasi isim + ISO ulke kodu; referans oyunun bot isimlerini KULLANMA).
   Seed: `BotRaceConfigSeed` + `ConfigAssetSeeder`'a kayit (mevcut seed'lerin desenine uy; asset yolu oradaki gibi).
2. **`BotFieldPlanner`** (saf): `Plan(config, oyuncuReferansSuresi?, System.Random)` -> `IReadOnlyList<BotPlan>`
   (isim, ulke, hedefSure, beceri, tepki, periyot, tutma, titreme, hataOrani). Hedef sureden beceri, kalibrasyon tablosunun
   tersiyle (dogrusal ara deger). Ayni yarista isim tekrarlanmaz. Ayni seed = ayni plan.
3. **`BotPressSchedule`** (saf): `BotPlan` + `System.Random` -> GO'dan itibaren basis/birakma olaylari (bacak 0/1 sirayla,
   basis zamani, birakma = basis + tutma), ilk basis >= tepki, zamanlar monoton, titreme sinirli, hata = ara sira gec ya da
   erken basis. Kosucu fiziginin sevdigi duzen icin bak: `Scripts/Themes/ThemeRunPlayground.cs` demo (0.32 sn arayla sirali
   basis, 0.10 sn tutma) ve `CLAUDE.md` "Son olcumler" (ritim 0.32 sn iyi; cok sik basmak basi one egip yavaslatir). En iyi
   periyodun varsayilani buna yakin olsun; kesin sayilar kalibrasyonla gelecek.
4. **`RaceStandingsModel`** (saf, `IRaceStandings`'i uygular): her kare girdi listesi (oyuncu + botlar: isim, ulke, mesafe,
   bitti mi, sure) alir; siralama = bitenler sureye gore, sonra kosanlar mesafeye gore; `PlayerRank`, `RunnerCount`;
   `Changed` yalniz sira ya da bitis degisince. `Reset()` baslangica dondurur.

Kisitlar: SAF C#, `SpeedStars.Core` asmdef'i (Malik'in Assembly-CSharp'ina referans YOK; RunnerSim'i burada kullanma).
Magic number yok: sayilar config'te. Unity/batchmode/verify CALISTIRMA, commit atma. Calisma klasoru:
`D:\Pundun\speed-starts-rakip` (dal `agent/T-074-076-rakipler`); ayni worktree'de T-075 (Bridge, SceneBuilder, ShotTool,
RaceSessionAdapter) ve T-076 (UI) calisiyor — onlarin dosyalarina dokunma.

## Kabul kriterleri
- [ ] EditMode: zorluk artinca hedef sureler kisalir; skillRange artinca yayilim genisler; bot sayisi tutar; isimler tekil;
      ayni seed ayni plan (iliski assert'i, sabit sayi yok)
- [ ] EditMode: takvim bacak sirasi, ilk basis >= tepki, monoton zaman, birakma basistan sonra
- [ ] EditMode: siralama (biten once, sureyle; kosan mesafeyle), PlayerRank, Changed yalniz degisimde
- [ ] Config seed'i mevcut seeder'a bagli; asset uretimi supervizorde (needs ASSET)

## Kapsam disi
- Sahnede kosturma, kalibrasyon araci (T-075), arayuz (T-076), ayar ekranindan zorluk secimi

## Calisma notu
(Agent doldurur: 5 satir)
