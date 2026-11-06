---
id: T-154
title: Tek ayak sekis: girdi kapisi, yukseklik, geri atis, sabit serbest bacak ve kol, ayaktan tek basista sekis
status: review
owner: gameplay-agent
depends_on: [T-153]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "Assets/Speed-Staes/Tests/PlayMode/RunnerFormProbeTests.cs"
  - "docs/tasks/T-154-tek-ayak-sekis.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [86473717, f51c8b3c, fc4367a9, 8608e6ce, 7692878a, 1b9018c4, 793ffc9b, cc3474d4, 0f13ed11, 3cbf6f03, a0e7f7e1, 9a6dc2ab, 5a500f03, 4afdece3, f009f0c8, 962294c1, 1c670498, 242e9207, 643591d7, da2611a3, 11dcd68f, 83cd12ce, d3a25e54, 47a36e6e, 3723e198, ae750e15]
tests:
  - "gerekmiyor: gorsel; olcum RunnerTransitions_WriteFrameStrips supervizorde"
analytics:
  - "yok: projede analytics yok"
needs: []
started: 2026-11-06
finished: 2026-11-06
blocked_reason:
---

## Hedef
Kullanici 06.11 (ozet, sol ayak uzerinde sekerken):
- Bacak arkaya cok az gidiyor, daha cok gitsin. Karakter Y'de daha cok ziplasin.
- Animasyon bitmeden tekrar input kabul edilmesin.
- Sol kol tamamen sabit ve kapali. Sag bacak dumduz ve sabit, onde. Belden ya da baska yerden oynama yok.
- Hareket eden kol bacakla senkron, geriye kendini smooth atar.
- Ust govde cok geriye gidiyor, daha az gitsin.
- Dururken tek tusa basinca normal sekme calissin: basilan ayak sekme yapar, diger ayak sekmedeki gibi onde durur. Bugun cok kucuk bir adim atip diger ayak hemen yanina geliyor.

## Olculen bugunku hal (`D:\Pundun\form-probe\t153-e`, hopSwap 0.27 sn)
| | Bugun | Sebep |
|---|---|---|
| Kalca Y araligi | 12 cm | `hopRise` 0.26 x (ucus / `hopRiseTime` 0.45)^1.5 |
| Sekis bacagi uyluk min | -18 | Adim kisa, Hermite `hopPushEnd` -45 ve `hopRecover` -35'e varamiyor |
| Serbest bacak uyluk | 50-85, diz 140 | `Balance()`: `balanceLift`, `hopSway` |
| Kalca egimi, govde | -31..-38, -30..-41 | `hopBackLean` 42 + `hopRock` 14 + `rideBob` 6 + `pelvisRockShare` |
| Kol | -67..59, iki kol da sallaniyor | `StepArm` hop dali, basis saatine bagli |
| Girdi kapisi | 0.27 ritminde %89 alindi | `_sameStreak >= SettledSame` kapiyi aciyor, `pressGateMax` 0.3 |

## Istenen
1. **Sert kapi.** Yeni alan `hopTime` (ornek 0.42 sn): bir sekis alindiktan sonra bu sure dolmadan hicbir basis pozda alinmaz (ayni tus da, sekis icindeki diger tus da). `_sameStreak` istisnasi kalkar. Sim yine her basisi sayar, fizik kapisi yok (T-150). Sekisin suresi de bundan kisa olmaz: `NoteCycle` ve salinim `_hop` agirligiyla `hopTime` altina inmez. Ritim 0.27 ise her ikinci basis alinir, spam'de sekis `hopTime` civarinda.
   Koslar etkilenmez: `_altStreak` ve kosu kapisi aynen. Sekisten kosuya gecis (hopToRun) calismaya devam eder.
