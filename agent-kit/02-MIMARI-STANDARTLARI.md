# MİMARİ STANDARTLARI

> Kullanıcı birebir (21.09.2026): *"zenject mimarisi kullanalım audio manager
> pooling system. event system kullanalım (Unity Event) temiz mimari yazalım
> solide uygun olsun. objelerde find vs arama yapmayalım enumler kullanalım.
> kalabalık kod over engineering e girmeyelim."*
>
> Bu dosya o cümlenin uygulanabilir hâli. Her kural bu projede gerçekten
> yaşanmış bir soruna dayanıyor — genel tavsiye değil.

---

## 0. Ölçülen mevcut durum (yeni projede tekrarlanmayacak)

`Bills Must Be Paid` üzerinde sayıldı, 21.09.2026:

| Ölçüm | Sayı | Sorun |
|---|---|---|
| `Find` / `FindObjectOfType` / `FindObjectsOfType` çağrısı | **91** | Her biri sessiz kırılma adayı |
| Zenject bind'i | **2** (`AudioManager`, `PoolingSystem`) | DI kurulmuş ama fiilen kullanılmıyor |
| `Singleton<T>` + Zenject aynı sınıfta | `PoolingSystem` | İki ayrı erişim yolu = iki gerçek |
| `EventManager` (statik event bus) | **var, 6 dosya** | Assembly-CSharp'ta; 13 asmdef'li oyun assembly'si ona ULAŞAMIYOR — bkz. §5 |
| Serialize `UnityEvent` alanı | **0** | Agent'lar prefab'a dokunamadığı için bağlayamıyorlardı |
| String anahtarlı API | `AudioManager.Play(string)` | Typo derlemede yakalanmıyor |

Yeni proje bu beş satırın tersiyle başlar.

---

## 1. Zenject — nasıl kullanılır, nasıl kullanılmaz

### Kurulum
- **`ProjectInstaller`** (tek, global): oturum boyu yaşayanlar —
  `AudioManager`, `PoolingSystem`, `SaveService`, `SettingsService`, `EventBus`.
- **`SceneInstaller`** (sahne başına): o sahneye ait servisler.
- Bağımlılık **`[Inject]`** ile alınır. `FindObjectOfType` ile **alınmaz**.

```csharp
public sealed class PigSpawner : MonoBehaviour
{
    private PoolingSystem _pool;
    private AudioManager _audio;

    [Inject]
    public void Construct(PoolingSystem pool, AudioManager audio)
    {
        _pool = pool;
        _audio = audio;
    }
}
```

### Yasaklar
- **`Singleton<T>` ile DI'yı KARIŞTIRMA.** Bu projede `PoolingSystem` hem
  `Singleton<PoolingSystem>` hem Zenject'e bind'li. İki erişim yolu var,
  ikisi farklı zamanlarda hazır oluyor ve hangisinin kazandığı yazılı değil.
  **Bir sınıfın tek erişim yolu olur.** DI seçildiyse `Instance` silinir.
- **`FromComponentInHierarchy` kırılgandır** — nesne sahneden çıkınca bind
  sessizce null döner. Kalıcı servisi prefab'tan bind et:
  `Container.Bind<AudioManager>().FromComponentInNewPrefab(audioPrefab).AsSingle().NonLazy();`
- **Her şeyi inject etme.** Saf veri sınıfı (`DamageInfo`, `WaveSpec`) inject
  edilmez, `new`'lenir. DI **servisler** içindir, **değerler** için değil.
- **`DiContainer`'ı sınıfın içine taşıma** (service locator). Bu, DI'yı
  gizlenmiş bir singleton'a çevirir.

---

## 2. `Find` yasağı — ve yerine ne konur

`Find` ailesi sessizce yanlış cevap verir. Bu projede iki kez kanıtlandı:

- **`FindObjectsOfType`, `HideFlags.DontSave` nesnelerini ATLAR.** Bu yüzden
  sahnede duran bir bileşen için "kurulu değil" raporu verdim — iki kez.
