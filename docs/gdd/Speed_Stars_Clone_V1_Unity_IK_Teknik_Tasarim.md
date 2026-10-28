# SPEED STARS CLONE V1.0 — UNITY + IK TEKNİK TASARIM DOKÜMANI

**Belge türü:** Game Design + Technical Design (uygulama brifi)  
**Sürüm:** 1.0 — 12 Ekim 2026  
**Hedef:** Unity ile Speed Stars referansındaki koşu deneyimini üretmek; kullanıcı tarafından belirlenen Sokak Yarışı temasını uygulamak ve `Speed Stars_V1.0.pdf` gereksinimlerini eksiksiz entegre etmek.  
**Referanslar:**
- Oyun: https://apps.apple.com/us/app/speed-stars/id1573039936
- Gereksinim belgesi: `Speed Stars_V1.0.pdf`, 12.10.2026, Xie Jintao (sayfa 1–4 Çince; 5–9 Türkçe aynı belge).
- Gameplay: `YTDown.com_Shorts_Media_wC9zVBOzTiQ_100m-DASH-9-15-Speed-Stars-Handcam-speedstars_001_1080p.mp4` (~15,1 sn; 60 FPS; 100m Dash handcam).

> **Kaynak ayrımı:** **[KİLİTLİ]** kullanıcının açık kararı; **[PDF]** kaynak belgede yazan gereksinim; **[VİDEO]** videoda doğrudan görülebilen davranış; **[TASARIM]** bu davranışları Unity ile üretmek için önerilen implementasyon; **[AÇIK]** kaynaklarda tanımlanmamış ve ölçüm/karar gerektiren nokta. `[TASARIM]` orijinal oyunun iç kodunun bilindiği anlamına gelmez.

---

## 1. PROJE SINIRLARI / DEĞİŞMEZ KARARLAR

1. **[KİLİTLİ] Gameplay:** Speed Stars'ın oyun hissi, karakter kontrol mantığı, yarış akışı ve temel mekanikleri referans alınarak mümkün olduğunca aynı deneyim üretilecek. Referans oyunun kaynak kodu, özgün fizik formülleri ve gerçek rig'i mevcut değil; eşdeğer davranış testle yakalanacak.
2. **[KİLİTLİ] Görsel stil:** Orijinal Speed Stars'ın görsel yaklaşımı korunacak. Bu ifade, elimizde orijinal asset'lerin bulunduğu veya bunlara kullanım hakkı olduğu anlamına gelmez; üretimde asset ve marka hakları ayrıca doğrulanmalı.
3. **[KİLİTLİ] Tema:** **Sokak Yarışı yalnızca temadır.** Kontrol sistemi, koşu mekaniği, yarış kategorileri, ödül ve bilet kuralları bu ifade nedeniyle değiştirilmeyecek. Yarışların isimleri/belirlenmiş mesafeleri korunur. Temanın sahne dekoruna tam yansıması ayrıca kararlaştırılır.
4. **[KİLİTLİ] Karakter animasyonu:** Unity'de **IK (Inverse Kinematics)** tabanlı sistem kullanılacak. IK karakter hareketinin bütün fizik modelinin yerine geçmez; hareket kontrolörü ile birlikte çalışır.
5. **[KİLİTLİ] Ekonomi, giriş ve yeni UI:** PDF kapsamına göre uygulanacak; PDF'de olmayan değerler otomatik tamamlanmayacak.
6. **[KİLİTLİ] Bu belgedeki öneri sistemleri:** İleride kullanıcı tarafından farklı karar verilene kadar prototip/uygulama önerisi olarak değerlendirilir; PDF kurallarına üstün değildir.

### 1.1 Geliştirme önceliği

Birincil başarı koşulu: **karakterin dokunma ritmine verdiği fiziksel tepkinin Speed Stars'a yakın hissettirmesi.** Önce 100m koşu çekirdeği ve giriş zamanlaması, ardından diğer yarışlar, en sonunda PDF metası/UI sistemleri tamamlanır. Bu sıralama bir **[TASARIM]** üretim planıdır; PDF'nin bölüm sırası ve gereksinimleri değişmez.

---

## 2. REFERANS VİDEO ANALİZİ — GÖZLEM / ÇIKARIM

### 2.1 Kesin gözlemler [VİDEO]

