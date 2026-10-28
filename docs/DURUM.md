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
- Unity'ye geçince Faz 1 kodu derlenir (`c3dbe46`, kopyada 116 test yeşil): konsol temiz mi. Görülecek ekran yok (T-006/T-007 bekliyor).

## Sıradaki iş
1. **Oturumu yeniden aç** (pano + Unity MCP yüklenir) → T-001 yatay ekran, T-006 Home/Race sahneleri + config asset'leri (PDF değerleri) +
   ProjectContext, T-007 yer tutucu UI prefab'ları — scene-integrator, sırayla (sahneye dokunan iş tek başına).
2. Faz 2 dikey dilim task'ları (`docs/YOL-HARITASI.md`): EntryTicketService, RaceEntryGate, kaynak çubukları, Event Select, Entry Gate,
   Race HUD (Bridge adaptörü), Results, Dev bitiş.
- Faz 1 kod: T-002/3/4 done, T-005/T-008 merged (gözle bakılacak yanı T-006/T-007'de). Doğrulama kopyası `D:\Pundun\speed-starts-verify-1`
  (Library hazır, ~30 sn derleme).

## Dikkat
- Unity'nin commit'siz dosyası: `ProjectSettings/ProjectSettings.asset` — dokunma, commit'e katma.
- `CLAUDE.md` Malik'in (sık değişiyor); bizim kurallar `docs/CALISMA-KURALLARI.md`, `CLAUDE.local.md` ile yüklenir.
  `install.ps1 -Fark` → "CLAUDE.md#KURALLAR isaret-yok" bilerek (1 fark).
- Yerel, git'e girmeyen (`.git/info/exclude`): `.claude/settings.json` (hook'lar), `.claude/settings.local.json` (pano),
  `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`. Başka PC'de: kit kurulumu + `claude plugin install pano@agent-kit -s local`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. Malik'in alanı hook'la kilitli (config `KorunanBelgeler`).
- Tur kapısı kapalı (bilerek): T-005 SCENE ihtiyacı açık, T-006/T-007 ile kapanır.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
