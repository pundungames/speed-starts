---
id: T-116
title: Tek tus - dizde cokme, ayak zeminde kayiyor, ziplamasi lazim
status: review
owner: gameplay-agent
depends_on: [T-115]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [27bab4fa, 0ccd83c6]
tests:
  - "gerekmiyor: gorsel; supervizor form olcumuyle (RunnerFormProbeTests) dogrular"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
Kullanici 05.11'de `IKTest`'i (`cb1763bf`) oynadi: "tek ayakla giderken karakter dizinde asagiya cokuyor, sonra ayak zeminde kayiyor. ziplamasi lazim". Bu cumle T-113'teki "topuk az kalkar, obur ayak en fazla ~5 cm" kabulunun onundedir: tek tus gorunur bir sicrama olmali. Hiz ve zamanlama sim'de kalir, sim'e dokunulmaz.

## Olcum (`D:\Pundun\form-probe\t114\form_same025.csv`, 60 kare/sn, aralik 0.25 sn, hiz 5.7 m/s)
Bir dongu, basilan bacak L; R denge bacagi:

| t | olay | kalca | diz | ayak ucu | ayak dunya z |
|---|---|---|---|---|---|
| 4.25 | havada, inise yakin | 0.980 | 164 | 30.9 | 16.72 |
| 4.27 | basis (Push) | 0.973 | 164 | 25.5 | 16.81 |
| 4.28 | yere iniyor | 0.964 | 153 | 9.5 | **16.48 (−33 cm)** |
| 4.32 | yerde, cokme | 0.931 | 148 | 6.3 | 16.51 |
| 4.35 | itis sonu | 0.953 | 156 | 6.8 | 16.52 |
| 4.37 | Recover, hala yerde | 0.961 | 156 | 3.1 | **16.80 (+28 cm)** |
| 4.40 | kalkti | 1.018 | 119 | 12.0 | 16.83 |
| 4.43-4.47 | havada, duz tepe | 1.01 | 109-129 | 14-21 | 17.33-17.78 |

Sayimlar (t >= 3 sn, 180 kare):
| senaryo | ayak bir karede geri, en kotu | ileri, en kotu |
|---|---|---|
| same020 | 34 kez, −36.7 cm | 9 kez, +48.9 cm |
| same025 | 16 kez, −34.9 cm | 2 kez, +39.4 cm |
| same030 | 15 kez, −35.3 cm | 5 kez, +38.1 cm |
| mash012 | L 12 kez −21.7 cm, R 12 kez −31.0 cm | 0 |
| perfect, rhythm026 | en fazla −6.6 cm | 0 |
| IKTest eski, same025 | 12 kez, −35.5 cm | 0 |

- Kalca her basista ~9 cm cokuyor (1.01 -> 0.93, ~5 karede), diz 164 -> 146. Bu taban T-113'te de var (0.932), `hopDip` (T-115) tek basina sebep degil ama yonu yanlis.
- Sim ucusu `hopY` tepe yalniz 1.5 cm; gorunen yukselme pose'tan (`hopRise` 0.05, T-113'te 0.1'den dusuruldu). Tepe duz (~5 kare 1.01), parabol degil. Kalcanin yukselisi itis bittikten sonra geliyor (`hipRiseMax` 2.9 cm/kare sinirinda), itis sirasinda degil.
- Basili bacagin ayak ucu yere hic oturmuyor: en dusuk 2.4 cm, bilek 14 cm (dinlenme 12.8). Denge bacaginin ayak ucu 24-43 cm havada.

## Hipotezler (kodla dogrula; curuyen varsa notuna yaz)
1. **Ayak atlamasi:** `SitBack` (−15 derece) yerdeki bacagi pivot alip havadaki bacagi donduruyor. Bacak `grounded` olunca ya da kalkinca ayak bu farki (~0.83 m x sin 15 ≈ 21 cm ve ustu) bir karede atliyor. Normal kosuda geri oturma yok, atlama da yok; ezmede on egim ayni izi birakiyor (`mash012`, kontrol et).
2. **Dongu:** `leg.cycle` = 2 x `StepTime` kosu adimina gore (ayni bacak iki adimda bir basar). Tek tusta ayni bacak her adimda basar, salinim bitmeden gelen basis `StartCatch`'e duser, ayak ~25 cm havadan iniyor.
3. **Cokme:** dizin 146'ya inmesi `stanceDip`, `hopDip` ve `hopPushEnd`e giden yolda kalca yuksekligi ihtiyacindan gelebilir.

