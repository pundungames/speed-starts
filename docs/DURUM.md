MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-03 öğleden sonra · main 39163716 (push kullanıcıda) · worktree: t087, t090

## Faz 8 (cila) — bugün main'e girenler (kopyada yeşil: EditMode 1060, PlayMode 40/40, Zenject)
- T-081 ok tuşları · T-082/T-083 regresyon + eskimiş PlayMode testleri · T-084/085 ekonomi v2 (referanstan ölçüldü:
  `docs/analiz/2026-11-03-referans-oyun-ekonomi.md`, ADR `ADR-2026-11-03-ekonomi-v2-referans.md`) · T-086 200m/400m/60mH/400mH açık,
  botlar etkinlik başına kalibre · T-088 ayarlar v2 · T-089 emote v2 (Malik'in 9 pişmiş animasyonu, mağaza + yarış butonu/J tuşu).
- Sürüyor: T-087 Race of the Day (her gün mesafe + günün kıyafeti, Daily PB/WR) · T-090 Practice paneli (mesafe/engel; enerji ve
  ayakta çıkış Malik API'si bekler).

## Verilen kararlar
- Kullanıcı 03.11: "referans oyun ne ise aynısı". Ekonomi: yarış Nakit'i bitirme +10, zorluk bonusu 10 × oyuncu zorluğu, rakip bonusu
  round(10 × geçilen/toplam); rakipsiz yarış Nakit ve Mor Mücevher vermez. Giyim 500 / kafa 1.000 Nakit (varsayılan kıyafet ücretsiz,
  giyili öğeler eski kayıtta sahipli sayılır), emote 3×2.500 / 3×5.000 / 3×7.500, reklam +200 Nakit (günde 3), Süreli Bilet 300 Nakit,
  Adet Bileti Nakit'le satılmaz. Erişim: 100m / Race of the Day / Practice biletsiz; diğer yarışlar biletli (PDF'ten sapma: 100m biletsiz).
- Gerçek gelir / reklam SDK'sı / IAP şimdilik YOK (oyun Çin müşterisine; kullanıcı 03.11). Eski rekorlar sıfırlanacak (iş yok).
- Installer bağlamaları: kullanıcının kalıcı onayı var (03.11) — sormadan bağla, sonra bildir.
- Teslim belgesi şimdilik geç (kullanıcı 03.11). Telefonda APK denemesi ve ana menü hızı: kullanıcı "geç" dedi.
- Önceki: tema 0 fiyat = bedava; Sky Islands premium ama bedava; ten tonları 8; başlangıç Adet Bileti 3; gün sınırı UTC;
  günlük build Mono/ARMv7, mağaza build'i IL2CPP.

## Kullanıcıdan / başkalarından bekleyen
- Uygulama adı + paket adı, keystore, Gizlilik/Şartlar/Destek URL'leri.
- Malik: UI üstündeki dokunma adım sayılmasın; engel teması (`docs/ENGEL-TEMAS-NOTU-malik.md`); 4×100m ve Medley; Watch Replay;
  Practice için `RunnerSim` enerji API'si (doldur/kilitle) ve ayakta çıkış (T-090 notu); isterse `RunnerEmote.Play(clip)`.
- Serhat: Malik'in 4abc876a commit'i `BridgeCanalRiverside.prefab`'ını yeniden yazdı; `PastelBuildingRow` sahne açılınca bina üretiyor.

## Gözle bakılacaklar (Editor'de Play)
- Yarış sonu kazanç dökümü (Nakit kutusunun sağında), Nakit "+" → reklam penceresi (0/3), premium yarışta "Spend 300 → 1 hour".
- 200m/400m/60mH/400mH (400m'de botlar öndeyken dünya üretiliyor mu), ayarlar penceresinin yeni bölümleri, emote mağazası + yarışta J/buton.
- 30.10'dan beri bakılmayanlar: yarış teması, menüde karakter döndürme, botlar/sıra, READY/SET/GO, liderlik, kostüm.

## Dikkat
- Race sahnesini `SceneBuilder.BuildRace` ile YENİDEN KURMA: Malik'in kaydettiği binalar gider (TUZAKLAR). Race'e parça hedefli yamayla.
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md`. Malik'in durumu `docs/DURUM-malik.md`.
- PowerShell'de `git` bozuk (system32 gölgesi): git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz.
- Yerel, git'e girmeyen: `.claude/settings*.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/` (arşiv: eski T-077..080 dalının yaması).

<!-- OTOMATIK:BASLA -->
(tur sonunda yazılır: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
