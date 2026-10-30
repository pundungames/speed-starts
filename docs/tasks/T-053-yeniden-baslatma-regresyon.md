---
id: T-053
title: Faz 8 regresyon - uygulamayi oldurup acma, saat geri alma ve ileri sarma, bozuk kayit; butun meta sistemleri birlikte
status: review
owner: test-agent
depends_on: [T-042, T-046]
owns_paths:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/**"
touches_scene: false
branch: agent/T-053-regresyon
commits: []
tests:
  - Assets/Speed-Staes/Tests/EditMode/Meta/Regression/RegressionHarness.cs
  - Assets/Speed-Staes/Tests/EditMode/Meta/Regression/RestartTimersRegressionTests.cs
  - Assets/Speed-Staes/Tests/EditMode/Meta/Regression/RestartDayRewardsRegressionTests.cs
  - Assets/Speed-Staes/Tests/EditMode/Meta/Regression/RestartMoneySaveAndResetRegressionTests.cs
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
YOL-HARITASI Faz 8: "kill/restart, saat kaymasi, cevrimdisi". Tek tek servis testleri var (`Tests/EditMode/Meta/**`); eksik olan,
**ayni kayit uzerinde servislerin yeniden kurulmasi** (uygulama kapanip acildi) ve saat oyunlarinin **sistemler arasi** etkisi.
Gercek `ProgressRepository` + gercek servisler (Tickets, TimedAccess, EconomyLedger, DailyGift, SevenDay, PersonalBests/DayBest,
Cosmetics, Themes, Settings, ProgressReset) sahte `IClock` ve gecici kayit yoluyla kurulur; "yeniden baslatma" = butun servisleri ayni
kayittan yeniden olusturmak. Mevcut test kitlerini (`EconomyTestKit`, `CosmeticTestKit`, ProgressRepository testlerinin kurulumu)
yeniden kullan; config'ler test icinde kurulur, sabit denge sayisi assert edilmez (iliski ya da config'ten okunan deger).

Senaryolar (en az):
1) Bilet harca -> yeniden baslat -> sayi ve yenilenme zamani ayni; kapaliyken N aralik gecti -> tavana kadar telafi, fazlasi yok.
2) Sureli Bilet etkinken yeniden baslat -> kalan sure gercek saatle akmis; bitis aninda kapali.
3) Gunluk hediye al -> yeniden baslat ayni gun -> tekrar alinamaz; saat 1 gun geri -> yine alinamaz ve bilet uretmez; ertesi UTC gunu -> alinir.
4) 7 gun: G1 al -> saat ileri 3 gun -> sadece bir kart alinabilir (kacirilan gun sifirlamaz); saat geri -> ilerleme geri gitmez.
5) Odeme islem kimligi: ayni yaris odulu yeniden baslattiktan sonra ikinci kez verilmez.
6) Kayit dosyasi bozuk/yarim yazilmis -> yedekten ya da yeni oyuncu olarak acilir, istisna firlatmaz (ProgressRepository'nin bugunku
   sozlesmesine gore; neyse onu belgeleyen test).
7) Tam sifirlama -> yeniden baslat -> yeni oyuncu + ayarlar korunmus; rekor sifirlama -> liderlikte oyuncu yok.

Bir senaryo **kod hatasi** gosterirse kodu duzeltme: testi `[Ignore("BUG: ...")]` ile birak, calisma notuna ve raporuna yaz (supervizor
ayri task acar). Kod ile senin beklentin celisiyor ama hangisinin dogru oldugu belirsizse KOD haklidir (CALISMA-KURALLARI §6).

## Kabul kriterleri
- [x] Yukaridaki 7 senaryo testli, hepsi tek asmdef'te (var olan EditMode test assembly'si)
- [x] Bulunan hata varsa `[Ignore("BUG: ...")]` + rapor; sessizce gecen yok
- [x] Gecici dosyalar test sonunda silinir

## Geri bildirim
GORSEL  : yok
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- 7 senaryo icin 3 test sinifi + RegressionHarness (gercek ProgressRepository, ClockService - son gorulen damga kayitta -, tum servisler; Boot() = uygulamayi oldurup acma).
- Kod hatasi bulunmadi, [Ignore] yok. Gozlem: EconomyConfig.dayBoundary (seed LocalMidnight) hicbir kod tarafindan okunmuyor; RewardDay hep UTC - testler UTC varsayar.
- Calistirilmadi (merkezi dogrulama bekliyor). Yeni klasorun .meta'lari yok: kopyada derlenip .meta alinmali (TUZAKLAR .meta maddesi).
- Sabit denge sayisi assert edilmiyor: degerler harness'te test girdisi, beklentiler iliski ya da config'ten okunuyor.
- Kapsam disi: gercek uygulama yasam dongusu (OnApplicationPause/Quit), PlayerPrefs, emote servisi.
