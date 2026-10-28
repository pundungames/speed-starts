---
id: T-020
title: Menu karakteri - kollar asagi ayakta durus (T-poz yerine), Malik'in koduna dokunmadan
status: merged
owner: gameplay-agent
depends_on: [T-017]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Bridge/**"
touches_scene: false
branch: agent/menu-karakter
commits: []
tests:
  - "gerekmiyor: kemik pozu; kabul PlayMode ekran goruntusu (T-022)"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-10-28
finished: 2026-10-28
blocked_reason:
---

## Hedef
`MenuCharacter` (T-017) poz sürücüsünü kapatıyor; karakter prefab'ın dinlenme duruşunda kalıyor ve o duruş **T-poz** (ekran görüntüsü:
`D:\Pundun\speed-starts\pipeline-logs\shots-2026-10-28_153039\Home.png`). Tasarımdaki gibi (`docs/gdd/referans-ekranlar/tasarim-ana-ekran-v2.png`)
ayakta, kollar aşağıda, rahat bir menü duruşu ver.

## Kabul kriterleri
- [ ] `MenuCharacter` (ya da yanında yeni `MenuIdlePose` bileşeni) `LateUpdate`'te üst kol / ön kol (gerekirse el) kemiklerine yerel dönüş uygular
      (kemikler isimle: `B-upperArm.L/R`, `B-forearm.L/R` … — gerçek adları prefab'tan ve `RunnerPoseDriver`'ın bulduğu adlardan doğrula). Açılar
      `[SerializeField]` + Tooltip (Inspector'dan ayarlanır); varsayılanlar kollar gövde yanında, dirsek hafif bükük.
- [ ] Rig (Animation Rigging) ile çakışmaz: menüde el IK kapalı (T-017 notu), bacaklar mevcut haliyle.
- [ ] Malik'in kodu/prefab'ı değişmez; poz yalnız menü örneğinde (Race'te hiçbir etki yok).
- [ ] İsteğe bağlı: çok hafif nefes salınımı (göğüs/omuz, küçük genlik, Tooltip'li alan) — yoksa sabit duruş yeter.

## Çalışma kuralları
- Çalışma klasörü MUTLAK: `D:\Pundun\speed-starts-fix1` (worktree, dal `agent/menu-karakter`). Ana klasöre yazma.
- Önce oku: `Scripts/Bridge/MenuCharacter.cs`, Malik'in `Scripts/Physical/RunnerPoseDriver.cs` (kemik adları, kol kurulumu; salt okunur), `CLAUDE.md` (Rig bölümü).
- Unity/batchmode/verify YASAK, `git commit` YASAK, `.meta` yazma. Derleme kontrolü scratchpad'de Unity'nin Roslyn'iyle.
- Bitince `status: review`, `## Çalışma notu` 5 satır (kemik adları, varsayılan açılar).

## Çalışma notu
- Yeni `Scripts/Bridge/MenuIdlePose.cs`; `MenuCharacter.Initialize` runner'a otomatik ekler (sahne bağlaması gerekmez). Malik'e dokunulmadı.
- Kemikler: `B-upperArm.L/R`, `B-forearm.L/R` (RunnerPoseDriver ile aynı), nefes için `B-chest`. LateUpdate (order 1000), rig'den sonra.
- Yöntem: sürücünün PoseArm tarifi (runner uzayında FromToRotation + rest üstüne), eksenlerden bağımsız. Varsayılan: armOutward 8, armSwing 0, elbowBend 14, nefes chest 0.8 / kol 0.8 deg, 0.22 Hz.
- Roslyn ile derlendi, hata yok (yalnız CS0649 uyarısı). Oyunda görülmedi.
- Risk: rest T-poz'un init anında okunması; elbow işareti yanlışsa (kol geri bükülürse) elbowBend'i eksiye çevir.
