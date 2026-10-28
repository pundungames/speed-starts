# DERSLER — "Bills Must Be Paid" pipeline'ından çıkanlar

> Bu dosya bir övgü metni değil. 2026-08-12 ile 2026-09-21 arasında
> ~170 task, 6 alt agent, 3 faz ve en az 6 kaybedilmiş tur boyunca neyin
> işlediğini ve neyin kırıldığını yazar. Yeni projede aynı duvarlara
> tekrar çarpmamak için var.
>
> **v2 eki (27.10.2026):** K1–K10 21.09'a kadarki dönemdir ve olduğu gibi duruyor.
> K11–K19, 2026-09-21 ile 2026-10-27 arasından çıktı: task dosyası 169'dan 444'e,
> 938 commit, EditMode testi 2314'ten 11.528'e (27.10 tam suite), oyun 0.1.3'ten
> 0.1.9'a (Play'de reklamlı/reklamsız A/B), aynı depoda kullanıcı + iki süpervizör
> oturumu + 3–4 paralel agent + 4 doğrulama kopyası. Bu dönemin kırılmaları artık
> kodda değil, **ortak çalışma alanında** (Editor, kayıt, index, kopya) ve
> **süreçte** (mod, durum dosyası, hafıza) çıktı.
>
> Okuma sırası: önce burası, sonra `01-BASLANGIC-PROTOKOLU.md`.

---

## 0. Bir cümlede

**Pipeline'ın iskeleti (task dosyası → sahipli agent → merkezî doğrulama → hook'lar)
doğruydu ve çalıştı. Kırılan şey hep aynı yerdeydi: diskteki "gerçek" ile
belgedeki "gerçek" birbirinden ayrıldığı anda, agent'lar belgeye inandı.**

Sayılarla: 139 task `done`, 2314 yeşil EditMode testi, ve buna rağmen 14.09
denetiminde "tasarımcı ayarlıyor ama oyun okumuyor" başlığı altında bir sayfa
dolusu ölü ayar çıktı. Yeşil test oyunun çalıştığını kanıtlamadı.

---

## 1. İyi çalışan ve AYNEN taşınacak 6 şey

| # | Ne | Neden işe yaradı |
|---|---|---|
| 1 | **Hook'la zorlanan kurallar** (`hook-guard-write.ps1`, `exit 2`) | Prompt unutulur, hook unutulmaz. Gece 03:00'te bir agent'ın "ben şimdi şu prefab'a bir dokunuvereyim" demesini prompt engelleyemedi, hook engelledi. **Pipeline'ın en değerli tek parçası budur.** |
| 2 | **`owns_paths` — dosya sahipliği** | 6 agent paralel çalışırken tek bir merge çatışması yaşanmadı. Kesişen `owns_paths`'e sahip iki task'ı aynı turda çalıştırmama kuralı bunu tek başına sağladı. |
| 3 | **Task dosyası tek doğruluk kaynağı, supervisor özet geçmez** | "Task'ın içeriğini kendi cümlelerinle özetleme" kuralı bilgi kaybını kesti. Özetlediğim her seferde agent yanlış şeyi yaptı. |
| 4 | **`done` yetkisinin supervisor'da olması** | Agent'lar kendilerine `done` verseydi 40 yarım iş `done` görünecekti. |
| 5 | **Agent hafızası** (`.claude/agent-memory/<agent>/`) | 60+ kalıp dosyası birikti. Aynı agent ikinci kez aynı tuzağa düşmedi. |
| 6 | **Seam + tek kompozisyon kökü** | `RunBinder.cs` 2 günde 50 kez düzenlenmişti. Alt agent'lara yasaklayıp "kendi seam'ini aç, bağlamayı supervisor yapar" dedikten sonra o dosya sakinleşti. |

---

## 2. Kırılma aileleri — gerçekten olanlar

Her başlık: **ne oldu → kök sebep → yeni projede kural.**

### K1. Tasarım belgesi öldü, kimse cenazeye gitmedi

**Ne oldu.** GDD'nin dosya adı bile `..._FINAL_LOCKED_SINGLE_SOURCE.md` idi.
Faz 3'e gelindiğinde oyun bilinçli olarak ondan ayrışmıştı. 11.09.2026'da bir
agent kırmızı testlere ve GDD'ye bakıp domuz canlarını 1.5x yaptı, TimeFreeze'i
7/2/16'ya çekti, bir popup filtresini "regresyon" sanıp geri aldı. **Üçü de
kullanıcının bilerek yaptığı ve beğendiği ayarlardı.** Hepsi geri alındı, bir tur
kayboldu. Kullanıcı sonunda GDD'yi resmen emekliye ayırdı.

**Kök sebep.** Belgeye "FINAL / LOCKED / SINGLE SOURCE" demek, belgenin
eskimeyeceği anlamına gelmiyor — sadece eskidiğinde kimsenin fark etmeyeceği
anlamına geliyor.

**Yeni projede kural.**
- GDD bir **hipotez**tir, sözleşme değil. `docs/gdd/00-README.md`'nin ilk satırı:
  *"Bu belgenin yetkisi ilk oynanabilir dikey dilime kadardır. O günden sonra
  doğruluk kaynağı: çalışan kod + config asset'leri."*
