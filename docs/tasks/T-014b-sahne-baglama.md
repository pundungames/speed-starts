---
id: T-014b
title: Sahne builder'i - Race'te RaceSession adapter + Dev, installer alanlari, Home'da ekranlar arasi referanslar
status: merged
owner: scene-integrator
depends_on: [T-011, T-012, T-013]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/**"
touches_scene: true
branch: agent/faz2-dilim
commits: []
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu (overwrite) + Home/Race Play"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`SceneBuilder`/`SceneUiLayout` (T-006) Faz 2 için sahne nesnelerini ve **sahneler arası değil, sahne içindeki** referansları bağlar. Prefab içi
alanları T-014a bağlar (prefab builder). Installer'lar hazır (süpervizör yazdı, `Scripts/Core/Installers/**` — DEĞİŞTİRME):
`RaceSceneInstaller` alanları `raceSession` (RaceSessionAdapter), `results` (ResultsScreenView); `HomeSceneInstaller` ek alan yok.

## Kabul kriterleri
- [ ] **Race.unity**: kökte `RaceSession` nesnesi + `SpeedStars.Bridge.RaceSessionAdapter` (`race` → sahnedeki `RaceController`, `body` → koşucu
      örneğindeki `RunnerBody`) + `SpeedStars.Bridge.DevRaceFinish` (`adapter` → aynı nesne). Bağlama tablosu: `docs/tasks/T-012-bridge-race-session.md`.
      `RaceSceneInstaller.raceSession` → bu adapter; `results` → sahnedeki ResultsScreen örneğinin `ResultsScreenView`'ı; `rootScreen` → RaceHud örneği.
- [ ] **Home.unity**: `HomeScreenView`'ın sahne nesnesine giden alanları (EventSelect / Cosmetics / HighScores ekranları, 7 gün popup'ı, EntryGate modalı —
      adlar view kodunda `Scripts/UI/Home/HomeScreenView.cs`) sahnedeki örneklere bağlanır. T-014a'nın "sahneye kalan alanlar" listesi de buraya.
      Race'teki EntryGate örneği için de aynı (varsa sahne referansı alanları).
- [ ] Alan adları `SerializedObject` ile koddan; bulunamayan alan açık hatayla durdurur.
- [ ] Malik'in dosyaları değişmez (Race, PhysicalTest'in kopyası olarak kalır; override'lar sahnede).
- [ ] `overwrite` true iken Race yeniden kopyalanır + Faz 1 ve Faz 2 eklemeleri birlikte kurulur (tek akış).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `.claude/agents/scene-integrator.md` (ana klasörde), `docs/tasks/T-011|T-012|T-013*.md` çalışma notları, `Scripts/UI/Home|Race/**`,
  `Scripts/Bridge/**`, `Scripts/Core/Installers/**`, mevcut `Tools/Editor/Builders/Scene/*.cs`.
- Paralelde ui-agent `Tools/Editor/Builders/Ui/**`'de (T-014a). Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs`.
  Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle yapılabilir.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
1. Yeni `Tools/Editor/Builders/Scene/SceneWiring.cs` (WireHome/WireRace); `SceneBuilder.BuildHome/BuildRace` Save'den once cagirir (tek akis, overwrite'ta Faz 1 + Faz 2 birlikte).
2. Home: HomeScreenView -> eventSelectScreen, entryGateModal, cosmeticsScreen, highScoresScreen, sevenDayPopup (kod alan adlari; tip ve kok bilesen kontrollu, prefab kaynak varligiyla bulunur). T-014a listesi okunmadi (bos); alanlar view kodundan cikarildi.
3. Race: kok `RaceSession` nesnesi + RaceSessionAdapter (race, body) + DevRaceFinish (adapter); RaceSceneInstaller raceSession/results/rootScreen(RaceHud). EntryGate'in sahne referansi alani yok.
4. Eksik alan/bilesen/nesne InvalidOperationException ile adini soyleyerek durdurur. Derleme kontrolu yapilmadi (Unity/Roslyn kosulmadi).
5. Risk: DevRaceFinish sahneye kaydedilir; release build'de bilesen derlenmedigi icin "missing script" olur (T-012 `#if`). RunnerBody/RaceController birden fazlaysa ilki + uyari.
