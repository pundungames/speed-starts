# DOTWEEN VE UI ANİMASYON — genel geçer reçete

> Kullanıcı birebir (21.09.2026): *"dotween içinde geçerli animasyonları nası
> kullanıyorum genelde. ui panellerine yaptığımız animasyonlarda aynı şekilde.
> attığımız temel polishleri istiyorum senden. x paneline z şeklinde bir polish
> atmış değil yani. genel geçer bir kural."*

Bu dosya panel panel liste değil. **Bir kez kurulan, her panelin aynı şekilde
kullandığı hareket kiti.** Aşağıdaki değerler bu projede oturmuş ve onaylanmış
gerçek değerlerdir (`Game.HUD.Animation`).

---

## 0. Temel kural: hareket panelde değil, KİTTE yaşar

Yanlış: her panel kendi `DOScale`'ini kendi süresiyle yazar → 12 panel,
12 farklı his, hiçbiri toplu değiştirilemez.

Doğru: **tek `UiMotionConfig` ScriptableObject** + üç yeniden kullanılabilir
bileşen. Panel yalnızca "aç" der.

```
UiMotionConfig (SO, Resources/HUD/UiMotionConfig)   ← tüm süre/ölçek/easing
   ├── UiPanelAnimator   : panel aç / kapa
   ├── UiStaggerReveal   : liste elemanlarının sırayla gelmesi
   └── UiButtonPunch     : butona basınca punch
```

Kodda **magic number yok.** Bir panelin hissi değişecekse asset değişir,
kod değişmez — ve o değişiklik **bütün panellere** uygular.

---

## 1. Onaylı değerler (kod varsayılanları)

| Ne | Değer | Neden |
|---|---|---|
| **Panel açılış** süresi | `0.22 s` | Daha uzun = ağır, daha kısa = fark edilmiyor |
| **Panel kapanış** süresi | `0.16 s` | Kapanış her zaman açılıştan **hızlı** |
| Panel açılış scale | `0.90 → 1` | |
| Panel kapanış scale | `1 → 0.95` | Küçülerek gider, sıfıra inmez |
| Panel açılış easing | `Ease.OutBack` | Hedefi hafif aşıp oturur — "canlı" his |
| Panel kapanış easing | `Ease.InQuad` | Kapanışta back **yok**, tereddüt gibi durur |
| **Stagger** aralığı | `0.05 s` | Eleman başına gecikme |
| Stagger eleman süresi | `0.20 s` | |
| Stagger scale | `0.70 → 1` | |
| Stagger easing | `Ease.OutBack` | |
| Stagger **toplam tavan** | `0.60 s` | 40 satır varsa aralık otomatik sıkışır |
| **Buton punch** genliği | `0.12` (+%12) | |
| Buton punch süresi | `0.18 s` | |
| Buton punch vibrato | `6` | |
| **Damga (stamp)** | `1.5 → 1`, `0.14 s`, `OutBack` | "Yapıştırma" hissi **hızdan** gelir |

> Bu tablo yeni projeye aynen taşınır. Değiştirmek serbest — ama **asset'ten**,
> koddan değil.

---

## 2. Panel aç/kapa — kanonik kod

```csharp
private void Open()
{
    KillActiveSequence();                       // 1. önceki tween'i öldür

    var cfg = UiMotionConfig.ResolveOrCreate();
    _canvasGroup.alpha = 0f;
    transform.localScale = _baseScale * cfg.PanelOpenScaleFrom;
    _canvasGroup.interactable = !cfg.BlockInputWhileOpening;   // 2. girdiyi kilitle
    _canvasGroup.blocksRaycasts = true;

    var seq = DOTween.Sequence();
    seq.Insert(0f, transform.DOScale(_baseScale, cfg.PanelOpenDuration)
                            .SetEase(cfg.PanelOpenEase));
    seq.Insert(0f, DOTween.To(() => _canvasGroup.alpha,
                              v  => _canvasGroup.alpha = v,
                              1f, cfg.PanelOpenDuration)
                          .SetEase(cfg.PanelOpenEase));   // 3. DOFade DEĞİL
    seq.SetUpdate(true);                                  // 4. unscaled time
    seq.SetLink(gameObject);                              // 5. nesne ölünce tween ölsün
    seq.OnComplete(() => { _canvasGroup.interactable = true; _activeSequence = null; });

    _activeSequence = seq;
}
```

