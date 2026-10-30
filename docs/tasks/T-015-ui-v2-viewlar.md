---
id: T-015
title: UI v2 view'lari - kapat butonu (UiPanel), Home yeni alanlar, ResourceBar, RaceHud telemetri + Exit/Restart
status: merged
owner: ui-agent
depends_on: [T-011, T-013]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/**"
touches_scene: false
branch: agent/ui-v2
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/PanelCloseRuleTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowLogicTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceDisplayFormatTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "IRaceTelemetry -> RaceSceneInstaller (Bridge adaptoru), supervizor -> RaceSceneInstaller IRaceSession+IRaceTelemetry ayni adaptor"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Kullanıcı hatası: "açılan UI'lar kapat'a basınca kapanmıyor" — düz `UiScreen`/`UiPopup` ekranlarında (7 gün, Cosmetics, High Scores) Close/Back
butonunu dinleyen kod yok. Ayrıca yeni UI tasarımı (UI v2) geldi. **İsim sözleşmesi:** `docs/tasks/UI-v2-ISIMLER.md` — view alan adları ORADAKİ gibi
(builder T-016 aynı adlarla bağlayacak). Metin legacy `Text`, İngilizce.

## Kabul kriterleri
- [ ] `UiPanel`'e opsiyonel `[SerializeField] Button closeButton` (Tooltip): popup → `navigator.ClosePopup(this)`, ekran → `navigator.Back()`.
      `ScreenNavigator` sahne başına bağlı (SceneUiInstaller); enjeksiyon opsiyonel olabilir (`[InjectOptional]`) ki navigator'süz test sahnesi kırılmasın.
      Kendi kapatması olan view'larda (EntryGateModalView, EventSelectScreenView) taban alan boş kalacak — çift tetik olmasın.
- [ ] `HomeScreenView`: sözleşmedeki yeni alanlar (settings, sound, environment, upgrade, players, emote, previousRacer, nextRacer butonları;
      `racerNameText`, `powerFill`, `staminaFill`, `heightText`, `sevenDayRedDot`). Sistemi olmayan butonlar açılışta `interactable=false`
      (PDF'te tanımı yok; görsel tasarımda var). Mevcut davranış (Play, RaceSelect, EditRacer, HighScores, 7 gün) aynen.
- [ ] `ResourceBarView`: sözleşmedeki alanlarla eşleşir (fark varsa sözleşmeyi KODA göre değil, view'ı sözleşmeye göre ayarla; tablo dışı alan
      gerekirse çalışma notuna). Bilet doluyken `entryTimerRoot` gizli.
- [ ] `RaceHudView`: `IRaceTelemetry` (Core, `Scripts/Core/Runtime/Race/IRaceTelemetry.cs`) enjekte edilir; hız "x.x m/s", enerji fill, cadence işaretçisi
      `cadenceTrack` genişliğinde `PressesPerSecond / cadenceMaxPressesPerSecond` (view alanı, Tooltip, clamp 0..1); Nakit sayacı `IWallet.Cash`.
      Progress alanları opsiyonel (null-safe). `exitButton` → Main menu, `restartButton` → RaceFlow'un Race again yolu (gate.Enter; bilet iadesi yok).
- [ ] RaceFlow/RaceFlowLogic: Exit ve Restart (yarış sürerken de) — mantık saf sınıfta, EditMode testi.
- [ ] UiPanel close mantığı testi (saf kısım: popup mu ekran mı kararı) ve mevcut 196 test kırılmaz.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-uiv2` (worktree, dal `agent/ui-v2`). Ana klasöre yazma.
- Paralelde: ui-agent builder (`Tools/Editor/Builders/Ui/**`, T-016), gameplay-agent Bridge (`Scripts/Bridge/**`, T-017), test-agent
  (`Tests/PlayMode/**`, T-018), scene-integrator (`Tools/Editor/Builders/Scene/**`, T-019).
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta`/`.prefab` yazma. Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle yapılabilir
  (referans DLL'ler: `D:\Pundun\speed-starts-verify-1\Library\ScriptAssemblies`).
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır + sözleşmeden sapmalar.

## Çalışma notu
Yapilan: UiPanel.closeButton (+PanelCloseRule saf kural, UiPopup.IsPopupPanel), HomeScreenView yeni alanlar, ResourceBarView alan adlari sozlesmeye, RaceHudView telemetri/cash/exit/restart, RaceFlowLogic.Restart/Exit + IRaceHudActions (RaceFlow uygular).
Derleme: Unity'nin Roslyn'i ile Core+Meta+UI+EditMode testleri kaynaktan derlendi, 0 hata (test KOSULMADI, yalniz derleme). Zenject icin Assets/Plugins/Zenject/Source/Usage/Zenject-usage.dll ref gerekir.
Sapma 1: EntryGateModalView kendi `closeButton` alanini kaldirdi; taban UiPanel.closeButton ayni adla onu kapatir (iki ayni adli private SerializeField Unity'de carpisir). Sozlesmedeki "taban alan bos kalacak" bu view icin tersine: taban alan DOLU, kendi alani yok. Mevcut prefab degeri ayni anahtardan gelir.
Sapma 2: RaceHudView -> flow baglantisi IRaceHudActions arayuzuyle; RaceFlow BindInterfacesAndSelfTo oldugu icin installer degismez. IRaceTelemetry/IWallet/IRaceHudActions [InjectOptional]; IRaceTelemetry baglanana dek hiz/enerji/cadence durur (WIRING acik).
Sapma 3: cadenceMarker konumu cadenceTrack.rect uzerinden dunya konumuyla (anchor'dan bagimsiz); cadenceMaxPressesPerSecond varsayilan 10 (view alani, Tooltip). Eski builder (UiViewBinder) eski alan adlarini (RaceHud progress, ResourceBar Amount...) hala kullaniyor: T-016 yeni adlara gecmeli. sevenDayRedDot yalniz baglanir (surucusu yok; PDF'te kural yok).
