MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-30 (öğleden sonra) · origin/main birleşti (881966a, push kullanıcıda) · dal `main`

## Sürüyor
- Yok (bütün worktree'ler kapandı). Sıradaki: Faz 4 / Faz 6 (aşağıda).

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema fiyatı (Nakit) yok → asset'te 0; geliştirmede bütün temalar serbest. Ön ayar Custom 1..3. Face = Glasses, Emote pasif,
  Power/Stamina/Height yalnız gösterim. Normal kozmetik bedava, premium 50 Mor Mücevher (PDF).
- Başlangıç Adet Bileti = tavan (3): PDF'te değer yok, 0 ile yeni oyuncu ilk yarış için 30 dk bekliyordu (entegrasyonda uygulanır).
- Serhat'ın `Docs/` md'leri index'te `docs/`'a taşındı (2188d99). `Backups/` (370 MB) Serhat'ın: silinmedi (pull eden makinede
  dosyaları silerdi); Serhat'a "yedekleri depo dışında tut" denecek.

## Kullanıcıdan bekleyen
- Malik'ten (Faz 7, sona bırakıldı): yarış başladı/bitti olayı, UI üstündeki dokunma ayak sayılmasın, geri sayım sinyali (`docs/TEMEL-SOZLESME.md` §11).

## Gözle bakılacaklar (main 649e7d3; kopyada yeşil: EditMode 629, PlayMode 16, Zenject; çekimler pipeline-logs/shots-t040b-*, shots-t039-*, shots-t038-*)
- 30.10 eklenenler: kostüm 2./3. renk (T-035), Analiz ekranı (T-036), Liderlik tablosu Faz 6 (T-037/T-038, yerel, bot dereceleri yer tutucu),
  yarış başı READY/SET/GO (T-039; girdi GO'da açılır), Emote ekranı (T-040; prosedürel 6 emote, fiyatlar yer tutucu Nakit).
- Eksik UI listesi: `docs/eksik-ui/` (orijinal oyun telefonda gezildi; yalnız bizde olmayanlar).

## Senden beklenen
- Push. Unity'de dene: tişört/oklar (T-034), emote, liderlik, yarış başı geri sayımı.
- Malik'e: yarış saati ilk basışta başlıyor, GO'da başlamalı (T-039 DECISION). Serhat'a: şehir temasındaki binalar sahne açılınca yeniden üretiliyor.

## Sıradaki iş
1. Faz 4 günlük hediye + 7 gün (sanat yok → var olan sanatla taslak), giriş kapısı sanatı gelince.
2. eksik-ui listesinden: Ayarlar'a Gizlilik/Şartlar/Sıfırlama satırları (mağaza için şart), kazanç dökümü + DB/PB, Race of the Day kartı.
3. Backlog: ölü kod, DNF çizgisi, başlangıç yazı ölçeği, emote yerleşimi.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
