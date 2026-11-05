---
id: T-115
title: Ayak ucu zemine giriyor (8/15 senaryo), tek tusta kol geri gitmiyor, kalca sicramasi
status: merged
owner: gameplay-agent
depends_on: [T-113]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [3166f444]
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
T-113'un form olcumu (05.11, `D:\Pundun\form-probe\t113\`, CSV'de kare basina kemik degerleri, 60 kare/sn). Kullanicinin sozu: "tek ayak kosarken karakter zeminin icine giriyor, kol cok az hareket ediyor, sanki kendini ziplatiyormus hissi vermiyor, kollar daha canli olsun". T-113'un zemin kirpmasi yalniz `SitBack` sonrasini kapatti (`same025` temiz); baska fazlarda ayak ucu hala giriyor.

## Olcum: zemin (dinlenme = t=0'da +1.0 cm; kabul sinir +0.5 cm)
| senaryo | en dusuk ayak ucu | t | faz | uyluk / diz | bilekY | dinlenme-0.5 alti kare |
|---|---|---|---|---|---|---|
| holdOne, holdBoth | −4.0 cm | 2.18 | Mid | 20.5 / 107 | 12.8 | 2 |
| tap016 | −3.5 cm | 4.65 | Hold | −16.5 / 171 | 12.8 | 8-10 |
| tap012 | −3.2 cm | 4.30 | Hold | −17.9 / 165 | 12.8 | 8-9 |
| human | −3.0 cm | 5.97 | Hold | −10.1 / 154 | 13.1 | 1-3 |
| late050 | −2.2 cm | 0.47 | Mid | 20.5 / 107 | 14.7 | 2 |
| mash012 | −1.8 cm | 0.27 | Push | 10 / 135 | 12.8 | 2 |
| same030 | −1.2 cm | 0.75 | Recover | −3 / 141 | 15.3 | 3 |
| both030 | −1.0 cm | 0.33 | Push | 10.8 / 140 | 12.8 | 1 |
Temiz: perfect, rhythm026, lane_*, same025; same020 tam sinirda (+0.4).

Ipucu: bilek cogunlukla tam dinlenme yuksekliginde (12.8), yani **bilek yere oturmus, ayak burnu asagi donuk**. `AnkleGround` = `_ground + HeelLift + ToeRise`, `Update`'te kullaniliyor; ayagin son acisi `LateUpdate` (~satir 2283-2292: `footPitch`, `bend`). Hipotez: kirpma ayagin son perdesini gormuyor. Dogrula, baska sebep ciktiysa onu duzelt. Ders (`docs/TUZAKLAR.md`): "gövdeyi hareket ettiren her adımın SONUNDA kırp".

## Olcum: tek tus
| senaryo | kol (min..max), hedef ~−45..+60 | basilan uyluk min, `hopPushEnd` −30 | kalca artis cm/kare, hedef <= 3 |
|---|---|---|---|
| same020 | −10..+56 | −17 | 4.8 |
| same025 | −18..+54 | −18 | 4.5 |
| same030 | −23..+52 | −15 | 2.8 |
Kol: `_armKick` `armHopReturn` 6/sn ile iner; 0.25 sn'de e^(−1.5) = 0.22 kalir, kol −45'e varmaz. Hizli cikis, yumusak donus korunarak geri gidis tamamlanmali.

## Kabul
- [ ] Butun senaryolarda, t=0'dan, ayak ucu ve bilek dinlenme yuksekliginin en fazla 0.5 cm altina iner (`L_toeY`, `R_toeY`, `L_ankleY`, `R_ankleY`). Temiz senaryolar bozulmaz.
- [ ] Kirpma ayagi "yere bastirmaz": yerde basan ayak kaymaz (`footSlide` kotulesmez), diz patlamaz, kalca sicramaz (run senaryolarinda artis/dusus cm/kare mevcut degerin ustune cikmaz).
- [ ] Kollar: 0.20, 0.25, 0.30 aralikta tek tus kol araligi yaklasik −35..+55 ya da daha genis, dirsek ~90; basisla ani cikis, yumusak donus.
- [ ] Tek tus: basilan bacak geride genis (uyluk yaklasik −25 ve otesi, ulasilamiyorsa neden oldugunu yaz), obur ayak yerden en fazla ~5 cm; kalca artis <= 3 cm/kare, kutu kosusu ve yuruyus hissi yok.
- [ ] Iyi kosu pozu degismez: perfect, rhythm026 satirlari (`trunkLeanAvg`, `passKnee`, `hipsYawRange`, kol araligi) +-%5. Hiz ve zamanlama hic degismez (gorsel taraf hizi degistirmez).
- [ ] Yeni Inspector alaninin Tooltip'i var; ozel durum, mod yok.

## Kapsam disi
- `RunnerSim.cs` (T-114 ayni dalda oraya yaziyor). Prefab, sahne. Unity/batchmode/verify calistirma: supervizor olcer.

## Calisma notu
- Ayak: kok sebep `leg.ankle.y` tabaninin (Update ~1595) yalniz HeelLift'i bilmesi; bosta (Hold/swing) ayak PoseAnkle'da burnu asagi donuyor ve top 4 cm giriyor. `FreeFootRise` PoseAnkle'in gercek perdesini (kaval acisi, kirpilmis bukum, swing karisimi) tahmin edip ayak bilegini yukseltiyor; SitBack ve kalca limitinden SONRA da uygulanir (yalniz !grounded && !carry).
- Kol: `_armKick` ustel dusus yerine basistan beri gecen zamanin SmoothStep'i; dusus suresi min(1/armHopReturn, 0.85 x son basis araligi).
- Kalca: `LimitHipRise` (yeni `hipRiseMax` 1.75 m/s) SitBack sonrasi nihai kalca yuksekligini sinirlar; `hopDip` 0.05 tek tus itisinde pelvisi cokertir ki bacak -25'e ulasabilsin (belirsiz, olcumle dogrulanmali).
- Olculmedi (Unity yok); emin olmadigim: ayak tahmininin dogrulugu, hopDip'in gorunumu, hipRiseMax'in mash/human satirlarina etkisi.
- Supervizor olcumu 05.11 (`D:\Pundun\form-probe\t115\`, commit 3166f444, sim T-113): derleme temiz. Ayak ucu her senaryoda >= +0.6 cm. Tek tus kol −42..+56, kalca artis <= 2.9 cm/kare, uyluk −21 (hedef ~−25, kismen). same020 kalca dusus 2.9 -> 3.5 cm/kare (`hopDip`, tek tus; run satirlari degismedi). perfect, rhythm026 birebir ayni. Kalan 2 kirmizi test T-114'un.
