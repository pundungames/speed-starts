---
id: T-037
title: Faz 6 liderlik cekirdegi - yerel kaynak (bot dereceleri), oyuncu gunluk en iyileri, donem/bolge sorgusu, sonuc ekrani durusu
status: todo
owner: systems-agent
depends_on: [T-033, T-031]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/LeaderboardContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Scripts/Meta/Records/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/RecordContracts.cs"
  - "Assets/Speed-Staes/Scripts/Meta/Persistence/PlayerProgress.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/LeaderboardConfigSeed.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Records/**"
  - "docs/decisions/**"
touches_scene: false
branch: agent/T-037-liderlik
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/LocalLeaderboardTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: open
    not: "ProjectInstaller: LeaderboardConfig + ILeaderboard (LocalLeaderboard) AsSingle"
started:
finished:
blocked_reason:
---

## Hedef
High Scores / Leaderboard'un veri tarafı. Kullanıcı kararı (28.10): liderlik **yerel**, Sticker Patch'teki gibi (rapor:
`reports/leaderboard-tasima-2026-10-28.md`). Davranış sözleşmesi: süpervizör scratchpad'inde `leader-ui\LEADERBOARD_MASTER.md` (sunucu
varsayar; biz aynı sözleşmeyi yerel kaynakla karşılarız, sunucu gelince yalnız kaynak değişir).

Kararlar (süpervizör):
- **Diğer satırlar = deterministik bot dereceleri**: etkinlik başına bot sayısı, süre dağılımı (en iyi / medyan / en kötü, metre başı
  tempo ya da etkinlik başı saniye), isim listesi, ülke dağılımı (Ayarlar'daki 16 bayrak kodu) `LeaderboardConfig` asset'inde. Değerler
  YER TUTUCU (Malik'in koşucusunun süreleri netleşince ayarlanır) — tohumda açıkça "placeholder" Tooltip'i. Bot üretimi tohumlu (etkinlik
  id'si + bot no) → her açılışta aynı tablo; botların "achievedAt" tarihleri geçmiş N gün içinde dağılır (dönem filtreleri anlamlı olsun).
- **Oyuncu**: her bitirilen yarış (süre > 0) etkinlik + UTC gün başına en iyi olarak kaydedilir (`PlayerProgress`'e ek liste; tavan config'te;
  var olan kişisel rekor kaydıyla tutarlı — T-033'ün `IPersonalBests.Record` yolu genişletilebilir). Ad ve bayrak `IPlayerSettings`'ten.
- **Dönemler UTC** (MASTER.md): AllTime; ThisMonth = ayın 1'i 00:00; ThisWeek = pazartesi 00:00; Today = 00:00. Dönemde kişi başı tek satır
  (o dönemdeki en iyi). Sıralama ham milisaniye artan; eşitlikte daha eski achievedAt, sonra entryId.
- **Bölge**: Global ya da ülke kodu (satırın ülke kodu eşleşir). Oyuncunun kaydı yoksa OwnEntry = null (NOT RANKED).

Sözleşme (`Meta/Contracts/LeaderboardContracts.cs`, ad alanı `SpeedStars.Meta.Contracts`) — T-038 UI'ı buna yazılıyor, aynen uygula:
```
public enum LeaderboardPeriod { AllTime, ThisMonth, ThisWeek, Today }
public readonly struct LeaderboardQuery { RaceEventId EventId; LeaderboardPeriod Period; string CountryCode /* null/empty = Global */; int Offset; int Limit; ctor(all) }
public sealed class LeaderboardRow { string EntryId; int Rank; string DisplayName; string CountryCode; long ElapsedMilliseconds; DateTime AchievedAtUtc; bool IsLocalPlayer; }
public sealed class LeaderboardPage { IReadOnlyList<LeaderboardRow> Rows; int TotalCount; int NextOffset /* -1 = son */; LeaderboardRow OwnEntry /* null = not ranked */; DateTime PeriodStartUtc; }
public interface ILeaderboard {
    // Sunucuya hazır biçim: yerel kaynak hemen çağırır. requestId/eski yanıt yönetimi UI'da.
    void Query(LeaderboardQuery query, Action<LeaderboardPage> onDone, Action<string> onError);
    // Sonuç ekranı kartları (AllTime, Global): oyuncunun bu etkinlikteki sırası ve toplam; kaydı yoksa false.
    bool TryGetStanding(RaceEventId eventId, out int rank, out int total);
    // Bugünün (UTC) en iyisi (bot ya da oyuncu); veri yoksa false.
    bool TryGetBestOfDay(RaceEventId eventId, out LeaderboardRow best);
    event Action Changed; // oyuncu yeni derece yapınca
}
```

## Kabul kriterleri
- [ ] EditMode: dönem sınırları (UTC; hafta pazartesi), dönemde kişi başı tek satır, eşit süre bağlayıcısı, ülke filtresi + ülke içi sıra,
      sayfalama (Offset/Limit/NextOffset tekrar yok), OwnEntry (kayıtsız = null), standing/percentile girdileri, bot tablosunun deterministik
      olması; sabit süre/sayı assert edilmez (config test içinde kurulur)
- [ ] Kayıt göçü: eski kayıt yeni listeyle açılır (gerekirse kısa ADR)
- [ ] needs listesinde open WIRING yok (WIRING süpervizörde)

## Kapsam disi
- UI (T-038), sunucu/çevrimiçi liderlik, hile koruması

## Calisma notu
(Agent doldurur)
