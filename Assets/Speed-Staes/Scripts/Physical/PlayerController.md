# Oyuncu (Player) ve Rig Rehberi — PhysicalTest sahnesi

Bu dosya `PhysicalTest.unity` sahnesindeki koşucu objesinin (`HumanCharacterDummy_F`) üzerindeki
bütün bileşenleri ve Animation Rigging kurulumunu anlatır: **ne neye yarıyor, kim kimi sürüyor,
bir şeyi değiştirmek istediğinde nereye bakmalısın.**

Yazıldığı tarih: 2026-10-23. Değerler kodun varsayılanlarıdır. Sahnedeki Inspector değeri her zaman
koddakini **ezer**, bu yüzden sahnede farklı görebilirsin.

---

## 1. Büyük resim: tek cümle

> **Girdi → Fizik → Görsel.** Tuşlar `RunnerInput`'a düşer, `RunnerBody` bunları `RunnerSim`'e
> (fizik) verir, `RunnerPoseDriver` fizikten sadece **okur** ve rig hedeflerini (IK) yazar,
> Animation Rigging de kemikleri bu hedeflere çeker.

```
 Klavye ←/→ · ekranın sol/sağ yarısı          RunnerReplay (1–4 tuşları, video dizileri)
                 │                                   │ Inject()
                 ▼                                   ▼
          ┌──────────────┐  zaman damgalı basış/bırakma kuyruğu
          │ RunnerInput  │──────────────────────────────┐
          └──────────────┘                              ▼
                                               ┌──────────────┐   Tapped / RunReset olayları
                                               │  RunnerBody  │──────────────────────┐
                                               │  └ RunnerSim │ (hız, eğim, hop,      │
                                               └──────┬───────┘  bacak yayları, enerji)
                       kökü ileri taşır ◄─────────────┤                               │
                                                      │ sadece okur                   ▼
 RaceController (Race objesi) ◄── okur ───────────────┤                    ┌──────────────────┐
 RunnerBodyHUD  ◄── okur ─────────────────────────────┘                    │ RunnerPoseDriver │
                                                                           │  └ LegCycle × 2  │
                                                                           └────────┬─────────┘
                                         IK hedeflerini yazar (Hip/Spine/Leg target, hint…)
                                                                                    ▼
                                         RigBuilder → Control Rig → constraint'ler → kemikler
                                                                                    ▼
                                    LateUpdate: kollar, diz twist, ayak bileği, parmaklar (doğrudan kemik)
```

**Altın kural:** Görsel taraf (`RunnerPoseDriver`) **asla** hızı ya da zamanlamayı değiştirmez.
Karakter "yavaş gidiyor" diyorsan `RunnerBody → Tuning`'e, "garip görünüyor" diyorsan
`RunnerPoseDriver`'a ya da poz asset'lerine bakarsın.

---

## 2. Koşucu objesindeki bileşenler (Inspector'daki sırayla)