- Yaklaşık 15 saniyelik 100m Dash videosu; görüntüde hem oyun ekranı hem gerçek el/parmak hareketlerini gösteren alt kayıt bulunuyor.
- Yarış başında koşucu alçak, öne yatık sprint çıkış pozisyonunda bekliyor; başlangıç sinyali/geri sayım benzeri **SET** aşaması görünüyor.
- Oyuncu yarış sırasında ekrana art arda parmak dokunuşları yapıyor. Tek tek dokunuş ile her animasyon frame'i arasındaki kesin eşleşme bu kayıttan ölçülmedi.
- Karakterler yandan gözlenen yatay pist düzleminde koşuyor, kamera yarışçılarla ilerliyor.
- Birden fazla koşucu aynı anda yarışta bulunuyor. Gövdeler öne eğiliyor, ayaklar adım alıyor ve kollar ritme eşlik ediyor.
- Yarış arayüzünde ilerleme/zaman bilgileri ve oyun içi HUD öğeleri bulunuyor; küçük HUD yazılarının bütün değerleri videodan güvenilir şekilde okunamıyor.
- Yarış bitişinde sonuç paneli ve **Watch replay**, **Race again**, **High scores**, **Menu**, **Practice** gibi seçenekler görüntüleniyor.

### 2.2 Videodan kesin çıkarılamayanlar [AÇIK]

- Oyunun gerçekten IK, ragdoll, procedural animation veya klip animasyon karışımı kullanıp kullanmadığı.
- Sol/sağ dokunuşun her durumda bir bacağa birebir mi bağlandığı; ekranın hangi bölgelerinin geçerli sayıldığı.
- Dokunma süresi, basılı tutma, kaydırma veya release olaylarının oyun fiziğine nasıl dönüştüğü.
- Özgün oyundaki kuvvet, enerji, hız, sürtünme, stamina/cadence formülleri ve rakip yapay zekâ modeli.
- Rekor doğrulama, çevrim içi yarış altyapısı, ghost kaydı formatı, replay verisi ve anti-cheat.
- 100m dışındaki yarışların tüm engel/relay/kombinasyon kuralları; PDF bunları sistematik biçimde açıklamıyor.

**Uygulama ilkesi:** Bu bilinmeyenler “orijinalin kesin mekaniği” olarak yazılmayacak; yapılacak testler, değer aralıkları ve teknik seçenekler şeklinde tutulacak.

---

## 3. CORE GAMEPLAY — 100m ÖNCELİKLİ

### 3.1 Yarış akışı [VİDEO] + [TASARIM]

```text
Home → Event Select → Entry Check (bilet gerekiyorsa) → Race Preparation
→ Start Stance → Countdown/SET → Start → Running → Finish
→ Results → Replay / Race Again / High Scores / Menu / Practice
```

- PDF'de yarışı başlatacak bileti hangi anda harcayacağımız tanımlıdır: giriş onayı, geri sayımın başlaması veya resmi koşu başlangıcı. Aynı yarışa iki kez harcamayı önlemek için uygulamada **tek, idempotent RaceEntryConfirmed** olayı kullanılır. [PDF]+[TASARIM]
- Koşu başladıktan sonra bileti geri verme davranışı yoktur; başarısızlık, yarışı terk etme ve restart bunun istisnası değildir. [PDF]
- SET/Start ve Finish aşamaları video referansına göre görselleştirilir; yarış start timing, false start vb. kurallar **[AÇIK]**.

### 3.2 Girdi işleme [TASARIM]

**Amaç:** Parmak zamanlamasının koşucunun adım kalitesini belirlediği, gecikmesiz ve kararlı bir kontrol hissi.

Önerilen event veri yapısı:

```csharp
public readonly struct RunnerInputEvent
{
    public readonly double Timestamp;  // monotonik zaman
    public readonly int PointerId;
    public readonly RunSide Side;      // Left / Right
    public readonly InputPhase Phase;  // Down / Up
}
```

- Input System (New Input System) dokunmatik çoklu dokunma kullanımı için önerilir; PC testinde klavye iki tuş emülasyonu eklenir.
- `Down`, `Up`, dokunma süreleri ve ardışık dokunuş aralığı telemetri/test için kaydedilir; etkin kontrol fonksiyonu referansa karşı doğrulanmadan kilitlenmez.
- Girdileri render frame süresine değil zaman damgasına bağla; fizik çözümlemesi sabit adımda yapılır.
- Aynı anda çoklu dokunma, UI üzerine dokunma, geri dönüş ve uygulama odağını kaybetme testleri gerekir.

### 3.3 Adım modeli [TASARIM]

Her bacak için durum:

```text
STANCE/CONTACT → PUSH_OFF → SWING_FORWARD → FOOT_PLANT → STANCE/CONTACT
```

- `LeftLeg` / `RightLeg` ayrı faz/durum tutar. Hareket hedefleri gameplay controller tarafından üretilir; IK bunları çözer.
- Hareket öncesi tüm parametreler ölçüme açık tutulur: `stepPeriod`, `contactFraction`, `strideLength`, `footClearance`, `maxLegExtension`, `cadenceResponse`, `inputTimingWindow`.
- Alternating tap, erken/geç input, arka arkaya aynı taraf dokunması ve boşta bırakma durumlarını test etmek gerekir; bunların cezaları **orijinalden doğrulanmış değildir**.
- Hedef: animasyon ve ilerleme aynı koşu fazından beslenmeli; hareket hızlanırken ayak kayması oluşmamalı.