## Kabul (tek tus: same020, same025, same030)
- [ ] Ayak kaymaz: yerdeyken ayak dunya z'si temas boyunca en fazla 2 cm oynar. Bir karede geri atlama en fazla ~6 cm (normal kosu duzeyi); ileri atlama, yere yakin ayakta yok.
- [ ] Cokme yok: yerde kalca temas anindaki yuksekliginden en fazla 3-4 cm iner, basan diz >= ~155. `hopDip` kaldirilir (ya da yerine gecen sey gerekcelenir).
- [ ] Ziplama gorunur: itis sirasinda kalca yukselmeye baslar, ayak yerden kesilince tek tepeli yay (duz tepe yok), kalca tepesi temas yuksekliginin en az 8 cm ustunde; tepede basan ayagin ucu >= 15 cm, denge ayagi da bedenle kalkar. Ayak ucu yerde gercekten oturur (en dusuk <= ~1.5 cm, dinlenme +1.0).
- [ ] Yerde gecen sure gorunur: en az ~4 kare (0.25 sn aralikta), 0.20 ve 0.30 aralikta da ayni karakter; aralik degisince ucurum yok.
- [ ] Denge bacagi onde, yere inmez; yerdeyken ayak ucu 5-15 cm civari (su an 24-30). Govde 10-20 derece geride kalir. Kollar T-115'teki gibi (−35..+55 ve genis).
- [ ] Uyluk geride genis (hedef yaklasik −25; cokme olmadan ulasilamiyorsa neden ve olculen deger).
- [ ] Zemin kabulu korunur: butun senaryolarda ayak ucu ve bilek dinlenme yuksekliginin en fazla 0.5 cm altina iner.
- [ ] Iyi kosu bozulmaz: perfect, rhythm026 satirlari +-%5, ayak atlamasi en fazla ~6 cm. Kalca artisi 3 cm/kare siniri tek tus disinda korunur; tek tusta ziplamanin gerektirdigi degisikligi notuna yaz. Hiz, zamanlama, `RunnerSim.cs` degismez.
- [ ] Yeni Inspector alaninin Tooltip'i var; ozel durum, mod yok; tek tus agirligi (`_hop`) surekli kalir.

## Bilgi amacli (ayni kok sebepse)
- `mash012` ayak atlamasi: duzeliyorsa soyle; ayri kok sebepse dokunma, olcumu notuna yaz.

## Kapsam disi
- Sim, prefab, sahne. Unity/batchmode/verify calistirma: supervizor olcer.

## Calisma notu
- H1 dogru (mekanizma): SitBack serbest bacagi kalca etrafinda donduruyor, basan bacak sabit; kilit donmemis noktaya konuyor, kalkista tersi. Serbest bacak (Hold ve dalis disinda) artik donmeden kalcaya asili; kilit sitShiftZ ile gorulen yere; kalkan bacak gosterilen aciyla basliyor (ShowPlantedAngles).
- H2 buyuk olcude curuk: ayni bacagin cycle'i aralik olcusuyle ~0.25; ayak yuksekligi H1'in y kaymasindan (kalca, SitBack) geliyor.
- H3: stanceDip tek tusta hop agirligiyla sonuyor, itis boyunca kalca yukseliyor (hopPushRise); hopDip kaldirildi. Uyluk -25 ulasilmadi/denenmedi: yukselen kalca bacagi daha erken gerer.
- Olculmedi (Unity yok).
- 2. tur (0ccd83c6): kok sebep: SitBack pivotu basan ayagin topugunda; gosterilen kalca ~20 cm geri gidiyor, SolveStance ise dönmemis kalcadan cozuyor. Gosterilen bacak (21 derece) ile kayitli (4.6) 17 derece ayrisiyor; itis ekranda geriye ulasmiyor, kalkista ayak oynuyor. same020 ayni kok sebep (ayri degil). Cozum: tek tusta pivotun topuk payi (1 - hop agirligi) ile sonuyor, govde kalcayla doner; hopContact uyluk 20 -> 10. Olculmedi.
- Supervizor olcumu 05.11 (`D:\Pundun\form-probe\t116b\`, `IKTest` 0ccd83c6; 23/23 hedefli yesil): kalca tek tepeli yay 12.4 cm, itiste yukseliyor; basan diz 156-161; temas boyunca ayak z 2 cm icinde; zemin temiz; kol -42..+54; govde -15; perfect, rhythm026, mash012, both030 satirlari birebir.
- Tutmayan: (1) kalkis karesinde ayak ucu 2.8 cm iken z +11 cm (1 kare), sonra havada -11/-8 (same020 -19.6). (2) itis uylugu +4.6'da bitiyor, diz 161 (hedef -15, 165); temas 3 kare (hedef 4). Sebep adayi: sim itisi ~0.06 sn, kalca da yukselince bacak uzunlugu erken bitiyor (Overstretch). Kullanicinin gozu bekleniyor, kor ucuncu tur yapilmadi. mash012 (-31 cm), both030 (-18 cm) ayak atlamalari ayri, degismedi.
