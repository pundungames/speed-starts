---
id: T-070
title: Fiyati 0 olan tema bedava (herkes kullanir) - Sky Islands premium ama simdilik ucretsiz; fiyat girilince satin alinabilir
status: review
owner: systems-agent
depends_on: [T-068]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Meta/Themes/**"
  - "Assets/Speed-Staes/Scripts/Meta/Contracts/ThemeContracts.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/ThemeCatalogSeed.cs"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/**"
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Regression/**"
touches_scene: false
branch: agent/T-070-fiyatsiz-tema
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/Meta/Themes/ThemeServiceTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
Kullanici (30.10): Sky Islands fiyati icin "fiyati sen belirle" sonra "beles olsun simdilik". Bugunku kural (T-023/T-068): katalogda fiyat 0 =
"satilik degil" (NotForSale), tema yalniz editor/dev'de sahipli (T-023 bayragi); magaza build'inde (T-058, Development kapali) bu yuzden
varsayilan disindaki butun temalar ve premium Sky Islands kilitli kalir.

Yeni kural (supervizor karari): **fiyati 0 olan tema bedavadir** — sahipli sayilir, her build'de kullanilir, satin alma gerekmez, para dusmez.
Fiyat > 0 girilince satin alinir (bugunku yol: para birimi, idempotent islem kimligi). Premium gorunum (altin/kirmizi) fiyattan bagimsiz kalir
(`premium` bayragi). Editor/dev "hepsi sahipli" bayragi aynen kalir.
- `ThemeService` sahiplik/eylem karari (IsOwned / action rules): fiyat 0 -> sahipli; `TryPurchase` fiyat 0'da para dusmeden basarili ya da
  "zaten sahipli" (hangisi mevcut sozlesmeye uygunsa; UI "USE" gostermeli, satin alma butonu degil).
- Testler: T-068'in "fiyatsiz premium = satin alinamaz" testi bu kurala gore guncellenir (fiyat 0 -> sahipli/kullanilir); fiyat > 0 yollari
  ayni; RegressionHarness/ThemeService testleri yesil kalir. Sabit fiyat assert edilmez.
- `ThemeCatalogSeed` yorum/log metni yeni kurala gore (tohum fiyatlari 0 kalir).
Unity calistirma yok; asset degisikligi gerekmez (fiyatlar zaten 0).

## Kabul kriterleri
- [ ] EditMode: release kosulunda (dev bayragi kapali) fiyat 0 tema sahipli ve uygulanabilir; fiyat > 0 tema bakiyeyle satin alinir, idempotent
- [ ] Var olan tema testleri yesil (guncellenen test aciklamasi calisma notunda)

## Geri bildirim
GORSEL  : tema ekraninda USE (satin al degil)
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
- Kural `ThemeService.IsOwned` icinde: fiyat <= 0 -> sahipli (her build); TryPurchase bu temada AlreadyOwned doner, para dusmez; ActionRules degismedi (owned once kontrol edilir -> UI "USE").
- Testler (ThemeServiceTests): ActionState "d" NotForSale -> OwnedInactive; Purchase_Failures'tan "d NotForSale" cikti; Premium_Unpriced_CannotBeBought -> Premium_Unpriced_IsFree_NothingCharged; yeni FreeTheme_IsOwnedAndUsable_WithoutDevFlag_AndNeverCharges.
- Regression testleri degismedi (ucretli tema yolu ayni). ThemeCatalogSeed yorum/log metni guncellendi, fiyatlar 0.
- Unity/test kosulmadi; derleme riski dusuk (IsOwned'da `out ThemeEntry entry`).
- UI (ThemeSelectPresenter) degismedi: OwnedInactive durumunu zaten USE olarak gosteriyor.
