# Oyuncu (Player) ve Rig Rehberi — PhysicalTest sahnesi

Bu dosya `PhysicalTest.unity` sahnesindeki koşucu objesinin (`HumanCharacterDummy_F`) üzerindeki
bütün bileşenleri ve Animation Rigging kurulumunu anlatır: **ne neye yarıyor, kim kimi sürüyor,
bir şeyi değiştirmek istediğinde nereye bakmalısın.** Bölüm 8, Inspector'daki **her alanı** tek tek listeler.

Yazıldığı tarih: 2026-10-23, son güncelleme: 2026-10-27 (baş fizikte ve hızı etkiliyor, bel zinciri, boyun kırpma,
dalışta gövdenin yere yerleşmesi, tuşlar N/M). Değerler kodun varsayılanlarıdır; prefab'ta (Inspector) kayıtlı değer
her zaman koddakini **ezer** — farklı olanlar yanında "prefab" diye yazılı.

---

## 1. Büyük resim: tek cümle

> **Girdi → Fizik → Görsel.** Tuşlar `RunnerInput`'a düşer, `RunnerBody` bunları `RunnerSim`'e
> (fizik) verir, `RunnerPoseDriver` fizikten sadece **okur** ve rig hedeflerini (IK) yazar,
> Animation Rigging de kemikleri bu hedeflere çeker.

```
 Klavye N (sol) / M (sağ) · ekranın sol/sağ yarısı      RunnerReplay (1–4 tuşları, video dizileri)
                 │                                         │ Inject()
                 ▼                                         ▼
          ┌──────────────┐  zaman damgalı basış/bırakma kuyruğu
          │ RunnerInput  │──────────────────────────────┐
          └──────────────┘                              ▼
                                               ┌──────────────┐   Tapped / RunReset olayları
                                               │  RunnerBody  │──────────────────────┐
                                               │  └ RunnerSim │ (hız, baş açısı,      │
                                               └──────┬───────┘  eğim, hop, enerji)   │
                       kökü ileri taşır ◄─────────────┤                               │
                                                      │ sadece okur                   ▼
 RaceController (Race objesi) ◄── okur ───────────────┤                    ┌──────────────────┐
 RunnerBodyHUD  ◄── okur ─────────────────────────────┘                    │ RunnerPoseDriver │
                                                                           │  └ bacak açıları │
                                                                           └────────┬─────────┘
                                 açılar → FK → IK hedeflerini yazar (Hip/Spine/Leg target, hint…)
                                                                                    ▼
                                         RigBuilder → Control Rig → constraint'ler → kemikler
                                                                                    ▼
                          LateUpdate: boyun kırpma, kollar, diz twist, ayak bileği, parmaklar (doğrudan kemik)
```

**Altın kural — gerçek hareket ile görsel ayrı:**