### 3.4 Hız ve hareket modeli [TASARIM]

Özgün formül bilinmediği için aşağıdaki yalnızca **tune edilebilir model** başlangıcıdır:

```text
stepQuality = TimingEvaluator(inputTime, idealContactTime)
impulse     = StepImpulseCurve(stepQuality, stridePhase, runnerState)
velocity    = Integrate(velocity, impulse, resistance, fixedDeltaTime)
position   += velocity * fixedDeltaTime
```

**Not:** Burada fiziksel birim, katsayı, maksimum hız, hızlanma eğrisi veya referansa ait resmi sayı iddia edilmiyor. Kalibrasyonda videodan ölçülen zaman/mesafe, yer değiştirme, gövde pozisyonu ve yarış bitiş süresi birlikte kullanılacak.

### 3.5 Yarış simülasyonu [TASARIM]

- Aynı fixed-timestep altında yarış saati ve mesafe tutulur.
- Finish trigger gerçek koşu mesafesinin geçilmesine dayanır; görsel kamera kararları sıralamayı etkilemez.
- Rakip/ghost için oyuncuyla aynı mesafe ve zaman koordinatlarının kullanılması önerilir.
- Finish frame'inde sonuçları dondur, sıralamayı **tek yarış sonucu** üzerinden hesapla; Mor Mücevher için yalnız bu sıralama kullanılır. [PDF]

---

## 4. UNITY IK + KARAKTER FİZİĞİ TASARIMI

### 4.1 Rig [TASARIM]

Önerilen hiyerarşi:

```text
RunnerRoot (world position / race distance)
└── VisualRoot (body rotation, lean, cosmetic offsets)
    └── Skeleton
        ├── Pelvis / Spine / Chest / Neck / Head
        ├── LeftUpperArm → LeftLowerArm → LeftHand
        ├── RightUpperArm → RightLowerArm → RightHand
        ├── LeftThigh → LeftShin → LeftFoot
        └── RightThigh → RightShin → RightFoot
    └── IKTargets
        ├── LeftFootTarget / LeftKneeHint
        ├── RightFootTarget / RightKneeHint
        └── OptionalHandTargets
```

Unity Animation Rigging paketinin `Two Bone IK Constraint` veya eşdeğer 2-bone çözücü kullanılabilir. Gerekirse özel planar analitik IK geliştirilebilir. Prototipte seçilecek yöntem performans ve animasyon hissi karşılaştırmasıyla belirlenecek.

### 4.2 Ayak hedefleri [TASARIM]

- **Temas:** Ayak dünya uzayında temas noktasına sabitlenir (foot lock); model yürüdükçe görsel kayma engellenir.
- **İtiş:** Kalça/ayak fazına göre ayak-geriye-itme ve gövde-ilerleme ilişkisi tanımlanır.
- **Havada salınım:** Foot target kontrollü yay çizer; yerden yükseklik faza göre değişir.
- **Tekrar temas:** Ayak hedefi yeni zemin temasına hazırlanır; zemin yüksekliği örneklenir.
- **Diz yönü:** Knee hint kullan; diz bükülmesi kamera açısından tutarlı görünmeli.
- **Uzama sınırı:** Bacak kemik uzunluğu aşılmamalı; aşırı stride hedefi çözücü sınırlarına getirilir.

Önerilen taslak eğri (orijinalin formülü **değil**):

```text
swingT = 0..1
footForward = Lerp(rear, front, SmoothStep(swingT))
footHeight  = peakLift * Sin(PI * swingT)
```

### 4.3 Kalça, gövde ve kollar [TASARIM]

- Sprint çıkışı sırasında kuvvetli öne eğilme, hızlanmayla birlikte uygun koşu postürüne geçiş.
- Pelvis yüksekliği, minik dikey salınım ve rotasyon koşu fazına bağlıdır.
- Kollar bacakların ters fazında prosedürel hareket eder; hareketin el animasyonu ile çakışmasına izin verilmez.
- Baş stabilizasyonu ve gövde salınımı yarış görüntüsüne uyumlu ayarlanır.
- Karakter performansı doğrudan animasyon çiziminden değil gameplay controller'dan hesaplanır; IK sonuçları simülasyonun görsel temsilidir.

### 4.4 Fixed Update sırası [TASARIM]

```text
Collect Input (Update / Input callbacks; timestamp)
→ Queue / Validate Input
→ Update Race Simulation (FixedUpdate)
→ Advance Step Phase and Runner State
→ Compute World-space IK Targets
→ Evaluate Rig and Visual Pose (animation pass)
→ Smooth Camera / HUD (LateUpdate)
```