| # | Bileşen | Kimin | Görevi | Ne zaman dokunursun |
|---|---|---|---|---|
| 1 | **Animator** | Unity | Rig'in çalışması için gerekli. Controller ve avatar **yok**; klip oynatmıyor. Animation Rigging, Animator'ün güncelleme döngüsünde çalışır. | Hiç. Silme: rig durur. |
| 2 | **RigBuilder** | Animation Rigging | Hangi rig'lerin çalışacağını listeler. Tek katman: `Control Rig` (aktif). | Rig'i tamamen kapatmak için katmanın `active`'ini kapatırsın. |
| 3 | **RunnerInput** | `Scripts/Runner` (eski sistemden, aynen) | ←/→ ve dokunmatik sol/sağ yarıyı okur, her basış/bırakmayı **kendi zaman damgasıyla** kuyruğa atar. Hiçbir şeyi değerlendirmez. | Tuş ya da dokunma alanını değiştireceksen. |
| 4 | **BoneRenderer** | Animation Rigging | Sahne görünümünde kemikleri çizer (54 kemik). Sadece editör yardımcısı, oyunu etkilemez. | Kemikleri görmek/gizlemek için. |
| 5 | **RunnerBody** | `Scripts/Physical` | Fiziğin sahnedeki yüzü. Kuyruktaki basışları `RunnerSim`'e **tam basış anında** verir, kökü (karakter objesini) `Sim.Distance` kadar ileri taşır. Bütün oyun sayıları **Tuning** alanında. | Hız, itiş, zamanlama, eğim, zıplama, enerji ayarı. |
| 6 | **RunnerPoseDriver** | `Scripts/Physical` | **Sadece görsel.** Pozları karıştırır, adım döngüsünü (ayaklar) çalıştırır, eğimi ve zıplamayı uygular, rig hedeflerini yazar; IK'dan sonra kolları, dizleri, ayak bileklerini ve parmakları pozlar. | Duruş, adım şekli, ayak/kol hareketi. |
| 7 | **RunnerReplay** | `Scripts/Physical` | Videolardan ölçülen basış dizilerini gerçek oyuncu basıyormuş gibi `RunnerInput`'a enjekte eder. **1–4** başlatır, **Esc** durdurur. | Ayar yaparken aynı girdiyi tekrar tekrar denemek için. |
| 8 | **RunnerBodyHUD** | `Scripts/Physical` | Sol üstteki ayar paneli: süre, hız, eğim, son basışın sonucu, canlı grafikler, tekrar oynatma düğmeleri. **H** gizler. | Sadece okuma/ölçüm. |

Sahnedeki diğer ilgili objeler:

| Obje | Bileşen | Görevi |
|---|---|---|
| **Race** | `RaceController` | Yarış akışı: *Ready* (çizgide bekliyor) → ilk basışta *Running* (saat başlar). 100 m geçilince süre `LastRaceTime`'a yazılır ve koşucu **direkt başa döner**. |
| **FinishLine** | Mesh | Bitiş çizgisi görseli. `RaceController` play'de bunu `raceDistance` ileriye koyar. |
| **Main Camera** | `SideFollowCamera` (eski sistemden, aynen) | Yandan takip; sadece koşu ekseninde izler, zıplama kamerayı sallamaz. |

### Execution order (bir karede kim önce çalışır)

| Sıra | Bileşen | Neden bu sırada |
|---|---|---|
| −200 | `RunnerInput` | Önce girdiler toplanır. |
| −150 | `RunnerReplay` | Oynatılan dizi de girdi gibi kuyruğa girer. |
| −100 | `RunnerBody` | Fizik bu karenin girdileriyle ilerler. |
| −50 | `RunnerPoseDriver` (`Update`) | Fiziğin **güncel** hâlini okuyup IK hedeflerini yazar. |
| 0 | `RaceController`, `RunnerBodyHUD` | Okuyucular. |
| — | Animator + Animation Rigging | Kemikleri hedeflere çeker. |
| sonra | `RunnerPoseDriver` (`LateUpdate`) | IK bittikten sonra kollar, diz twist, bilek, parmak. |

---

## 3. RunnerBody ve RunnerSim — fizik

`RunnerSim` saf C# (sahneden habersiz); `RunnerBody` onu çalıştırır. Simülasyon 1/240 sn'lik
küçük adımlarla ilerler. Fizik birkaç basit büyüklükten oluşur:

| Büyüklük | Model | Ne yapar |
|---|---|---|
| **Hız** | Kütle + itme (impulse) + sürtünme | Her basış bir itme; basılı tutmak kısa süre itmeye devam eder; sürtünme sürekli yavaşlatır. |
| **Bacak `ext` yayı** (her bacak) | Yay, 0 = gövde altında, 1 = tamamen geride | Bacağın "hâlâ geride mi" bilgisi. Bacak geri gelmeden aynı bacağa basmak verimsiz. |
| **Bacak `hold` yayı** (her bacak) | Yay, 0 = koşu, 1 = tutma pozu | Basılı tutunca yavaşça 1'e çıkar, bırakınca hızla 0'a iner. Görseldeki hamle/dalış pozlarını bu sürer. |
| **Eğim (`lean`)** | **Az sönümlü yay** (≈1 Hz, ζ≈0.3) | Her itiş gövdeyi öne iter. Bırakınca dik konumu geçip **geriye yaylanır**; bu davranış ayrıca kodlanmadı, yayın doğası. |
| **Zıplama (`hop`)** | Yerçekimi + yukarı itme | **Sadece zamanlama için** (havadayken basmak = "erken"). Ekranda görünmez; gövdenin inip kalkması ayaklardan gelir (bkz. bölüm 4). |
| **Enerji** | Basış ve tutma azaltır | 25'in altında itişler zayıflar. Dururken dolar. |

### Basış nasıl değerlendirilir (zamanlama eğrisi yok)

Basışın verimi **fiziksel durumdan** çıkar:

| Sonuç (HUD) | Ne zaman | Etkisi |
|---|---|---|
| **çıkış** | Dururken ilk basış | Tam itiş |
| **tam zamanında** | Önceki adım yere indikten hemen sonra | Tam itiş (≈0.3 sn ritim) |
| **geç** | İnişten çok sonra | İtiş azalır (hızlıyken daha çok) |
| **erken (havada)** | Karakter havadayken ya da diğer ayak hâlâ iterken | Neredeyse boşa gider **ve** gövdeyi öne iter → çok hızlı basınca karakter sendeler, yavaşlar |
| **iki ayak** | İki tuşa aynı anda (0.06 sn içinde) | Dalış: daha güçlü itiş |

Ek cezalar: bastığın bacak hâlâ geride ise verim düşer; aynı bacağa 1 sn içinde iki kez basmak ×0.4.

### Tuning (Inspector → RunnerBody → Tuning) — en çok kullanılanlar

| Grup | Alan | Ne yapar |
|---|---|---|
| Speed | `maxSpeed`, `pushGain`, `dragConstant`, `dragLinear` | Tavan hız, bir itişin kazandırdığı hız, yavaşlama |
| Stride | `stanceTime`, `hopVelocity`, `hopAtRest`, `gravity` | İtiş süresi ve görünmeyen uçuş süresi (dururken / hızlıyken). Uçuş sadece "erken basış" kararı için; ekrandaki inip kalkma `RunnerPoseDriver → Body height`'tan gelir |
| Push efficiency | `earlyEfficiency`, `lateGrace`, `lateFloorTime`, `lateFloor`, `sameLegFactor` | Erken/geç basış ne kadar cezalı |
| Hold | `minStepTime`, `holdThrust`, `holdThrustTime`, `bothMultiplier` | Ne kadar basılı tutunca "tutma" sayılır, tutmanın itişi |
| Springs | `extRise/Fall`, `holdRise/Fall`, `lean` | Yayların hızı (Hz) ve sönümü |
| Lean | `lungeLean`, `diveLean`, `leanKick`, `stumbleKick`, `stumbleLean`, `releaseKick` | Hamle/dalış eğimi, itişte öne tekme, sendeleme eşiği, bırakınca geri tekme |
| Energy | `drainPerPush`, `drainHoldPerSecond`, `lowEnergy` | Enerji tüketimi |

Bu sayılar videolara göre ayarlı: yavaş dokunuş ~3.4 m/s, 0.32 sn ritim ~7.8 m/s, hızlı ezme ~2.7 m/s'ye
düşüş, tek ayak tutma ~1.4 m/s, iki ayak ~2.2 m/s. Ayar sonrası **1–4** ile diziyi oynatıp HUD'dan kontrol et.

---

## 4. RunnerPoseDriver — görsel

