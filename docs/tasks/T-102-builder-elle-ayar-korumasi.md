---
id: T-102
title: Builder'lar elle duzenlenmis prefab/sahneyi asla ezmez - butun kaydetmeler tek korumali yoldan, yamalar yalniz eksigi ekler, cikti TMP
status: todo
owner: ui-agent
depends_on: [T-100]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/**"
  - "Assets/Speed-Staes/Tools/Editor/Migration/TmpMigration.cs"
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/BuilderGuardTests.cs"
touches_scene: false
branch: agent/T-102-builder-korumasi
commits: []
tests:
  - "Assets/Speed-Staes/Tests/EditMode/UI/Builders/BuilderGuardTests.cs"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: open
    not: "Supervizor kopyada butun builder giris noktalarini (EconomyUiRebuild.Run, Faz1 overwrite, patch menuleri) kosar; sonra UI prefab'lari + Home.unity + Race.unity icin git diff BOS olmali."
started:
finished:
blocked_reason:
---

## Hedef
Kullanici (03.11): "builder'i ona gore duzenle, prefab ayarlari gitmesin". Kullanici UI prefab'larini ve sahneleri ELLE duzenliyor
(4f38e016); T-100 `UiPrefabBuilder.BuildAll` ve `SceneBuilder.BuildHome`'a koruma koydu (var olani ezmez, `-forceOverwriteHandEdits`
haric; yeni uretileni `TmpMigration.ConvertPaths` ile TMP'ye cevirir). Ama korumayi atlayan yollar var (supervizor 03.11 taramasi):
- `UiRaceHudPrefabs.PatchRaceHudPrefab`, `UiSettingsPrefabs` (Settings v2 bolumleri `AddExtraSections`, `AddCameraRowToPrefab`):
  var olan prefab'i acip KENDI bolumunu silip yeniden kuruyor (o bolumdeki elle ayar gider, yeniden kurulan yazilar legacy Text olur ->
  view alanlari `TMP_Text`, baglanti kopar).
- `SceneBuilder.BuildRace` (Malik'in kayitli binalari + kullanici ayarlari; TUZAKLAR), Race yamalari (`SceneCosmetics`,
  `ScenePracticePanel`, `SceneRaceEmotes`, varsa `SceneRaceBots`/`SceneRaceHurdles`): sahneyi kaydediyor; sil-yeniden-kur yapiyorlarsa ayni sorun.
- `UiPracticePrefabs.BuildPrefab` var olani koruyor ama yeni uretirse TMP'ye cevirmiyor.
Istenen:
1) Builders altindaki BUTUN prefab ve sahne yazimlari tek bir korumali yardimcidan gecer (ornek `HandEditGuard`): var olan dosyayi
   butunuyle yeniden uretmek yalniz zorlama bayragiyla; yama (var olan prefab/sahneye eksik parcayi eklemek) "yoksa ekle, varsa DOKUNMA"
   (sil-yeniden-kur yok; mevcut dugum bulunursa log + atla). Her kayittan sonra o dosyaya `TmpMigration.ConvertPaths` (legacy Text kalmaz).
2) `BuildRace` de BuildHome gibi korunur. Menu basliklarindaki "(overwrite)" artik dogru degilse duzelt (ornek "(keeps existing)").
3) `ProjectContextBuilder` (kompozisyon koku prefab'i, config referanslari; elle duzenlenmiyor) istisna olabilir: gerekcesini koda yaz.
4) Regresyon testi (EditMode): Builders altindaki .cs dosyalarini metin olarak tarar; `PrefabUtility.SaveAsPrefabAsset` /
   `EditorSceneManager.SaveScene` cagrisi yalniz korumali yardimcida (ve istisnada) bulunabilir. Korumanin karar mantigi (var/yok,
   bayrak) saf fonksiyonsa onu da test et.
Davranis degismez: eksik prefab/sahne yine uretilir; var olanlar oldugu gibi kalir. Prefab/sahne/asset dosyasina YAZMA, Unity/builder/test
CALISTIRMA (supervizor kopyada butun builder'lari kosup diff'in bos oldugunu kontrol eder).

## Kabul kriterleri
- [ ] Builders altinda korumasiz SaveAsPrefabAsset / SaveScene kalmaz (test bunu yakalar)
- [ ] Yama metodlari var olan bolumu silmez; ikinci kosu hicbir sey degistirmez
- [ ] Yeni uretilen her prefab/sahne TMP'ye cevrilir
- [ ] Kopyada butun builder giris noktalari kosunca UI prefab'lari, Home ve Race'te diff bos

## Kapsam disi
- Builder uretim mantiginin TMP'ye cevrilmesi (cikti donusturucuyle TMP), yeni UI isi

## Calisma notu