Fazlar arasında fizik ve çizim senkronu dikkatle yönetilmeli. Rendering interpolation kullanılırken finish ve gerçek yarış pozisyonu simülasyon verisinde kalmalı.

### 4.5 IK kabul testleri [TASARIM]

- Tek ayak temasında taban duruşu kaymaz.
- Hız değişiminde bacaklar kopma/ters diz üretmez.
- İki bacak fazı çakıştığında pelvis patlaması yaşanmaz.
- 30/60/120 FPS render hızlarında aynı timestamp girişleri aynı koşu sonucunu verir (belirlenen tolerans dahilinde).
- Farklı boy/ölçek karakterlerinde IK kemik oranlarına göre çalışır.
- Telefon ekranındaki el/girdi alanları HUD kontrolüne çarpışmaz.

---

## 5. KAMERA, SUNUM, UI TEMELİ

### 5.1 Kamera [VİDEO] + [TASARIM]

- Referans: **yandan izlenen**, yatay yarış akışı, yarışçılarla ilerleyen kamera.
- Uygulama: world-space yatay mesafe, ayrı görsel lane offset, takip ve bitiş kompozisyonları.
- Kamera zoom, easing, hedef seçimi ve finish sabitleme sayıları **[AÇIK]**; referans görüntülere göre ayarlanır.

### 5.2 Öncelikli ekranlar [PDF] + [VİDEO]

1. Home — oyun temel menüsü ve dört kaynak göstergesi.
2. Event Select — orijinal ücretsiz / biletli ayrımı.
3. Entry Gate — Süreli Bilet aktiflik kontrolü veya Adet Bileti girişi.
4. Race HUD — sayaç/ilerleme ve referans kontrollere uygun yarış ekranı.
5. Results — yarış sonucu ve replay/yeniden yarış seçenekleri.
6. Daily Login Popup — günlük Adet Bileti alma penceresi.
7. 7-Day Check-in — günlük ödül paneli ve ana ekran bildirim işareti.
8. Cosmetics — premium varyantlar ve Mor Mücevherle satın alma.

**Önemli:** PDF'nin ekran görüntüleri bir **örnek ekran/yerleşim referansıdır**; gösterilen görsel prototipleri gerçek çalışır Unity UI'ı veya tam akış spesifikasyonu değildir.

---

## 6. GİRİŞ BİLETİ EKONOMİSİ — KAYNAK GEREKSİNİMLERİ

### 6.1 Yarış erişim ilkesi [PDF]

- Orijinal oyunda ücretli giriş gerektiren **bütün** etkinlikler Giriş Bileti ile açılır.
- Orijinalde ücretsiz olan etkinlikler ücretsiz kalır.
- Hangi etkinliklerin orijinal oyunda ücretli olduğu PDF'de tek tek işaretlenmediği için **[AÇIK]** bir `RaceEventCatalog.requiresTicket` verisiyle ayrıca doğrulanmalıdır.

### 6.2 Süreli Bilet [PDF]

- Bir Süreli Bilet kullanıldığında **gerçek zamanda 1 saat** etkinleşir; oyun açık kalma süresine bağlı değildir.
- Etkin iken bilet gerektiren bütün resmi yarışlara sınırsız giriş yapılır; **Adet Bileti tüketilmez.**
- Etkin durumdayken ikinci Süreli Bilet kullanılamaz, süre birikmez.
- Etkin durumdayken “Adet Bileti Kullan” seçeneği gösterilmez veya pasif yapılır.
- Süre yarış sırasında biterse yarış bölünmez, mağlubiyet verilmez; yeni girişte tekrar bilet kontrolü yapılır.
- Oyun kesilmesi/yeniden girişte gerçek zaman bitiş timestamp'i esas alınmalıdır. [TASARIM: implementasyon tercihi]

### 6.3 Adet Bileti [PDF]

- Bilet gerektiren bir yarışa **tek giriş = 1 Adet Bileti**.
- Giriş onayında (geri sayım başladığında / resmen yarışa başlanırken) tüketilir.
- Yarışı terk etme, başarısızlık veya restart: iade **yok**.
- Envanter üst sınırı **3**.
- Doluyken otomatik yenileme durur, günlük giriş ödülü verilmez ve “Adet Bileti Dolu” uyarısı görünür.
- Elmas/Nakit ile ve reklam izleyerek edinimde de **3 sınırı** geçilemez; doluysa satın alma seçenekleri gri/pasif.
- Her gün giriş: **1 Adet Bileti**, yer varsa.
- Doğal yenilenme: **30 dakikada 1**. Envanter 3 ise sayaç durur; 3'ün altına düşünce **yeni bir 30 dakika** başlar.

### 6.4 Önerilen state modeli [TASARIM]

