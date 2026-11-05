# Karakter — iyi çalışan parçalar (başka denemeyle harmanlamak için)

> Kaynak: branch `claude/karakter-hareketleri-baec15`, commit `387a1b23` (2026-11-05). Hepsi
> `Scripts/Physical/RunnerPoseDriver.cs` içinde; fizik (`RunnerSim.cs`) **değişmedi**. Değerler karakter prefab'ındaki
> (`Prefabs/Character/HumanCharacterDummy_F.prefab`) değerlerdir; prefab'ta olan değer koddaki varsayılanı ezer.
>
> Belge **tüm vücudu** anlatır. **Korunacak (iyi bulunan) dört parça:** zıplama (uçuş/iniş), sırt ve üst vücut,
> kollar ve dirsekler, kafa (bölüm 1–4). **Bacaklar** (bölüm 6) bilgi için eksiksiz yazıldı; harmanlamada başka
> denemenin bacaklarıyla değiştirilebilir. Her parça için: ne yapar, hangi metot, hangi alanlar, neye bağlı.

---

## 0. Ortak girdiler (bu parçaların okuduğu şeyler)

| Girdi | Nereden | Kim kullanır |
|---|---|---|
| `sim.HeadAngle` | `RunnerSim` (fizik): ritimde ~dik, sık basma/basılı tutmada öne (`headKick`, `holdPush`), bırakınca geri (`headBack` −15) | Sırt zinciri, kafa |
| `sim.Legs[i].WeightValue` (`_holdL`, `_holdR`) | `RunnerSim`: tuş basılı kaldıkça 0→1 dolan "tutma ağırlığı". Kısa basış ~0.3'te kalır | Sırt (tek/iki tuş devrilmesi) |
| `sim.Speed`, `Speed01()`, `_stepping` | Hız; tuş basılı/koşuyor mu | Sırt (hıza göre eğilme) |
| `leg.thighWorld` | **Bacak sisteminden**: uyluğun dünyadaki açısı (dikeyden, + = önde) | Kollar |
| `leg.phase`, `leg.phaseTime`, `leg.duration` | **Bacak sisteminden**: bacağın o anki fazı ve süresi | Kollar (tween başlangıcı) |
| `Carries(leg)`, `leg.grounded` | **Bacak sisteminden**: ayak yere basıp gövdeyi taşıyor mu | Zıplama |
| `LegNeed(leg)` | Taşıyan bacağın istediği kalça yüksekliği | Zıplama |

**Taşırken dikkat:** Kollar ve zıplama bacak sisteminden bu birkaç değeri okur. Yeni bacak sistemi aynı anlamda
`thighWorld`, faz değişimi (yeni faz → yeni `phaseTime`) ve "ayak yerde mi" bilgisini vermeli. Sırt ve kafa yalnız
fizikten okur, bacaktan bağımsızdır.

---

## 1. Zıplama: koşu uçuşu, dalış kalkışı, iniş

**Metot:** `UpdateBodyHeight(sim, dt)` (gövdenin dikey konumu `_bodyY` yayı), `Carries(leg)`, `KeepAboveGround`.

### Kural
- **Gövdeyi yalnız yere basan ayak taşır** (`Carries`: ayak `grounded` ve faz Rest/Front/Push). Havadaki bacak gövdeyi
  aşağı çekmez → "yere yapışma" yok.
- **İki ayak da havada, koşuyor (dalış değil):** koşu uçuşu. Her uçuş **aynı hızla** kalkar (`flightLift`), kendi
  yerçekimiyle (`flightGravity`) iner. Yükseklik yayının hızı devralınmaz (fırlama olmaz).
- **Ayak basınca:** gövde yayla oturur; inerken yumuşak `dropSpring`, çıkarken `heightSpring`. Çok alçaktan kalkarken
  `liftSpring`.
- **Dalış (iki tuş basılı, iki bacak Hold):** son ayak kalkınca gövde **hep aynı** `liftKick` hızıyla kalkar ve
  sim'in yerçekimiyle (`gravity` 5) iner. Baş/diz/kalça zemine girmez (`KeepAboveGround`).
