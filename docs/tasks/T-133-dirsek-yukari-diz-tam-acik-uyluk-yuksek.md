---
id: T-133
title: Omuz onde dirsek yukari; uyluk tepeye varirken alt bacak tam acik; uyluk daha yukari
status: review
owner: gameplay-agent
depends_on: [T-132]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [92099e49, 6d74bcea, 3b283905]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11: "omuz en one gelirken dirsekten de yukari dogru gelmeli, sinirin cok az, acisi daha fazla olmali. bacak belden aciyi tamamlamaya yakinken dizden de alt bacak full acik hale gelmeli. ust bacak belden one uste gelme acisi daha fazla olsun."

## Varsayimlar (kullanici sayi vermedi)
- Dirsek onde 105 -> 130, yavas kosuda da yakin (`armElbowMinShare` 0.6 -> 0.85); kapanma omuz one giderken surer.
- Alt bacak tepe aninda 170 (tam duz degil, IK kirilmasin).
- Uyluk anahtari +15: yavas 68 -> 85, hizli 78 -> 92.

## Isler (uc commit)
1. Kosu kolu.
2. Diz uyluk tepesinde acik: gecikme yalniz kiliftan tepeye; duz kosuda tepe daha erken, inise zaman kalir.
3. Uyluk daha yukari.

## Kapsam disi
- Tek ayak ve cift ziplama sayilari, tutma, dalis, ayak kurali. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
1. Kosu kolu: armElbowFront 35, armElbowMinShare 0.85, yeni armElbowCloseAt 0.8 (92099e49).
2. Diz gecikmesi kilif->uyluk tepesi, kneeOpen/Fast diz 170, yeni runSwingKeys (0.22,0.45,0.68), Recover/Mid knots[2]'ye bagli (6d74bcea).
3. Uyluk 85 / 92 (3b283905).
Calistirilmadi, derlenmedi; swingKeys'in baska okuyucusu yok. Gorsel dogrulama kullanicida.
