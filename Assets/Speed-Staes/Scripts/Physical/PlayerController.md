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
                                                                           │  └ FootCycle × 2 │
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
| **Bacak `hold` yayı** (her bacak) | Yay, 0 = koşu, 1 = tutma pozu | Basılı tutunca yavaşça 1'e çıkar, bırakınca hızla 0'a iner. Görselde tutmanın ne kadar "oturduğunu" bu belirler (belin kayması, eğim, kollar). |
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
| **birlikte** | Diğer ayaktan hemen sonra (`togetherWindow`, 0.08 sn içinde) | Özel durum değil: erken sayılmaz, normal değerlendirilir (hızlı koşuda basışlar 0.05–0.08 sn arayla gelir) |

Ek cezalar: bastığın bacak hâlâ geride ise verim düşer; aynı bacağa 1 sn içinde iki kez basmak ×0.4.

### Tuning (Inspector → RunnerBody → Tuning) — en çok kullanılanlar

| Grup | Alan | Ne yapar |
|---|---|---|
| Speed | `maxSpeed`, `pushGain`, `dragConstant`, `dragLinear` | Tavan hız, bir itişin kazandırdığı hız, yavaşlama |
| Stride | `stanceTime`, `hopVelocity`, `hopAtRest`, `gravity` | İtiş süresi ve görünmeyen uçuş süresi (dururken / hızlıyken). Uçuş sadece "erken basış" kararı için; ekrandaki inip kalkma `RunnerPoseDriver → Body height`'tan gelir |
| Push efficiency | `earlyEfficiency`, `lateGrace`, `lateFloorTime`, `lateFloor`, `sameLegFactor` | Erken/geç basış ne kadar cezalı |
| Hold | `minStepTime`, `holdThrust`, `holdThrustTime` | Ne kadar basılı tutunca "tutma" sayılır, basılı ayak başına tutma itişi (iki ayak = kendiliğinden 2 kat) |
| Springs | `extRise/Fall`, `holdRise/Fall`, `lean` | Yayların hızı (Hz) ve sönümü |
| Lean | `lungeLean` (basılı ayak başına), `leanKick`, `stumbleKick`, `stumbleLean`, `releaseKick` | Fiziğin kendi eğimi — sadece oyun mantığı (sendeleme, hız kaybı); görseldeki eğim `RunnerPoseDriver → Waist`'ten gelir |
| Energy | `drainPerPush`, `drainHoldPerSecond`, `lowEnergy` | Enerji tüketimi |

Bu sayılar videolara göre ayarlı: yavaş dokunuş ~3.4 m/s, 0.32 sn ritim ~7.8 m/s, hızlı ezme ~2.7 m/s'ye
düşüş, tek ayak tutma ~1.4 m/s, iki ayak ~2.2 m/s. Ayar sonrası **1–4** ile diziyi oynatıp HUD'dan kontrol et.

---

## 4. RunnerPoseDriver — görsel

Her karede dört katman üst üste konur:

| Sıra | Katman | Kaynak | Ne yapar |
|---|---|---|---|
| 1 | **Pozlar** | `Physical/Poses/*.asset` | **Ready** → (hızlandıkça) **Run** → (çok eğilince) **Stumble**. Tutma pozu **yok**: tek ayak ve dalış ayaklardan kendiliğinden oluşur (aşağıda "Tutma"). |
| 2 | **Adım döngüsü** (ayaklar) | `FootCycle` + sürücüdeki eşli adım kuralları | Ayaklar hep **eşli** ve **aynı hızda** hareket eder; süre sabit değil, yol ÷ hız. Basılan ayak alt yaydan Back'e iter (Step), diğeri aynı anda üst yaydan havada Front'a gelir (Reach); yolu kısa olan önce varır. Tuş bırakılınca **iki ayak aynı anda** eve döner (önde olan yerden geriye, arkada olan havadan öne); yakın olan önce varır, kimse kimseyi beklemez. **Ev hıza bağlı:** dururken Rest (çömelme), koşarken taban (koşu yüksekliği); yavaşladıkça Rest'e kayar, gövde de yavaşça çöker. Tutma başlayınca ayaklar döngüden tutma pozuna geçer. |
| 3 | **Eğim (bel)** | `RunnerPoseDriver → Waist` (kendi yayı) | Belin açısını **görsel kendi verir**; fizikteki `RunnerSim.Lean` sadece oyun mantığında (sendeleme, hız kaybı) kalır. Hedef açı = **ayaklara göre ortalama** (ayaklar evlerinin gerisindeyse öne, önündeyse geriye; çok sık basma ayakları geride tuttuğu için beli öne verir) + **basılı ayak başına `holdLean`** (iki ayak = 2 kat) + hıza göre biraz öne. Her basış beli biraz öne iter (`stepKick`), yay küçük bir yaylanmayla ortalar; tutma bitince hedef düşer ve yay geriye taşar (oturma). **Öne eğim belden olur:** kalça yerdeki ayağın üstünde kalır (eğimin sadece `pelvisWaistShare` kadarını alır), bel ve üst gövde eğimin tamamını alır. Tutulan bacak kalça etrafında yukarı kalkar; gövde öne indikçe arka bacak kalkar ("uçak" pozu). Böylece tutmada ve çok sık basmada kalça öne fırlamaz. **Hiçbir ayak yerde değilse** (dalış) öne eğim bütün gövdeyi ayakların etrafında yatırır. **Geriye eğim** (bırakma sonrası yaylanma) bütün gövdeyi ayakların etrafında geriye oturtur. |
| 4 | **Gövde yüksekliği** | Ayakların yere teması | Yerdeki ayak gövdeyi taşır. Hiç basılmıyorken iki ayak **Rest** noktasında yere basar; Rest gövdeye göre yukarıda olduğu için gövde o kadar **çöker**. İlk basışta ayaklar döngüye geçip yerden kesilir, gövde zıplar gibi kalkar; iki ayak Rest'te buluşunca tekrar iner. Koşarken sadece **tabandaki** ayak yerdedir. Önde (Front) ya da arkada bekleyen ayak **havada kalır**, gövdeyi aşağı çekmez. Tabandan itişe başlayan ayak gövdeye göre yükseldikçe gövde onun üstüne iner; `stanceCompression` kadar yükselince kalkar ve itiş gövdeyi yukarı atar (iyi basış = daha yüksek). Hiçbir ayak yerde değilken kısa bir uçuş olabilir. Ayrı bir zıplama yok. |

### Tutma (poz yok, ayaklardan oluşur)

| | Tek ayak basılı | İki ayak birden basılı (dalış — özel durum yok, aynı kurallar) |
|---|---|---|
| **Ayaklar** | Tutulan ayak Back'te; diğeri Front'ta, tutma oturdukça yere iner ve gövdeyi taşır | İkisi de Back'te; gövde yere indikçe ayaklar da yere iner ve yerde kalır |
| **Bel yeri** | İki ayağın ortasından destek ayağının üstüne **yavaşça** kayar (`holdShiftSpring`), `holdCrouch` kadar iner | Ayaklara göre konum alır (iki ayak da geride olduğu için ortalama beli öne verir) |
| **Eğim** | Belden öne (`holdLean` × 1); arka bacak kalça etrafında yukarı kalkar | Belden öne (`holdLean` × 2) + başın çekişi. **Hiçbir ayak gövdeyi taşımıyor** → gövde yerçekimiyle düşer, kalça yerde `pelvisClearance` kadar yukarıda durur; kalça da eğimin tamamını alır. Tutulan bacaklar düzleştirilmez, gövde inerken kendiliğinden katlanır |
| **Baş** | `holdHead` kadar öne, en fazla `oneFootMax`: gövdeyle aynı çizgide, ileri-aşağı bakar | 2 × `holdHead`, tavan yok: baş önden gider, gövde takip eder |
| **İtiş** | Basış + tutma itişi | Her basış kendi itişi + ayak başına tutma itişi (2 kat); özel kural yok |
| **Kollar** | Karşı ayağa kilitli: tutulan ayağın karşı kolu geride | İki ayak geride → iki kol geride (yerdeyken de) |

