---
id: T-075
title: Yarista botlar kossun - kosucu kopyalari, Inject ile surme, seritler, bitis sureleri, siralama, kalibrasyon araci
status: review
owner: gameplay-agent
depends_on: [T-074]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBots.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBotRunner.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneRaceBots.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/BotCalibration.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/ShotTool.cs"
touches_scene: true
branch: agent/T-074-076-rakipler
commits: []
tests: "gerekmiyor: sahne/surme, T-074 testleri + cekim + kalibrasyon"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "Race sahnesini builder ile yeniden kur (SceneBuilder.BuildRace overwrite): 'Race Bots' nesnesi (RaceBots) + adapter.bots + RaceController.body/dunya runner alanlari. Sonra ShotTool.CaptureRaceBots cekimi"
  - tip: ASSET
    durum: open
    not: "Yeni .cs dosyalarinin .meta'lari kopyada derlenip alinacak: Scripts/Bridge/RaceBots.cs, Tools/Editor/Builders/Scene/SceneRaceBots.cs, Tools/Editor/Builders/BotCalibration.cs. BotRaceConfig.asset T-074 seeder'indan (ConfigAssetSeeder). Kalibrasyon kosusundan sonra tablo config'e (-apply)"
  - tip: WIRING
    durum: open
    not: "IRaceStandings -> RaceSceneInstaller: [SerializeField] private RaceBots raceBots; Install'da raceBots varsa Container.Bind<IRaceStandings>().FromInstance(raceBots).AsSingle(). Builder SceneRaceBots 'raceBots' alanini sahnede doldurur (alan yoksa uyarir). RaceBots hem IRaceStandings'i uygular hem Changed olayini ileten tek nesnedir"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Analiz: `docs/analiz/2026-10-30-referans-oyun-rakipler.md` (OKU). Race sahnesinde oyuncunun yaninda botlar kossun. Saf plan
T-074'te (`Scripts/Core/Runtime/Race/Bots/`: `BotRaceConfig`, `BotFieldPlanner`, `BotPressSchedule`, `RaceStandingsModel`);
ayni anda yaziliyor — once onun dosyalarini oku, isimler degisirse uy. Sozlesme: `Core/Runtime/Race/IRaceStandings.cs`.

**Malik'in koduna dokunmadan** (hook kilitli; gerekirse task'a BLOCKED notu): `RunnerInput.Inject(leg, pressed, gameTime)`
public; `RunnerInput.enabled = false` klavye/dokunmayi keser ama kuyruk calisir; `RunnerBody.Update` kuyrugu her kare sim'e verir
(gelecek zamanli olay "simdi" sayilir: olayi zamani gelince, KENDI zaman damgasiyla ver — `ThemeRunPlayground.Update` demosu gibi).

1. **`RaceBots`** (+ gerekirse `RaceBotRunner`), Bridge, Tooltip'li alanlar, idempotent `Initialize()`:
   - Yaris sahnesinin kullandigi kosucu prefab'indan N bot (config) olusturur (3B kosucu, UI degil: Instantiate serbest).
     Her botta: `RunnerInput` kapali (Inject icin duruyor), `RunnerReplay`, `RunnerBodyHUD`, kamera/AudioListener, varsa
     `RaceController` kapali. Kozmetik: `CosmeticApplier`/`CosmeticCatalog` ile rastgele hazir set + ten (varsa; yoksa atla).
   - Seritler: oyuncuya gore yanal ofset (config ya da Tooltip'li alan); yan kamera (`SideFollowCamera`) oyuncuyu kapatmasin,
     cekimle dogrula. Tema dunyasi (T-072 `RaceThemeView`) yolu genis mi bak.
   - `IRaceStartSignal` Go -> her bota `BotFieldPlanner` plani + `BotPressSchedule`; kare kare zamani gelen olaylari Inject.
     Oyuncu referans suresi: Meta'daki en iyi sure servisi (etkinlige gore; ara, yoksa config fallback).
   - Bitis: bot `Sim.Distance >= yaris mesafesi` -> sure (kare ici ara degerle), basmayi birakir, bitti isaretlenir.
     Restart / Ready -> butun botlar `ResetRun`, takvim sifir.
   - `RaceStandingsModel`'i her kare oyuncu + botlarla besler; `IRaceStandings` olarak disari verir (WIRING: supervizor
     `RaceSceneInstaller`'a baglar — needs'e yaz). Oyuncunun adi/ulkesi Meta profilinden (varsa).