```csharp
[Serializable]
public sealed class TicketState
{
    public int EntryTicketCount;          // [0..3]
    public int TimedTicketCount;          // stok; PDF stok limiti tanımlamıyor
    public long TimedAccessExpiresUtc;   // UTC epoch millis
    public long EntryRegenDueUtc;        // >=0; full halde yok
    public string LastDailyClaimDate;    // yerel takvim / reset politikası açık
}
```

**Giriş kontrolü (önerilen):**

```text
if event.requiresTicket == false → ENTER_FREE
else if timedAccessNowActive → ENTER_WITHOUT_CONSUMPTION
else if entryTicketCount >= 1 → CONFIRM_AND_CONSUME_1 → ENTER
else → SHOW_GET_TICKET_OPTIONS
```

**Durum değişimi (önerilen):**

```text
UseTimedTicket:
  precondition: timedAccess inactive AND timedTicketCount > 0
  timedTicketCount -= 1
  timedAccessExpiresUtc = trustedNowUtc + 60 minutes

ConsumeEntryTicket:
  precondition: timedAccess inactive AND count > 0 AND entry required
  count -= 1
  if count became < 3 and no timer is running: start full 30-minute regen timer

RegenTick:
  if count == 3: timer disabled
  else if trustedNowUtc >= due:
     count += 1
     if count == 3: disable timer
     else: next 30-minute recovery interval
```

**[AÇIK]** Cihaz saatini değiştirme/offline güvenliği, oyuna geri dönünce birden çok 30 dk intervalini telafi etme, hediye günü sıfırlanması, günlük pop-up'ın dolu bilet durumundaki UX'i PDF'de ayrıntılanmıyor; test edilen canlı ekonomi kurallarıyla çelişmeden çözülmelidir.

### 6.5 Giriş/regen acceptance testleri [PDF]

| Test | Beklenen |
|---|---|
| Adet Bileti 2 iken giriş (Süreli Bilet pasif) | 1'e düşer |
| Süreli Bilet aktifken yarış girişi | Adet Bileti harcanmaz |
| Süreli Bilet aktifken ikinci kullanım | Engellenir |
| Süreli Bilet yarış ortasında biter | Yarış sonlandırılmaz, sonraki girişte kontrol edilir |
| Biletle girilen yarıştan çıkış/restart | İade yapılmaz |
| Adet Bileti 3 iken 30 dakika geçer | 3 kalır, timer işlemez |
| Adet Bileti 3→2 | Yeni 30 dakikalık sayaç başlar |
| Adet Bileti 3 iken günlük ödül | Verilmez, dolu uyarısı |
| Adet Bileti 3 iken reklam/elmas edinimi | Pasif; limit aşılmaz |

---

## 7. MOR MÜCEVHER (ÖZEL PARA BİRİMİ)

### 7.1 Tanım [PDF]

- Mor Mücevher, Nakit/elmas ve giriş biletlerinden **ayrı** bir kaynaktır.

### 7.2 Yalnızca ödül veren etkinlikler [PDF]

| Etkinlik | 1. | 2. | 3. | 4. ve sonrası |
|---|---:|---:|---:|---:|
| 400m | 3 | 2 | 1 | 0 |
| 60mH | 3 | 2 | 1 | 0 |
| Medley | 3 | 2 | 1 | 0 |

- Ödül **tek yarışın** sıralamasına göre hesaplanır; günlük/haftalık/sezonluk liderlik tabloları temel alınmaz.
- Şunlar Mor Mücevher **vermez**: Antrenman Alanı, Race of the Day, 100m, 200m, 400mH, 110mH, 4×100m.
- **Premium kozmetik fiyatı: 50 Mor Mücevher.**

### 7.3 Ödül yerleştirme [TASARIM]

- `RaceCompleted` sonucu doğrulandıktan sonra `RaceRewardResolver` yarış ID'si ve yarış sırasını inceler.
- `PayoutTransactionId` ile aynı yarış ödülünün iki kez verilmesini engelle.
- 4. veya sonrası; sonuç geçersiz/yarış tamamlanmadıysa bu PDF'de olmayan ödülü otomatik verme.

---

## 8. GÜNLÜK GİRİŞ HEDİYESİ [PDF]

- Günün ilk oyun girişinde **Giriş Hediyesi pop-up'ı** açılır.
- Oyuncu butona basarak **1 Adet Bileti** teslim alır (envanter dolu değilse).
- Pop-up kapandıktan sonra **kartın ekran ortasından ilgili kaynak çubuğuna uçtuğu** ödül animasyonu **bir kez** oynar.
- Dolu envanterle +1 eklenmeyecek; “Adet Bileti Dolu” kuralı korunur.

**[AÇIK]** Gün tanımı (yerel tarih mi UTC mi), kaçırılan günler, ödül kutusu kapatılırsa hak durumu ve doluyken pop-up'ın kapatma biçimi ayrıca kararlaştırılır.