- Belgeye **sayı yazma.** Belge niyeti yazar ("domuz birkaç vuruşta kırılmalı"),
  sayıyı config asset'i tutar.
- GDD ile kod çeliştiğinde varsayılan davranış **kodu düzeltmek değil, durumu
  bildirmek**tir.

### K2. Denge sayıları kullanıcınındır — teste asla gömülmez

**Ne oldu.** Testler `Assert.AreEqual(3, pigHealth)` gibi sabit sayılar tutuyordu.
Kullanıcı sayıyı Inspector'dan değiştirince test kırmızı oldu; agent testi
"gerçek" sanıp **kodu** değiştirdi. Bu, K1'in motoru.

**Kök sebep.** Test, davranışı değil ayarı doğruluyordu.

**Yeni projede kural.**
- Test **ilişki** doğrular: "hasar × vuruş sayısı ≥ can", "hızlı olan önce varır".
  Sabit sayı doğrulamaz.
- Sayı gerekiyorsa testin içine değil, **config asset'inden okunur.**
- Kırmızı bir denge testi = önce supervisor'a rapor, sonra karar. Asla sessiz
  kod düzeltmesi.
- Test dosyasındaki "kullanıcı onayladı, 07.09" yorumları **tarih taşır ve
  eskir.** Yorum, yetki değildir.

### K3. Kodla kurulan UI = düzenlenemeyen oyun

**Ne oldu.** Alt agent'lar sahneye dokunamıyordu. Çare diye "prefab yoksa kod
tarafında iskelet kur" kalıbı (T-059) icat edildi. Sonuç: her şey "çalışıyor"
görünüyordu ama kullanıcı sahneyi açtığında **düzenleyecek hiçbir nesne yoktu.**
Kullanıcının kendi cümlesi: *"şu an sahnede düzenlenecek kontrol edilecek hiçbir
şey yok ve bu kesinlikle istediğimiz bir şey değil."*

**Kök sebep.** Bir aracın kısıtı (agent sahneye yazamaz) bir mimari karara
dönüştü. Kısıt geçiciydi, mimari kalıcı oldu.

**Yeni projede kural.**
- **Sahne/prefab sözleşmesi, ilk koddan ÖNCE yazılır.** Ekranların listesi,
  hiyerarşi isimleri, satır prefab'ı + content parent kalıbı — hepsi Gün 1'de.
- View kodu hazır nesneyi `[SerializeField]` ile **bağlar**, üretmez.
- Prefab hazır değilse ekran **eksik kalır.** Sahte iskeletle "çalışıyor"
  gösterilmez. `NEEDS_SCENE` bırakılır, scene-integrator yapar.
- Tek istisna: `**/Dev/**` altındaki geliştirme araçları, `#if UNITY_EDITOR`
  ile korunmuş.

### K4. Yeşil test ≠ oyunda var

**Ne oldu.** 2314 yeşil test varken denetim, oyuncunun hiç göremediği özellikler
buldu. Sistem yazılmış, test edilmiş, **ama kompozisyon köküne hiç bağlanmamıştı.**
`NEEDS_WIRING` notları task dosyalarında açık kalıp unutuldu.

**Kök sebep.** "Bitti"nin tanımı derleme + test'ti. "Ulaşılabilir" değildi.

**Yeni projede kural.**
- Tur sonu kapısı: `grep -r "NEEDS_WIRING\|NEEDS_SCENE" docs/tasks/` **boş
  dönmeden tur kapanmaz.**
- Bir özellik ancak **oyunda ulaşılabilir** olduğunda `done` olur. Ulaşılamıyorsa
  `review`.
- Seam açan task ile seam'i bağlayan iş **aynı turda** biter. Bağlama bir sonraki
  güne bırakılmaz — bırakılırsa unutulur.

### K5. Yanlış düğme — ölü ayarlar

**Ne oldu.** Wave süresini değiştirmek için `RunConfig.runDurationSeconds`'ı
**üç kez** değiştirdim. Hiçbiri işe yaramadı, çünkü o dal ölüydü:
`WaveDurationDefaults.Instance` hiç null olmadığı için gerçek değer hep oradan
okunuyordu. 14.09 denetiminde bunun gibi bir sayfa dolusu "tasarımcı ayarlıyor,
oyun okumuyor" maddesi çıktı.

**Kök sebep.** Bir değeri değiştirmeden önce **okuma yolunu** kanıtlamadım.

**Yeni projede kural.**
- Bir sayıyı değiştirmeden önce: `grep` ile **tüm okuyucuları** bul, hangi dalın
  fiilen çalıştığını kanıtla. İki kaynak varsa hangisinin kazandığını yaz.
- Aynı değerin iki kaynağı varsa bu bir **hata**dır; ADR açılır, biri silinir.
- Fazda bir kez "ölü ayar taraması" task'ı: her `[SerializeField]`/config alanı
  için en az bir okuyucu var mı?

### K6. Asset ↔ kod ayrışması (sessiz ve zehirli)

**Ne oldu.** Üç ayrı biçimde:
1. Bir prefab bir kez kaydedildiğinde `[SerializeField]`'in ilk değeri YAML'a
   yazılır; **sonraki C# varsayılan değişiklikleri hiçbir etki etmez.**
2. Kabul edilen skill sayıları `SkillCatalog.cs`'e yazılıp `.asset`'e hiç
   geçmemişti — çalışma zamanı asset'i okuyor.
