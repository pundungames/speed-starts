---
id: T-125
title: Tussuz bacaklar kendi hareket etmez stock poza doner, input ust bedeni hafif geri gotürür, tek zıplamada dirsek acilir
status: review
owner: gameplay-agent
depends_on: [T-124]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [dc8eee88, 9eb8381e]
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
Kullanici 05.11 (`9b71bf79`):
- "karakterin bacaklari otomatik hareket etmesin, input vermedigimde oldugu gibi stock poz'a geri gelsin"
- "input gecislerinde araya kendin animasyon uydurma, karakter sadece benim verdigim inputa gore gecis yapsin"
- "input verince karakterin ust bedeni hafif geriye dogru gidip gelsin, smooth"
- "tek ayak ziplarken de dirsekten aci var, kol acilsin, dirsegi sabit tutma"

## Isler (iki ayri commit, bu sira)
**A. Kol ve ust beden (bacak ve kalcaya dokunmaz):**
- Tek zıplama dirsegi ters: `armHopElbow - armHopElbowSwing * (2 kick - 1)` geride (kick 0) 120 bukulu, tepede 60. Dogrusu: geride ACIK (~35-40), tepede bukulu (~85-90); kosudaki `open` egrisi gibi. Sayilar Tooltip'li alanlarda.
- Kullanici: "dirsekler genel olarak daha oynak olabilir, biraz daha acabilirsin": kosuda geride 28, onde ~85; tek ziplamada geride ~28, tepede ~90; cift ziplamada (25,105).
- Basista ust beden hafif geri gidip doner: kosu ve tek ayak: `runRock`, `hopRock` tek yonlu: `-rock * _armKick` (bas gelince geri, sonra 0'a), yay yumusatir. Bir kez one savurma kalkar. Miktar hafif: kosu ~8, tek ayak ~8 (zaten geride). `accelRock` kalir, cift ayak degismez.

**B. Tussuz stock poz:**
- Son basistan bu yana bir adim suresi gecti ve tus basili degilse bacaklar plana devam etmez: `Recover/Mid/Reach/Front` anahtarlari biter, ikisi de dogrudan stock poza (bekleme duruşu, `Rest` hedefi) `Track` ile gider. Ara poz, kendi adimi yok. Yeni basis nerede kaldiysa oradan alir.
- Kendi kendine adim: `PinFeet`: tussuz gövde kayarken civili ayak bacak boyu bitince kopuyor, `pinOffset` eriyor, ayak one gelip tekrar civileniyor: tekrar eden otomatik adim. Tussuz durumda bu dongu olmayacak: kopan ayak yeniden civilenip adimlamaz. Koşucu duruyorsa (hiz ~0) civi normal.
- Tutma, dalis, yere yerlesme, basisla gelen itis, `PinFeet`'in basisli kosudaki kurali aynen.
- Bilinen acik: sim `stopBrake` 3.5 ile tussuz ~3 sn suzulur; stock pozda bu surece ayak yerde kayar. Sim sayisina dokunma, kullanicinin karari soruldu.

## Kapsam disi
- `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok. Baska ayar yok.

## Calisma notu
- A dc8eee88: tek zipla dirsek acik geride (28) / bukulu tepede (90), open egrisi; hopRock/runRock tek yonlu (-rock * _armKick), 8; kosu dirsegi geri 28, on ~85; cift ayak (25,105).
- B 9eb8381e: _idleStock = !_stepNow (stepTimeMax 0.6 sn: 0.5 sn ritim kesilmez); Recover/Mid/Reach/Front/Push-catch -> Rest; tussuz toe-off -> Rest; PinFeet tussuzda giren yeni pin yok (hiz < 0.3 normal).
- Emin degilim: _stepNow'un stepTimeMax'i (0.6) "bir adim suresi" icin yeterince kisa olup olmadigi; tussuz suzulurken Rest ayagi kayar (bilinen).