2. **Yukseklik.** Sekis tam yayini yapar: settled hopSwap kalca araligi >= 0.25 m. Dikkat: T-151 kisa ritimde titredigi icin yuksekligi kismisti. Sert kapi ile sekis hep tam sure, tek tepe. `hopHipRiseMax`, `hopDropSpeed` gerekirse.
3. **Geri atis.** Sekis bacagi uyluk min <= -60, ayak kalcanin arkasinda ekranda >= 0.45 m, geri atista diz <= 90. `hopPushEnd`, `hopRecover` (prefab'ta kayitli degiller ama asagidaki tuzaga bak).
4. **Serbest bacak dumduz ve sabit.** `Balance()` sabit: yeni alan (ornek `hopFreeLeg` (55, 175)); `hopSway` ve `balanceLift` etkisi kalkar. Settled sekiste uyluk ve diz araligi <= 2 derece. Ayak yere degmez (>= 0.25 m).
5. **Govde daha az geri, oynama yok.** Yeni alan ile kalca egimi -10..-18 (bugun -35). Settled sekiste kalca egimi ve `trunkLean` araligi <= 2.5 derece: `hopRock`, `rideBob` sekiste etkisiz. Bel, gogus, boyun da oynamaz.
6. **Sekis tarafindaki kol sabit ve kapali.** Sekis ayagiyla ayni taraftaki kol: yeni alan (ornek `armHopFixed` swing, elbow), dirsek >= 110, swing ve dirsek araligi <= 2.5 derece. Kol govdeye gore tanimli (`PoseArm` parent'a gore) ve govde sabit oldugu icin ekranda da sabit.
7. **Hareket eden kol bacakla senkron.** Diger kol, basis saatine (`_armThrow`) degil sekis bacaginin gosterilen uyluguna bagli: bacak geride kol geride (swing <= -55, dirsek acik), bacak onde kol onde (dirsek kapali). Filtre var ama gecikme <= ~0.06 sn. Aralik >= 90. Kare farki <= 12 derece, ikinci fark <= 3 derece. Roller `opposite.idle` ile surekli degisir (hopSwap'te sicrama yok).
8. **Dururken tek basis.**
   - Basilan ayak sekis yapar (ayak >= 15 cm kalkar, kalca >= 15 cm yukselir), diger ayak serbest bacak gibi onde durur.
   - Oneri, kosu baslangicini bozmamak icin: ilk basista T-148 adimi aynen baslar (diz yukari, kosu ile ortak ilk bolum). `hopStandWait` (ornek 0.2 sn) icinde diger tus gelmezse `_hopTarget` 1 olur, ayni bacak oradan `Balance`'a acilir, basilan ayak sekise cikar. Diger tus gelirse hic bir sey degismez (kosu). Bu sira `start`, `walkStart`, `restart`, `blockStart`, `blockPair` senaryolarini aynen birakir. Daha iyi bir yol bulursan ayni kabulle sen sec.
   - Hic basis gelmezse serbest bacak `HopRepeatWindow` kadar onde kalir, sonra yumusak iner.
   - Ayni tus tekrar: sekis serisi (kapi gecerli). Diger tus: kosu.
   - Yavas hizda da (ilk basis ~1 m/s) sekis olur, ayak bacak boyuna takilmaz (sure ile kalkar).

## Kurallar ve tuzaklar
- Oku: `docs/TUZAKLAR.md`, `docs/tasks/T-147*`, `T-150*`, `T-151*`, `T-152*`.
- Input hic bir uzvu snap etmez. Yeni agirliklar yay ile; hedefler degisir, poz degil.
- Prefab: alan kodda varsayilani degisince prefab import'u eski sayiyi tutabilir (TUZAK). Sayisi degisen alan icin yeni ad ac; eski alan okunmuyorsa sil. Prefab ve sahneye dokunma. Her Inspector alaninda Tooltip.
- Duz kosu (`_hop` 0, `_pair` 0) sayilari degismez. Cift tus (`pair`) ayni. Tutma (`holdOne`, `holdBoth`) ve dalis ayni. Sim'e dokunma (`RunnerSim.cs` baska task'ta, T-155).
- Unity, batchmode, verify calistirma. Olcumu supervizor yapar. Her adim ayri commit: `git commit -F <mesaj-dosyasi> -- <yollar>`, mesaj dosyasinin adinda "main" gecmez, mesaj sonu `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Git'i Bash ile cagir. `git add -A`, push yok.
- Calisma yeri `D:\Pundun\speed-starts-ik`, dal `agent/ik-kosucu`. Yorumlar Ingilizce, "neden"i anlatan.

## Senaryolar
`RunnerFormProbeTests.TransitionScenarios`: `oneTap` 2.5 sn'ye uzar (tek basis, sonra bosta), yeni `standHops` (dururken ayni tus 0.45 ile 3 sn, ilk 1 sn serit), `spamHop`, `hopSwap`, `slowHop`, `hopToRun`, `runToHop`, `hopSwap` serit yerleri yeni sureye uyar.

## Kabul kriterleri
- [ ] Yukaridaki 1-8 sayilari `hopSwap`, `slowHop`, `spamHop`, `standHops`, `oneTap` kayitlarinda.
- [ ] Alinan basis araliklari sekiste hep >= `hopTime`.
- [ ] Kalca kare farki <= 6 cm, ikinci fark <= 2.5 cm, sekis basina tek tepe.
- [ ] `start`, `walkStart`, `blockStart`, `rhythm032`, `fastRun`, `spamRun`, `stop` onceki kayitla ayni (kalip, snap yok).
- [ ] Ekran seritleri: sekiste kol ve bacak istenen gibi.

## Calisma notu
- Yapildi: (1) `hopTime` sert kapi + `HopCycleFloor`, `_sameStreak` kalkti; (2) `hopFreeLeg`, `hopSitBack`, hopRock/rideBob/hopSway/balanceLift/balanceLeg/hopBackLean silindi; (3) kol: `armHopThigh`, `armHopFixed`; (4) `hopStanceMax`, `hopSwingKeys`, `hopRecover` (-68,85), `hopRise` 0.27, `hopRiseTime` 0.28; (5) `hopStandWait` ile ayaktan tek basis; (6) `oneTap`, `standHops`, `hopSwap` senaryolari.
- Derleyici yok, hicbir sey olculmedi. Sayilar tahmin; ince ayar olcumden sonra.
- Karar: `hopStandWait` 0.34 (oneri 0.2 degil): start/restart/blockStart tus araligi 0.30, 0.2'de ilk adim sekise donerdi. walkStart (0.6 sn aralik) kacinilmaz degisir: 0.34'te sekis, 0.6'da kosu.
- Blok baslangicinda (`startStance != None`) tek basis sekise donmez (kosu baslangici korunsun).
- Supheli: hopStanceMax kisa stance yuzunden yukseklik ve itis suresi degisir; `hopSitBack` 18 govde -10..-18'i tutturmak icin tahmin.
- Tur 2-4 (olcum t154-a/b/c): kapi havadayken kapali, stance dokunustan, hopStanceMin/Max, hopTimeMax, hopSwingMin, hopLean (mutlak), kol yayi (armHopFollow 4 Hz), hopStandWait 0.15 + _hopQuick.
- Tur 5: Key() artik Lerp(Stride(key), hopKey) (hop key'leri amp ile kuculmez); hopRecover (-58,110), hopKneeDrive (40,100); zamanli toe-off yalniz basis istemis ayakta (swungSincePress) - tek basis tek sekis.
- hopLean olculen -6.9 vs hedef -11: kaynagini bulamadim (kod: toplam egim = pelvisPitch + lean = hopLean), Tooltip'te yazili.

- Tur 6 (t154-d): ilk sekiste diger tus kapidan gecer (openOther), inis karesi yuksekligi kaldirmaz (_landHold), hopSwingMin 0.40, hopTime 0.46, armHopUp 42.
