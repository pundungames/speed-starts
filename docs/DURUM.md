MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · origin/main birleşti (881966a, push kullanıcıda) · dal `main`

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

## Gözle bakılacaklar (main ccdafad, kopyada yeşil: EditMode 441, PlayMode 10, Zenject; çekimler pipeline-logs/shots-t033-2026-10-28_173524)
- Home (Serhat'ın sahnesinin kopyası) + tema seçimi + yarış seçimi v1 + kostüm editörü + ayarlar + giriş kapısı + yarış sonu ekranı.
- Yarış sonu: kişisel rekor gerçek (etkinlik başına kayıt); dünya sırası/yüzdelik "--", Best of the Day "No races today" (veri Faz 6);
  tabloda yalnız oyuncu (rakipler Malik'in NPC'leri gelince). Watch Replay / Stats / Practice / High Scores pasif.
- Faz 3 ekonomi kapandı. Giriş kapısının sanatı yok (yer tutucu görünüm).

## Senden beklenen
- Push. Unity'de Player Settings → dikey kutular işaretsiz mi bak (434879b dosyadan yazıldı).
- Serhat'ın `Anamenu Themaları.unity` sahnesi sende commit'siz değişik (16:59): bilerek mi? Evet ise Home'a geçirilir.

## Sıradaki iş
1. Faz 4 günlük hediye + 7 gün; Faz 6 High Scores (ad + bayrak T-031'de, sonuç ekranı kartları bekliyor).
2. Ölü kod temizliği (`_backlog.md`); builder prefab fileID'lerini her çalışmada yeniden üretiyor → sahne + prefab'lar hep aynı build'den.
3. Android mağaza öncesi: IL2CPP + ARM64, paket adı. Giriş kapısı sanatı gelince giydirme.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
