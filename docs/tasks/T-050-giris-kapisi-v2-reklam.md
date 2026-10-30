---
id: T-050
title: Giris kapisi v2 - eldeki odul sanatiyla yeniden gorunum + sahte odullu reklam (Watch Ad -> 1 Adet Bileti)
status: merged
owner: ui-agent
depends_on: [T-043]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Home/EntryGate*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/HomeTexts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Ads/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/AdContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiEntryGatePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiScreenPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiViewBinder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiPrefabBuilder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Ads/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: true
branch: agent/T-050-giris-kapisi-v2
commits: [c61d3be, 82a3198, 864ed15]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/EntryGateAdFlowTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/EntryGateAdSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "IRewardedAds -> FakeRewardedAds (yalniz #if UNITY_EDITOR || DEVELOPMENT_BUILD; ProjectInstaller: Container.BindInterfacesTo<FakeRewardedAds>().AsSingle() + AdsDevConfig asset FromInstance). Release build: baglama yok, WATCH AD pasif kalir (view [InjectOptional])."
  - tip: ASSET
    durum: closed
    not: "AdsDevConfig asset (menu Speed Stars/Meta/Ads Dev Config; fakeAdSeconds ~1, fakeResult Completed) + yeni dosyalarin .meta uretimi (Meta/Ads, Meta/Ads/Dev, EntryGateAdFlow.cs, testler)"
  - tip: ASSET
    durum: closed
    not: "EconomyConfig asset: rewardedAdEnabled=1, rewardedAdEntryTickets=1 (supervizor karari, PDF 1.2); ConfigAssetSeeder.cs:97-98 simdi false/0 seed ediyor, o dosya benim yolum degil"
  - tip: SCENE
    durum: closed
    not: "EntryGateModal prefab yeniden uretimi (UiPrefabBuilder.BuildAll overwrite=true; hiyerarsi UiEntryGatePrefabs, alan baglari UiViewBinder.BindEntryGate) + Home sahnesindeki EntryGateModal ornegi yenilenmeli. Yerlesim olculeri (pencere 620x700 sanat-px, daily_panel dikey gerilmis) kod okuyarak yazildi, gozle ince ayar gerekebilir; Home_entrygate ekran goruntusunu kontrol et"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
`docs/eksik-ui/README.md` C bolumu: giris kapisi (Play'e basinca acilan bilet penceresi, `EntryGateModalView`, builder
`UiScreenPrefabs.EntryGate`) calisiyor ama duz yer tutucu gorunumde. Tasarimi gelmeyecek; **eldeki Gunluk_Odul sanatiyla**
(`Assets/Speed-Staes/UI/Art/Rewards/`, ayni pencere dili) yeniden kurulur ve "Watch Ad" ilk kez calisir.

1) **Gorunum** (builder; yeni dosya `UiEntryGatePrefabs.cs`'e tasinabilir): `modal_dimscreen` tam ekran dim, `daily_panel` pencere,
   `daily_header` uzerinde baslik (ENTRY / "RACE ENTRY" — metin `HomeTexts`'te), `close_background`+`close_icon` X, kirmizi Adet Bileti
   `daily_ticket_icon` + "Tickets 2/3" + yenilenme geri sayimi, Sureli Bilet `day_card_timed_ticket` + stok/kalan sure, birincil buton
   `daily_button_claim` (USE TICKET / USE TIMED TICKET), ikincil `button_secondary` (BUY WITH CASH fiyatla, WATCH AD), pasif hal
   `daily_button_disabled`, basili `daily_button_pressed`. Dolu envanterde var olan "TICKETS FULL" mantigi aynen. Yerlesim
   `approved_daily_available.jpg` ile ayni olcu dili (panel ortada, ikon ustte, butonlar altta); konumlari kendin olc. Metin legacy Text,
   Ingilizce. Var olan `EntryGateModel` mantigi degismez (yalniz gorunum ve reklam).
2) **Odullu reklam seam'i** (`Meta/Contracts/AdContracts.cs`, `SpeedStars.Meta.Contracts`): `IRewardedAds { bool IsReady; void Show(Action<AdResult> done); }`,
   `enum AdResult { Completed, Skipped, Failed }`. Sahte uygulama `Meta/Ads/Dev/FakeRewardedAds.cs`, tamami
   `#if UNITY_EDITOR || DEVELOPMENT_BUILD` icinde: config'teki kisa bekleme (`AdsDevConfig` ya da `EconomyConfig` disinda kucuk bir SO;
   varsayilan ~1 sn) sonra Completed. SDK yokken (release, baglama yok) WATCH AD pasif kalir.
