MOD: süpervizör
# DURUM — Speed Stars (speed-starts)

> Oturum başı ilk okunan dosya. En fazla 60 satır; her checkpoint'te BAŞTAN yazılır (eklenmez).
> İlk satır geçerli moddur: `MOD: süpervizör` ya da `MOD: hızlı` (bkz. docs/CALISMA-KURALLARI.md §14 A).
> Geçmiş: `docs/checkpoints/`. Alttaki OTOMATIK bölümü `scripts/tur-sonu.ps1 -DurumYaz` yazar; elle düzenleme.

Son güncelleme: 2026-11-03 · main 4abc876a (kullanıcı pull etti) · dal `main`, worktree yok

## Faz durumu (`docs/YOL-HARITASI.md`)
- Faz 0-6 bitti (T-001..T-076 merged). Faz 7 büyük ölçüde bitti (Malik, 02.11). **Şu an: Faz 8** (cila, cihaz, teslim).
- Malik 02.11'de devredilen yarım işleri main'de baştan yazdı (bizim dalı almadı): süre GO'dan (oyuncu + bot, bitiş kare içi),
  Stats'ta botlar (`IRaceTraces`), Race'te bilet penceresi, Practice biletsiz/botsuz, kilitli modda COMING SOON, opak Loading örtüsü.
  Ek olarak: 110mH engelli yarış (60mH/400mH verisi var, kapalı), kamera tepkisi + Ayarlar'da Camera kaydırıcısı, ritim ayarı,
  PlayerPrefsEditor eklentisi. Bizim alanda yazdığı yerler: `RaceSceneInstaller`, UI/Meta, builder'lar, Race/Home, SettingsPopup.
- Eski dal `agent/T-077-080-duzeltmeler` + `D:\Pundun\speed-starts-duzeltme` silindi (03.11, kullanıcı onayı). Yerel yama arşivi:
  `pipeline-logs/arsiv/agent-T-077-080-duzeltmeler-77a3c763.patch` (ok tuşları `KeyboardRunnerInput.cs` orada). Sıradaki task no: T-081.

## Verilen kararlar (kullanıcı 28.10: "kararları riskleri sen ayarla"; değiştirmek isterse söyler)
- Tema: fiyatı 0 olan tema bedava (T-070); Sky Islands premium (Mor Mücevher) ama "beleş olsun şimdilik".
- Normal kozmetik bedava, premium 50 Mor Mücevher (PDF). Ten tonları 8 (varsayılan koyu ten 462B27).
- Başlangıç Adet Bileti = tavan (3). Gün sınırı UTC; 7 gün kaçırılan gün sıfırlamaz; G6 300 Nakit, G7 PH02 şapka.
- Ödüllü reklam: 1 reklam = 1 Adet Bileti, editör/dev'de sahte reklam. Titreşim efekt anahtarını izler.
- Günlük build Mono/ARMv7; mağaza build'i (scripts/android-release.ps1) IL2CPP+ARMv7/ARM64, ayarı kalıcı değiştirmez.

## Kullanıcıdan bekleyen
- Eski rekorlar: süre artık GO'dan (tepki dahil) → yeni süreler eski PB/liderlik sürelerinden ~0.2-0.6 sn kötü. Sıfırlansın mı, kalsın mı?
- Uygulama adı + paket adı (orijinal oyunun adı marka riski) ve imza anahtarı (keystore).
- Gizlilik / Şartlar / Destek URL'leri (ayarlarda satırlar hazır, URL gelene kadar gri).
- Malik'ten (Faz 7 kalanı): UI üstündeki dokunma ayak sayılmasın; engel teması (yerde de geçiyor, bariyer devrilmiyor,
  `docs/ENGEL-TEMAS-NOTU-malik.md`); Watch Replay; 200/400 m, bayrak/Medley, Race of the Day.
- Serhat'a: `PastelBuildingRow` sahne açılınca binaları yeniden üretiyor (Race'te Malik kapattı, Home kopyamızda kaldırılmıştı).
  Malik'in 4abc876a commit'i Serhat'ın `BridgeCanalRiverside.prefab`'ını yeniden yazdı (3176 satır, çoğu Unity gürültüsü): Serhat bilsin.

## Dikkat (denge değişti, 02.11 Malik)
- `RunnerSim.stepTime` 0.4/0.22 → 0.255/0.21; bot en iyi periyot 0.32 → 0.24, tutma 0.10 → 0.08; kalibrasyon GO'dan, 10.5-22.7 sn.
- Yarış çekilişi: %30 kolay (×1.08), %20 zor (×0.96). Bu sayılar `BotRaceConfig.asset`'te; dokunma.

## Gözle bakılacaklar
- 30.10 bizim merged işler (T-072 yarış teması, T-073 menüde karakter döndürme, T-074..T-076 botlar/sıra, READY/SET/GO, HUD,
  liderlik, premium kart v2, kostüm Body/Emote/Custom 1..9) — kullanıcı "tamam" demedi.
- 02.11 Malik: "hep 1. gösteriyor" düzeldi mi, Stats'ta botlar, 3 yarıştan sonra Race again → bilet penceresi, Practice, 110mH, kamera.

## Sıradaki iş (Faz 8)
1. Malik'in değişiklikleriyle main'i kopyada doğrula: derleme + **tam suite** (kayıt/ayar, kompozisyon kökü ve denge değişti).
   main 4abc876a bizim tarafta HİÇ doğrulanmadı.
2. Telefonda APK/AAB (Zenject + IL2CPP, ses, titreşim, güvenli alan, 5 botun performansı).
3. Ana menü hızı: Home her girişte 5 tema dünyasını yüklüyor → yalnız seçili dünya (Serhat'ın prefab'ları değişmez).
4. Ok tuşları (arşivdeki `KeyboardRunnerInput.cs`), eksik-ui: Race of the Day kartı, kazanç dökümü.
5. Kill/restart, saat kayması, çevrimdışı regresyonu; APK öncesi tam tur; teslim (README, test raporu, açık kararlar).

## Dikkat (genel)
- `CLAUDE.md` Malik'in; bizim kurallar `docs/CALISMA-KURALLARI.md` (`CLAUDE.local.md` ile yüklenir). Malik'in durumu `docs/DURUM-malik.md`.
- Yerel, git'e girmeyen: `.claude/settings.json`, `.claude/settings.local.json`, `.claude/agents/`, `CLAUDE.local.md`, `pipeline-logs/`.
- Malik ve Serhat aynı main'e push ediyor; pull/push kullanıcıda. `docs/` altına yeni dosya: commit'ten sonra yol `Docs/`'a kaymış mı bak.
- Bu makinede PowerShell'de `git` bozuk (system32'de aynı adlı dosya): git komutları Bash'ten.

<!-- OTOMATIK:BASLA -->
_tur-sonu.ps1 2026-11-03 10:34 - otomatik blok, elle yazilan bu blokta kaybolur._

- **KAPI TEMIZ (cikis 0)**
- Acik needs: 2 (DECISION 2)
- Review kuyrugu: 0
- Goz bekleyen (merged): 72 - T-076, T-075, T-074, T-073, T-072, T-071, T-070, T-069
- in-progress: 0 / blocked: 0
- Uyari: govde-needs 0, test 1, analytics 0 (kapsam: status in-progress/review/merged)
- GIT: 1 calisma agaci (ana: main); ana agacta commit'siz .prefab/.unity/.asset: 0
- KOPYALAR: 2 (kopya.ps1 -Liste -Json)
<!-- OTOMATIK:BITIR -->
