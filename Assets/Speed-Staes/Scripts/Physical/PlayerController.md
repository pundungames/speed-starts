# Oyuncu (Player) ve Rig Rehberi — PhysicalTest sahnesi

Bu dosya `PhysicalTest.unity` sahnesindeki koşucu objesinin (`HumanCharacterDummy_F`) üzerindeki
bütün bileşenleri ve Animation Rigging kurulumunu anlatır: **ne neye yarıyor, kim kimi sürüyor,
bir şeyi değiştirmek istediğinde nereye bakmalısın.**

Yazıldığı tarih: 2026-10-23, son güncelleme: 2026-10-25 (bacaklar açıyla: uyluk + diz → FK → IK). Değerler kodun varsayılanlarıdır. Sahnedeki Inspector değeri her zaman
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
                                                                           │  └ bacak açıları │
                                                                           └────────┬─────────┘
                                 açılar → FK → IK hedeflerini yazar (Hip/Spine/Leg target, hint…)
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
| 3 | **RunnerInput** | `Scripts/Runner` | ←/→ ve dokunmatik sol/sağ yarıyı okur, her basış/bırakmayı **kendi zaman damgasıyla** kuyruğa atar. Hiçbir şeyi değerlendirmez. | Tuş ya da dokunma alanını değiştireceksen. |
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
| **Main Camera** | `SideFollowCamera` (`Scripts/Runner`) | Yandan takip; sadece koşu ekseninde izler, zıplama kamerayı sallamaz. |

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
| 2 | **Bacak açıları** | `RunnerPoseDriver → Leg angles / Leg timing` | Bacak bir ayak yolu değil, **açıdır**: her bacağın bir uyluk açısı (kalçada, dikeye göre, + = önde) ve bir diz açısı (180 = düz) var. Hareket **uyluktan başlar**, alt bacağa hareket yönüne göre açı verilir; ayak nereye düşerse adım odur. Açılardan FK ile diz ve bilek noktası hesaplanır, IK sadece bunu üretir (target = bilek, hint = dizin önü). Fazlar aşağıda ("Bacak fazları"). Her faz bacağın **o anki açısından** başlar: ortada gelen basış kaldığı yerden geriye devam eder. |
| 3 | **Eğim (kalça)** | `RunnerPoseDriver → Head & neck` (`hipFollow`) + `Body` | Eğim **sadece baştan** gelir: kalça baş açısını baştan sona `hipFollow` oranında, bir yayla (`hipFollowSpring`) takip eder — eşik yok, önce baş gider, kalça arkasından eğilir. Tutma başı sürekli öne ittiği için (`holdPush`) gövde akarak eğilir. Eğimin **tamamını kalça** alır: kalça açısı = pozun `pelvisPitch` + eğim, `pelvisPitchRange` (−10 … 90) içinde. **Bel sabit**: `B-spine` X hep `waistAngle` (6.5), hiç bükülmez. Kalçanın **yüksekliği bacaklardan** gelir, sadece açısı değişir: yerde ayak varken / bacaklar altına toparlanırken gövde kalkar, baş yere değemez. Havadaki ve tutulan bacaklar kalçanın eğimiyle döner (tutulan bacak en fazla `heldLegMaxAngle`); yere basan bacak yere göre kalır (`FollowHips`). Fizikteki `RunnerSim.Lean` sadece oyun mantığında kalır. |
| 4 | **Gövde yüksekliği** | Bacak açıları | Kalça, **en alttaki taşıyan ayak** (tutulmayan her bacak) tam zemine değecek yüksekliğe konur (`heightSpring`). Açı genişleyince bacak uzar → gövde kalkar; kapanınca iner. Çömelme de böyle: `crouch` açıları (diz bükük) gövdeyi alçaltır. Ayak hiçbir zaman zemine girmez (yay geride kalırsa ayak yerde tutulur, IK dizi büker). Havada katlanmış bacak diğerinden yüksek kalır, gövdeyi aşağı çekmez. Yerdeki ayağın hafif kayması kabul. Tutulan bacak taşımaz: **iki ayak tutulunca karakter yere iner** (dalış, aşağıda "İki ayak tutma: dalış"). Ayrı zıplama yok. |

