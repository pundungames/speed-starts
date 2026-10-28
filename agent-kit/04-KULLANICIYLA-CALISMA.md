# KULLANICIYLA AYNI PROJEDE ÇALIŞMAK

> Kullanıcı birebir (21.09.2026): *"ben projede bir şeyler yaparken sende
> girince benim save gidiyordu bunu düzelttin mesela."*
>
> Kullanıcı birebir (20.10.2026): *"kanka sen bunları farklı worktree de yapsana
> ben oyunu test etmek istiyorum karışmasın. sonraki işleri farklı worktree de yap
> beni engellememiş olursun hem."*

Bu kısa ama en pahalı derslerden biri: **editör kullanıcının elindedir.**
Agent, kullanıcının açık projesine girer — kendi kopyasına değil.

**v2 eki (27.10.2026):** 21.09'dan sonra kayıpların çoğu yine burada oldu: test
koşusu kullanıcının prefab işini beş kez geri aldı, doğrulama kopyası ve ana Editor'deki
test koşusu kullanıcının kaydını iki kez sildi (bkz. `00-DERSLER.md` K11, K12, K13).
Aşağıdaki §1–§6 korunmuş, güncellenmiştir; §7–§10 yenidir. Teknik ayrıntı ve komutlar
`docs/TUZAKLAR.md`'de.

---

## 1. Kullanıcının kaydedilmemiş işini silme

**Ne oluyordu.** Kullanıcı bir prefab'ı düzenliyor, henüz kaydetmemiş. Ben kodu
derlemek için `refresh_unity` çağırıyorum → domain reload → **kullanıcının
kaydedilmemiş prefab/sahne düzenlemesi yok oluyor.** Kaybı ben yapıyorum, kullanıcı
fark ettiğinde iş çoktan gitmiş.

**Auto Refresh durumu sabit değil.** 16.09'da **kapalı** ölçüldü (derlemeyi yalnız
benim refresh'im tetikliyordu), 02.10'da **açık** ölçüldü (kullanıcı Unity'ye
odaklanınca agent'ın yarım yazdığı .cs'ler de derleniyor). Varsayma; her seferinde
`EditorUtility.IsAutoRefreshEnabled()` ile ölç. Açıksa ve agent'lar ana klasöre yazıyorsa
kullanıcı Play'e girmek istediğinde Auto Refresh'i kapatmasını öner — ya da daha iyisi
işi worktree'ye taşı (§7).

**Kural — derleme/refresh tetiklemeden ÖNCE üç kontrol:**