3) **Akis**: WATCH AD -> buton yukleniyor hali (`loading_spinner` donusu, DOTween cekirdek) -> Completed ise
   `IEntryTicketSource.TryGrant(TicketGrantSource.RewardedAd)` -> sayi +1, ticket ikonuna punch; Full ya da Skipped/Failed ise odul yok,
   kisa sallanma. Bekleme sirasinda pencere kapanirsa sonuc yine islenir ama UI'a dokunulmaz. Cift basis iki reklam acmaz.
   `EconomyConfig.TryGetRewardedAdEntryTickets` 0 donerse (config kapali) buton pasif.

Kod kalici UI kurmaz; dokunma alanlari gorunur bolge (`UiArt.AlphaBounds`); dekor katmanlarinda raycastTarget kapali; DOTween yalniz
cekirdek (TUZAKLAR). Baglama (ProjectInstaller) ve asset/prefab uretimi supervizorde: ihtiyaclari `needs:`'e yaz.

## Kabul kriterleri
- [ ] EditMode: reklam akisi saf modeli (hazir degil -> pasif, Completed -> TryGrant bir kez, Full/Skipped/Failed -> odul yok, cift basis tek
      istek, config 0 -> pasif) sahte servislerle testli; sabit miktar assert edilmez
- [ ] PlayMode: Home -> Play -> giris kapisi acilir -> WATCH AD (gercek isinli Tap) -> bilet sayisi artar (envanter dolu degilken kurulur);
      var olan giris kapisi testleri yesil; ekran goruntusu `Home_entrygate` yeni gorunumle
- [ ] needs: WIRING (IRewardedAds -> FakeRewardedAds, yalniz editor/dev) + SCENE (prefab yeniden uretimi) + ASSET (EconomyConfig
      rewardedAdEnabled=1, rewardedAdEntryTickets=1; supervizor karari, PDF 1.2) yazili

## Geri bildirim
GORSEL  : yukleniyor donusu; odulde bilet ikonu punch + sayi; reddedilince sallanma
SES     : T-052'nin buton sesi kendiliginden gelir
DOKUNSAL: T-052
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Gercek reklam SDK'si, Nakit fiyati karari (config bos kalir), giris kapisinin acilma kurali

## Calisma notu
- Yapildi: IRewardedAds/AdResult (AdContracts.cs), AdsDevConfig + dev-only FakeRewardedAds (+Pump), saf EntryGateAdFlow (UI/Home), EntryGateModalView yeni alanlar + WATCH AD akisi (spinner, punch, sallanma), UiEntryGatePrefabs + binder, HomeTexts.
- Pencere: daily_panel 620x600 sanati 620x700'e dikey gerildi (Timed satiri + iki buton sigsin diye); diger parcalar dogal oranda. Olculer koddan, gozle dogrulanmadi.
- Zorluk: Unity calistirilmadi, derleme dogrulanmadi. FakeRewardedAds bekleme icin gizli DontDestroyOnLoad pompa nesnesi kullanir (Meta asmdef'inde Zenject/DOTween yok).
- Reklam sonucu pencere kapaliyken de akis tarafinda islenir (bilet verilir + kayit); view kapaliysa UI'a dokunmaz.
- Birakilan: WIRING/ASSET/SCENE needs (frontmatter); mevcut PlayMode testleri icin alan adlari (useTicketButton, closeButton) korundu.