Her karede dört katman üst üste konur:

| Sıra | Katman | Kaynak | Ne yapar |
|---|---|---|---|
| 1 | **Pozlar** | `Physical/Poses/*.asset` | **Ready** → (hızlandıkça) **Run** → (çok eğilince) **Stumble**; sonra tutma köşeleri: sol tutulu = **LungeLeft**, sağ = **LungeRight**, ikisi = **Dive**. |
| 2 | **Adım döngüsü** (ayaklar) | Eski sistemin `LegCycle` mantığı | Basılan ayak alt yaydan Back'e iter (Step), diğeri üst yaydan havada Front'a gelir ve önce varır (Reach). Bırakınca iki ayak da **Rest** (bekleme) noktasına döner. Koşarken ayak tabana iner; beklerken Rest'te durur. Tutma başlayınca ayaklar döngüden tutma pozuna geçer. |
| 3 | **Eğim** | `RunnerSim.Lean` | Bütün gövde yerdeki ayağın etrafında öne/geri döner; tutulan ayak gövdeyle birlikte döner. |
| 4 | **Gövde yüksekliği** | Ayakların yere teması | Yerdeki ayak gövdeyi taşır. Hiç basılmıyorken iki ayak **Rest** noktasında yere basar; Rest gövdeye göre yukarıda olduğu için gövde o kadar **çöker**. İlk basışta ayaklar döngüye geçip yerden kesilir, gövde zıplar gibi kalkar; iki ayak Rest'te buluşunca tekrar iner. Koşarken sadece **tabandaki** ayak yerdedir. Önde (Front) ya da arkada bekleyen ayak **havada kalır**, gövdeyi aşağı çekmez. Tabandan itişe başlayan ayak gövdeye göre yükseldikçe gövde onun üstüne iner; `stanceCompression` kadar yükselince kalkar ve itiş gövdeyi yukarı atar (iyi basış = daha yüksek). Hiçbir ayak yerde değilken kısa bir uçuş olabilir. Ayrı bir zıplama yok. |

IK'dan sonra (`LateUpdate`): **kollar** (karşı ayakla çapraz sarkaç, yaylı → gecikmeli/sallanan),
**diz twist düzeltmesi**, **ayak bileği** (baldıra göre açı, sınırlı), **parmaklar** (yerde düz, itişin
sonunda yuvarlanma, havada eğri).

### Inspector başlıkları

| Başlık | İçerik |
|---|---|
| **Rig** | Hip/Spine target'ları, iki bacak (target, hint, kemikler, **Front/Back** adım noktaları), iki kol. Kurulum aracı doldurur. |
| **Key poses** | 6 poz asset'i. Boş bırakılırsa koddaki varsayılan kullanılır. |
| **Blend** | `runPoseSpeed`: Ready→Run geçişinin tamamlandığı hız. `stumbleBlendRange`: sendeleme pozunun devreye girme aralığı. |
| **Step cycle** | `stepDuration` 0.12, `reachDuration` 0.08, `returnDuration` 0.25, `frontReturnDuration` 0.15 (sn). |
| **Foot** / **Foot motion** / **Toes** | Tutulan ayağın açısı; hızlanma ↔ max hız ayak profilleri (topukla iniş, itişte topuk kalkışı, havada bilek ve parmak eğrileri); parmak sınırları. |
| **Foot loop** | `loopHeight` (üst yay yüksekliği), `loopSkew` (tepe erken/geç), `kneeHintOffset` (koşarken diz yönü), `groundBlendTime`. Alt yayın derinliği **otomatik**: zemine tek noktada (taban) değecek kadar. |
| **Body height** | `stanceCompression` (itişte gövdenin ayağın üstüne ne kadar ineceği, sonra ayak kalkar), `contactSpring` (yerdeki ayağı yerde tutar), `bodySpring` (kalkıştan sonra gövde), `liftKick` (itişin gövdeye verdiği yukarı hız). |
| **Ankle** | Bilek açısı sınırları. |
| **Body** | `kneeLeanFollow` (eğimde diz yönü), `maxLegReach` (bacağın gerilip "çıtlamasını" önler). |
| **Secondary motion** | Kol yayı, `headFollowLean` (baş hangi eğimde aşağı bakar). |
| **Arms** | Kol salınım genliği öne/geriye, dirsek, kolun dışa açısı, omuz takibi. |
| **Preview** | Play mode'da tek bir pozu sabit tutar (aşağıya bak). |

