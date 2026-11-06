---
id: T-162
title: Eller yine yumruk: Serhat'in FBX'i parmaklari acti, prefab'a geri yazilir
status: todo
owner: scene-integrator
depends_on: [T-161]
owns_paths:
  - "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab"
  - "docs/tasks/T-162-yumruk.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "gerekmiyor: gorsel; kullanici gozle bakar"
analytics:
  - "yok: projede analytics yok"
needs: []
started:
finished:
blocked_reason:
---

## Hedef
Kullanici 06.11: "adamin ellerine naptin lan" (kosarken eller acik parmakli). `t158-c` kaydinda yumruk, `t161-a` ve sonrasinda stance'siz kosuda bile acik. Aradaki fark: `origin/main`'den gelen `2ba66e27` (Serhat, 05.11) `Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx`'i degistirdi (6039020 -> 6165420 bayt). Parmak kemiklerini kimse her kare yazmiyor, durus FBX'ten geliyor.

## Istenen
1. Eski FBX (`git show 2516ccff:"Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx"`) ile yenisinin butun kemiklerinin yerel donus ve pozisyonlarini karsilastir, farklari log'a yaz. Bu teshisi dogrular ya da curutur.
2. Parmak kemikleri (`B-hand`'in altindakiler) eskiden farkliysa eski donusleri `HumanCharacterDummy_F.prefab`'a override olarak yaz. Baska kemige dokunma; onlarda fark varsa yalniz raporla.
3. FBX'e, `.fbx.meta`'ya, `RunnerAccessoryMeshes.asset`'e dokunma (Serhat'in isi kalir).

Teshis curursa (parmaklar iki FBX'te ayni) prefab'a yazma, dur ve raporla.

## Calisma notu
