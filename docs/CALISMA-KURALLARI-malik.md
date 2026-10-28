# ÇALIŞMA KURALLARI — Malik (karakter yürüme/koşma mekaniği)

> Bu dosya Malik'in makinesinde `CLAUDE.local.md` üzerinden yüklenir (agent-kit v2, kurulum 2026-10-28:
> `powershell -File scripts/kit-kur-yerel.ps1 -Kisi malik`). `CLAUDE.md` Malik'in devir notudur ve geçerlidir; bu dosya
> yalnız kit düzenini ve ekip sınırlarını ekler. Çelişkide `CLAUDE.md`'deki kullanıcı kuralları (Malik'in tercihleri) kazanır,
> ekip sınırları (§1) ve git (§3) bu dosyadan. Hook korumalıdır.

## 0. Ortam
- Unity 2022.3.62 (Built-in RP), Windows. Kabuk PowerShell 5.1. Proje kökü = depo kökü.
- Paketler: Animation Rigging, Input System (Both), MCP for Unity; UI tarafı uGUI + legacy `Text` (TextMeshPro YOK, Çin pazarı),
  Zenject (`Assets/Plugins/Zenject`), DOTween (`Assets/Plugins/Demigiant`), Test Framework ekledi.
- Kit dosyaları: `scripts/` (hook'lar, `tur-sonu`, `kopya`, `unity-verify`), `prompts/`, `agent-kit/` (salt okunur belgeler).

## 1. Ekip ve alanlar
| Kim | Alan | Malik'in oturumları için |
|---|---|---|
| **Malik** | Karakter yürüme/koşma: `Scripts/Physical/`, `Physical/Poses`, `Prefabs/Character`, `Scenes/Test/PhysicalTest.unity`, `Scripts/Runner/RunnerInput.cs`, `SideFollowCamera.cs`, karakter/rig asset'leri, `CLAUDE.md`, `PlayerController.md` | Senin alanın — serbest |
| **Pundun (UI/meta)** | Menüler, UI, ekonomi, kayıt, sahne akışı: `Scripts/Core|Meta|UI|Bridge/`, `Tools/`, `UI/`, `Data/`, `Tests/`, `Scenes/Menu/`, `Scenes/Game/Race.unity`, `Resources/ProjectContext.prefab`, `Plugins/`, `docs/DURUM.md`, `docs/tasks/`, `docs/YOL-HARITASI.md`, `docs/TEMEL-SOZLESME.md`, `docs/gdd/` | **Yazılmaz** — hook engeller. Okunur |
| **Serhat** | Dünya/tema/harita: `Scripts/Runner/` (LoopWay* vb.), `Scripts/Themes`, `Editor/` ThemeKit*, `Prefabs/Themes` … | Değiştirmeden önce sor |

- **UI tarafı mekaniği okur:** `Race.unity`, `PhysicalTest.unity`'nin kopyasıdır; bir adaptör `RaceController` / `RunnerBody`'nin
  public alan ve olaylarını salt okur (`docs/TEMEL-SOZLESME.md` §10-11). Bunları (ad, tür, anlam) değiştirmeden önce Pundun'a
  haber ver; değişiklik `docs/TUZAKLAR.md`'ye bir satır.
- UI tarafının istediği ama mekanikte olmayanlar (yarış başladı/bitti olayı, bitişte başa dönmeme, UI üstündeki dokunmanın adım
  sayılmaması, geri sayım) kullanıcı kararına bağlı; Malik yapacaksa `docs/tasks-malik/`'te task açılır.

## 2. Mod
- `docs/DURUM-malik.md` ilk satırı. Varsayılan **hızlı**: Malik'in bugünkü akışı (tek oturum, Unity MCP ile play-mode ölç/ayarla,
  küçük adımlar). Büyük, ayrık iş için `MOD: süpervizör` (iş alt agent'lara, `docs/tasks-malik/` task dosyalarıyla).

## 3. Git
- `main` ortak; Malik, Serhat ve Pundun aynı dala push eder. `CLAUDE.md`'deki "git'e izinsiz dokunma" Malik'in kuralı olarak geçerli.
- Commit her zaman yolla sınırlı: `git commit -F <mesaj-dosyası> -- <yollar>` (`git add -A`, `.`, `commit -a` hook'la engelli;
  paylaşılan checkout'ta başkasının değişikliğini alır). Mesaj dosyasının adında "main" geçmesin. Push'u Malik yapar.
- Pull'dan sonra çakışma UI tarafının dosyasındaysa onu değiştirme; Pundun'a bildir.

## 4. Hook'lar (kural prompta değil hook'a yazılır)
- `.unity/.prefab/.asset/.meta/.mat` dosyaları Write/Edit ile yazılmaz → Unity'de MCP ile `SerializedObject` + `SaveAssets`
  (mevcut yöntemin). Yazım sonrası `git diff` ile yalnız istenen satırları kontrol et (CLAUDE.md kuralı).
- MCP `run_tests` commit'siz prefab/sahne varken durur (test koşusu prefab'ı geri alabiliyor). `refresh_unity` Malik'te serbest
  (prefab'ı sık değiştirdiği için bilerek kapalı).
- `git push`, `reset --hard`, internetten indirme engelli; geçmek gerekirse kullanıcıya sor, `scripts/kit-izin.ps1`.

## 5. Doğrulama ve tuzaklar
- Derleme/test kopyada: `scripts/kopya.ps1` (config'te `KopyaKoku`'yu doldur) ve `scripts/unity-verify.ps1`. Ana Editor açıkken
  batchmode aynı projeyi açamaz (çıkış 12 = ortam, kod hatası değil).
- `docs/TUZAKLAR.md` ortaktır: yeni tuzak anında, tek satır, §12'ye.

## 6. Çalışma düzeni (kit bloğu)
`agent-kit/07-AKIS.md`, `prompts/MASTER-PROMPT.md`. Mesaj sonu biçimi: **⚠ Dikkat / Senden beklenen / Yapılan**. Pano kuruluysa
önemli bulgu `bildir` ile panoya.
