MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · origin/main birleşti (881966a, push kullanıcıda) · dal `main`

## Sürüyor (worktree'ler, agent'lar)
- T-026 kostüm editörü UI (Kostum_Editor_UI_v1 + Serhat'ın 72 ikonu) — `D:\Pundun\speed-starts-t026`
- T-028 Faz 3 ekonomi çekirdeği (tek işlem yolu, Süreli Bilet, Mor Mücevher ödülü, dolu envanter) — `D:\Pundun\speed-starts-t028`
- Sonra T-029 Faz 3 ekonomi UI (T-028'e bağlı).

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema fiyatı (Nakit) yok → asset'te 0; geliştirmede bütün temalar serbest. Ön ayar Custom 1..3. Face = Glasses, Emote pasif,
  Power/Stamina/Height yalnız gösterim. Normal kozmetik bedava, premium 50 Mor Mücevher (PDF).
- Başlangıç Adet Bileti = tavan (3): PDF'te değer yok, 0 ile yeni oyuncu ilk yarış için 30 dk bekliyordu (entegrasyonda uygulanır).
- Serhat'ın `Docs/` md'leri index'te `docs/`'a taşındı (2188d99). `Backups/` (370 MB) Serhat'ın: silinmedi (pull eden makinede
  dosyaları silerdi); Serhat'a "yedekleri depo dışında tut" denecek.

## Kullanıcıdan bekleyen
- Malik'ten (Faz 7, sona bırakıldı): yarış başladı/bitti olayı, UI üstündeki dokunma ayak sayılmasın, geri sayım sinyali (`docs/TEMEL-SOZLESME.md` §11).

## Gözle bakılacaklar (main 0fc8f43, kopyada yeşil: EditMode 299, PlayMode 10, Zenject, 5 tema çekimi)
- Home artık Serhat'ın `Anamenu Themaları` sahnesinin kopyası: kaidede kıyafetli karakter, bilet 3/3, Custom 1 (T-023/T-025).
- Sağ üst resim butonu → tema seçimi (oklar önizler, USE / IN USE; geliştirmede hepsi serbest) (T-027).
- Yarış seçimi v1: Sprints/Hurdles/Relays, mesafe, Race of the Day, Practice, Continue (yalnız 100m oynanır) (T-024).
- Çekimler: `pipeline-logs/shots-int2-2026-10-28_164506/` (Home_play, home_theme_0..4, Race_play).

## Senden beklenen
- Player Settings → Resolution and Presentation: Portrait ve Portrait Upside Down kapat (oyun cihazda dikeye dönebiliyor) (T-001).
- Push (main 12+ commit önde).

## Sıradaki iş
1. T-026 ve T-028 birleştir → T-029 → Faz 3 kapanışı (uç durum regresyonu kopyada tam suite).
2. Faz 4 günlük hediye + 7 gün, Faz 6 High Scores. Ölü kod: EventRowView/EventRow builder'ı (T-024 sonrası).

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