---

## 9. ANA EKRAN KAYNAK BARLARI [PDF]

Yeni göstergeler:
1. **Mor Mücevher** (adet).
2. **Adet Bileti** (örn. `2/3`) + sonraki yenilenmeye geri sayım.
3. **Süreli Bilet** (eldeki sayı / aktiflik durumuna göre sunum).
4. **Nakit** (orijinal oyun elmasının yerini alan, mevcut değer).

- PDF'deki örnek kaynak barı görsel referans kabul edilir.
- Timer doluyken yenilenmeye devam ediyormuş gibi gösterilmez.
- Süreli Bilet aktifken uygun yarış girişinde Adet Bileti tüketme UI'ı görünmez/pasiftir.

---

## 10. PREMIUM KOZMETİKLER [PDF]

- **Her kozmetik kategorisinde 2 premium kozmetik.**
- Liste görünümünde premium kozmetikler **varsayılan olarak en üstte** gösterilir.
- Özel **parlak/renkli arka plan paneli** olmalıdır.
- Premium kozmetikler **Mor Mücevherle** satın alınır; fiyat **50**.
- Efektler öne çıkmalı: özel trail ve partikül gibi görünür farklar kullanılabilir.

**[TASARIM] IK entegrasyonu:** Kozmetik mesh/sprite ve efektler rig üzerinde socket/bone parent'a bağlanır; kostüm ayak IK hedeflerini bozmamalı. Trail ve partiküller koşu sırasında mobil performansa uygun sınırlandırılmalı.

**[AÇIK]** Kozmetik kategorilerinin tam listesi, 2 premium eşyanın tek tek isim/asset tanımı ve mevcut market fiyatları kaynakta bulunmuyor.

---

## 11. 7 GÜNLÜK GİRİŞ ÖDÜLÜ [PDF]

- Ana ekrana **7 Günlük Giriş Ödülü** giriş ikonu eklenir.
- Alınabilir ödül için **kırmızı bildirim noktası** bulunur.
- PDF'de ödül yapılandırması **geçici/taslak** olarak verilmiştir; aşağıdaki veriler belgenin **birebir** içeriğidir:

| Gün | Ödül |
|---|---|
| 1 | 100 Nakit |
| 2 | 200 Nakit |
| 3 | 1 Süreli Bilet |
| 4 | 200 Nakit |
| 5 | 1 Süreli Bilet |
| 6 | **PDF'DE BELİRTİLMEMİŞ — AÇIK KARAR** |
| 7 | Mor Mücevherle alınabilen 1 premium kozmetik; şimdilik baş/kafa kozmetiği |

**[AÇIK]** Bu 7 günün ardışık/bağımsız giriş mi olduğu, 7. gün sonrası döngü, gün sıfırlama saati, otomatik/manuel claim, kaçırılan günlerin yönetimi ve 6. gün hediyesi PDF'de tanımlanmıyor.

---

## 12. YARIŞLAR / MOD KATALOĞU

**PDF'de adları geçen etkinlikler:**

| Etkinlik | Mor Mücevher | Bilet gerekliliği |
|---|---|---|
| Training / Antrenman Alanı | Vermez | Orijinal oyunda ücretsiz/ücretli durumu doğrulanmalı |
| Race of the Day | Vermez | Doğrulanmalı |
| 100m | Vermez | Doğrulanmalı |
| 200m | Vermez | Doğrulanmalı |
| 400m | İlk 3'e 3/2/1 | Doğrulanmalı |
| 60mH | İlk 3'e 3/2/1 | Doğrulanmalı |
| 400mH | Vermez | Doğrulanmalı |
| 110mH | Vermez | Doğrulanmalı |
| 4×100m | Vermez | Doğrulanmalı |
| Medley | İlk 3'e 3/2/1 | Doğrulanmalı |

Bu tablo **yarışların tam kurallarını** değil PDF'de geçen isimleri ve Mor Mücevher kazançlarını içerir. Engel, bayrak yarışı ve Medley koşu kuralları referans oynanış üzerinden ayrıca belgelenecek. **Sokak Yarışı teması bu etkinlik listesini değiştirmez.**

---

## 13. ÖNERİLEN UNITY MİMARİSİ [TASARIM]

