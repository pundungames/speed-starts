---
id: T-123
title: Sirali basis cift sayiliyor, tek ayak yarim kaliyor, ayakkabi ucu zemine giriyor
status: review
owner: gameplay-agent
depends_on: [T-122]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [79916d4e, 56a83a56, 7b53acc5]
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
Kullanici 05.11: "surekli zeminin icine giriyor, bacaklar anlik sekiyor; bazen sag ve sol duzgun basiyorum ama ayni anda basiyormusum gibi ziplamaya basliyor; tek ayak basarak gitme cok kotu, gercekten ziplatmiyor, oldugu yerde kayiyor; tek ayaktayken ust vucut arkaya yonelmeli ama yapmiyor."

## Bulunan kok sebepler (kod okundu, olculmedi)
1. **Sahte cift basis (`PairWeight`):** `d = |a.lastPress - b.lastPress| % cycle`, `min(d, cycle - d)`. Mod yuzunden iki basis arasi `cycle`in katina yakinsa "birlikte" sayiliyor. `cycle` kirpildigi icin (2 x stepTimeMax) yavas sirali basista (~0.6-0.7 sn) agirlik 1'e kadar cikiyor. Ayni agirlik `Key`, `hopRise`, kollar, gövdeyi degistiriyor: anlik sekmelerin de bir kaynagi.
2. **Yarim hop:** `RunnerSim`: `_hopBias += ((LastLeg == leg ? 1 : 0) - _hopBias) * follow`; `repeat`in `sinceLast < 1` kosulu yok, uzun aradan sonra ayni ayak 0.45 veriyor (tek adimda hop acilir). Poz: `_hop = SmoothStep(0, 1, HopBias)`: kararli durumda 0.25 sn'de ~0.9, 0.5 sn'de 0.66-0.85, 1 sn'de 0.40-0.75; duzensiz ve yavas tek tus yarim kaliyor. Geriye yatma `lean -= hopBackLean * _hop`, ustelik basili tus kafayi one attiriyor (`_headAngle * (1 - _hop)`): ikisi birbirini yiyor.
3. **Ayakkabi ucu:** taban kontrolu `B-toe` kemigi (`ballOffset`), ucu hesaba katmiyor; burun asagi donunce ucu kemikten ~8 cm ileride zemine giriyor. `swingToe` parmagi da asagi bukuyor. Olcum `toeY` de ayni kemigi olcuyor, yani temiz gorunuyordu.

## Isler (her biri ayri commit)
1. `PairWeight` yeniden: iki basis gercekten yakin mi? (`togetherWindow` civari gercek zaman farki, mod yok.) Birlikte basis agirligi yukseltir, tek basis (pencere dolup ortagi gelmediyse) dusurur, zamanla soner; sirali basista hic bir aralikta agirlik olusmaz. Gercek cuval ziplama (iki tus hep birlikte) oldugu gibi kalir.
2. `_hopBias`: yalniz `repeat` (ayni ayak ve 1 sn icinde) arttirir. Poz: hop agirligi tekrarda hizla dolsun: 2. tekrarda belirgin, 3.'de tam, yavas tek tusta da (1 sn'e kadar) tam kalsin. Tek adim 0. Geriye yatma tek tusta one atma ile yarismaz: kafanin one itisi hop agirligiyla tamamen soner, govde gercekten geride kalir.
3. Taban: `PoseAnkle` son adiminda taban noktalari topuk, top ve UC (top + `toeTipLength` ~0.08, Tooltip'li alan, parmak bukumu dahil). Once parmak yukari esner (uc yere paralel), yetmezse ayak doner. Basan ayakta da ayak yere gomulmez.

## Kapsam disi
- Hiz ve zamanlama degismez (`_hopBias` degisikligi yalniz 1 sn'den uzun aradaki ayni ayak). Baska ayar yok. Prefab, sahne. Unity, test, olcum yok.

## Calisma notu
- 1: PairWeight yalniz iki son basisin gercek zaman farki (PairWindow 0.06); mod yok. 2: _hopBias yalniz repeat ile artar; poz HopBiasFrom 0.2 / To 0.7. 3: PoseToes sonunda uc kontrolu (toeTipLength 0.08): once parmak yukari, sonra ayak.
- Olculmedi. Emin degilim: toeInFoot'un birim ve ileri yonlu oldugu varsayimi, RunnerSimRelationTests'in hop bias testlerinin (230-250) yeni repeat kuralinda gecmesi, hop agirliginin ara ritimlerde (0.4-0.6 sn) tam dolmasi.
