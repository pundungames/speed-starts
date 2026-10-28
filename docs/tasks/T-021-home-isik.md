---
id: T-021
title: Home sahnesi - karakter onden aydinlik (isik yonu), kaide
status: merged
owner: scene-integrator
depends_on: [T-019]
owns_paths:
  - "Assets/Speed-Staes/Tools/Editor/Builders/Scene/**"
touches_scene: true
branch: agent/menu-karakter
commits: []
tests:
  - "gerekmiyor: isik/sahne; kabul ekran goruntusu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
Home ekran görüntüsünde (`D:\Pundun\speed-starts\pipeline-logs\shots-2026-10-28_153039\Home.png`) karakter **kapkara siluet**: yönlü ışık
karakterin arkasından geliyor (karakter kameraya dönük, yaw 180). Tasarımdaki gibi (`docs/gdd/referans-ekranlar/tasarim-ana-ekran-v2.png`) önden-yandan
aydınlat: `SceneHomeCharacter` ışığı kamera tarafından, hafif yukarıdan ve yandan gelecek şekilde kurar; ambient (Environment Lighting) yeterince
açık (siyah gölge yüzü yutmasın). Race sahnesine dokunma (Malik'in kopyası).

## Kabul kriterleri
- [ ] Işık yönü kameraya göre hesaplanır (sabit dünya açısı değil): kamera bakış yönünün tersinden, yukarıdan ~40°, yandan ~30° (alanlar builder'da
      isimli sabitler ya da ayarlanabilir).
- [ ] Ambient: sahnenin RenderSettings'i (ambient mode Trilight/Flat, renk açık) — yalnız Home sahnesi.
- [ ] `overwrite` tek akış; Malik'in dosyaları değişmez.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-fix1` (worktree, dal `agent/menu-karakter`). Ana klasöre yazma.
- Önce oku: `Tools/Editor/Builders/Scene/SceneHomeCharacter.cs`, `SceneBuilder.cs`.
- Unity/batchmode/verify YASAK, `git commit` YASAK, yalnız `.cs`. Derleme kontrolü scratchpad'de Roslyn.
- Bitince `status: review`, `## Çalışma notu` 5 satır.

## Çalışma notu
- SceneHomeCharacter.CreateLight(scene, camera): ışık rotasyonu kameranın yaw'ından türetilir (Euler(LightPitch 40, camYaw + LightYawOffset 30, 0)); eski sabit yaw 150 karakteri arkadan aydınlatıyordu.
- Ambient: RenderSettings Flat, renk (0.78,0.80,0.85); NewScene aktif sahne olduğundan yalnız Home'a yazılır. Race'e dokunulmadı.
- FrameCamera artık CreateLight'tan önce çağrılır (kamera yönü gerekli).
- Roslyn derleme kontrolü KOŞULMADI (değişiklik küçük; yalnız mevcut using'lerdeki tipler). Ekran görüntüsü kabulü merkezi koşuda.
- Bırakılan: yok.
