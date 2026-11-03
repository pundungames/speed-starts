---
id: T-094
title: Watch Replay bizim akislarda - PlayMode duman testi (100m, Race of the Day, Race Again sonrasi), bulgular rapor
status: merged
owner: test-agent
depends_on: [T-092]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/WatchReplaySmokeTests.cs"
touches_scene: false
branch: agent/T-094-watch-replay-duman
commits: [632595af, 68022b57]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/WatchReplaySmokeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Malik 03.11'de Watch Replay'i ekledi (cbefbf82, merge e7d269f2): kayit `Scripts/Physical/RaceRecorder|RacePlayback|RaceRecording|ReplayCamera`,
kopru `Scripts/Bridge/RaceReplayAdapter.cs` (+ `Core/Runtime/Race/IRaceReplay.cs`), arayuz `Scripts/UI/Race/ReplayControlsView.cs`
(+ `ReplayControlsSkin`), sonuc ekraninda `ResultsScreenView` Watch Replay butonu. Bizim akislarla birlikte hic denenmedi.
Gercek tiklamali PlayMode duman testi yaz (`Tests/PlayMode/PlaySmokeSupport.cs` yardimcilari, mevcut dev bitis tetikleyicisi; kalip:
`HomeRaceButtonSmokeTests`, `EconomyV2UiSmokeTests`):
1) 100m: bitis -> Results -> Watch Replay -> replay kontrolleri gorunur, kosucu(lar) oynatiliyor (mesafe ilerliyor) -> replay biter ya da
   kapatilir -> Results'a donulur -> Race Again yeni yarisi normal baslatir (READY/SET/GO, girdi GO'da), Nakit iki kez yazilmaz.
2) Race of the Day: ayni akis; replay sirasinda kosucular gunun kiyafetinde kalir (RaceOfTheDayOutfit), Daily PB/WR satirlari bozulmaz.
3) Practice (T-092'den sonra): sonuc ekrani yok -> Watch Replay'e ulasilmaz; test bunu dogrular.
Urun koduna YAZMA. Bir adim kirmiziysa sebebi koddan bul, testi [Ignore] DEGIL; calisma notuna "BULGU: <dosya:satir> <ne oluyor>" yaz,
supervizor Malik'e / ilgili task'a tasir. Malik'in kodu (`Scripts/Physical/**`) PlayMode asmdef'inden gorunmez: mevcut testlerin
yaptigi gibi Core arayuzleri (IRaceReplay, IRaceSession) ya da reflection.
Not: ReplayControlsView UI'i runtime'da kuruyor (bizim kural 3.1'e aykiri, Malik'in kodu) - testte yalniz gozlemle, duzeltme konusu degil.

## Kabul kriterleri
- [x] WatchReplaySmokeTests: 1-3 adimlari gercek tiklamayla
- [x] Kirmizi adim varsa BULGU satiri (dosya:satir + gozlenen davranis)
- [x] Unity calistirma (merkezi dogrulama supervizorde)

## Kapsam disi
- Replay mekaniginin kendisi (Malik), ReplayControlsView'in prefab'a cevrilmesi (karar bekliyor)

## Calisma notu
- Yazildi: WatchReplaySmokeTests, 4 test (100m Exit+Race Again, 100m sona kadar+ikinci izleme, Race of the Day kiyafet/Daily satirlari, Practice sonuc yok). Koşulmadi (merkezi dogrulama).
- Bitis GERCEK: DevForceFinish kullanilmadi; RaceController.raceDistance reflection ile 15 m'ye kisaltilip Inject ile kosuluyor (sebep BULGU 1). Sayi assert yok: Cash kendisiyle, kiyafet kendi onceki gorunumuyle, Daily satirlari onceki metinle karsilastirilir.
- Practice testi T-092 sonrasi davranisi dogrular; T-092 main'de degilken KIRMIZI beklenir (RaceFlowLogic.cs:170 Results'i push ediyor).
- BULGU: Scripts/Bridge/RaceSessionAdapter.cs:238-243 - DevForceFinish oturumu Complete() ile bitirir ama RaceController kosuyu bitirmez (Current Running kalir, LastRaceTime degismez); RaceRecorder.cs:30 HasRace false kalir, Watch Replay dugmesi gri: dev tusuyla (F) acilan Results'ta replay denenemez.
- BULGU: Scripts/Physical/RaceRecorder.cs:30 - HasRace `Last != null`: ilk yaristan sonra Last hep dolu; sonraki yarista dev bitisle Results acilirsa Watch Replay ESKI yarisi oynatir (Race Again sonrasi). Ayrica ReplayControlsView hala runtime'da kuruluyor (bilinen, kapsam disi).
- Supervizor 03.11: kopyada 4 test yesil (PlayMode 51/51). BULGU 1-2 (dev bitis tusu F replay'i gormuyor / eski yarisi oynatir) yalniz gelistirici yolunda; Malik listesine (DURUM) tasindi.
