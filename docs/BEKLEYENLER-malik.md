# Malik'te bekleyenler — kod kontrolü

**Tarih:** 3 Kasım 2026
**Kaynak:** `docs/DURUM.md`, "Kullanıcıdan / başkalarından bekleyen" bölümü.

Her madde `main`'deki koda bakılarak kontrol edildi. Play'de denenmedi.

| # | Madde | Durum | Kısaca |
|---|---|---|---|
| 1 | Engel teması | Kodu yazıldı (03.11), denenmedi | Bacakların bar altından geçmesine bakılıyor |
| 2 | 4×100m ve Medley | Açık | Katalogda var, `isPlayable: 0`; bayrak/Medley kodu yok |
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

- `Data/Config/Events/4x100m.asset`: 400 m, `isPlayable: 0`.
- `Data/Config/Events/medley.asset`: mesafe 0, `isPlayable: 0`.
- Kodda geçenler yalnız menü/sıralama tarafında (`EventCategory`, `EventSelectScreenView`, `LeaderboardEventGroups`, seeder).
  Bayrak değişimi, takım ya da Medley kuralı yok.
- Diğer bütün etkinlikler açık: 100m, 200m, 400m, 60mH, 110mH, 400mH, Race of the Day, Practice.

**Yapılacak:** Önce kural: referans oyundan değişim anı, Medley'nin bacak mesafeleri. Sonra kod.
`docs/decisions/OPEN-2026-10-28-gdd-acik-kararlar.md` madde 9 bunu bekliyor.

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
