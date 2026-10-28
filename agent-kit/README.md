# AGENT KIT v2 — süpervizör + alt agent düzeni, her projede

**Ne:** Bir Unity mobil oyununda (Piggy Payday, Ağustos–Ekim 2026, ~440 task, 6 alt agent, 11.500+ EditMode
testi) kurulan çalışma düzeni ve kaybedilen turlardan çıkan dersler. Kurallar, prompt'lar, agent tanımları,
hook'lar, script'ler ve şablonlar. Tek komutla yeni projeye kurulur, sonra güncellenir.

**v2'de ne değişti:** v1 (21.09) "gece dönen otonom tur" anlatıyordu ve bir daha güncellenmedi; canlı
proje ondan ayrıştı, 5 haftalık ders yalnız bir makinenin hafızasında kaldı. v2 gerçek düzeni anlatır
(`07-AKIS.md`), ayrı bir depodadır, `install.ps1` ile kurulur ve projedeki sapmayı `-Fark` ile gösterir.

---

# 🤖 AGENT: BUNU OKUYORSAN, ÖNCE BURAYI OKU

## Adım 1 — Durumunu belirle

| Durum | Ne yapacaksın |
|---|---|
| **A.** Yeni proje, elde GDD/tasarım `.md` var | `prompts/gdd-bootstrap.md`. **Gün 0'da kod yazma.** |
| **B.** Yeni proje, sadece fikir var | `01-BASLANGIC-PROTOKOLU.md` Gün 0 |
| **C.** Devam eden projeye sonradan takılıyorsun | Gün 0'ı geriye dönük, koda bakarak; eski task'lar için `scripts/task-gocu.ps1` (önce kuru) |
| **D.** Kit zaten kurulu, oturum açıyorsun | `docs/DURUM.md` → `ListAgents` → `git status`. Gerisi `prompts/supervisor.md` |

> **Her durumda:** `prompts/MASTER-PROMPT.md` — kullanıcıyla nasıl konuşacağın, ne zaman itiraz edeceğin.

## Adım 2 — Sırayla oku

1. `00-DERSLER.md` — kuralların neden var olduğu. Her kırılma ailesi gerçek bir kayıptan geliyor.
2. `07-AKIS.md` — bir iş baştan sona nasıl yürür: mod, worktree, dağıtım, doğrulama, birleştirme, checkpoint.
3. `docs/TUZAKLAR.md` (projede) — bu projede düşülmüş tuzaklar.
4. Kod yazacaksan `02-MIMARI-STANDARTLARI.md` + `03-HIS-VE-GERI-BILDIRIM.md`; UI ise `05-DOTWEEN-UI-ANIMASYON.md`.
5. Kullanıcı aynı projede çalışıyorsa `04-KULLANICIYLA-CALISMA.md`; verimli konuşmak için `06-BIRLIKTE-CALISMA.md`.

## Adım 3 — Bu 12 kuralı ezberle

1. **Mod tahmin edilmez.** `docs/DURUM.md` ilk satırı: `MOD: süpervizör` (her kod işi agent'a) ya da `MOD: hızlı`.
2. **Sadece kendi `owns_paths`'ine yaz.** Gerekirse `BLOCKED:` yaz, dur.
3. **UI asla runtime'da kurulmaz.** Her ekran sahnede prefab; view bağlar, üretmez.
4. **Sahne/prefab/asset'e yalnız `scene-integrator`.** Gerekirse task `needs:`'e `tip: SCENE`.
5. **Kompozisyon köküne yazma.** Seam aç, `needs:`'e `tip: WIRING`. Açık WIRING/SCENE ile tur kapanmaz.
6. **Denge sayılarına dokunma.** Kod ile test çelişiyorsa KOD haklıdır.
7. **Sayı config asset'inde durur.** Değiştirmeden önce okuma yolunu grep ile kanıtla.
8. **Test sabit denge sayısı assert etmez.**
9. **`status` sabit değerlerden biridir**; `merged`/`done` yalnız süpervizörde, `done` kullanıcının "tamam"ı.
10. **Ana Editor'de test koşma, kopyada koş.** Koşu kullanıcının prefab'ını geri alır, kaydını siler.
11. **Kullanıcı Unity'deyken iş worktree'de; commit yolla sınırlı** (`git commit -F m -- <yollar>`).
12. **Agent raporuna değil diff'e inan.** "Bitti" bir iddiadır.

---

# 👤 İNSAN: KURULUM

```powershell
# Yeni proje (ya da kit henüz kurulu olmayan proje):
powershell -File D:\Pundun\agent-kit\install.ps1 -Proje D:\yol\yeni-oyun -Kuru   # ne yapacağını göster
powershell -File D:\Pundun\agent-kit\install.ps1 -Proje D:\yol\yeni-oyun         # kur
```

Kurulum ne yapar (`kit-manifest.psd1`):
- **Kit dosyaları** (kitin malı, projede birebir): `scripts/*.ps1`, `prompts/*.md`, kitin belgeleri
  `agent-kit/` altına salt okunur anlık kopya. Hash'leri `agent-kit/SURUM.json`'a yazılır.
- **Şablonlar** (yalnız ilk kurulumda, varsa dokunulmaz; sonra projenin malı): `CLAUDE.md`,
  `.claude/settings.json`, `.claude/agents/*.md`, `agent-kit.config.psd1`, `docs/DURUM.md`, `docs/TUZAKLAR.md`,
  `docs/tasks/_TEMPLATE.md`, `docs/checkpoints/_TEMPLATE.md`, `_backlog.md`, ADR/OPEN şablonları.
- **Kit blokları** (projenin dosyası, içinde kitin bölgesi): CLAUDE.md'deki `<!-- KIT:KURALLAR:... -->`,
  agent tanımlarındaki `<!-- KIT:ORTAK:... -->`. Güncellemede yalnız işaretlerin arası yenilenir.

Sonra elle:
1. `agent-kit.config.psd1` — `<KÖŞELİ>` yerler: kompozisyon kökü, kopya kökü/öneki, analytics dosyaları.
   Aynı depoda birden çok kişi kit kullanıyorsa kişiye özel kilitler (`KorunanBelgeler`, `DurumDosyasi`, `TaskKlasoru` …)
   `agent-kit.config.local.psd1`'e (git'e girmez; aynı anahtarı ezer).