Beş numaralı maddenin her biri gerçek bir hatadan geliyor:

1. **`KillActiveSequence()` önce.** Kullanıcı paneli hızlı açıp kaparsa iki
   tween aynı `localScale`'i yazar ve panel yarı boyutta takılı kalır.
2. **Açılırken girdiyi kilitle.** Açılış animasyonu sırasında butona basılırsa
   panel hem açılır hem kapanır.
3. **`CanvasGroup.DOFade` KULLANMA — `DOTween.To` kullan.** asmdef'li bir
   assembly'den `DOFade`/`Image.DOFade` çağırmak **CS1929** verir; o kısayollar
   DOTween'in ayrı bir modülünde. `DOTween.To(getter, setter, hedef, süre)` her
   yerden çalışır. *(Bu tuzak bu projede bir tur yedi.)*
4. **`SetUpdate(true)` = unscaled time.** Panel açıldığında `Time.timeScale = 0`
   yapıyorsan (duraklatma) bunu koymazsan animasyon hiç oynamaz.
5. **`SetLink(gameObject)`.** Nesne yok edilince tween otomatik ölür. Yoksa
   tween ölü referansa yazmaya çalışır.

Kapanış aynı iskelet, tek fark: `OnComplete`'te `gameObject.SetActive(false)`
ve `interactable = false` **başta**.

---

## 3. Üç değişmez kural

### 3.1 Kurulu ölçeği yakala, `Vector3.one` varsayma
```csharp
private void Awake() => _baseScale = transform.localScale;
```
Tasarımcı paneli `0.9` ölçekte kurduysa, animasyonun sonunda `1`'e çekmek onun
tasarımını **ezer**. Bu projede hem `UiStaggerReveal`'da hem `CpiStampPop`'ta
aynı tuzağa düşüldü ve ikisinde de böyle düzeltildi.

### 3.2 Tetikleyici `OnEnable`, kodun açma yolu DEĞİL
```csharp
private void OnEnable() { CaptureBaseScale(); Play(); }
```
Animasyonu "paneli açan kod yolu"na bağlarsan, kullanıcı paneli **Inspector'dan
elle** açtığında hiçbir şey olmaz. Bu projede damga animasyonu tam olarak bu
yüzden "çalışmıyor" göründü. `OnEnable`, onu kimin açtığından bağımsızdır.

### 3.3 `OnDisable`'da tween'i öldür
```csharp
private void OnDisable() { _tween?.Kill(); _tween = null; }
```
Havuzlanan/kapanan nesnede yaşayan tween, ikinci hayatında yanlış değerden
başlar.

---

## 4. Sırayla gelme (stagger) — liste/grid için

Kural: **toplam süre tavanı vardır.** 8 satır varsa `0.05` aralık güzel;
40 satır varsa 2 saniye sürer ve oyuncu bekler. Bu yüzden:

```
gerçekAralık = min(hedefAralık, tavan / elemanSayısı)
```

`staggerMaxTotalSeconds = 0.60` → eleman sayısı ne olursa olsun liste
0.6 saniyede tamamlanır, aralık otomatik sıkışır.

Diğer kurallar:
- Stagger **yalnızca açılışta**. Kapanışta hepsi birlikte gider.
- Elemanlar havuzdan geliyorsa animasyon `OnSpawn`'da **sıfırlanır**.
- Scroll'da görünmeyen elemanı animasyona sokma.

---

## 5. Buton geri bildirimi

Her tıklanabilir şey **basıldığını söylemeli**:

```csharp
transform.DOPunchScale(Vector3.one * cfg.ButtonPunchScale,
                       cfg.ButtonPunchDuration,
                       cfg.ButtonPunchVibrato)
         .SetUpdate(true)
         .SetLink(gameObject);
```