### Pozlar (`Physical/Poses/`)

Her poz, rig'in dinlenme duruşuna göre **ofsetlerdir** (x = ileri, y = yukarı, metre / derece):

| Alan | Anlamı |
|---|---|
| `pelvis` | Kalçanın ileri/yukarı kayması (negatif y = çömelme) |
| `pelvisPitch`, `trunkPitch` | Pelvis ve belden yukarısının öne eğimi |
| `headDown` | 0 = baş ileri bakar, 1 = gövdeyle birlikte aşağı |
| `width` | Ayakların (ve dizlerin) yana açıklığı |
| `left` / `right` | Ayak bileği ofseti, ayak açısı, diz ofseti, `withBody` (1 = bu ayak gövdeyle döner, yani tutulan ayak) |
| `leftArm` / `rightArm` | Omuz salınımı, dirsek |
| `armSwing` | Koşarken kolların ne kadar sallanacağı (0 = poz sabit) |

**Ready = bekleme duruşu:** Girdi yokken pelvis/gövde eğimi, kollar ve ayak açıklığı (`width`) bu pozdan gelir.
**Çömelme derinliği buradan gelmez:** Ayakların durduğu yer ve gövdenin ne kadar çökeceği Step Points'teki
**Rest** noktalarından gelir. Ready'deki `pelvis` ve `left/right.ankle` sadece önizlemede ve tutma pozlarına
geçişte kullanılır.

**Pozu canlı düzenleme:** Play'e gir → `RunnerPoseDriver` → **Preview**'u aç → `Preview Pose`'a bir poz
sürükle (`Preview Lean` ile eğimi dene) → poz asset'ini seçip değerleri değiştir. Karakter anında
değişir; ScriptableObject olduğu için **play'den çıkınca da kalır**. Asset'e sağ tık →
**Mirror** sol/sağı takas eder (LungeLeft → LungeRight yapmak için).

---

## 5. Rigging — Control Rig

Kurulum: `RigBuilder` → `Control Rig` (Rig, weight 1). Altındaki her constraint bir **hedef** objeyi
bir **kemiğe** uygular. Kod hedefleri hareket ettirir, constraint kemiği oraya çeker.

```
Control Rig  [Rig]
├─ Hip Rig            [MultiParentConstraint]      Hip target            → B-hips
├─ Spine Rig
│  └─ Twist Spine Rig [TwistChainConstraint]       Spine Root/Tip Target → B-spine … B-neck zinciri
├─ Leg Rig
│  ├─ Left Leg        [TwoBoneIKConstraint]        Left Leg Target/Hint  → B-thigh.L, B-shin.L, B-foot.L
│  │                  [MultiReferentialConstraint] (weight 0, kullanılmıyor)
│  └─ Right Leg       (aynısı, .R)
├─ Hand Rig
│  ├─ Left Hand       [TwoBoneIKConstraint]        → B-upperArm.L, B-forearm.L, B-hand.L  (runtime'da KAPALI)
│  └─ Right Hand      (aynısı, .R)
└─ Head Aim Rig       [MultiAimConstraint]         Head LookAt Target    → B-head
```

