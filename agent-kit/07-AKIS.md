# AKIŞ — bir iş baştan sona nasıl yürür

> v1 kit "gece kendi kendine dönen tur" düzenini anlatıyordu (`supervisor-loop.ps1`, sabah raporu).
> Gerçekte 12.08'den sonra hiç kullanılmadı. Gerçek düzen: **kullanıcı canlı iş atar, süpervizör dağıtır
> ve doğrular, kullanıcı gözle bakar.** Bu belge o düzeni anlatır. Kurallar `blocks/KURALLAR.md`'dedir
> (her projenin CLAUDE.md'sine bu blok olarak girer); burada nedenleri ve reçeteleri var.

---

## 1. Roller

| Rol | Yapar | Yapmaz |
|---|---|---|
| **Kullanıcı** | Ne istendiğini söyler, ürün/denge kararını verir, oyunda/cihazda gözle bakar, "tamam" der | — |
| **Süpervizör** (ana oturum) | Task dosyası yazar, agent'a dağıtır, diff'i inceler, kopyada doğrular, birleştirir, kompozisyon kökünü bağlar, prefab/sahne builder'ını çalıştırır, DURUM'u tutar | Süpervizör modunda ürün kodu yazmaz; denge sayısını kendiliğinden değiştirmez |
| **Alt agent** | Task dosyasındaki işi `owns_paths` içinde yapar, test yazar, `review` der | Unity açmaz, doğrulama koşmaz, `merged`/`done` demez |
| **scene-integrator** | Tek başına, sahne/prefab/asset'e dokunan iş | Ürün kodu yazmaz |

## 2. Mod — en çok tur yiyen belirsizliğin çözümü

"Süpervizör kod yazar mı" kuralı bu projede beş kez değişti (03.09 "tek satır bile agent'a", 10.09 aynı,
29.09 "temel işleri sen yap", 19.10 "alt agent'lara da ver, 3-4 paralel", 26.10 "neden kendin yaptın").
Her değişiklik doğruydu — o anın ihtiyacına göre. Sorun kural değil, **hangisinin geçerli olduğunun
tahmin edilmesiydi.**

Çözüm: iki adlandırılmış mod ve tek bir satır.

```
docs/DURUM.md ilk satırı:   MOD: süpervizör    |    MOD: hızlı
```

- **süpervizör**: kod değişikliği ne kadar küçük olursa olsun agent'a. Süpervizör "işler aksamasın"
  diye yeni gelen işi, kendisi doğrulama/birleştirme ile meşgulken bile hemen bir agent'a verir.
- **hızlı**: ~5 dakikalık iş süpervizörde: değiştir → derle → konsol → commit. Task dosyası,
  agent, uzun test turu yok. Karar sorusu: *"bu iş agent'la 20 dk'dan uzun sürer mi, ben 5 dk'da yapar
  mıyım?"* Kısa olan yol seçilir.
- Kullanıcı "süpervizör ol" / "hızlı mod" / "sen yap" dediği an satır güncellenir. Satır yoksa
  varsayılan **süpervizör**.

## 3. Bir işin yolculuğu

```
istek ─► task dosyası ─► worktree/dal ─► agent ─► review ─► inceleme ─► kopyada doğrulama
                                                                              │
          done ◄── kullanıcı "tamam" ◄── merged (göz listesi) ◄── ff birleştirme ◄┘
```

1. **Task dosyası** (`docs/tasks/_TEMPLATE.md`): hedef, kabul kriterleri, `owns_paths`, kısıtlar.
   Agent'a özet geçilmez, dosyanın yolu verilir.
2. **Çalışma yeri**: kullanıcı Unity'de çalışıyorsa ayrı git worktree (`<KopyaKoku>\<oyun>-<konu>`,
   dal `work/<konu>-<gg-aa>` ya da `agent/T-XXX-*`). Ana klasöre yazılan her `.cs` Unity'nin
   auto-refresh'ini tetikler ve kullanıcının Play testini bozar.
3. **Agent** işini yapar, `status: review` der, son mesajında değişen dosyaları ve açık needs'i yazar.
4. **İnceleme** (süpervizör): `git diff --name-only` ile `owns_paths` ihlali · test var mı, sabit denge
   sayısı assert ediyor mu · kabul kriterleri tek tek · özellik oyunda ulaşılabilir mi (needs).
   Düzeltme gerekiyorsa **aynı agent'a** geri gönderilir (bağlamı onda).
