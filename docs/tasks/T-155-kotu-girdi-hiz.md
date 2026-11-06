---
id: T-155
title: Tek tusta kotu girdi hizi cok dusurmesin, basis gelirken hiz sifirlanmasin
status: review
owner: gameplay-agent
depends_on: [T-153]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerSim.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
  - "scripts/runner-olcum/Program.cs"
  - "docs/tasks/T-155-kotu-girdi-hiz.md"
touches_scene: false
branch: agent/ik-kosucu
commits: []
tests:
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerSimRelationTests.cs"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11: "tekli kosular kotu input vermeler karakteri cok yavaslatiyor. input verdigimiz takdirde hic bir sekilde hiz tam olarak sifir olmaz ve cok da yavaslamamali." Bu turda kullanici bu dengeyi acikca istedi.

## Olculen bugunku hal (sim, prefab degerleriyle, tek tus, ayaktan)
| Aralik sn | 100 m | Oturmus hiz min / ort |
|---|---|---|
| 0.05 | 31.8 | 3.1 / 3.3 (bas 73) |
| 0.08 | 31.8 | 3.1 / 3.4 (bas 65) |
| 0.10 | 27.1 | 4.4 / 4.9 |
| 0.12..0.25 | ~17 | 4.4 / 5.9 (en iyi 0.25: 16.85, 6.0) |
| 0.30 | 19.1 | 3.7 / 5.2 |
| 0.35 | 23.5 | 3.1 / 4.1 |
| 0.40 | 31.2 | 2.9 / 3.2 |
| 0.50 | 34.6 | 2.4 / 3.0 |
| 0.70 | 36.2 | 1.7 / 2.6 |
| 0.80 | bitmiyor | 0.7 / 1.3 |
| 1.00 | bitmiyor | **0.0** / 0.8 |

## Sebepler (koddan)
- `Press`: yerde tekrar basis "gec" sayilir (`lateCurve`), sonra `sameLegFactor` 0.4 ile de carpilir. Sekiste yerde olmak gec kalmak degil.
- `eff *= HeadUpright` ve `headDrag`: spam'de bas 65-80. Gorselde sekiste bas zaten gosterilmiyor.
- `tapFloorSpeed` 3.2, `tapFloorGain` 0.6: kosu icin de gecerli; bir basis 0.6'dan fazla kurtarmaz.
- `stopBrakeForce` 12, `stopDelay` 0.6, `stopRamp` 0.3: araligi 0.65+ olan seride fren basislar arasinda calisir, hizi sifira indirir. 1 sn araliginda `repeat` de dusuyor (`sinceLast < 1f`).

## Istenen tablo (`gateprobe hop`)
- Tek tus, aralik <= 0.70, sapmali satirlar dahil: oturmus ort >= 4.5 m/s, min >= 3.8.
- Spam (0.05, 0.08, 0.10): en iyi tek tusun %85'inden az degil.
- Aralik 1.0 ve 1.4: hiz **sifira inmez**, min >= 1.5.
- En iyi tek tus 16 sn'den hizli olmaz (iki tusun onune gecmesin, bugun 16.85).
- Tek basis (`oneTap`, seri yok) yine durur, bugunku gibi.
- Basis kesilince (seri sonrasi) karakter durur: fren en gec `stopWaitMax` (ornek 1.6 sn) sonra gelir.

