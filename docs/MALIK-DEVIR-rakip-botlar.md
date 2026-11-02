# Malik'e devir: yarışta rakip botlar (30.10.2026)

> Yazan: UI/meta tarafı (Pundun oturumu), kullanıcı onayıyla. Senin dosyalarına (Scripts/Physical, RunnerInput,
> SideFollowCamera, Prefabs/Character, PhysicalTest) **hiç dokunulmadı**; botlar yalnız senin public API'ni kullanıyor.
> Referans analizi: `docs/analiz/2026-10-30-referans-oyun-rakipler.md`.

## Ne eklendi
- Race sahnesinde 5 bot (`Scripts/Bridge/RaceBots.cs`): her bot senin koşucu prefab'ının (`HumanCharacterDummy_F`) bir kopyası.
  GO'da kalkar, kendi şeridinde (kameranın uzak tarafı, 1.2 m arayla) koşar, bitişte durur; süresi sıralamaya girer.
- Bot "beyni" saf C#: `Scripts/Core/Runtime/Race/Bots/` (saha planı, basış takvimi, canlı sıralama), sayılar
  `Data/Config/BotRaceConfig.asset`'te (bot sayısı, zorluk, yayılım, tepki/periyot/tutma/titreme/hata aralıkları, kalibrasyon).
- Kalibrasyon aracı `Tools/Editor/Builders/BotCalibration.cs`: senin `RunnerSim`'ini prefab'taki `RunnerBody.tuning` ile
  başlıksız koşturup "beceri → 100 m süresi" tablosunu ölçer. Bugünkü ölçüm: beceri 0 → 22.3 sn, 1 → 16.0 sn.

## Senin koduna dayandığımız yerler (bunları değiştirirsen bize haber ver)
1. `RunnerInput.Inject(leg, pressed, gameTime)` — bileşen **kapalıyken** de kuyruğa yazar; bot kopyasında `RunnerInput.enabled = false`
   (klavye/dokunma dinlenmez), basışlar Inject ile gelir.
2. `RunnerBody.Update` kuyruğu her kare tüketir, olayı kendi zaman damgasıyla sim'e verir (geleceğe dönük olay "şimdi" sayılır;
   biz olayı zamanı gelince veriyoruz). Çalışma sırası: RunnerInput -200, **RaceBots -150**, RunnerBody -100.
3. `RunnerBody.Awake` başlangıç konumunu transform'dan alır: bot kopyası pasif bir kap altında konumlanıp sonra açılır.
4. `RunnerBody.ResetRun` / `RunReset` / `acceptInput` (bitişten sonra false yapıyoruz) / `Sim.Distance`, `Sim.Time`, `Sim.LastPressTime`.
5. `RunnerSim(tuning)`, `Press`, `Release`, `Advance`: `Advance(dt)` 1e-6 altındaki adımı yok sayıyor — kalibrasyon döngüsü
   buna göre korumalı (yoksa sonsuz döngü oluyordu).
6. Bot kopyalarında `RunnerBodyHUD` ve `RunnerReplay` **silinir** (ikisi de `FindObjectOfType` ile oyuncunun nesnelerini buluyor).

## Tek koşucu varsayımları (bulduklarımız ve çözümü)
| Yer | Varsayım | Çözüm |
|---|---|---|
| `RaceController.Start` | `body` boşsa `FindObjectOfType<RunnerBody>` | builder `body`'yi açıkça bağlar; botlar bütün `Start`'lardan sonra doğar |
| `LoopWayGenerator`, `ThemedLoopWay.Start` (Serhat) | `runner` boşsa `FindObjectOfType<RunnerBody>` | `RaceThemeView` + builder `runner`'ı açıkça verir |
| `RunnerBodyHUD`, `RunnerReplay` | `FindObjectOfType<RaceController / RunnerReplay>` | bot kopyalarında silinir |
Yeni bir `FindObjectOfType<RunnerBody>()` eklersen sahnede artık 6 RunnerBody var: lütfen açık referans kullan.

## Senin değişikliğin botları nasıl etkiler
- Fizik/tuning sayıları (prefab `RunnerBody.tuning`) değişirse botların süreleri kayar → kalibrasyonu yeniden koş:
  `Unity.exe -batchmode -nographics -quit -projectPath <kopya> -executeMethod SpeedStars.Tools.Builders.BotCalibration.Run -out <csv> -apply`
  ve `BotRaceConfig.cs`'teki varsayılan tabloyu asset'tekiyle eşitle.
- Botların sürücüsü açık döngü: basış takvimi koşucunun o anki durumuna (baş açısı, hız) bakmaz. İnsan gibi "geride kalınca
  hızlanan / baş öne düşünce sakinleşen" bir sürücü istenirse senin sim'inden okuma gerekir — konuşalım.

## Bilinen açıklar
- Oyuncu bitirince `RaceController` onu başa alıyor ve dünya yeniden kuruluyor; bitirmemiş botlar kadraj dışında koşmaya devam ediyor.
- Botlarda `RunnerContactShadow` yok (Race sahnesinde hiç yok).
- 5 ek rig'in (Animation Rigging + skinned mesh) telefondaki maliyeti ölçülmedi; ağırsa `BotRaceConfig.botCount` düşer.
