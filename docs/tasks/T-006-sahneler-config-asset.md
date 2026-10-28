---
id: T-006
title: Config asset'leri (PDF), ProjectContext, Home/Race sahneleri, Build Settings - editor builder'i
status: merged
owner: scene-integrator
depends_on: [T-004, T-005, T-008]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/**"
touches_scene: true
branch: agent/faz1-ui
commits: []
tests:
  - "gerekmiyor: editor builder'i; kabul supervizorun batchmode kosusu + Home/Race Play'de konsol temiz"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: SCENE
    durum: closed
    not: "Faz1Builder.BuildAll kopyada batchmode; geri alinacak ciktilar (.meta dahil): Assets/Speed-Staes/Data/Config/** (6 asset + Events/ 10 + Cosmetics/ 72), Assets/Resources/ProjectContext.prefab, Assets/Speed-Staes/Scenes/Menu/ (klasor + Home.unity), Assets/Speed-Staes/Scenes/Game/Race.unity, ProjectSettings/EditorBuildSettings.asset, Tools/Editor/Builders/Scene klasor ve .cs meta'lari -> Faz1Builder batchmode (kopya) + GeriAl, agent/faz1-ui"
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Unity MCP bu oturumda yok; asset ve sahneler **editörde çalışan builder'larla** üretilir, süpervizör doğrulama kopyasında batchmode'da
koşturur (`SpeedStars.Tools.Builders.Faz1Builder.BuildAll` — sırayı o belirler: config → UI prefab'ları (T-007) → ProjectContext → Home →
Race → Build Settings). Bu task üç builder sınıfını yazar (namespace `SpeedStars.Tools.Builders`, imzalar `Faz1Builder.cs`'teki çağrılarla aynı):

1. `ConfigAssetSeeder.SeedAll(bool overwrite)` → `Assets/Speed-Staes/Data/Config/`:
   - `EconomyConfig`, `GemRewardConfig`, `SevenDayRewardConfig`, `RaceEventCatalog` + `Events/*.asset`, `CosmeticCatalog` + `Cosmetics/*.asset`,
     `UiMotionConfig` (alan varsayılanlarıyla, T-005).
   - **Değerler PDF'ten** (`docs/gdd/Speed_Stars_V1.0_Turkce.pdf` §1.1-1.3, §2.4): seed yalnız **ilk doldurmadır**; asset varsa ve `overwrite`
     false ise dokunmaz (asset kullanıcının malı; çalışma zamanı asset'i okur). Seed'deki her sayının yanına PDF madde numarası yorum olarak yazılır.
   - PDF'te OLMAYAN değer uydurulmaz: başlangıç bakiyeleri 0, bilet fiyatları 0 (kapalı), ödüllü reklam kapalı (SDK yok), 6. gün `None`,
     normal kozmetik fiyatı 0. Etkinlikler (PDF §1.3, MD §12): Antrenman Alanı, Race of the Day, 100m, 200m, 400m, 60mH, 110mH, 400mH, 4×100m,
     Medley; mesafe adından belli olanlar dolu, belirsizler (Antrenman, Race of the Day, Medley) 0; Mor Mücevher ödülü yalnız 400m/60mH/Medley;
     `RequiresTicket` yalnız 100m (OPEN #3 dilim varsayımı); oynanabilir yalnız 100m.
   - `CosmeticCatalog`: karakter prefab'ı `Prefabs/Character/HumanCharacterDummy_F.prefab`'ı **salt okunur** yükleyip `Accessories/<Kategori>/`
     altındaki setlerden üretilir (8 kategori × 7 normal + 2 premium; adlandırma ve çok parçalı setler: `reports/kozmetik-hazirlik-2026-10-28.md`,
     `CosmeticDefinition.partNames`). Premium: Mor Mücevher 50 (PDF §1.3). 7. gün = premium Headwear'ın ilki (OPEN #6).
2. `ProjectContextBuilder.Build(bool overwrite)` → `Assets/Resources/ProjectContext.prefab`: `ProjectContext` + `ProjectInstaller`
   (T-008); ProjectContext'in installer listesine ProjectInstaller; config referansları `SerializedObject` ile atanır.
3. `SceneBuilder.BuildHome(bool)`, `BuildRace(bool)`, `SetBuildSettings()`:
   - **Home** `Assets/Speed-Staes/Scenes/Menu/Home.unity`: Main Camera (düz renk arka plan), `SceneContext` (+ `HomeSceneInstaller`,
     `rootScreen` = HomeScreen örneği, SceneContext installer listesinde), `EventSystem` (+ `InputSystemUIInputModule`), `UI Root` altında üç Canvas
     (Screen Space Overlay, `CanvasScaler` Scale With Screen Size 1920×1080, match 0.5): `Canvas Screens` (HomeScreens sırasıyla; yalnız HomeScreen aktif),
     `Canvas Popups` (HomePopups, hepsi kapalı), `Canvas Overlay` (ResourceBar aktif, RewardFlight kapalı, `TransitionBlocker` tam ekran alpha 0
     raycast Image kapalı) + UI Root'ta `SceneTransitionBlocker` (blocker alanı bağlı).
   - **Race** `Assets/Speed-Staes/Scenes/Game/Race.unity`: **`Scenes/Test/PhysicalTest.unity`'nin kopyası** (`AssetDatabase.CopyAsset`; Malik'in
     dosyası değişmez). Kopyada koşucu örneğinde `RunnerBodyHUD` ve `RunnerReplay` bileşenleri kapatılır (sahne override'ı, prefab'a dokunulmaz).
     Üstüne `SceneContext` (+ `RaceSceneInstaller`, rootScreen = RaceHud), EventSystem, UI Root: `Canvas Hud` (RaceHud aktif), `Canvas Popups`
     (ResultsScreen, EntryGateModal kapalı), Overlay'de TransitionBlocker + `SceneTransitionBlocker`.
   - Build Settings: Home (0), Race (1), ikisi etkin; başka sahne eklenmez.

## Kabul kriterleri
- [ ] Üç sınıf derlenir; `overwrite == false` iken var olan asset/prefab/sahneye dokunmaz.
- [ ] Malik'in dosyaları (`Scenes/Test/PhysicalTest.unity`, `Prefabs/Character/**`, `Scripts/Physical/**`) değişmez — yalnız okunur/kopyalanır.
- [ ] PDF değerleri yalnız seed'de ve PDF madde numarasıyla; kodun geri kalanında sayı yok.
- [ ] Bileşen alanları `SerializedObject`/`SerializedProperty` ile atanır (private `[SerializeField]` alanlar), adlar T-004/T-005/T-008 kodundan okunur.

## Kapsam dışı
- UI prefab'larının içeriği (T-007), view mantığı ve Bridge adaptörü (Faz 2), yatay ekran ayarı (T-001, ProjectSettings — kullanıcıda).

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-ui1` (worktree, dal `agent/faz1-ui`). Ana klasöre yazma.
- Önce oku: `.claude/agents/scene-integrator.md` (ana klasörde: `D:\Pundun\speed-starts\.claude\agents\scene-integrator.md`),
  `docs/CALISMA-KURALLARI.md`, `docs/TEMEL-SOZLESME.md` §4-§6, `docs/TUZAKLAR.md`, `Assets/Speed-Staes/Scripts/Meta/Config/**`,
  `Scripts/UI/Framework/**`, `Scripts/UI/Flow/**`, `Scripts/Core/Installers/**`, `Scripts/Physical/Editor/PhysicalSetup.cs` (Malik'in kurulumu,
  referans), `Tools/Editor/Builders/UiPrefabPaths.cs` + `Faz1Builder.cs` (DEĞİŞTİRME).
- Paralelde ui-agent `Tools/Editor/Builders/Ui/**`'de çalışıyor (T-007): ona yazma.
- Unity, batchmode, `unity-verify` YASAK (builder'ı süpervizör koşturur). `git commit` YAPMA. Yalnız `.cs` yaz.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- Yazılan: `Tools/Editor/Builders/Scene/` altında `ConfigAssetSeeder` (+ `CosmeticCatalogSeed`), `ProjectContextBuilder`, `SceneBuilder` (+ `SceneUiLayout`), `SceneBuilderUtil`. Unity koşulmadı; Roslyn (Unity'nin csc'si) ile verify-1 `ScriptAssemblies`'e karşı, ui-agent'ın `Ui/` dosyalarıyla birlikte derlendi: 0 hata, 0 uyarı.
- Seed (PDF madde no yorumda): Adet Bileti üst sınır 3, 30 dk, günlük 1; Süreli 60 dk; Mor Mücevher 3/2/1; 7 gün 100 / 200 / 1 SB / 200 / 1 SB / boş / `headwear.ph01`; premium 50 MM. Uydurulmayan her şey 0 / kapalı. 10 etkinlik (`Events/<id>.asset`), 72 kozmetik (8 × 7+2, id `<kategori>.<ana parça>`, çok parçalılar `partNames`'te).
- Kararlar: Serhat'ın `SetFor` kuralı kopyalandı (çağrılmadı; onun kodu değişince bizim editör assembly'si kırılmasın). Race ilk seferde `AssetDatabase.CopyAsset`, overwrite'ta dosya kopyası (Race.unity.meta GUID'i korunur). Var olan katalog, liste ve girdileriyle birlikte dokunulmaz.
- Güvenlik: sahneler Single modda tek tek açılır; batchmode dışında Play / kirli sahne / kirli prefab stage varken hata verip durur, iş bitince kullanıcının sahnelerini geri açar. UI prefab'ı ya da karakter prefab'ı eksikse hiçbir dosya yazılmadan hata; yarım kalan Race kopyası silinir. Build Settings'te Home 0, Race 1; önceden eklenmiş başka sahne varsa arkada korunur (uyarı).
- Bırakılan: koşu ve çıktıların geri alınması süpervizörde (needs SCENE). Agent hafızası yazılmadı (ana klasörde, owns_paths dışı).