### Baş ve boyun (hızla aynı kaynak, ters grafik)

Tek bir **baş açısı** var: geride ↔ dik ↔ önde. Basışın sonucu ve verimi fizikten okunur (`LastResult`,
`LastEfficiency`); hız neye göre veriliyorsa baş da ona göre itilir, ama **tersine**: hız az geldikçe baş çok gider.

| Basış | Hız | Baş |
|---|---|---|
| **Tam zamanında / çıkış** | Tam | Dik kalır (geriye düşmeye başlamadan yeni basış gelir) |
| **Geç** | Az | Dikliğe yaklaşır ama biraz geride kalır (`lateRecover`) |
| **Erken / boşa** (havada, bacak hâlâ geride, aynı bacak) | Neredeyse yok | `earlyKick × (1 − verim)` öne itilir; sık basınca itmeler birikir (`headRecover` yetişmez) |

- Adım indikten sonra yeni basış gelmezse baş `headBack`'e doğru geriye düşer (sadece koşarken; çömelmede ileri bakar).
- Her basışta küçük bir sallanma (`headNod`); baş hedefini kendi yayıyla (`headSpring`) gecikerek, hafif taşarak izler.
- Tutulan her ayak başı öne iter (`holdHead`); tek ayakta tavan `oneFootMax`, iki ayakta tavan yok.
- **Boyun** `neckMax`'a kadar alır; aşan kısım **beli çeker** (`trunkFollow`): önce baş gider, gövde arkasından gelir.
  Hazır bir yatma pozu **yok**: çok sık basmada gövde alçalır ama bacaklar koşmaya devam eder (kollar da onlara kilitli).

IK'dan sonra (`LateUpdate`): **kollar** (karşı ayakla çapraz sarkaç, yaylı → gecikmeli/sallanan),
**diz twist düzeltmesi**, **ayak bileği** (baldıra göre açı, sınırlı), **parmaklar** (yerde düz, itişin
sonunda yuvarlanma, havada eğri).

### Inspector başlıkları

