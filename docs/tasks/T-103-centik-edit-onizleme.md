---
id: T-103
title: Centik korumasi Edit modda da gorunsun - SafeAreaFitter onizlemesi (surulen anchor, sahneye/prefab'a YAZILMAZ)
status: todo
owner: ui-agent
depends_on: [T-099]
owns_paths:
  - "Assets/Speed-Staes/Scripts/UI/Framework/SafeAreaFitter.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Framework/SafeAreaEditPreviewTests.cs"
touches_scene: false
branch: agent/T-103-centik-onizleme
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Framework/SafeAreaEditPreviewTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici (03.11, Device Simulator ekran goruntusu, Edit mod: centik sagda, kart altinda): "hani sagdan soldan UI'i ice alacaktik".
T-099'un aynali `SafeAreaFitter`'i yalniz Play'de calisiyor (MonoBehaviour OnEnable/Update); Edit modda simulator tasarim yerlesimini
gosteriyor. Kullanici UI'i Edit modda simulatorde ELLE duzenliyor: centikli cihaz secince yerlesim Edit modda da iceri gorunmeli.
**Sart: onizleme hicbir zaman kaydedilmez** (sahne/prefab ornegine override olarak dusmez; aksi halde Play'de iki kez iceri girer).
1) `[ExecuteAlways]`; Edit modda (`!Application.isPlaying`) ayni hesap, ama anchor'lar `DrivenRectTransformTracker` ile SURULUR
   (AnchorMin/AnchorMax): Unity surulen degeri sahne/prefab kaydinda yazmaz ve override saymaz (dogrula: test 3). `OnDisable`'da tracker
   temizlenir ve tasarim anchor'lari geri yazilir. Tasarim anchor'lari surulmeyen durumda toplanir (etkinlesmede, tracker bosken).
2) Simulatorde cihaz/yon degisince Edit modda Update gelmeyebilir: `#if UNITY_EDITOR` `EditorApplication.update` ile iki onbellekli degeri
   yokla (yalniz Edit modda, OnDisable'da birak). Play davranisi T-099'daki gibi aynen.
3) EditMode testleri: (a) Edit modda sahte centikle anchor'lar iceri; (b) devre disi birakinca tasarim anchor'lari geri; (c) **kayit testi**:
   fitter'li gecici bir nesneyi onizleme acikken gecici klasore prefab olarak kaydet (ya da sahne), dosyadaki/geri yuklenen anchor'lar
   TASARIM degerleri; test kendi gecici dosyasini siler. (d) centik yokken hicbir sey surulmez.
Kullanici Edit modda en uc dugumlerin anchor'unu onizleme acikken degistiremez (surulen alan gri) - Tooltip ve `<summary>`'de yaz.
Prefab/sahne/asset dosyasina YAZMA (testin gecici dosyasi haric); Unity/test CALISTIRMA.

## Kabul kriterleri
- [ ] Simulatorde centikli cihazda Edit modda kenar ogeleri iceri gorunur; cihaz degisince guncellenir
- [ ] Sahne/prefab kaydi onizlemeyi icermez (test c); Play'de cift inset yok
- [ ] Play davranisi T-099 ile ayni; mevcut SafeArea testleri yesil

## Kapsam disi
- Prefab modunda (prefab stage) onizleme; dikey kenarlar

## Calisma notu
