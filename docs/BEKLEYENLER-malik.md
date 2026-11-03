# Malik'te bekleyenler — kod kontrolü

**Tarih:** 3 Kasım 2026
**Kaynak:** `docs/DURUM.md`, "Kullanıcıdan / başkalarından bekleyen" bölümü.

Her madde `main`'deki koda bakılarak kontrol edildi. Play'de denenmedi.

| # | Madde | Durum | Kısaca |
|---|---|---|---|
| 1 | Engel teması | Kodu yazıldı (03.11), denenmedi | Bacakların bar altından geçmesine bakılıyor |
| 2 | 4×100m ve Medley | 4×100m NPC'lerle oynanır (03.11), Medley açık | Dört bacağı da NPC koşuyor; oyuncu girdisi karakter dönünce |
| 3 | UI'a dokunmanın adım sayılmaması | Açık | `RunnerInput` UI kontrolü yapmıyor |
| 4 | Practice: enerji kilitleme ve ayakta çıkış | Açık | API yok; paneldeki satırlar gri bekliyor |
| 5 | Ayak kilidi: ayak yerde kayıyor mu? | Kilit yok | Yerdeki ayak dünyaya sabitlenmiyor; kayma ölçülmedi |

Watch Replay listeden çıktı: 03.11'de yapıldı (`cbefbf82`, `main`'e `e7d269f2` ile girdi).

## 1. Engel teması

- `Scripts/Physical/RunnerHurdles.cs`, `Update`: koşucu engel çizgisini geçerken `sim.JumpInAir` doğruysa geçti, değilse
  `HitHurdle` sayılıyor. Gövde yüksekliğine ya da gerçek bir temasa bakılmıyor.
- Fizik temas kullanılmıyor. Prefab'sız yedek küplerin collider'ı siliniyor (`Part`). Prefab'ın collider'ı kalıyor ama hiçbir
  kod okumuyor. Koşucu da fizik gövdesiyle değil, `transform` ile ilerliyor.
- Botlar aynı sınıfı kullanıyor (`Scripts/Bridge/RaceBots.cs`, `CreateLane`). Düzeltme ikisine birden gelir.
- Ayrıntı: `docs/ENGEL-TEMAS-NOTU-malik.md`.

**Yapılacak:** Geçiş anında ayak/gövde yüksekliğini engel yüksekliğiyle karşılaştırmak (görsel taraftan okunur,
fizik değişmez) ya da yerde atlamayı geçersiz saymak. Hangisi olacağına Malik karar verir.

## 2. 4×100m ve Medley

**03.11'de yapılan (4×100m, NPC'lerle):**
- Referans oyundan ölçüldü (telefon kaydı, kare kare): 8 takım; sıradaki koşucu bölgede bekler, gelen koşucu ~8–12 m
  yaklaşınca kendisi çıkar, kaçmaz (gelen koşucunun hızının altında kalır), fark 0 m olunca bayrak geçer (çizgiden birkaç
  metre önce); gelen koşucu dik koşarak yavaşlar. HUD: sıradaki koşucunun üstünde geri sayan "Xm", çizgide bant
  "100m: 23.82s (+9.24)" + "2nd leg: 12.28", "Perfect/Late handoff!", soldaki ara süre listesi; her bacakta enerji dolu.
  Erken basış hatalı çıkış sayılır (mevcut kural).
- Kod: `Bridge/RaceRelay.cs` (takımlar, değişim, ara süreler, sıralama, bitiş), `Bridge/RaceRelayHud.cs` (çalışma anında
  kurulan HUD; sahne/prefab değişmedi), kurallar `Core/Runtime/Race/Relay/RelayRules.cs` (+ EditMode testi).
  `RaceBots` relay'de kendi botlarını kapatıp relay'i çalıştırır; `RaceSessionAdapter.FinishRelay` takımın bitişini sonuca
  çevirir; `RaceThemeView.Follow` dünyayı bayrağı taşıyana bağlar; `BotFieldPlanner.Plan(count:)` takım başına plan.
  `RaceEventDefinition.relayLegCount` (4×100m: 4, `isPlayable: 1`).
