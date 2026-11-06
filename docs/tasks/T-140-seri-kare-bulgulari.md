---
id: T-140
title: Seri kare bulgulari - uzuv hedefini gecmesin, ayak isinlanmasin, kisa basis tutma olmasin, gecisler yumusak, gövde titremesin
status: review
owner: gameplay-agent
depends_on: [T-139]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [443b4183, 5d19ddf4, 7474a1b0, 5f0ebc11, 6f6015d6]
tests:
  - "olcum: RunnerTransitions_WriteFrameStrips ve transition_check.py, once t139-before; sonrasi supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11: "tum gecislerde problemler var, hic akici degil, tekleyerek geciyor, karakter titriyor. onerilerini yap, seri kare al ve incele."

## Olculen (D:\Pundun\form-probe\t139-before, 60 fps, her kare)
| Bulgu | Olcu | Kaynak |
|---|---|---|
| Uzuv hedefini geciyor | Uyluk anahtari 64-70, gercek 98-115; diz 177 | `Track.Step` ivme siniri frenlemeyi de kisiyor: 24 deg/kare hizdan durmak ~54 deg |
| Ayak isinlaniyor | Basista 46-81 cm/kare | `AdvanceStance` needLock, civili ayagin yerini degil FK yerini aliyor |
| Kisa basis tutma | 0.18 sn tusta 16 tutma, hepsi < 0.2 sn; oburu bacak 17 kez Reach'e atildi | `TickLegs`: `PushTime()` (0.1-0.2 sn) |
| Tek ayak acilisi | Kalca 10-11 deg/kare, kalca yuksekligi 10 cm/kare | Agirlik Lerp'i ilk karede en hizli (0.26) |
| Tutma girisi ve cikisi | Bas 31 deg/kare, kalca 14 deg/kare | `_bothHeld` ve `_holdHead` yaysiz |
| Govde titremesi | Hizli kosuda kalca 8, gogus 14 deg, 5 Hz | `runRock` 16 her basista, sonum 0.5 |

## Isler (bes commit)
1. `Track`: ivme siniri yalniz hizlanmada; freni yay yapar.
2. Civili ayak basilinca oldugu yerde kalir.
3. Tutma yalniz `holdAfter` 0.22 sn sonra.
4. `_hop`, `_pair`, tutma egimi ikinci derece yayla; ilk kare kick yok.
5. Basis sallanmasi hizli ritimde kucuk; basista yumusak baslar; dikey hiz filtreli.

## Kapsam disi
- Sayilarin kendisi (anahtarlar), yukseklik yaylari (olcumden sonra karar). `RunnerSim.cs`, prefab, sahne.

## Calisma notu
5 commit, her biri tek basina geri alinabilir; hicbiri calistirilmadi, olcum supervizorde.
Track.Step: fren serbest (a*v<0), yalniz hizlanma sinirli; alt adimlar (w*h<=0.5) kararli kalir.
Civi: pinned ise pinPoint'in yerel z'si (1-_dive ile karisik), eritirken ankle+sitShift+pinOffset, yoksa eski.
holdAfter 0.22; RunnerSim ayri pushTime/minStepTime kullanir, dokunulmadi. HopBlend kaldirildi.
rockKick en hizli ritimde (donem 0.1) dususte 16.7/sn'lik MoveTowards'i hafif geride kalabilir.