- **`GameObject.Find` yalnızca AKTİF nesneleri bulur.** Kapalı duran `Fail`
  paneli hiçbir zaman bulunamadı; kod sessizce hiçbir şey yapmadı.

### Yerine, tercih sırasıyla

| Sıra | Yöntem | Ne zaman |
|---|---|---|
| 1 | **DI inject** | Servisler (`AudioManager`, `PoolingSystem`, `SaveService`) |
| 2 | **`[SerializeField]` referans** | Aynı prefab/sahne içindeki nesneler |
| 3 | **Kayıt defteri (registry)** | Çok sayıda dinamik nesne: `OnEnable`'da kaydol, `OnDisable`'da çık |
| 4 | **Event** | "Şunu bul ve haber ver" yerine "olduğunda haber versinler" |

Kayıt defteri kalıbında tuzak: bu projede `RunBinder` her domuzun
`wanderer.enabled` değerini `false` yapıyordu, bu yüzden `OnEnable` tabanlı
defterler oyunda **hep boş kaldı**. Defteri `OnEnable`'a değil, açık bir
`Register()` çağrısına bağla.

### Tek istisna
Editör araçları (`#if UNITY_EDITOR`) ve tek seferlik bootstrap. Bootstrap'ta
bile pasif nesne aranıyorsa sahne köklerinden `Transform.Find` ile inilir.

---

## 3. String yerine enum

`AudioManager.Play("lose")` yazımı derlemede yakalanmaz. `"Lose"`, `"loose"`,
`"lose "` — üçü de sessizce hiçbir ses çalmaz.

```csharp
// YANLIŞ
audio.Play("lose");
pool.Spawn("BossArrow");

// DOĞRU
audio.Play(Sfx.Lose);
pool.Spawn(PoolId.BossArrow);
```

Kural:
- Ses, havuz, VFX, ekran, skill ve domuz tipi kimlikleri **enum**dur.
- Enum → asset eşlemesi bir **ScriptableObject tablosunda** durur
  (`[SerializeField] SfxEntry[] entries;` her biri `enum` + `AudioClip`).
- Tablo eksikse **Editör'de hata ver**: enum'daki her değerin karşılığı var mı
  diye kontrol eden bir `OnValidate` yaz. Sessiz no-op en pahalı hatadır.
- `switch` yazarken **`default` şart** — enum büyüdüğünde kardeş kodun
  sessizce yanlış dala düşmesini engeller. (Bu projede enum büyümesi kardeş
  testleri kırdı; `systems-agent` hafızasında kayıtlı.)

---

## 4. Pooling

**Sahnede `Instantiate`/`Destroy` edilen her şey havuzdan gelir:** VFX, mermi,
hasar yazısı, coin, düşman, UI satırı.

```csharp
public interface IPoolable
{
    void OnSpawn();     // durumu SIFIRLA
    void OnDespawn();   // aboneliği bırak, coroutine durdur
}
```

Kurallar:
- **`OnSpawn` durumu sıfırlar.** Havuzdan çıkan nesne önceki hayatını
  hatırlamaz. Bu projede bir işaret bayrağı için tam olarak bu not düşülmüştü:
  *"nesne havuzlanırsa yanlış 'işaretli' durumda uyanmasın."*
- **Havuza oyun mantığı koyma.** Mevcut `PoolingSystem`'de şu satır var:
  `if (sourceObj.ID == "BossArrow" && PlayerPrefs.GetInt("_level") != 16) continue;`
  Havuz bir altyapıdır; bölüm numarasını bilmemeli. Böyle bir koşul çağıran
  tarafta durur.
- Havuz kimliği **enum**.
- Ön ısıtma (`prewarm`) sayısı config asset'inden gelir, koddan değil.

---

## 5. Event sistemi — statik EventManager

