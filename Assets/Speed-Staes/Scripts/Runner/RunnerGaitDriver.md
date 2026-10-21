# RunnerGaitDriver — Ayar Rehberi

`RunnerGaitDriver` **sadece görseldir**: ayakların, gövdenin ve kolların nasıl göründüğünü belirler.
Hızı, gücü veya zamanlamayı **değiştirmez**. Onlar `RunnerStepController` ve `RunnerMovement`'ta.

"Şu an" sütunu bu dosya yazılırken sahnedeki değerdir (2026-10-21). Sen değiştirdikçe farklı olur.

---

## Önce: hızlı arama (sorun → hangi ayar)

| Gördüğün sorun | Bakılacak ayar |
|---|---|
| Öndeki ayak Front'ta beklerken havada | Step Points'te **Front** objesinin yüksekliği (Mid her zaman yerde) |
| Bacaklar dümdüz / gergin | `Ready Crouch` ↑ (gövdeyi alçalt) |
| Gövde çok çömelik | `Ready Crouch` ↓, `Run Crouch` ↓ |
| Ayakların birleştiği yer çok önde / arkada | `Mid Along` |
| Öne gelen ayak çok yükseğe kalkıyor | `Loop Height` ↓ |
| Ayak hep çok erken / geç tepe yapıyor | `Loop Skew` |
| Adımlar çok hızlı / yavaş | **`RunnerStepController`** → `Step Duration`, `Reach Duration`, `Return Duration`, `Front Return Duration` |
| Koşarken belden çok dik / çok eğik | `Lean By Tap Rate` |
| Karakter "süzülüyor", zıplama yok / çok fazla | `Hop Height` |
| Tutunca gövde yeterince öne eğilmiyor | `Hip Extension Limit` ↓, Back noktası daha geride, `Max Forward Lean` |
| Gövde eğilmesi geç / sallantılı | `Lean Lag` |
| Uçak pozunda gövde çok yüksek / alçak | `Plane Hip Height` |
| Uçak pozunda öndeki ayak gövdeye göre yanlış yerde | `Support Ahead Of Hip` |
| Diz yanlış yöne bakıyor | `Knee Hint Offset` |
| Ayak bileği doğal olmayan açıda | `Ankle Up Max`, `Ankle Down Max`, ayak profilleri |
| Ayak yere inerken burun / topuk açısı | `Touchdown Pitch` (profillerde) |
| Arkada tutulan ayağın açısı | `Pitch Back` |
| Kollar çok az / çok sallanıyor | `Arm Swing Front`, `Arm Swing Back` |

---

## Temel fikir: üç nokta

Her ayağın **ayak topu (parmak eklemi)** üç noktadan birine gider:

| Nokta | Nerede | Ne zaman |
|---|---|---|
| **Front** (yeşil küre) | Sahnede `Step Points` altındaki Front objesi | Diğer ayağa basılınca bu ayak buraya gelir; diğeri tutuldukça burada bekler |
| **Back** (kırmızı küre) | Sahnede `Step Points` altındaki Back objesi | Basılan ayak buraya gider; tuş tutuldukça burada kalır |
| **Mid** (sarı küre) | Front ile Back arası, **her zaman yerde** (`Mid Along`) | Tuşlar bırakılınca iki ayak burada buluşup bekler |

Hareket sırası:

1. **Sola bas:** sol ayak Mid → Back (yerden, `Step Duration`), sağ ayak Mid → Front (havadan, `Reach Duration`).
   Sağ daha kısa sürede gittiği için **önce varır**.
2. **Tutmaya devam:** sol Back'te, sağ Front'ta bekler. Gövde öne eğilir (uçak pozu).
3. **Bırak:** önce sol havadan Mid'e gelir (`Return Duration`), sağ bu sırada Front'ta yerde bekler.
   Sol Mid'e varınca sağ da yerden Mid'e gelir (`Front Return Duration`).
   Sağ Front'ta beklerken sağa basarsan tam güç alırsın; geç kalırsan sağ Mid'e iner, güç düşer.