| Constraint | Kemik(ler) | Hedefi kim yazar | Not |
|---|---|---|---|
| **Hip Rig** (MultiParent) | `B-hips` | PoseDriver → `Hip target` (konum + dönüş) | Kalçanın yeri ve eğimi; bütün gövde buna bağlı. Offset tutmaz, hedef = kemik. |
| **Twist Spine Rig** (TwistChain) | `B-spine` → `B-chest` → `B-neck` | PoseDriver → `Spine Root Target` ve `Spine Tip Target` (sadece dönüş) | Bel ve göğüs eğimi; dönüş zincire dağıtılır. |
| **Left/Right Leg** (TwoBoneIK) | uyluk, baldır, ayak | PoseDriver → `Leg Target` (ayak bileği konumu) ve `Leg Hint` (diz yönü) | Sahnede rotation weight 1 görünür, **play'de kod 0'a çeker**: IK sadece bileği yerleştirir, ayak açısını sonra kod verir. |
| **MultiReferential** (bacaklarda) | — | — | Ayak pivotları (Toe/Ball/Heel Pivot) için hazır gelmiş; **weight 0, kullanılmıyor**. |
| **Left/Right Hand** (TwoBoneIK) | omuz, dirsek, el | — | **Play'de kod weight'i 0 yapar.** Kollar IK ile değil, LateUpdate'te kemik dönüşüyle sarkaç olarak sürülür. |
| **Head Aim Rig** (MultiAim) | `B-head` | Hedef sabit (`Head LookAt Target`, önde); PoseDriver sadece **weight**'i değiştirir | Dikken baş ileriye bakar; gövde eğildikçe weight düşer, baş gövdeyle aşağı iner. |