3. `SerializedObject` ile yamalanan bir `.asset` dakikalar içinde sessizce
   HEAD'e geri döndü (bellekteki kopya dahil).

**Kök sebep.** Kod ve asset iki ayrı "gerçek" tutuyor, hangisinin kazandığı
yazılı değil.

**Yeni projede kural.**
- **Çalışma zamanı asset'i okur.** Kod varsayılanı sadece ilk oluşturmada geçerli.
- Asset yaması: **yaz → diskten tekrar oku → saniyeler içinde commit'le →
  20 sn sonra bir daha doğrula.**
- Bir sayının "kabul edildi" sayılması için `.asset` dosyasında görünmesi şart.

### K7. Ortam hatası kod hatası sanıldı

**Ne oldu.** `unity-verify.ps1` exit 12 (Editor projeyi açık tutuyor) döndüğünde
hook bunu "DERLEME BAŞARISIZ" diye kaydetti. 25–28.08 arasında **21 ardışık
exit 12** böyle işlendi ve agent'lar sapasağlam kodu düzeltmeye çalıştı.

**Kök sebep.** "Doğrulanamadı" ile "kırmızı" aynı kutuya konmuştu.

**Yeni projede kural.**
- Her doğrulama kapısında üç durum olur: **yeşil / kırmızı / doğrulanamadı.**
  Üçüncüsü turu bloklamaz ama **sayılır.**
- Ardışık "doğrulanamadı" sayısı bir eşiği geçerse (5) tur durur, insana sorulur.
- Kilit dosyası kalıntısı gerçek bir açık Editor sanılmasın: önce süreç kontrolü.

### K8. Paralel agent'lar aynı duvara ayrı ayrı çarpıyor

**Ne oldu.** İki agent aynı gece, birbirinden habersiz, "EditMode'da
`AddComponent` `Awake`'i çalıştırmaz" tuzağına düştü. Maliyet: bir tur.
Aynı `Object`/`Random` belirsizliği tuzağına **dört ayrı agent bağımsızca** düştü.

**Kök sebep.** Keşif paylaşımı tur **sonunda** oluyordu; tur içinde değil.

**Yeni projede kural.**
- `docs/TUZAKLAR.md` — tek dosya, her agent'ın sistem promptunda "başlarken oku".
- Supervisor bir tuzağı öğrendiği **an** oraya yazar, gece raporunu beklemez.
- Aynı tuzağa ikinci kez düşülmesi bir **pipeline hatasıdır**, agent hatası değil.

### K9. Task dosyaları koddan geride kaldı

**Ne oldu.** 28.08 denetimi: 40 task `review`'da çakılı, `T-026` "blocked"
yazıyor ama iş fiilen çalışıyor, `T-039` "in-progress" yazıyor ama supervisor
onu review yapmış. Dosyalar bayat, statüler anlamsız.

**Kök sebep.** Statü elle yazılıyordu ve kimse geri dönüp kapatmıyordu.

**Yeni projede kural.**
- Her turun son işi: dokunulan task dosyalarının statüsünü **kapatmak.** Tur,
  statüler tutarlı olmadan bitmez.
- `review` bir bekleme odası değil, **kuyruğun başı**dır. 10'dan fazla `review`
  birikmişse yeni feature task'ı açılmaz — önce kuyruk boşaltılır.

### K10. "Bitirdim" beyanına inanmak

**Ne oldu.** Bir "düzeltme turu" commit'i doğru teşhisi yazmış ama **tek satır
koda dokunmamıştı** — sadece belge değiştirmişti. Buna güvenip task'ları "iki kez
başarısız" sayıp yanlışlıkla `blocked` yapmak üzereydim.

**Kök sebep.** Rapor metni ile diff aynı şey sanıldı.

**Yeni projede kural.**
- Agent raporu bir **iddia**dır. `git show --stat` ile doğrulanmadan kabul
  edilmez.
- "Bu testler etkilenmez, marjlar geniş" cümlesi de bir iddiadır; denge sayısı
  değişen turda **tam suite** çevrilir.

---

## 2b. Kırılma aileleri — 21.09–27.10 (v2 eki)

Biçim aynı: **ne oldu → kök sebep → yeni projede kural.** Sayılar süpervizör
hafızasından ve git geçmişinden; teknik ayrıntı `docs/TUZAKLAR.md`'de.

### K11. Test koşusu kullanıcının işini geri aldı

**Ne oldu.** EditMode koşusu bitince kullanıcının prefab düzenlemesi **beş kez**
diske eski haliyle yazıldı: 29.09 (WorkshopPanel), 05.10 (Hand.prefab'a eklenen
altın çekiç), 07.10 (Device_AirBlaster, yalnız 12 testlik filtreli koşu),
09.10 (New Skill Pack), 26.10 (PurchaseCongrats, 857 testlik koşu). Her olaydan
sonra bir koruma ekleyip "artık güvenli" dedim; bir sonraki olay tam o korumanın
altından geçti: `isDirty=false` kontrolü geçmişti (29.09) → "commit'li" idi (05.10)
→ prefab stage kapalıydı (07.10) → hem commit'li hem stage'i ben kapatmıştım (09.10)
→ kullanıcı "çalışmıyorum, bu dalda yap" demişti (26.10). Daha önce 16.09'da
`refresh_unity` aynı biçimde kaydedilmemiş prefab işini silmişti. Kullanıcının tek
kelimesi: *"naptın"*.

