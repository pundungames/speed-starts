---
id: T-056
title: Android build duman testi - batchmode'da gelistirme APK'si, Android'e ozgu kodun derlenmesi, manifest izin kontrolu
status: todo
owner: systems-agent
depends_on: [T-052]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Build/**"
  - "scripts/android-build.ps1"
touches_scene: false
branch: agent/T-056-android-build
commits: []
tests:
  - "gerekmiyor: build araci; dogrulamasi supervizorun kopyada kosacagi build'in kendisi"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
YOL-HARITASI Faz 8 "APK oncesi tam test turu". Editor dogrulamasi `#if UNITY_ANDROID && !UNITY_EDITOR` kodunu (T-052 `AndroidVibrator`)
hic derlemiyor; Android player derlemesi, sahne listesi ve manifest izni hic denenmedi. Kullanici projeyi Android'e cevirdi; Android
build destegi + NDK kurulu (`C:\Program Files\Unity\Hub\Editor\2022.3.62f2-x86_64\Editor\Data\PlaybackEngines\AndroidPlayer`).

1) `Tools/Editor/Build/AndroidBuildSmoke.cs` (`SpeedStars.Tools.Build`, Editor-only): `-executeMethod SpeedStars.Tools.Build.AndroidBuildSmoke.Run`
   - Sahneler `EditorBuildSettings.scenes`'ten (etkin olanlar); bos ise hata.
   - `BuildPlayerOptions`: Android, `BuildOptions.Development`, cikti `-buildOut <yol>` argumanindan (yoksa proje kokune gore
     `../pipeline-logs/android/SpeedStars-dev.apk`; Assets disinda). **ProjectSettings/PlayerSettings'e kalici yazma yok** (scripting
     backend, mimari, paket adi neyse o). Build target Android degilse `EditorUserBuildSettings.SwitchActiveBuildTarget` (yalniz o
     surecin Library'si).
   - Sonuc: `BuildReport` ozeti tek satir `ANDROID_BUILD_OK size=<MB> time=<sn>` ya da `ANDROID_BUILD_FAIL <ozet>` + ilk 20 hata mesaji;
     cikis kodu `EditorApplication.Exit(0/1)`.
   - Build sonrasi birlesmis manifest'i bul (Unity 2022.3: `Library/Bee/Android/Prj/**/AndroidManifest.xml` altinda, launcher/unityLibrary;
     yolu kodda arayarak bul) ve `android.permission.VIBRATE` var mi diye `ANDROID_MANIFEST vibrate=<true/false>` yaz. Ayrica
     `ANDROID_SETTINGS backend=<Mono/IL2CPP> arch=<...> minSdk=<..> target=<..> package=<..>` satiri (yalniz okuma) — magazaya cikis
     icin gereken (64-bit/IL2CPP, hedef API) farklarini supervizor rapora yazacak.
2) `scripts/android-build.ps1`: kopya klasorunu parametre alir, Unity'yi batchmode `-buildTarget Android -executeMethod ...` ile calistirir
   (`-quit` yok; metot cikisi kendisi yapar), zaman asimi 40 dk, log'dan yukaridaki satirlari ve `error CS` satirlarini basar. Var olan
   `scripts/kopya.ps1` / `unity-verify.ps1` desenine uy (Unity yolu, log yeri `pipeline-logs/`).
Unity calistirma, build alma YOK (supervizor kopyada calistirir).

## Kabul kriterleri
- [ ] Betik + metot yazildi; ProjectSettings'e yazmadigi kodda acik (yalniz okuma)
- [ ] Supervizor kopyada calistirinca ANDROID_BUILD_OK/FAIL, ANDROID_MANIFEST ve ANDROID_SETTINGS satirlari cikar

## Geri bildirim
GORSEL  : yok
SES     : yok
DOKUNSAL: yok
ZAMAN   : yok
KAMERA  : yok

## Calisma notu
