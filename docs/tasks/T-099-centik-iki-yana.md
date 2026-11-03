---
id: T-099
title: Guvenli alan iki yana - centik hangi kenardaysa iki kenar da ayni miktar iceri (SafeAreaMath + SafeAreaFitter)
status: merged
owner: ui-agent
depends_on: [T-051]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/SafeAreaMath.cs"
  - "Assets/Speed-Staes/Scripts/UI/Framework/SafeAreaFitter.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Framework/SafeAreaTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/SafeAreaPlayTests.cs"
touches_scene: false
branch: agent/T-099-centik-iki-yana
commits: [9b58f63b]
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Framework/SafeAreaTests.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/SafeAreaPlayTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-03
finished: 2026-11-03
blocked_reason:
---

## Hedef
Kullanici (03.11, Device Simulator ekran goruntusu: yatay telefonda centik sagda, ana menu karakter karti centigin altinda):
"simulasyonda telefonlarin centikleri geliyor o yuzden UI'lari sagdan ve soldan iceri almamiz lazim".
Bugun T-051'in `SafeAreaFitter`'i (Home/Race'teki ekran orneklerinde ekli, Children/Self modlari) yalniz centigin oldugu kenari
iceri alir (Android kamera deligi tek kenarda; `SafeAreaMath.SafeAnchors` sol ve sag inset'i ayri hesaplar). Istenen: **iki yatay
kenar da ayni miktar iceri** - inset = max(sol, sag), iki yana. Boylece telefon ters cevrilince (LandscapeLeft/Right) yerlesim
ziplamaz, iki yan dengeli kalir.
1) `SafeAreaMath`: yatay inset'leri aynalayan secenek (ornek `mirrorSides`); tolerans ve "guvenli alan ekrana sigmiyorsa inset yok"
   kurali aynen kalir; dikey eksen bu secenekten etkilenmez.
2) `SafeAreaFitter`: `[SerializeField, Tooltip(...)] private bool mirrorSides = true;` - sahnelerdeki mevcut bilesenlerde alan
   kayitli degil, kod varsayilani (true) gecerli olur; sahne/prefab degismez. `Configure(...)` seam'i de secenegi alabilir.
3) Testler: EditMode'da centik yalniz sagda / yalniz solda / iki yanda farkli / yok, ayna acik ve kapali; dikey bayrakla birlikte.
   PlayMode `SafeAreaPlayTests`'te sahte centik (ornek sol 120 px) artik sag kenardaki ogeleri de 120 px iceri almali; tam ekran
   arka plan/dim yine kenardan kenara.
Not: fitter yalniz Play'de calisir (Edit modda simulatorde tasarim yerlesimi gorunur - kullanicinin ekran goruntusu Edit modda).
Edit modda onizleme bu task'ta YOK: kullanicinin elle duzenledigi sahne/prefab'a anchor yazma riski var.

## Kabul kriterleri
- [ ] Tek kenarda centik (ornek sag 100 px): iki kenardaki ogeler de 100 px'lik orana gore iceri; centiksiz cihazda yerlesim bugunkuyle ayni
- [ ] `mirrorSides` kapaliyken davranis bugunkuyle birebir (eski testler gecer)
- [ ] EditMode + PlayMode testleri guncel; sabit denge sayisi yok
- [ ] Sahne/prefab dosyasi degismez (varsayilan koddan)

## Kapsam disi
- Edit modda onizleme, dikey kenarlar, yeni yerlesim; TMP/font isi (T-100, T-101)

## Calisma notu
- SafeAreaMath.SafeAnchors: yeni opsiyonel `mirrorSides` (varsayilan false); yatay inset = max(sol, sag), tolerans ve "ekrana sigmiyor" kurali ayni, dikey etkilenmez.
- SafeAreaFitter: `mirrorSides` SerializeField (varsayilan true, Tooltip'li); `Configure(mode, vertical, mirror = true)`. Sahne/prefab degismedi.
- EditMode: 7 Mirror_* + MirrorOff_IsTheOldBehaviour + 2 Fitter_Mirror* test; eski testler degismedi (acik/kapali ayni sonuc: sol centik).
- PlayMode: Home_LeftNotch_AlsoPullsRightEdgeItemsIn (ayna dikdortgene gore); eski testler ayni kaldi.
- Calistirilmadi (merkezi dogrulama). Risk: sahnelerdeki tum fitter'lar artik aynali; Race'te sag kenar ogeleri sola centikte ice kayar (istenen).
- Supervizor 03.11: kopyada derleme + EditMode 1198/1198, PlayMode 52 gecti / 7 atlandi (Home_LeftNotch_AlsoPullsRightEdgeItemsIn dahil).
