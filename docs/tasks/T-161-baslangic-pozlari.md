---
id: T-161
title: Yaris baslangic pozlari FBX'ten: Readypose READY'de, GOpose SET'te
status: todo
owner: gameplay-agent
depends_on: [T-153]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/RunnerStartPose.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceSessionAdapter.cs"
  - "Assets/Speed-Staes/Scripts/Bridge/RaceBots.cs"
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Scripts/Core/Runtime/Race/RaceStartConfig.cs"
  - "docs/tasks/T-161-baslangic-pozlari.md"
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
Kullanici 06.11: indirilenlerdeki `HumanCharacterDummy_F.fbx` "karakter baslangic hazirligi icin kullanicaz ... koy calissin, kontrolu ben yaparim". FBX'te iki klip var: `Readypose`, `GOpose.001`. Eskisiyle ayni kemikler; ek olarak `Helmet`, `Shoes`, `T05` nesneleri var, onlar alinmaz. Eski FBX ve Malik'in prefab'i degismez (Malik ayni anda bayrak yarisinda calisiyor).

## Istenen
- **Klipler:** `RaceStartConfig`'e iki `AnimationClip` alani: `readyPose` (READY = `RunnerStartStance.Marks`), `setPose` (SET = `RunnerStartStance.Set`, `GOpose.001`). Tooltip'li. Bos ise T-153 hesaplanan durus aynen calisir.
- **`Bridge/RunnerStartPose`** (MonoBehaviour, Malik'in prefab'ina eklenmez, calisma aninda eklenir, `RelayBatonArms` gibi): `RunnerPoseDriver`'in `LateUpdate`'inden sonra calisir (`[DefaultExecutionOrder]`). Iki klibi bir kere ornekler: bulundugu kemiklerin yerel donus ve pozisyonlarini saklar (`clip.SampleAnimation` ile, sonra eski haline dondurur; kare basi ornekleme yok, bot sayisi cok). Her kare `driver` agirligiyla karistirir: `Slerp/Lerp(simdiki, hedef, w)`, hedef `Lerp(ready, set, _stanceSet)`.
- **Agirlik:** `RunnerPoseDriver`'a salt okunur `StanceWeight` (`_stanceW`) ve `StanceSetWeight` (`_stanceSet`). Yaylar T-153'ten: ilk basista `blockOutSpring` ile yumusak cikar, snap yok. `w <= 0` iken hic is yapma.
- **Baglama:** `RaceSessionAdapter` oyuncu runner'ina, `RaceBots` bot kopyalarina ekler (`ApplyStartStance` yanina). Klip yoksa eklemez.
- **Ornekleme koku:** `Animator`'in oldugu nesne (klip yollari ona gore). Eslesen kemik sayisi 0 ise tek sefer `Debug.LogWarning`, duzgun devam.
- **Kapsam disi nesneler:** yalniz var olan kemiklere yazar, klip baska nesne oynatsa da hiyerarsi degismez.

## Sonra (senin isin degil)
`needs: tip: ASSET`: scene-integrator FBX'i `Assets/Speed-Staes/Animations/Start/RunnerStartPoses.fbx` olarak alir (Generic, malzeme yok), klipleri `RaceStartConfig` asset'ine baglar.

## Kurallar
Yalniz `owns_paths`. Sahne, prefab, asset, `.meta` yazma. Unity calistirma, derleyici yok; C# 9, `using System;` + `using UnityEngine;` olan dosyada `Object/Random/Debug` niteliksiz degil. Yeni alanda Tooltip. Commit `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasi scratch'te (`C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\1c7d0dba-289b-4948-ba09-42f88d086602\scratchpad\`), adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. `git add -A`, push yok. Calisma yeri `D:\Pundun\speed-starts-ik`.

## Calisma notu
