---
id: T-093
title: High Scores'ta Race of the Day listesi bos - gunun mesafesinin kayitlari + gunun bot alani (sonuc ekranindaki Daily WR ile ayni)
status: todo
owner: systems-agent
depends_on: [T-087]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Scripts/UI/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Leaderboard/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Leaderboard/**"
touches_scene: false
branch: agent/T-093-highscores-race-of-the-day
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
