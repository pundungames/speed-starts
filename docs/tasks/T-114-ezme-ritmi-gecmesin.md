---
id: T-114
title: Hizli ezme iyi ritmi geciyor - her ikinci basis zamaninda, aradaki bosa basisin bedeli yok
status: merged
owner: gameplay-agent
depends_on: [T-113]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
  - "scripts/runner-olcum/Program.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [affcc010, cb1763bf]
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-05
finished: 2026-11-05
blocked_reason:
---

## Hedef
T-113 (`dd3b6fe0`) ilk kez derlendi ve olculdu (05.11). Iki iliski testi kirmizi: `LightFastMashIsSlowerThanTheRhythm`, `SustainedMashingKeepsMovingAndIsSlowerThanASlowRhythm`. Kullanicinin istedigi: "surekli sag sol spamlersem ilerlesin yine de ama kor topal". Olculen: 0.12-0.16 sn aralikla ezen oyuncu iyi ritim kadar hizli kosuyor.

## Olcum (prefab degerleri, `v10` = 10. sn hizi, `bas10` = 10. sn baş acisi)
| aralik/tus | T-113: 100 m, v10, bas10 | IKTest: 100 m, v10 | T-113 + wasteKickShare 3.5: 100 m, v10 |
|---|---|---|---|
| 0.11/0.04 | >30, 3.1, 80 | >30, 0.0 | >30, 3.1 |
| 0.12/0.04 | 11.14, 10.7, 8 | >30, 0.0 | >30, 3.1 |
| 0.13/0.04 | **10.49**, 11.5, 0 | 14.90, 7.9 | 16.63, 6.8 |
| 0.14/0.04 | **10.58**, 11.0, -3 | 13.03, 8.9 | 13.55, 8.4 |
| 0.16/0.04 | 11.51, 10.3, -7 | 13.03, 9.2 | 12.99, 9.0 |
| 0.18/0.04 | 13.59, 8.6, -6 | 14.93, 7.6 | 15.10, 7.3 |
| 0.20/0.04 | 17.13, 7.0, 27 | 15.45, 7.8 | 23.79, 5.5 |
| 0.13/0.07 | 29.65, 3.1, 80 | >30, 0.0 | >30, 3.1 |
| 0.14/0.07 | 14.55, 7.2, 44 | >30, 0.0 | >30, 3.0 |
| 0.15/0.07 | 12.26, 9.5, 21 | >30, 0.0 | >30, 3.1 |
| 0.16/0.07 | 12.15, 9.7, 9 | >30, 0.2 | >30, 3.1 |
| 0.20/0.07 | 19.39, 6.5, 32 | 20.33, 5.6 | >30, 2.9 |
| 0.22/0.07 | 12.23, 12.3, -10 | 14.10, 8.8 | 17.45, 6.2 |

Iyi satirlar (uc surumde ayni, korunacak): 0.24 = 10.34, 0.26 = 10.62, 0.28 = 11.13, 0.30 = 11.80, 0.40 = 17.56, 0.50 = 21.25 (v10 5.2).