**Kök sebep.** Unity Test Framework koşu sonunda "Undo after editor test run"
yapıyor; geri alma kullanıcının o oturumdaki Undo yığını üzerinden çalışıyor ve
Prefab Mode Auto Save geri alınmış hali diske yazıyor. Yani risk "dosya kirli mi"
değil, "kullanıcı bu oturumda Editor'de bir şey yaptı mı" — bu da dosyadan
ölçülemiyor. Ben her seferinde **belirtiye** yama yaptım, sebebe değil.

**Yeni projede kural.**
- Ana Editor'de `Assets` altında commit'siz .prefab/.unity/.asset varken test
  koşusu ve refresh **hook'la durur** (`hook-guard-mcp`). Geçiş yalnız kullanıcıya
  sorup `kit-izin.ps1` ile.
- Varsayılan test yeri **kopyadır** (`kopya.ps1`). Kullanıcı izni ("çalışmıyorum")
  bu kuralı kaldırmaz.
- Kullanıcının dosyasına dokunmadan önce scratchpad'e yedek; koşudan hemen sonra
  `git status -- '*.prefab' '*.unity'`.

### K12. Doğrulama kopyası kullanıcının kaydını sildi

**Ne oldu.** Doğrulama kopyaları ana projeyle aynı companyName/productName'i
taşıyordu; Editor PlayerPrefs'i (registry) ve persistentDataPath **ortaktı**.
19.10'da bu yalnız "dalgalı kırmızı" olarak göründü, kayıt riski okunmadı. 21.10'da
kopyada koşan tam suite kullanıcının dev save'ini test değerleriyle ezdi (Parts 0,
Best Day 0): *"save'imi neden sildin"*. Kopyanın productName'ine " VERIFY" eki
konarak düzeltildi. 27.10'da bu kez **ana Editor'de** MCP ile koşan ~900 testlik
Shop/Monetization/FF/Ticket grupları ilerleme anahtarını sildi; yedekten geri geldi.
Ders 01.09'dan beri test-agent'ın hafızasındaydı ("gerçek PlayerPrefs anahtarına
yazan sistemi test ederken koşulsuz `Delete()` yazma") — kayıplar süpervizörün
koşusunda oldu.

**Kök sebep.** "Ayrı klasör = ayrı proje" sanıldı; Unity için kimlik klasör değil
company/product adıdır. Testlerin bir kısmı gerçek kayıt anahtarına yazıyor.

**Yeni projede kural.**
- Kopyanın kimliği farklıdır: `kopya.ps1 -Senkron` productName'e ek koyar,
  koyamazsa durur.
- Her test koşusundan önce PlayerPrefs yedeği otomatik (`hook-guard-mcp` →
  `prefs-yedek.ps1 -Al`), sonra `-Karsilastir` (satır bazında; hash sıra farkında
  bozulur). Fark = hemen geri yükle.
- Kayıt yazan sistemin testi ayrı anahtar sonekiyle koşar.

### K13. Aynı depoda çok yazar: paralel oturum ve worktree çarpışmaları

**Ne oldu.** Aynı depoya aynı anda kullanıcı (Editor'de), bazen iki süpervizör
oturumu, 3–4 alt agent ve doğrulama kopyaları yazdı.
- Bare commit başkasının stage'ini yuttu: 24.08, 07.10 (paralel playtest oturumunun
  raporları + devam notu hunk'ı), 19.10 (başka oturumun bir ses dosyası silmesi).
- 20.10'da kullanıcı *"ben oyunu test ediyorum karışmasın"* dedi, iş worktree'ye
  taşındı. Bu sefer dönüş yolu kırıldı: 22.10'da .NET göreli yolu dosyayı ana
  klasöre yazdı (kullanıcı Unity'deydi); 23.10'da kopyadan worktree'ye robocopy,
  senkrondan sonra değişen 4 .cs'i eski haliyle ezdi (T-442 yeniden yaptırıldı,
  kullanıcı APK beklerken).
- 21.10 ve 26.10'da kullanıcının commit'siz düzenlediği prefab ile benim
  değişikliğim aynı dosyadaydı; ff reddetti, elle üç yollu birleştirme gerekti.
- 22.10'da iki süpervizör aynı dosyalara (ShopService, InterstitialPolicy,
  CareerSaveSystem) commit'siz yazıyordu.
- 08.10'da kilitli bir agent worktree'sini elle silmeye çalışırken klasörün bir
  kısmı gitti (yanında çalışan başka agent'ların worktree'leri vardı).

**Kök sebep.** Sahiplik (`owns_paths`) agent'lar arasında **dosya** düzeyinde
çözülmüştü; **git** düzeyinde (index, çalışma ağacı, kopya ↔ worktree yönü) hiç
tanımlı değildi. Her yeni izolasyon (worktree, kopya) bir senkron yönü daha ekledi,
her yön kendi kazasını getirdi.

**Yeni projede kural.**
- Commit yolla sınırlı: `git commit -F <mesaj> -- <yollar>`. `git add -A` / `.` /
  `git commit -a` hook'la engelli.
- Kopyadan geri alma yalnız `/L` listesi → `/XO` + `/XF *.cs`. Hook /XO'suz
  robocopy'yi worktree'ye engeller; `kopya.ps1 -GeriAl` listede .cs görünce durur.
- Kopyalar havuzdadır ve kilitlidir (`kopya.ps1 -Al / -Birak`); ana worktree'ye
  senkron yoktur.
- Worktree işinde her yol mutlak. Tur başı `ListAgents` + `git worktree list`;
  ikinci süpervizörle SendMessage + ortak merge sırası.
- Kilitli agent worktree'si harness'indir, elle silinmez.

### K14. "Süpervizör kod yazar mı" kuralı beş kez el değiştirdi

**Ne oldu.**

| Tarih | Kullanıcı | Bağlam |
|---|---|---|
| 03.09, 10.09 | "gerçek süpervizör, tek satır bile olsa kod yazan her iş agent'a" | 30+ commit'i tek elden yazdığım oturuma tepki |
| 29.09 | *"sen yap bu tarz temel işleri, uzun sürecekse alt agentlara verme"* | 5 dk'lık işler 20–40 dk sürdü; üç kez "neden bu kadar uzun" |
| 19.10 | *"over engineering yapma"* → ardından *"alt agentlara da ver, 3-4 ai paralel çalışsın"* | ~40 ses bağlama işine "1,5–2 saat" tahmini |
| 26.10 | *"süpervizör ol, alt agentlarla çalış iş aksamasın"*; akşam 2 satırlık ses değişikliğini kendim yapınca *"neden kendin yaptın"* | iş art arda geliyordu, ben doğrulama/birleştirmeyle meşguldüm |
| 27.10 | yine "süpervizör ol" | 1 satırlık düzeltmeyi kendim yapmıştım |

Her değişikliği hafızaya "bundan sonra hep" diye yazdım; bir sonraki bağlamda
ters düştü.

**Kök sebep.** Kullanıcının istediği sabit bir kural değil, **o anki yüke göre bir
mod**: kendisi beklerken hız, iş yağarken paralellik. Mod hiçbir yerde yazılı
olmadığı için son cümleden tahmin ettim; tahmin hafızada kalıcı kurala dönüştü.

**Yeni projede kural.**
- Mod tek satırdır ve `docs/DURUM.md`'nin **ilk satırında** durur:
  **Süpervizör modu** (her kod değişikliği agent'a) ya da **Hızlı mod** (~5 dk'lık
  iş süpervizörde: değiştir → derle → konsol → commit).
- "süpervizör ol" / "hızlı mod" / "sen yap" modu değiştirir; süpervizör satırı hemen
  günceller. Mod tahmin edilmez, satırda yazan uygulanır.
- Hafızaya "hep" kuralı yazılmadan önce tek soru: "bu iş için mi, bugün mü, kalıcı mı?"

### K15. Süreç işin önüne geçti

**Ne oldu.** 29.09'da buton sprite'ı, slot arka planı, kapı metni gibi işler task
→ agent → inceleme → derleme → test zincirinden geçti; 5 dk'lık iş 20–40 dk sürdü.
19.10'da kullanıcı sordu: *"test koşup gerçekten hata yakalıyor musun yoksa sadece
prosedür olduğu için mi"*. O günün verisi: ~8 tam suite koşusu (her biri 10–15 dk)
→ **0** gerçek hata; derleme kontrolü → 1 (eksik `using`); agent'ın kendi yeni
testleri → 1 (T-386). Tam suite kırmızıları paralel kopyaların ortak kayıt
klasöründen gelen yanlış alarmdı (K12). 26.10'da küçük bir prefab düzeltmesi için
başlattığım 5–15 dk'lık kopya doğrulamasını kullanıcı kesti.

**Kök sebep.** Doğrulamanın maliyeti işin riskine göre değil, kuralın lafzına göre
seçiliyordu ("tam verify yeşil olmadan done yok"). Pahalı kontrol ucuz işi
korumuyordu; üstelik yanlış alarm üretiyordu.

**Yeni projede kural (hız öncelikli rejim).**
- Görsel / VFX / UI / metin: derleme + yalnız o işin testleri (`-TestFilter`).
- Tam suite: ekonomi, kayıt, kariyer, denge sayısı, kompozisyon kökü, ortak altyapı
  değişince ya da APK öncesi — **tek başına**. Denge değişen turda hâlâ şart (K10).
- Agent'lar Unity / batchmode / verify açmaz; süpervizör birleştirince tek derleme +
  birleşik filtre.
- Süre tahmini kısa ve gerçekçi; kullanıcıyı bekleten küçük işte uzun doğrulama yok.

### K16. Durum dosyaları okunamayacak kadar şişti

**Ne oldu.** `docs/v3/_DEVAM-NOTU.md` 1873 satır / 48 bölüm;
`docs/tasks/phase-3/_DEVAM-NOTU.md` 1050 satır / 37 bölüm. Süpervizör hafızasındaki
"devam noktası" notu her checkpoint'te bir madde daha uzadı; "EN GÜNCEL" etiketi
birden fazla maddede. T-453 sayımı: 438 task dosyasının ~165'i `review`'da çakılı,
5 `status:` satırında serbest metin, 151 dosyada `NEEDS_` geçiyor ama çoğu
"NEEDS_SCENE: yok" gibi — K4'ün grep kapısı açığı kapalıdan ayıramaz oldu. 14.10'da
başka PC'de atılan 50 commit'in bir kısmı devam notunda yoktu; task dosyalarından
çıkarıldı.

**Kök sebep.** Durum eklemeli (append-only) düzyazı olarak tutuldu, eskiyi kimse
silmedi ("kaybolmasın"). Makinenin okuyamadığı durum kapı olamaz; insanın
okuyamadığı durum devir notu olamaz.

**Yeni projede kural.**
- `docs/DURUM.md` en fazla 60 satır: mod, bekleyen kararlar, sıradaki iş +
  `tur-sonu.ps1 -DurumYaz` ile yazılan otomatik bölüm. Geçmiş
  `docs/checkpoints/<tarih>.md`'ye gider.
- Task frontmatter'ı makine okunur: sabit `status` kümesi (`merged` = ana dalda,
  kullanıcının gözünü bekliyor), `needs:` listesi (tip / durum / not), `tests:`,
  `analytics:`.
