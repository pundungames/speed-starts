---
id: T-011
title: Home view'lari (HomeScreen, EventSelect + EventRow, EntryGate modal, ResourceBar)
status: merged
owner: ui-agent
depends_on: [T-005, T-010]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/**"
touches_scene: false
branch: agent/faz2-dilim
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/HomeLogicTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "Home view'lari -> HomeSceneInstaller (factory/QueueForInject), T-014 -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
  - tip: SCENE
    durum: closed
    not: "View bilesenleri Home prefab'larina eklenir ve [SerializeField] alanlari hiyerarsi adlariyla baglanir (builder, T-014) -> T-014a/b builder + Installers (agent/faz2-dilim), Zenject validate 2 sahne OK"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Home sahnesindeki ekranların davranışı. Prefab'lar hazır (`Assets/Speed-Staes/UI/`, builder: `Tools/Editor/Builders/Ui/`); view'lar onları
**bağlar ve doldurur, üretmez** (satır prefab'ı + Content dışında). Servisler `SpeedStars.Meta.Contracts` arayüzlerinden (`MetaContracts.cs`):
`IEntryTickets`, `IWallet`, `IRaceSelection`, `IRaceEntryGate`; `RaceEventCatalog`; `ScreenNavigator`, `SceneFlow` (T-005). Enjeksiyon
`[Inject] void Construct(...)` (UI asmdef Zenject'i görür). Metin **legacy `UnityEngine.UI.Text`** (TMP yok), İngilizce.

## Kabul kriterleri
- [ ] `HomeScreenView` (`SpeedStars.UI.Home`): Play → seçili etkinlik biletliyse EntryGate modalını açar, değilse doğrudan `gate.Enter` → başarılıysa
      `SceneFlow.GoTo(GameScene.Race)`; "Event select" etiketi seçili etkinliğin adını gösterir, tıklayınca EventSelect ekranı (navigator.Push);
      Edit racer → Cosmetics ekranı, High scores → HighScores ekranı, 7-Day ikonu → 7 gün popup'ı (bu üçünün içi Faz 3+; yalnız açılır).
- [ ] `EventSelectScreenView` + `EventRowView`: katalogdaki etkinlikleri satır prefab'ıyla `Content` altına dizer (bir kez, açılışta; Zenject factory
      ya da `container.InstantiatePrefabForComponent`), biletli rozeti ve oynanamaz kilidi; oynanabilir satır → `selection.Select` + geri.
- [ ] `EntryGateModalView`: etkinlik adı, kalan bilet "2/3", "Use Entry Ticket" (bilet yoksa pasif), Süreli erişim aktifse bilet butonu gizli ve
      "Timed Ticket active" bilgisi (PDF §1.1), bilet edinme butonları (Nakit / ödüllü reklam) pasif (fiyat yok); onay → `gate.Enter` → sonuca göre Race'e
      geçiş ya da uyarı. Kapat.
- [ ] `ResourceBarView`: Mor Mücevher, Adet Bileti `n/cap` + yenilenmeye geri sayım (dolu ise sayaç gizli), Süreli Bilet sayısı/aktif, Nakit;
      saniyede bir `IEntryTickets.Refresh()` (Zenject `ITickable` sınıfı ya da view içinde zaman biriktirme — `Update`'te GetComponent yok).
- [ ] Geri sayım biçimi saf bir sınıfta (`mm:ss`, saat varsa `h:mm:ss`) ve EditMode testi var; seçim/kapı akışının UI'dan bağımsız mantığı
      (ör. "Play'e basınca ne olur" kararı) testlenebilir saf sınıfta.
- [ ] Her view'ın `[SerializeField]` alanları prefab hiyerarşi adlarıyla eşleşecek şekilde adlandırılır; hangi alanın hangi çocuğa bağlanacağı
      çalışma notunda tablo (T-014 builder bağlaması bunu kullanacak). Prefab'lara DOKUNMA.
- [ ] Geri bildirim: buton punch zaten var (ButtonFeedback); bilet harcanınca ResourceBar sayısı değişir; NoTicket'ta modal sallanır/uyarı metni.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-faz2` (worktree, dal `agent/faz2-dilim`). Ana klasöre yazma.
- Önce oku: `docs/CALISMA-KURALLARI.md` (§3.1 UI kuralı, §9 legacy Text), `docs/TEMEL-SOZLESME.md` §5, `docs/TUZAKLAR.md` (test asmdef DOTween/Zenject
  görmez!), `Scripts/UI/Framework|Flow/**`, `Scripts/Meta/Contracts/**`, `Scripts/Meta/Config/**`, `Tools/Editor/Builders/Ui/*.cs` (prefab hiyerarşi
  adları için, salt okunur), `docs/gdd/referans-ekranlar/`.
- Paralelde: systems-agent arayüzlerin uygulamalarını (T-010), başka ui-agent `Scripts/UI/Race/**` (T-013), ui-agent builder'ı (T-009) yazıyor.
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta`/`.prefab` yazma. Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle yapılabilir.
- Bitince `status: review`, `tests:` dolu, `## Çalışma notu` 5 satır + bağlama tablosu.

## Çalışma notu
1. Yapildi: HomeScreenView, EventSelectScreenView + EventRowView, EntryGateModalView, ResourceBarView; saf siniflar CountdownFormatter, HomeEntryRules (Play karari + EntryResult esleme), EntryGateModel, HomeEntryFlow (gate.Enter -> SceneFlow.GoTo Race), HomeTexts (sabit metinler, tablo asset'i yok).
2. Zorluk: UiPanel zaten `Construct(UiMotionConfig)` [Inject]'i tasiyor; turetilmis view'larda enjeksiyon metodlari farkli adli (ConstructHome/ConstructEventSelect/ConstructEntryGate).
3. Sapma: Sureli erisim aktifken Play modal acmadan dogrudan girer (aksi halde biletsiz/gizli butonla cikmaz sokak); modal yine de bu durumu (buton gizli, "Timed Ticket active") gosterir.
4. Prefab'ta uyari metni yok: NoTicket'ta Remaining satirina uyari yazilir + pencere sallanir (sure/siddet view alanlari). HomeScreenView secim bossa ilk oynanabilir etkinligi secer; Start'ta kendini navigator'a Push eder (kok ekran).
5. Birakilan: Zenject baglama (WIRING) ve prefab alan baglama (SCENE, T-014). Derleme/test KOSULMADI.

### Baglama tablosu (view alani -> prefab hiyerarsi yolu; kok = prefab koku)
HomeScreenView (HomeScreen koku, UiScreen yerine bu bilesen):
- playButton -> Play (Button) | eventSelectButton -> EventSelect (Button) | eventNameText -> EventSelect/EventName (Text)
- editRacerButton -> EditRacer | highScoresButton -> HighScores | sevenDayButton -> SevenDayReward (kok Button)
- eventSelectScreen -> EventSelectScreen sahne nesnesi | entryGateModal -> EntryGateModal sahne nesnesi
- cosmeticsScreen / highScoresScreen (UiScreen), sevenDayPopup (UiPopup) -> Faz 3+ nesneleri; bos birakilabilir (uyari + islem yok)
EventSelectScreenView (EventSelectScreen koku):
- content -> Panel/List/Viewport/Content | rowPrefab -> EventRow prefab asset'i (EventRowView) | backButton -> Back
EventRowView (EventRow koku):
- button -> kok Button | nameText -> Name | distanceText -> Distance | ticketBadge -> TicketBadge | lockOverlay -> Lock
EntryGateModalView (EntryGateModal koku):
- window -> Window | eventNameText -> Window/EventName | useTicketButton -> Window/UseEntryTicket | useTicketLabel -> Window/UseEntryTicket/Label
- remainingText -> Window/UseEntryTicket/Remaining | timedLabel -> Window/TimedTicketStatus/Label | timedValue -> Window/TimedTicketStatus/Value
- buyWithCashButton -> Window/GetTickets/BuyWithCash | watchAdButton -> Window/GetTickets/WatchAd | closeButton -> Window/Close
ResourceBarView (ResourceBar koku):
- gemAmount -> PurpleGem/Amount | ticketAmount -> EntryTicket/Amount | ticketCountdown -> EntryTicket/Countdown
- timedAmount -> TimedTicket/Amount | cashAmount -> Cash/Amount
Not: builder hala TextMeshProUGUI uretiyor; T-009 legacy Text'e cevirince ayni adlar korunmali.