## Bozulmayacak (iki tus, sayisi sayisina)
`gateprobe gate` ve `gateprobe human` ciktisi ayni kalir (ilk satirlari: en iyi 9.78; ritim 0.24 10.33; 0.28 11.13; 0.32 12.62; iki tus mash sutunlari). Kosudan birakinca durma (ritim <= 0.40) ayni. `RunnerSimRelationTests` yesil kalir (`HopBiasChangesNoSpeed`: `_hopBias` hiza bagli olamaz; `TappingOneKeyFasterThanHopFullGap...`: hizli basis `hopFullGap`'ten hizli olamaz).

## Yol onerisi (daha iyisini bulursan onu yap, ozel durum az olsun)
1. `repeat` basis gec sayilmaz: `repeat && (Airborne || launchPending)` dali yalniz `repeat`.
2. `repeat` basis basi firlatmaz ve bas onu bozmaz (`_hopBias` degil, basisin kendi `repeat`'i; `headDrag` ve tutma itisi icin spam olcusune bak).
3. Yeni `hopFloorSpeed` + `hopFloorGain`: `repeat` basista `tapFloor` mantigi, iki tus etkilenmez.
4. Fren beklemesi ritme bagli: `max(stopDelay, min(stopWaitMax, stopWaitShare x son iki basis araligi))`. Sonraki basisin vakti gecmeden fren yok; ilk basista son aralik yok, `stopDelay` gecerli. Ritim <= 0.40'ta bu `stopDelay`'in altinda kalmali (iki tus ayni).
5. `repeat` penceresi `sinceLast < 1f`: tabloyu tutturmak icin gerekiyorsa sabit yerine alan.

## Araclar
Sim'i Unity'siz olcer. Klasor: `C:\Users\Monster\AppData\Local\Temp\claude\d--Pundun-speed-starts\1c7d0dba-289b-4948-ba09-42f88d086602\scratchpad\gateprobe`. `D:\Pundun\speed-starts-ik` icindeki `RunnerSim.cs`'i derler.
`dotnet build -c Release -v q` sonra `bin/Release/net10.0/gateprobe.exe hop`, `gate`, `human`. Deneme: `SIM_SET="hopFloorSpeed=4.5"` ortam degiskeni float alanlari ezer. Prefab degerleri `Program.cs` `T()` icinde (maxSpeed 13, pushGain 1.22, dragQuadratic 0.032, gravity 5, headKick 12, headReturnDelay 0.1). `HopTable` metodunu `scripts/runner-olcum/Program.cs`'e de ekle.

## Kurallar
- Yalniz `owns_paths`. `RunnerPoseDriver.cs` baska agent'ta (T-154): oraya yazma. Pozun bu fren beklemesini okumasi gerekiyorsa task notuna yaz.
- Prefab'ta kayitli alanlar kodu ezer: `stopBrake` (kullanilmiyor), `stopDelay`, `stopRamp`, `sameLegFactor`, `pushGain`, `maxSpeed`, `headDrag`. Bunlarin sayisini degistirme, yeni alan ac. Her alanda Tooltip.
- Unity calistirma; testleri supervizor kosar. EditMode degil, bu test dosyasi PlayMode klasorunde (reflection ile sim).
- Test sabit denge sayisi assert etmez, iliski assert eder: basis gelirken hiz sifira inmez, kotu aralik en iyinin belli bir payindan az degil, iki tus ritmi tek tustan hizli.
- `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasinin adinda "main" yok, son satir `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. Calisma yeri `D:\Pundun\speed-starts-ik`, dal `agent/ik-kosucu`. Baska agent ayni dalda `RunnerPoseDriver.cs` yaziyor: `git add -A` yok, commit yola sinirli; `index.lock` gorursen bekle ve tekrar dene.

## Kabul kriterleri
- [ ] Istenen tablonun her satiri.
- [ ] `gate` ve `human` ciktisi birebir ayni (once ve sonra yapistir).
- [ ] Test eklendi, yeni alanlarda Tooltip.

## Calisma notu

Once/sonra (tek tus, ayaktan, 100 m sn / oturmus hiz min ort):

| Aralik | Once | Sonra |
|---|---|---|
| 0.05 | 31.8 / 3.1 3.3 | 18.9 / 5.4 5.5 |
| 0.08 | 31.8 / 3.1 3.4 | 19.1 / 5.4 5.5 |
| 0.10 | 27.1 / 4.4 4.9 | 18.0 / 5.6 6.0 |
| 0.25 (en iyi) | 16.85 / 4.4 6.0 | 16.35 / 5.3 6.4 |
| 0.40 | 31.2 / 2.9 3.2 | 20.2 / 4.7 5.3 |
| 0.50 | 34.6 / 2.4 3.0 | 20.3 / 4.9 5.2 |
| 0.70 | 36.2 / 1.7 2.6 | 20.9 / 4.7 5.1 |
| 1.00 | bitmiyor / 0.0 0.8 | 21.9 / 4.0 4.9 |
| 1.40 | bitmiyor | 23.0 / 2.9 4.6 |
| 0.5+-0.3 sapmali | 33.0 / 1.8 3.1 | 20.4 / 4.3 5.2 |

`gateprobe gate` iki tus sutunlari ve `human` ciktisi birebir ayni (tek tus sutunlari ve ilk satirlar degisti, beklenen). Tek basis ve ritim <= 0.40 durma suresi ayni (`gateprobe stop`).

- Yeni alanlar (`RunnerTuning`): `hopWindow` 1.6 (eski sabit 1 sn), `hopSeriesAfter` 1, `hopSlowGap` 0.6, `hopFloorSpeed` 5.6, `hopFloorGain` 2.8 (m/s per sn aralik), `stopWaitShare` 1.5, `stopWaitMax` 1.6.
- Mantik: "seri" = ayni tus ust uste tekrar (ya da 0.6 sn+ aralikli tekrar); seride gec basis ve `HeadUpright` basisi bozmaz, `hopFloor` tabani uygulanir (tek tusa ozgu; iki tusun arada bir cift basisi seri sayilmaz, bu yuzden `human` ayni kaldi). Taban basisi aralikla orantili: en iyi tempoda ek yok, yavasta surtunmenin aldigini geri verir. Fren beklemesi son iki aralikla uzar (`stopWait*`), ilk basista `stopDelay`.
- `hopBias` hiza hala bagli degil (`_repeatRun` ayri sayac). Zor kisim: `human` tek erken basis satiri, probe'un basili kalan bacagi yuzunden iki tusta da "tekrar" uretiyor; seri esigi bunu disarida birakmak icin.
- Pozun fren beklemesini okumasi gerekirse: bekleme `wait = max(stopDelay, min(stopWaitMax, stopWaitShare x max(son iki aralik)))`; sim'de kamuya acik degil (T-154 notu).
- Koşulmadi: Unity testleri (3 yeni test `RunnerSimRelationTests` sonunda). Probe: `scripts/runner-olcum/Program.cs` `hop`, `stop`.