- Öne uzanan ayak, ayak yere değene kadar havada kalır (`reachAirMax` en fazla), inerken `reachLand` açısına iner.

### Alanlar (prefab değeri)
| Alan | Değer | Ne |
|---|---|---|
| `flightLift` | 0.85 m/s | Koşu uçuşunun kalkış hızı (~3 cm sıçrama) |
| `flightGravity` | 12 m/s² | Koşu uçuşunun yerçekimi (yüksek = daha ağır, kısa) |
| `dropSpring` | 4.5 Hz, 0.6 | Ayak basınca gövdenin oturması |
| `heightSpring` | 8 Hz, 0.8 | Gövdenin bacak yüksekliğini izlemesi (yukarı) |
| `liftSpring` | 0.9 Hz, 1 | Çok alçaktan (yerden) kalkış |
| `liftKick` | 1.5 m/s | Dalışta kalkış hızı |
| `reachLand`, `reachLandTime` | 35/155, 0.1 sn | Uzanan bacağın iniş açısı ve süresi |
| `reachAirMax` | 0.5 sn | Uzanan ayağın havada en uzun beklemesi |
| Sabitler | `GroundContact` 0.01 m, `GroundKeep` 0.05 m | Ayağın "yere değdi" ve "hâlâ yerde" eşikleri (titreme olmasın) |

---

## 2. Sırt ve üst vücut: baştan kalçaya zincir

**Metot:** `UpdateLean(dt, out baseBend, out bend, out headLean)`; sonuç `Update` içinde kalça (`hipTarget`), bel dibi
(`spineRootTarget`) ve belin üstü/boyun (`WriteHead` → `spineTipTarget`) hedeflerine yazılır. Rig: TwistChain
`B-spine → B-chest → B-neck` (eğri ortaya %60).

### Kural
1. **Ölçü sim'in baş açısıdır** (`HeadAngle`, en az 0). Ritimde dik, sık basma/tutmada öne.
2. **Zincir yukarıdan aşağı, azalan paylarla:** göğüs başın `chestShare`, bel dibi `waistShare`, kalça `hipShare`
   kadarını alır. Her halka bir öncekinden **az** eğildiği için sırt **yay gibi kavislenir** (sopa değil).
3. **Gecikme:** göğüs başı `hipFollowSpring` ile, bel dibi göğsü ve kalça bel dibini `waistSpring` ile izler. Baş
   önce gider, kalça en son gelir; salınım yok (sönüm 1).
