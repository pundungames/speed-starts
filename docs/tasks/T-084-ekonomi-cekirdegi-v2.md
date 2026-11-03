---
id: T-084
title: Ekonomi v2 cekirdegi - yaris Nakit odulu (dokumlu), reklamla Nakit (gunde 3), Sureli Bilet Nakit'le, fiyatlar config'te
status: todo
owner: systems-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Economy/**"
  - "Assets/Speed-Staes/Scripts/Meta/Ads/**"
  - "Assets/Speed-Staes/Scripts/Meta/Config/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/**"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/**"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlowLogic.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceViewContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
touches_scene: false
branch: agent/T-084-ekonomi-v2
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
Kullanici karari (03.11): referans oyunun ekonomisi `docs/analiz/2026-11-03-referans-oyun-ekonomi.md` bolum 4'teki tabloyla "oldugu gibi"
uygulanir. Gercek para (IAP, tam surum) BU TASK'TA YOK. GDD sabitleri degismez (Adet Bileti 3 / 30 dk, Sureli Bilet 1 saat, 7 gun odulleri,
Mor Mucevher 3/2/1, premium 50). Sayilar config asset'inde durur; kod varsayilani = asset (seeder ayni degeri yazar).

1) **Yaris Nakit odulu** (yalniz bilet gerektiren etkinlik, `RaceEventDefinition.requiresTicket`; biletsiz/Practice 0): bitirme +10,
   zorluk bonusu +10 (sabit; bizde zorluk ayari yok), rakip bonusu = round(10 x gecilen rakip / toplam rakip) (5 bot: her biri +2;
   son 20, birinci 30; rakipsiz 0). Odeme ledger uzerinden, yarisin payout id'siyle (ayni yaris iki kez odemez; gem odulunun yaninda).
   Sonuc bir doku olarak doner (toplam + satirlar: bitirme / zorluk / rakip) ki sonuc ekrani gosterebilsin. Nerede bagli oldugunu
   `RaceFlowLogic` gem odemesinden (`IRaceRewardResolver`, `RaceFlowGemView`/`IGemRewardView`) ornek al; UI icin bir view seam'i ac
   (`IRaceCashRewardView.ShowCashReward(dokum)` gibi), view'i T-085 yazar. Sayilar yeni bir config'te (ya da EconomyConfig'te; tek kaynak).
2) **Reklamla Nakit**: odullu reklam +200 Nakit, gunde 3 hak (gun siniri EconomyConfig.dayBoundary, gunluk hediyeyle ayni kural; saat geri
   alininca hak uretmez). Mevcut `IRewardedAds` (editor/dev'de sahte) kullanilir. Kalan hak + Changed olayi disari verilir (UI: T-085).
3) **Fiyatlar**: Adet Bileti 100 Nakit (mevcut `entryTicketCashPrice`), Sureli Bilet 1.000 Nakit (`timedTicketCashPrice`; satin alma akisi
   yoksa cekirdegini yaz: Nakit dus, Sureli Bilet stok +1, ayni kurallar). Normal kozmetik: Headwear 1.000, diger kategoriler 500
   (premium'lar 50 Mor Mucevher kalir). **Varsayilan kiyafet ve 9 hazir kostumde (Custom 1..9 / preset) kullanilan ogeler ucretsiz kalir**
   (yoksa preset'ler kilitli oge gosterir; once kodda preset/varsayilan kiyafet okuyucularini bul, kurali calisma notuna yaz). Emote:
   ucretsiz olanlar ucretsiz, digerleri sirayla 2.500 / 2.500 / 5.000 / 7.500 Nakit. Fiyatlar seeder'dan (`ConfigAssetSeeder`) asset'lere yazilir;
   asset'e sen dokunmazsin (needs: ASSET, supervizor seeder'i kopyada kosar).
Kurulum/baglama: yeni servisler `ProjectInstaller`/`RaceSceneInstaller`'a supervizorce baglanir -> needs: WIRING (tam satir `not:`'ta).
Once oku: docs/TUZAKLAR.md, docs/CALISMA-KURALLARI.md §6 (denge sayisi), mevcut `EconomyLedger`, `EntryTicketShop`, `RaceRewardResolver`,
`DailyGiftService`, `EntryGateAdFlow`, `CosmeticService`, emote servisi.

## Kabul kriterleri
- [ ] Biletli yarista bitiren oyuncuya dokum kadar Nakit; ayni payout id ikinci kez odemez; biletsiz etkinlikte 0
- [ ] Reklam -> +200 Nakit, gunde 3; 4. hak yok; UTC gun degisince sifirlanir; saat geri alininca hak uretmez
- [ ] Sureli Bilet Nakit'le alinabilir (yetersizse alinmaz), Adet Bileti 100 Nakit (3 tavani korunur)
- [ ] Fiyatlar seeder'da; varsayilan kiyafet + preset ogeleri ucretsiz
- [ ] EditMode testleri; sabit denge sayisi assert yok (sayi config'ten okunur ya da iliski)
- [ ] needs'te WIRING ve ASSET maddeleri tam

## Kapsam disi
- Sonuc ekrani dokum gorunumu, para magazasi penceresi, butonlar (T-085); gercek para; tema fiyatlari; Malik'in kodu

## Calisma notu
(Agent doldurur: 5 satir)