```text
GameBootstrap
├── SaveSystem
├── ClockService
├── EconomyService
│   ├── CashWallet
│   ├── PurpleGemWallet
│   ├── EntryTicketService
│   ├── TimedTicketService
│   └── RewardTransactionService
├── DailyRewardsService
│   ├── DailyEntryGift
│   └── SevenDayCheckIn
├── RaceService
│   ├── RaceEventCatalog
│   ├── RaceEntryGate
│   ├── RaceStateMachine
│   ├── RaceSimulationClock
│   ├── RaceResultService
│   └── RaceRewardResolver
├── RunnerController
│   ├── TouchInputReader
│   ├── TimingEvaluator
│   ├── StepPhaseController
│   ├── RunnerKinematics
│   ├── RunnerBodyPose
│   └── RunnerIKRig
├── OpponentService
├── CameraController
├── ReplayService
└── UI
    ├── HomeScreen
    ├── EventSelectScreen
    ├── EntryGateModal
    ├── RaceHUD
    ├── ResultsScreen
    ├── DailyGiftPopup
    ├── SevenDayCheckInPanel
    └── CosmeticsScreen
```

### 13.1 Örnek ScriptableObject veri parçaları [TASARIM]

```csharp
[CreateAssetMenu(menuName = "SpeedClone/Race Event")]
public sealed class RaceEventDefinition : ScriptableObject
{
    public string EventId;
    public string DisplayName;
    public bool RequiresTicket; // orijinal paid/free ayrımından doğrulanacak
    public bool AwardsPurpleGems; // yalnız 400m, 60mH, Medley
}
```

```csharp
[CreateAssetMenu(menuName = "SpeedClone/Runner Tuning")]
public sealed class RunnerTuning : ScriptableObject
{
    public AnimationCurve TimingToStepQuality;
    public AnimationCurve StepQualityToImpulse;
    public AnimationCurve SwingFootArc;
    public float ContactPhaseFraction;
    public float TargetStrideLength;
    public float FootClearance;
    public float BodyLeanAtStart;
}
```

**Kurallar:** Yarış ekonomisi (kaynak/ödül) simülasyondan ayrıdır. IK, ödül hesabı için bir veri kaynağı olamaz. `RaceEntryGate` her girişte aynı merkezi kontrolü kullanır; UI butonlarının her biri farklı para harcama yolu yaratamaz.

---

## 14. KALICILIK / ZAMAN / GÜVENİLİRLİK [TASARIM]

Önerilen kalıcı state:

```text
PlayerProgress
  Cash
  PurpleGems
  EntryTickets (0..3)
  TimedTickets
  TimedAccessExpiryUtc
  EntryTicketRegenDueUtc
  DailyEntryClaimKey
  SevenDayCheckInState
  OwnedCosmetics
  SelectedCosmetics
  BestTimesByEvent
```

- Ekonomi işlemlerinin tekilleştirilmesi ve atomik kaydı önerilir.
- Günlük reset ve biletten bağımsız saat kayması tasarımı gerekir.
- Oyun arka plana gidip geri döndüğünde süreli bilet kalan süresi gerçek saatten türetilir; yarış devam ediyorsa yarışı kesme.
- Unity PlayerPrefs kritik ekonomi için tek güvenilir kaynak olarak kullanılmamalı; kalıcı kayıt/servis yaklaşımı sonraki üretim kararında seçilmeli.

---

## 15. REPLAY, GHOST, OPPONENT [VİDEO] / [TASARIM]

- Video sonuç panelinde **Watch replay** seçeneğini gösteriyor.
- Görüntüde aynı yarışta başka koşucular bulunuyor.
- **[TASARIM]** Replay için deterministik input log (timestamp + side + phase) ya da pozisyon/pose snapshot formatı değerlendirilir; IK versiyonu değişince replay doğruluğu test edilir.
- **[AÇIK]** Rakiplerin gerçek oyuncu, ghost, bot veya karma olduğu; ghost/leaderboard backend'i ve rakip seçim kriterleri bu videodan kesinleştirilemez.
- Offline minimum çözüm olarak test için sabit zaman/pozisyon profilli rakipler kullanılabilir; bu, nihai referans davranışın doğrulandığı anlamına gelmez.

---

## 16. MOBİL PERFORMANS / TEST [TASARIM]

- Hedef cihaz ve minimum desteklenen Android/iOS sürümleri **[AÇIK]**.
- Referans için 60 FPS görsel akıcılık hedeflenebilir; kesin FPS gereksinimi kullanıcı onayı ve cihaz testine bağlı.
- IK solve sayısını kontrol et; rakip karakterlerde LOD/animasyon güncelleme politikası belirle.
- Trail/parçacık efektleri kozmetik envanteri büyüdüğünde de performansı bozmasın.
- Simülasyonun `FixedUpdate` adımına bağımlılığı, input latency ve race timing ölçülsün.
- Telefon aspect ratio ve çoklu dokunma stres testleri yapılmalı.

---

## 17. ÜRETİM AŞAMALARI / DONE KRİTERLERİ [TASARIM]

### Faz A — Running Prototype (öncelik 1)

