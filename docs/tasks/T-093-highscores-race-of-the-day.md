---
id: T-093
title: High Scores'ta Race of the Day listesi bos - gunun mesafesinin kayitlari + gunun bot alani (sonuc ekranindaki Daily WR ile ayni)
status: merged
owner: systems-agent
depends_on: [T-087]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Scripts/UI/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Leaderboard/**"
touches_scene: false
branch: agent/T-093-highscores-race-of-the-day
commits: [0c8be28f, f9743163, 68022b57]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/LocalLeaderboardTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Leaderboard/LeaderboardDailyOptionTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "-> f9743163 (T-091: EventModal 4 sekme) + 68022b57 (kopyada rebuild). HighScoresScreen prefab: Race of the Day appears in the event filter as a 4th category (title DAILY, one option). LeaderboardScreenView.categoryTabs needs a 4th LeaderboardChoiceView tab, otherwise the option is only reached when the screen opens with the daily race selected (no tab to switch to it)."
  - tip: DECISION
    durum: closed
    not: "Backlog'a tasindi (supervizor 03.11; Meta/RaceOfTheDay bizim alan, Malik'in degil). RaceOfTheDayService.TryBuildField builds the field id by hand (eventId + ':' + DayKey). Leaderboard now has DailyBotField.FieldId(eventId, dayKey) with the same format; switching the service to it removes the duplicated format. Tests (list first row == TryGetFieldRecord) catch any drift meanwhile."
  - tip: WIRING
    durum: closed
    not: "none needed: LocalLeaderboard takes IRaceOfTheDay as an optional constructor parameter (default null); Zenject resolves it because ProjectInstaller already binds IRaceOfTheDay. Not verified in a container run (verification is central)."
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
T-087 Race of the Day surelerini mesafeli kayit id'siyle sakliyor (`IRaceOfTheDay.RecordIdFor`, ornegin "race_of_the_day:150m";
TUZAKLAR "Gunun mesafesi degisince PB/DB tek event id'de karisir"). High Scores ekraninin etkinlik filtresinde "Race of the Day" secilince
liste bos: sorgu "race_of_the_day" id'siyle yapiliyor, oyuncu kaydi yok; bot alani da `BotField.Generate` mesafe 0 olan etkinlikte
bot uretmiyor (BotField.cs:36). Yapilacak: Race of the Day secilince liste BUGUNUN mesafesinin kayit id'si uzerinden sorgulanir
(oyuncunun o mesafedeki sureleri + o mesafeye olcekli bot alani). Bot alani sonuc ekranindaki "Daily World Record" ile AYNI uretilmeli
(T-087: Meta icinde BotField, gun + mesafe tohumlu - `Meta/RaceOfTheDay/**` oku, oraya YAZMA; ortak bir yardimci gerekiyorsa Leaderboard
tarafinda ac). Donem filtresi (gunluk / haftalik / tum zamanlar) ayni kayit id'sinde normal calisir. Etkinlik basliginda gunun mesafesi
gorunsun (ornegin "Race of the Day 150m"; metin Ingilizce). `IRaceOfTheDay` ProjectInstaller'da bagli (opsiyonel enjekte et; yoksa eski davranis).
Installer'a YAZMA (gerekirse needs: WIRING, tam satir). Prefab'a/builder'a YAZMA (gerekirse needs: SCENE).

## Kabul kriterleri
- [ ] Race of the Day filtresinde liste dolu: gunun mesafesinde oyuncu satiri (varsa) + bot satirlari; ertesi gun mesafe degisince liste o mesafeye gecer (test, saat sahte)
- [ ] Listedeki birinci sure, ayni gun sonuc ekranindaki Daily World Record ile ayni (test)
- [ ] Diger etkinliklerin listesi aynen (mevcut testler yesil)
- [ ] EditMode testleri; sabit denge sayisi assert yok

## Kapsam disi
- Liderlik backend'i (yerel), sonuc ekrani (T-087), Practice

## Calisma notu
1. LocalLeaderboard resolves the Race of the Day id to the day's record id (RecordIdFor(today distance)) for the player's times and to DailyBotField.FieldId(eventId, dayKey) + today's distance for bots; same inputs as the service's Daily World Record.
2. High Scores screen gets IRaceOfTheDay (optional) and adds a DAILY category whose label is "Race of the Day 150m", refreshed on each opening (day rollover).
3. Tests added to LocalLeaderboardTests (list first row == TryGetFieldRecord, scaling, own time only on today's distance, periods, next day, other events unchanged) and a new UI test file.
4. Struggled with: the daily race is not in EventSelectLayout categories, so the filter had no way to show it; solved in code, a 4th tab still needs the prefab.
5. Left: nothing run (no Unity/tests); constructor default-parameter injection is unverified in a container.
