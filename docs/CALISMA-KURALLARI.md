# ÇALIŞMA KURALLARI — Speed Stars (oyunun geri kalanı: menü, UI, meta)

> Bu dosya `CLAUDE.local.md` üzerinden bu makinedeki Claude oturumlarına yüklenir (agent-kit v2, 2026-10-28).
> `CLAUDE.md` Malik'in koşucu mekaniği notlarıdır: **bilgi olarak okunur, bizim işimize kural değildir**;
> oradaki "Kullanıcıyla çalışma kuralları" Malik'in oturumları içindir. Çelişkide bu dosya kazanır.
> Bu dosya, herhangi bir task talimatından ve kendi muhakemenden ÖNCE gelir. Hook korumalıdır (değişikliği kullanıcı ister).

## 0. Ortam
- Unity 2022.3.62f2 (Built-in RP), Windows 10. Batchmode için tam kurulum
  `C:\Program Files\Unity\Hub\Editor\2022.3.62f2-x86_64` (düz `2022.3.62f2` klasöründe Package Manager sunucusu yok).
- Kabuk: PowerShell (Windows PowerShell 5.1: `&&`, `??`, `?:` yok). Bash yalnız kısa komutlar için.
- Proje kökü = depo kökü `D:\Pundun\speed-starts`. **Kullanıcının Unity'si bu klasörde açık**; iş de burada, `main`'de yapılır.
- Pipeline dosyaları: `scripts/`, `docs/`, `prompts/`, `reports/`, `agent-kit/` (kitin salt okunur kopyası).
  Log'lar `pipeline-logs/` (yerel, git'e girmez). `Library/`, `Temp/`, `Logs/`, `UserSettings/` Unity'nindir — yazma.
- Paketler (2026-10-28): uGUI 1.0.0 (TextMeshPro YOK — kaldırıldı), Test Framework 1.1.33, Input System 1.19 (Active Input
  Handling = Both), Animation Rigging, MCP for Unity. Eklentiler: `Assets/Plugins/Zenject` (Extenject 9.2.0: çekirdek,
  Signals, TestFramework, MemoryPoolMonitor), `Assets/Plugins/Demigiant/DOTween` (1.3.045, ücretsiz).

## 1. Ekip ve alanlar (2026-10-28, kullanıcı kararı)

| Kim | Alan | Bizim için |
|---|---|---|
| **Malik** (game dev) | Karakterin yürüme/koşma mekaniği: `Scripts/Physical/` (Editor/PhysicalSetup dahil), `Physical/Poses`, `Prefabs/Character`, `Scenes/Test/PhysicalTest.unity`, `Scripts/Runner/RunnerInput.cs`, `SideFollowCamera.cs`, karakter/rig asset'leri (`Kevin Iglesias`, `Ash Assets`, `Samples/Animation Rigging`), `CLAUDE.md` | **Yazılmaz** — hook engeller (config `KorunanBelgeler`). Okunur. |
| **Serhat** | Dünya/tema/harita: `Scripts/Runner/` (LoopWay*, Pastel*, Accessory*, CloudVariation, RunnerContactShadow), `Scripts/Themes`, `Editor/` (ThemeKit*, LoopWay*, …), `Prefabs/` (Themes, StreetProps, Buildings, LoopWay*, …), `Materials/`, kendi test sahneleri | Hook kilidi yok; **değiştirmeden önce kullanıcıya sor**. |
| **Kullanıcı + biz** | Oyunun geri kalanı: ana menü, UI, meta sistemler, yeni sahneler, altyapı (Zenject, DOTween, test) | Bizim kod: `Assets/Speed-Staes/Scripts/Core/`, `Scripts/UI/`, `Scripts/Meta/`; UI prefab'ları `Assets/Speed-Staes/UI/`; sahneler `Assets/Speed-Staes/Scenes/Menu/` (ve kullanıcının açtıkları). |