2. `CLAUDE.md` — projeye özgü bölümler (ortam, kod standardı, kompozisyon kökü, analytics kuralı).
3. Doğrulama script'i — `scripts/verify.ps1.ORNEK` sözleşmesiyle (`-ProjectPath`, `-CompileOnly`, `-TestFilter`;
   çıkış 0 / 1-3 / 10-19). Yolunu config'teki `DogrulamaScripti`'ne yaz.
4. **Hook'ları dene:** bir agent'a bilerek `.prefab` yazdır, `git add -A` çalıştırt. Engellenmiyorsa hook çalışmıyor.

## Pano — yan panel (makine başına bir kez)

Süpervizörün önemli bildirimleri (⚠ risk / ? karar / bitti), senin notların, ara mesajlar ve (Göster ile) yapılan
işler tek bir yan panelde. Mesajlar uzun iş akışında kaybolmasın diye var (CLAUDE.md "Çalışma düzeni" G).
Kit klasörü bir eklenti pazarıdır (`.claude-plugin/marketplace.json`); panel `plugins/pano/` altında.

```powershell
claude plugin install pano --marketplace D:/Pundun/agent-kit -s user
```

Terminalde `/plugin install pano --marketplace D:/Pundun/agent-kit` da olur. Desktop'ta: prompt kutusunun yanındaki
`+` → Plugins → Add marketplace (`D:/Pundun/agent-kit`) → pano. Kullanıcı kapsamında bir kez kurulunca bütün
projelerde açılır; kapanırsa `/pano`. Araçları: `bildir` (panoya madde) ve `notlar` (kullanıcının notları).

## Kiti güncel tutmak

```powershell
powershell -File D:\Pundun\agent-kit\install.ps1 -Proje . -Fark           # kit ile proje arasındaki farklar
powershell -File D:\Pundun\agent-kit\install.ps1 -Proje . -Guncelle       # kitteki yenilikleri al
powershell -File D:\Pundun\agent-kit\install.ps1 -Proje . -GeriTasi -Yol scripts/kopya.ps1   # projede düzelttiğini kite taşı
```

- Projede bir kit dosyasını düzelttiysen `-Fark` "projede-değişmiş" der → `-GeriTasi` ile kite taşı, kitte commit'le.
- Projeye özgü tuzak `docs/TUZAKLAR.md`'ye; her projede geçerli olan kitin `templates/TUZAKLAR.md`'sine ve
  gerekirse `00-DERSLER.md`'ye.
- Her checkpoint'in son sorusu: "kite bir satır var mı?"

## Günlük komutlar (projede)

| Komut | Ne yapar |
|---|---|
| `scripts/tur-sonu.ps1 [-DurumYaz] [-Since <ref>]` | Tur sonu kapısı: açık needs, review kuyruğu, göz listesi, testsiz task, analytics, worktree'ler, kopyalar. Çıkış 0 = temiz |
| `scripts/kopya.ps1 -Liste / -Al / -Senkron / -Dogrula / -GeriAl / -Birak` | Doğrulama kopya havuzu |
| `scripts/kit-izin.ps1 <ad>` | Hook engelini bilinçli olarak 10 dk aş (önce kullanıcıya sor) |
| `scripts/prefs-yedek.ps1 -Al / -Karsilastir / -GeriYukle` | Editor PlayerPrefs (dev kaydı) yedeği |
| `scripts/task-gocu.ps1 [-Uygula]` | Eski biçimli task dosyalarını v2'ye taşır (önce kuru) |