| Başlık | İçerik |
|---|---|
| **Rig** | Hip/Spine target'ları, iki bacak (target, hint, kemikler, **Front/Back** adım noktaları), iki kol. Kurulum aracı doldurur. |
| **Key poses** | 3 poz asset'i (Ready, Run, Stumble). Boş bırakılırsa koddaki varsayılan kullanılır. |
| **Holds** | `holdShiftSpring` (tek ayak tutmada belin destek ayağına kayma hızı — yavaş = sürünerek), `holdCrouch` (tam tutmada belin inişi), `heldLegMaxAngle` (tutulan bacağın kalça etrafında en fazla kalkacağı açı; 90 = yatay). |
| **Head & neck** | Baş açısı (°, + = öne). `headBack` (basış gelmezse adım indikten sonra başın düştüğü geri açı), `headFallSpring` (geriye düşme hızı), `lateRecover` (geç basış başı dikliğe ne kadar yaklaştırır; 1 = tam dik), `earlyKick` (verimi 0 olan basışın öne itişi; × (1 − verim) — hız az geldikçe baş çok gider), `headRecover` (öndeki birikimin geri çekilme hızı, °/sn; basışlar bundan hızlıysa birikir), `headNod` (her basışta küçük sallanma, canlılık), `holdHead` (basılı ayak başına öne itiş), `oneFootMax` (tek ayak tutmada tavan; iki ayakta tavan yok), `headSpring` (baş hedefini bu yayla takip eder; sönüm ↓ = daha çok sallanır), `neckMax` (boynun tek başına eğilebileceği en büyük açı; aşan kısım beli çeker. Gövde bu açıya kadar eğikken baş hâlâ ileri bakar), `trunkFollow` (aşan kısmın bele aktarılma oranı). |
| **Falling** | Hazır yatma pozu yok. İki ayak geride tutulunca hiçbir ayak inemez: gövde yerçekimiyle (`RunnerBody → Tuning → gravity`) düşer ve yerde durur. `pelvisClearance` (yerdeyken kalçanın zeminden yüksekliği — gövdenin kalınlığı). Bırakınca ayaklar döner, yerdeki ayak gövdeyi tekrar kaldırır. |
| **Blend** | `runPoseSpeed`: Ready→Run geçişinin tamamlandığı hız. `stumbleBlendRange`: sendeleme pozunun devreye girme aralığı. |
| **Step cycle** | `strideSpeed` (6 m/s: basılan ayak geriye, diğeri öne — ikisi aynı hızda), `returnSpeed` (2.5 m/s: tuş yokken iki ayak birlikte eve), `minMoveTime` (çok kısa yolda ani sıçramayı önler), `restToBaseSpeed` (adımlar sürerken: bu hızların altında ev = Rest/çömelme, üstünde ev = taban/koşu yüksekliği), `stanceKeepTime` (son basıştan sonra koşu duruşu bu kadar korunur; sonra tuş yoksa karakter hız ne olursa olsun **doğrudan Ready çömelmesine** yönelir — arada dik duruş yok), `stanceBlend` (koşu duruşu ↔ çömelme geçiş hızı). |
| **Foot** / **Foot motion** / **Toes** | Tutulan ayağın açısı; hızlanma ↔ max hız ayak profilleri (topukla iniş, itişte topuk kalkışı, havada bilek ve parmak eğrileri); parmak sınırları. |
| **Foot loop** | `loopHeight` (üst yay yüksekliği), `loopSkew` (tepe erken/geç), `kneeHintOffset` (koşarken diz yönü), `groundBlendTime`. Alt yayın derinliği **otomatik**: zemine tek noktada (taban) değecek kadar. |
| **Body height** | `stanceCompression` (itişte gövdenin ayağın üstüne ne kadar ineceği, sonra ayak kalkar), `contactSpring` (yerdeki ayağı yerde tutar), `bodySpring` (kalkıştan sonra gövde), `liftKick` (itişin gövdeye verdiği yukarı hız). |
| **Ankle** | Bilek açısı sınırları. |
| **Waist** | `holdLean` (basılı ayak başına öne açı, ° — iki ayak 2 kat), `balanceGain` (ayaklara göre ortalanmanın gücü), `speedLean` (en yüksek hızdaki öne eğim), `stepKick` (her basışın verdiği küçük yaylanma), `waistSpring` (sönüm düşük = bırakınca daha çok geriye taşar). |
| **Body** | `pelvisWaistShare` (öne eğimin kalçaya düşen payı; gerisi bel), `kneeLeanFollow` (eğimde diz yönü), `maxLegReach` (bacağın gerilip "çıtlamasını" önler). |
| **Arms** | Kol **karşı ayağa kilitli** (sağ kol ↔ sol ayak, sol kol ↔ sağ ayak; yay yok, gecikme yok; tutma ve yerde de). `armFront`/`armBack` (salınım genliği), `elbowSwing` (önde daha bükük, arkada daha az), `elbowOpenBack` (kol geriye giderken gövdenin yanından geçerken dirseğin açılması; öne gelirken açılmaz), `armOutward`, `shoulderFollow`. |
| **Preview** | Play mode'da tek bir pozu sabit tutar (aşağıya bak). |

### Pozlar (`Physical/Poses/`)

Her poz, rig'in dinlenme duruşuna göre **ofsetlerdir** (x = ileri, y = yukarı, metre / derece):