- UI'ın mekanikten ihtiyaç duyduğu bir olay/özellik yoksa **Malik'in koduna yazılmaz**: task `needs:`'e `tip: DECISION`
  (ne gerektiği `not:`'ta), kullanıcı Malik'ten ister. Mekaniğin public API'sini okuyan adaptör bizim tarafta yazılır
  (giriş/çıkış noktaları: `docs/TEMEL-SOZLESME.md`).
- `Scenes/Game/Run_100m.unity`'nin sahibi belirsiz: dokunmadan önce sor.

## 2. Dosya sahipliği
Her agent SADECE kendi task dosyasındaki `owns_paths` altına yazar. Başka bir yola yazman gerekiyorsa YAZMA —
task dosyasına şunu bırak ve dur:

    BLOCKED: <yol> yazma yetkim yok. Gereken değişiklik: <açıklama>

## 3. Sahne, prefab ve asset dosyaları
- `.unity`, `.prefab`, `.meta`, `.asset`, `.mat`, `.controller`, `.uxml`, `.uss` dosyalarına SADECE `scene-integrator`
  dokunur (hook). Sahne değişikliği gereken iş: `needs:`'e `tip: SCENE` (ne gerektiği `not:`'ta), durum `blocked`.
- Kullanıcının açık sahnesinde/prefab'ında kaydedilmemiş iş olabilir: MCP ile sahne/prefab işinden ve refresh'ten önce
  `isPlaying` · prefab stage · `isDirty` kontrolü. Kullanıcının açık sahnesini onun yerine kaydetme.
- Referansları Inspector'dan elle bağlamak yerine mümkün olan her yerde ScriptableObject + Zenject bağlaması.

## 3.1 UI ASLA RUNTIME'DA KURULMAZ
Her UI ekranı **sahnede ve prefab olarak** durur; Inspector'dan görülebilir, seçilebilir, taşınabilir. Kullanıcı UI'ı
kendisi düzenleyecek.
- Kodla **kalıcı** UI hiyerarşisi kurmak YASAK. View script'i hazır nesneleri `[SerializeField]` ile **bağlar ve doldurur**.
- Dinamik içerik: sahnede **satır prefab'ı** + **content parent**; kod prefab'ı `Instantiate` eder (Zenject'te factory).
- Prefab hazır değilse ekran **eksik kalır**; kodla sahte iskelet kurulmaz. `needs:`'e `tip: SCENE`.
- İstisna: `**/Dev/**` — geçicidir, `#if UNITY_EDITOR || DEVELOPMENT_BUILD` ile korunur.

## 4. Doğrulama
- Doğrulama script'i `scripts/unity-verify.ps1` (sözleşme: `scripts/verify.ps1.ORNEK`; 0 yeşil / 1-3 kırmızı /
  10-19 doğrulanamadı). Ana klasörde Unity açık olduğu için doğrulama **kopyada** koşar
  (`scripts/kopya.ps1`, kopyalar `D:\Pundun\speed-starts-verify-1..2`).
- "Derlendi sanıyorum" kabul edilmez. Doğrulama **merkezîdir**: paralel agent'lar Unity/batchmode/verify çalıştırmaz;
  süpervizör birleştirince bir kez çevirir.
- Üç sonuç: **yeşil / kırmızı / doğrulanamadı(ortam)**. Üçüncüsü KOD HATASI DEĞİLDİR.
- Test rejimi: görsel UI işi (yerleşim, animasyon, metin) = derleme, `tests: "gerekmiyor: görsel"`. Mantık içeren her kod
  (durum makinesi, biçimlendirme, kayıt, ekonomi, ilerleme) EditMode testi ister; testi yoksa `merged` olmaz.
  Test yerleşimi ilk test işinde belirlenir: Malik'in kodu asmdef'siz (Assembly-CSharp) ve asmdef'li bir assembly
  Assembly-CSharp'a referans veremez — bunu `docs/TUZAKLAR.md`'ye yaz.

## 5. Git (2026-10-28 kullanıcı kararı — kitin "main'e yazma" kuralı bu projede GEÇERSİZ)
- Çalışma `main`'de, ana klasörde. Commit süpervizörde serbest; her commit yolla sınırlı:
  `git commit -F <mesaj-dosyası> -- <yollar>`. Mesaj dosyasının adında "main" geçmez (hook deseni).