> Kullanıcı (21.09.2026): *"bir tane monobehaviour olmayan cs var. içinde
> static eventler var. x code dan event manager.xx.invoke deyince o çalışıyor
> ve tüm on enable on disable de bu eventi dinleyen kodlar çalışıyor...
> yeni projede kullanalım kod atamalarından kurtuluruz güzel bir sistem."*

**Kullanıyoruz. Bu, yeni projenin varsayılan olay mekanizmasıdır.**

### Bu projede neden kullanılmadı — assembly duvarı

Ölçüldü 21.09.2026: `Assets/Game/Scripts/Managers/EventManager.cs` **zaten
var** (27 satır, `static event UnityAction` + `OnX()` invoker'ları) ve 6 dosya
onu kullanıyor — 8 abone, 8 bırakma, 5 invoke. Yani kalıp doğru kurulmuş.

Ama onu kullanan **altı dosyanın altısı da Assembly-CSharp'ta**:
`Managers/`, `Core/Projectiles/`, `UI/`. Oyunun asıl sistemleri ise asmdef'li
assembly'lerde: `Game.Run`, `Game.Pigs`, `Game.Skills`, `Game.Economy`,
`Game.Meta`, `Game.HUD`, `Game.Machine`, `Game.Feedback` (13 tane).

> **Bir asmdef assembly'si Assembly-CSharp'a referans VEREMEZ.** (Tersi
> otomatiktir.) Yani `Game.Run` isteseydi bile `EventManager`'ı göremezdi.

Bu yüzden her sistem kendi `event Action`'ını açtı ve bağlamayı kompozisyon
kökü yaptı → **`RunBinder.cs` iki günde 50 kez düzenlendi.** Sevdiğin sistem
oradaydı, duvarın arkasında kaldı.

### Yeni projede kural: EventManager KENDİ asmdef'inde

```
Game.Core.Events   (asmdef, hiçbir şeye referans vermez)
      ↑     ↑     ↑
  Game.Run  Game.Pigs  Game.UI ...     ← hepsi buna referans verir
```

`Game.Core.Events` yaprak bir assembly'dir; içinde yalnızca `EventManager`,
olay tipleri ve enum'lar bulunur. Böylece **her sistem her olayı görebilir**
ve kimse kimseye referans vermek zorunda kalmaz.

Bu tek karar, kompozisyon kökündeki çakışmanın büyük kısmını ortadan kaldırır.

### Yazım biçimi — mevcut EventManager'daki gibi

```csharp
public static class EventManager
{
    public static event Action<WaveIndex> WaveStarted;
    public static void OnWaveStarted(WaveIndex i) => WaveStarted?.Invoke(i);

    public static event Action<PigDeath> PigDied;
    public static void OnPigDied(PigDeath d) => PigDied?.Invoke(d);
}
```

- **Kavram başına bir açık event.** `Raise<T>()` tarzı generic bus YAZMA:
  daha "akıllı" ama "bu olayı kim dinliyor" aranamaz hale gelir ve
  over-engineering yasağına girer (§7).
- **Invoke sarmalayıcısı (`OnX`) şart.** `?.Invoke` sadece tanımlandığı
  sınıftan çağrılabilir; sarmalayıcı olmadan dışarıdan tetiklenemez.
- `UnityAction` yerine **`Action`** kullan. `UnityAction` düz bir delegedir,
  `Action`'a göre hiçbir faydası yok; faydası yalnız serialize edilen
  `UnityEvent` alanlarında ortaya çıkar.
- Olay verisini **struct/readonly sınıf** olarak taşı (`PigDeath`), beş ayrı
  parametre değil — imza değişince tüm dinleyiciler kırılmasın.

### Abonelik: `OnEnable` / `OnDisable` simetrisi

```csharp
private void OnEnable()  => EventManager.PigDied += HandlePigDied;
private void OnDisable() => EventManager.PigDied -= HandlePigDied;
```

Bu simetri **pazarlık konusu değil.** Statik event, yok edilmiş bir
MonoBehaviour'a güçlü referans tutar: bırakılmayan abonelik hem sızıntı hem
`MissingReferenceException` demektir. Havuzlanan nesnelerde ise
`OnEnable`/`OnDisable` zaten havuza giriş-çıkışla eşleştiği için bu kalıp
**pooling ile çok iyi çalışır** — ek bir şey gerekmez.

### Dört sessiz tuzak ve önlemi

1. **Domain reload kapalıysa statikler play oturumları arasında YAŞAR.**
   (Enter Play Mode Options → Reload Domain kapalı.) Önceki oturumun
   dinleyicileri kalır → çift tetikleme, ölü referans.
   → `EventManager`'a bir temizleyici koy:
   ```csharp
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
   private static void ResetAll() { WaveStarted = null; PigDied = null; /* ... */ }
   ```
   **Her yeni event eklendiğinde bu listeye de eklenir.**
2. **Dinleyici sırası tanımsızdır.** İki dinleyicinin hangisinin önce
   çalışacağına güvenme. Sıra gerekiyorsa olay değil, açık bir çağrı zinciri kullan.
3. **Test izolasyonu.** Statik durum testler arasında sızar. EditMode
   testlerinde `[SetUp]`/`[TearDown]` içinde `ResetAll()` çağır.
   (Bu projede `PlayerPrefs` için aynı sorun yaşandı ve aynı şekilde çözüldü.)
4. **Invoke sırasında abonelik değiştirmek.** Dinleyicinin içinde
   `-=`/`+=` yapmak koleksiyonu davet ediyor. Gerekiyorsa bayrak koy,
   bir sonraki karede uygula.

### Ne zaman event DEĞİL

| Durum | Bunun yerine |
|---|---|
| **Değer sormak** ("şu an kaç para var?") | DI ile servis inject et, ya da `Func<>` seam |
| **Her karede her varlık** için çalışan sıcak döngü | Doğrudan çağrı / delege |
| **Sıra veya dönüş değeri gerekiyorsa** | Açık çağrı |
| **Sadece iki sınıf ilgileniyorsa** | Doğrudan referans; global yapma |

Kural: **event bir BİLDİRİMDİR, bir SORGU değil.** Sorgu için DI ve seam var.

### Ayrıca: serialize edilen `UnityEvent` (ayrı bir araç)

Bunlar farklı şeyler; ikisi birlikte kullanılır:

| | **static EventManager** | **`[SerializeField] UnityEvent`** |
|---|---|---|
| Ne | Sistemler arası bildirim | Tek prefab içinde, Inspector'dan bağlanan tepki |
| Bağlayan | Kod (`+=`) | Tasarımcı (Inspector) |
| Kapsam | Global | O prefab'a ait |
| Örnek | `PigDied` | O domuzun kırılma partikülü + sesi |

Inspector'dan bağlanan tarafın tek ciddi riski: bağlantı hedef metodun adını
**string** olarak saklar, rename edilince **sessizce kırılır**. Bu yüzden
Inspector'a açılan metotların adı sözleşmedir.

## 6. SOLID — pragmatik okuma

Harf ezberi değil, bu projede karşılığı olan beş kural:

- **S — Tek sorumluluk.** `RunBinder.cs` iki günde **50 kez** düzenlendi çünkü
  her yeni sistem "beni de bağla" diye oraya indi. Çözüm: sistem kendi
  **seam**'ini açar (`Func<float>` provider, arayüz, `Initialize(...)`
  parametresi), bağlamayı tek yerden kompozisyon kökü yapar.