| | Gerçek hareket (fizik) | Görsel |
|---|---|---|
| Dosya | `RunnerSim.cs` (saf C#), `RunnerBody.cs` | `RunnerPoseDriver.cs`, poz asset'leri |
| Inspector | `RunnerBody → Tuning` | `RunnerPoseDriver` |
| Ne belirler | Hız, mesafe, basışın verimi, zamanlama, **baş açısı**, enerji | Bacak açıları, gövde/bel/boyun eğimi, yükseklik, dalış, kollar, ayaklar |
| Ne zaman bakarsın | "Yavaş / hızlı gidiyor", "basış ceza yemiyor", "baş öne gitmiyor" | "Garip görünüyor", "bel kütük gibi", "ayak yerden kalkmıyor" |

Görsel taraf hızı ya da zamanlamayı **asla** değiştirmez. Tek köprü **baş açısı**: fizikte hesaplanır (hızı
etkiler), görsel onu okuyup çizer.

---

## 2. Koşucu objesindeki bileşenler (Inspector'daki sırayla)

| # | Bileşen | Kimin | Görevi | Ne zaman dokunursun |
|---|---|---|---|---|
| 1 | **Animator** | Unity | Rig'in çalışması için gerekli. Controller ve avatar **yok**; klip oynatmıyor. | Hiç. Silme: rig durur. |
| 2 | **RigBuilder** | Animation Rigging | Tek katman: `Control Rig` (aktif). | Rig'i tamamen kapatmak için. |
| 3 | **RunnerInput** | `Scripts/Runner` | **N** (sol ayak) / **M** (sağ ayak) ve dokunmatik sol/sağ yarıyı okur, her basış/bırakmayı **kendi zaman damgasıyla** kuyruğa atar. Hiçbir şeyi değerlendirmez. (Ok tuşları Scene görünümünü kaydırdığı için 2026-10-27'de N/M yapıldı.) | Tuş ya da dokunma alanını değiştireceksen. |
| 4 | **BoneRenderer** | Animation Rigging | Sahne görünümünde kemikleri çizer. Sadece editör yardımcısı. | Kemikleri görmek/gizlemek için. |
| 5 | **RunnerBody** | `Scripts/Physical` | Fiziğin sahnedeki yüzü. Basışları `RunnerSim`'e **tam basış anında** verir, kökü `Sim.Distance` kadar ileri taşır. Bütün oyun sayıları **Tuning** alanında. | Hız, itiş, zamanlama, baş, enerji. |
| 6 | **RunnerPoseDriver** | `Scripts/Physical` | **Sadece görsel.** Pozları karıştırır, bacak fazlarını çalıştırır, eğimi/beli/boynu ve yüksekliği uygular, rig hedeflerini yazar; IK'dan sonra boyun, kol, diz, bilek, parmak. | Duruş, adım, gövde, ayak/kol. |
| 7 | **RunnerReplay** | `Scripts/Physical` | Video dizilerini gerçek oyuncu basıyormuş gibi enjekte eder. **1–4** başlatır, **Esc** durdurur. | Aynı girdiyi tekrar denemek. |
| 8 | **RunnerBodyHUD** | `Scripts/Physical` | Sol üstteki panel: süre, hız, eğim, son basış, grafikler, tekrar düğmeleri. **H** gizler. | Okuma/ölçüm. |

Sahnedeki diğer objeler: **Race** (`RaceController`: Ready → ilk basışta Running; `raceDistance` geçilince süre
`LastRaceTime`'a, koşucu **direkt başa**), **FinishLine** (play'de `raceDistance` ileriye konur), **Main Camera**
(`SideFollowCamera`: yandan, sadece koşu ekseninde izler).

### Execution order

| Sıra | Bileşen | Neden |
|---|---|---|
| −200 | `RunnerInput` | Önce girdiler. |
| −150 | `RunnerReplay` | Oynatılan dizi de girdi gibi. |
| −100 | `RunnerBody` | Fizik bu karenin girdileriyle ilerler. |
| −50 | `RunnerPoseDriver` (`Update`) | Fiziğin güncel hâlini okuyup IK hedeflerini yazar. |
| 0 | `RaceController`, `RunnerBodyHUD` | Okuyucular. |
| — | Animator + Animation Rigging | Kemikleri hedeflere çeker. |
| sonra | `RunnerPoseDriver` (`LateUpdate`) | Boyun kırpma, kollar, diz twist, bilek, parmak. |

---

## 3. Gerçek hareket: RunnerBody ve RunnerSim

`RunnerSim` saf C# (sahneden habersiz), 1/240 sn'lik adımlarla ilerler:

| Büyüklük | Model | Ne yapar |
|---|---|---|
| **Hız** | Kütle + itme + sürtünme | Her basış bir itme; basılı tutmak kısa süre itmeye devam eder; sürtünme sürekli yavaşlatır; **baş öndeyken ek sürtünme**. |
| **Baş açısı** | İki yay (`_headFwd` → `_head`) | Tuş basılı → öne, tuş yok → geri (aşağıda). Basışın verimini ve sürtünmeyi belirler; görsel bunu çizer. |
| **Tutma ağırlığı** (`LegSim.Weight`, her tuş) | Yay | Tuş basıldığı an dolmaya başlar (eşik yok), bırakınca boşalır. `holdPush` ve görseldeki tutma açıları bunu kullanır. |
| **Bacak `ext` / `hold` yayları** | Yay | `ext`: bacak hâlâ geride mi (verim). `hold`: 0.14 sn (`minStepTime`) eşikli tutma — sadece fizikte (tutma itişi, eğim, enerji); **görsel kullanmaz**. |
| **Eğim (`lean`)** | Az sönümlü yay | Sadece oyun mantığı: Stumble pozunun karışması ve HUD. Ekrandaki gövde eğimi **baştan** gelir. |
| **Zıplama (`hop`)** | Yerçekimi + itme | Sadece zamanlama (havadayken basmak = erken). Ekrandaki inip kalkma bacak açılarından. |
| **Enerji** | Basış ve tutma azaltır | `lowEnergy` altında itişler zayıflar; dururken dolar. |

### Basış nasıl değerlendirilir (zamanlama eğrisi yok)

| Sonuç (HUD) | Ne zaman | Etkisi |
|---|---|---|
| **çıkış** | Dururken ilk basış | Tam itiş |
| **tam zamanında** | Önceki adım indikten hemen sonra | Tam itiş (≈0.3 sn ritim) |
| **geç** | İnişten çok sonra | İtiş azalır (hızlıyken daha çok) |
| **erken (havada)** | Havadayken ya da diğer ayak hâlâ iterken | Neredeyse boşa gider |
| **birlikte** | Diğer ayaktan hemen sonra (`togetherWindow` 0.08 sn) | Erken sayılmaz (hızlı koşuda basışlar 0.05–0.08 sn arayla) |

Ek çarpanlar: bastığın bacak hâlâ geride → verim düşer; aynı bacağa 1 sn içinde iki kez → ×0.4; enerji; **baş**:

### Baş açısı ve hıza etkisi (2026-10-27)

Tek bir baş açısı (°, + = öne). Her tuşun tek bilgisi: **basılı mı**. Bas-çek ile basılı tut arasında ayrım yok.

- **Tuş basılıyken** baş öne: ilk anda `headKick` (16°, ~0.1 sn içinde), basılı kaldıkça `holdPush` (70°/sn × tutma
  ağırlığı; iki tuş 2 kat). Basılıyken **geri gelmez**. Her basışta küçük sallanma (`headNod`).
- **Hiç tuş yokken** geri döner: önce `headReturnSpring` ile dinlenme yerine (`headBack` −15°, hızla
  `headBackSpeed` arasında devreye girer; dururken 0), onu geçince yumuşak `headSettleSpring` ile (geriye oturma,
  öne salınmadan). Dönüş tuş kalkınca hemen tam güçle başlamaz: `headReturnDelay` (0.08 sn) içinde güçlenir — hızlı
  bas-çekte bir sonraki basış dönüşü zayıf yakalar, baş tutmadaki gibi birikir.
- **Hıza etkisi (eşik yok):** basışın verimi × `1 − (baş / headSpoil)^headSpoilPower` → dik/geride tam, 30° ~%70,
  60° ~%35, 80° ~%10. Ayrıca baş öndeyken ek sürtünme `headDrag` × (baş / `headForwardMax`). Sık basma kendini
  cezalandırır; ritme dönünce verim baş dikleştikçe geri gelir.

Ölçüm (2026-10-27, play):

| Girdi | Baş (son 1 sn) | Not |
|---|---|---|
| Ritim 0.32 sn | ~7° | 6. sn'de 6.0 m/s (önce 6.2) |
| Bas-çek saniyede 5 | 39° | |
| Bas-çek saniyede ~7 | 75° | hız düşer |
| Bas-çek saniyede 10 | 80° | 3 sn'de ~2.7 m/s (video ~6 mph) |
| Tek tuş basılı | 80° (~1 sn'de) | |

---

## 4. Görsel: RunnerPoseDriver

Her karede şu katmanlar üst üste konur:

| Sıra | Katman | Kaynak | Ne yapar |
|---|---|---|---|
| 1 | **Pozlar** | `Physical/Poses/*.asset` | **Ready** → (hızlandıkça) **Run** → (sim'in eğimi aşınca) **Stumble**. Tutma/dalış/yatma pozu **yok**. |
| 2 | **Bacak açıları** | `Leg angles` / `Leg timing` | Bacak bir **açıdır**: uyluk (dikeye göre, + = önde) ve diz (180 = düz). Hareket uyluktan başlar; açılardan FK ile diz ve bilek bulunur, IK sadece bunu üretir. Her faz bacağın **o anki açısından** başlar. |
| 3 | **Eğim: baş → boyun → bel → kalça** | `Head & neck` + `Body` | Aşağıda. |
| 4 | **Gövde yüksekliği** | Bacak açıları | Kalça, en alttaki taşıyan ayak (Hold'da olmayan her bacak) zemine değecek yükseklikte (`heightSpring`). Yerden kalkarken yumuşak `liftSpring`. Taşıyan kalmayınca gövde uçar (dalış). Hiçbir parça zemine girmez. |

### Eğim zinciri (2026-10-27)

- Eğim **sadece baştan** gelir (sim'in `HeadAngle`'ı). Zincir: **baş → boyun → belin üstü (`B-chest`) → belin dibi
  (`B-spine`) → kalça**; her halka bir üsttekini sönümlü bir yayla izler (`hipFollowSpring`, sonra `waistSpring`):
  baş önden gider, kalça en son gelir (canlılık, salınım yok).
- **Paylar (öne):** kalça eğimin `hipShare` (0.65) kadarını alır; bel bunun üstüne kavis yapar: belin dibi
  `waistBaseShare` (0.27), belin üstü `waistTopShare` (0.67) kadar. **Bacaklar** (tutulan dahil) eğimin tamamıyla
  döner — ayak kalkışı kalçanın payından etkilenmez.
- **Duruşta bel:** `waistAngle` (2°, `B-spine` X) + `waistTopAngle` (13°): hafif kavis (kullanıcının seçtiği
  girdisiz duruş açıları: spine 2, chest 4.5, neck 12).
- **Geriye:** gövde sadece başın dinlenme yerini **geçen** kısmı alır, bütün vücudu yerdeki ayakların **topukları**
  etrafında döndürerek (`SitBack`, `heelRock`); bel geriye en fazla `waistBackMax` (7°). Baş ileri bakar
  (`headKeep`).
- **Boyun:** öne eğilmede `B-neck` zincirin verdiğinden `neckTrim` (10°) daha az bükülür (`neckTrimLean` 30°'den
  itibaren tamamı); baş kendi açısını korur → baş gövde çizgisinde kalır, yere kıvrık baş değil gövde iner.
  IK'dan sonra `LateUpdate`'te uygulanır.
- `B-chest` kemiğini rig'in TwistChain'i ara değer olarak doldurur; prefab'taki eğri ortaya %60 verir (bölüm 5).

Ölçüm (tek ayak tutma, kemiklerin üst kemiğe göre açısı): baş 28° → kalça 15 / bel dibi 14 / orta 9 / boyun 21;
baş 63° → 35 / 25 / 21 / ~19 (kırpma sonrası). Arka ayak ucu yerden ~0.26 m.

### Bacak fazları

| Faz | Ne zaman | Hedef (uyluk / diz) | Sonra |
|---|---|---|---|
| **Rest** | Girdi yok | `crouch` 40 / 100 (bir tuş basılıyken hızla `runHome` 10 / 160) | — |
| **Push** | Bu bacağın tuşu | `Lerp(pushEnd −30 / 150, holdBack, tutma ağırlığı)` | Tuş basılıysa Hold, değilse Recover |
| **Recover** | İtiş bitti | `recover` −40 / 100: uyluk geride, alt bacak katlanır | Mid |
| **Mid** | Kendiliğinden | `mid` 15 / 70 | Rest |
| **Reach** | Diğer bacağın tuşu | `reach` 47 / 175: öne neredeyse düz, ayak önde iner | Front |
| **Front** | Reach bitti | Önde yerde; diğer bacak tutuldukça `holdSupport` 80 / 100 | Diğeri itmiyor/tutulmuyorsa Rest (`frontHomeTime`) |
| **Hold** | İtiş bitince tuş hâlâ basılı | `holdBack` −60 / 145 (diz hafif bükük); gövdenin eğimiyle kalça etrafında `heldLegMaxAngle` (90 = yatay) ve gövde çizgisine (`heldLegArch`) kadar kalkar; bu sınırlar bacağı kendi tutma açısının önüne çekmez | Bırakınca Mid |

Süreler: `pushTime`, `recoverTime`, `midTime`, `homeTime`; havadaki fazlar hızlıyken `airTimeAtSpeed` kadar kısalır.

**Ayak bileği:** havada (Recover/Mid) bilek salınım eğrisini (`swingAnkle`) izler. Geçişte yerdeki ayak açısından
gelen değer önce bilek sınırına kırpılır (2026-10-27): geriye katlanan bacakta ayak `B-foot` X ~−47'ye kadar açılır
(önce −63'te kalıyordu).

### Dalış — iki ayak, tek ayak özel durumu yok

Kural tek: **son ayak yerden itişle kalkar** (iki tuş birden de, tutulurken diğerine basmak da aynı).

| Aşama | Ne olur |
|---|---|
| **Kalkış** | İten bacaklar gövdeyi aşağı çekemez; taşıyan kalmayınca gövde `liftKick` (1.5 m/s) yukarı hız alır. |
| **Havada** | Sabit yerçekimi (sim'in `gravity`), ileri gitme koşu hızından. Bütün vücut baş ne kadar öndeyse o hızla öne devrilir (`airTip`, prefab 60), baş yere değene ya da kalça yataya gelene kadar. Bacaklar gövde çizgisinde, kollar arkada. |
| **Yere iniş** | Önce baş değer. **Sonra gövde yerleşir** (2026-10-27, `settleSpring`): bel kavisi açılır, baş gövdeye göre `settleHeadUp` kadar kalkıp öne bakar, devrilme yataya kadar sürer; baş artık gövdeyi tutmaz → göğüs, kalça ve dizler sırayla ~0.8 sn'de yere iner. Hiçbir parça zemine girmez (baş `headRest`, kalça `MinPelvis`, taşımayan diz `kneeRest` ve ayak). |
| **Bırakınca** | Bacaklar toparlanır ve taşır; yerleşme ~0.5 sn'de geri açılır, gövde `liftSpring` ile ~0.6 sn'de kalkar, devrilme `airTipBack` ile döner, topuklar üstünde geriye oturma. |

### Kollar

El IK kapalı. **Bir tuş = bir bacak + bir kol** (sağ tuş: sağ bacak + sol kol). Kol o bacağın uyluğunun sadece
evin gerisine gidişini izler; yay/gecikme yok; tam geride sabit `armBack` / `elbowBack`.

---

## 5. Rigging — Control Rig

```
Control Rig  [Rig]
├─ Hip Rig            [MultiParentConstraint]      Hip target            → B-hips
├─ Spine Rig
│  └─ Twist Spine Rig [TwistChainConstraint]       Spine Root/Tip Target → B-spine → B-chest → B-neck
├─ Leg Rig
│  ├─ Left Leg        [TwoBoneIKConstraint]        Left Leg Target/Hint  → B-thigh.L, B-shin.L, B-foot.L
│  │                  [MultiReferentialConstraint] (weight 0, kullanılmıyor)
│  └─ Right Leg       (aynısı, .R)
├─ Hand Rig           [TwoBoneIK ×2]               (play'de KAPALI)
└─ Head Aim Rig       [MultiAimConstraint]         Head LookAt Target    → B-head
```

| Constraint | Kemik(ler) | Hedefi kim yazar | Not |
|---|---|---|---|
| **Hip Rig** | `B-hips` | `Hip target` (konum + dönüş) | Kalçanın yeri ve eğimi. |
| **Twist Spine Rig** | `B-spine` → `B-chest` → `B-neck` | `Spine Root Target` (bel dibi açısı) ve `Spine Tip Target` (belin üstü) | `B-chest` aradan dolar. **Eğri (curve) prefab'ta değiştirildi:** (0,0) (0.43, 0.6) (1,1) — ortaya eğimin %60'ı (doğrusalda ~%42 idi). |
| **Left/Right Leg** | uyluk, baldır, ayak | `Leg Target` (FK bilek), `Leg Hint` (dizin önü) | Play'de rotation weight 0: ayak açısını sonra kod verir. |
| **Hand** | kol | — | Play'de weight 0; kollar LateUpdate'te. |
| **Head Aim Rig** | `B-head` | `Head LookAt Target` başın önüne istenen açıda konur (weight hep 1) | Boyun (Spine Tip) baş açısının yarısını alır. |

**Kodun kemiğe doğrudan dokunduğu yerler** (IK'dan sonra): `B-neck` (kırpma, baş açısı korunur), `B-upperArm`,
`B-forearm`, `B-shoulder`, `B-thigh`/`B-shin` (diz twist), `B-foot` (bilek), `B-toe` (parmak).

---

## 6. Hızlı arama: sorun → nereye bak

| Gördüğün | Bakılacak yer |
|---|---|
| Çok yavaş / hızlı hızlanıyor, tavan hız | `Tuning → pushGain`, `dragConstant`, `maxSpeed` |
| Ritim basışları "geç" sayılıyor | `Tuning → lateGrace`, `hopVelocity` |
| Sık basma yeterince / çok cezalı | `Tuning → headSpoil`, `headSpoilPower`, `headDrag`; baş ne kadar birikiyor: `headReturnDelay`, `headKick` |
| Hızlı bas-çekte baş tutmadaki kadar öne gitmiyor | `headReturnDelay` ↑ (0.1); ritimde baş fazla öndeyse ↓ (0.06) |
| Ritimde baş dik kalmıyor | `headKick` ↓/↑, `headReturnSpring` |
| Seyrek basınca baş çok / az geride | `headBack` |
| Tutunca baş/gövde çok / az / yavaş iniyor | `holdPush`, `headForwardMax`, `holdIn` |
| Gövde başı geç / erken izliyor | `hipFollowSpring`, `waistSpring` |
| Bel kütük gibi / çok kavisli | `hipShare` (↓ = bel daha çok alır), `waistBaseShare`, `waistTopShare` |
| Duruşta bel açısı | `waistAngle`, `waistTopAngle` |
| Boyun çok / az kıvrık, baş yere önce değiyor | `neckTrim`, `neckTrimLean` |
| Bırakınca geriye oturma az / çok | `headSettleSpring` (sönüm ≥ 1 kalmalı), `waistBackMax`, `heelRock`, `headKeep` |
| Kalça çok / az öne | `hipShare`, `pelvisPitchRange`, pozdaki `pelvisPitch` |
| Tutulan bacak az kalkıyor / geriye açılmıyor | `holdBack`, `heldLegMaxAngle`, `heldLegArch` |
| Dalış yeterince / çok hızlı yatmıyor | `airTip` (prefab 60), `holdPush` |
| Dalışta gövde yere inmiyor / çok yavaş iniyor | `settleSpring` frekansı, `settleHeadUp` |
| Dalıştan kalkış hızı | `liftSpring`, `liftBelow` |
| Adımda gövde az / çok yükseliyor | `pushEnd` / `reach` diz açısı, `heightSpring` |
| Beklerken çömelme | `crouch` |
| Adım uzunluğu / hızı | `pushEnd.thigh`, `reach`, `pushTime`, `recoverTime`, `midTime`, `airTimeAtSpeed` |
| Arka ayak itişten sonra az / çok kalkıyor | `recover.knee` |
| Ayak bileği / parmak garip | `ankleUpMax/DownMax`, ayak profilleri (`swingAnkle` …), `toeUpMax/DownMax` |
| Kollar | `armBack`, `elbowBack`, `armOutward`, `shoulderFollow` |
| Bacak gerilip çıtlıyor | `maxKnee` ↓ |

---

## 7. Hangi dosya nerede

| Dosya | Klasör | Not |
|---|---|---|
| `RunnerInput.cs`, `SideFollowCamera.cs` | `Scripts/Runner/` | Girdi (N/M + dokunma) ve kamera. Aynı klasördeki `LoopWay*`, `Pastel*`, `CloudVariation`, `RunnerContactShadow`, `AccessoryPhotoStudio` Serhat'ın kodu. |
| `Spring`, `RunnerSim`, `RunnerBody`, `RunnerPose`, `RunnerPoseDefaults`, `RunnerPoseDriver`, `RaceController`, `RunnerReplay`, `ReferenceSequences`, `RunnerBodyHUD`, `DebugKeys` | `Scripts/Physical/` | Sistem. |
| `PhysicalSetup.cs` | `Scripts/Physical/Editor/` | **Tools → Speed Stars → Set Up PhysicalTest Scene**: eksik bileşenleri ekler, referansları atar; ayar değerlerine dokunmaz. |
| Poz asset'leri | `Physical/Poses/` | Ready, Run, Stumble |

**Tuşlar:** **N** sol ayak, **M** sağ ayak (dokunmatikte ekranın sol/sağ yarısı; Game penceresi seçili olmalı) ·
**1–4** video dizisi · **Esc** durdur · **H** HUD. Bitişte otomatik başa döner.

---

## 8. Inspector alanları — tek tek

Her alanın İngilizce açıklaması kodda `Tooltip` olarak da var (Inspector'da üstüne gelince görünür).

### 8.1 RunnerBody

| Alan | Ne işe yarar |
|---|---|
| `input` | Basışların geldiği `RunnerInput`. |
| `acceptInput` | Kapalıyken basışlar yok sayılır. |
| `tuning` | Bütün fizik sayıları (aşağıdaki gruplar). |

### 8.2 RunnerBody → Tuning (gerçek hareket, `RunnerSim.cs`)

| Grup | Alan | Ne işe yarar |
|---|---|---|
| Speed | `maxSpeed` | Tavan hız (m/s); itişler ona yaklaştıkça azalır. |
| | `pushGain` | Dururken tam bir itişin kazandırdığı hız. |
| | `pushTime` | İtişin kazancının yayıldığı süre (sn). |
| | `dragConstant` / `dragLinear` | Sabit ve hızla artan yavaşlama. |
| Stride | `stanceTime` | Basıştan kalkışa ayağın yerde ittiği süre. |
| | `hopVelocity` / `hopAtRest` | Görünmeyen uçuşun kalkış hızı (max hızda / dururken oranı) → uçuş süresi. |
| | `gravity` | Uçuşun ve dalışta gövdenin düşüşünün yerçekimi. |
| Push efficiency | `togetherWindow` | Diğer ayaktan bu kadar sonra gelen basış erken sayılmaz. |
| | `earlyEfficiency` | Havada / diğer ayak iterken basışın verimi. |
| | `lateGrace`, `lateFloorTime`, `lateFloor`, `lateSpeed` | İnişten sonra geç basışın cezası: ne zaman başlar, ne zaman tabana iner, taban, hangi hızdan tam sayılır. |
| | `legReadyExt` | Basılan bacak hâlâ geride ise verim düşüşünün aralığı. |
| | `sameLegFactor` | Aynı bacağa 1 sn içinde iki kez basmanın çarpanı. |
| Hold | `minStepTime` | Fizikte "tutma" eşiği (görsel kullanmaz). |
| | `holdThrust`, `holdThrustTime` | Basılı ayak başına itiş ve süresi. |
| | `holdDrag` | Tutmada ek kayıp. |
| Springs | `extRise/extFall`, `holdRise/holdFall`, `lean` | Fiziğin bacak ve eğim yayları. |
| Lean | `lungeLean`, `runLean`, `leanKick`, `stumbleKick`, `releaseKick` | Fiziğin kendi eğimi (sadece oyun mantığı). |
| | `stumbleLean` | Bunun üstünde Stumble pozu karışır (görsel). |
| | `maxForwardLean` / `maxBackLean` | Fizik eğim yayının sınırları (HUD). |
| Head | `headBack`, `headBackSpeed` | Tuş yokken başın dinlendiği geri açı ve hangi hızlarda devreye girdiği. |
| | `headKick` | Tuşun ilk anının öne itişi (°). |
| | `holdPush` | Basılı kaldıkça öne gitme hızı (°/sn × tutma ağırlığı). |
| | `headReturnSpring` | Tuş yokken dinlenme yerine dönüş yayı. |
| | `headSettleSpring` | Dinlenme yerini geçince yumuşak dönüş (geriye oturma). |
| | `headReturnDelay` | Tuş kalktıktan sonra dönüşün tam güce çıkma süresi (hızlı bas-çekte birikme). |
| | `headForwardMax` | Başın en fazla öne gideceği açı. |
| | `headNod` | Her basışın küçük sallanması. |
| | `headSpring` | Başın hedefini izlediği yay. |
| | `holdIn` / `holdOut` | Tuşun tutma ağırlığının dolma / boşalma yayı. |
| Head → speed | `headSpoil`, `headSpoilPower` | Baş öndeyken basış veriminin düşüşü (eşik yok). |
| | `headDrag` | Baş en öndeyken ek yavaşlama. |
| Energy | `energyMax`, `drainPerPush`, `drainHoldPerSecond`, `regenPerSecond`, `lowEnergy`, `lowEnergyFactor` | Enerji tüketimi, dolumu ve düşük enerjide itiş gücü. |

### 8.3 RunnerPoseDriver (görsel)

| Başlık | Alan | Ne işe yarar |
|---|---|---|
| (üst) | `body` | Çizilen fizik (boşsa aynı objeden). |
| Rig | `hipTarget`, `spineRootTarget`, `spineTipTarget` | Kalça, bel dibi, belin üstü hedefleri. |
| | `leftLeg` / `rightLeg` | IK target, hint, kemikler (boşsa isimle bulunur). |
| | `leftArm` / `rightArm` | Kol kemikleri ve (kapatılan) IK hedefi. |
| Key poses | `ready`, `run`, `stumble` | Poz asset'leri (boşsa koddaki varsayılan). |
| Blend | `runPoseSpeed` | Ready → Run'ın tamamlandığı hız. |
| | `stumbleBlendRange` | Stumble'ın devreye girme aralığı. |
| Leg angles | `crouch`, `runHome`, `pushEnd`, `recover`, `mid`, `reach`, `holdBack`, `holdSupport` | Fazların uyluk / diz açıları (bölüm 4 tablo). |
| | `maxKnee` | Diz hiç tam düz olmaz. |
| | `heldLegMaxAngle` | Tutulan bacağın en fazla kalkacağı açı (90 = yatay). |
| | `heldLegArch` | Tutulan bacağın gövde çizgisini en fazla geçeceği açı. |
| Leg timing | `pushTime`, `recoverTime`, `midTime`, `homeTime`, `frontHomeTime`, `airTimeAtSpeed` | Faz süreleri. |
| | `restToBaseSpeed` | Tuş basılıyken ev: altında `crouch`, üstünde `runHome`. |
| | `stanceBlend` | Koşu duruşu ↔ çömelme geçiş hızı. |
| Body height | `heightSpring` | Kalçanın bacak yüksekliğini izlediği yay. |
| | `liftSpring`, `liftBelow`, `liftRange` | Yerden kalkarken yumuşak yay ve devreye girdiği alçaklık. |
| | `liftKick` | Son ayak kalkarken gövdeye yukarı hız. |
| | `airTip`, `airTipBack` | Taşıyan yokken öne devrilme hızı; taşınınca geri dönüş. |
| | `settleSpring`, `settleHeadUp` | Dalışta baş yere değince gövdenin yere yerleşmesi; yerleşince başın kalkışı. |
| Ground | `headRest`, `kneeRest` | Baş ve dizin yerdeki yüksekliği. |
| Foot | `pitchBack` | Tutulan ayağın açısı. |
| Foot motion | `accelProfile`, `maxSpeedProfile` | Hızlanma / max hız ayak profilleri: `touchdownPitch`, `stancePitch`, `toeRoll`, `swingAnkle`, `swingToe`. |
| | `accelProfileSpeed`, `maxProfileSpeed` | Profillerin tam kullanıldığı hızlar. |
| Toes | `toeLength`, `toeUpMax`, `toeDownMax` | Parmak uzunluğu ve sınırları. |
| Ankle | `ankleUpMax`, `ankleDownMax` | Bilek sınırları. |
| Head & neck | `neckMax` | Boynun kendi başına en fazla bükülmesi. |
| | `headLead` | Başın gövdenin en fazla önüne geçmesi. |
| | `headKeep` | Geriye oturmada başın aldığı pay (ileri bakar). |
| | `heelRock` | Geriye oturmada ayakların topuğa kalkma payı. |
| | `neckTrim`, `neckTrimLean` | Öne eğilmede boyundan geri alınan açı ve tam devreye girdiği eğim. |
| | `hipFollow`, `hipFollowSpring` | Gövdenin baş açısını izleme oranı ve yayı (belin üstü). |
| Body | `waistAngle`, `waistTopAngle` | Duruşta bel dibi ve belin üstünün açısı. |
| | `hipShare` | Öne eğimin kalçanın aldığı payı. |
| | `waistBaseShare`, `waistTopShare` | Belin dibinin ve üstünün kavis payı. |
| | `waistSpring` | Zincirde her halkanın bir üsttekini izleme yayı. |
| | `waistBackMax` | Belin geriye en fazla bükülmesi. |
| | `pelvisPitchRange` | Kalçanın öne eğim sınırı. |
| Arms | `armBack`, `elbowBack` | Tam geride kolun ve dirseğin sabit açısı. |
| | `armOutward` | Kolların yana açısı. |
| | `shoulderFollow` | Köprücük kemiğinin kol salınımını izlemesi. |
| Preview | `preview`, `previewPose`, `previewLean` | Play'de tek pozu sabit tutup asset'i canlı düzenleme. |

### 8.4 Diğerleri

| Bileşen | Alan | Ne işe yarar |
|---|---|---|
| RaceController | `body`, `raceDistance`, `finishLine` | Koşucu, yarış uzunluğu, bitiş çizgisi objesi. |
| RunnerBodyHUD | `body`, `race`, `replay`, `window`, `visible` | Gösterilen koşucu/yarış/diziler, grafik süresi, başta açık mı. |
| RunnerReplay | `input`, `race` | Dizinin enjekte edildiği girdi; oynatmadan önce başa alma. |
| Poz asset'i (`PoseData`) | `pelvis`, `pelvisPitch`, `headDown`, `width`, `leftArm`/`rightArm` | Kalça kayması ve temel açısı, baş gövdeyi izlesin mi, ayak açıklığı, kol duruşu. |

**Pozu canlı düzenleme:** Play → `RunnerPoseDriver` → **Preview** aç → `Preview Pose`'a poz sürükle → asset'in
değerlerini değiştir; ScriptableObject olduğu için play'den çıkınca kalır. Sağ tık → **Mirror** sol/sağı takas eder.