4. **İki tuş birden tutulursa:** ikisi de Back'te, yüzüstü (dalış) pozu.

Front ve Back noktalarını sahnede taşıyarak adım uzunluğunu doğrudan değiştirebilirsin.
Scene görünümünde yayların önizlemesi çizilir.

---

## Zamanlama — `RunnerStepController` üzerinde

Bunlar görseli de etkiler ama **gücü/mekaniği de** değiştirir.

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Step Duration` | 0.12 s | Basılan ayağın Back'e gitme süresi |
| `Reach Duration` | 0.08 s | Diğer ayağın Front'a gitme süresi (Step'ten kısa = önce varır) |
| `Return Duration` | 0.25 s | Bırakınca arkadaki ayağın Mid'e dönme süresi |
| `Front Return Duration` | 0.15 s | Arkadaki Mid'e vardıktan sonra öndeki ayağın Mid'e gelme süresi |

---

## Foot (Ayak)

| Ayar | Şu an | Ne yapar | ↑ artırınca |
|---|---|---|---|
| `Pitch Back` | 50° | Ayak Back'te **tutulurken** ayağın açısı | Topuk daha çok kalkar, ayak daha dik |

## Foot motion (Ayak profilleri)

İki profil var, hıza göre karışır:
- **`Accel Profile`**: yavaşken (hızlanma) kullanılır.
- **`Max Speed Profile`**: hızlıyken kullanılır.

`Accel Profile Speed` (şu an 2 m/s) altında tamamen hızlanma profili,
`Max Profile Speed` (şu an 9 m/s) üstünde tamamen hız profili kullanılır. Arası yumuşak geçiş.

Her profilin içinde:

| Ayar | Ne yapar | X ekseni (eğrilerde) |
|---|---|---|
| `Touchdown Pitch` | Ayak yere inerken açısı. **Eksi = burun yukarı, topukla iner.** (şu an −8° / −12°) | — |
| `Stance Pitch` | Ayak yerde geriye iterken açısı (+ = topuk kalkar) | 0 = itiş başı, 1 = ayak yerden kalkıyor |
| `Toe Roll` | İtişin sonunda parmakların üstünden yuvarlanma | 0 → 1 itiş boyunca |
| `Swing Ankle` | Ayak havadayken bilek bükümü, baldıra göre (+ = burun aşağı) | 0 = havalandı, 1 = yere iniyor |
| `Swing Toe` | Ayak havadayken parmak bükümü (+ = parmak yukarı) | 0 → 1 havada geçen süre |

## Toes (Parmaklar)

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Toe Length` | 0.06 m | Parmak ekleminden parmak ucuna mesafe. Yuvarlanma ve "parmak yerde mi" hesabı için |
| `Toe Up Max` | 55° | Parmakların en fazla yukarı bükülmesi |
| `Toe Down Max` | 0° | Parmakların en fazla aşağı bükülmesi (0 = hiç aşağı bükülmez) |

## Foot loop (Ayak yayı)