- **O — Açık/kapalı.** Yeni skill eklemek mevcut skill kodunu değiştirmemeli:
  veri (SO) + ortak arayüz. Bu projede 30 skill böyle eklendi ve işledi.
- **L — Yerine geçebilirlik.** Alt tür, üst türün sözleşmesini daraltmaz.
  Test double'ı "gerçek config'i kopyala" diye kurma — testin başlangıç
  durumuyla çatışır ve sessizce yanlış yolu ölçer.
- **I — Dar arayüz.** `ISkillFeedbackPorts` gibi, çağıranın ihtiyacı kadar.
  20 metotlu "manager arayüzü" yazma.
- **D — Bağımlılığın tersine çevrilmesi.** Bu projede assembly'ler birbirine
  referans veremediği için her alt sistem `Func<float>` seam'i açtı ve
  kompozisyon kökü lambda'yı besledi. **Mecburiyetten doğdu ama doğru olan
  buydu** — artık istisna değil, kural.

---

## 7. Over-engineering yasağı

Kullanıcı bunu açıkça istedi. Somut sınırlar:

- **Üç kuralı.** Bir kalıbı **üç kez** tekrarlamadan soyutlama yazma.
  İki kez tekrar, kopyala-yapıştırdan daha ucuzdur.
- **Tek uygulayıcısı olan arayüz açma.** İkinci uygulama gerçekten geldiğinde aç.
- **"İleride lazım olur" parametresi ekleme.** Lazım olduğunda eklersin.
- **Katman sayısı üçtür:** ince `MonoBehaviour` → saf C# servis → veri (SO).
  Dördüncü katman (`Manager` → `Controller` → `Handler` → `Service`) yazılı
  gerekçe ister.
