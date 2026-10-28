# HİS VE GERİ BİLDİRİM (polish / juice)

> Kullanıcı birebir (21.09.2026): *"biz oyuna polish atarız mesela. yukarıdaki
> süre timer. en başından beri orda duruyordu ama hiçbir efekti yoktu mesela.
> domuzlara vurunca hiçbir feedback yoktu. bu tarz yaptığımız çok fazla sistem
> oldu. node kısımlarında açık kapalı alınmış vs gibi görsel feedbacklerimiz
> oldu."*

---

## 0. Asıl ders: geri bildirim bir FAZ değildir

Bu projede sonunda çok iyi bir geri bildirim katmanı oluştu —
`CameraShakePresenter`, `HapticFeedbackRouter`, `ToneThrottleGate`,
`DamagePopup`, `PigDespawnPoofPresenter`, çatlak shader'ı, skill banner'ları.
**Ama hepsi Faz 3'te, sistemler yazıldıktan aylar sonra geldi.**

O aya kadar:
- Süre sayacı ekranda duruyordu ve **hiçbir şey yapmıyordu** — son saniyelerde
  bile kıpırdamıyordu.
- Domuza vuruyordun, **hiçbir şey olmuyordu.**
- Bir düğüm açık mı kapalı mı, bakarak anlaşılmıyordu.

Bunlar "polish" değildi, **eksik sistemdi.** Vuruşun geri bildirimi yoksa
vuruş mekaniği bitmemiştir.

> **KURAL: Bir sistem, geri bildirimi olmadan `done` olamaz.**
> Task dosyasında geri bildirim satırı boşsa iş yarımdır.

---

## 1. Her etkileşimin beş kanalı

Yeni bir etkileşim (vuruş, kırılma, satın alma, açılma, hata) yazan her task'ın
dosyasında bu blok **doldurulmuş** olmalı. Boş bırakılan her satır **gerekçe**
ister — "gerekmiyor" geçerli bir gerekçedir, ama yazılmış olmalı.

```
## Geri bildirim
GÖRSEL : <renk flaşı / scale punch / partikül / iz / çatlak>
SES    : <hangi Sfx enum değeri>
DOKUNSAL: <hangi HapticTone / yok>
ZAMAN  : <hit-stop var mı, kaç ms / yok>
KAMERA : <shake yoğunluğu / yok>
```

Referans değerler (bu projede oturmuş olanlar):

| Kanal | Tipik | Not |
|---|---|---|
| Scale punch | `1.0 → 1.15 → 1.0`, ~120 ms | Vuruş alan her şey |
| Renk flaşı | 60–100 ms, tek kare değil | Tek karelik flaş mobilde görünmez |
| Hit-stop | 40–80 ms | Sadece **önemli** vuruşta; her vuruşta yaparsan oyun kekeler |
| Kamera shake | 0.05–0.15 birim | **Arbiter üzerinden**, doğrudan değil |
| Ses | Her etkileşimde | Kırılma/başarı sesi olmadan hiçbir şey tatmin etmez |

---

## 2. Durum görünürlüğü — "açık mı kapalı mı"

Kullanıcının bahsettiği düğüm (node) açık/kapalı geri bildirimi bir genel
kuralın örneği:

> **İkili durumu olan her şey, o durumu BAKARAK anlatmalı.**

Kilitli/açık · seçili/seçilmemiş · parası yeter/yetmez · aktif/cooldown ·
kullanılabilir/tükendi · yeni/görülmüş.

Kurallar:
- **Tek başına renk yetmez.** Renk + (ikon | ölçek | şeffaflık | kenarlık).
  Küçük ekranda ve renk körlüğünde renk tek başına okunmaz.
- **Geçiş anımlanır.** Kilit açılıyorsa *açılma* görünür; sessizce durum
  değiştiren şey fark edilmez. Değişim anı, durumun kendisinden önemlidir.
- **Erişilemez olan gizlenmez, SÖNÜKLEŞTİRİLİR** — oyuncu neyi hedefleyeceğini
  görmeli.
- **İşareti gövde rengiyle yapma.** Bu projede Assembly Line işaretlenen domuzu
  maviye boyuyordu; hasar flaşı, yanma ve domuz tipi aynı renk kanalını
  kullandığı için üçü birden boğuldu. Çözüm `PigMarkVisual`: **ayrı bir nesne**,
  kafa üstünde. Gövde rengine hiç dokunulmuyor.

---

## 3. Sayaçlar, süreler, ilerleme

Kullanıcının verdiği ilk örnek buydu — ekranda duran ölü sayaç.

