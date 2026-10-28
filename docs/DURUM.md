MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · agent-kit v2 kurulumu · dal `main` (ad3616b üstü)

## Kullanıcıdan bekleyen kararlar
- `docs/YOL-HARITASI.md` sonundaki 5 soru (Upgrade, Players, High Scores backend, ikon/efekt kimden, reklam/IAP) + sözleşme onayı.
- Malik'ten istenecekler (UI bunlarsız kurulamaz; `docs/TEMEL-SOZLESME.md` §11): (1) yarış başladı/bitti olayı ve
  bitişte anında başa dönmemesi (`RaceController.cs:55-58` aynı karede `ResetRace`) → sonuç ekranı için; (2) dokunma
  UI üstündeyse ayak sayılmasın (`RunnerInput.cs:76-81` bütün ekranı dinliyor) → menü/duraklat butonu için;
  (3) geri sayım / başlama sinyali. Kullanıcı kararı (28.10): sona bırakıldı (Faz 7, Malik ya da biz).
- Oyuncuya görünen metin dili / yerelleştirme — md'lerle netleşir.

## Gözle bakılacaklar (öncelikli)
- Yok (DOTween Setup yapıldı, `f3b5f8a`).

## Sıradaki iş
1. Kullanıcı `docs/YOL-HARITASI.md` 5 soruyu cevaplar + sözleşmeyi onaylar → Faz 1 iskele task'ları (T-001..T-008) açılır.
- Gün 0 durumu: GDD `docs/gdd/`; sözleşme taslak v2; açık ürün kararları `docs/decisions/OPEN-2026-10-28-gdd-acik-kararlar.md`;
  raporlar `reports/leaderboard-tasima-*`, `reports/kozmetik-hazirlik-*`.
2. Faz 1: `Scripts/Core/Installers` (ProjectContext + sahne installer'ları), `UiMotionConfig`, `Scenes/Menu/Home.unity`.

## Dikkat
- Unity'nin commit'siz dosyası: `ProjectSettings/ProjectSettings.asset` — dokunma, commit'e katma.
- `CLAUDE.md` Malik'in (sık değişiyor); bizim kurallar `docs/CALISMA-KURALLARI.md`, `CLAUDE.local.md` ile yüklenir.
  `install.ps1 -Fark` → "CLAUDE.md#KURALLAR isaret-yok" bilerek (1 fark).
- Yerel, git'e girmeyen (`.git/info/exclude`): `.claude/settings.json` (hook'lar), `.claude/settings.local.json` (pano),
  `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`. Başka PC'de: kit kurulumu + `claude plugin install pano@agent-kit -s local`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. Malik'in alanı hook'la kilitli (config `KorunanBelgeler`).
- Doğrulama kopyaları: `D:\Pundun\speed-starts-verify-1..2` (henüz açılmadı).

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
