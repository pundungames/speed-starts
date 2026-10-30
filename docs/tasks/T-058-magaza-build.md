---
id: T-058
title: Magaza build metodu - yalniz o build icin IL2CPP + ARMv7/ARM64 + paket adi + surum, AAB; imza anahtari ortam degiskeninden
status: todo
owner: systems-agent
depends_on: [T-056]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Packaging/**"
  - "scripts/android-release.ps1"
  - "docs/TUZAKLAR.md"
touches_scene: false
branch: agent/T-058-magaza-build
commits: []
tests:
  - "gerekmiyor: build araci; dogrulamasi supervizorun kopyada kosacagi build"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Supervizor karari (30.10): gunluk/test build'i Mono + ARMv7 kalir (1 dk); magaza build'i IL2CPP + ARMv7/ARM64 (kopyada denendi:
9.5 dk, 71 MB APK, `Assets/Speed-Staes/link.xml` Zenject'i korur). Proje ayarlari (ProjectSettings.asset) Malik/Serhat ile ortak ve
kullanicinin Unity'si acik: **kalici ayar degisikligi YOK**; magaza ayarlari yalniz build suresince uygulanir, sonra geri alinir.

1) `Tools/Editor/Packaging/ReleaseBuildConfig.cs` (ScriptableObject, editor asset'i; asset yolu `Assets/Speed-Staes/Data/Build/
   ReleaseBuildConfig.asset`, olusturulmasi supervizorde): `packageId` (bos = projenin bugunku kimligi; kullanici karari bekleniyor),
   `versionName`, `versionCode`, `architectures` (ARMv7|ARM64), `il2cpp` (true), `buildAppBundle` (true = .aab), `targetSdk` (0 = en
   yuksek kurulu), `keystorePath` + `keyAlias` (bos olabilir). **Parolalar asla asset'te degil**: ortam degiskenleri
   `SPEEDSTARS_KEYSTORE_PASS`, `SPEEDSTARS_KEY_PASS`. Her alanin Tooltip'i.
2) `Tools/Editor/Packaging/ReleaseBuild.cs`: `-executeMethod SpeedStars.Tools.Build.ReleaseBuild.Run` (`-buildOut <yol>`, `-config <asset yolu>`
   istege bagli). Once butun ilgili PlayerSettings/EditorUserBuildSettings degerlerini kaydet, config'i uygula, build (Development KAPALI),
   `finally` icinde hepsini geri yaz. Keystore yoksa ya da parola ortamda yoksa: debug imzasiyla devam + `ANDROID_RELEASE_UNSIGNED` uyari
   satiri (magazaya yuklenemez). Cikti satirlari T-056 deseninde: `ANDROID_RELEASE_SETTINGS ...`, `ANDROID_RELEASE_OK size=.. time=..` /
   `ANDROID_RELEASE_FAIL`, `ANDROID_MANIFEST vibrate=..` (T-056'daki manifest aramasini paylas, kopyalama). `EditorApplication.Exit`.
   T-056'nin `AndroidBuildSmoke` ortak parcalarini (sahne listesi, arg okuma, rapor, manifest) bir yardimci sinifa al; smoke davranisi degismez.
3) `scripts/android-release.ps1`: `scripts/android-build.ps1` deseniyle (zaman asimi 60 dk; `$null = $p.Handle` ile cikis kodu).
Unity/build calistirma YOK (supervizor kopyada calistirir).

## Kabul kriterleri
- [ ] Kodda kalici ayar yazimi yok: her degistirilen ayar `finally`'de geri yaziliyor (calisma notunda liste)
- [ ] Parola asset'te/kodda/log'da yok
- [ ] needs: ASSET (ReleaseBuildConfig.asset tohumu: packageId bos, versionName 1.0, versionCode 1, ARMv7+ARM64, il2cpp, aab) yazili

## Geri bildirim
GORSEL  : yok
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