### Bacak fazları (videodan ölçülen başlangıç açıları; Inspector'dan ayarlanır)

| Faz | Ne zaman | Hedef (uyluk / diz) | Sonra |
|---|---|---|---|
| **Rest** | Girdi yok | `crouch` 40° / 100° (basışlar sürerken hızla `runHome`'a kayar) | — |
| **Push** | Bu bacağın tuşu | `pushEnd` −30° / 150°: uyluk geriye, diz açılır, parmak ucu arkada yerde | Tuş basılıysa Hold, değilse Recover |
| **Recover** | İtiş bitti | `recover` −40° / 100°: uyluk biraz daha geriye, alt bacak katlanır (topuk kalça hizasına çıkar) | Mid |
| **Mid** | Kendiliğinden | `mid` 15° / 70°: uyluk biraz önde, diz kapalı (topuk kalçaya yakın) | Rest |
| **Reach** | **Diğer** bacağın tuşu (aynı anda, aynı süre) | `reach` 47° / 175°: bacak neredeyse düz öne açılır, ayak çok önde yere iner (videoda ~47°) | Front |
| **Front** | Reach bitti | Önde yerde; diğer bacak tutuldukça `holdSupport` 80° / 100° (alt bacak dik) | Diğer bacak itişi **ve toparlanmayı** bitirince (Mid'e geçince) ya da bırakılınca Rest (`frontHomeTime`, gövde üstünden geçer) |
| **Hold** | İtiş bittiğinde tuş hâlâ basılı | `holdBack` −45° / 159° (diz hafif bükük) (tutma yayıyla); bel eğildikçe kalça etrafında daha da kalkar (`heldLegMaxAngle` 79; 90 = yatay), tek ayakta da iki ayakta da | Bırakınca Mid |

Süreler: `pushTime` (dururken / en hızlı), `recoverTime`, `midTime`, `homeTime`; havadaki fazlar
hızlıyken `airTimeAtSpeed` kadar kısalır.

### İki ayak tutma: dalış (`UpdateBodyHeight` / `UpdateDive`)

Referans: `Downloads/screen-20261026-000711.mp4` (6.28–6.60 ve 10.28–10.56: tek ayak tutarken ikinci tuş;
18.02–18.62: çömelmeden iki tuş birden). Ayaklar yere yapışmaz; kafa önce yere değer.

| Aşama | Ne olur |
|---|---|
| **Düşüş** | Tutulan bacak taşımaz. Diğeri zaten tutulurken basılan bacak da taşımaz: yerden kalkıp geriye savrulur, gövde yükselmez. Gövde o anki yüksekliği ve hızıyla yerçekimiyle iner; hızlıyken daha yavaş (`diveTime` dururken 0.3 sn → `diveSpeed`'te 0.5 sn). (Çömelmeden iki tuş birden: iki bacak önce birlikte iter.) İki bacak geride, açılı, havada: kalçayla döner, en fazla `heldLegMaxAngle` (79). |
| **Öne yatma** | Düşerken gövde `diveLean`'a doğru öne yatar — zamanla ve düşülen mesafeyle (hangisi öndeyse); eğimi kalça alır, en fazla 90. Böylece kafa kalçadan önce iner. |
| **Kafa yerde** | Kafa kemiği zeminden `headRest` kadar yukarı inince düşüş ve eğim durur (kafa zemine bastırılmaz). Kalça yere değmez; güvenlik için `MinPelvis` altına inmez. |
| **Bırakınca** | Eğim `diveRecover` yayıyla geri gelir, bacaklar Mid → Rest, yere değen ayak gövdeyi kaldırır. |

### Tutma (poz yok, bacak açılarından oluşur)

| | Tek ayak basılı | İki ayak birden basılı (dalış — özel durum yok, aynı kurallar) |
|---|---|---|
| **Bacaklar** | Tutulan bacak `holdBack` (geride, diz hafif bükük) ve gövde öne yattıkça kalça etrafında yataya kadar kalkar; öndeki bacak tutma yayı oturdukça `holdSupport`'a geçer (alt bacak dik) ve gövdeyi taşır | İkisi de geride, açılı, havada (kalçayla döner, en fazla `heldLegMaxAngle` (79)); ikinci basılan bacak yerden kalkıp geriye savrulur |
| **Bel yeri** | Destek bacağının açısından çıkar: yavaşça (tutma yayı) ayağın arkasına-üstüne iner, üstüne çökmez | **Yere iner (dalış):** gövde öne yatarak düşer, kafa yere değince durur; kalça yere değmez |
| **Eğim** | Basılı kaldıkça baş akarak öne gider (`holdPush`, tuş basılıyken geri gelmez), kalça takip eder (`hipFollow`): ~1.5 sn'de kalça 90'a (tavan); arka bacak kalçayla birlikte yataya kalkar | Baş iki kat hızla gider, kalça takip eder + dalış eğimi, en fazla 90 |
| **Baş** | `holdPush` ile `headForwardMax`'a kadar iner (ölçüm: baş kemiği ~0.55 m), bırakınca ~0.34 sn'de dikleşir, geriye taşıp oturur | 2 × `holdPush`: baş önden gider, gövde takip eder, dalışta yere iner |
| **İtiş** | Basış + tutma itişi | Her basış kendi itişi + ayak başına tutma itişi (2 kat); özel kural yok |
| **Kollar** | Tutulan tuşun kolu (karşı taraf) geride, diğeri yerinde | İki ayak geride → iki kol geride (yerdeyken de) |

### Baş ve boyun (tuş basılı → öne, tuş yok → geri; basma ile tutma aynı şey)

Tek bir **baş açısı** var: geride ↔ dik ↔ önde. Her tuşun tek bilgisi var: **basılı mı, değil mi**. Kısa basış
kısa süren bir tutmadır; "bas-çek" ile "basılı tut" arasında ayrım, eşik, mod yok. Basışın verimine de bakılmaz.

- **Tuş basılıyken** baş öne gider: ilk anda hızlı (`headKick`, ~0.1 sn içinde hepsi — her basış aynı miktar),
  sonra tuş basılı kaldıkça `holdPush` × tuşun tutma ağırlığı ile akarak devam eder. Tuş basılıyken baş **geri gelmez**.
- **Hiç tuş basılı değilken** baş bir yayla (`headReturnSpring`, sönüm < 1) `headBack`'e döner: dik konumu geçip
  geriye taşar ve oturur (tutmadan / sık basmadan bırakınca "geriye oturma"). Çömelmede hedef dik.
- **Tutma ağırlığı** (`holdIn` / `holdOut`): tuş basıldığı an dolmaya başlar (eşik yok), bırakınca boşalır. Bacakların
  tutma açıları ve `holdPush` bunu kullanır; fizikteki tutma yayı (`HoldValue`, 0.14 sn eşikli) görselde kullanılmaz.

| Basış aralığı | Baş |
|---|---|
| **Baş geri gelmeden basmak** (çok sık / erken, aralıksız) | İtişler üst üste biner → baş giderek öne yatar → kalça takip eder → eğilme |
| **Ritimde basmak** | İtiş ve geri dönüş dengelenir → baş aşağı yukarı dik, canlı sallanır |
| **Seyrek basmak** | Baş dik konumu geçip `headBack`'e gider |
| **Basılı tutmak** | Baş akarak iner, kalça takip eder; bırakınca dikleşip geriye taşar |

- Her basışta ayrıca küçük bir sallanma (`headNod`); baş hedefini kendi yayıyla (`headSpring`) gecikerek izler.
- **Kalça başı baştan sona takip eder** (`hipFollow` = 1, `hipFollowSpring`), geriye de: baş geriye taşınca kalça da
  geriye yaslanır (`pelvisPitchRange` −40 … 90). Boyun, kalçanın henüz takip etmediği farkı büker (en fazla `neckMax`).

Ölçüm (2026-10-26, play; `headKick` 24, `holdPush` 40, `headReturnSpring` 1 Hz / 0.42; kalça kemiğinin açısı):

| Durum | Kalça | Baş |
|---|---|---|
| Tek basış (dururken) | −27 … +15 | +24 → −22 (sallanıp oturur) |
| Ritim 0.3 sn | 14 … 27, ort. 20 | −2 … +21, ort. +10 |
| Sık basma 0.1 sn | ~86 | 75 … 80 |
| Sık basmayı bırakınca | ~0.34 sn'de dik, −35'e taşar | −37 |
| Tek ayak tutma | ~1.56 sn'de 75 | 80'e kadar |
| Tutmayı bırakınca | ~0.34 sn'de dik, −25'e taşar | −21 |

  Hazır bir yatma pozu **yok**: çok sık basmada gövde alçalır ama bacaklar koşmaya devam eder (kollar da onlara kilitli).

IK'dan sonra (`LateUpdate`): **kollar** (bir tuş = bir bacak + bir kol; ayağa kilitli, yay/gecikme yok),
**diz twist düzeltmesi**, **ayak bileği** (baldıra göre açı, sınırlı), **parmaklar** (yerde düz, itişin
sonunda yuvarlanma, havada eğri).

### Inspector başlıkları

| Başlık | İçerik |
|---|---|
| **Rig** | Hip/Spine target'ları, iki bacak (target, hint, kemikler), iki kol. Kurulum aracı doldurur. |
| **Key poses** | 3 poz asset'i (Ready, Run, Stumble): gövde, baş, kollar, ayak açıklığı. Bacaklar buradan gelmez. Boş bırakılırsa koddaki varsayılan kullanılır. |
| **Leg angles** | Her faz için uyluk / diz açısı (yukarıdaki tablo), `maxKnee` (diz hiç tam düz olmaz; IK tam boyda çıtlar), `heldLegMaxAngle` (tutulan bacağın kalça etrafında en fazla kalkacağı açı; 90 = yatay). |
| **Leg timing** | `pushTime`, `recoverTime`, `midTime`, `homeTime`, `frontHomeTime`, `airTimeAtSpeed`; `restToBaseSpeed` (basışlar sürerken: bu hızların altında ev = `crouch`, üstünde = `runHome`), `stanceKeepTime` (son basıştan sonra koşu duruşu bu kadar korunur; sonra tuş yoksa **doğrudan Ready çömelmesine** — arada dik duruş yok), `stanceBlend`. |
| **Head & neck** | Baş açısı (°, + = öne). `headBack` (tuş yokken başın döndüğü geri açı), `headKick` (tuşun ilk anının öne itişi, verimden bağımsız, ~0.1 sn içinde), `holdPush` (tuş basılı kaldıkça öne gitme hızı, °/sn × tutma ağırlığı; iki tuş 2 kat), `headReturnSpring` (tuş yokken geri dönüş yayı; sönüm < 1 = dik konumu geçip geriye taşar), `headForwardMax` (baş en fazla ne kadar öne), `headNod` (her basışta küçük sallanma), `holdIn` / `holdOut` (tuşun tutma ağırlığı: basıldığı an dolmaya başlar, bırakınca boşalır; eşik yok), `headSpring` (baş hedefini bu yayla izler), `neckMax` (boynun en fazla bükülmesi), `hipFollow` (kalçanın baş açısını takip oranı, baştan sona), `hipFollowSpring` (kalça başı bu yayla izler: baş önden gider). |
| **Blend** | `runPoseSpeed`: Ready→Run geçişinin tamamlandığı hız. `stumbleBlendRange`: sendeleme pozunun devreye girme aralığı. |
| **Body height** | `heightSpring` (kalça, bacakların verdiği yüksekliği bu yayla izler; sönüm < 1 = açılınca hafif süzülme). |
| **Dive** | İki ayak tutma: `diveTime` (dururken / `diveSpeed`'te düşüş süresi), `diveSpeed`, `diveLean` (düşerken gövdenin yattığı eğim; kalça en fazla 90), `headRest` (kafa kemiğinin yerdeki yüksekliği: düşüş burada durur), `diveRecover` (bırakınca geri dönüş yayı). |
| **Foot** / **Foot motion** / **Toes** | Tutulan ayağın açısı; hızlanma ↔ max hız ayak profilleri: `touchdownPitch` (öndeki ayağın açısı: topuk yerde, parmak ucu yukarıda, −17° / −15°; ayak önde durdukça böyle kalır, eve dönerken düzleşir; topuk zemine girmesin diye bilek biraz kalkar, parmaklar ayakla düz kalır), Push'ta topuk kalkışı, Recover+Mid boyunca havada bilek ve parmak eğrileri; parmak sınırları. |
| **Ankle** | Bilek açısı sınırları. |
| **Body** | `waistAngle` (belin kalça üstündeki sabit açısı, `B-spine` X = 6.5; bel bükülmez), `pelvisPitchRange` (kalça açısı sınırı −10 … 90; eğimin tamamı kalçada). |
| **Arms** | **Bir tuş = bir bacak + bir kol** (sağ tuş: sağ bacak + sol kol; sol tuş: sol bacak + sağ kol). Kol o bacağın **uyluk açısına** kilitli, ama sadece uyluğun evin gerisine gidişini izler (basış, tutma, eve dönüş); diğer tuş o bacağı öne uzatırken kol yerinde kalır. Yay yok, gecikme yok. `armBack` / `elbowBack` (ayak tam gerideyken üst kolun ve dirseğin **sabit** açısı — hız ve pozlar bunu değiştirmez, tutarken kol tam burada kalır; önkol üst kolla birlikte geriye döner), `armOutward`, `shoulderFollow`. |
| **Preview** | Play mode'da tek bir pozu sabit tutar (aşağıya bak). |

### Pozlar (`Physical/Poses/`)

Her poz, rig'in dinlenme duruşuna göre **ofsetlerdir** (x = ileri, y = yukarı, metre / derece):

| Alan | Anlamı |
|---|---|
| `pelvis` | Kalçanın ileri kayması (x). y artık kullanılmıyor: yükseklik bacak açılarından gelir |
| `pelvisPitch` | Kalçanın temel açısı (Ready −10 = geride); eğim bunun üstüne eklenir, toplam `pelvisPitchRange` içinde. Bacaklar bu temel açıyla dönmez, sadece eğimle |
| `headDown` | 0 = baş ileri bakar, 1 = gövdeyle aynı çizgide (Ready 0, Run 1: hızlı koşuda baş gövdeyi izler) |
| `width` | Ayakların (ve dizlerin) yana açıklığı |
| `leftArm` / `rightArm` | Kolun duruş açısı (omuz salınımı, dirsek); tam geride kol `armBack`/`elbowBack`'te |

**Ready = bekleme duruşu:** Girdi yokken pelvis/gövde eğimi, kollar ve ayak açıklığı (`width`) bu pozdan gelir.
**Çömelme derinliği buradan gelmez:** `RunnerPoseDriver → Leg angles → crouch` açılarından gelir (diz ne kadar
bükükse gövde o kadar alçak).

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
| **Left/Right Leg** (TwoBoneIK) | uyluk, baldır, ayak | PoseDriver → `Leg Target` (FK'nın bulduğu bilek) ve `Leg Hint` (FK'nın bulduğu dizin önü) | IK karar vermez, bacak açılarını aynen üretir. Sahnede rotation weight 1 görünür, **play'de kod 0'a çeker**: ayak açısını sonra kod verir. |
| **MultiReferential** (bacaklarda) | — | — | Ayak pivotları (Toe/Ball/Heel Pivot) için hazır gelmiş; **weight 0, kullanılmıyor**. |
| **Left/Right Hand** (TwoBoneIK) | omuz, dirsek, el | — | **Play'de kod weight'i 0 yapar.** Kollar IK ile değil, LateUpdate'te kemik dönüşüyle sürülür (karşı ayağa kilitli). |
| **Head Aim Rig** (MultiAim) | `B-head` | PoseDriver → `Head LookAt Target`'ı başın önüne, istenen açıda **konumlandırır** (weight hep 1) | Başın açısı = gövdeyi takip (`neckMax`'tan sonra) + baş açısı (bkz. **Head & neck**). Boyun (Spine Tip) baş açısının yarısını alır. |

Bacaklar açılarla sürülür (adım noktası yok). Play'de sahne görünümünde her bacağın açılardan çıkan hâli
çizilir (kalça eklemi → diz → bilek; mavi = sol, turuncu = sağ).

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
| Adım atarken gövde az / çok yükseliyor | `pushEnd` / `reach` diz açısı (↑ = bacak uzun = gövde yukarı), `heightSpring` |
| Gövde ayaklara geç yetişiyor / çok sallanıyor | `heightSpring` frekansı ↑ / sönümü ↑ |
| Bel bükülüyor gibi | Olmamalı: `waistAngle` sabit |
| Tutmada bel destek ayağına çok hızlı / yavaş iniyor | `Tuning → holdRise` (tutma yayı) |
| Tutmada ön diz çok / az bükük (kalça çok alçak / yüksek) | `holdSupport.knee` (↑ = kalça daha yukarıda) |
| Tutmada destek ayağı çok önde / geride | `holdSupport.thigh` |
| Beklerken çok az / çok çömeliyor | `crouch.knee` (↓ = daha derin) |
| Beklerken ayaklar gövdeye göre önde / geride | `crouch.thigh` (↑ = ayaklar önde) |
| Bekleme duruşunda eğim, kollar, ayak açıklığı | `Poses/Ready.asset` (`pelvisPitch`, kollar, `width`) |
| Koşu duruşu yanlış | `Poses/Run.asset`, `runPoseSpeed` |
| Dalış yeterince yatmıyor | `diveLean`, `holdPush`, `hipFollow` |
| Sık basmada baş yeterince öne gitmiyor / çok çabuk gidiyor | `headKick` ↑ / ↓, `headReturnSpring` frekansı ↓ / ↑, `headForwardMax` |
| Ritimde basınca baş dik kalmıyor (önde / geride) | `headKick` ↓ / ↑ veya `headReturnSpring` frekansı ↑ / ↓; öne-arkaya fazla sallanıyorsa `headReturnSpring` sönümü ↑, `headNod` ↓ |
| Seyrek basınca baş yeterince / çok geriye gidiyor | `headBack` |
| Gövde başı yeterince / çok takip ediyor, çok geç / erken eğiliyor | `hipFollow`; `hipFollowSpring` frekansı |
| Kalça çok / az öne eğiliyor, geriye çok yaslanıyor | `pelvisPitchRange`, `hipFollow`; Ready'de pozdaki `pelvisPitch` |
| Tutunca gövde yeterince / çok / çok yavaş eğiliyor | `holdPush` (hız), `headForwardMax` (ne kadar), `hipFollow` |
| Dalışta ayaklar yanlış yerde | `holdBack` (iki bacak da bu açıda kalır) |
| Tutulan bacaklar çok / az kalkıyor (tek ayak) | `heldLegMaxAngle` |
| Tek ayak tutmada baş çok / az iniyor | `headForwardMax`, `holdPush` |
| Dalışta kalça kafadan önce yere iniyor | `diveLean` ↑ (gövde daha erken yatar), `diveTime` ↓ |
| Dalışta gövde çok hızlı / yavaş iniyor | `diveTime` (dururken / hızlıyken), `diveSpeed` |
| Adım çok uzun / kısa | `pushEnd.thigh` (arka), `reach` (ön: uyluk ve diz) |
| Arka ayak itişten sonra az / çok yükseğe kalkıyor | `recover.knee` (↓ = topuk daha yukarı) |
| Öne gelen bacak çok / az katlanıyor | `mid.knee` |
| Adımlar hızlı / yavaş | `pushTime`, `recoverTime`, `midTime`, `airTimeAtSpeed` |
| Tuş bırakınca bacaklar eve çok hızlı / yavaş dönüyor | `homeTime`, `frontHomeTime` |
| Durunca çömelmeye geçiş ani / çok yavaş | `RunnerPoseDriver → stanceBlend` (↓ = daha yumuşak) |
| Tuşu bırakınca çömelmeye çok erken / geç dönüyor | `stanceKeepTime` (yavaş dokunuşlarda bile koşu duruşu kalsın istiyorsan ↑) |
| Durunca önce dik bir duruşa geçiyor | Olmamalı: koşu duruşu sadece adımlar sürerken. `stanceKeepTime` çok büyükse o süre boyunca kalır |
| Koşarken her adımda gövde çömeliyor | `restToBaseSpeed.y` çok yüksek: koşu hızının altına çek |
| Diz yanlış yöne bakıyor | Olmamalı: hint uyluğun önüne konur. Kemik twist'i için `FixLegTwist` |
| Bacak gerilip "çıtlıyor" | `maxKnee` ↓ |
| Ayak bileği / parmak açısı garip | `ankleUpMax/DownMax`, ayak profilleri, `toeUpMax/DownMax` |
| Kollar az / çok geriye gidiyor | `armBack` |
| Kol geride önkol çok / az geriye dönük | `elbowBack` (küçük = dirsek daha açık) |
| Tutarken kol yerinde durmuyor | Olmamalı: tam geride kol sabit `armBack`/`elbowBack`'te; sadece duruştaki kol pozla değişir |
| Gövde eğilince baş çok erken aşağı iniyor | `neckMax` ↑, pozdaki `headDown` |
| Karakter bitişte durmuyor / başa dönüyor | Bilerek öyle: `RaceController`; mesafe `raceDistance` |

---

## 7. Hangi dosya nerede

| Dosya | Klasör | Not |
|---|---|---|
| `RunnerInput.cs`, `SideFollowCamera.cs` | `Scripts/Runner/` | Girdi ve kamera (yeni sistem kullanıyor). Aynı klasördeki `LoopWay*`, `Pastel*`, `CloudVariation`, `RunnerContactShadow`, `AccessoryPhotoStudio` Serhat'ın dünya/ortam kodu. Eski prosedürel sistem ve `TestScene` 2026-10-26'da silindi. |
| `Spring`, `RunnerSim`, `RunnerBody`, `RunnerPose`, `RunnerPoseDefaults`, `RunnerPoseDriver`, `RaceController`, `RunnerReplay`, `ReferenceSequences`, `RunnerBodyHUD`, `DebugKeys` | `Scripts/Physical/` | Yeni sistem. |
| `PhysicalSetup.cs` | `Scripts/Physical/Editor/` | **Tools → Speed Stars → Set Up PhysicalTest Scene**: PhysicalTest'teki koşucuya eksik bileşenleri ekler, referansları (rig hedefleri, pozlar) yeniden atar (sahne yoksa hata verir). Tuning ve diğer ayar değerlerine dokunmaz. |
| Poz asset'leri | `Physical/Poses/` | Ready, Run, Stumble |

**Tuşlar:** ←/→ koş (dokunmatikte ekranın sol/sağ yarısı) · **1–4** video dizisini oynat · **Esc** durdur ·
**H** HUD'u gizle. Bitişi geçince otomatik başa döner (R tuşu yok).