- `git push` YOK — kullanıcı yapar. `pull`/`merge origin/main` kullanıcı yapar (Malik ve Serhat aynı main'e push ediyor).
- Kullanıcının ya da Unity'nin commit'siz değişikliği (ör. `ProjectSettings/ProjectSettings.asset`) commit'e katılmaz.
- Büyük/riskli iş ve ağır kontrol: ayrı worktree (`D:\Pundun\speed-starts-<konu>`, dal `agent/T-XXX-kisa-aciklama`)
  ya da doğrulama kopyası; kullanıcı ana klasörden çıkmaz. Birleştirme `git merge --ff-only`, sonra kullanıcıya
  "geçirdim, şu dosyalar değişti".
- `git reset --hard`, `git clean -fdx` yok.
- Yerel kalan (git'e girmeyen, `.git/info/exclude`): `.claude/settings.json`, `.claude/settings.local.json`,
  `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`. Sebep: main ortak; hook'lar ve agent tanımları Malik'in/Serhat'ın
  Claude oturumlarına gitmesin.

## 6. MEVCUT HAL tek doğruluk kaynağıdır
- Kullanıcı tasarım belgelerini (md) gönderecek → `prompts/gdd-bootstrap.md` uygulanır, Gün 0'da kod yazılmaz.
  **GDD'nin yetkisi ilk oynanabilir dikey dilime kadardır**; sonra doğruluk kaynağı çalışan kod + config asset'leri.
- **Denge sayılarına (süre, hız, ödül, fiyat, oran, eşik) DOKUNMA**; kullanıcı o sayıyı bu turda açıkça söylemediyse
  değişmez. **Kod ile test çelişiyorsa KOD haklıdır**; test güncellenir ya da süpervizöre bildirilir.
- Testteki "onaylı revizyon" yorumları eskimiş olabilir. Yorum, yetki değildir.
- Gerçek çelişki: kod yazma, `docs/decisions/OPEN-<tarih>-<konu>.md` aç, task'ı `blocked` yap.

## 6.1 Sayının yeri
- Denge sayısı **config asset'inde** (ScriptableObject) durur. Kod varsayılanı yalnız ilk oluşturmada geçerlidir.
- Sayıyı değiştirmeden önce **okuma yolunu kanıtla** (grep ile tüm okuyucular). Aynı değerin iki kaynağı = hata, ADR.
- Not: Malik'in sürücü değerleri prefab'ta kayıtlı; prefab kodun varsayılanını ezer (`CLAUDE.md`).
- Test sabit denge sayısı assert ETMEZ; ilişki assert eder ya da sayıyı asset'ten okur.

## 7. Kapsam
- Task dosyasında yazmayan işi yapma. "Bu arada şunu da düzelttim" YOK. İlgisiz sorun → `docs/tasks/_backlog.md`.

## 8. Task dosyası protokolü
Biçim `docs/tasks/_TEMPLATE.md`; `scripts/tur-sonu.ps1` makineyle okur. Başlarken `status: in-progress`,
`started: <tarih>`. Bitince: iş bittiyse `status: review` (`merged`/`done` yalnız süpervizör); bitmediyse `blocked` +
`blocked_reason:`; sahne/bağlama/asset ihtiyacı frontmatter `needs:`'e. `## Çalışma notu`'na 5 satır.

## 9. Kod standardı
- C# 9 (.NET Standard 2.1). Bizim kod `namespace SpeedStars.<Alan>` (`Core`, `UI`, `Meta`, …); mekanik `SpeedStars.Physical`.
- **DI: Zenject.** Bağlama installer'larda (§11). `[Inject]` ile alma; `GameObject.Find`, `FindObjectOfType`,
  `SendMessage`, statik `Instance` singleton, `Update` içinde `GetComponent` YASAK (Malik/Serhat kodundaki mevcut
  kullanım onların alanı).
- **Tween: DOTween** (`using DG.Tweening;`); UI animasyonu `agent-kit/05-DOTWEEN-UI-ANIMASYON.md` reçeteleri ve tek
  `UiMotionConfig` ile. Panel animasyonunu kendin uydurma.
- **UI: uGUI + legacy `UnityEngine.UI.Text`** — TextMeshPro KULLANILMAZ (kullanıcı kararı 28.10: oyun Çin pazarına; legacy Text'in
  dinamik fontu cihazda CJK karakterleri işletim sistemi fontundan alır). Font: `LegacyRuntime.ttf` (yerleşik) ya da dinamik bir TTF.
  Oyuncuya görünen metin **İngilizce**. Input: Input System. UI referans çözünürlüğü 2400×1080 yatay, CanvasScaler `ScreenMatchMode.Expand` (kullanıcı kararı 30.10: ≥20:9'da yükseklik eşleşmesiyle aynı, daha dar ekranda sığacak kadar küçülür; T-034).
- Public alan yerine `[SerializeField] private`. Magic number yok — config asset'ine.
- String yerine enum; olay: C# `event`/Zenject Signal (UnityEvent yalnız Inspector'dan bağlanan UI'da).
- `using System;` + `using UnityEngine;` olan dosyada `Object`, `Random`, `Debug` niteliksiz kullanılmaz.
- Yorumlar İngilizce, kısa, "neden"i anlatan `<summary>` (mevcut kod stili).

## 10. Öğrenilen tuzaklar
`docs/TUZAKLAR.md`. **Her agent işe başlarken okur.** Yeni tuzak öğrenen tur sonunu beklemeden ANINDA yazar.

## 11. Kompozisyon kökü — agent'lar dokunmaz
`Assets/Speed-Staes/Scripts/Core/Installers/` (Zenject ProjectContext/SceneContext installer'ları; ilk altyapı işinde
açılır) bu projenin kompozisyon köküdür.
- **Alt agent'lar buraya YAZMAZ** (hook). Kendi sistemini bir seam ile dışa ver (arayüz, `Initialize(...)`, factory).
- Bağlamayı süpervizör yapar. Task frontmatter'ına:

      needs:
        - tip: WIRING
          durum: open
          not: "<sistem> -> <installer/seam> (<ne bekliyor>)"

- **Açık WIRING ile tur kapanmaz** (`scripts/tur-sonu.ps1` çıkış 1). Seam açan task ile bağlama AYNI turda biter.

## 12. Bitmiş ne demek
`merged` için altısı birden: 1) doğrulama **yeşil** (doğrulanamadı ≠ yeşil) · 2) kabul kriterlerinin **hepsi** ·
3) testi **var** (ya da gerekçeli `gerekmiyor`) ve sabit denge sayısı assert etmiyor · 4) **oyunda ulaşılabilir**
(installer'a bağlı, açık WIRING/SCENE yok) · 5) frontmatter **güncel** · 6) **geri bildirimi var** (görsel/ses/dokunsal;
`agent-kit/03-HIS-VE-GERI-BILDIRIM.md`). `merged` → `done`: kullanıcı baktı, "tamam" dedi.