---

## Dosya haritası

```
agent-kit/
├── README.md · VERSION · install.ps1 · kit-manifest.psd1
├── .claude-plugin/marketplace.json   kit klasörü = eklenti pazarı
├── plugins/pano/              yan panel modu (Önemli · Notlar · Mesajlar · İşler)
├── 00-DERSLER.md              ne kırıldı, neden, yeni kural (K1-K10 Ağu-Eyl, K11-K19 Eyl-Eki)
├── 01-BASLANGIC-PROTOKOLU.md  GDD geldiğinde Gün 0/1/2/3
├── 02-MIMARI-STANDARTLARI.md  DI · Find yasağı · enum · pooling · event · over-engineering
├── 03-HIS-VE-GERI-BILDIRIM.md beş kanal · durum görünürlüğü · efekt bütçesi
├── 04-KULLANICIYLA-CALISMA.md editör kullanıcının elinde: save, prefab, Play, worktree
├── 05-DOTWEEN-UI-ANIMASYON.md tek UiMotionConfig + reçeteler
├── 06-BIRLIKTE-CALISMA.md     post mortem + çalışma sözleşmesi
├── 07-AKIS.md                 ← gerçek düzen: mod, iş yolculuğu, paralel dağıtım, doğrulama, checkpoint
├── blocks/                    projedeki dosyalara giren kit bölgeleri
│   ├── KURALLAR.md            → CLAUDE.md "Çalışma düzeni"
│   └── ORTAK.md               → her agent tanımının ortak bölümü
├── prompts/                   MASTER-PROMPT · supervisor · 00-ilk-mesaj · gdd-bootstrap
├── agents/                    gameplay · systems · ui · scene-integrator · test · denetci
├── templates/                 CLAUDE.md · settings.json · agent-kit.config.psd1 · task · DURUM · checkpoint
│                              · TUZAKLAR (Unity tohumu) · ADR · OPEN · _backlog · TEMEL-SOZLESME
├── scripts/                   kit-lib · task-lib · tur-sonu · task-gocu · kopya · kit-izin · prefs-yedek
│                              · hook-guard-write · hook-guard-command · hook-guard-mcp · hook-post-agent
│                              · verify.ps1.ORNEK
└── tests/                     script'lerin smoke testleri (gerçek Unity açmaz)
```

## Bu kit hangi sorunları çözüyor

| Problem | Kitin cevabı |
|---|---|
| Agent'lar birbirinin dosyasını ezer | `owns_paths` + tek sahipli task dosyası |
| Prompt'taki kural unutulur | Kural **hook**'la zorlanır (`exit 2`) |
| "Süpervizör kod yazar mı" her gün yeniden tartışılır | Adlandırılmış **mod**, DURUM.md'nin ilk satırı |
| Kod yazılır ama oyuna hiç girmez | Frontmatter `needs:` + `tur-sonu.ps1` kapısı |
| Review kuyruğu mezarlığa döner (~165 task) | `merged` (göz bekliyor) ≠ `done` (kullanıcı tamam dedi) |
| Devam notu 1873 satıra şişer | `DURUM.md` ≤60 satır + `checkpoints/` tarihçe |
| Test koşusu kullanıcının prefab'ını geri alır, kaydını siler | `hook-guard-mcp` + kopya havuzu + PlayerPrefs yedeği |
| Paralel oturum birbirinin stage'ini commit'ler | `git add -A`/`commit -a` engeli, yolla commit |
| Dersler bir makinenin hafızasında kalır | `docs/TUZAKLAR.md` git'te; kit ayrı depo, `install -Fark` |
| Kit canlı projeden ayrışır | `SURUM.json` hash'leri + `-Fark` / `-GeriTasi` |
| Yeşil test "çalışıyor" sanılır | "Bitmiş" tanımı + oyunda ulaşılabilirlik |
| Ortam hatası kod hatası sanılır | Doğrulamanın **3** sonucu |

## Tek cümlelik özet

**Belgeye değil diske güven; kuralı prompta değil hook'a yaz; modu tahmin etme, oku; "bitti" demeden
önce oyunda ulaşılabilir mi bak; dersi makinede değil git'te tut.**

---

*Kaynak: Bills Must Be Paid / Piggy Payday · Unity 2022.3 · v1 21.09.2026 · v2 27.10.2026*