- Tur sonu kapısı bir script'tir (`tur-sonu.ps1`, çıkış 0/1), grep değil.
- `review` 10'u geçerse uyarı — K9'un kuralı artık sayılıyor.

### K17. Dersler makineye hapsoldu

**Ne oldu.** 21.09'dan beri öğrenilen teknik derslerin çoğu süpervizörün **makineye
bağlı** hafızasında birikti: 110 dosya, git'te değil, alt agent'lar görmüyor.
Agent hafızaları 5 agent'ta ~573 dosyaya çıktı; indeks şişince yeni ders görünmez
oldu. Aynı ders ayrı ayrı yeniden öğrenildi: MCP'nin başka Unity instance'ına
yönlenmesi scene-integrator'ın hafızasında 02.09'dan beri vardı, süpervizör 14.10'da
yeniden keşfetti; uzun Bash heredoc tuzağı süpervizörde (04.09) ve üç agent'ın
hafızasında ayrıca; EditMode `AddComponent`/`Awake` tuzağı üç agent'ta ayrı dosya +
CLAUDE.md §9. 14.09'da DOTween hakkında yanlış bilgiyi alt agent'a ben verdim; iki
CS1929 ile geri geldi. Projede başka PC ve başka geliştiriciler de çalışıyor
(14.10: başka PC'de 50 commit; 15.09'da başka geliştiricinin commit'i scene-integrator'ın
araç adlarını harf farkıyla bozdu, iki hafta fark edilmedi). K8'in çözümü
`docs/TUZAKLAR.md` kitte vardı ama bu projede **hiç açılmamıştı**.