- Değişim kaydırmayla (03.11 akşam, Malik'in tarifi): yukarı kaydırınca bayraklı kol öne, diğer kol geriye, alıcının kolu
  geriye uzanır; bayrak yetişirse geçer → "Perfect". Kaydırma olmadan koşucular birbirine değince bayrak yine geçer →
  "Late" (referansta kaydırmadan koştuğumda hep Late). NPC'ler 1.7 m'de kendileri kaydırır (%10 kaçırır). Kol pozu
  `Bridge/RelayBatonArms.cs` (PoseDriver'dan sonra, sadece relay kopyalarında). Bayrak: silindir (0.045 × 0.30 m),
  `B-handProp.L/R`'de, sıra sağ-sol-sağ-sol, materyal `CornerShop/Blank Sign Red.mat` (4×100m `batonMaterial`).
  Tutuş açısı Malik'in play'de verdiği: sol el (357.97, 16.44, 15.23), sağ el aynası (357.65, 344.21, 344.92).
  Bekleyen koşucu çıktığı andan bayrağı alana kadar bayrak elini geride tutarak koşar: üst kol geriden 30° aşağı,
  dirsek 18.7° bükük (Malik'in play'de verdiği poz; `RelayBatonArms.takeDown` / `takeElbow`).
- Oyuncunun takımı da NPC: oyuncunun kendi koşucusu gizli bekler, kamera bayrağı izler. Karakter dosyalarına dokunulmadı.
- Fark: bizim fizikte duruştan 7 m/s ~3.5 sn sürüyor (referansta daha kısa); bu yüzden çıkış farkı 20 m (`startGapMeters`),
  Perfect penceresi 4 sn. Play ölçümü: bayrak ~8.5 m/s'de, çizgiden ~5 m önce geçiyor; bacaklar 12.0 / 10.0 / 10.8 / 9.7 sn.

**Açık:**
- Oyuncu girdisi (karakter dönünce): oyuncunun dokunuşu bayrağı taşıyan koşucuya gitsin; menüde bacak seçici
  (`◀ 1st leg ▶`, bacak renkleri, Custom 1–4 koşucu, bacağa göre Edit racer) — UI tarafı.
- Watch Replay ve Analysis relay'de oyuncunun gizli koşucusunu gösterir (relay izi yok).
- 32 rig (8×4) telefonda ölçülmedi; aynı anda takım başına ~2–3'ü açık.
- Kaydırma kuralı videoyla doğrulanmadı (kayıt telefonda `/sdcard/cc_relay2.mp4`, bağlantı koptu): bayrak eli sırası,
  alıcının kol pozu, erken/geç kaydırmanın sonucu.
- Medley: bacak mesafeleri ölçülmedi, `isPlayable: 0`.

## 3. UI'a dokunma adım sayılmasın

- `Scripts/Runner/RunnerInput.cs`: dokunmalar `EnhancedTouch` (`onFingerDown/Up/Move`) ve eski `Input.GetTouch` ile
  okunuyor. `EventSystem` / `IsPointerOverGameObject` kontrolü yok.
- Sonuç: Practice paneline, emote butonuna, duraklat butonuna basmak da bir ayak basışı oluyor.

**Yapılacak:** Parmak basarken UI üstündeyse o parmağı yok saymak (basış ve bırakış ikisi de).
Küçük bir iş; dosya `RunnerInput`, Malik'in alanı.

## 4. Practice: enerji ve ayakta çıkış

- `RunnerSim.Energy` dışarıdan yazılamıyor (`private set`). Doldurma ya da kilitleme yok.
- Ayakta çıkış (standing start) mekaniği yok. Koşu hep çömelme duruşundan başlıyor.
- UI hazır ve bekliyor (T-090): `Meta/Practice/PracticeContracts.cs` içinde `IPracticeEnergy` ve
  `IPracticeStandingStart`. Bağlantı `PracticeSession.BindEnergy` / `BindStandingStart` ile.

**Yapılacak:**
- `RunnerSim`'e `RefillEnergy()` ve `lockEnergy` bayrağı. Kilitliyken harcama durur.
- Ayakta çıkış için bir bayrak ve ona göre başlangıç duruşu. Bu görsel taraf da ister (karakter animatörde).
- İkisini `PracticeSession`'a bağlayan küçük bir köprü.

## 5. Ayak kilidi: ayak yerde kayıyor mu?

- `RunnerPoseDriver` bacakları açıyla sürüyor (faz → açı → FK → IK). Yerdeki ayak için yalnız **dikey** koruma var:
  ayak yerin altına girmez, gövde yüksekliği bacak açılarından gelir.
- **Yatay kilit yok.** Yerdeki ayak dünyada bir noktaya sabitlenmiyor. Kayıp kaymayacağı, itiş fazında uyluğun dönüş
  hızının koşu hızıyla ne kadar eşleştiğine bağlı.
- Kayma miktarı ölçülmedi. `docs/ANIMASYON-KOSU.md` §5 madde 4 aynı fikri öneriyor.
- Karakter şu an animatörde. Bu iş onun dosyalarına (`RunnerPoseDriver`) dokunur.

**Ölçmek için:** Play'de, ayak yerdeyken `B-foot` (ya da ayak IK hedefi) dünya X'inin kaç cm kaydığına bakılır. Ritim
ve sık basma ayrı ayrı ölçülür. Kayma birkaç cm'yi geçiyorsa itiş açıları ya da bir ayak kilidi düşünülür.

## Öneri sırası

1. **UI dokunma (3):** En küçük iş, oyuncuyu doğrudan etkiliyor.
2. **Enerji API'si (4, ilk yarı):** Küçük. Panel hazır bekliyor.
3. **Engel teması (1).**
4. **Ayak kaymasını ölçmek (5):** Karakter animatörden dönünce.
5. **Ayakta çıkış (4, ikinci yarı):** Karakter animatörden dönünce.
6. **4×100m ve Medley (2):** Önce kural kararı.
