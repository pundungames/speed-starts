MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-30 (akşam) · main bce0ddb5 (push kullanıcıda) · dal `main`

## Sürüyor
- Yok (worktree'ler kapandı). Referans oyun rakip analizi: `docs/analiz/2026-10-30-referans-oyun-rakipler.md` (Global players = sabit hayalet,
  Bots = her yarış değişen AI, Difficulty/Skill range ayarı). Rakip AI tasarım kararı kullanıcıda (pano).

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema: fiyatı 0 olan tema bedava (her build'de sahipli, T-070); Sky Islands premium (Mor Mücevher para birimi) ama "beleş olsun şimdilik".
- Normal kozmetik bedava, premium 50 Mor Mücevher (PDF). Ten tonları (8, açıktan koyuya; varsayılan bugünkü koyu ten 462B27) supervizör seçimi.
- Başlangıç Adet Bileti = tavan (3). Gün sınırı UTC; 7 gün kaçırılan gün sıfırlamaz; G6 300 Nakit, G7 PH02 şapka.
- Ödüllü reklam: 1 reklam = 1 Adet Bileti, editör/dev'de sahte reklam. Titreşim efekt anahtarını izler.
- Günlük build Mono/ARMv7; mağaza build'i (scripts/android-release.ps1) IL2CPP+ARMv7/ARM64, ayarı kalıcı değiştirmez.

## Kullanıcıdan bekleyen
- Uygulama adı + paket adı (Play/Çin mağazası; orijinal oyunun adı marka riski) ve imza anahtarı (keystore).
- Gizlilik / Şartlar / Destek URL'leri (ayarlarda satırlar hazır, URL gelene kadar gri).
- Malik'ten (Faz 7): yarış saati GO'da başlasın, UI üstündeki dokunma ayak sayılmasın, rakipler/sıra (`docs/TEMEL-SOZLESME.md` §11).
- Serhat'a: `PastelBuildingRow` sahne açılınca binaları yeniden üretiyor (bizim Home kopyasında kaldırıldı).

## Gözle bakılacaklar (main bce0ddb5; kopyada yeşil: EditMode 836, PlayMode 34, Zenject)
- 30.10 (6bcc2159): T-072 yarışta seçili temanın akan dünyası (5 tema, düz pist kapalı); kopyada derleme + 28 test yeşil.
- 30.10 (d36bcd55): T-073 menüde karakteri UI dışından yatay sürükleyerek döndürme (bırakınca yavaşlar, açıda kalır).
- 30.10 (6d9ee299): T-074..T-076 yarışta 5 bot (bizim fizikle, kalibre: beceri 0→22.3 sn, 1→16.0 sn), HUD'da yer, sonuç listesi;
  EditMode 111 + PlayMode 27 + 2 çekim yeşil. Malik'e devir notu `docs/MALIK-DEVIR-rakip-botlar.md`. Telefonda performans ölçülmedi.
- 30.10 akşam: READY/SET/GO banner'lı (dama bayrak), yarış HUD'u yeni parçalarla, liderlik pencereleri/durumları, premium kart v2
  (altın/kırmızı), kostümde Top + Body (ten) + Emote sekmesi + Custom 1..9, ana menüde Upgrade/Players yok, tema/emote'ta ortak sayaç,
  Sky Islands premium (bedava). APK 64 MB (ASTC), mağaza AAB 57 MB (debug imzalı).

## Sıradaki iş
1. Telefon bağlanınca APK/AAB'yi cihazda dene (Zenject + IL2CPP, ses, titreşim, güvenli alan).
2. eksik-ui: Race of the Day kartı (içerik Malik), kazanç dökümü (Nakit kuralı yok), para mağazası/Upgrade (PDF'te yok).
3. Faz 8: kill/restart, saat kayması, çevrimdışı regresyonu; APK öncesi tam tur.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
