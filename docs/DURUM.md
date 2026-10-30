MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-30 (akşam) · main 7ffb62a (push kullanıcıda) · dal `main`

## Sürüyor
- T-050 giriş kapısı v2 + sahte ödüllü reklam · T-051 güvenli alan · T-052 ses ve titreşim (worktree'ler `speed-starts-t050..t052`).

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema fiyatı yok → 0; geliştirmede bütün temalar serbest. Normal kozmetik bedava, premium 50 Mor Mücevher (PDF).
- Başlangıç Adet Bileti = tavan (3). Günlük gün sınırı UTC; 7 gün kaçırılan gün sıfırlamaz; G6 300 Nakit, G7 PH02 şapka.
- Ödüllü reklam: 1 reklam = 1 Adet Bileti (PDF §1.2), SDK yok → editör/dev'de sahte reklam (T-050). Titreşim efekt anahtarını izler (T-052).
- Bot süreleri, emote fiyatları yer tutucu (config).

## Kullanıcıdan bekleyen
- Gizlilik / Şartlar / Destek URL'leri (ayarlarda satırlar hazır, URL gelene kadar gri).
- Malik'ten (Faz 7): yarış saati GO'da başlasın, UI üstündeki dokunma ayak sayılmasın, rakipler/sıra (`docs/TEMEL-SOZLESME.md` §11).
- Serhat'a: `PastelBuildingRow` sahne açılınca binaları yeniden üretiyor (bizim Home kopyasında kaldırıldı).

## Gözle bakılacaklar (main 7ffb62a; kopyada yeşil: EditMode 708, PlayMode 19, Zenject)
- 30.10: ayarlarda gizlilik/şartlar/destek + iki sıfırlama (onay penceresi), yarış sonunda PB/DB rozetleri, READY/SET/GO ölçeği,
  emote kartı yerleşimi, analizde DNF çizgisi, günlük hediye + 7 gün, ses kapalı ikonu + premium kart.

## Sıradaki iş
1. T-050/T-051/T-052 birleştir.
2. eksik-ui: Race of the Day kartı (içerik Malik), kazanç dökümü (Nakit kuralı yok), para mağazası/Upgrade (PDF'te yok).
3. Faz 8: kill/restart, saat kayması, çevrimdışı regresyonu; APK öncesi tam tur.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
