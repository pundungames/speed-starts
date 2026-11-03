---
id: T-087
title: Race of the Day - her gun degisen mesafe + gunun yarisci kiyafeti (herkes ayni), biletsiz, gunluk PB / gunluk dunya rekoru
status: todo
owner: gameplay-agent
depends_on: [T-084, T-088, T-089]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/RaceOfTheDay/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/RaceOfTheDayContracts.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceOfTheDayOutfit.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlowLogic.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsModel.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsTexts.cs"
  - "Assets/Speed-Staes/Scripts/UI/Home/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiV2Prefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneCosmetics.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/RaceOfTheDay/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Home/**"
touches_scene: false
branch: agent/T-087-race-of-the-day
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
Kullanici (03.11): "race of the day yapalim" (referans ne ise o). Referans olcumu (03.11 telefon + `docs/eksik-ui/05-gunun-yarisci-karti.png`,
`07-sonuc-kazanc-dokumu.png`):
- Etkinlik secimi "Race of the day" (biletsiz, sinirsiz, rakipli). Ana ekranda Custom secici yerine **"Racer of the day"** karti: gunun hazir
  yarisci kiyafeti karakterde; Edit racer / Emote / Custom oklari gizli. Referansta Power/Stamina/Height var - bizde yok, gosterilmez.
- **Her gun degisir**: hem kiyafet hem mesafe (30.10 ~60 m, 03.11 150 m: ara sureler 10/30/60/100 m, bitis 150). Yarista **butun
  kosucular** (oyuncu + botlar) gunun kiyafetiyle.
- Sonuc ekraninda "Personal Best / World Record" yerine **"Daily Personal Best" / "Daily World Record"** (gunun en iyisi, yerel liderlik
  tablosunun gunluk satiri - `docs/decisions/ADR-2026-10-30-liderlik-gunluk-en-iyiler.md`), oyuncu satirinda DB / PB rozetleri (var, T-047).
- Kazanc: diger yarislar gibi (bitirme + zorluk + rakip; referansta "Finished race +12"). Mor Mucevher yok (GDD).
Yapilacak: gunun kurali saf ve deterministik (UTC gun anahtari -> mesafe listesinden biri + kozmetik havuzundan bir kiyafet/ten; ayni gun her
acilista ayni; liste ve havuz config'te: mesafeler ornegin 60/100/150/200/300 m, engelsiz). Yaris akisi `race_of_the_day` etkinliginde mesafeyi
bu kuraldan alir (bugun `RaceFlowLogic` etkinlik tanimindan aliyor); botlar kalibrasyon tablosu olmayan mesafede T-086'nin olceklemesiyle kosar.
Kiyafet oyuncuya ve botlara `CosmeticApplier`/`RaceRunnerOutfit` yoluyla uygulanir (yalniz bu yarista; oyuncunun kendi kaydi degismez).
Ana ekran karti ve sonuc etiketleri builder'da. `race_of_the_day` etkinligi seeder'da acilir (`isPlayable`, biletsiz, rakipli, gem yok).
Paralel calisan T-090 (Practice paneli) `RaceSessionAdapter`, `RaceHurdles`, `RaceHud`'a yaziyor: sen onlara YAZMA.

## Kabul kriterleri
- [ ] Ayni UTC gunde ayni mesafe + kiyafet, ertesi gun degisir (test, saat sahte)
- [ ] Race of the Day secilince ana ekranda gunun yarisci karti; yarista herkes o kiyafetle; oyuncunun kendi kiyafet kaydi degismez
- [ ] Sonuc ekraninda Daily Personal Best / Daily World Record; biletsiz; Nakit doku calisir, gem yok
- [ ] EditMode testleri; sabit denge sayisi yok; needs'te WIRING/SCENE/ASSET tam

## Kapsam disi
- Power/Stamina/Height; engelli gunler; Practice paneli (T-090); Malik'in kodu

## Geri bildirim
GORSEL  : kart acilisinda mevcut panel reçetesi (UiMotionConfig)
SES     : mevcut buton Sfx
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
(Agent doldurur: 5 satir)
