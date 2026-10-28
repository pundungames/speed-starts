MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · origin/main birleşti (881966a, push kullanıcıda) · dal `main`

## Sürüyor (worktree'ler, agent'lar)
- T-023 ana menü Serhat'ın `Scenes/Anamenu Themaları.unity` kopyasında + environment butonu temayı değiştirir — `D:\Pundun\speed-starts-t023`
- T-024 yarış seçimi v1 (Yaris_Secimi_UI_v1) — `D:\Pundun\speed-starts-t024`
- T-025 kozmetik çekirdeği (Custom N ön ayarları, premium satın alma, önizleme, CosmeticApplier) — `D:\Pundun\speed-starts-t025`
- Sonra T-026 kostüm editörü UI (Kostum_Editor_UI_v1), T-025'e bağlı.

## Kullanıcıdan bekleyen kararlar
- Tema fiyatları (Nakit): asset'te 0 = fiyat yok. Geliştirmede bütün temalar serbest (kullanıcı 28.10).
- Ön ayar sayısı (Custom 1..N): tohum 3. Power/Stamina/Height PDF'te yok → yalnız gösterim. Emote içeriği yok → sekme pasif. Face = Glasses varsayımı.
- Serhat'ın `Docs/` (büyük D) klasörü `docs/`'a taşınsın mı (index'te iki klasör; TUZAKLAR). Depo kökünde `Backups/` 370 MB.
- Başlangıç Adet Bileti (yeni kayıt) PDF'te yok → şu an 0 (`EconomyConfig.startingEntryTickets`).
- Malik'ten (Faz 7, sona bırakıldı): yarış başladı/bitti olayı, UI üstündeki dokunma ayak sayılmasın, geri sayım sinyali (`docs/TEMEL-SOZLESME.md` §11).

## Gözle bakılacaklar
- `298823b` menü karakteri: kollar aşağı, önden ışık (Play ekran görüntüsü `pipeline-logs/mergeshots-2026-10-28_162045`).
- Doğrulama: derleme + EditMode (218) + PlayMode (10) + Zenject + ekran görüntüsü; merge sonrası hepsi yeşil.

## Sıradaki iş
1. T-023/T-024/T-025 birleştir → kopyada builder + doğrulama + her tema için ekran görüntüsü.
2. T-026 kostüm editörü. 3. Faz 3 PDF ekonomisi, Faz 4 günlük hediye + 7 gün, Faz 6 High Scores.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
