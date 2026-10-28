MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · origin/main birleşti (881966a, push kullanıcıda) · dal `main`

## Sürüyor (worktree'ler, agent'lar)
- T-031 ayarlar penceresi (Music, Language, Flag, Player Name) + ana ekran Settings/Sound/Environment butonlarının açılması + yarışçı okları
  Custom 1..3 arasında — `D:\Pundun\speed-starts-t031`. HATA: tema butonu builder'da `interactable=false` (main'de çalışmıyor), T-031 düzeltir.

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema fiyatı (Nakit) yok → asset'te 0; geliştirmede bütün temalar serbest. Ön ayar Custom 1..3. Face = Glasses, Emote pasif,
  Power/Stamina/Height yalnız gösterim. Normal kozmetik bedava, premium 50 Mor Mücevher (PDF).
- Başlangıç Adet Bileti = tavan (3): PDF'te değer yok, 0 ile yeni oyuncu ilk yarış için 30 dk bekliyordu (entegrasyonda uygulanır).
- Serhat'ın `Docs/` md'leri index'te `docs/`'a taşındı (2188d99). `Backups/` (370 MB) Serhat'ın: silinmedi (pull eden makinede
  dosyaları silerdi); Serhat'a "yedekleri depo dışında tut" denecek.

## Kullanıcıdan bekleyen
- Malik'ten (Faz 7, sona bırakıldı): yarış başladı/bitti olayı, UI üstündeki dokunma ayak sayılmasın, geri sayım sinyali (`docs/TEMEL-SOZLESME.md` §11).

## Gözle bakılacaklar (main 2b9f19e, kopyada yeşil: EditMode 397, PlayMode 10, Zenject)
- Home = Serhat'ın `Anamenu Themaları` kopyası (build anında): kaidede kıyafetli karakter, bilet 3/3, Custom 1 (T-023/T-025).
- Kostüm editörü (Edit racer): 9 sekme, Serhat'ın 72 ikonu, renk seçici, Custom N, Random/Save, premium satın alma, karartma (T-026).
- Yarış seçimi v1 (T-024), tema seçimi (T-027; buton T-031'e kadar basılmıyor).
- Faz 3 ekonomi: tek işlem yolu, Süreli Bilet kullan + geri sayım, dolu envanter uyarısı, Mor Mücevher ödülü (T-028/T-029).

## Senden beklenen
- Player Settings → Resolution and Presentation: Portrait ve Portrait Upside Down kapat (oyun cihazda dikeye dönebiliyor) (T-001).
- Push (main 12+ commit önde).

## Sıradaki iş
1. T-031 birleştir → ekran görüntüsü testi (Home_eventselect/themeselect/costume/entrygate) yeşil olmalı.
2. Faz 4 günlük hediye + 7 gün, Faz 6 High Scores (ad + bayrak T-031'den). Ölü kod: EventRowView/EventRow builder'ı.
3. Builder her çalışmada prefab fileID'lerini yeniden üretiyor (büyük diff) → sahne + prefab'lar hep aynı build'den commit'lenir.

## Dikkat
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir).
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