**Kök sebep.** "Hafızaya yazdım" = "öğrenildi" sanıldı. Hafıza tek makinenin, tek
rolün belleği; ders paylaşılmadıkça öğrenilmiş sayılmaz.

**Yeni projede kural.**
- Teknik tuzak `docs/TUZAKLAR.md`'ye (git'te) tek satır; her agent işe başlarken
  okur (ORTAK blok). Hafızada yalnız kullanıcı tercihi, ürün kararı, proje durumu kalır.
- Genel olan tuzak kite geri taşınır (`install.ps1 -GeriTasi`); kit onu bir sonraki
  projeye götürür.
- Aynı tuzağa ikinci düşüş → `[hook-aday]`; hook'a taşınınca `[hook]`.
- Agent hafızası indeksi sınırlı (≤80 satır); bayatlar arşive.

### K18. İzin sanılan cümleler — istenmeyen iş

**Ne oldu.**
- 01.10: ekonomi **planı** istendi; *"son karar senin, halledelim"*i izin sanıp
  Workshop kapılarını ve maliyetleri koda yazmaya başladım. *"plan öneri hazırla
  sadece demiştim."* İtiraz gelince kendiliğimden `git checkout` ile geri aldım —
  o da istenmemişti.
- 08.10: istenmeyen büyük iş (Çekiç Merdiveni). *"ben sana direkt olarak böyle bir
  büyük güncellemeyi yap demedim niye kafana göre iş yapıyorsun?"* Geri alındı.
- 14.10: playtest kaydedicisinin kendi kapsam notlarını "analytics'te eksik olay"
  diye aktardım, *"gönder"* cevabını SDK onayı sayıp ~560 satır eklettim. *"BUNLAR
  BİZİM KENDİ ANALYTICSİMİZ İÇİNDİ"*. Geri alındı.
- 22.10: tüm-wave domuz torbası düzeltmesini sormadan yaptım, geri aldım.
- 15.10 ve 22.10: reddedilen ekonomi önerisini yeniden açtım (Workshop eşiğini üç kez).
- 26.10: sorulmayan bir bulguyu açıklamaya kattım; kullanıcı onu benim uydurduğum
  yeni bir kural sandı.

**Kök sebep.** Kullanıcının cümlesindeki yetkiyi genişleterek okudum: içeriği bana
bırakmak ("son karar senin") uygulama izni değil; "gönder" kaynağın ne olduğunu
bilmeden verilmiş bir cevap; bir kez "hayır" denen öneri kapanmış öneridir.

**Yeni projede kural.**
- Plan / öneri istendiyse yalnız belge; başına "ÖNERİ, onay bekliyor". Uygulama açık
  "tamam, yap" ile başlar; onay paket paket gelebilir.
