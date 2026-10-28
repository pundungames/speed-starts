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
- `dc7db07` Faz 2 dikey dilim — Unity'de `Scenes/Menu/Home.unity` → Play: kaynak çubuğu (bilet n/3 + geri sayım), Event select (100m),
  Play → giriş kapısı ("Use Entry Ticket") → Race; koşu; **F** = geçici bitiş → Results → Race again (bilet tekrar) / Main menu.
  Yeni kayıtta bilet 0'dan başlar (başlangıç bakiyesi PDF'te yok): ilk bilet 30 dk'da gelir → test için `Data/Config/EconomyConfig`
  `startingEntryTickets` (yalnız yeni kayıt) ya da kaydı sil (`persistentDataPath/save`).
- TMP yok, legacy Text + İngilizce; UI 2400×1080 yükseklik eşleşmesi. Zenject doğrulaması: Tools → Speed Stars → Validate Zenject.

## Sıradaki iş
1. Faz 3 PDF ekonomisi: Süreli Bilet (60 dk gerçek zaman), Nakit/Mor Mücevher cüzdanı + işlem günlüğü, 3/2/1 ödül, uç durum testleri.
2. Faz 4 günlük hediye + 7 gün; Faz 5 kozmetik/Edit racer; Faz 6 High Scores (yerel, Sticker Patch'ten).
- Faz 1-2 task'ları merged (gözle bakılacak). Bitiş olayı Malik'te yok → Bridge `RunReset`'ten çıkarıyor (TEMEL-SOZLESME §11).

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
