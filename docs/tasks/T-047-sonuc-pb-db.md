---
id: T-047
title: Yaris sonu - oyuncu satirinda PB (yeni kisisel rekor) ve DB (gunun en iyisi) rozetleri
status: merged
owner: ui-agent
depends_on: [T-033, T-037]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Race/Results*.cs"
  - "Assets/Speed-Staes/Scripts/UI/Race/RaceFlow*.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
touches_scene: true
branch: agent/T-047-pb-db
commits: [280ec1c]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/ResultsModelTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: WIRING
    durum: closed
    not: "RaceFlowLogic.BuildResults (Scripts/UI/Race/RaceFlowLogic.cs, owns_paths disinda) -> ResultsInput isDayBest: Record sonrasi IDayBestSource.CopyDayBests(eventId) ile bugunun (IClock.UtcNow.Date.Ticks) kaydini bul, ResultsModel.IsDayBest(has, ms, outcome.TimeSeconds) sonucunu ResultsInput'un son parametresine ver. IPersonalBests isNewRecord zaten geciyor (PB hazir)."
  - tip: SCENE
    durum: closed
    not: "ResultsTableRow prefab'i ve Race_results sahnesini builder ile yeniden uret (PersonalBestBadge/DayBestBadge Text'leri ve personalBestBadge/dayBestBadge alanlari yeni)"

started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
`docs/eksik-ui/README.md` B-8 (`07-sonuc-kazanc-dokumu.png`): sonuç tablosunda oyuncunun satırında, süreden sonra küçük rozetler: **PB** = bu
koşu yeni kişisel rekor (`IPersonalBests` yeni rekor bilgisi, T-033), **DB** = bu koşu oyuncunun bugünkü (UTC) en iyisi (`IDayBestSource`,
T-037). İkisi birden olabilir. Rozet sanatı yok: satır prefab'ında iki küçük legacy Text (PB teal #00B6B0, DB kırmızı #FF202D), yalnız
gerekince görünür; açılışta küçük punch (UiMotionConfig). Kazanç dökümü (Nakit) bu task'ta YOK (PDF'te yarış başı Nakit ödülü tanımlı
değil). Saf model (`ResultsModel`) rozet kararını verir, EditMode testli.

## Kabul kriterleri
- [ ] EditMode: ilk koşu = PB + DB; daha yavaş koşu = rozet yok; bugünün en iyisi ama eski rekordan yavaş = yalnız DB
- [ ] PlayMode yeşil; `Race_results` görüntüsünde düzen bozulmaz

## Calisma notu
Model: ResultRowData.ShowPersonalBest/ShowDayBest, ResultsInput.IsDayBest (opsiyonel), ResultsModel.IsDayBest(ms karsilastirma).
Satir: iki kucuk Text (PB teal, DB kirmizi), yalniz true iken acik, UiMotionConfig punch (Fill'e motion verildi).
Builder: Name ile Time arasi iki rozet slotu eklendi; prefab uretilmedi (SCENE need).
Flow: RaceFlowLogic IDayBestSource+IClock (opsiyonel, sonda) ile DB'yi hesaplar; RaceFlow InjectOptional; WIRING kapandi. 3 RaceFlowLogicTests eklendi.
Testler yazildi, kosulmadi.