## 12.1 Kullanıcıyla konuşma
`prompts/MASTER-PROMPT.md` her oturumda geçerlidir: itiraz bir kez, tek satır, öneriyle · ölçülemez istekte sayı/benzetme/
ekran görüntüsü iste ve somut varsayımla ilerle · bakabileceğini sorma · seçenek formu açma. Kullanıcı Türkçe konuşur.

## 12.2 Zanaat standartları
`agent-kit/02-MIMARI-STANDARTLARI.md` (DI, Find yasağı, enum, pooling, event, over-engineering sınırları) ·
`agent-kit/03-HIS-VE-GERI-BILDIRIM.md` · `agent-kit/05-DOTWEEN-UI-ANIMASYON.md`.

## 13. Pano
Yan panel (`pano` eklentisi, bu projede `local` kapsamda kurulu). Önemli bulgu ve karar bekleyen şey `bildir` ile panoya;
kullanıcının notları `notlar` ile okunur. Panel açık değilse `/pano`.

## 14. Çalışma düzeni

<!-- KIT:KURALLAR:BASLA -->
## Çalışma düzeni (agent-kit v2)

> Bu blok kitten gelir; elle düzenleme. `install.ps1 -Guncelle` yeniler. Akışın tamamı: `agent-kit/07-AKIS.md`.

### A. Mod — her an biri geçerlidir
- **Süpervizör modu:** kod değişikliği ne kadar küçük olursa olsun alt agent'a gider. Süpervizör dağıtır,
  inceler, doğrular, birleştirir, kompozisyon kökünü bağlar ve agent'ların dokunamadığı prefab/sahne işini yapar.
- **Hızlı mod:** ~5 dakikalık iş (birkaç satır kod, metin, sprite/alan bağlama, tek dosyalık ayar) süpervizör
  tarafından doğrudan yapılır: değiştir → derle → konsol → commit. Büyük iş yine agent'a gider.
- Geçerli mod `docs/DURUM.md`'nin İLK satırındadır. Kullanıcı "süpervizör ol" / "hızlı mod" / "sen yap" dediğinde
  mod değişir; süpervizör o satırı hemen günceller. Mod tahmin edilmez, satırda yazan uygulanır.

### B. Task dosyası (biçim: `docs/tasks/_TEMPLATE.md`)
- `status`: `todo` · `in-progress` · `review` (agent bitirdi) · `blocked` · `merged` (ana dalda, kullanıcının
  gözünü bekliyor) · `done` (kullanıcı "tamam" dedi ya da gözle bakılacak yanı yok) · `cancelled`. Başka metin yazılmaz.
- Bağlantı ihtiyaçları frontmatter `needs:` listesindedir (`tip` SCENE/WIRING/ASSET/DECISION, `durum` open/closed,
  `not`). Gövdeye "NEEDS_X: yok" gibi satır yazılmaz.
