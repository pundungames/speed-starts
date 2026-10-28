---
id: T-006
title: Home/Race sahneleri, config asset ornekleri (PDF degerleri), ProjectContext prefab'i
status: todo
owner: scene-integrator
depends_on: [T-004, T-005, T-008]
owns_paths:
  - "Assets/Speed-Staes/Scenes/Menu/**"
  - "Assets/Speed-Staes/Scenes/Game/Race.unity"
  - "Assets/Speed-Staes/Data/Config/**"
  - "Assets/Resources/ProjectContext.prefab"
  - "ProjectSettings/EditorBuildSettings.asset"
touches_scene: true
branch: agent/faz1-iskele
commits: []
tests:
  - "gerekmiyor: sahne/asset; kabul gozle + kopyada derleme"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Sözleşme §5-§6: `Scenes/Menu/Home.unity` ve `Scenes/Game/Race.unity` (Malik'in koşucu prefab'ı örneği + `Race`/`RaceController`,
FinishLine, Track, Main Camera — PhysicalTest'teki adlarla; prefab'a dokunulmaz, sahnedeki örnekte debug HUD ve RunnerReplay kapatılır),
her sahnede `SceneContext` + EventSystem; `Assets/Resources/ProjectContext.prefab` (ProjectInstaller); Build Settings (Home 0, Race 1).
Config asset'leri `Data/Config/`: EconomyConfig, RaceEventCatalog + 10 etkinlik (PDF/MD §12 adları), GemRewardConfig, SevenDayRewardConfig,
CosmeticCatalog (prefab'taki 8 yuva × 7+2 set), UiMotionConfig. **Değerler PDF §1.1-1.3 ve §2.4'ten, Inspector/MCP ile asset'e girilir**
(koda yazılmaz). PDF'te olmayan değer boş/kapalı kalır (6. gün, bilet fiyatları, normal kozmetik fiyatı); `requiresTicket` OPEN #3 varsayımı.

## Kabul kriterleri
- [ ] Home'dan Play: ProjectContext kurulur, konsol temiz
- [ ] Race'ten Play: Malik'in koşucusu çalışır (dokunulmamış prefab), HUD/Replay debug kapalı
- [ ] Asset'lerde kodda sayı yok; PDF değerleri asset'te
- [ ] Malik'in prefab/sahne dosyalarında değişiklik yok (`git diff --name-only`)

## Çalışma notu