- [ ] 100m test sahnesi; yandan kamera; başlangıç/finish.
- [ ] Sol/sağ dokunma input recorder ve debug overlay.
- [ ] İki bacak için adım-faz state machine.
- [ ] Ayak/diz hedefli procedural IK; foot lock ve pelvis eşleme.
- [ ] Girdiye tepkili kinematik/velocity.
- [ ] Referans videonun sprint start, orta yarış ve finish karelerine karşı yan yana karşılaştırma.
- [ ] Race result ekranı ve replay başlangıç prototipi.

**Done:** Giriş → karakter tepki süresi, adım hissi, poz uyumu ve 100m yarış başından sonuna akışı testle değerlendirilebilir.

### Faz B — Race Framework

- [ ] Yarış katalogları ve ücretli/ücretsiz doğrulaması.
- [ ] Yarış sonuç/sıralama merkezi.
- [ ] Rakip/ghost sistemi seçeneklerinin araştırılması.
- [ ] 200m, 400m, 60mH, 110mH, 400mH, 4×100m, Medley ve diğer referans modlarının kurallarının ayrı doğrulanması.

**Done:** Her yarış, gerçek referans kurallarıyla tanımlanmış; adı var fakat mekaniği uydurulmuş mod kalmamış.

### Faz C — PDF Economy/UI

- [ ] Süreli Bilet 1 saat gerçek zaman kontrolü.
- [ ] Adet Bileti 3 limit / 30 dk yenilenme / günlük +1.
- [ ] Giriş tüketimi ve iade olmama senaryoları.
- [ ] Mor Mücevher dağıtımı yalnız 400m, 60mH, Medley ilk 3.
- [ ] Home kaynak çubukları ve timer.
- [ ] Daily Login pop-up / kaynak bara uçan kart animasyonu.
- [ ] Premium kozmetikler, kategori başına 2, fiyat 50.
- [ ] 7 Günlük Ödül UI (6. gün ödülü belirlendiğinde konfigürasyon tamamlanır).

**Done:** Kaynak kazanımı/harcaması, timer ve UI PDF kurallarıyla birebir testten geçer.

### Faz D — Polish / Validation

- [ ] Video referansındaki karakter posture, kamerayla kadraj ve yarış hızı his kalibrasyonu.
- [ ] Kozmetik rig/IK uyumu ve görsel efekt optimizasyonu.
- [ ] Arka plana geçiş/yeniden açılış/çevrim dışı durumları.
- [ ] Çifte ödül, çifte bilet harcama, saat manipülasyonu testleri.
- [ ] Ekonominin farklı yarış sonuçlarında regresyon testleri.

---

## 18. AÇIK KARARLAR / KAYNAKTA YOK

**Kaynak belgede tanımlanmadığından kendiliğimizden belirlemeyeceğiz:**

1. 7 günlük ödülün **6. günü**.
2. Ödül gününün reset saati, ardışıklık/kaçırılan gün/döngü kuralları.
3. Hangi yarışların orijinalde ücretli giriş gerektirdiğinin **tam listesi**.
4. Süreli Bilet ve Adet Bileti'nin Nakit karşılığı veya reklam edinim oranları; PDF bunları fiyatlandırmıyor.
5. Nakit kazanım kaynakları ve diğer temel mağaza fiyatları.
6. Kozmetik kategori sayısı, gerçek premium varyantlar, asset adları.
7. Speed Stars fizik/IK/input matematiği, cadence ve enerji değerleri; videodan ölçülmeden kesinlenmez.
8. Engel yarışları, 4×100m ve Medley ayrıntılı kontrol/yarış kuralları.
9. Ghost/backend/leaderboard ve kayıt doğrulama yaklaşımı.
10. Sokak Yarışı teması için **hangi çevresel unsurların** değişeceği; mekaniklerin sabit kalacağı net.
11. Hedef platform teknik kapsamı, cihaz gereksinimleri, input modları, performans bütçesi.
12. Orijinal oyuna özgü adlar, ekranlar, asset'ler ve olası kullanım hakları.

---

## 19. REFERANS İLE UYGULAMA ARASINDA KURAL

- **Gameplay referansı**, koşu kontrolünü ve yarış hissini doğrulamak için kullanılır; kaynak koduna erişim varmış gibi yapılmaz.
- **PDF** ekonomi ve UI değişiklikleri için tek belirleyici kaynak olarak kalır; özellikle 1 saat / 30 dk / 3 maksimum / 50 mücevher / 3-2-1 ödüller değişmez.
- **Sokak Yarışı** sadece temadır; “sokak” kelimesi yarış türü ya da mekanik ekleme izni vermez.
- **Unity IK** uygulama kararıdır; 100m önce tune edilir, sonra ortak runner altyapısı diğer modlara taşınır.
- **Açık noktalar** test veya kullanıcı onayıyla doldurulur; bu sürümde icat edilmez.

**Belge sonu — Speed Stars Clone V1.0 / Unity + IK.**
