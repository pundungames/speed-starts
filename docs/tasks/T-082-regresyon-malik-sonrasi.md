---
id: T-082
title: Faz 8 regresyon eki - Malik'in 02.11 degisiklikleri (kamera ayari kaydi, bilet penceresi, Practice/110mH bileti, Stats'ta botlar, bot cekilisi)
status: todo
owner: test-agent
depends_on: [T-053]
owns_paths:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/**"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Race/**"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/**"
touches_scene: false
branch: agent/T-082-regresyon-ek
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
(Agent doldurur: ne yapti, nerede zorlandi, ne birakti - 5 satir)
