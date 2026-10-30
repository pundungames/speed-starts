---
id: T-045
title: Home kopyasinda Serhat'in bina ureticisi (PastelBuildingRow) sahne her acilista binalari yeniden uretiyor - kopyadan cikar
status: merged
owner: gameplay-agent
depends_on: [T-023]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneHomeTheme.cs"
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/SceneBuilder.cs"
touches_scene: true
branch: agent/T-045-bina-churn
commits: [8f7d8a9]
tests:
  - "gerekmiyor: builder sahne ayari; kabul = Home'u acip kaydedince bina nesneleri degismiyor (iki build arasi Home.unity'de bina diff'i yok)"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-30
finished: 2026-10-30
blocked_reason:
---

## Hedef
`Scripts/Runner/PastelBuildingRow.cs` (Serhat'ın; `[ExecuteAlways]`, `OnValidate` → `EditorApplication.delayCall` → `Rebuild()`) sahne
Editor'de her yüklendiğinde "Generated Buildings" altındaki binaları yeni fileID'lerle yeniden kuruyor. Home, Serhat'ın sahnesinin kopyası
olduğu için kullanıcının Unity'si Home'u her açıp kaydettiğinde 142 nesnelik anlamsız diff oluşuyor ve ana klasörde commit'siz değişiklik
birikip birleştirmeleri durduruyor (30.10'da iki kez). Çözüm (yalnız bizim kopyada): Home builder'ı (`SceneHomeTheme.Prepare` ya da Home
kurulumunun uygun adımı) kopyalanan sahnedeki bütün `PastelBuildingRow` bileşenlerini **kaldırır** (prefab örneğinde "removed component"
override'ı; üretilmiş binalar sahnede kalır). Serhat'ın sahnesine, prefab'ına ve script'ine dokunulmaz. Aynı desende başka ExecuteAlways
üretici varsa (LoopWayGenerator/ThemedLoopWay `autoPreview`) aynı kopyada kapat (override) ve çalışma notuna yaz.

## Kabul kriterleri
- [ ] Home iki kez üst üste build edilince Home.unity'de bina nesneleri aynı kalır (kopyada doğrulanır)
- [ ] Oyunda (Play) şehir binaları görünür (ekran görüntüsü)

## Calisma notu
- SceneHomeTheme.Prepare now calls RemoveBuildingGenerators: Undo.DestroyObjectImmediate on every PastelBuildingRow (removed-component override on prefab instances; DestroyImmediate on instance components can error, Undo path is the supported one). Buildings stay.
- LoopWayGenerator/ThemedLoopWay autoPreview is only read from custom inspectors and undo recovery (not on load), so left alone. PastelPlantGenerator rebuilds are hash-guarded, left alone.
- Not run in Unity: unverified; check that the removal logs a count and a second build leaves buildings unchanged.
- Home.unity not regenerated here (builder output comes with the next build).
- Open: none.
