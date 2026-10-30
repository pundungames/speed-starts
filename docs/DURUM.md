MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-30 (akşam) · main 86bff46+ (push kullanıcıda) · dal `main`

## Sürüyor
- T-054 giriş kapısı cilası · T-055 küçük borçlar (dayBoundary, Watch Replay etiketi, Leggings) — worktree'ler `speed-starts-t054/t055`.

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema fiyatı yok → 0; geliştirmede bütün temalar serbest. Normal kozmetik bedava, premium 50 Mor Mücevher (PDF).
- Başlangıç Adet Bileti = tavan (3). Günlük gün sınırı UTC; 7 gün kaçırılan gün sıfırlamaz; G6 300 Nakit, G7 PH02 şapka.
- Ödüllü reklam: 1 reklam = 1 Adet Bileti (PDF §1.2), SDK yok → editör/dev'de sahte reklam (T-050). Titreşim efekt anahtarını izler (T-052).
- Bot süreleri, emote fiyatları yer tutucu (config).

## Kullanıcıdan bekleyen
- Gizlilik / Şartlar / Destek URL'leri (ayarlarda satırlar hazır, URL gelene kadar gri).
- Malik'ten (Faz 7): yarış saati GO'da başlasın, UI üstündeki dokunma ayak sayılmasın, rakipler/sıra (`docs/TEMEL-SOZLESME.md` §11).
- Serhat'a: `PastelBuildingRow` sahne açılınca binaları yeniden üretiyor (bizim Home kopyasında kaldırıldı).

## Gözle bakılacaklar (main 86bff46; kopyada yeşil: EditMode 792, PlayMode 24, Zenject)
- 30.10 akşam: giriş kapısı yeni görünüm + Watch Ad (editörde sahte reklam, 1 bilet), güvenli alan (çentik), UI sesleri + titreşim
  (yer tutucu sesler `scripts/sfx-uret.py`), kapat-aç/saat regresyon testleri. Malik yarış içi emote ekledi (J, eeb4b57).
- 30.10: ayarlarda gizlilik/şartlar/destek + iki sıfırlama (onay penceresi), yarış sonunda PB/DB rozetleri, READY/SET/GO ölçeği,
  emote kartı yerleşimi, analizde DNF çizgisi, günlük hediye + 7 gün, ses kapalı ikonu + premium kart.

## Sıradaki iş
1. T-054/T-055 birleştir.
2. eksik-ui: Race of the Day kartı (içerik Malik), kazanç dökümü (Nakit kuralı yok), para mağazası/Upgrade (PDF'te yok).
3. Faz 8: kill/restart, saat kayması, çevrimdışı regresyonu; APK öncesi tam tur.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
