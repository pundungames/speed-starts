---
id: T-162
title: Eller yine yumruk: Serhat'in FBX'i parmaklari acti, prefab'a geri yazilir
status: cancelled
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
- Eski (2516ccff) ve yeni FBX kopyada ayni importer ayariyla (Generic, NoAvatar) karsilastirildi: 133 ortak kemik, parmak farki 0, diger fark 0 (>0.5 derece / >1 mm); eskide 1 fazla kemik.
- Prefab'a yazilmadi. Fark duruş eğrisinde (FBX klibi/bind) ya da baska yerde olabilir; yerel donus farki degil.
- Gecici dosyalar yalniz kopyada kaldi; repo degismedi.
- Iptal: sebep FBX degil. `13d3ef87` kaydinda (Serhat'in FBX'i dahil) yumruk var. Eller T-161 katmanindan acildi; parmaklar katmandan cikinca (58a8d9fd, t161-c) kosu, sekis, blok cikisi yumruk.