| Kontrol | Sonuç |
|---|---|
| Play modunda mı? | Evet ise **dokunma** (izin varsa önce kirli mi bak, sonra Play'i durdur — §2) |
| Prefab stage açık mı? | Evet ise **dokunma**, kullanıcı prefab düzenliyor |
| Aktif sahne `isDirty` mi? | Evet ise **dokunma**, kaydedilmemiş iş var |

Üçünden biri doluysa: derleme yapma, kullanıcıya söyle (*"Unity'de kaydedilmemiş
değişikliğin var, Ctrl+S yap, sonra derleyeyim"*), o kaydettikten sonra devam et.
**Bu kontrol bir "test" değildir** — kullanıcının işini korumaktır ve her zaman yapılır.

- Uzun kod turlarında derlemeyi **turun sonunda tek seferde** yap, her dosyadan sonra değil.
- `hook-guard-mcp` ana Editor'de commit'siz .prefab/.unity/.asset varken refresh'i durdurur.
  Ama hook yalnız **diske yazılmış** commit'siz dosyayı görür; bellekteki kaydedilmemiş işi
  göremez — tablodaki üç kontrol hâlâ elle.
- Prefab Mode **Auto Save** kaydedilmemiş işi refresh'ten korur; ama test koşusunun geri
  almasını da diske yazar (§8). İkisini karıştırma.
- Kullanıcı "yaptığım değişiklik kayboldu" derse önce kendi refresh / test çağrılarına bak;
  suçu Unity'ye atma.

---

## 2. Play moduna girme

Kullanıcı üç ayrı turda söyledi: *"test etme ben edicem"*, *"doğrulama yapma"*,
*"test yapma"*, *"sadece kodsal olarak düzelt. doğrudur zaten."*

**Neden haklı:** Unity onun ekranında açık. Benim Play'e girmem projeyi ondan
alır, kaydedilmemiş işi riske atar ve bir dalgayı beklemek dakikalar götürür.
O aynı doğrulamayı saniyeler içinde kendisi yapıyor.

**Uygulama:**
- `EditorApplication.isPlaying = true` **yapma**. Ana Editor'de test suite koşturma
  (koşman gerekiyorsa §8'in şartlarıyla, tercihen kopyada).
- Yaptığın: kodu/asset'i değiştir → **derle** → konsolu hataya karşı oku →
  commit'le. Commit mesajında "doğrulanmadı" diye yaz.
- Kullanıcı bir hata bildirdiğinde kök sebebi **statik** bul: kod okuma,
  `git show`, sahne/prefab dosyasını okuma. **Okumak serbest, Play açmak değil.**
- Play'e girmeden davranış ölçmen gerekiyorsa **önizleme sahnesi** kullan
  (`EditorSceneManager.NewPreviewScene`, bkz. TUZAKLAR); açık sahne/prefab kirlenmez.
- İstisna: kullanıcı açıkça "bak/ölç/dene" derse, ya da statik okumayla
  ayırt edilemeyen bir şey varsa **önce izin iste.**

**Play'den ÇIKMAK serbest olabilir (01.10.2026 izni):** *"sen test yapacaksan
playmode'u kapatabilirsin, çekinme, bana sormadan kapat"*. Derleme ya da ölçüm için
Play açık kalmışsa durdurabilirsin — ama önce prefab/sahne kirli mi bak. 19.10'da
Play'i kontrol etmeden refresh çağırdım: oyun çalışırken domain reload sahte bir
ArgumentNullException üretti. Her refresh'ten önce `is_playing` oku. Bu izin Play'e
**girmek** için değildir.

**Cihaz ayrıdır:** kullanıcı telefonu adb ile kullanma izni verdiyse (logcat, ekran
görüntüsü, dokunma) bu Editor kuralını değiştirmez. Gerçek parayla ödeme ekranında dur.

---

## 3. Paralel oturum farkındalığı

Aynı checkout'ta başka bir oturum çalışıyor olabilir — kullanıcının telefondan açtığı
bir oturum, ikinci bir süpervizör ya da senin agent'ların.

- Tur başında `ListAgents` çağır, `git worktree list` bak.
- `git status` kirliyse **kimin olduğunu anla.** Çalışma ağacındaki yabancı
  değişiklikler ve konsoldaki CS hataları başka bir oturumun olabilir.
- Başkasının değişikliğine **dokunma**; kendi hunk'larını ayrı stage'le.
- Bare `git commit` yasak — başkasının stage'ini yutar. Her commit yolla sınırlı:
  `git commit -F <mesaj> -- <yollar>` (`git add -A` / `git commit -a` hook'la engelli).
- Edit "dosya okunduktan sonra değişti" diye reddedilirse o dosyayı başkası yazıyor:
  `git add`'leme, önce kimin yazdığına bak.
- İkinci bir süpervizör varsa SendMessage ile hangi dosyayı kimin aldığını yazışın;
  ikiniz aynı anda merge etmeyin.

---

## 4. Asset'e yazdıysan hemen doğrula ve commit'le

Editörde açık bir proje, bellekteki kopyayı diskin üstüne yazabilir.
Bu projede `SerializedObject` ile yamalanan bir `.asset` **dakikalar içinde
sessizce eski hâline döndü** (aynı gün dört kez).

Sıra: **yaz → `File.ReadAllText` ile diskten doğrula → saniyeler içinde
commit'le → 20 sn sonra bir kez daha doğrula.**

`AssetDatabase.SaveAssets()` bütün kirli nesneleri yazar; her kayıttan sonra `git diff`
ile yalnız beklediğin satırların değiştiğine bak.

---

## 5. Kullanıcının biçimi

- **Seçenek listesi / form açma.** Öneriyi gerekçesiyle yaz ve devam et.
  Sadece **denge ve ürün** kararlarını sor.
- Kullanıcı bir kararı tekrarladıysa o karardır — tartışma, uygula.
- Rapor kısa olsun; ne yapıldığı, neyin doğrulanmadığı, neyin açık kaldığı.
- **Plan / öneri istendiyse yalnız belge yaz.** "Son karar senin" içeriği bırakır,
  uygulama izni vermez; uygulama açık "tamam, yap" ile başlar (K18).
- Açıklamaya **yalnız sorulan davranışı** koy; elerken baktığın başka şeyi yazman
  gerekiyorsa "bu zaten vardı, dokunmadım" diye ayır.

---

## 6. Kontrol listesi — her editör dokunuşundan önce

- [ ] Play modunda değil (ya da izinliyse: kirli değil, durdurdum)
- [ ] Prefab stage kapalı
- [ ] Sahne `isDirty` değil
- [ ] Çalışma ağacındaki yabancı değişiklik tespit edildi, dokunulmayacak
- [ ] Asset yazdıysam: diskten doğruladım ve commit'ledim
- [ ] Kullanıcı Editor'de test ediyorsa iş worktree'de, ana klasöre hiçbir şey yazılmıyor (§7)
- [ ] Test koşacaksam: ana Editor'de `git status --short -- Assets` temiz ya da koşu kopyada (§8)
- [ ] Test koşusundan önce PlayerPrefs yedeği alındı, sonra karşılaştırıldı (§9)
- [ ] Kullanıcının commit'siz dosyasına dokunacaksam önce yedeği aldım (§10)

---

## 7. Kullanıcı test ederken iş ayrı worktree'de

**Neden:** Ana klasöre yazılan her .cs Unity'nin otomatik refresh'iyle derlemeyi
tetikler; kullanıcı Play'de test ederken domain reload ya da yarım dosya derleme hatası
testini bozar.

- Yeni kod/asset işi ana klasöre değil bir **git worktree'ye** yazılır. Agent prompt'una
  çalışma klasörü **mutlak yolla** yazılır ve "ana klasöre yazma" denir.
- Göreli yol çözen araçlarda (.NET `[IO.File]`, python, robocopy) **her zaman mutlak yol**:
  22.10'da `Set-Location` sonrası `[IO.File]::WriteAllText('Assets\...')` dosyayı ana
  klasöre yazdı, kullanıcı o sırada Unity'deydi. Ana klasörde beklenmedik `M` görürsen önce
  kendi komutundan şüphelen.
- Doğrulama kopyada yapılır (önce worktree'de commit; head-only senkron commit'siz
  dosyayı HEAD'e döndürür). Ana Editor'e dokunmaz.
- **Birleştirme:** kopyada yeşil → `git merge --ff-only` ile ana dala → ana Editor'de Play
  kapalı mı, prefab stage / sahne kirli mi bak → refresh → konsol → kullanıcıya "geçirdim".
  Doğrulanmamış iş ana klasöre girmez.
- Başka oturumlar aynı worktree'yi kullanıyor olabilir; kendi işine kendi worktree'sini
  aç, doğrulama hedefi de ayrı kopya olsun.
- **Tersi:** kullanıcı *"bu dalda yapabilirsin, çalışmıyorum şu anda"* derse küçük
  düzeltmeyi ana klasörde doğrudan yap, uzun kopya doğrulamasıyla bekletme. Ama §8 ve §10
  bu izinle **kalkmaz** (26.10'da tam bu izinle test koşusu kullanıcının commit'siz
  prefab ayarlarını geri aldı).

---

## 8. Test koşusu kullanıcının işini geri alır

**Ne oluyordu.** EditMode koşusu bitince Unity "Undo after editor test run" yapıyor;
geri alma kullanıcının oturumdaki Undo yığını üzerinden çalışıyor ve Prefab Mode Auto
Save geri alınmış hali **diske** yazıyor. Beş kez oldu (29.09, 05.10, 07.10, 09.10,
26.10). `isDirty=false`, "commit'li", "prefab stage kapalı" — üçü de **korumadı**;
07.10'daki yalnız 12 testlik filtreli bir koşuydu.

**Kural:**
- Ana Editor'de test koşmadan önce: `git status --short -- Assets` içinde commit'siz
  .prefab / .unity / .asset var mı? Varsa ya sahibine sorup commit'let ya da testleri
  **kopyada** koş. `hook-guard-mcp` bu durumda `run_tests`'i durdurur; bilerek geçmek için
  önce kullanıcıya sor, sonra `kit-izin.ps1 unity-test`.
- Kullanıcı bu oturumda Unity'de prefab/sahne düzenlediyse (özellikle prefab stage açık
  bulduysan) tam suite koşma; derleme + konsolla yetin ya da kopyaya git.
- Koşudan **hemen sonra** `git status -- '*.prefab' '*.unity'`: değişen dosya varsa mtime'ını
  koşu bitişiyle karşılaştır, **commit'leme**, kullanıcıya "test koşusu X'i geri aldı,
  HEAD'den / yedekten geri yükleyelim mi" de.
- Kullanıcının "commit'le" dediği prefab'ı commit'lemeden önce içeriğini HEAD ile kıyasla —
  geri alınmış eski hali commit'lemek onun işini siler.

---

## 9. Kullanıcının kaydı da onun işidir

**Ne oluyordu.** Kullanıcı Editor'de dev save ile test ediyor. 21.10'da doğrulama
kopyasında koşan tam suite bu save'i sildi — kopya ana projeyle **aynı Unity kimliğini**
(company/product) taşıdığı için Editor PlayerPrefs'i ve kayıt klasörü ortaktı. 27.10'da bu
sefer ana Editor'de MCP ile koşan ~900 test ilerleme anahtarını sildi.

**Kural:**
- Kopyanın productName'i ayrı bir ek taşır (`kopya.ps1 -Senkron` ekler); elle kurulan
  kopyada batchmode'dan önce bunu diskte doğrula.
- Her test koşusundan önce PlayerPrefs yedeği (hook `run_tests` öncesi `prefs-yedek.ps1 -Al`
  çağırır), sonra `prefs-yedek.ps1 -Karsilastir` (satır bazında). Fark varsa hemen geri yükle
  ve kullanıcıya söyle.
- Tam suite tek başına koşar; aynı anda iki koşu dalgalı kırmızı ve kayıt riski üretir.

---

## 10. Kullanıcının commit'siz dosyasına dokunmadan önce yedek

Kullanıcı aynı dosyayı commit'siz düzenliyor olabilir (21.10: 98 KB'lık bir prefab,
Prefab Mode'da açık; 26.10: konfeti ayarları). Bu durumda:

- Dokunmadan önce dosyanın kopyasını scratchpad'e al. 26.10'daki kayıp bu yedekle geri geldi.
- Kendi değişikliğini kullanıcının satırlarını **ezmeden** ekle; worktree dalındaki aynı
  değişikliği geri al ki ff çakışmasın.
- ff reddederse üç yollu birleştir (`git merge-file` kullanıcı / HEAD / benim), çatışma 0 ve
  kullanıcının satırları duruyor mu bak, prefab stage kirli **değilse** kapat, birleşik dosyayı
  yaz, `numstat` kullanıcının eski farkıyla aynı mı kontrol et, refresh + konsol. Adımlar
  `docs/TUZAKLAR.md`'de.
- Kullanıcının dosyası için `git checkout --` gibi geri dönüşsüz bir komut çalıştırmadan önce
  yedeğin olduğundan emin ol; otomatik modda bu komut reddedilebilir, geri yüklemeyi
  kullanıcıya bırakmak gerekebilir.
