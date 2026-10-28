MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-10-28 · agent-kit v2 kurulumu · dal `main` (ad3616b üstü)

## Kullanıcıdan bekleyen kararlar
- Başlangıç Adet Bileti (yeni kayıt) PDF'te yok → şu an 0; test için 3 öneriliyor (`EconomyConfig.startingEntryTickets`, kullanıcının sayısı).
- Yatay ekran: `ProjectSettings` → Portrait kapalı, diske kaydedilmedi (kullanıcı Player Settings'ten).
- Malik'ten istenecekler (UI bunlarsız kurulamaz; `docs/TEMEL-SOZLESME.md` §11): (1) yarış başladı/bitti olayı ve
  bitişte anında başa dönmemesi (`RaceController.cs:55-58` aynı karede `ResetRace`) → sonuç ekranı için; (2) dokunma
  UI üstündeyse ayak sayılmasın (`RunnerInput.cs:76-81` bütün ekranı dinliyor) → menü/duraklat butonu için;
  (3) geri sayım / başlama sinyali. Kullanıcı kararı (28.10): sona bırakıldı (Faz 7, Malik ya da biz).
- Oyuncuya görünen metin dili / yerelleştirme — md'lerle netleşir.

## Gözle bakılacaklar (öncelikli)
- `50e95cd` UI v2: yeni ana ekran + yarış HUD'u (sanat `UI/Art/`), kapat butonları çalışıyor, Home'da karakter örneği.
  Bilinen: karakter T-pozda (menü duruşu yok), ışık arkadan (siyah görünüyor), kıyafet kapalı (Faz 5) — düzeltme turu sürüyor.
- Test için bilet: `Data/Config/EconomyConfig` → Starting Entry Tickets (yalnız yeni kayıt; PDF'te değer yok, karar kullanıcıda).
- Doğrulama artık: derleme + EditMode (218) + PlayMode (9) + Zenject + ekran görüntüsü (`pipeline-logs/shots-*`).

## Sıradaki iş
1. Menü karakteri: kollar aşağı duruş, önden ışık; PlayMode ekran görüntüsü (gerçek değerlerle).
2. Faz 5 kozmetik (Serhat'ın customization UI'ı gelince) — CosmeticApplier menü karakterini giydirir.
3. Faz 3 PDF ekonomisi, Faz 4 günlük hediye + 7 gün, Faz 6 High Scores.

## Dikkat
- Unity'nin commit'siz dosyası: `ProjectSettings/ProjectSettings.asset` — dokunma, commit'e katma.
- `CLAUDE.md` Malik'in (sık değişiyor); bizim kurallar `docs/CALISMA-KURALLARI.md`, `CLAUDE.local.md` ile yüklenir.
  `install.ps1 -Fark` → "CLAUDE.md#KURALLAR isaret-yok" bilerek (1 fark).
- Yerel, git'e girmeyen (`.git/info/exclude`): `.claude/settings.json` (hook'lar), `.claude/settings.local.json` (pano),
  `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`. Başka PC'de: kit kurulumu + `claude plugin install pano@agent-kit -s local`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. Malik'in alanı hook'la kilitli (config `KorunanBelgeler`).

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
