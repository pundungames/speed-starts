MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-03 14:45 · main 8cac9cca (push kullanıcıda) · worktree yok (D:\Pundun\speed-starts-t093 klasörü kilitli kaldı, git'e bağlı değil; silinebilir)

## Faz 8 (cila) — bugün main'e girenler (son kopya koşusu yeşil: EditMode 1144, PlayMode 51/51, Zenject)
- T-081..T-090: ok tuşları, regresyon, ekonomi v2 (referanstan; ADR `ADR-2026-11-03-ekonomi-v2-referans.md`), 200m/400m/60mH/400mH,
  ayarlar v2, emote v2 (Malik'in 9 animasyonu), Race of the Day, Practice paneli · e7d269f2 Malik'in 03.11 işi (Watch Replay, kamera, mobil UI).
- T-091 Malik'in elle ayarları builder'da (Close sağ kenara sabit, replaySkin) + EventModal 4. sekme · T-092 Practice'te sonuç ekranı,
  ödül, rekor yok · T-093 High Scores'ta Race of the Day (günün mesafesi, Daily WR ile aynı botlar) · T-094 Watch Replay duman testleri
  · T-095 lisans envanteri (`reports/lisans-envanteri-2026-11-03.md`) · T-096 FPS toleransı: 30/60/120 FPS bitiş farkı %0.001.

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
- Malik: UI üstündeki dokunma adım sayılmasın; engel teması (`docs/ENGEL-TEMAS-NOTU-malik.md`); 4×100m ve Medley;
  Practice için `RunnerSim` enerji API'si (doldur/kilitle) ve ayakta çıkış (T-090 notu); isterse `RunnerEmote.Play(clip)`;
  dev bitiş tuşu (F) replay kaydını bitirmiyor / eski yarışı oynatıyor (T-094 BULGU 1-2); ayak kilidi ölçülmedi (soru).
- Kullanıcı kararı: ReplayControlsView UI'ı koddan kuruyor (kural 3.1) → prefab'a çevrilsin mi; lisans envanterindeki "incelenmeli"ler
  (ürün adı yer tutucu, UI sanatı kaynağı, Mixamo ham FBX repo'da, repo görünürlüğü).
- Serhat: Malik'in 4abc876a commit'i `BridgeCanalRiverside.prefab`'ını yeniden yazdı; `PastelBuildingRow` sahne açılınca bina üretiyor.

## Gözle bakılacaklar (Editor'de Play)
- Yarış sonu kazanç dökümü (Nakit kutusunun sağında), Nakit "+" → reklam penceresi (0/3), premium yarışta "Spend 300 → 1 hour".
- 200m/400m/60mH/400mH (400m'de botlar öndeyken dünya üretiliyor mu), ayarlar penceresinin yeni bölümleri, emote mağazası + yarışta J/buton.
- Practice bitişi (sonuç ekranı yok, Restart), High Scores → DAILY sekmesi, Watch Replay, iki popup'ın Close konumu.
- 30.10'dan beri bakılmayanlar: yarış teması, menüde karakter döndürme, botlar/sıra, READY/SET/GO, liderlik, kostüm.

## Dikkat
- Race sahnesini `SceneBuilder.BuildRace` ile YENİDEN KURMA: Malik'in kaydettiği binalar gider (TUZAKLAR). Race'e parça hedefli yamayla.
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md`. Malik'in durumu `docs/DURUM-malik.md`.
- PowerShell'de `git` bozuk (system32 gölgesi): git Bash'ten. Kancada `git commit` + "main" aynı komutta olmaz.
- Yerel, git'e girmeyen: `.claude/settings*.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/` (arşiv: eski T-077..080 dalının yaması).

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-03 14:39 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 3 (DECISION 3)
- Review kuyrugu: 0
- Goz bekleyen (merged): 88 - T-095, T-093, T-094, T-091, T-092, T-096, T-087, T-090
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 1, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 7 calisma agaci (ana: main); ana agacta commit'siz .prefab/.unity/.asset: 0
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
