---
id: T-042
title: Faz 4 cekirdek - gunluk giris hediyesi (1 Adet Bileti) ve 7 gunluk odul servisi
status: merged
owner: systems-agent
depends_on: [T-028]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Rewards/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/RewardContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Config/SevenDayRewardConfig.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Config/EconomyConfig.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Rewards/**"
  - "docs/decisions/**"
  - "Assets/Speed-Staes/Scripts/Meta/Cosmetics/CosmeticService.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/CosmeticContracts.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Cosmetics/**"
touches_scene: false
branch: agent/T-042-gunluk-oduller
commits: [8f7d8a9]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Rewards/DailyGiftServiceTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Rewards/SevenDayRewardServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "ProjectInstaller: Container.BindInterfacesAndSelfTo<DailyGiftService>().AsSingle(); Container.BindInterfacesAndSelfTo<SevenDayRewardService>().AsSingle(); (SevenDayRewardConfig asset must be bound as an instance; IClock, IEconomyLedger, ICosmeticService, IEntryTickets, IEntryTicketSource, EconomyConfig, ProgressRepository already exist)"
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
PDF §1.2, §2.1, §2.4 (GDD §8, §11). Davranış: süpervizör scratchpad'inde `odul-ui\DAILY_REWARDS_MASTER.md`. Bugün yalnız config var
(`SevenDayRewardConfig`, `EconomyConfig.dailyGiftEntryTickets`, `PlayerProgress.dailyGiftDayKey` / `SevenDayCheckInState`); servis yok.
Var olan yollar: `IEntryTicketSource.TryGrant(TicketGrantSource.DailyGift)` (dolu → Full), `IEconomyLedger.Grant` (Nakit, Mor Mücevher,
Süreli Bilet; idempotent işlem id'si), `ICosmeticService` (sahiplik), `IClock`.

Kararlar (süpervizör; kullanıcı kararları bana bıraktı):
- **Gün = UTC günü** (liderlikle aynı). Saat `IClock`.
- **Günlük hediye**: günün ilk ana menü girişinde açılır; CLAIM = +1 Adet Bileti (`TryGrant(DailyGift)`), bugün alındı olarak kaydedilir.
  Envanter doluysa: ödül yok, durum Full (UI "TICKETS FULL" + OK), **günün hakkı yanmaz** — aynı gün bilet azalınca bir sonraki ana menü
  girişinde tekrar teklif edilir. Çift ödül yok (gün anahtarı + işlem id'si).
- **7 gün**: günde en fazla bir kart; elle CLAIM; kaçırılan gün sıfırlamaz (kaldığı yerden); 7. gün alınınca seri 1'e döner (`cyclePolicy`
  config'te). Ödüller config'te: G1 100 Nakit, G2 200 Nakit, G3 1 Süreli Bilet, G4 200 Nakit, G5 1 Süreli Bilet, **G6 300 Nakit** (PDF'te
  yok; tasarım ve kullanıcı kararı), **G7 Premium 02 şapka** (`CosmeticCatalog`'daki PH02 öğesi — id'yi katalogdan bul, uydurma; tohum
  bugün PH01'i gösteriyor, PH02'ye çevir). Süreli Bilet stoğa eklenir, **etkinleşmez**. G7 şapkası zaten sahipse telafi = o öğenin Mor
  Mücevher fiyatı (katalogdan). Ödül verilmeden ilerleme artmaz; başarısız işlemde hiçbir şey değişmez.
- Kayıt: var olan alanlar yeterliyse onlar; yeni alan gerekirse ek (eski kayıt varsayılanla açılır; kısa ADR).

Sözleşme (`Meta/Contracts/RewardContracts.cs`, ad alanı `SpeedStars.Meta.Contracts`) — T-043 UI'ı buna yazıyor, aynen uygula:
```
public enum DailyGiftState { NotDue, Available, Full }
public enum DailyGiftResult { Granted, Full, AlreadyClaimed, NotDue }
public interface IDailyGift {
    DailyGiftState State { get; }          // bugün için
    int Amount { get; }                    // config (1)
    DailyGiftResult Claim();               // Granted: bilet eklendi + kaydedildi
    void MarkSeenFull();                   // Full gösterildi (OK) — hakkı yakmaz, yalnız bu oturumda tekrar göstermemek için
    event Action Changed;
}
public enum SevenDayRewardKind2 { Cash, PurpleGems, TimedTicket, Cosmetic }   // config'teki enum'u kullanabilirsin; adı çakışırsa onu kullan ve UI'a bildir
public enum SevenDayCardState { Claimed, Available, Locked }
public readonly struct SevenDayCard { int DayNumber; SevenDayCardState State; <kind> Kind; int Amount; string CosmeticId; }
public enum SevenDayClaimResult { Claimed, NothingAvailable, Failed }
public interface ISevenDayRewards {
    IReadOnlyList<SevenDayCard> Cards { get; }   // 7 kart, bugünkü duruma göre
    bool HasClaimable { get; }                   // kırmızı nokta
    SevenDayClaimResult Claim();                 // yalnız Available kart
    event Action Changed;
}
```

## Kabul kriterleri
- [ ] EditMode: UTC gün geçişi, dolu envanterde hak yanmaz + aynı gün sonra alınır, çift claim yok, 7 gün sırası, kaçırılan gün, döngü,
      G7 sahipse telafi, Süreli Bilet etkinleşmez, başarısızlıkta değişiklik yok; sabit miktar assert edilmez (config test içinde kurulur)
- [ ] Tohum: G6 = 300 Nakit, G7 = PH02 (katalog id'si) — `ConfigAssetSeeder` seven-day tohumu güncellenir
- [ ] needs listesinde open WIRING yok (WIRING süpervizörde)

## Kapsam disi
- UI (T-043), reklam, sunucu

## Calisma notu
- Yapildi: RewardContracts.cs, Rewards/{DailyGiftService,SevenDayRewardService,RewardDay}.cs, tohum G6=300 Cash + G7=headwear.ph02, 2 test dosyasi (kod yazildi, koşulmadi).
- Sozlesme tam; tek sapma: SevenDayCard.Kind = config enum SevenDayRewardKind (Cash/TimedTicket/Cosmetic; PurpleGems yok, ihtiyac yok). DailyGiftService'e ek: FullNoticeSeenToday (sinif uyesi).
- Kayit degisikligi yok (DailyGiftDayKey + SevenDayCheckInState yeterli) -> ADR yok. Gun = UTC "yyyy-MM-dd"; EconomyConfig.dayBoundary kullanilmiyor (karar UTC).
- ICosmeticService'te sahiplik ekleme yolu yok ve CosmeticService benim alanim degil: G7 sahiplik Progress.OwnedCosmeticIds'e dogrudan yazilir; ICosmeticService.Changed tetiklenmez (UI IsOwned'i yeniden okumali).
- EntryTicket turu 7 gun kartinda desteklenmez (Failed); None/bos gun takvimi ilerletir. Tam suite gerekir (ekonomi/kayit). WIRING sûpervizorde.

- Ek (2026-10-30, koordinator): ICosmeticService.GrantOwned eklendi (varsayilan govde UnknownItem; UI testindeki FakeCosmeticService alanim disinda oldugu icin derleniyor). SevenDay artik onu kullanir; yukaridaki 'dogrudan yazar' notu gecersiz.
