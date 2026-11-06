---
id: T-117
title: Input hicbir uzvu resetlemesin, snap yok; karakter esnek, tek tus gercek ziplama
status: review
owner: gameplay-agent
depends_on: [T-116]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "scripts/runner-olcum/snap_check.py"
touches_scene: false
branch: agent/ik-kosucu
commits: [8bd13489, ae7f9859, be508b6d, f9b6ba55, 38166c97]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11 (`IKTest` 0ccd83c6), kendi sozleriyle: "tek ayak sekerken ust govde daha geriye gitsin, one cikan ayak daha yukarida dursun"; "karakteri daha esnek yap her konudan, tum acilari arttir, robot gibi duruyor, cekinme, bacaklarini uzun acsin kaldirsin"; "kollar kutuk gibi hareket ediyor, ac korkma"; "gercekten ziplandi hissi lazim, olduğu yerde ayak kayiyor, ziplayarak ilerlemesi lazim"; "karakter surekli tekliyor bir input verdigimde; input vermeyi resetleme olarak algilama, nerde kaldiysa ordan devam ettir; bu oyun tam zamaninda sag sol inputu verme oyunu; kolunu bacagini resetleme, snapleme; gercek insan anatomisi nasil davranirsa oyle".

## Ana kural (degismez)
Input yalniz HEDEF degistirir. Hicbir uzvun gosterilen acisi ya da hizi input yuzunden bir karede atlamaz: nerede ve ne hizla gidiyorsa oradan devam eder. Tam zamaninda basmamak normaldir; oyuncu ritimden sapinca hareket bozulur ama kopmaz. Mod, sifirlama, ozel durum yok. Sim, hiz, zamanlama degismez.

## Olcum: tekleme (`scripts/runner-olcum/snap_check.py D:\Pundun\form-probe\t116b`, 60 kare/sn)
Bir karede en buyuk sicrama / bir karede hiz degisimi (derece ya da cm):

| senaryo | kol | dirsek | uyluk | diz | kalca | bilek z (kalcaya gore) |
|---|---|---|---|---|---|---|
| perfect | 35/28 | 8/9 | 32/27 | 38/50 | 3/2 | 26/22 |
| rhythm026 | 28/28 | 6/4 | 26/26 | 33/41 | 3/4 | 24/24 |
| human | 26/27 | 6/5 | 25/25 | 31/47 | 3/5 | 21/18 |
| mash012 | 27/34 | 8/7 | 29/32 | 62/56 | 3/3 | 44/55 |
| both030 | 30/27 | 9/8 | 28/25 | 63/42 | 3/3 | 33/49 |
| same025 | **95/97** | 3/1 | 20/28 | 21/28 | 4/4 | 22/23 |
| same020 | **98/101** | 2/2 | 21/27 | 27/31 | 4/4 | 30/30 |

Tek tuşta kolun 95 derecesi `_armKick = 1`den (T-113 "hizli cikis" kabulu, supervizorun hatasi, kaldirildi).

## Kabul
Olcum: `snap_check.py` + `form_*.csv` + sheet.

1. **Snap yok, butun senaryolarda (yeni duzensiz senaryolar dahil):** kol en fazla 20 derece/kare, hiz degisimi 6; uyluk 20/6; diz 26/8; dirsek 15/5; kalca 5 cm/kare, 2. Tutmayan satiri sayiyla yaz.
2. **Yeni senaryolar:** `rough` (sirali, aralik 0.30 +-0.10, tus 0.05-0.18), `hopRough` (tek tus, 0.27 +-0.08), `swap` (tek tus ile sirali arasinda degisen). Hepsi 1. maddeden gecer.
3. **Kayma yok:** ayak ucu yerden 6 cm icindeyken dunya z'si kare basina 2 cm'den cok oynayan kare sayisi (`skim`) tek tusta 0'a yakin; basan ayak temas boyunca en fazla 2 cm oynar. Ayak bir yere inerse GORULDUGU yere iner, isinlanmaz.
4. **Tek tus gercek ziplama (same020/025/030, hopRough):** govde `trunkLean` −25..−30 (su an −15). One cikan bacak yukarida: ayak ucu yerdeyken bile >= 35 cm, uyluk ~55-70 derece, sallanir. Basan bacak: itiste uyluk −30..−45, diz >= 165; sonra topuk kalcaya (diz <= ~80), diz one (+40..+55), iniste acilir. Kalca yayi temas seviyesinin en az 20 cm ustune, tek tepe; iki ayak yerden kesik kare payi >= %60; temas >= 4 kare.
5. **Kollar:** tek tusta iki kol en az −65..+85, dirsek 60-120 arasi canli degisir; sag sol koşuda en az −65..+70, dirsek 60-120; omuz ve govde burgusu daha genis (kalca burgusu ~25+, gogus karsi yonde). Kolun onu hep bacaga bagli gorunur, kutuk yok.
6. **Kosu esnek:** rhythm026 ve perfect'te uyluk en az −60..+70, topuk kalkisi daha yuksek (diz gecis <= ~35), kalca dalgasi ve gogus/bas tepkisi gorunur; kosu dongusu bozulmaz, dizler kalkar.
7. **Zemin korunur:** butun senaryolarda ayak ucu ve bilek dinlenme yuksekliginin en fazla 0.5 cm altina inmez.
8. Tutma, dalis, yere yerlesme: davranis ayni kalir, gecislerinde snap yok. Sim ve iliski testleri degismez, 23/23 + yeni senaryolar yesil. Yeni Inspector alaninin Tooltip'i var.

## Kapsam disi
- `RunnerSim.cs`, prefab, sahne.

## Calisma notu
- Mimari: `Track` (sonumlu yay + hiz ve ivme siniri) serbest bacagin uyluk/dizini ve kolun salinim/dirsegini izler; faz kodu yalniz hedef uretir; yerdeki bacak toe-off'ta acisini ve hizini devreder. Kol hedefi `_armKick` ani degil.
- Kok sebepler: (1) kol 95 derece: hedef anlik; (2) diz/ayak sicramalari: kilit, ayak havadayken (Catch bitti / Reach-Land'dan ani basis) yere koyuyordu; artik gorulerek temas bekleniyor (CatchGrace) ve havadaki ayak yakalaniyor; (3) trunkLean -15: pelvisPitch (+13 poz) backLean'e ekleniyor, hopBackLean 42 ile -26.
- Olcum t117f (5 tur): kol 17/4, dirsek 13/4 (same*), tek tus uyluk 17/25, diz 26/30, mash012 uyluk 13/10 diz 18/20, rough 27/17 36/39, swap diz 32/57. Tutmayan: perfect/rhythm026 diz hiz degisimi 36-52 ve uyluk 28 (planted solve, takipci disinda), swap bilek z 85, kalca 7-10 cm/kare (tek tus), tek tusta basan ayak temas 2 kare (kabul >=4) ve itiste uyluk -30..-45 yok (+10): 0.25 sn dongude buyuk yay ile temas kisaliyor. trunkLean -26, kalca yayi 25 cm, dirsek 64-100 tek tus.
