---
id: T-110
title: Govde ve kollar - bacaklara tepki veren ust beden, hizla egim, gercek kol pompasi
status: merged
owner: gameplay-agent
depends_on: [T-108]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-govde
commits: [5366e19a, 8b7d38f8]
tests:
  - "gerekmiyor: gorsel; T-107 olcumu once/sonra karsilastirilir"
analytics:
  - "yok: projede analytics yok"
needs:
  - tip: DECISION
    durum: closed
    not: "Scripts/Physical kilidi (T-108 ile ayni karar). -> kullanici 04.11 acti"
started: 2026-11-04
finished: 2026-11-04
blocked_reason:
---

## Hedef
Slack: "üst vücut tek başına duruyor, sadece bacaklar hareket ediyor"; "üst beden hep tam dik"; "yandan bakınca ayak ve gövde ayrı
gibi"; "kollar gerçekten koştuğu hissi versin, hızına/doğruluğuna göre farklı".

## Kök sebepler
1. Eğim yalnız baştan gelir (`UpdateLean`); ritimli koşuda baş −12..+3° olduğundan gövde fiilen dik.
2. Gövde yalnız X'te döner (dosya başı: "Y and Z rotations stay 0"): kalça burgusu, omuz karşı burgusu, yana düşme yok; bacaklar gövdeye hiçbir tepki aktarmaz.
3. `StepArm`: kol yalnız evden geriye gider, bacak öne uzanırken dinlenmede durur; `armBack`/`elbowBack` sabit, hızla değişmez. Ölçülen kol ileri +33..39° aslında pompa değil: `_stepping` düşük olduğu için `Ready` pozunun yumruğu çenede duruşu karışıyor (T-109 madde 1 bunu kaldırır, kol ondan sonra ölçülür).
4. Ölçüm (önce): gövde eğimi ort. `perfect` 7.5°, `rhythm026` 1.2°, `late050` −2.4°; kalça ve göğüs yaw'ı ~0.

## Yapılacak
1. Kalça burgusu (yaw): `pelvisTilt`'e iki uyluğun farkı × `pelvisTwist`, kalça eklemleri `LegFK`'de birlikte döner (adım da uzar). Göğüs ters yönde (`chestTwist`, `spineTipTarget`). Baş ileri bakmaya devam eder.
2. Yana düşme (roll): salınan bacak tarafına küçük kalça düşmesi, göğüs ters (`pelvisRoll`).
3. Hızla eğim: yay zincirine baştan bağımsız ek hedef: `gaitLean × hız eğrisi` + ivmelenirken ek (`accelLean`). Sim'in `Form`'u düşükken dikleşmez, bozulur: baş zaten öne gider. Kaldırılan `speedLean`, `trunkFollow`, `stepKick` adları KULLANILMAZ.
4. Basış tepkisi: her itiş `sim.LastEfficiency` kadar gövdeye küçük pitch vuruşu; iniş anında göğüs kalçanın gerisinden gelir (yay). Salınım yok, sönümlü.
5. Kol pompası: kol karşı bacağın uyluğunu **iki yönde** izler (ileride de); genlik ve dirsek `hız × Form` ile büyür: yavaşta küçük ve gevşek, hızlıda geniş ve 90°'ye yakın; ezmede dağınık. Omuz burgusu kolla birlikte.

## Kabul kriterleri (T-107)
- [ ] Kalça ve göğüs yaw'ı ters fazda, `perfect`'te aralık en büyük, `holdBoth`'ta ~0.
- [ ] Gövde eğimi ort.: `perfect` ≥ 8° öne, `late050` ondan az; dikey "hep aynı açı" durumu yok (kare içi aralık > 3°).
- [ ] Kol swing aralığı `perfect` > `late050`; `perfect`'te kol öne de geçer (swing > 0).
- [ ] Ayak ve diz twist'i bozulmadı (`FixLegTwist`: kareler), baş yere çakılmıyor (`holdBoth`), `RaceFinishSequence` duruşu, `RelayBatonArms`, emote, menü karakteri hata vermiyor.
- [ ] Bütün yeni alanlarda `Tooltip`; dosya başındaki "only pitches around X" güncellendi.

## Kapsam dışı
- Bacak fazları (T-109), iki/tek tuş (T-111), sim, kamera.

## Çalışma notu
- Yapılan (dal agent/ik-govde, RunnerPoseDriver.cs): `UpdateGaitLean` (hız+ivme eğimi, basış pitch'i, kapı), `UpdateTrunk` (kalça/göğüs burgusu ve roll, uyluk farkından), `StepArm` yeniden (çift yönlü pompa, gait ağırlığıyla eski Ready/armBack davranışına karışır).
- Kapı: `_stepping` × hız eğrisi × yerde × zıplamıyor × yerleşmiyor × tutma ağırlığı (0.35-0.95 yumuşak); baş eğimi >30° ve geriye eğim eğimi sönümler (ezme, dalış, SitBack aynı).
- Zorlanılan: Unity/ölçüm çalıştırılmadı; işaretler (yaw/roll) koddan türetildi, ekranda doğrulanmadı. T-109 ile birleşince `thighWorld` sürekliliği varsayıldı.
- Bırakılan: ölçüm (T-107), varsayılan değerlerin ince ayarı; prefab'a yazma yok.
- Süpervizör: kapıdaki _carried koşuda titreşiyordu, T-111 turunda _ballistic'e geçti. Son ölçüm: eğim 19°, kalça yaw 17.7°, göğüs 11.3°, kol −50..+45.