- **Event zinciri ikiyi geçmesin.** A → B → C tamam; A → B → C → D → E'de
  hatayı kimse bulamaz.
- **Dosya 300 satırı geçerse böl** — ama bölmek için bölme; bölünen parçanın
  kendi başına anlamlı bir adı olmalı.
- **Erken genellik yok:** `IRepository<T>`, `BaseManager<T1,T2>`, generic event
  bus üzerine generic wrapper. Bunlar mobil bir oyunda maliyet, fayda değil.

> Ölçü: **yeni bir feature eklerken kaç dosya açman gerekiyor?** Üçten
> fazlaysa mimari sana çalışmıyor, sen mimariye çalışıyorsun.

---

## 8. Kod hijyeni (derlemede veya incelemede yakalanır)

- `Update` içinde `GetComponent` **yok** — referans `Awake`/`Initialize`'da alınır.
- `public` alan yok — `[SerializeField] private` + gerekiyorsa `public` property.
- Magic number yok — config asset'ine taşı.
- Kurulumu `Awake`'e bırakma: **idempotent `Initialize()`** yaz,
  `Awake`/`OnEnable`/`Initialize` üçünden de çağır. (Batchmode test koşucusunda
  `AddComponent<T>()` `Awake`'i çalıştırmaz — bu projede üç ayrı görevde
  gerçek hataya yol açtı.)
- `using System;` + `using UnityEngine;` bulunan dosyada **`Object`, `Random`,
  `Debug`** adlarını asla niteliksiz kullanma. (`CS0104`; dört ayrı agent bu
  tuzağa düştü.)
- `async void` yok — `async Task` veya coroutine.
- Sahne/prefab referansını koddan **yol stringi** ile çözme; `[SerializeField]`
  veya enum→asset tablosu kullan.

---

## 9. Yeni bir sistem yazarken kontrol listesi

- [ ] Bağımlılıkları `[Inject]` ile alıyor, `Find` kullanmıyor
- [ ] Kimlikler enum, string değil
- [ ] Spawn ettiği her şey havuzdan geliyor, `OnSpawn`'da durumu sıfırlanıyor
- [ ] Mantık C# event yayıyor; sunum katmanı `UnityEvent` ile Inspector'a açık
- [ ] Kompozisyon köküne kendi seam'ini veriyor, oraya kendisi yazmıyor
- [ ] Sayıları config asset'inde
- [ ] Üç kuralına uyuyor: gereksiz arayüz/katman/generic yok
- [ ] **Geri bildirimi var** → bkz. `03-HIS-VE-GERI-BILDIRIM.md`
