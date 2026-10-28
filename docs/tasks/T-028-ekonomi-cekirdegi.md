---
id: T-028
title: Faz 3 ekonomi cekirdegi - tek islem yolu (idempotent), Sureli Bilet etkinlestirme, Mor Mucevher odulu, dolu envanterde edinim kapali
status: todo
owner: systems-agent
depends_on: [T-025, T-023]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Economy/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/EconomyContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Wallet/**"
  - "Assets/Speed-Staes/Scripts/Meta/Tickets/**"
  - "Assets/Speed-Staes/Scripts/Meta/Race/RaceReward*.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Cosmetics/CosmeticService.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Themes/ThemeService.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Config/EconomyConfig.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Tickets/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/**"
touches_scene: false
branch: agent/T-028-ekonomi
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/EconomyLedgerTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/TimedAccessTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/RaceRewardResolverTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Economy/EconomyEdgeCaseTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: IEconomyLedger, ITimedAccess, IRaceRewardResolver AsSingle; Cosmetic/Theme servisleri yeni imzayla"
started:
finished:
blocked_reason:
---

## Hedef
PDF ekonomisinin (GDD §6, §7, §14) eksik kalan çekirdeği. Bugün Adet Bileti yenilenmesi (`EntryTicketService`), giriş kapısı
(`RaceEntryGate`: ücretsiz / Süreli erişimle / biletle) ve cüzdan okuma (`WalletService : IWallet`) var; Cosmetic ve Theme servisleri parayı
`PlayerProgress`'ten doğrudan düşüyor. Eklenecekler:
1) **Tek işlem yolu** `IEconomyLedger`: Nakit, Mor Mücevher, Süreli Bilet için `TrySpend(currency, amount, transactionId)` ve
   `Grant(currency, amount, transactionId)`; sonuç enum'u (Done, AlreadyProcessed, NotEnough, InvalidAmount). Aynı `transactionId` ikinci kez
   hiçbir şey değiştirmez (`PlayerProgress.processedTransactionIds`; liste sınırı config'te, en eskiler düşer). Başarısız işlem hiçbir alanı
   değiştirmez. Değişince `IWallet.Changed`. Cosmetic ve Theme satın almaları buna geçer (işlem id'si öğe/tema id'sinden türetilir,
   davranışları ve testleri aynı kalır).
2) **Süreli Bilet** `ITimedAccess.TryActivate()`: stok > 0 ve etkin değilse stok −1, bitiş = güvenilen şimdi + `timedAccessMinutes`
   (config); etkinken ikinci kullanım reddedilir, süre birikmez (sonuç enum'u: Activated, AlreadyActive, NoTicket). Bitiş zaman damgasıyla;
   oyun kapalıyken de akar. Yarış ortasında biterse yarış sürer (giriş kontrolü yalnız girişte; mevcut kapı zaten öyle — testle sabitle).
3) **Mor Mücevher ödülü** `IRaceRewardResolver.Resolve(outcome)`: yalnız `AwardsPurpleGems` etkinliklerinde, tamamlanmış yarışta sıra
   → `GemRewardConfig.GetPurpleGems(rank)`; ödeme işlem id'si = yarış oturum id'si (aynı yarış iki kez ödemez); geçersiz/tamamlanmamış = 0.
   Dönen değer UI için (kazanılan miktar). `RaceOutcome`'da oturum id'si yoksa Core sözleşmesine ekleme gerekiyorsa BLOCKED yaz.
4) **Dolu envanter:** Adet Bileti tavanında günlük hediye / reklam / Nakit ile edinim reddedilir (`EntryTicketService` üzerinde tek
   `TryGrant(source)` → Granted / Full); Nakit fiyatı config'te 0 = satılmıyor (PDF'te fiyat yok).
5) **Uç durum regresyonu** (GDD §6.5 tablosu + 50 fiyat + çift işlem): 3 tavanı, 30 dk yenilenme, 3→2'de yeni 30 dk, 1 saat Süreli,
   Süreli etkinken bilet harcanmaz, iade yok, çift işlem yok. Saat `IClock` ile sahte.

## Kabul kriterleri
- [ ] Para değiştiren tek yol `IEconomyLedger` (grep: `PlayerProgress.Cash =`/`PurpleGems =`/`TimedTickets =` yalnız ledger'da ve kayıt göçünde)
- [ ] EditMode testleri yukarıdaki beş başlığı kapsar; sabit denge sayısı assert edilmez (config test içinde kurulur)
- [ ] Mevcut testler (218 + T-023/T-025) yeşil kalır; değişen davranış yoksa test değişmez
- [ ] needs listesinde open WIRING yok (WIRING süpervizörde)

## Kapsam disi
- UI (T-029), reklam SDK'sı (projede yok), bilet Nakit fiyatı (PDF'te yok), günlük hediye ve 7 gün (Faz 4)
- Mekanik: yarış sırası/rakipler Malik'te; sıra bugün `RaceOutcome.Rank`'tan okunur

## Calisma notu
(Agent doldurur)
