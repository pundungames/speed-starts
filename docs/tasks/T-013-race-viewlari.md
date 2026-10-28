---
id: T-013
title: Race view'lari (RaceHud, Results) + RaceFlow (Race sahnesi akisi)
status: merged
owner: ui-agent
depends_on: [T-005, T-010]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
touches_scene: false
branch: agent/faz2-dilim
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowLogicTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceDisplayFormatTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "RaceFlow (IInitializable), RaceHudView, ResultsScreenView -> RaceSceneInstaller, T-014 -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
  - tip: SCENE
    durum: closed
    not: "View bilesenleri RaceHud/ResultsScreen prefab'larina + [SerializeField] baglari (builder, T-014) -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Race sahnesinin akışı ve ekranları. Yarış `SpeedStars.Core.Race.IRaceSession` üzerinden okunur (uygulaması Bridge'te, T-012 — sen yalnız
arayüzü kullanırsın). Giriş `IRaceEntryGate`, seçim `IRaceSelection`, katalog `RaceEventCatalog`, sahne `SceneFlow`, ekran `ScreenNavigator`.
Metin legacy `Text`, İngilizce.

## Kabul kriterleri
- [ ] `RaceFlow` (saf C#, Zenject `IInitializable`/`IDisposable`; `SpeedStars.UI.Race`): sahne açılınca `gate.TryTakePending(out id)` →
      katalogdan mesafe → `session.RequestStart`. Bekleyen giriş yoksa (editörde Race sahnesinden doğrudan Play) seçili etkinlikle **ücretsiz**
      başlar ve bir kez uyarı loglar. `Finished` → Results ekranını açar (sonucu verir). "Race again" → `gate.Enter(aynı etkinlik)`: girdiyse
      yeniden `RequestStart`, NoTicket ise Entry Gate modalı (Race sahnesindeki örneği) ya da uyarı; "Main menu" → `SceneFlow.GoTo(Home)`.
      Akışın karar mantığı sahnesiz test edilebilir (sahte IRaceSession/IRaceEntryGate ile EditMode testi).
- [ ] `RaceHudView`: süre (`ss.cc`), mesafe/ilerleme çubuğu (Distance/TargetDistance); süre biçimi saf sınıfta, testli.
- [ ] `ResultsScreenView`: etkinlik başlığı, oyuncu süresi + "Results" listesinde tek satır (sıra 1, ad "You", süre, PB işareti — PB/WR değerleri Faz 6'ya
      kadar boş/"—"), butonlar: Watch replay (pasif), Race again, High scores (pasif bu fazda), Practice (pasif), Main menu (MD §2.1).
- [ ] `[SerializeField]` alanları prefab hiyerarşi adlarıyla eşleşir; bağlama tablosu çalışma notunda (T-014). Prefab'lara DOKUNMA.
- [ ] Geri bildirim: Results açılışı panel reçetesiyle (UiScreen/UiPopup), butonlarda punch; NoTicket'ta uyarı.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §4-§5, §10-11, `docs/TUZAKLAR.md` (test asmdef DOTween/Zenject görmez),
  `Scripts/Core/Runtime/**`, `Scripts/Meta/Contracts/**`, `Scripts/UI/Framework|Flow/**`, `Tools/Editor/Builders/Ui/UiScreenPrefabs.cs` (RaceHud/Results hiyerarşisi,
  salt okunur), `docs/gdd/referans-ekranlar/orijinal-sonuc.jpg`.
- Paralelde: başka ui-agent `Scripts/UI/Home/**` (T-011), systems-agent Meta (T-010), gameplay-agent Bridge (T-012), ui-agent builder (T-009).
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta`/`.prefab` yazma.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır + bağlama tablosu.

## Çalışma notu
- Yapılan: `Scripts/UI/Race/` — RaceFlowLogic (saf karar mantığı) + RaceFlow (ince Zenject kabuğu: IInitializable/IDisposable; asmdef'li test Zenject'i görmediği için bölündü), RaceHudView, ResultsScreenView, ResultRowView, RaceDisplayFormat (süre/ilerleme, saf), IRaceEventLookup + CatalogRaceEventLookup (RaceEventCatalog sarmalayıcı; test ScriptableObject kurmasın diye), IResultsView/IEntryGatePrompt/RaceResult.
- Testler: RaceFlowLogicTests (sahte session/gate/lookup/view, 17 durum), RaceDisplayFormatTests. Koşulmadı (Unity yasak).
- Zorlandığım: builder (UiScreenPrefabs) hâlâ TMP kullanıyor; view'ler legacy `Text`. T-014 builder'ı Text'e çevirmeli (alan tipleri Text/Image/Button).
- Bıraktığım: PB/WR "-" (Faz 6). Entry Gate modalı için `IEntryGatePrompt` seam'i açık; yoksa uyarı loglanır. Results satırı Instantiate ile (satır prefab'ında inject edilen bileşen yok).
- Not: tüm "Race again" yolu girişi gate'ten geçirir; Enter sonrası `TryTakePending` çağrılır (yoksa sonraki Enter ücretsiz kalırdı).

### Bağlama tablosu (T-014)
| View / sınıf | Alan | Prefab yolu |
|---|---|---|
| RaceHudView (RaceHud kökü, UiScreen yerine) | timeText | RaceTime/Value (Text) |
| | progressFill | Progress/Bar/Fill (Image, Filled) |
| | distanceText | Progress/Distance (Text) |
| ResultsScreenView (Results kökü, UiScreen yerine) | eventTitleText | LeftPanel/EventTitle/Label |
| | personalBestText | LeftPanel/PersonalBest/Value |
| | worldRecordText | LeftPanel/WorldRecord/Value |
| | resultsContent | LeftPanel/Results |
| | rowPrefab | HighScoreRow prefab (köküne ResultRowView) |
| | watchReplayButton / raceAgainButton / highScoresButton / practiceButton / mainMenuButton | Buttons/WatchReplay, RaceAgain, HighScores, Practice, MainMenu (her birinde ButtonFeedback) |
| ResultRowView (HighScoreRow kökü) | rankText / nameText / timeText / dateText / personalBestMark | Rank / Name / Time / Date (Text) / PbMark (GameObject) |

### Zenject bağlama ihtiyacı (RaceSceneInstaller, süpervizör)
- `Container.BindInterfacesAndSelfTo<RaceFlow>().AsSingle().NonLazy()`
- `Container.Bind<IRaceEventLookup>().To<CatalogRaceEventLookup>().AsSingle()` (RaceEventCatalog ProjectContext'ten gelir)
- `Container.Bind<IResultsView>().FromInstance(resultsScreenView)`; RaceHudView sahnede `[Inject]` alır (IRaceSession, UiMotionConfig); IRaceSession = Bridge adaptörü (T-012); IRaceEntryGate/IRaceSelection/SceneFlow ProjectContext.
- İsteğe bağlı: `IEntryGatePrompt` -> Race sahnesindeki EntryGate popup'ı açan adaptör (T-011 view'ına bağlı).
