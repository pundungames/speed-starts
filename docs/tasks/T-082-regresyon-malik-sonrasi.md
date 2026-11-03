---
id: T-082
title: Faz 8 regresyon eki - Malik'in 02.11 degisiklikleri (kamera ayari kaydi, bilet penceresi, Practice/110mH bileti, Stats'ta botlar, bot cekilisi)
status: review
owner: test-agent
depends_on: [T-053]
owns_paths:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/**"
touches_scene: false
branch: agent/T-082-regresyon-ek
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/CameraDistanceSettingRegressionTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/EventEntryRulesRegressionTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/RaceFlowRegressionTests.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/BotRaceDrawTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
T-053 meta sistemlerin yeniden baslatma / saat / bozuk kayit regresyonunu kurdu. Malik 02.11'de bizim alanda yeni davranis ekledi ama
yalniz sahte siniflari guncelledi, yeni davranisin testi yok (`git show fd891f4d 27282cdf 8b246685 1f18e51f 67f757fa`). Bu task o
davranislari mevcut test kitleriyle (`RegressionHarness`, `EconomyTestKit`, `RaceFlowLogicTests` kurulumu, bot testleri) kilitler.
Yalniz test yazilir; urun koduna dokunulmaz. Test bugunku kodla kirmiziysa: once kodun niyetine bak (commit mesaji); kod hakli ise
testi koda uydur, gercek hata gibi gorunuyorsa testi `[Ignore("T-082: ...")]` ile birak ve calisma notuna yaz (supervizore bildirim).
Sabit denge sayisi assert edilmez: sayi config'ten okunur ya da iliski assert edilir. Once oku: `docs/TUZAKLAR.md`.

Senaryolar (en az):
1) Kamera mesafesi ayari (`PlayerSettingsService`, `SettingsConfig`, `PlayerProgress`): yaz -> yeniden baslat -> ayni deger; ilerleme
   sifirlamasi (`ProgressReset`) degeri korur (commit boyle diyor); config araligi disi deger nasil davraniyorsa onu belgeleyen test.
2) Yaris akisi (`RaceFlowLogic`): Adet Bileti 0 iken "Race again" -> `IEntryGatePrompt` acilir, bilet harcanmaz, yaris baslamaz.
3) Bilet gerektirmeyen etkinlik (katalogda `requiresTicket` kapali olanlar: Practice/training, 110mH) bilet harcamaz; Practice rakipsizdir
   (`hasOpponents` kapali) - kural koddan okunur, etkinlik adi sabit yazilmaz.
4) Stats/analiz: `IRaceTraces`'ten gelen bot izleri analiz serisine donusur; oyuncudan sonra bitiren bot sure alir (DNF degil).
5) Bot sahasi (`BotFieldPlanner`): ayni seed ayni plan; kolay/zor yaris cekilisi butun sahayi config carpaniyla kaydirir (iliski),
   sans 0 iken saha degismez.

## Kabul kriterleri
- [ ] 5 senaryonun her biri en az bir testte; test dosyalari `tests:` alaninda
- [ ] Urun kodunda degisiklik yok; sabit denge sayisi assert yok
- [ ] Kirmizi/Ignore edilen test varsa calisma notunda sebebiyle

## Kapsam disi
- PlayMode / sahne testleri, Malik'in `Scripts/Physical/**` ve `Scripts/Runner/**` kodu (asmdef'siz; test assembly'si erisemez)
- Cevrimdisi: oyunda ag kodu yok (Scripts altinda UnityWebRequest/HttpClient yok) - test gerekmez

## Calisma notu
1. Dort yeni test dosyasi (kamera ayari kaydi/sifirlama, etkinlik giris kurallari, yaris akisi bilet+bot analizi, bot yaris cekilisi); urun kodu ve denge sayisi yok.
2. Senaryo 2'nin sahte kapili hali RaceFlowLogicTests'te zaten vardi; gercek RaceEntryGate + bilet servisiyle (bilet harcanmaz, pencere acilir, yaris baslamaz) yeni dosyada eklendi.
3. Senaryo 3 sevk edilen RaceEventCatalog.asset'ten (AssetDatabase) kural olarak okunur; etkinlik adi yok. RaceBots.HasOpponents (Assembly-CSharp) test assembly'sinden erisilemez, yalniz katalog bayragi kilitlendi.
4. Hicbir test Ignore edilmedi; Unity calistirilmadi (derleme/kosu kopyada beklenir). Yeni .cs'lerin .meta'si supervizorde.
5. Dikkat: trace degisiminde yeniden cizim yalniz Results acikken calisir; Analysis ekrani Results'tan once acilirsa bot zamani gelmez (test kilitlemedi, davranis kodda boyle).
