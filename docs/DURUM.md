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
- `87dd96d` Faz 1 içerik: `Scenes/Menu/Home.unity`'yi aç → Play: ProjectContext kurulur, HomeScreen görünür, konsol temiz mi; butonlar
  punch yapar (view mantığı Faz 2'de). `Scenes/Game/Race.unity` → Play: Malik'in koşucusu koşar, üstte RaceHud yer tutucusu.
- Prefab'lar `Assets/Speed-Staes/UI/` (16 yer tutucu, yatay), config'ler `Data/Config/` (PDF değerleri; 6. gün boş, fiyatlar 0 = kapalı).
- T-001 yatay ekran: kullanıcı Player Settings'ten yapacak, sonra `ProjectSettings.asset` commit'lenir.

## Sıradaki iş
1. Faz 2 dikey dilim task'ları (`docs/YOL-HARITASI.md`): EntryTicketService + RaceEntryGate (Meta, testli), Home/EventSelect/EntryGate/
   ResourceBar view'ları, Race HUD + Bridge `RaceSessionAdapter`, Results view, Dev bitiş tetikleyicisi, Home↔Race akışı.
- Faz 1: T-002/3/4 done; T-005/6/7/8 merged (gözle bakılacak). Builder'lar: Tools → Speed Stars → Build (var olanı ezmez).
- Font: LiberationSans SDF'de ğ ı ş İ yok → Türkçe metin için font kararı (Faz 2/8).

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
