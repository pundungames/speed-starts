---
id: T-118
title: Kosuda yerdeki bacak teklemesi, tek ayakta gercek itis, gecis sicramalari
status: review
owner: gameplay-agent
depends_on: [T-117]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
touches_scene: false
branch: agent/ik-kosucu
commits: [4eee73f3, 9cdf8069, a7e8da06, 07d3b5c4, ea0383b2]
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
Kullanici: "sen yapmaya devam et, yaptikca gecir, ben bakayim". T-117'nin (`38166c97`) kalanlari, oncelik sirasiyla. Her biri ayri commit, biter bitmez gecer. Unity testi ve olcum yok; veri olarak `D:\Pundun\form-probe\t117f\` okunur.

## Isler
0. **Lineer hareket:** kullanici "hareketler lineer oldugu icin cansiz, tween ekle". `Track` hiz sinirina yapisip sabit hizla gidiyor (kol 17 derece/kare = `armMaxSpeed`). DOTween degil (her basista yeniden baslamak reset olur): takipcide can egrisi hiz, hafif tasma, zincir gecikmesi (uyluk -> diz -> ayak, omuz -> dirsek -> el).
1. **Kosuda tekleme:** yerdeki bacagin dizi basista bir karede 36-52 derece hiz degistiriyor, uyluk 28 derece/kare (perfect, rhythm026). Sebep: basan ayak inince gosterilen bacak aniden stance cozumune geciyor; kalkista tersi. Inis pozu ile ilk stance cozumu, son stance cozumu ile salinimin ilk hali ayni aci ve hizda bulusmali. Ana kural: input uzuv resetlemez.
2. **Tek ayakta gercek itis:** su an ayak 2-3 kare yerde, uyluk +9'da kalkiyor, bacak geriye uzanmiyor. Yon: ayak basista degil, govde inince yere degsin (gorunen temas); basis geldiginde zaten yerdedir; govde ilerledikce uyluk geriye gider, itis bacagi −25 ve otesinde, diz acik kalkar. Kalca yayi 20 cm civarinda kalsin, temas ~5-7 kare (0.25 sn aralik). Gec gelen basista ayak yerde bekler, kaymaz.
3. **Gecis:** tek tus ile sirali basis arasinda (`swap`) ayak bir karede 85 cm atliyor (`form_swap.csv`). Geciste de snap yok.
4. **Abarti:** on ayak havada 70 cm: basan ayagin tepesi ve denge bacaginin yayi makul olsun (ayak ucu ~55 cm ust sinir, yerdeyken >= 35). Kosuda el yuze ve sapkaya degiyor: salinim genis kalsin, tepede dirsek biraz acilsin, el cene hizasini gecmesin.
5. **Ezme, iki tus:** `mash012` −31 cm, `both030` −18 cm ayak atlamalari; aynilarsa kapat.

## Kapsam disi
- `RunnerSim.cs`, prefab, sahne. Unity, batchmode, verify, probe.

## Calisma notu
- Tween istegi (9cdf8069): Track hiz siniri platosuydu (kol 17 derece/kare = armMaxSpeed). Limitler 1800/1700, sonum 0.8, ikinci halka (diz, dirsek) limbLag 8.5 Hz ile geriden izler.
- 1 (4eee73f3): SolveStance toe-off'u bir kare onceden alir (dNext), gerilmis bacagin bir karede 25/30 derece donmesi bitti.
- 2 (a7e8da06): tek tusta swing hopLead (0.05 s) erken biter, inen ayak yerinde civilenir (HopPlant 0.5), basis yalniz stance'i uzatir. Esik hop agirligi > 0.5 (surekli degil).
- 3 (07d3b5c4): Enter() yerden ayrilan her bacagi gosterilen aciyla baslatir (ToHold/Reach dahil).
- 4 (ea0383b2): el cene: armRunFront 62, armElbowFront -20, hop dirsegi tepede acik; balanceLeg 62/148.
- 5: ezme/iki tus icin ayri kod yok; t117f'te mash012 bilek z 25 cm, ayni kok (havadaki ayagin planti) kapatilmis olmali. Olculmedi; emin degilim: hopLead/plant zamanlamasi, yeni itis derinligi (-25), balance bacagi yerdeyken >=35 cm (hip yayi yuzunden ~17 cm kalabilir).
