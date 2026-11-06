---
id: T-122
title: Yere basan ayak orada durur, karakter hareket eder; ayak zemine girmez
status: review
owner: gameplay-agent
depends_on: [T-121]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [3db02e02]
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
Kullanici 05.11: "herhangi bir ayak yere basiyorken o ayak orda durmali. karakter hareket etmeli. gerekirse o ayak bacak karakteri ziplatmali ama ayak zeminin icine girmemeli."

Mutlak kural, faz bagimsiz: yere degen ayak dunyada SABIT. Ayarla degil, kurulumla garanti edilir.

## Bulunan sebepler (koddan)
1. `AdvanceStance`: `leg.footZ -= rootStep * footGrip`, `footGrip` 0.92: basan ayak govde yolunun %8'i kadar yerde surunuyor (10 m/s'de 0.8 m/s). Ayni carpan `SolveStance` `stepBack`, `StanceRate` icinde.
2. `UpdateCarry`: `Reach`, `Front`, `Rest` bacaklari `carry = true`, ama `grounded` degil: yerdeki ayak civilenmiyor, FK ile govdeyle birlikte tasiniyor. Olcumde `mash012` karelerinde iki ayak yan yana yerde 7+ m/s kayiyordu; `skim` kosuda 13-36, ezmede 52-73 kare.
3. Topuk kalkarken ayak bilek etrafinda doner: top z'si oynar.

## Tasarim
1. `footGrip` kalkar: basan ayak koku tam izler. `StanceRate`, `stepBack` de tam hizla.
2. **Son katman, butun bacaklar, her faz (`WriteLeg`'den once, govdeyi oynatan her seyden sonra):** her ayagin dunya z'si tutulur.
   - Ayak yere degiyorsa (`ankle.y - AnkleGround <= TouchHeight`, cikis icin histerezisli) ve bacak bir kez yukari kalkmaya baslamadiysa: `pinZ` = o anki dunya z'si, `ankle.z = pinZ - rootZ`. Kok konumunu dogrudan kullan, hiz integrali degil; surtunme kalmaz.
   - Bacak uzunlugu biterse (kalca-ayak mesafesi `MaxReach`) ayak kaymaz: kopar, toe-off. Yerden kalkmadan yatay gitmez: kalkarken y once yukselir.
   - Ayak yerdeyken kilit disinda yatay hareket etmez: FK yeni yer gosteriyorsa ayak once kalkar.
   - Kilit bitince FK konumuna fark sonumlu erir (ofset ~15-25 1/sn), snap yok; dususte `ankle.y`, ofset eriyene kadar yerden kalkik kalir.
   - Dizler hint/IK ile cozulur, kilit kalca ve uyluk zamanlamasina dokunmaz.
3. Topuk kalkarken pivot top: top z'si yerinde kalir, bilek ona gore hesaplanir.
4. Zemin: bu katmandan sonra da ayak topu ve topugu zeminin altina inmez (`PoseAnkle` son adimi kalir).
5. "Gerekirse ziplatir": basan bacak gerilince toe-off var; ayak kalkana kadar govde o ayaga bagli, kalkis itisle.
6. Disarida kalanlar: `preview`, jump, dalista yere yerlesme (`_settle`), basili tutmada arkada duran bacak davranisi bozulmaz; orada kural gerekiyorsa agirlikla.

## Kapsam disi
- Baska hic bir sey: kol, govde, yay, hop ayari yok. Tek degisiklik. `RunnerSim.cs`, prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- 3db02e02: footGrip silindi; PinFeet (Rest/Front/Reach/Land, dunya noktasina civi, bacak boyu biterse ya da ayak kalkarsa birakir, pinOffset 20/sn ile erir ve 6 cm'e kadar kalkik tutar); topuk kalkarken top pivotu (lockBallZ/BallZ, SolveStance ballShift).
- Kilit acilma/kapanma: Enter ile Push+grounded olan ayak pinned noktasini footZ olarak alir (pinOffset sifirlanir); Recover/Mid/Hold/Push-catch fazina gecen pin birakilir ve erir; Land plant'i T-119'dan, dalis/jump/settle PinFeet disi, dive agirligiyla solar.
- Emin degilim: uzayan pin bacagin gerilmesiyle sik birakir mi (Rest'te), knee.z yarisi yaklasimi, ball pivotunun kosuda kalcaya yansimasi. Olculmedi.