**Step Points** (rig'in dışında, koşucunun altında): `Left/Front`, `Left/Back`, `Right/Front`, `Right/Back`.
Ayak topunun (parmak eklemi) adım döngüsünde gittiği **ön** ve **arka** noktalar. İkisi de havadadır.
Aralarındaki alt yay zemine **tek bir noktada** değer: **taban** (sarı küre, "Taban" yazısı); koşarken
ayak oraya iner. Front/Back'i sürükledikçe taban da kayar.

Her ayağın bir de **Rest** noktası var (`Step Points/Left/Rest`, `Right/Rest`; camgöbeği küre, "Bekleme"
yazısı). Girdi yokken ayak orada yere basar. Rest'in **yüksekliği = çömelme derinliği**: yukarı sürüklersen
karakter daha derin çöker. **İleri-geri yeri** ayakların gövdeye göre yerini belirler (öne alırsan kalça
geride kalır). Küreden zemine inen çizgi gövdenin ne kadar ineceğini gösterir.

Döngü sahnede **play'e girmeden de** çizilir: yeşil = Front, kırmızı = Back, sarı = taban, camgöbeği = Rest,
üst yay = havadan öne, alt yay = itiş.

**Rig'de kodun kemiğe doğrudan dokunduğu yerler** (IK'dan sonra, constraint'siz): `B-upperArm`,
`B-forearm`, `B-shoulder` (kollar), `B-thigh`/`B-shin` (diz twist), `B-foot` (bilek açısı), `B-toe` (parmak).

---

## 6. Hızlı arama: sorun → nereye bak

| Gördüğün | Bakılacak yer |
|---|---|
| Çok yavaş / çok hızlı hızlanıyor | `RunnerBody → Tuning → pushGain`, `dragConstant` |
| Tavan hız | `Tuning → maxSpeed` |
| Ritim basışları "geç" sayılıyor | `Tuning → lateGrace`, `hopVelocity` (uçuş süresi ritmi belirler) |
| Hızlı basınca yeterince cezalanmıyor | `Tuning → earlyEfficiency` ↓, `stumbleKick` ↑ |
| Gövde itişte çok az / çok iniyor | `RunnerPoseDriver → stanceCompression` |
| Ayak kalkınca gövde az / çok yükseliyor | `RunnerPoseDriver → liftKick`, `bodySpring` |
| Ayakların buluştuğu yer (taban) çok önde / arkada | Sahnede Step Points **Front/Back** yükseklikleri ve yerleri (sarı "Taban" küresine bak) |
| Bırakınca geri yaylanma az / çok | `Tuning → releaseKick`, `lean` (damping ↓ = daha çok sallanma) |
| Tutunca yeterince öne eğilmiyor | `Tuning → lungeLean` (tek ayak), `diveLean` (iki ayak) |
| Beklerken çok az / çok çömeliyor | Sahnede **Rest** noktalarının yüksekliği |
| Beklerken ayaklar gövdeye göre önde / geride | Sahnede **Rest** noktalarının ileri-geri yeri |
| Bekleme duruşunda eğim, kollar, ayak açıklığı | `Poses/Ready.asset` (`pelvisPitch`, `trunkPitch`, kollar, `width`) |
| Koşu duruşu yanlış | `Poses/Run.asset`, `runPoseSpeed` |
| Hamle / dalış pozu yanlış | `Poses/LungeLeft`, `LungeRight`, `Dive` |
| Adım çok uzun / kısa, arka ayak çok yüksek | Sahnede **Step Points** Front/Back objeleri |
| Öne gelen ayak çok yükseğe kalkıyor | `RunnerPoseDriver → loopHeight` |
| Adımlar hızlı / yavaş | `RunnerPoseDriver → stepDuration`, `reachDuration`, `returnDuration`, `frontReturnDuration` |
| Diz yanlış yöne bakıyor | `kneeHintOffset` (koşu), pozdaki `knee` (tutma), `kneeLeanFollow` |
| Bacak gerilip "çıtlıyor" | `maxLegReach` ↓ |
| Ayak bileği / parmak açısı garip | `ankleUpMax/DownMax`, ayak profilleri, `toeUpMax/DownMax` |
| Kollar az / çok sallanıyor | `armFront`, `armBack`, pozdaki `armSwing`, `armSpring` |
| Baş çok erken aşağı iniyor | `headFollowLean` ↑, pozdaki `headDown` |
| Karakter bitişte durmuyor / başa dönüyor | Bilerek öyle: `RaceController`; mesafe `raceDistance` |

---

## 7. Hangi dosya nerede

| Dosya | Klasör | Not |
|---|---|---|
| `RunnerInput.cs`, `LegCycle.cs`, `SideFollowCamera.cs` | `Scripts/Runner/` | Eski sistemden **kullanılmaya devam eden** üç parça. Değiştirirsen eski TestScene de etkilenir. |
| `RunnerGaitDriver.cs`, `RunnerStepController.cs`, `RunnerMovement.cs`, `RunnerDebugHUD.cs` … | `Scripts/Runner/` | Eski sistem; **PhysicalTest'te kullanılmıyor** (sadece TestScene'de). |
| `Spring`, `RunnerSim`, `RunnerBody`, `RunnerPose`, `RunnerPoseDefaults`, `RunnerPoseDriver`, `RaceController`, `RunnerReplay`, `ReferenceSequences`, `RunnerBodyHUD`, `DebugKeys` | `Scripts/Physical/` | Yeni sistem. |
| `PhysicalSetup.cs` | `Scripts/Physical/Editor/` | **Tools → Speed Stars → Set Up PhysicalTest Scene**: eksik bileşenleri ekler, eski sistemin bileşenlerini kaldırır, referansları (rig hedefleri, Step Points, pozlar) yeniden atar. Tuning ve diğer ayar değerlerine dokunmaz. |
| Poz asset'leri | `Physical/Poses/` | Ready, Run, LungeLeft, LungeRight, Dive, Stumble |

**Tuşlar:** ←/→ koş (dokunmatikte ekranın sol/sağ yarısı) · **1–4** video dizisini oynat · **Esc** durdur ·
**H** HUD'u gizle. Bitişi geçince otomatik başa döner (R tuşu yok).