| Alan | Anlamı |
|---|---|
| `pelvis` | Kalçanın ileri/yukarı kayması (negatif y = çömelme) |
| `pelvisPitch`, `trunkPitch` | Pelvis ve belden yukarısının öne eğimi |
| `headDown` | 0 = baş ileri bakar, 1 = gövdeyle aynı çizgide (Ready 0, Run 1: hızlı koşuda baş gövdeyi izler) |
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
**Mirror** sol/sağı takas eder.

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
| **Head Aim Rig** (MultiAim) | `B-head` | PoseDriver → `Head LookAt Target`'ı başın önüne, istenen açıda **konumlandırır** (weight hep 1) | Başın açısı = gövdeyi takip (`neckMax`'tan sonra) + baş açısı (bkz. **Head & neck**). Boyun (Spine Tip) baş açısının yarısını alır. |

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
| Bırakınca geri yaylanma az / çok (görsel) | `RunnerPoseDriver → waistSpring` sönümü; fizikteki `Tuning → releaseKick` sadece oyun mantığını etkiler |
| Tutunca yeterince / çok öne eğiliyor | `RunnerPoseDriver → holdLean` (ayak başına; iki ayak 2 kat) |
| Dokunuşlarda bel çok sallanıyor | `stepKick` ↓, `balanceGain` ↓ |
| Çok sık basmada bel yeterince öne gitmiyor | `balanceGain` ↑ |
| Bırakınca geriye oturma az / çok | `waistSpring` sönümü (↓ = daha çok) |
| Öne eğilirken kalça da çok / az eğiliyor | `RunnerPoseDriver → pelvisWaistShare` |
| Tutmada bel destek ayağına çok hızlı / yavaş kayıyor | `RunnerPoseDriver → holdShiftSpring` |
| Tutmada bel yeterince inmiyor / çok iniyor | `holdCrouch` |
| Tutmada destek ayağı çok önde | Sahnede **Front** noktası (destek ayağı Front'ta yere iner) |
| Beklerken çok az / çok çömeliyor | Sahnede **Rest** noktalarının yüksekliği |
| Beklerken ayaklar gövdeye göre önde / geride | Sahnede **Rest** noktalarının ileri-geri yeri |
| Bekleme duruşunda eğim, kollar, ayak açıklığı | `Poses/Ready.asset` (`pelvisPitch`, `trunkPitch`, kollar, `width`) |
| Koşu duruşu yanlış | `Poses/Run.asset`, `runPoseSpeed` |
| Dalış yeterince yatmıyor | `holdLean` (ayak başına eğim), `holdHead` |
| Dalışta ayaklar yanlış yerde | Sahnede **Back** noktaları (iki ayak Back'te kalır) |
| Tutulan bacaklar çok / az kalkıyor (tek ayak) | `heldLegMaxAngle` |
| Sık basmada baş yeterince öne gitmiyor / çok çabuk gidiyor | `earlyKick` ↑ / ↓, `headRecover` ↓ / ↑ |
| İyi ritimde baş fazla geride / öne-arkaya fazla sallanıyor | `headBack`, `headFallSpring`; `headNod` ↓, `headSpring` sönümü ↑ |
| Geç basışta baş yeterince geride kalmıyor | `lateRecover` ↓ |
| Tek ayak tutmada baş çok / az iniyor | `holdHead`, `oneFootMax` |
| Baş öne gidince gövde takip etmiyor / çok erken takip ediyor | `neckMax` ↓ / ↑, `trunkFollow` |
| Sık basmada gövde yeterince alçalmıyor | `earlyKick` ↑, `headRecover` ↓, `neckMax` ↓ |
| Yerdeyken kalça çok yüksek / alçak | `pelvisClearance` |
| İki ayak tutmada gövde çok hızlı / yavaş düşüyor | `Tuning → gravity` (zamanlamayı da etkiler) |
| Karakter havada kalıyor | `liftKick` ↓, `bodySpring` |
| Adım çok uzun / kısa, arka ayak çok yüksek | Sahnede **Step Points** Front/Back objeleri |
| Öne gelen ayak çok yükseğe kalkıyor | `RunnerPoseDriver → loopHeight` |
| Adımlar hızlı / yavaş | `RunnerPoseDriver → strideSpeed` |
| Tuş bırakınca ayaklar eve çok hızlı / yavaş dönüyor | `RunnerPoseDriver → returnSpeed` |
| Durunca çömelmeye geçiş ani / çok yavaş | `RunnerPoseDriver → stanceBlend` (↓ = daha yumuşak) |
| Tuşu bırakınca çömelmeye çok erken / geç dönüyor | `stanceKeepTime` (yavaş dokunuşlarda bile koşu duruşu kalsın istiyorsan ↑) |
| Durunca önce dik bir duruşa geçiyor | Olmamalı: koşu duruşu sadece adımlar sürerken. `stanceKeepTime` çok büyükse o süre boyunca kalır |
| Koşarken her adımda gövde çömeliyor | `restToBaseSpeed.y` çok yüksek: koşu hızının altına çek |
| Diz yanlış yöne bakıyor | `kneeHintOffset` (koşu), pozdaki `knee` (tutma), `kneeLeanFollow` |
| Bacak gerilip "çıtlıyor" | `maxLegReach` ↓ |
| Ayak bileği / parmak açısı garip | `ankleUpMax/DownMax`, ayak profilleri, `toeUpMax/DownMax` |
| Kollar az / çok sallanıyor | `armFront`, `armBack`, pozdaki `armSwing` |
| Kol geri giderken dirsek çok / az açılıyor | `elbowOpenBack` |
| Gövde eğilince baş çok erken aşağı iniyor | `neckMax` ↑, pozdaki `headDown` |
| Karakter bitişte durmuyor / başa dönüyor | Bilerek öyle: `RaceController`; mesafe `raceDistance` |

---

## 7. Hangi dosya nerede

| Dosya | Klasör | Not |
|---|---|---|
| `RunnerInput.cs`, `SideFollowCamera.cs` | `Scripts/Runner/` | Eski sistemden **kullanılmaya devam eden** iki parça. Değiştirirsen eski TestScene de etkilenir. (`LegCycle.cs` artık yeni sistemde kullanılmıyor; yerine `Physical/FootCycle.cs`.) |
| `RunnerGaitDriver.cs`, `RunnerStepController.cs`, `RunnerMovement.cs`, `RunnerDebugHUD.cs` … | `Scripts/Runner/` | Eski sistem; **PhysicalTest'te kullanılmıyor** (sadece TestScene'de). |
| `Spring`, `FootCycle`, `RunnerSim`, `RunnerBody`, `RunnerPose`, `RunnerPoseDefaults`, `RunnerPoseDriver`, `RaceController`, `RunnerReplay`, `ReferenceSequences`, `RunnerBodyHUD`, `DebugKeys` | `Scripts/Physical/` | Yeni sistem. |
| `PhysicalSetup.cs` | `Scripts/Physical/Editor/` | **Tools → Speed Stars → Set Up PhysicalTest Scene**: eksik bileşenleri ekler, eski sistemin bileşenlerini kaldırır, referansları (rig hedefleri, Step Points, pozlar) yeniden atar. Tuning ve diğer ayar değerlerine dokunmaz. |
| Poz asset'leri | `Physical/Poses/` | Ready, Run, Stumble (LungeLeft, LungeRight, Dive asset'leri klasörde kaldı ama artık kullanılmıyor; silebilirsin) |

**Tuşlar:** ←/→ koş (dokunmatikte ekranın sol/sağ yarısı) · **1–4** video dizisini oynat · **Esc** durdur ·
**H** HUD'u gizle. Bitişi geçince otomatik başa döner (R tuşu yok).
