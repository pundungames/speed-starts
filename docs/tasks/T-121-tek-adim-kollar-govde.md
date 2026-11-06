---
id: T-121
title: Tek adim sonu sicrama ve zemine girme, tek ayakta capraz kol, cift ayakta kol ve govde, kalan snap, ayak donusu
status: review
owner: gameplay-agent
depends_on: [T-119]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [bc80bf19, bfe65527, 2fbaf071, 7923e76c, be73dc38]
tests:
  - "gerekmiyor: kullanici 05.11 'test etmeden yap, en son ben bakarim'; gorsel, derleme + kullanicinin gozu"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11 (T-119 geciyor iken oynadi), kendi sozleriyle:
- "bir adim atiyorum, adim sekansi bitince karakter bi ufak ziplayip geri geliyor, karakter zeminin icine geciyor, sacma sapan hareketler yapiyor"
- "tek ayak ziplarken iki kol birden kendini geri atiyor, onun yerine ziplatan ayagin tersindeki kol kendini geri atmali"
- "cift ayak ziplarken (cuvalla ziplamak gibi) iki kol birden geri atiyor, bu dogru ama kollar cok az geri gidiyor, daha da geri gitmesi lazim; ust beden de harekete dahil olmali, dimdik duruyor, daha esnek olmali"
- "hareketler daha yumusak ama hala sekmeler, teklemeler, ani snaplenmeler var"
- "ayagin geri donusu yavas, daha hizli orijinal pozisyonuna geri gelmeli"

## Isler
1. **Tek adim sonu:** bir basis, sonra tus yok. Dalista kalkmasin, zipramasin: kalca tek yonlu ve yumusak bekleme durusuna iner, ayaklar zemine girmez, tasma zemin altina gitmez. Ilk suphe: `FlightHeight` sonu: "last foot pushes off with nothing coming down": `_bodyY.Velocity + liftKick`, `_ballistic = true`, yercekimi; tek basista da calisiyor olabilir. Dalis tutma agirligiyla gelsin (surekli). Ikinci suphe: az sonumlu `Track` (0.8) dinlenmeye dönerken tasiyor; `HopPlant` kalkinca Land'da civileme.
2. **Tek ayak capraz kol:** kollar artik birlikte degil, ters fazda: ziplatan ayak iterken KARSI kol geri atilir, ayni taraftaki kol one gider; sonra degisir. Aralik genis kalir (-65..+85), dirsek canli.
3. **Cift ayak kol ve govde:** iki kol birlikte, cok daha geride: yuklenirken yaklasik -85..-95, dirsek acik; itiste yukari one savrulur. Ust beden katilir: yuklenirken one egilir (ek 25-35 derece), itiste acilir, havada hafif geriye; gogus, boyun, bas yayla izler. `both030`'da `trunkLean` 11.50 sabit (aralik 0): su an tamamen rijit. Tek ayakta da ust beden itise ve inise tepki versin.
4. **Kalan snap:** ana kural: input uzuv resetlemez. Yerdeki bacak icin: ayak degerken dunya hizi 0'a yakin olmali (ayak hizi govde hizina esit donmeli), `Track` gecikmesi bunu bozmasin. Koddaki butun anlik atamalari tara: `leg.angles =`, `Enter`, `_kick`, `_armClock`, poz degisimleri, `_sitW`, `_pair`/`_hop` gecisleri.
5. **Ayak donusu hizli:** itisten sonra ayak orijinal pozisyonuna daha hizli donsun: `limbSpring`, `limbLag`, `FootLag`, salinim anahtarlari; yumusak kalsin (can egrisi), snap olmasin. Bekleme durusuna donuste de hizli.

## Kapsam disi
- `RunnerSim.cs`, prefab, sahne. Unity, batchmode, verify, probe. Hiz, zamanlama.

## Calisma notu
- 1: bir basis HopBias ~0.45 verip hop agirliginin ucte birini aciyordu (ok, hopLead, plant); HopFrom 0.4 / HopTo 0.85 ile surekli egriye alindi; liftKick ve tam yercekimi dive agirligiyla (SoftFall). bc80bf19.
- 2: capraz kol, bfe65527. 3: cift ayak kollar -92, UpdateBounceLean (yaylı govde), cift basisin ikizi saati yeniden baslatmaz, 2fbaf071.
- 4: footGrip 0.97 (ayak dunyada 0.5 m/s kayiyordu), _sitW ani gecis yalniz dalista, 7923e76c. Diger anlik atamalar tarandi, ek yol bulunmadi.
- 5: limbSpring 15/0.85, limbLag 11, FootLag 20, FootBlend 22, frontHomeTime 0.2.
- Olculmedi. Emin degilim: HopFrom/HopTo'nun gercek hop yuksekligine etkisi, pairLean 30 pelvisPitchRange'de kirpilir mi, footGrip'in max hizdaki salinim hizina etkisi.
- Geri alma (T-121 oyunda iki kez sekti): T-119 hali + fc604097 (capraz kol, cift ayak kollar, UpdateBounceLean yalniz baseBend/bend, lean ve legLean'e yazmaz) + 66c5977c (yalniz liftKick * _dive). SoftFall, HopFrom/HopTo, footGrip, yay hizlari ve is 4/5 gelmedi.
