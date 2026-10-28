MOD: hızlı
# DURUM — Malik (karakter mekaniği)

> Malik'in oturum başı dosyası (agent-kit v2). İlk satır geçerli moddur: `MOD: hızlı` (tek oturum, Unity MCP ile ölç-ayarla)
> ya da `MOD: süpervizör` (iş alt agent'lara). Ayrıntılı mekanik notları `CLAUDE.md` ve `PlayerController.md`'de; burası kısa durum.

Son güncelleme: 2026-10-28 · kit kurulumu (Pundun oturumu hazırladı)

## Bekleyen kararlar
- UI tarafının mekanikten beklediği olaylar (`docs/TEMEL-SOZLESME.md` §11): yarış başladı/bitti, bitişte anında başa dönmeme,
  UI üstündeki dokunmanın adım sayılmaması — kimin yapacağına kullanıcı (Pundun) karar verecek.

## Sıradaki iş
- (Malik doldurur)

## Dikkat
- UI/meta tarafının dosyaları kilitli (`agent-kit.config.local.psd1` → `KorunanBelgeler`). Okumak serbest.
- Mekaniğin public API'si (`RaceController`, `RunnerBody` olayları/alanları) UI tarafındaki adaptör tarafından okunuyor:
  değiştirmeden önce Pundun'a haber ver.

<!-- OTOMATIK:BASLA -->
(henüz çalıştırılmadı: powershell -File scripts/tur-sonu.ps1 -DurumYaz)
<!-- OTOMATIK:BITIR -->