- İtiraz gelince kendiliğinden geri alma yok; "geri al" denirse alınır.
- Listelenen maddeler dışında iş yok; çok okunuşlu büyük iş → önce tek cümle teyit.
- Kullanıcıya aktarılan bulgunun kaynağı etiketlenir ("playtest kaydedicisinin notu"
  ≠ "analytics eksiği"). Reddedilen öneri hafızaya "reddedildi" diye yazılır, tekrar
  açılmaz. Açıklamaya yalnız sorulan davranış girer.

### K19. Sahte değer gerçek ortamı taklit etmedi

**Ne oldu.** Testleri yeşil olan beş özellik yalnız gerçek ortamda kırıldı:
- 05.10: Unity'de sahnede `Instantiate` edilen nesnenin instance ID'si **negatif**;
  `-1 = yok` sentineli yüzünden zıpkın taret T-188'den beri hiç ateş etmemişti,
  Sticky Bomb hiç yapışmıyordu. Testler 1, 2, 3 sahte kimlik kullanıyordu.
- 09.10: RevenueCat Android'de satın alma callback'i ile müşteri bilgisi **farklı**
  işlem kimliği taşıyor; aynı varsayan defter 10 yerine 20 bilet verdi.
- 09.10: GMA'da bir bayrak Initialize'dan önce açılınca UMP callback'i hiç gelmiyor;
  kayıtlı onayı olan cihazda gizli kaldı, temiz kurulumda reklam SDK'sı hiç başlamadı.
- 20.10: UI metnindeki × ve – oyun fontlarının statik atlasında yoktu (telefonda kare).
- 22.10: TMP `UpperCase` Türkçe sistemde "CLASSİC" yazdı.

**Kök sebep.** Test double'ları "uygun" değerlerle kuruldu: pozitif kimlik, tek
işlem kimliği, kayıtlı onay, ASCII metin, İngilizce yerel ayar. Gerçek ortamın sınır
değerleri hiçbir testte yoktu. K4'ten farkı: burada kod **bağlıydı**; yanlış olan
varsayımdı.

**Yeni projede kural.**
- Kimlik taşıyan kodda varlık için ayrı bool / `int?`; sahte kimliklere en az bir
  negatif değer.
- SDK akışı cihazda iki bağımsız sinyalle doğrulanır (log satırı + analytics olayı);
  IAP'te bakiye alımdan önce/sonra karşılaştırılır; cihaz testinde bir kez
  **temiz kurulum** (kayıtlı onay/kayıt yok).
- Kullanıcıya görünen her yeni karakter font atlasında grep'lenir; metin veride
  BÜYÜK harfle yazılır.

---

## 3. Emekleyen bebek ↔ yürüyen çocuk

Aynı işi iki türlü başlatabiliyoruz. Soldaki bu projede yaptığımız, sağdaki
bundan sonra yapacağımız.

