---
id: T-043
title: Faz 4 UI (Gunluk_Odul_UI_v2) - gunluk hediye popup'i + bilet ucusu, 7 gunluk odul paneli, takvim kirmizi noktasi
status: todo
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
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
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
(Agent doldurur)