5. **Doğrulama**: kopya havuzunda (bölüm 5). Sonuç üç durumlu.
6. **Birleştirme**: `git merge --ff-only`; önce ana Editor'de Play kapalı mı, prefab stage temiz mi,
   kullanıcının aynı dosyada commit'siz işi var mı bakılır. Sonra refresh + konsol.
7. `status: merged`, `commits:` doldurulur. DURUM'un göz listesine girer.
8. Kullanıcı bakar, "tamam" der → `done`. Görsel yanı olmayan iş (test düzeltmesi, belge) doğrudan `done`.

## 4. Paralel dağıtım reçetesi (19.10'da kanıtlandı)

Çok parçalı bir iş (ör. ~40 sesin bağlanması, beş VFX'in boyutu):

1. Süpervizör **önce ortak dosyaları** kurar (enum, adaptör, kayıt tablosu) — 5 dakika. Ortak dosyada
   iki agent çakışmasın diye.
2. 3-4 agent, **ayrık `owns_paths`** ile paralel başlar. Aynı checkout'ta mı worktree'de mi?
   - Kullanıcı ana klasörde çalışıyorsa: hepsi tek bir worktree'de, ayrık dosyalarla.
   - Her agent kendi bootstrap/kayıt dosyasını açar; ortak dosyaya ekleme yapmaz.
3. Agent'lara **Unity/batchmode/verify yasak** denir. Her biri ayrı Unity açarsa makine kilitlenir
   ve her worktree Library'yi sıfırdan kurar (dakikalar).
4. Derleme **en sonda bir kez**, birleşik test filtresiyle.
5. Süre tahmini verilir ama kısa tutulur; "1,5-2 saat" gibi tahmin kullanıcıyı haklı olarak kızdırır —
   iş bölünmemiş demektir.

## 5. Doğrulama

**Rejim (19.10, kullanıcı kararı):** bir gün boyunca ~8 tam suite koşusu 0 gerçek hata buldu; derleme
kontrolü 1, agent'ın kendi yeni testleri 1 gerçek sorun yakaladı. Bu yüzden:

| İş | Doğrulama |
|---|---|
| Görsel / VFX / UI / metin | derleme + o işin testleri (`-TestFilter`) |
| Ekonomi, kayıt, kariyer, denge sayısı, kompozisyon kökü, ortak altyapı | tam suite, tek başına |
| APK / AAB öncesi | tam suite, tek başına |

**Kopya havuzu** (`scripts/kopya.ps1`): ana projenin Library kilidi tekildir; batchmode Editor açıkken
aynı projeyi açamaz. Doğrulama depo dışındaki kopyalarda yapılır:

```powershell
$k = powershell -File scripts/kopya.ps1 -Al -Sahip "T-123"    # son satır kopya yolu
powershell -File scripts/kopya.ps1 -Senkron -Ad <kopya> -Kaynak <worktree> -HeadOnly
powershell -File scripts/kopya.ps1 -Dogrula -Ad <kopya> -TestFilter "T123|Crosscut"
powershell -File scripts/kopya.ps1 -Birak -Ad <kopya>
```

Script kopyanın productName'ine " VERIFY" ekler: aksi halde kopya ana projeyle aynı PlayerPrefs ve kayıt
klasörünü kullanır ve test koşusu kullanıcının dev kaydını siler (21.10'da oldu).

**Ana Editor'de test koşmak** (MCP `run_tests`) en riskli işlemdir: koşu sonunda Unity "Undo after editor
test run" yapar ve kullanıcının commit'siz — hatta commit'li ama oturumda düzenlenmiş — prefab'ını geri alır
(dört kez oldu). `hook-guard-mcp` commit'siz prefab/sahne/asset varken koşuyu durdurur ve her koşudan önce
PlayerPrefs yedeği alır. Koşudan sonra `scripts/prefs-yedek.ps1 -Karsilastir`.

**Kopyadan geri alma** (prefab builder çıktısı gibi): `kopya.ps1 -GeriAl` — `/L` listesi, `/XO`, `/XF *.cs`
hazır gelir. Elle robocopy, sync'ten sonra değişen `.cs`'leri eski haliyle ezer (23.10'da oldu).

## 6. Paralel oturumlar

Aynı makinede birden fazla Claude oturumu çalışabilir; kullanıcı da başka bir PC'den aynı dala push edebilir.

- Oturum başında `ListAgents`. Meşgul bir oturum varsa onun dosyalarına dokunulmaz.
- İki süpervizör aynı sisteme dokunacaksa: `SendMessage` ile kim hangi dosyayı aldı yazılır; ikinci
  süpervizör kendi worktree'sini birincinin commit'inin üstüne kurar; birleştirme sırası birlikte belirlenir.
- **Her commit yolla sınırlıdır**: `git commit -F <mesaj> -- <yollar>`. Paylaşılan index'te başkasının
  stage'i olabilir; bare commit onu da alır (üç kez oldu). Hook `git add -A` / `.` / `commit -a`'yı engeller.
- Edit aracı "dosya okunduktan sonra değişti" derse: dosyayı başka biri yazıyor demektir — önce kim olduğuna bak.
- Konsolda yabancı bir derleme hatası başka oturumun yarım işi olabilir; kendi kodunu "düzeltmeye" kalkma.

## 7. Oturum başı, tur sonu, checkpoint

**Oturum başı (2 dakika):** `docs/DURUM.md` → `ListAgents` → `git status` + `git worktree list`.
Önceki rapora güvenilmez: son commit'ler `git show --stat` ile doğrulanır.

**Tur sonu:** `powershell -File scripts/tur-sonu.ps1 -DurumYaz`
- açık WIRING/SCENE · format hataları · review kuyruğu · göz bekleyenler · testsiz task'lar ·
  analytics kapısı · worktree'ler · kopya kilitleri. Çıkış 0 değilse tur kapanmaz.

**Checkpoint** ("checkpoint at", gün sonu):
1. `docs/checkpoints/<yyyy-mm-dd>.md` yeni dosya (şablon) — tarihçe buraya, bir daha değişmez.
2. `docs/DURUM.md` baştan yazılır (≤60 satır). Eskiden tek bir devam notu 1873 satıra, 48 bölüme ve
   41 yıldızlı başlıklara ulaştı; oturum başı okuması pahalılaştı, bilgi gömüldü.
3. **"Kalıcı bir ders çıktı mı?"** → projeye özgüyse `docs/TUZAKLAR.md`, her projede geçerliyse kite
   (bölüm 8). Aynı tuzağa ikinci kez düşüldüyse hook'a taşınır.

## 8. Kitin bakımı — v1 neden bayatladı, v2 neden bayatlamayacak

v1 kit 21.09'da yazıldı ve bir daha dokunulmadı; canlı projedeki script'ler ve kurallar ondan ayrıştı,
yeni dersler süpervizörün makineye bağlı hafızasında kaldı (110 dosya). Başka PC'deki oturum, alt
agent'lar ve yeni projeler hiçbirini görmedi.

v2'de:
- Kit ayrı bir depodur (`KitYolu`). Projeye `install.ps1` ile kurulur; kurulan her dosyanın hash'i
  `agent-kit/SURUM.json`'a yazılır.
- `install.ps1 -Fark` → kit dosyası projede değiştirildiyse görünür ("kite taşı"). `-GeriTasi` onu
  kite geri kopyalar. `-Guncelle` kitteki yenilikleri projeye alır.
- Proje dosyalarının içindeki kit bölgeleri (`<!-- KIT:<AD>:BASLA -->`) — CLAUDE.md'deki kurallar bloğu,
  agent tanımlarındaki ortak blok — güncellemede yenilenir, dışarısı korunur.
- Teknik tuzaklar git'teki `docs/TUZAKLAR.md`'de durur; hafızada yalnız kullanıcı tercihleri kalır.
- Her checkpoint'in son sorusu "kite bir satır var mı?"dır.

## 9. Kullanıcı Unity'deyken — kısa kontrol listesi

- [ ] Yeni iş worktree'de; ana klasöre yazma yok.
- [ ] Play'e girme (kullanıcı test etmeni istemiyor). Ölçüm gerekiyorsa önizleme sahnesi ya da kopya.
- [ ] Refresh/derleme öncesi: `isPlaying` · prefab stage · `isDirty`.
- [ ] Ana Editor'de test koşusu öncesi: commit'siz prefab/sahne/asset yok (hook da bakar), PlayerPrefs yedeği.
- [ ] Kullanıcının commit'siz dosyasına dokunmadan önce yedek; birleştirmede `git merge-file` ile üç yollu.
- [ ] Birleştirince "geçirdim, şu dosyalar değişti" de.