- `tests:` ve `analytics:` dolu ya da gerekçeli (`"gerekmiyor: ..."` / `"yok: ..."`).
- `merged` ve `done`'ı yalnız süpervizör yazar.

### C. Oturum başı / tur sonu / checkpoint
- Oturum başı: `docs/DURUM.md` (en fazla 60 satır) → `ListAgents` → `git status` + `git worktree list`.
  Geçmiş `docs/checkpoints/` altındadır; gerekmedikçe okunmaz.
- Tur sonu: `powershell -File scripts/tur-sonu.ps1 -DurumYaz`. Çıkış 0 değilse (açık WIRING/SCENE, format hatası)
  tur kapanmaz. Süpervizör DURUM.md'nin elle yazılan kısmını (mod, bekleyen kararlar, sıradaki iş) günceller.
- Checkpoint ("checkpoint at", gün sonu): `docs/checkpoints/<yyyy-mm-dd>.md` yeni dosya + DURUM.md baştan yazılır
  + "kalıcı bir ders çıktı mı?" → projeye özgüyse `docs/TUZAKLAR.md`, genelse kite (`install.ps1 -GeriTasi`).

### D. Doğrulama
- Üç sonuç: **yeşil / kırmızı / doğrulanamadı (ortam)**. Doğrulanamadı kod hatası değildir.
- Hız öncelikli rejim: görsel/UI/metin işi = derleme + o işin testleri. Tam suite yalnız ekonomi, kayıt, kariyer,
  denge sayısı, kompozisyon kökü, ortak altyapı değişince ya da APK öncesi — tek başına.
- Doğrulama kopyada yapılır: `scripts/kopya.ps1 -Al` → `-Senkron [-HeadOnly]` → `-Dogrula [-TestFilter]` → `-Birak`.
- Ana Editor'de test koşusu/refresh, commit'siz prefab/sahne/asset varken hook tarafından durdurulur. Geçmek için
  önce kullanıcıya sor, sonra `scripts/kit-izin.ps1 unity-test` (ya da `unity-refresh`). Koşudan sonra
  `scripts/prefs-yedek.ps1 -Karsilastir`.

### E. Paralel çalışma
- Kullanıcı Unity'de çalışırken yeni iş ayrı git worktree'de yapılır; ana klasöre yazılmaz. Birleştirme:
  doğrulama yeşil → `git merge --ff-only` → ana Editor'de Play kapalı / prefab stage temiz → refresh → konsol.
- Her commit yolla sınırlıdır: `git commit -F <mesaj> -- <yollar>`. `git add -A`, `git add .`, `git commit -a` engellidir.
- Başka oturumun commit'siz değişikliğine dokunulmaz; konsoldaki yabancı derleme hatası başka oturumun olabilir.

### F. Hook'lar — kural prompta değil hook'a yazılır
`hook-guard-write` (korunan uzantılar, kompozisyon kökü, güvenlik dosyaları) · `hook-guard-command` (push,
reset --hard, add -A, commit -a, uzun heredoc, kopyadan /XO'suz robocopy) · `hook-guard-mcp` (commit'siz prefab
varken test/refresh; test öncesi PlayerPrefs yedeği) · `hook-post-agent` (agent bitince hızlı derleme).
Aynı tuzağa ikinci kez düşüldüyse o madde hook'a taşınmaya adaydır.

### G. Kullanıcıya mesaj — bulgu kaybolmaz
- Ara mesaj tek satırdır: "ne yapıyorum". Bulgu ara mesajın içine gömülmez — uzun iş akışında kaybolur.
- Oyunu etkileyebilecek her bulgu (kırmızı test, beklenmedik davranış, silinen/değişen dosya, başka oturumun değişikliği,
  doğrulanamayan şey) ve kullanıcıdan karar bekleyen her şey O AN panoya iğnelenir (`pano` modu, `bildir` aracı:
  risk / karar / bitti / bilgi). Pano yoksa iş sonu mesajının en üstüne yazılır.
- İş sonu mesajı sabit biçimdedir ve kısadır: **⚠ Dikkat** (yoksa "yok") → **Senden beklenen** → **Yapılan** (en fazla 5 satır).
- Agent raporu geldiğinde önemli bulguları panoya, kullanıcıya tek satır özet; rapor olduğu gibi yapıştırılmaz.
<!-- KIT:KURALLAR:BITIR -->