| Ayar | Şu an | Ne yapar | ↑ artırınca |
|---|---|---|---|
| `Loop Height` | 0.25 m | Havadan giden ayağın yay yüksekliği. Kısa yolculukta (Mid'e dönüş gibi) otomatik olarak alçalır. Yerden gidişte aynı değer aşağı yay olarak kullanılır ve zeminde kesilir (o düz kısım = yere temas) | Ayak daha yükseğe kalkar |
| `Loop Skew` | 0.6 | Yayın tepesinin zamanı. 1 = ortada, <1 = erken (topuk önce kalkar), >1 = geç | — |
| `Knee Hint Offset` | (0, −0.35, 0.6) | Dizin baktığı yön (kalçaya göre). Z = ileri, Y = aşağı | Z ↑ diz daha öne bakar |
| `Mid Along` | 0.3 | Ayakların buluşup beklediği nokta. 0 = Front'ta, 1 = Back'te, 0.5 = tam orta | Mid geriye kayar |
| `Ground Blend Time` | 0.25 s | Koşucu tamamen durunca Mid pozuna geçiş süresi | Duruş daha yavaş oturur |

## Ankle (Ayak bileği)

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Ankle Up Max` | 20° | Bileğin en fazla yukarı bükülmesi (burun yukarı) |
| `Ankle Down Max` | 50° | Bileğin en fazla aşağı bükülmesi (burun aşağı) |

Bu sınırlar her şeyin üstündedir: profiller ne derse desin bilek bunları aşmaz.

## Pelvis (Kalça / gövde yüksekliği)

| Ayar | Şu an | Ne yapar | ↑ artırınca |
|---|---|---|---|
| `Run Crouch` | 0.04 m | Koşarken kalçanın alçalması | Gövde aşağı iner |
| `Ready Crouch` | 0.13 m | Ek alçalma: ayaklar Mid'de birleşince dizler kırık kalsın, koşuya hazır dursun | Gövde daha aşağı, dizler daha bükük |
| `Hop Height` | 0.04 m | Hop tavanı: basışla kalçanın en fazla ne kadar kalkacağı | Zıplama daha belirgin |
| `Pelvis Forward` | 0.04 m | Kalçanın öne kayması | Kalça daha önde |
| `Pelvis Tilt` | 6° | Kalçanın öne devrilmesi | Kalça daha çok öne yatar |

> **Not:** `Run Crouch` ve `Ready Crouch` **toplanır** ve ikisi de her zaman uygulanır. Toplam alçalma şu an 0.17 m.
> Gövdeyi alçaltmak/yükseltmek için ikisinden birini değiştirmen yeter.

### Hop nasıl çalışır

- Her basış kalçayı **gücü kadar** kaldırır: Front'ta basış (tam güç) = `Hop Height` kadar,
  Mid'de basış (0.38) = tavanın 0.38'i kadar, Back'te basış = hiç.
- Kalkış `Step Duration` sürer, düşüş `Return Duration` sürer (ayağın Mid'e dönüşüyle aynı).
  Daha alçaktan düşüyorsa o oranda kısa sürer. Düşüş tepede yavaş, yere yakın hızlıdır.
- Kalça düşerken yeni basış gelirse **o anki yükseklikten** tekrar kalkar ama **tavanı geçmez**.
  Doğru anlarda basılırsa kalça tavanda kalır; basılmazsa veya zayıf basılırsa iner.
- Hop sadece görseldir, hızı etkilemez.

## Lean (Gövde eğilmesi)

İki ayrı eğilme var:

- **Bel eğilmesi (koşarken):** basma sıklığına göre. Sadece **belden yukarısı** (omurga) eğilir;
  kalça ve bacaklar koşuya devam eder. Hızlı bastıkça öne gider, yavaşlayınca geri gelir.
- **Bütün beden eğilmesi (tutunca):** kalça limiti. Bacak gövdeye göre ancak `Hip Extension Limit`
  kadar geriye gidebilir (gerçek insan ~20–30°). Back'te **tutulan** ayak bundan fazla gerideyse,
  aradaki fark kadar bütün beden (kalça dahil) öne yatar. Bir ayak = uçak, iki ayak = yüzüstü.

Kafa gövdeyle birlikte eğilir: gövde dikken ileri bakar, ~15° eğilince tamamen gövdeyi takip eder.

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Hip Extension Limit` | 25° | Bacağın gövdenin gerisine en fazla açılabileceği açı. **Küçültürsen** tutunca gövde **daha çok** öne yatar |
| `Lean By Tap Rate` | (0→0°), (2→8°), (5→20°), (8→28°) | Bel eğilmesi. **X:** saniyedeki basış sayısı (HUD'daki "Sıklık"). **Y:** açı |
| `Max Forward Lean` | 80° | Bütün beden öne eğilmesinin üst sınırı |
| `Max Back Lean` | 35° | Geriye eğilmenin üst sınırı |
| `Lean Lag` | 1.75 | Eğilmelerin ne kadar gecikmeyle takip ettiği (× `Step Duration`). Büyük = daha yumuşak ama geç |

> Tutunca ne kadar eğileceğini iki şey belirler: `Hip Extension Limit` ve sahnedeki **Back** noktasının yeri
> (Back ne kadar geride / yüksekse bacak o kadar geride, gövde o kadar öne yatar).
> İki ayak tutulunca yüzüstü pozu `Prone Lean` açısına gider.

## Hold poses (Tutma pozları)

**Uçak:** tek tuş tutulu. Tutulan bacak gövdeyle aynı çizgide düz geride, diğer ayak Front'ta destek.
**Yüzüstü:** iki tuş tutulu. Gövde öne dalıp yere yatar.

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Held Leg Reach` | 0.95 | Tutulan bacağın ne kadar gergin uzandığı (1 = tam düz) |
| `Held Toe Point` | 0° | Tutulan düz bacakta ayağın ekstra uzatılması |
| `Plane Hip Height` | 0.5 m | Uçak pozunda kalçanın yerden yüksekliği. Öndeki diz buna göre bükülür |
| `Support Ahead Of Hip` | 0.3 m | Uçak pozunda öndeki (destek) ayağın kalçanın ne kadar önünde kalacağı |
| `Support Knee Follow` | 0.3 | Destek dizinin gövde eğimini ne kadar takip ettiği (0 = hep ileri bakar, 1 = gövdeyle döner) |
| `Prone Lean` | 88° | Yüzüstü pozda gövde açısı (90 = yere paralel) |
| `Prone Hip Height` | 0.15 m | Yüzüstü pozda kalçanın yerden yüksekliği |
| `Prone Forward` | 0.4 m | Yüzüstü pozda gövdenin öne dalma mesafesi |

## Arms (Kollar)

Her kol **karşı ayağı** takip eder: sağ ayak → sol kol, sol ayak → sağ kol.
Ayak Front'tayken kol öne, Back'teyken arkaya salınır. Arada orantılı.

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Arm Swing Front` | 45° | Karşı ayak Front'tayken kolun öne açısı |
| `Arm Swing Back` | −40° | Karşı ayak Back'teyken kolun arkaya açısı |
| `Elbow Front` | 100° | Kol öndeyken dirsek bükümü (0 = düz) |
| `Elbow Back` | 70° | Kol arkadayken dirsek bükümü |
| `Elbow Standing` | 15° | Dururken dirsek bükümü |
| `Arm Outward` | 8° | Kolun gövdeden yana açılması |
| `Shoulder Swing` | 10° | Kol öne giderken omzun öne gelmesi |
| `Shoulder Swing Back` | 22° | Kol arkaya giderken omzun geri çekilmesi |
| `Shoulder Lift` | 4° | Kol öndeyken omzun kalkması |
| `Shoulder Lift Back` | 3° | Kol arkadayken omzun kalkması |

> Ayaklar Mid'de beklerken kollar da Front ile Back arasındaki orana göre (`Mid Along`) ara bir pozda durur.

## Hands (Eller)

| Ayar | Şu an | Ne yapar |
|---|---|---|
| `Flat Hand` | 0 | 0 = sahnedeki el pozu (koşu yumruğu), 1 = düz el, avuç yere bakar (start pozu için) |

---

## Rig References (dokunma, sadece bağlantı)

`Hip Target`, `Spine Root/Tip Target`, `Left/Right Leg` (Target, Hint, Toe/Shin/Foot Bone, Front, Back),
`Left/Right Arm` rig bağlantılarıdır. Boş kemik alanları isimle otomatik bulunur
(`B-shin.L`, `B-foot.R`, `B-upperArm.L` …). Ayar değil, değiştirmen gerekmez.
