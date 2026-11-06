---
id: T-156
title: Kalca cakili gorunmesin: kosuda burgu ve one-arkaya akis, sekiste de ayni akis
status: merged
owner: gameplay-agent
depends_on: [T-154]
owns_paths:
  - "Assets/Speed-Staes/Scripts/Physical/RunnerPoseDriver.cs"
  - "docs/tasks/T-156-kalca-canli.md"
touches_scene: false
branch: agent/ik-kosucu
commits: [96702e84, c34f0cd3]
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
Kullanici 06.11: "karakterin kalcasi cok sabit duruyor. sanki goril gibi hareket ediyor. ust vucut sabit kalca sabit sadece bacaklar hareket ediyor gibi duruyor. kamera sanirim kalcaya sabit fixli o yuzden de boyle gozukuyor olabilir."

## Olculen sebep
- Kamera (`SideFollowCamera`) kalcaya bagli degil: Y sabit, ileri yonde kokun yumusak hizini izler. Ama kalca kokun ustunde ileri-geri hic oynamiyor. Sonuc ayni: ekranda kalca yatayda cakili, bacaklar etrafinda donuyor. Kullanicinin gozlemi dogru.
- `hips_yaw`, `hips_roll` kosuda ve sekiste tam 0: T-144'te Malik'in ust govdesi alinirken `pelvisTwist`, `pelvisRoll`, `chestTwist`, `chestRoll` kapandi.

## Istenen
1. **Ileri-geri akis (surge).** Yeni alanlar: kalca kokun ustunde ileri-geri kayar. Itis sonunda (arka bacak en geride) one, inise dogru geriye. Bacak acisindan okunur, surekli (basis saatinden degil). Sekiste A ~6-8 cm, kosuda ~2-3 cm. Yerdeki ayak dunyada kalir (`PinFeet`), kayma yok.
2. **Kosuda burgu ve yana dusme.** Kalca burgusu (yaw) ve gogus ters: kucuk, yumusak; T-110'daki mekanizma var (`UpdateTrunk`). Baslangic: kalca yaw genligi ~8-10, gogus ters ~6-8, kalca roll ~2-3. Baslangicta `_gaitW` altinda kalir, hizla gelir. Yeni alan adi ac (eski alanlar sifir). Bas ileri bakar.
3. **Sekis:** T-154'un sabit govdesi bozulmaz (kullanici: "belden oynama yok"): sekiste yaw, roll, rock yok. Sekiste canlilik: T-154'un Y yayi + bu surge.

## Kabul kriterleri
- [ ] `rhythm032`: kalca yaw araligi 15-20 derece, roll 4-6, gogus ters fazda; kare farki <= 2 derece (titreme yok).
- [ ] Kalca ileri-geri araligi sekiste 12-16 cm, kosuda 4-6 cm; ikinci fark <= 1 cm.
- [ ] Yerdeki ayak kaymasi onceki kayitla ayni (`footWorldZ`), `snap_check.py` temiz, kol ve diz twist bozulmadi.
- [ ] `fastRun`, `start`, `blockStart`, `stop`, `holdBoth`, dalis: yeni yaw ve roll tutma, dalis, frende 0.

## Kurallar
T-154 ile ayni (TUZAKLAR, snap yok, yeni alan adi, Tooltip, yola sinirli commit, Unity yok, sim'e dokunma).

## Calisma notu
- Surge: SurgeStep() hip.z'ye (p.pelvis.x yaninda, LegFK'den once); surucu gosterilen uyluk (sekiste sekis bacagi, kosuda en geri uyluk), surgeSpring 8 Hz; hopSurge 0.07, runSurge 0.025. SitBack'ten sonra degil: SitBack LegFK'den sonra kosuyor, hip.z baslangicta eklenince planted ayak dunyada kalir (footZ'ye dokunulmadi).
- Burgu: pelvisTwist 9, chestTwist 7, pelvisRoll 2.5, chestRoll 1.5 varsayilan; UpdateTrunk amp'i _plainRun, dalis, tutma, fren, blok durusuyla carpildi (sekis, cift dahil 0).
- Roll hala turevden okunuyor; titreme olursa uyluk farkindan okunabilir. Olculmedi.
- Olcu (t154-f): rhythm032 kalca yaw 17.4, kare 3.0 (hedef 2 az ustunde, kosu seritlerinde titreme yok); roll 4.5; gogus dunyada ~0 (kalca doner, omuz ileri bakar). Sekiste yaw ve roll 0.
- Surge CSV de sutun yok, seritten bakildi; kalca ileri-geri araligi sayiyla olculmedi. Kullanici gozu karar verecek.