Tablo araci: `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\ee3ca720-a79f-4ec7-83c8-e8fd75af0772\scratchpad\spam\` (`Spam.cs`, `spam.csproj`); `scripts/runner-olcum/Program.cs`'e `spam` komutu olarak tasi.

## Teshis (olculdu, kodla dogrula)
- 0.13 aralikta her ikinci basis yere tam denk gelir (`good%` 50). `kickTiming` ~1 olan basis `strainRelief` 0.2 ile strain'i siler, bosa basis `strainGain` 0.045 ekler: net eksi, strain 0 kalir, bas dik kalir.
- `wasteKickShare` 3.5 -> 0.5 bosa basisin bas vurusunu da aldi: IKTest'te bedeli bu vurustu. Tek bedel `pressFullGap` (0.15) `pushScale`: 0.13'te yalniz 0.87.
- Tuş suresi de belirleyici: ayni aralikta 0.04 ile 0.07 cok farkli; gercek dokunus ~0.05-0.08.
- 0.11 -> 0.12 (tus 0.04) 3.1 -> 10.7 m/s: T-111'de kapatilan turden hiz ucurumu.
- **`wasteKickShare`'i 3.5'e dondurmek cozum degil** (son sutun): 0.14-0.20/0.07 aninda 3 m/s, 0.22 -> 0.24 ucurum (17.45 -> 10.34), yavas egilme gider.

## Onerilen yon (olc, curutursa baska yol; kok sebebi notuna yaz)
Zamaninda basisin rahatlatmasi "son zamanlarda bosa basis yoktu" sartina baglansin, esiksiz: orn. `strainRelief`i `_form` (basis zamanlamasinin hareketli ortalamasi, cift basista ~0.5) kuvvetine ya da onceki basisa gecen sureye baglamak. Video da boyle: fazla basista hiz ~6 sn en iyi halinde kalir, sonra govde yatar, hiz duser.

## Kabul
- [ ] Aralik 0.08-0.16, tus 0.04 ve 0.07: 10 sn surekli ezmede hiz 2.5 m/s ile ritim 0.50'nin v10'u arasinda; 100 m >= 13 sn (13 varsayimim, kullanici bakinca degisebilir).
- [ ] Aralik 0.18-0.22: ritim 0.30'dan yavas (100 m > 11.8), 0.16 ile 0.24 arasinda kalir.
- [ ] Ucurum yok: 0.08-0.24 arasinda 0.01 komsu araliklarda v10 orani <= 1.6 (tus 0.04, 0.07). Tutmayan cifti tabloyla notuna yaz.
- [ ] Yavas egilme: 0.10-0.14'te bas 30 derece ~2 sn, 60 derece ~4-5 sn, 80 derece ~6-7 sn (+-1.5 sn); 1 sn altinda yatma yok. Ilk 3 sn hiz kaybi en fazla %25.
- [ ] Ritme donus: 0.28'e donunce bas ~2 sn'de 15 derece altina (`phased` komutu).
- [ ] Iyi satirlar +-%2: en iyi (`Perfect`), 0.24-0.30, 0.40, 0.50 (tus 0.04 ve 0.07); tek tus 0.20-0.40; iki tus; tutma ve dalis ayni.
- [ ] `RunnerSimRelationTests`: `SustainedMashing...` 0.08-0.16 ve iki tus suresini de kapsar; assert gevsetilmez; sabit denge sayisi assert edilmez. `MashingShowsMoreHeadThanPressingEarlyThanARhythm` kirilgandi (40'a 38): anlami dogruysa marjini kontrol et, yanlissa duzelt.
- [ ] Yeni Inspector alaninin Tooltip'i var; ozel durum, mod, esik yok.

## Kapsam disi
- `RunnerPoseDriver.cs` (T-115 ayni dalda oraya yaziyor). Prefab, sahne. Unity/batchmode/verify calistirma: supervizor olcer.

## Calisma notu
- Kok sebep: ezmede her ikinci basis ziplayarak zamaninda dusuyor (mukemmel, kickTiming ~1); `strainRelief` 0.2 bu basisla strain'i siliyor, aradaki bosa basis 0.045 ekliyor -> strain 0. Cozum: rahatlatma `Form^5` ile carpilir (son zamanlarda zamaninda basis yoksa az rahatlatir), ekleme `(1-timing)^2 x pushScale^2` (hizli basis saniyede daha cok biriktirmez), `strainGain` 0.09, `strainHead` 1.3, `wasteKickShare` 0.25. Yeni alanlar: `strainReliefFormPower`, `strainWastePower` (Tooltip'li).
- Tutmayan: 0.22 -> 0.23 v10 3.0 -> 12.4 (oran 4.1, tus 0.04 ve 0.07): 0.22 durgundan kalkan kosucu icin tutulamayan kadans (adim suresi 0.255), 0.23'te tutulabiliyor; iki dengeli durum, strain/relief ayariyla yumusamadi (grid: ~400 kombinasyon). Ayrica 0.10-0.11'de bas 30/60/80 cok erken (0.9/1.8/2.4 sn); 0.12-0.14'te 2.8/5.2/6.8 sn.
- Testler Unity'de KOSULMADI (simprobe ile ayni degerler elle denetlendi). `NeighbouringMashGapsDoNotJumpInSpeed` 0.22'ye kadar; `MashingShowsMoreHead...` anlami degisti (0.20 artik ezme kadar kotu): `MashingAndPressingEarly...`. Arac: `simprobe spam|scan|trace|grid` (SIM_SET/GRID).
- Ikinci tur: strain artik `kickTiming`'den degil basisin erkenligi (inise kalan sure / StepTime) ile, doyan egriyle (`strainWastePower` 8, `strainWasteHalf` 0.23); 0.22 yine 12.2 sn, 0.08-0.24 komsu v10 orani <= 1.6 (iki tus suresi), ezme 3 m/s'de. Kick payi 0.03: eski payla 0.07 tusta kick tek basina egimi cok hizlandiriyordu; bedeli: kosudan tutmada bas ilk 1 sn'de en fazla 6 derece geri (duruştan 2 derece), hiz ayni.