- Punch **basıldığı anda** (`OnPointerDown`), bırakıldığında değil.
- Ses ile **aynı karede**. Gecikmiş tık sesi kopuk hissettirir.
- Devre dışı butonda punch **yok** — bunun yerine küçük bir "titreme" (shake)
  ve yetersizlik sesi.

---

## 6. Damga / vurgu (stamp) kalıbı

Bir şey **kesinleşti** demek için (Fail, Bill Paid, Level Up, Yeni Rekor):

```csharp
transform.localScale = _baseScale * 1.5f;
_tween = transform.DOScale(_baseScale, 0.14f).SetEase(Ease.OutBack);
```

- Büyükten küçüğe, **hızlı**. Yavaş yapılırsa "damga" değil "büyüme" olur.
- `OutBack` şart: hedefin biraz altına sarkıp geri oturur — "löp" hissi
  o sarkmadan gelir. (Önce `OutExpo` denendi, kullanıcı `OutBack` istedi.)
- Damga + ses + hafif kamera/shake **aynı anda**.

---

## 7. Sayı animasyonu (tick-up)

Sayılar zıplamaz, **sayarak** gelir:

```csharp
DOTween.To(() => _shown, v => { _shown = v; _label.text = Format(v); },
           hedef, 0.35f)
       .SetEase(Ease.OutCubic)
       .SetUpdate(true)
       .SetLink(gameObject);
```

- Para/skor: `OutCubic`, 0.3–0.5 s.
- Sayaç değişirken etikete küçük bir punch ekle.
- Büyük artışlarda süreyi artırma — **adım sesini** hızlandır.

---

## 8. Easing seçim tablosu

| Durum | Ease | Neden |
|---|---|---|
| Panel/eleman **geliyor** | `OutBack` | Hafif aşıp oturur, canlı |
| Panel **gidiyor** | `InQuad` | Tereddütsüz |
| Sayı sayma | `OutCubic` | Yumuşak yavaşlama |
| Damga / vurgu | `OutBack` | "Löp" |
| Hata / red | `OutElastic` (kısa) veya shake | Reddedildiğini anlatır |
| Sürekli/idle hareket | `InOutSine` + `LoopType.Yoyo` | Nefes alma |

**`Linear` kullanma** — ilerleme çubuğu ve dönen yükleme ikonu dışında.

---

## 9. Performans ve hijyen

- `DOTween.Init` bir kez, `ProjectInstaller`'da. `SetCapacity` ile tween
  kapasitesini baştan ayır (çalışma anında büyüme = GC).
- Her `Update`'te yeni tween **yaratma**. Tween bir olaydır, bir döngü değil.
- Sonsuz döngülü tween'lerde `SetLink` **şart**, yoksa sahne değişince sızar.
- `DOKill(this)` yerine tween referansını tut ve onu öldür — `DOKill` aynı
  hedefteki **başkasının** tween'ini de öldürür.
- UI tween'lerinde `SetUpdate(true)` varsayılan kabul edilir; oyun içi dünya
  tween'lerinde **edilmez** (onlar `timeScale`'e uymalı).
- Tween'in bittiğini `OnComplete` ile bil; `yield return new WaitForSeconds(aynı süre)`
  ile **tahmin etme**.

---

## 10. Yeni bir panel yazarken kontrol listesi

- [ ] `UiPanelAnimator` kullanıyor, kendi `DOScale`'ini yazmıyor
- [ ] Süre/easing `UiMotionConfig`'ten geliyor, kodda sabit yok
- [ ] Kurulu ölçek `Awake`'te yakalanıyor, `Vector3.one` varsayılmıyor
- [ ] Tetikleyici `OnEnable`, açma kod yolu değil
- [ ] `KillActiveSequence()` yeni animasyondan önce çağrılıyor
- [ ] `SetUpdate(true)` + `SetLink(gameObject)` var
- [ ] `CanvasGroup` alpha'sı `DOTween.To` ile (asmdef'te `DOFade` = CS1929)
- [ ] Açılırken `interactable` kapalı, `OnComplete`'te açık
- [ ] `OnDisable`'da tween öldürülüyor
- [ ] Liste varsa stagger toplam süre tavanına uyuyor
