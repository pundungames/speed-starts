---
id: T-086
title: Diger mesafeler - 200m, 400m, 60mH, 400mH oynanir (botlar mesafeye gore kalibre, engeller etkinlik verisinden); 4x100m ve Medley Malik'te
status: merged
owner: gameplay-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBots.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceHurdles.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/Bots/**"
  - "Assets/Speed-Staes/Tools/Editor/Builders/BotCalibration.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneRaceHurdles.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneRaceBots.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/**"
touches_scene: false
branch: agent/T-086-mesafeler
commits: [aab94d62, 702b6fdd, 4eef70a2]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Core/Race/Bots/BotEventProfileTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: ASSET
    durum: closed
    not: "-> def94b0a, kalibrasyon kopyada 03.11. Kalibrasyon: kopyada `Unity.exe -batchmode -nographics -quit -projectPath <kopya> -executeMethod SpeedStars.Tools.Builders.BotCalibration.Run -out <klasor-disinda>/bot-cal.csv -events all -apply` kosulur; Data/Config/BotRaceConfig.asset'e eventProfiles (110mh,200m,400m,60mh,400mh) yazilir ve ayni satirlar BotRaceConfig.cs'te eventProfiles varsayilanina (<csv>.table.txt'teki new BotEventProfile(...) satirlari) kopyalanir. 100m satirlari bugunku tabloyla ayni kalmali (dogrulama: -events 100m)."
  - tip: ASSET
    durum: closed
    not: "-> def94b0a, 03.11. Etkinlikleri acmak: Data/Config/Events/200m.asset, 400m.asset, 60mh.asset, 400mh.asset -> isPlayable 0 -> 1 (ConfigAssetSeeder.cs / T-084 ile cakisirsa seeder varsayilanini da). Bilet kurali (requiresTicket) kullanici karari, degismedi. Bot tablolari asset'e yazilmadan 200m/400m acilirsa botlar yine calisir (eski olcekleme + uzun basis takvimi) ama sureleri olculmemis olur."
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11): "engelli engelsiz normal kosularin uzunluklarini yap; bayrak kismini Malik yapacak". Bugun oynanir: 100m (bilet), 110mH,
Practice. Acilacak: **200m, 400m, 60mH, 400mH** (katalogda var, `isPlayable 0`; engel verisi hazir: 60mH 5 engel 13.72/9.14, 400mH 10 engel
45/35, 110mH 10 engel 13.72/9.14). 4x100m ve Medley bu task'ta YOK (Malik).
Mekanik hazir (Malik f83ae9ec): `RaceSessionAdapter.RequestStart` etkinligin mesafesini `RaceController.raceDistance`'a yazar,
`RaceController.PlaceFinishLine` her sifirlamada bitis cizgisini oraya koyar; `RaceHurdles` engelleri etkinlik verisinden dizer; dunya
(`ThemedLoopWay`) kosucunun onunde parca uretip arkada siler (uzunluk siniri yok). Eksik olan **botlar**: `RaceBots` sureyi 100 m'de olculen
tabloyla dogrusal olcekliyor ("good near 100 m; a much longer event needs its own measurement") ve `BotFieldPlanner`'in yedek referans suresi
100 m icin. Yapilacak:
1) `BotRaceConfig`: kalibrasyon tablosu mesafe/engel profiline gore (etkinlik basina ya da mesafe basina satir listesi; 100m tablosu bugunku
   degerleriyle aynen kalir). Yedek referans suresi de profile gore. RaceBots/BotFieldPlanner dogru tabloyu secer; tablo yoksa bugunku
   olcekleme (geri uyum).
2) `BotCalibration` araci her profil icin olcer (engelli profilde botun ziplamasi dahil; RaceBots'taki ziplama kuraliyla ayni) ve `-apply` ile
   config'e yazar; menu + batchmode `-executeMethod` girisi. Sonsuz dongu korumasi (TUZAKLAR: `RunnerSim.Advance` dt <= 1e-6'yi yok sayar).
3) 400m/400mH: `RunnerSim` enerjisi (energyMax, drainPerPush) bu mesafede yetiyor mu, kod okuyarak hesapla; bitirilemez ya da bot/oyuncu
   cokuyorsa mekanik Malik'in -> needs: DECISION (ne gerektigi not'ta), kendin degistirme.
4) Engel yerlesimi 60mH/400mH icin dogru mu (`RaceHurdles` + `SceneRaceHurdles`), bitis cizgisinin engelle cakismamasi.
Etkinlikleri acmak (isPlayable) ve bilet kurali asset degeridir: sen yazmazsin -> needs: ASSET (hangi asset, hangi alan). `Meta/Config/**` ve
`ConfigAssetSeeder.cs` T-084'te (paralel calisiyor): onlara YAZMA; gerekirse needs'e yaz.
Malik'in dosyalari (`Scripts/Physical/**`, `Scripts/Runner/RunnerInput.cs`, `SideFollowCamera.cs`) hook'la kilitli: okunur, yazilmaz.

## Kabul kriterleri
- [ ] Her acilacak etkinlik icin bot tablosu profiline gore secilir; 100m davranisi degismez (test)
- [ ] Kalibrasyon araci profil basina olcer, batchmode'dan kosulabilir; supervizor kosup asset + kod varsayilanina yazar (needs: ASSET)
- [ ] 400m enerji hesabi calisma notunda (yetiyor / yetmiyor + sayilar)
- [ ] EditMode testleri; sabit denge sayisi assert yok

## Kapsam disi
- 4x100m, Medley (Malik); HUD pist ilerleme halkasi; bilet kurali karari (kullanici); ekonomi (T-084)

## Calisma notu
1. Bot tablosu artik etkinlik basina: `BotRaceConfig.eventProfiles` (BotEventProfile: eventId, fallbackReferenceSeconds, calibration). 100m ve profili olmayan etkinlik eski tabloyla/olceklemeyle ayni (test); `BotEventSetup` secimi, `BotFieldPlanner.Plan(..., profile)` tabloyu uygular.
2. Ziplama kurali saf `BotJumpDriver`a cikti; RaceBots ve kalibrasyon ayni `RaceBots.WantsJump`i kullanir. `BotCalibration` her etkinligi kendi mesafesi + engelleri + ziplamasiyla olcer (-events, -apply: 100m tabana, digerleri profile).
3. 400m enerji: RunnerSim yalniz basis basina 0.25 harcar (kosarken yenilenme yok, tutmada 2/sn); ritim 0.32 sn'de 400 m ~125-200 basis = 31-51 enerji, en hizli bot (0.24 sn, ~40 sn) ~42, en yavas bot (0.55 sn, ~91 sn) ~41; lowEnergy 25'e inmez -> yetiyor. Arac enerji sayisini da olcer (table.txt basligi).
4. Engel yerlesimi dogru: 60mH son engel 50.28 (cizgi 60), 110mH 95.98 (110), 400mH 360 (400); cizgiyle cakisma yok (varlik testi). Seam ve builder degismedi (SceneRaceHurdles/RaceHurdles/RaceSessionAdapter dokunulmadi).
5. Bıraktığım: kalibrasyonu ben kosmadim (Unity yasak); tablolar yazilana kadar yeni mesafelerde botlar eski olcekleme ile calisir. Yeni dosya: BotEventProfileTests.cs (.meta kopyada uretilmeli). Dunya parcasi bot onde giderse uretilmis mi dogrulanmadi (Serhat'in uretimi, 400m'de bakilmali).