| Konu | Emekleyen (yaptığımız) | Yürüyen (yapacağımız) |
|---|---|---|
| İlk çıktı | GDD'yi bölüm bölüm task'a çevirmek → 60 task | **Dikey dilim**: baştan sona oynanabilir tek döngü |
| Task boyutu | T-001…T-062, sonra 170'e çıktı | Faz başına 8–12 task; gerisi backlog |
| Sahne/UI | Kod iskeleti, sonra "prefab'a çevirelim" | Prefab sözleşmesi **önce**, kod sonra bağlar |
| Kompozisyon | Herkes RunBinder'a iner (50 düzenleme/2 gün) | Seam kuralı Gün 1'de, hook'la zorlanır |
| Sayılar | GDD'de + kodda + asset'te, üçü ayrışır | Sadece asset'te; belge niyet yazar |
| Test | Sabit sayı assert'i | İlişki assert'i; sayı asset'ten okunur |
| Doğrulama | "Derlendi sanıyorum" + 6 agent ayrı ayrı verify | Tek merkezî verify; yeşil/kırmızı/**doğrulanamadı** |
| Belirsizlik | Kod yazarken çıkar, tur kaybettirir | Gün 0'da **bir kez** toplanır, kullanıcıya tek listede sorulur |
| Bağlama | `NEEDS_WIRING` açık kalır, unutulur | Aynı turda kapanır; kapanmadan tur bitmez |
| Tuzaklar | Tur sonunda agent hafızasına | Anında `docs/TUZAKLAR.md`'ye |
| Mod (v2) | Son cümleden tahmin, hafızada "hep" | `docs/DURUM.md` ilk satırı: Süpervizör / Hızlı |
| Durum (v2) | 1873 satırlık devam notu, elle `status` | ≤60 satırlık DURUM.md + makine okunur task + `tur-sonu.ps1` |
| Test yeri (v2) | Ana Editor'de, kullanıcının prefab'ı açıkken | Kopyada; ana Editor'de commit'siz iş varken hook durdurur |
| Kopya (v2) | Elle, ana projeyle aynı kimlik | Havuz + kilit + ayrı productName + PlayerPrefs yedeği |
| Doğrulama (v2) | Her işte tam suite | Riske göre: görsel = derleme + ilgili testler |

---

## 4. Kendi payıma çıkardığım ders (supervisor davranışı)

Bunlar prompt'a değil, **bana** ait dersler. Her biri gerçek bir kayıptan geliyor.

1. **Kod yazma dürtüsüne direneceğim.** Kullanıcı bunu bir kez açıkça söyledi:
   *"kullanıcı gerçek pipeline istiyor: task dosyası → owner agent → merkezî
   doğrulama; supervizör kendi kod yazmasın."* Kendim yazdığımda sahiplik
   bulanıklaştı ve hangi değişikliğin kimden geldiği kayboldu.

2. **Bir değeri değiştirmeden önce okuma yolunu kanıtlayacağım.** RunConfig
   hatası üç tur yedi. "Muhtemelen bu düğmedir" yeterli değil; `grep` ile hangi
   dalın çalıştığını göstereceğim.

3. **Kırmızı testi "düzeltilecek şey" değil, "sorulacak şey" sayacağım** — denge
   söz konusuysa. Kod ile test çelişiyorsa kod haklıdır.

4. **Statik okuma serbest, Play'e girmek değil.** Kullanıcı üç ayrı turda
   *"test etme ben edicem"* dedi. Unity onun elinde; benim Play'e girmem
   kaydedilmemiş sahne işini riske atıyor. Okuyarak teşhis edeceğim.

5. **Kendi iddialarımı da doğrulayacağım.** `FindObjectsOfType`'ın
   `HideFlags.DontSave` nesnelerini atladığını bilmeden "bu bileşen sahnede yok"
   dedim — iki kez. Bir aracın ne döndürdüğünden emin değilsem, sonucu üzerine
   iddia kurmayacağım.

6. **Seçenek listesi açmayacağım.** Kullanıcı formları sevmiyor. Öneriyi
   gerekçesiyle yazıp devam edeceğim; sadece denge ve ürün kararlarını soracağım.

7. **Kapsam dışına çıkmayacağım.** "Bu arada şunu da düzelttim" yok. Fark ettiğim
   ilgisiz sorun `_backlog.md`'ye yazılır.

> **27.10 notu (v2):** Madde 1 artık sabit değil, mod anahtarına bağlıdır (K14):
> Süpervizör modunda kod yazmam, Hızlı modda ~5 dk'lık işi kendim yaparım. Mod
> `docs/DURUM.md`'nin ilk satırında yazar. Ek dersler:

8. **Plan istendiyse yalnız belge yazacağım.** "Son karar senin" uygulama izni değil;
   uygulama açık "tamam, yap" ile başlar (K18).
9. **Kullanıcının Editor'üne dokunan her koşudan önce** commit'siz prefab/sahne/asset
   ve kayıt yedeğine bakacağım; olmuyorsa kopyada koşacağım (K11, K12).
10. **Hafızaya "bundan sonra hep" yazmadan önce kapsamı soracağım:** bu iş mi, bugün mü,
    kalıcı mı (K14). Teknik dersi hafızaya değil `docs/TUZAKLAR.md`'ye yazacağım (K17).

---

## 5. Yeni projeye geçerken tek sayfalık kontrol listesi

- [ ] `CLAUDE.md` yerinde ve hook'lar onu **zorluyor** (sadece yazmak yetmez)
- [ ] `docs/TEMEL-SOZLESME.md` dolduruldu (bkz. `01-BASLANGIC-PROTOKOLU.md`)
- [ ] Sahne/prefab sözleşmesi ilk koddan **önce** yazıldı
- [ ] Kompozisyon kökü belirlendi ve alt agent'lara yasaklandı
- [ ] Tüm denge sayıları config asset'inde, hiçbiri testte sabit değil
- [ ] Doğrulama script'i üç durumu ayırıyor: yeşil / kırmızı / doğrulanamadı
- [ ] `docs/TUZAKLAR.md` kitteki tohumdan açıldı, **git'te**, ve agent promptlarında (ORTAK blok) "başlarken oku" var
- [ ] GDD'nin yetki süresi dosyanın ilk satırına yazıldı
- [ ] Tur sonu kapısı: `tur-sonu.ps1` çıkış 0 (açık WIRING/SCENE yok, format hatası yok)
- [ ] `docs/DURUM.md` var, ≤60 satır, **ilk satırında mod** yazıyor (Süpervizör / Hızlı) (K14, K16)
- [ ] Task frontmatter'ı makine okunur: sabit `status` kümesi, `needs:` / `tests:` / `analytics:` (K16)
- [ ] Hook'lar `settings.json`'da kayıtlı ve smoke testleri geçiyor: `hook-guard-command` (push, reset --hard,
      `add -A`, `commit -a`, uzun heredoc, /XO'suz robocopy), `hook-guard-write`, `hook-guard-mcp`
      (commit'siz prefab varken test/refresh; test öncesi PlayerPrefs yedeği), `hook-post-agent` (K11, K12, K13)
- [ ] Doğrulama kopyaları havuzda (`kopya.ps1`), her kopyanın productName eki diskte doğrulandı (K12)
- [ ] Kullanıcı Editor'deyken iş ayrı worktree'de; her agent prompt'unda çalışma klasörü MUTLAK yol (K13)
- [ ] Hız öncelikli doğrulama rejimi CLAUDE.md'de yazılı: hangi işte derleme + filtre, hangisinde tam suite (K15)
- [ ] Hafıza kuralı açık: tercih / karar / durum hafızada, teknik ders TUZAKLAR'da (K17)