2. **`RaceSessionAdapter`**: `RaceOutcome.Rank` / `RunnerCount` oyuncunun bitis karesindeki siralamadan (botlar yoksa 1/1).
3. **Builder** `SceneRaceBots` (`SceneBuilder.BuildRace` icinden): `RaceBots`'u sahneye ekler, prefab + config + oyuncu
   referanslarini baglar (Find yok).
4. **Kalibrasyon araci** `BotCalibration` (Editor, `-executeMethod`): kosucu prefab'indaki `RunnerBody.tuning` ile basliksiz
   `RunnerSim` olusturur, beceri 0..1 (0.1 adim) x 20 seed icin `BotPressSchedule` olaylarini `RunnerBody.Update`'in yaptigi gibi
   sim'e verir (Advance / Press / Release), 100 m surelerini olcer; `-out <csv>` + onerilen kalibrasyon tablosu satiri yazar.
   Supervizor kopyada calistirip config'e isler.
5. **Cekim**: `ShotTool`'a botlu yaris cekimi (GO'dan ~3 sn ve ~8 sn sonra) — supervizor bakacak.

Kisitlar: Malik'in dosyalari yalniz okunur. Unity/batchmode/verify CALISTIRMA, commit atma. Calisma klasoru
`D:\Pundun\speed-starts-rakip`; ayni worktree'de T-074 (Core/Race/Bots, ConfigAssetSeeder) ve T-076 (UI) var — dokunma.
Test: bot mantigi T-074'te test ediliyor; bu task `tests: "gerekmiyor: sahne/surme, T-074 testleri + cekim + kalibrasyon"`
yazabilir.

## Kabul kriterleri
- [ ] Race'te GO'da N bot kalkar, kendi seritlerinde kosar, oyuncuyu kamerada kapatmaz (cekim)
- [ ] Bot bitirince durur ve suresi siralamaya girer; Restart'ta herkes basa doner
- [ ] RaceOutcome.Rank/RunnerCount botlarla dogru
- [ ] Kalibrasyon araci CSV yaziyor (supervizor kosacak)
- [ ] Malik'in dosyalarinda diff yok

## Geri bildirim
GORSEL  : yanimizda kosan rakipler
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
Yapildi: Bridge/RaceBots (bot kopyalari, Inject ile surme, seritler, bitis, siralama -> IRaceStandings), RaceSessionAdapter (bots alani, CurrentEventId, Rank/RunnerCount), SceneRaceBots + BuildRace cagrisi, BotCalibration (Editor, -executeMethod), ShotTool.CaptureRaceBots.
Karar: botlar prefab'tan ilk karede kurulur (tum Start'lardan sonra); inaktif tutucu altinda olusturulup RunnerInput kapatilir, HUD/Replay yok edilir, sonra aktiflenir. Sure oyuncuyla ayni: ilk basistan (RaceController gibi).
Zorlandigim: T-074 dosyalari sonradan geldi; BotSimDriver.PlanForSkill BotFieldPlanner'in skill->surucu lerp'lerini elle kopyalar (planner degisirse esitle).
Birakilan: kalibrasyon tablosunu calistirmak ve config'e islemek (supervizor), serit genisligi/kamera kapanmasi cekimle dogrulanacak, 5 ek rig performansi cihazda olculmedi.
Test yok (sahne/surme); Unity/batchmode/verify calistirilmadi, derleme dogrulanmadi.
