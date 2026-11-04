---
id: T-106
title: Sonuc ekrani - Practice dugmesi calissin, kazanilan Nakit okunur olsun; Antrenman Alani adi Ingilizce
status: merged
owner: ui-agent
depends_on: [T-090]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/ResultsScreenView.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlowLogic.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceViewContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/**"
  - "Assets/Speed-Staes/Tests/PlayMode/**"
touches_scene: false
branch: agent/T-106-sonuc-pratik-nakit
commits: [05f648b1, 7006df75, aa6b9787, c1176376]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowPracticeTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/RaceEventCatalogTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/PracticePanelSmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Yama metodu kopyada kosulacak: ResultsScreen.prefab CashBreakdown genisligi ve yazi boylari; training.asset displayName -> Practice. Supervizor kosar ve alir -> kopyada iki kosu, bayt farki yok; c1176376"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Kullanici (04.11) uc sey soyledi:
1. "Resultta neden Practice butonu var ama calismiyor; ya kaldiralim ya islevli hale getirelim." -> **Islevli** (dugme ve yeri kullanicinin duzeninde duruyor, Practice T-090'dan beri var; koddaki "not playable yet" yorumu bayat).
2. "Ana menude event secerken Turkce bir seyler yaziyor, Ingilizce yaz; baska yerde Turkce kaldiysa duzelt." -> Tarandi: oyuncuya gorunen tek Turkce metin `training` etkinliginin adi "Antrenman Alani" (`Data/Config/Events/training.asset`, `ConfigAssetSeeder.cs:49`). Yeni ad **"Practice"**. `RunnerBodyHUD` Turkce ama Malik'in gelistirici paneli (yazilmaz).
3. "Result panelde kac elmas kazandim hicbir sey belli degil." -> "Elmas" = Nakit (ikonu `diamond_icon`, kutu `Diamond`). `CashBreakdown` 198x161 px kutuya +30 ve uc satir sigdirmaya calisiyor, satirlar auto size ile 12-26 pt'ye dusuyor.

Yapilacak:
1. **Practice dugmesi:** `ResultsScreenView` `PracticeRequested` olayi; `practiceButton.interactable` calistirilabilir Practice etkinligi varken acik (`interactable = false` satiri ve bayat Tooltip/summary gider). `RaceFlowLogic` isler: Practice etkinligini katalogdan bulur (kural Practice paneliyle ve `RaceFlow.IsPracticeIn` ile ayni: `!HasOpponents`), secer, giris kapisindan gecirir, Race sahnesini yeniden acar (`HomeEntryFlow` ile ayni yol). Botlar sahne acilisinda kuruldugu icin yerinde `BeginRace` YETMEZ; `SceneFlow` ayni sahneye gecisi desteklemiyorsa bunu calisma notuna yaz ve en kucuk cozumu uygula. Catalogda Practice yoksa dugme kapali kalir.
2. **Adlar:** seeder'daki "Antrenman Alani" -> "Practice". Asset'i ben yazarim. Her etkinlik adinda Turkce harf olmadigini sevk edilen katalog asset'inden okuyan test ekle.
3. **Nakit okunur:** `UiResultsPrefabs`'a (dosya adi farkliysa Results builder'i) hedefli yama `PatchResultsCashBreakdown` (`public static`, argumansiz). `CashBreakdown` kutusu sagdaki bos alana genisler: x 348..778, y 29..200 (2400x1080 referans; `TopCards` x 789'da, `RaceAgain` y 210'da baslar, ikisine de degmez). Toplam yazi en cok 64 pt, satirlar en cok 34 pt; en kucuk boylar okunur kalsin (toplam 40, satir 24). Satirlar tek satirda sigsin (`Difficulty bonus +10`). Yalniz bu dugum ve dort yaziya dokunur; ikinci kosuda dosyada fark yok. Sifirdan kurulum da ayni degerleri uretir.
4. Runtime'da hiyerarsi kurulmaz. Prefab/asset'e elle yazma; Unity/test calistirma.

## Kabul kriterleri
- [ ] Results'ta Practice'e basinca Practice etkinligi bilet harcamadan rakipsiz baslar; bitiste Results acilmaz (T-092 kurali), Practice panelinin Exit/Restart'i calisir
- [ ] Practice etkinligi yoksa dugme kapali; `Denied` geri bildirimi mevcut ButtonFeedback'ten gelir
- [ ] Etkinlik secim ekraninda "Practice" yazar; sevk edilen katalogda Turkce harfli ad yok (test)
- [ ] Sonuc ekraninda toplam Nakit ve uc satir okunur boyutta ve kesilmeden gorunur; Race Again ve TopCards ile cakismaz (supervizor ekran goruntusuyle bakar)
- [ ] Yama idempotent; kullanicinin diger elle ayarlari degismez
- [ ] Test sabit denge sayisi assert etmiyor; `RaceFlowLogic` mantigi EditMode testli, dokunma akisi PlayMode testli (`PlaySmokeSupport.Tap`)
- [ ] needs listesinde open WIRING/SCENE yok

## Geri bildirim
GORSEL  : mevcut ButtonFeedback punch; Nakit acilis animasyonu ve sayac aynen
SES     : degismez
DOKUNSAL: degismez
ZAMAN   : yok
KAMERA  : yok

## Kapsam disi
- Mor Mucevher satiri, odul sayilari, Malik'in `RunnerBodyHUD`'u. Sonuc ekraninin diger yerlesimi.

## Calisma notu
- Practice: yeni `IResultsPracticeView` (olay + SetPracticeAvailable); `RaceFlowLogic.OnPractice` Practice etkinligini (RaceFlow katalogdan: oynanabilir, biletsiz, rakipsiz) gate.Enter ile girer, secer, `SceneFlow.GoTo(Race)` ile sahneyi YENIDEN ACAR (SceneFlow ayni sahneyi yeniden yukleyebiliyor, UnitySceneLoader single mode). Pending giris alinmaz, yeni sahne Start'ta alir.
- Nakit: `UiResultsPrefabs.PatchResultsCashBreakdown` (public static, idempotent: yalniz fark varsa kaydeder). Prefab'taki yazilar TMP; yama TMP ve legacy Text'i ikisini de isler, builder de ayni `ApplyBreakdownLayout`i kullanir.
- Kutu 430x171 (x348 y29), toplam 66 yuk / 64..40 pt, satir 34 yuk / 34..24 pt; 66+3x34=168 <= 171.
- Ad: seeder "Practice"; asset (training.asset) yazilmadi, katalog testi asset degisene dek kirmizi olur. Yeni dosya yok (.meta gerekmedi).
- Bilinmeyen: m_fontSize auto-size ile TMP'de degisebilir; kontrol yalnizca base/min/max'a bakar. Gorsel dogrulama (ekran goruntusu) supervizorde.
