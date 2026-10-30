---
id: T-055
title: Kucuk borclar - okuyucusuz dayBoundary ayari, sonuc ekraninda iki satira kirilan Watch Replay, kostumde kenara dayanan Leggings
status: review
owner: ui-agent
depends_on: []
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Config/EconomyConfig.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ConfigAssetSeeder.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/EconomyConfigTests.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiResultsPrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiCostumePrefabs.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Ui/UiBuildKit.cs"
  - "docs/tasks/_backlog.md"
touches_scene: true
branch: agent/T-055-kucuk-borclar
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Config/EconomyConfigTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "Builder'lari yeniden kos (UiResultsPrefabs + UiCostumePrefabs) ve ResultsPanel / CostumeScreen prefab'larini + sahneleri yeniden uret: nav etiketleri 38pt sabit tek satir, kostum kategori etiketleri 30pt sabit, sag pay 0.08. EconomyConfig.asset'te eski 'dayBoundary: 0' satiri kalabilir (Unity yok sayar)."
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
1) **dayBoundary**: `EconomyConfig.dayBoundary` (`DayBoundaryPolicy`) hicbir uretim kodunda okunmuyor; gun siniri her yerde UTC
   (`RewardDay.Key`, liderlik; supervizor karari). Tohum ise `LocalMidnight` yaziyor -> config ile davranis celisiyor (iki kaynak = hata,
   CALISMA-KURALLARI §6.1). Alan, enum ve tohum satiri kaldirilir; `EconomyConfigTests`'teki ilgili test silinir/guncellenir. Asset'te kalan
   eski satiri Unity yok sayar (asset'e dokunma). Baska okuyucu yok kaniti: grep (calisma notuna yaz).
2) **Watch Replay**: sonuc ekraninda etiket iki satira kiriliyor (`pipeline-logs/shots-int7b-2026-10-30_125137/Race_results.png`).
   Butun sol menu etiketleri tek satir ve **ayni punto** kalsin (ya hepsi biraz kuculur ya da etiket alani genisler); "Race Again" vb.
   gorunumu bozulmasin.
3) **Leggings**: kostum kategori sutununda "Leggings" yazisi butonun sag kenarina dayaniyor (`.../Home_costume.png`). Kategori
   etiketleri ayni puntoda, kenardan pay birakarak sigsin.
Bitenleri `_backlog.md`'den sil (ayrica "Renk ornek kutusu lacivert" maddesi T-041'de cozuldu, onu da sil).

Builder degisikligi; prefab/sahne uretimi supervizorde (`needs:` SCENE). Kod kalici UI kurmaz.

## Kabul kriterleri
- [ ] `dayBoundary`/`DayBoundaryPolicy` projede referanssiz (grep), derleme + testler yesil
- [ ] `Race_results` ve `Home_costume` goruntulerinde etiketler tek satir, kenara dayanmiyor
- [ ] needs: SCENE yazili

## Geri bildirim
GORSEL  : yok (yerlesim)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
1. dayBoundary: grep (tum Assets, Physical/Runner dahil) `dayBoundary|DayBoundaryPolicy|LocalMidnight` yalniz EconomyConfig.cs, ConfigAssetSeeder, EconomyConfigTests ve EconomyConfig.asset'te cikti; ucu de kaldirildi (asset'e dokunulmadi).
2. Test `CapGiftAndStartingBalances_ComeFromTheAsset` olarak adlandirildi, dayBoundary assert'i silindi.
3. Watch Replay: UiBuildKit.Label best fit + Wrap yuzunden kiriliyordu; Kit'e dokunmadan nav etiketi sabit 38pt, best fit kapali, Overflow (tum nav etiketleri ayni punto).
4. Kostum kategori etiketi: sabit 30pt, best fit kapali, Overflow, sag sinir 0.96 -> 0.92.
5. Prefab uretimi supervizorde (needs SCENE); gorsel dogrulama yeniden uretimden sonra. Backlog'dan 2 madde silindi.
