---
id: T-094
title: Watch Replay bizim akislarda - PlayMode duman testi (100m, Race of the Day, Race Again sonrasi), bulgular rapor
status: todo
owner: test-agent
depends_on: [T-092]
owns_paths:
  - "Assets/Speed-Staes/Tests/PlayMode/WatchReplaySmokeTests.cs"
touches_scene: false
branch: agent/T-094-watch-replay-duman
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
- [ ] WatchReplaySmokeTests: 1-3 adimlari gercek tiklamayla
- [ ] Kirmizi adim varsa BULGU satiri (dosya:satir + gozlenen davranis)
- [ ] Unity calistirma (merkezi dogrulama supervizorde)

## Kapsam disi
- Replay mekaniginin kendisi (Malik), ReplayControlsView'in prefab'a cevrilmesi (karar bekliyor)

## Calisma notu
