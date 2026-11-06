---
id: T-128
title: Kosuda adim hizla acilsin, yavas kosuda bacaklar dar kalmasin (Malik'in adim acilari)
status: review
owner: gameplay-agent
depends_on: [T-127]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [a9340e38]
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
Kullanici 05.11: Malik'in dalindaki genis adimi alalim, "bizim branchde de aynisini yap ama bozmadan". Malik'in dali birlestirilmez; acilar IKTest'in kendi yapisina yazilir.

## Olcu (hedef acilar, uyluk/diz)
- IKTest bugun, 2 m/s: `_amp` ~0.26, itis sonu ~-3, on ~+25 (dar).
- Malik, 2 m/s: itis -36/150, geri -37/83, on 61/155. Malik, 8 m/s ve ustu: geri -56/88, on 78/145.

## Isler (tek commit)
1. `strideMin` 0.25 -> 0.8; yeni `jumpStrideMin` 0.25 (hop ve cift ziplama eski tabaninda kalir).
2. `strideSpeed` degismez (hop ve cift ziplama ona bagli); yeni `fastStrideSpeed` (2, 8) yalniz hizli acilar icin.
3. Yeni alanlar: `recoverFast` (-60, 88), `kneeOpen` (68, 140), `kneeOpenFast` (78, 130). Yalniz duz kosuda devrede; `kneeDrive` ve `recover` degismez.
4. `stanceDip` buyuklugu eski formulde kalir (govde yavas kosuda daha fazla cokmesin).

## Kapsam disi
- `contact`, `pushEnd`, `mid`, hop ve cift ziplama anahtarlari; tutma, dalis (T-129). `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- Kod: strideMin 0.8, jumpStrideMin 0.25 (Amp tabani max(_hop,_pair) ile karisir); RecoverKey()/KneeDriveKey() yardimcilari dort cagri yerinde.
- Cukur: `_dipAmp` eski 0.25 tabaniyla (const DipStrideMin) hesaplanir, StanceNeed onu kullanir.
- _hop veya _pair 1 iken _plainRun=0: anahtarlar bugunkuyle birebir (okuyarak dogrulandi). Calistirilmadi.
- Bilek/kol anahtarlari bu iki anahtari okumuyor; baska okuyucu yok.
