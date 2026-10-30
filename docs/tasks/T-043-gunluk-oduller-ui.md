---
id: T-043
title: Faz 4 UI (Gunluk_Odul_UI_v2) - gunluk hediye popup'i + bilet ucusu, 7 gunluk odul paneli, takvim kirmizi noktasi
status: review
owner: ui-agent
depends_on: [T-042]
owns_paths:
  - "Assets/Speed-Staes/UI/Art/Rewards/**"
  - "Assets/Speed-Staes/Scripts/UI/Rewards/**"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/ResourceBarView.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiRewardPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/UiPrefabPaths.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneWiring.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Rewards/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-043-gunluk-oduller-ui
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Rewards/RewardPanelsModelTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RewardPanelsSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "UI prefab builder overwrite=true: DailyGiftPopup + SevenDayCheckInPanel + RewardFlight (same paths, now with views), new Rows/RewardDayCard + Rows/RewardPremiumCard, Data/Config/RewardUiConfig.asset seed, Art/Rewards sprite import (UiArt.ImportAll). Then Home scene rebuild/wiring (SceneWiring.WireDailyGift binds HomeScreenView.dailyGiftPopup, DailyGiftPopupView.flight, TicketFlightView.resourceBar). Prefabs/scene/.meta/.asset are not writable for the UI agent."
  - tip: WIRING
    durum: open
    not: "T-042's ProjectInstaller binding IDailyGift + ISevenDayRewards AsSingle: SevenDayPanelView and DailyGiftPopupView inject them (Home itself uses InjectOptional); without it the Home scene reports missing bindings"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Sanat: `C:\Users\Monster\Downloads\Gunluk_Odul_UI_v2.zip` (açılmış hâli süpervizör scratchpad'inde `odul-ui\`: README.md (katman/tuval
kuralları), **DAILY_REWARDS_MASTER.md = bağlayıcı davranış**, layout.json, approved_daily_available.jpg, approved_daily_full.jpg,
approved_seven_day.jpg, preview_layers.jpg, 36 PNG). Veri: T-042'nin `IDailyGift` / `ISevenDayRewards` sözleşmesi —
**`docs/tasks/T-042-gunluk-oduller-cekirdek.md`'deki kod bloğu**; T-042 paralel yazılıyor, imzaya göre yaz.

1) **Günlük hediye popup'ı** (bugün yer tutucu `DailyGiftPopup`; aynı yol): Home açılınca `IDailyGift.State == Available` ise kendiliğinden
   açılır; DAILY GIFT başlığı, kırmızı Adet Bileti ikonu, "+1", ENTRY TICKET, "Tickets 1/3", CLAIM. CLAIM → `Claim()` Granted → panel 150 ms
   kapanır → merkezden ana ekrandaki Adet Bileti kutusuna **bir kez** bilet uçar (var olan `UI/Widgets/RewardFlight` widget'ı ya da bu
   paketteki `ticket_flight_icon`), hedefte küçük punch. Full ise "TICKETS FULL" + "Tickets 3/3" + OK (ödül/uçuş yok, `MarkSeenFull`).
   Claimable panelde X yok. İstek sırasında CLAIM pasif.
2) **7 günlük panel** (bugün yer tutucu `SevenDayCheckInPanel`; aynı yol): ana ekranın sol üstündeki takvim butonu açar (var olan
   `sevenDayButton`), alınabilir ödül varken **kırmızı nokta** (`sevenDayRedDot`, `HasClaimable`). 7 DAY REWARDS, X, 1–6. gün kartları
   (3×2; normal/available(kırmızı kontur + AVAILABLE şerit)/claimed(CLAIMED + tik)/locked(kilit)), büyük 7. gün kartı **gerçek Premium 02
   şapka ikonu** (`UI/Art/Costume/Items/Headwear/PH02.png`, aspect korunur), CLAIM (yalnız Available varken etkin). Nakit = cyan elmas
   ikonu, Süreli Bilet = altın bilet ikonu, miktar metni ayrı.
3) Açılış önceliği: önce günlük hediye; kapanıp uçuş bitince ana menü serbest; 7 gün paneli otomatik açılmaz. İki modal üst üste yok.
Metin legacy Text, İngilizce; DOTween yalnız çekirdek; kod kalıcı UI kurmaz; dokunma alanları görünür bölge (TUZAKLAR).

## Kabul kriterleri
- [ ] Saf model (kart görünümleri, buton durumları, kapasite metni, açılış önceliği) EditMode testli (sahte servisler)
- [ ] PlayMode: takvim → 7 gün paneli → X kapanır; günlük hediye Available iken Home'da açılır, CLAIM → kapanır, bilet sayısı +1
      (gerçek ışınlı Tap); var olan `SevenDay_Button_OpensPopup_AndCloseButtonClosesIt` yeşil; ekran görüntüleri `Home_dailygift`, `Home_sevenday`
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : bilet uçuşu + hedef punch; claim edilen kart CLAIMED'e döner (punch); kırmızı nokta kaybolur
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Ödül kuralları (T-042), reklam

## Calisma notu
1. Yapilan: saf model `RewardPanelsModel` (+`RewardTexts`, `RewardUiConfig`), `DailyGiftPopupView`, `SevenDayPanelView`, `RewardDayCardView`, `TicketFlightView`; Home'a otomatik acilis (`dailyGiftPopup`) ve kirmizi nokta (`HasClaimable`); ResourceBar `EntryTicketBox`; builder `UiRewardPrefabs` (art `UI/Art/Rewards`, layout.json'dan konum); SceneWiring `WireDailyGift`; PlayMode `RewardPanelsSmokeTests` + `Home_dailygift`/`Home_sevenday` shot'lari; `LoadScene` Home'da gunluk hediyeyi (claim'siz) kapatir.
2. Sozlesme varsayimlari: T-042 blogu aynen; `SevenDayCard.Kind` tipi belirsiz oldugu icin ad uzerinden (`Kind.ToString()`: Cash/TimedTicket/Cosmetic) eslenir; `MarkSeenFull` sonrasi `State` bu oturumda tekrar Available/Full dondurmez; `Claim()` Granted sonrasi `State == NotDue`.
3. 7 gun kartlari prefab ornegi olarak panele yerlestirilir (sabit 7, Inspector'da gorunur), Instantiate ile degil; 7. gun sapka resmi `CostumeEditorConfig.IconOf(CosmeticId)` (gardirobla ayni kayit); baslik ve X konumu layout.json'da yok, preview_layers.jpg'den olculdu (UiRewardPrefabs `SevenDayTitleBox`/`CloseBox`).
4. Zorlandigim/riskli: hicbir sey derlenmedi/kosulmadi (Unity yasak); T-042 `RewardContracts.cs` gelmeden derlenmez; bilet sayisi uctugu an degil claim aninda artar (ResourceBar IEntryTickets.Changed); eski `UiPopupPrefabs.DailyGift/SevenDayCheckIn` ve `UiWidgetPrefabs.RewardFlight` yer tutuculari artik kullanilmiyor (silinmedi, baska dosya).
5. Birakilan: loading spinner (istek senkron, claim bir karede biter), azaltilmis hareket (projede ayar yok), SFX; PlayMode testleri `DailyGiftDayKey` bos = bugun alinabilir varsayar (T-042 kayit semantigi farkliysa test duzeltilir).