4. **Bütün gövdeye eklenenler** (her halkaya aynı):
   - hızla hafif öne eğilme: `hipRunLean` (duruş → max hız),
   - **tek tuş basılı tutulunca hızlı devrilme:** `holdLean`, yayı `holdTipSpring` (referans: 0.15 sn'de ~30°).
     Kısa basışlar tetiklemez: ağırlık 0.25–0.6 arasında yumuşakça devreye girer (`HeldWeight`),
   - **iki tuş basılı (dalış):** `bothLean × (küçük ağırlık)²` (kare: hızlı bas-çek tetiklemez).
5. **Yataya yakın** (kalça `pelvisPitchRange.y`'nin yarısından sonra) kavis **dalış kamburuna** döner:
   `spineLevelCurve` (bel dibi yukarı, omuzlar kalça çizgisinin üstünde).
6. **Geriye:** yalnız baş geri salınınca, en fazla `hipBackMax`; bel geriye en fazla `waistBackMax`, fazlası bütün
   gövdenin topuklar üstünde geriye oturması (`SitBack`, `heelRock`).
7. **Duruşta** hafif kavis: `waistAngle` + `waistTopAngle`.

### Alanlar (prefab değeri)
| Alan | Değer | Ne |
|---|---|---|
| `chestShare` | 0.78 | Göğsün baş açısından payı |
| `waistShare` | 0.6 | Bel dibinin payı |
| `hipShare` | **0.65** | Kalçanın payı. Kodun varsayılanı 0.45; prefab eski alandan 0.65 taşıyor (ölçümler 0.65 ile alındı) |
| `hipFollowSpring` | 4 Hz, 0.9 | Göğsün başı izlemesi |
| `waistSpring` | 3 Hz, 1 | Aşağı halkaların izlemesi |
| `hipRunLean` | 3 → 8° | Hıza göre eğilme |
| `holdLean`, `holdTipSpring` | 32°, 7 Hz / 1 | Tek tuş tutmada hızlı devrilme |
| `bothLean` | 55° | İki tuş (dalış) eğilmesi |
| `hipBackMax` | 12° | Geriye en fazla |
| `spineLevelCurve` | −12° | Yataya yakın dalış kamburu |
| `waistAngle`, `waistTopAngle` | 2°, 13° | Duruşta bel kavisi |
| `waistBackMax` | 7° | Belin geriye en fazla bükülmesi |
| `pelvisPitchRange` | −10 … 90° | Kalça eğim sınırı |

**Ölçüm (play):** ritim 0.32 sn → kalça 16° / bel 18° / göğüs 16° / boyun 28° (dik); tek tuş tutma → 26 / 31 / 39 / 51
(yay); dalış havada → 88 / 89 / 102 / 116 (düz gövde, omuzda kambur).

---

## 3. Kollar ve dirsekler

**Metotlar:** `StepArm(arm, pose, leg, now)` (her karede açı), `PoseArm(arm)` (LateUpdate'te kemiklere yazar),
`Ease(...)` (eğri).

### Kural
- **Bir kol = karşı bacak** (sağ bacak → sol kol, sol bacak → sağ kol).
- Kol karşı bacağın **o anki uyluk açısını** (`leg.thighWorld`) okur ve bacağın `recover` … `reach` aralığını kolun
  `armBack` … `armFront` aralığına çevirir; dirsek aynı oranla `elbowBack` … `elbowFront`. Yani bacak geride → kol
  geride ve dirsek açık; bacak önde → kol önde ve dirsek bükük. Tutmada bacak geride olduğu için kol da geride kalır.
- **Hareket bir tween'dir:** karşı bacak yeni bir faza girdiğinde (faz ya da `phaseTime` değişince) kol bulunduğu
  açıdan yeni hedefe doğru `armEase` eğrisiyle gider; süre bacağın faz süresi, ama en az `armMinTime`.
- **Duruşta** (bacak Rest ve koşmuyor) kol pozun kendi açısına (`ArmPose` asset) gider.
- **Kemiğe yazma (`PoseArm`):** üst kol `swing` kadar öne/geri döner, ön kol `swing + elbow`; kollar yana
  `armOutward` açılır; köprücük (`B-shoulder`) salınımı `shoulderFollow` kadar izler. Her şey koşucu uzayında
  kurulup omuzun dinlenme çerçevesine çevrilir, gövde eğimiyle birlikte döner. El IK kapalı.

### Alanlar (prefab değeri)
| Alan | Değer | Ne |
|---|---|---|
| `armBack`, `elbowBack` | −40°, 30° | En geri kol ve dirsek |
| `armFront`, `elbowFront` | +50°, 95° | En ön kol ve dirsek (bükük, el göğüs önünde) |
| `armEase` / `armCurve` | EaseOutQuint | Kol tween eğrisi (EaseInCirc, EaseOutQuint, Linear, Custom) |
| `armMinTime` | 0.2 sn | En kısa kol hareketi (kısa itiş kolu fırlatmasın) |
| `armOutward` | 8° | Yana açı |
| `shoulderFollow` | 12° | Köprücüğün salınımı izlemesi |

**Ölçüm:** kol iki yöne −54° … +41°, dirsek önde 86°, arkada 150° (iç açı).

---

## 4. Kafa ve boyun

**Metotlar:** `WriteHead(trunk, poseTrunk, head, sit, headDown, rootRot)`, `TrimNeck()` (LateUpdate), baş açısı
`UpdateLean`'den (`headLean`).

### Kural
- Başın açısı = bütün gövde eğimi + **sim'in baş açısı** (+ yataya yakın `headDip`). Yani kafa fiziğin "ölçüsünü"
  doğrudan gösterir: ritimde dik, sıkıştırınca öne.
- Boyun gövdenin üstü ile baş arasındaki farkı büker, en fazla `neckMax`; boynun payı sabit `NeckShare` (0.5), kalanı
  başın kendisi (`Head Aim Rig`, MultiAim → `B-head`, bakış hedefi koddan yerleştirilir).
- Baş gövdenin en fazla `headLead` önüne geçer (dalışta baş kalçanın altına katlanmaz).
- **Geriye oturmada** baş eğimin yalnız `headKeep` kadarını alır, ileri bakmaya devam eder.
- **Boyun kırpma:** öne eğilmede `B-neck` zincirin verdiğinden `neckTrim` daha az bükülür (eğim `neckTrimLean`'e
  gelince tamamı); baş kendi açısını korur → baş gövde çizgisinde kalır.
- Yataya yakın baş omuzların altına iner: `headDip`.

### Alanlar (prefab değeri)
| Alan | Değer | Ne |
|---|---|---|
| `neckMax` | 35° | Boynun kendi başına en fazla bükülmesi |
| `headLead` | 30° | Başın gövdenin en fazla önüne geçmesi |
| `headKeep` | 0.33 | Geriye oturmada başın aldığı pay |
| `heelRock` | 1 | Geriye oturmada ayakların topuğa kalkması |
| `neckTrim`, `neckTrimLean` | 10°, 30° | Boyun kırpma ve tam devreye girdiği eğim |
| `headDip` | 20° | Yataya yakın başın öne düşmesi |
| Fizik (`RunnerBody → Tuning`) | `headBack` −15, `headKick` 12, `holdPush` 70, `headForwardMax` 80 | Baş açısının kendisi (hızı da belirler: "kafa ölçü") |

---

## 5. Harmanlama için kontrol listesi

1. **Fiziği değiştirme:** sırt ve kafa `RunnerSim.HeadAngle`'a dayanıyor; o açı aynı zamanda hızı belirliyor.
2. **Bacak sistemi şunları vermeli:** `thighWorld` (uyluğun dünyadaki açısı), faz değişince yeni `phaseTime`, faz
   süresi `duration`, ayak yerde mi (`grounded` + taşıyan faz) ve taşıyan bacağın istediği yükseklik (`LegNeed`).
   Bunlar başka adla geliyorsa yalnız `StepArm`, `Carries` ve `UpdateBodyHeight`'ın okuduğu yerleri bağla.
3. **Kollarda `recover` / `reach`** bacak açılarının en geri/en ön sınırı olarak kullanılıyor; yeni sistemde bu iki
   açının karşılığını ver (ya da `StepArm`'daki `back`, `front` değerlerini sabitle).
4. **Tek tuş / iki tuş devrilmesi** yalnız sim'in tutma ağırlıklarına bakar (`_holdL`, `_holdR`); bacak fazından
   bağımsız.
5. **Prefab:** bu alanların değerleri prefab'ta kayıtlı; başka branch'e prefab'ı birleştirirken bu satırları koru
   (özellikle `hipShare` 0.65, `flightLift` 0.85, `flightGravity` 12, `dropSpring` 4.5 Hz).
6. Doğrulama için ölçüm: ritim 0.32 sn, tek tuş 1.5 sn tutma, iki tuş 1 sn dalış; sırt/kol açıları yukarıdaki
   ölçümlerle aynı çıkmalı.

---

## 6. Bacaklar (bilgi için; harmanlamada değiştirilebilir)

**Akış:** `OnTapped` (basış/bırakış) → `TickLegs` (faz geçişleri) → `UpdateLeg` (açı, eğri, diz yayı, ayak) →
`FollowHips` (kalçayla dönme, tek tuş tutma) → `LegFK` (açılardan diz ve bilek noktası) → `WriteLeg` (IK hedefi ve
ipucu) → LateUpdate'te `FixLegTwist`, `PoseAnkle`, `PoseToes`. Bacak bir **açıdır**: uyluk (dikeyden, + önde), diz
(180 = düz). **Tek tuş = tek bacak:** basılan bacak iter, diğeri öne uzanır.

### Fazlar
| Faz | Ne zaman | Hedef (uyluk / diz) | Sonra |
|---|---|---|---|
| Rest | Girdi yok / evde | `crouch` 40/100, koşarken `runHome` 10/160 (`restToBaseSpeed`) | — |
| Push | Bu bacağın tuşu | `pushEnd` (yavaş −36/150 ↔ `pushEndFast` −60/160, `strideSpeed` 2→8 m/s). Süre `pushBackTime` 0.12→0.08 sn, eğri `pushEase`; diz kendi yumuşak eğrisiyle açılır | Tuş basılıysa Hold, değilse Recover |
| Recover | İtiş bitti | `recover` (−37/83 ↔ `recoverFast` −56/88), süre `recoverTime` 0.07 | Basışlar sürüyorsa Wait, yoksa Mid |
| Wait | Geride, diğer tuşu bekler | `recover` açısında; diğer tuşta öne fırlar; `openHoldTime` 0.6 sn basış yoksa Mid | Reach / Mid |
| Mid | Kendiliğinden | `mid` 15/44 | Rest |
| Reach | Diğer bacağın tuşu | `reach` (61/155 ↔ `reachFast` 78/145), eğri `reachEase`. Diz önce katlanır (`reachFoldKnee` 100→55, uyluk yolunun `reachFoldAt` 0.3'ünde), uyluk yolun `reachOpenAt` 0.85'ine gelince açılır. Bitince `reachLand` 35/155'e iner | Ayak yere değince Front. Arka bacak geride iken havada açık bekler, gövde ayağın 1.5 cm üstünde süzülür |
| Front | Yere indi | Ayak yerinde, bacak gövde hızıyla eve akar (en fazla `frontGroundMax` 0.5 sn). Diğeri gerçekten tutuluyorsa destek: gövde devrildikçe `holdSupportStart` 55/110 → `holdSupport` 80/100 | Diğeri itmiyor/tutulmuyor/katlanmıyorsa Rest |
| Hold | İtiş bitince tuş basılı | **Tek tuş:** itiş sonundan `holdReachTime` 0.18 sn'de kendi geri açısına (`holdThigh` −65°), diz `holdStraightKnee` 165'e düzleşir; gövde çizgisi geçince onunla kalkar. **İki tuş:** kalça çizgisinde (`heldLegArch` −10, `holdKnee` 145) | Bırakınca: gövde dikse Recover, eğikse doğrudan gövdenin altına (Rest), takla yok |

### Zamanlama ve eğriler
| Alan | Değer | Ne |
|---|---|---|
| `pushTime` | 0.2 sn | Uzanma süresi (hızla `animSpeedUp` kadar kısalır) |
| `pushBackTime` | 0.12 → 0.08 sn | İtiş süresi (yavaş → hızlı) |
| `pushEase` / `pushCurve` | EaseOutQuint | İtiş eğrisi |
| `reachEase` / `reachCurve` | EaseOutQuint | Uzanma eğrisi |
| `recoverTime`, `midTime`, `homeTime` | 0.07, 0.15, 0.15 | Geri gelişin payları |
| `swingSteps` | 2 | Geri gelişin toplam süresi = sim adım süresi × bu |
| `kneeSpring` | 9 Hz, 0.9 | Havadaki bacakta alt bacağın uyluğu gecikmeyle izlemesi |
| `maxKnee` | 177° | Diz hiç tam düz olmaz (IK kırılmasın) |

### Ayak, bilek, parmak
- Yerde: iniş topuk önde (`touchdownPitch`), itişte topuk kalkar (`stancePitch`), parmaklar döner (`toeRoll`);
  havada `swingAnkle`, `swingToe` eğrileri. İki profil hızla karışır: `accelProfile` ↔ `maxSpeedProfile`
  (`accelProfileSpeed` 2, `maxProfileSpeed` 7.5 m/s).
- Sınırlar: `ankleUpMax` / `ankleDownMax`, `toeUpMax` / `toeDownMax`, `toeLength`; tutulan ayak `pitchBack`.

### Bacağın gövdeyle ilişkisi
- Havadaki ve tutulan bacak kalçayla döner, yerdeki bacak yere göre açısını korur (`FollowHips`, `FollowRate`).
- Gövde yüksekliği yalnız yere basan ayaktan gelir (bölüm 1).