- **Değişen sayı anında zıplamaz, ANİMLE gelir** (tick-up + küçük punch).
- **Süre sayacının üç bölgesi vardır:** normal · uyarı · kritik.
  Kritik bölgede renk + nabız + tik sesi. Sessiz biten süre, bitmemiş sayılır.
- **Hedef sayaçları (`03/05`) her artışta punch atar.** Artışı görmeyen oyuncu
  ilerlediğini bilmez.
- **Sıfıra inen bir şey sıfırda kaybolmaz**, bir kapanış hareketi yapar.

---

## 4. Çakışma yönetimi — geri bildirim bütçesi

20 domuz aynı anda kırılırsa 20 ses çalmaz, ekran titremez.

- **Kamera sarsıntısı tek elden.** Bu projede `CameraShakePresenter` + arbiter
  kalıbı var: birden çok sistem aynı anda sarsmak isterse **en güçlüsü** kazanır,
  toplanmaz. Doğrudan kameraya dokunan kod yazma.
- **Ses kısma kapısı.** `ToneThrottleGate` kalıbı: aynı sesin N ms içinde
  tekrarı bastırılır. Yeni projede bu, `AudioManager`'ın içinde olmalı —
  çağıran tarafın düşünmesi gereken bir şey değil.
- **Partikül tavanı:** aynı anda görünecek efekt sayısı havuz boyutuyla sınırlı.
  Havuz boşsa **en eskisini geri al**, yenisini iptal etme.

---

## 5. VFX ve prefab kuralları

- **Her efekt prefab'tır** ve havuzdan gelir. Kodla `ParticleSystem` kurma.
- Hazır bir efekti uyarlarken **prefab VARYANTI** oluştur, kopya değil —
  kaynak güncellenince varyant da güncellenir.
- **`Play on Awake` kapalı.** Efekti kod başlatır, kod durdurur.
- Döngüsel (looping) efektlerde: efektin kendisini `Play`/`Stop` etmek yerine
  **emisyonu** aç/kapa. Bazı efekt sistemleri (`CFXR` gibi) `IsAlive(false)`
  olunca GameObject'i **yok eder**.
- **Hareket eden efekti taşırken önce deaktive et, sonra taşı, sonra aktive et.**
  Aktif haldeyken ışınlanan "mesafeye göre emisyon" sistemi arkasında düz bir
  çizgi bırakır.
- Kafa üstü / nesneye yapışık efektler nesnenin **çocuğu** olur (Local uzay);
  yerde iz bırakanlar **World** uzayda kalır. Bu seçim bilinçli yapılır.

---

## 6. "İyi hissettirsin" bir kabul kriteri değildir

Task dosyasında `- [ ] iyi hissettirsin` yazamayız — doğrulanamaz.
Ölçülebilir hale getirilir:

```
- [ ] Vuruşta hedef 120 ms içinde 1.0→1.15→1.0 scale punch yapar
- [ ] Kırılmada Sfx.PigBreak çalar ve havuzdan BreakPoof efekti çıkar
- [ ] Son 5 saniyede sayaç kırmızıya döner ve saniyede bir nabız atar
- [ ] Aynı anda 10 kırılmada en fazla 3 ses çalar (throttle)
```

Son onay yine de **gözle** verilir — ama kriter, gözün neye bakacağını söyler.

---

## 7. Geri bildirim mimarisi (02 ile bağlantı)

Geri bildirim, mantığın **içine** yazılmaz; mantığın **yanına** takılır.

```
PigHealth            →  C# event: Died(DamageInfo)
   ↓
PigFeedbackView      →  event'i dinler, UnityEvent onPigBroke tetikler
   ↓  (Inspector'dan bağlanır — TASARIMCININ ELİNDE)
   partikül · ses · shake · hasar yazısı
```

Böylece:
- Mantık sınıfı test edilebilir kalır (görsel bağımlılığı yok)
- Tasarımcı efekti kod yazmadan değiştirir
- Geri bildirim kapatılınca oyun hâlâ çalışır

Bu projede bu ayrım `ISkillFeedbackPorts` + `SkillProcFeedbackRouter` ile
yapıldı ve **iyi çalıştı**. Tek hatası geç gelmesiydi.

---

## 8. Kontrol listesi — her yeni etkileşim için

- [ ] Beş kanal bloğu dolduruldu (boş olanlar gerekçeli)
- [ ] Durum değişimi bakarak anlaşılıyor, geçişi animasyonlu
- [ ] Efekt prefab, havuzdan geliyor, `Play on Awake` kapalı
- [ ] Ses enum ile çağrılıyor, throttle'dan geçiyor
- [ ] Kamera sarsıntısı arbiter üzerinden
- [ ] Sunum katmanı `UnityEvent` ile Inspector'a açık
- [ ] Kabul kriteri ölçülebilir yazılmış
