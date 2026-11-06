---
id: T-126
title: Cok hizli basis gorunumu bozmasin, yavas kosuda butun govde tepki versin, ayak donusu, kucuk kararsizliklar
status: review
owner: gameplay-agent
depends_on: [T-125]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [f375b17a, 4e77f866, afc441fd, 6e83c34e]
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
Kullanici 05.11 (`5e9a7702`): "cogunlukla iyi. Cok hizli basarsam, surekli spam edersem sacma sapan seyler oluyor, cok hizli basislar goruntuyu bozuyor. Yavas kosarsam karakterin gotunden basina kadar olan govde hicbir tepki vermiyor, arkaya dogru gidip gelmesini yapacaktik. Kalan isleri de yap, toplu bakalim." Mevcut iyi hali bozma. Dort ayri commit, toplu gecer.

## Isler
1. **Spam gorunumu bozmaz (`OnTapped`, yalniz poz; sim her basisi sayar, hiz degismez):** bacak ve kol, bir insanin atabilecegi adimdan hizli yeni adim baslatmaz.
   - Son KABUL edilen basistan `PoseMinGap` (~0.13 sn, ~7.7 adim/sn, sabit ya da Tooltip'li alan) icinde gelen basis: `BeginStance`, `NoteCycle`, `_lastPress` yok; kol saati (`_armPresses`, `_armClock`) ve hop hedefi (`_hopPresses`) sayilmaz. `l.held` tus bilgisi aynen.
   - Sirali spam adim sirasini bozmasin: kabul edilen basisin bacagi, arada diger tus atlandiysa bir oncekinin tersidir (L,R,L,R 0.08 sn aralikta tek bacakta sekmeye donusmez). Tek tus spam tek ayak kalir.
   - Sahte cift: basis akisi icinde (ondeki basistan < ~0.12 sn) `PairWeight` olusmaz; cift yalniz ayrik birlikte basis.
   - Gorunen sonuc: spamde kosu bacaklari kisa ve akici, govde sim'in `strain`i ile yatar (mevcut), snap yok.
2. **Yavas kosuda govde:** `UpdateBounceLean`: `rw` `_gaitW`'ye bagli, `_gaitW` yavas kosuda ~0: sallanma yok. Basis sallanmasi hiza bagli olmasin (dalis, tutma, tek ve cift dalini bozma). Kalcadan basa: pelvis de katilir: `lean`'e kucuk pay (bacak acisi `legLean` yok; planted ayak IK ile yerinde kalir, serbest bacak buna gore oynamasin). Yavas ritimde geri donus basis araligina yayilsin: `_armKick` dususu 0.167 sn'de bitiyor, ayri saat ya da yay: yumusak git-gel. Miktar hafif-orta, yavasta da gorunur (~8-10 derece). Pelvis payi gozle gorulur, kusma yok.
3. **Ayak donusu hizli:** `limbSpring` 12/0.8 -> 14/0.85, `limbLag` 8.5 -> 10; bekleme durusuna donus: prefab'ta kayitli `frontHomeTime` 0.3 ezer, yeni alan `stockReturnTime` 0.16 (Tooltip'li), `_idleStock` dallari onu okur. T-121'deki 15/11 ile ikili sekme iliskisi kanitlanmadi; yine de biraz altinda kal.
4. **Kararsizliklar:** (a) `Reach` carry esigi histerezisli: giris 0.08, cikis ~0.12 (kareler arasi titreme). (b) `_idleStock` basis aninda bir kare eski kalmasin: `OnTapped` basis geldiyse hemen false. (c) Koddaki gereksiz tekrar kalmissa temizleme, davranis degismez.

## Kapsam disi
- Tek ayak ayari (kullanici somut sey soylemedi; kor ayar bozabilir). `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- 1 f375b17a: OnTapped PoseMinGap 0.13: hizli basis yalniz sim'in; poz kendi sayaci _posePresses (kol saati ve hop hedefi onu okur); sirali spam bacagi cevirir (_poseSkippedOther); ayrik cift (pairSecond) kabul. sim.Presses okuyanlar: UpdateGaitLean (_kick, degismedi); UpdateHop kol ve hop hedefi artik _posePresses/_poseLastLeg/_poseLastTime.
- 2 4e77f866: rw _stepping'e bagli, _rockKick basis araligina yayilir, pelvisRockShare 0.3 (lean, legLean yok).
- 3 afc441fd: limbSpring 14/0.85, limbLag 10, stockReturnTime 0.16.
- 4 6e83c34e: Reach carry 0.08/0.12 histerezis, OnTapped _idleStock=false.
- Emin degilim: 0.13 sn esiginin ritim 0.14-0.2'de sinirda gecmesi (tum kabul edilen basislar bir sureli kayar), hizli spam'de sahte cift/yon cevirme senaryolari kordu.
